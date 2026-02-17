using System;
using TMPro;
using UnityEngine;

public class MagnetProjectile : MonoBehaviour
{
    public float maxSpeed = 20f;
    public float minSpeed = 5f;
    public float decelerationRate = 2f; // Speed decrease per second
    public TurretControls turret; // Reference to the turret script that fired the projectile
    public int shotIndex; // Identifier for the shot, useful for tracking
    public bool isTethered = true;

    private Rigidbody2D rb;
    private float holdDuration;
    private Vector2 travelDirection = Vector2.right;
    private float currentSpeed;
    private bool returningToTurret = false;
    internal TetherLine tetherLine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        //this.gameObject.transform.rotation = 

        if (rb == null)
        {
            Debug.LogError("MagnetProjectile requires a Rigidbody2D component!");
            return;
        }

        Debug.Log($"Shot fired at speed{currentSpeed:F2}");
    }

    void FixedUpdate()
    {
        if (returningToTurret)
        {
            // Set the travel direction as the source can keep moving, so we need to update the direction every frame
            travelDirection = (turret.firePoint.position - transform.position).normalized;
            if (Vector2.Distance(transform.position, turret.firePoint.position) < 0.5f)
            {
                transform.position = turret.firePoint.position; // Snap to exact position
                currentSpeed = 0;

                turret.ShotReturned(this);

                // Destroy the tether line if it exists
                if (tetherLine != null)
                {
                    Destroy(tetherLine.gameObject);
                }

                // Destroy the projectile
                Destroy(gameObject);
            }
        }
        else
        {
            currentSpeed -= decelerationRate * Time.deltaTime;
        }
        if (tetherLine != null)
        {
            this.tetherLine.UpdatePosition(turret.firePoint.position, transform.position); // Update the tether line position every frame
        }
        currentSpeed = Mathf.Max(currentSpeed, 0f); // Clamp to zero
        rb.linearVelocity = travelDirection * currentSpeed;
    }

    public void Fire(float duration, Vector2 direction)
    {
        holdDuration = duration;
        travelDirection = direction;
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(holdDuration));
    }

    internal void ReturnToTurret()
    {
        returningToTurret = true;
        currentSpeed = maxSpeed; // Reset speed for return
        Debug.Log($"Returning projectile to turret at direction{travelDirection}");
    }

    public override bool Equals(object other)
    {
        if (other == null) return false;

        if (other is MagnetProjectile otherProjectile)
        {
            return this.shotIndex == otherProjectile.shotIndex;
        }
        else
        {
            return false;
        }
    }
}
