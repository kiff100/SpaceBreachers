using Opsive.UltimateCharacterController.Game;
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


    private Camera mainCamera;
    private Collider2D turretCollider;
    private float fireHoldStartTime;
    private bool wasFirePressed;

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
            if (Time.time - lastFired >= fireDelayInSeconds)
            {
                float holdDuration = Time.time - fireHoldStartTime;
                Vector2 direction = GetFireDirection();
                FireProjectile(holdDuration, direction);
                lastFired = Time.time;
                Debug.Log($"Fire! Hold duration: {holdDuration:F2} seconds, Direction: {direction}");
            }
            wasFirePressed = false;
        }
    }

    Vector2 GetFireDirection()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();            
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 direction = (mouseWorldPos - firePoint.position).normalized;
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
        if (magnetProjectile != null)
        {
            magnetProjectile.Fire(holdDuration, direction);
        }
    }
}
