using System;
using System.Collections.Generic;
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
    public float magnetRadius = 15f; // Radius of the magnet effect
    public float magnetForce = 10f; // Force of the magnet pull
    public float attachDistance = 0.5f; // Distance at which objects attach to the magnet

    private Rigidbody2D rb;
    private float holdDuration;
    private Vector2 travelDirection = Vector2.right;
    private float currentSpeed;
    private bool returningToTurret = false;
    internal TetherLine tetherLine;
    private List<Transform> attachedObjects = new List<Transform>(); // List of attached metal objects

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError("MagnetProjectile requires a Rigidbody2D component!");
            return;
        }

        Debug.Log($"Shot fired at speed {currentSpeed:F2}");
    }

    void FixedUpdate()
    {
        if (returningToTurret)
        {
            // Set the travel direction as the source can keep moving, so we need to update the direction every frame
            travelDirection = (turret.firePoint.position - transform.position).normalized;
            rb.linearVelocity = travelDirection * maxSpeed; // Update velocity only when returning
            
            if (Vector2.Distance(transform.position, turret.firePoint.position) < 0.5f)
            {
                transform.position = turret.firePoint.position; // Snap to exact position
                rb.linearVelocity = Vector2.zero;

                turret.ShotReturned(this);

                // Destroy the tether line if it exists
                if (tetherLine != null)
                {
                    Destroy(tetherLine.gameObject);
                }

                // Detach all attached objects
                foreach (Transform attached in attachedObjects)
                {
                    attached.SetParent(null); // Detach from projectile
                    Rigidbody2D attachedRb = attached.GetComponent<Rigidbody2D>();
                    if (attachedRb != null)
                    {
                        attachedRb.bodyType = RigidbodyType2D.Dynamic; // Restore physics
                    }

                    Destroy(attached.gameObject);

                    // TODO: Update the amount of metal collected in the ship cargo
                }

                // Clear the attached objects list
                attachedObjects.Clear();

                // Destroy the projectile
                Destroy(gameObject);
            }
        }

        // Apply magnet effect
        ApplyMagnetEffect();
        
        if (tetherLine != null)
        {
            this.tetherLine.UpdatePosition(turret.firePoint.position, transform.position); // Update the tether line position every frame
        }
    }

    private void ApplyMagnetEffect()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, magnetRadius);

        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.CompareTag("Metal"))
            {
                Rigidbody2D targetRb = collider.GetComponent<Rigidbody2D>();
                if (targetRb != null && !attachedObjects.Contains(collider.transform))
                {
                    float distanceToTarget = Vector2.Distance(transform.position, collider.transform.position);
                    
                    if (distanceToTarget < attachDistance)
                    {
                        // Attach the object to the magnet
                        AttachObject(collider.transform, targetRb);
                    }
                    else
                    {
                        // Apply attraction force
                        Vector2 directionToProjectile = (transform.position - collider.transform.position).normalized;
                        targetRb.AddForce(directionToProjectile * magnetForce, ForceMode2D.Force);
                    }
                }
            }
        }
    }

    private void AttachObject(Transform objectTransform, Rigidbody2D targetRb)
    {
        // Make the object a child of the projectile
        objectTransform.SetParent(transform);
        
        // Stop its physics simulation
        targetRb.bodyType = RigidbodyType2D.Kinematic;
        targetRb.linearVelocity = Vector2.zero;
        targetRb.angularVelocity = 0f;
        
        // Add to attached list
        attachedObjects.Add(objectTransform);
        
        Debug.Log($"Attached metal object to magnet. Total attached: {attachedObjects.Count}");
    }

    public void Fire(float duration, Vector2 direction)
    {
        holdDuration = duration;
        travelDirection = direction;
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(holdDuration));

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Apply initial velocity only once
        rb.linearVelocity = travelDirection * currentSpeed;
        
        // Apply deceleration via drag instead of manually updating velocity
        rb.linearDamping = decelerationRate;
    }

    internal void ReturnToTurret()
    {
        returningToTurret = true;
        rb.linearDamping = 0f; // Remove drag so it moves at constant speed back
        Debug.Log($"Returning projectile to turret at direction {travelDirection}");
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
