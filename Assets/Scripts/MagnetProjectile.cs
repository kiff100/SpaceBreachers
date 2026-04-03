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
    public ObjectInventory playerInventory; // Reference to the player's inventory for collecting items
    public int shotIndex; // Identifier for the shot, useful for tracking
    public bool isTethered = true;
    public int tetherLength = 10; // Maximum length for tethering back to the turret
    public float tetherPullForce = 15f; // Force applied when rope limit is reached
    public float magnetRadius = 15f; // Radius of the magnet effect
    public float magnetForce = 10f; // Force of the magnet pull
    public float attachDistance = 0.5f; // Distance at which objects attach to the magnet
    public float rotationSpeed = 10f; // Speed at which projectile rotates to face tether

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

        // Configure projectile physics independent from ship
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;

        // Add player inventory reference here from turret reference
        playerInventory = turret.transform.parent.GetComponent<ObjectInventory>();

        Debug.Log($"Shot fired at speed {currentSpeed:F2}");
    }

    void FixedUpdate()
    {
        float distanceFromTurret = Vector2.Distance(transform.position, turret.firePoint.position);

        // Rotate to face the tether line
        RotateTowardTether();

        if (returningToTurret)
        {
            // Set the travel direction as the source can keep moving, so we need to update the direction every frame
            travelDirection = (turret.firePoint.position - transform.position).normalized;
            
            // Apply force to move toward turret at maxSpeed
            Vector2 requiredVelocity = travelDirection * maxSpeed;
            Vector2 velocityChange = requiredVelocity - rb.linearVelocity;
            rb.AddForce(velocityChange * rb.mass, ForceMode2D.Force);
            
            if (distanceFromTurret < 0.5f)
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

                    // Try to transfer the object to player inventory
                    if (playerInventory != null)
                    {
                        // Check if the object has a CollectibleItem component
                        CollectibleItem collectibleItem = attached.GetComponent<CollectibleItem>();

                        ObjectInventory.ItemType itemType = ObjectInventory.ItemType.ScrapMetal;
                        float quantity = 1f;

                        if (collectibleItem != null)
                        {
                            itemType = collectibleItem.ItemType;
                            quantity = collectibleItem.Quantity;
                        }
                        else
                        {
                            Debug.LogWarning($"CollectibleItem component not found on {attached.gameObject.name}, defaulting to ScrapMetal x1");
                        }

                        // Try to add the item to inventory
                        bool transferSuccessful = playerInventory.AddItem(itemType, quantity);

                        if (transferSuccessful)
                        {
                            Debug.Log($"Collected {quantity} {itemType} from {attached.gameObject.name}");
                            Destroy(attached.gameObject);
                        }
                        else
                        {
                            Debug.LogWarning($"Player inventory full! Cannot collect {quantity} {itemType}. Detaching: {attached.gameObject.name}");
                            // Don't destroy, just leave it floating
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Player inventory not assigned to MagnetProjectile");
                        Destroy(attached.gameObject);
                    }

                    // TODO: Update the amount of metal collected in the ship cargo
                }

                // Clear the attached objects list
                attachedObjects.Clear();

                // Destroy only the projectile, not the ship
                Destroy(gameObject);
            }
        }

        // Apply magnet effect
        ApplyMagnetEffect();

        // Apply rope effect when tether length is exceeded
        if (distanceFromTurret > tetherLength && !returningToTurret)
        {
            Vector2 directionToTurret = (turret.firePoint.position - transform.position).normalized;
            
            // Check if projectile is moving away from the turret
            float velocityDotProduct = Vector2.Dot(rb.linearVelocity, -directionToTurret);
            
            if (velocityDotProduct > 0) // Moving away from turret
            {
                // Remove the outward velocity component (snap the rope)
                Vector2 outwardVelocity = velocityDotProduct * -directionToTurret;
                rb.linearVelocity -= outwardVelocity;
                
                // Apply strong impulse to yank back (rope tension)
                rb.AddForce(directionToTurret * tetherPullForce, ForceMode2D.Impulse);
            }
        }

        if (tetherLine != null)
        {
            this.tetherLine.UpdatePosition(turret.firePoint.position, transform.position); // Update the tether line position every frame
        }
    }

    private void RotateTowardTether()
    {
        // Calculate direction from turret to projectile
        Vector2 tetherDirection = (transform.position - turret.firePoint.position).normalized;
        
        // Calculate target angle (perpendicular to tether line so flat side faces perpendicular)
        float targetAngle = Mathf.Atan2(tetherDirection.y, tetherDirection.x) * Mathf.Rad2Deg;
        
        // Get current rotation
        float currentAngle = transform.eulerAngles.z;
        
        // Smoothly rotate toward target angle
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
        transform.eulerAngles = new Vector3(0, 0, newAngle);
    }

    private void ApplyMagnetEffect()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, magnetRadius);

        foreach (Collider2D collider in nearbyColliders)
        {
            // Skip the ship/turret and its children
            if (collider.transform.IsChildOf(turret.transform))
            {
                continue;
            }

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

        // Swap the physics layer to projectile so that it is ignored by the ship
        objectTransform.gameObject.layer = LayerMask.NameToLayer("Projectiles");
        
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

        // Apply initial velocity as an impulse (instantaneous force)
        rb.AddForce(travelDirection * currentSpeed * rb.mass, ForceMode2D.Impulse);
        
        // Apply deceleration via linear damping
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
