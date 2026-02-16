using Opsive.Shared.Utility;
using Opsive.UltimateCharacterController.Game;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurretControls : MonoBehaviour
{
    InputAction moveAction;
    InputAction fireAction;

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float moveSpeed = 5f;
    public int fireDelayInSeconds = 5;
    public float lastFired = 0f;
    public CinemachineCamera vcam;
    public int maxProjectiles = 1;
    public GameObject tetherLinePrefab;

    private Camera mainCamera;
    private Collider2D turretCollider;
    private float fireHoldStartTime;
    private bool wasFirePressed;
    private List<MagnetProjectile> projectiles = new List<MagnetProjectile>();
    private List<TetherLine> tetherLines = new List<TetherLine>();

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        fireAction = InputSystem.actions.FindAction("Fire");
        turretCollider = GetComponent<Collider2D>();
        lastFired = -fireDelayInSeconds;
        // Use the Cinemachine camera's output camera
        CinemachineBrain cinemachineBrain = CinemachineBrain.GetActiveBrain(0);
        vcam = (CinemachineCamera)cinemachineBrain.ActiveVirtualCamera;
    }

    void Update()
    {
        HandleMovement();
        HandleFire();
    }

    void HandleMovement()
    {
        if (moveAction.IsPressed())
        {
            Vector2 moveValue = moveAction.ReadValue<Vector2>();
            Vector3 newPosition = transform.position + new Vector3(moveValue.x * moveSpeed * Time.deltaTime, 0, 0);
            
            if (IsWithinScreenBounds(newPosition))
            {
                transform.position = newPosition;
                Debug.Log($"Move: {moveValue}");
            }
        }
    }

    void HandleFire()
    {
        if (fireAction.IsPressed())
        {
            if (!wasFirePressed)
            {
                fireHoldStartTime = Time.time;
                wasFirePressed = true;
            }
        }
        else if (wasFirePressed)
        {
            if (Time.time - lastFired >= fireDelayInSeconds && projectiles.Count < maxProjectiles)
            {
                float holdDuration = Time.time - fireHoldStartTime;
                Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
                mouseScreenPos.z = -Camera.main.transform.position.z;
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                Vector2 direction = GetProjectileDirection(mouseWorldPos, this.firePoint.position);
                FireProjectile(holdDuration, direction);
                lastFired = Time.time;
                Debug.Log($"Fire! Hold duration: {holdDuration:F2} seconds, Direction: {direction}");
            }
            else if (projectiles.Count >= maxProjectiles)
            {
                foreach (MagnetProjectile projectile in projectiles)
                {
                    if (projectile != null)
                    {
                        projectile.ReturnToTurret();
                    }
                }
            }
                wasFirePressed = false;
        }
    }

    Vector2 GetProjectileDirection(Vector3 destination, Vector3 origin)
    {
        Vector2 direction = (destination - origin).normalized;
        return direction;
    }

    bool IsWithinScreenBounds(Vector3 position)
    {
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(position);
        
        if (turretCollider != null)
        {
            Bounds bounds = turretCollider.bounds;
            float halfWidth = bounds.extents.x;
            
            return screenPoint.x - halfWidth > 0 && 
                   screenPoint.x + halfWidth < Screen.width &&
                   screenPoint.y > 0 && 
                   screenPoint.y < Screen.height;
        }
        
        return screenPoint.x > 0 && screenPoint.x < Screen.width &&
               screenPoint.y > 0 && screenPoint.y < Screen.height;
    }

    void FireProjectile(float holdDuration, Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab not assigned!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        MagnetProjectile magnetProjectile = projectile.GetComponent<MagnetProjectile>();
        projectiles.Add(magnetProjectile);
        if (magnetProjectile != null)
        {
            magnetProjectile.shotIndex = projectiles.Count - 1;
            magnetProjectile.turret = this;
            magnetProjectile.Fire(holdDuration, direction);

            if (magnetProjectile.isTethered)
            {
                Debug.Log($"Creating tether line for projectile {magnetProjectile.shotIndex}");
                GameObject tetherLineObj = Instantiate(tetherLinePrefab);
                TetherLine tetherLine = tetherLineObj.GetComponent<TetherLine>();
                tetherLines.Add(tetherLine);

                if (tetherLine != null)
                {
                    Debug.Log($"Creating tether line for projectile {magnetProjectile.shotIndex}");
                    tetherLine.UpdatePosition(this.transform.position, magnetProjectile.transform.position);
                    magnetProjectile.tetherLine = tetherLine;
                }
            }
        }
    }

    internal void ShotReturned(MagnetProjectile magnetProjectile)
    {
        tetherLines.Remove(magnetProjectile.tetherLine);
        projectiles.Remove(magnetProjectile);
    }
}
