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
        
        if (fireAction == null)
        {
            Debug.LogError("Fire action not found in input system!");
        }
        
        turretCollider = GetComponent<Collider2D>();
        lastFired = -fireDelayInSeconds;
        
        CinemachineBrain cinemachineBrain = CinemachineBrain.GetActiveBrain(0);
        if (cinemachineBrain == null)
        {
            Debug.LogError("No active Cinemachine brain found!");
            return;
        }
        
        vcam = (CinemachineCamera)cinemachineBrain.ActiveVirtualCamera;
    }

    void Update()
    {
        HandleFire();
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
                if (Camera.main == null)
                {
                    Debug.LogError("Main camera not found!");
                    wasFirePressed = false;
                    return;
                }

                if (firePoint == null)
                {
                    Debug.LogError("Fire point not assigned!");
                    wasFirePressed = false;
                    return;
                }

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

    void FireProjectile(float holdDuration, Vector2 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab not assigned!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        MagnetProjectile magnetProjectile = projectile.GetComponent<MagnetProjectile>();
        projectiles.Add(magnetProjectile);
        if (magnetProjectile != null)
        {
            magnetProjectile.shotIndex = projectiles.Count - 1;
            magnetProjectile.turret = this;
            magnetProjectile.Fire(holdDuration, direction);

            if (magnetProjectile.isTethered)
            {
                GameObject tetherLineObj = Instantiate(tetherLinePrefab);
                TetherLine tetherLine = tetherLineObj.GetComponent<TetherLine>();
                tetherLines.Add(tetherLine);

                if (tetherLine != null)
                {
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
