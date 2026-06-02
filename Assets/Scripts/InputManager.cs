using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private GameObject breacherSoldierPrefab;
    [SerializeField] private Transform playerShip;
    [SerializeField] private Transform targetShip;
    [SerializeField] private GameObject boardButton;
    [SerializeField] private InputAction interactAction;

    private TurretControls turretControls;
    private bool isBoardingEnabled = false;
    private GameObject spawnedSoldier;
    private Button boardButtonComponent;
    private float fireHoldStartTime;
    private bool wasFirePressed;
    private InputAction pauseAction;
    private bool isGamePaused = false;

    void Start()
    {
        if (boardButton != null)
        {
            boardButtonComponent = boardButton.GetComponent<Button>();
        }

        // Set up the interact action
        interactAction = InputSystem.actions.FindAction("Fire");

        if (interactAction != null)
        {
            interactAction.started += OnInteractPerformed;
            interactAction.canceled += OnInteractCanceled;
            interactAction.Enable();
        }

        // Set up the pause action
        pauseAction = InputSystem.actions.FindAction("Pause");

        if (pauseAction != null)
        {
            pauseAction.started += OnPausePerformed;
            pauseAction.Enable();
        }
        else
        {
            Debug.LogWarning("InputManager: 'Pause' action not found in Input Actions!");
        }

        turretControls = targetShip.gameObject.GetComponentInChildren<TurretControls>();
    }

    void OnDestroy()
    {
        // Clean up the action listeners
        if (interactAction != null)
        {
            interactAction.started -= OnInteractPerformed;
            interactAction.canceled -= OnInteractCanceled;
        }

        if (pauseAction != null)
        {
            pauseAction.started -= OnPausePerformed;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Check if the button was clicked
        if (IsButtonClicked(boardButton))
        {
            ToggleBoardButton();
        }
        // If boarding is disabled and turret is available, start tracking fire hold
        else if (!isBoardingEnabled && turretControls != null)
        {
            fireHoldStartTime = Time.time;
            wasFirePressed = true;
            Debug.Log("Fire button pressed");
        }
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        // Handle fire release if boarding is not enabled
        if (wasFirePressed && !isBoardingEnabled && turretControls != null)
        {
            float holdDuration = Time.time - fireHoldStartTime;
            turretControls.OnFireReleased(holdDuration);
            Debug.Log($"Fire button released after {holdDuration:F2} seconds");
            wasFirePressed = false;
        }
        // Handle boarding target detection if boarding is enabled
        else if (isBoardingEnabled)
        {
            Transform boardableTarget = DetectBoardableTarget();
            if (boardableTarget != null)
            {
                HandleBoardingCommand(boardableTarget);
            }
        }
    }

    private Transform DetectBoardableTarget()
    {
        // Get the mouse position
        Vector3 mousePos = Mouse.current.position.ReadValue();

        // Create a ray from the camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Vector2 rayOrigin = new Vector2(ray.origin.x, ray.origin.y);
        Vector2 rayDirection = new Vector2(ray.direction.x, ray.direction.y).normalized;

        // Perform 2D raycast for 2D colliders
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection);

        if (hit.collider != null)
        {
            // Check if the hit object has the "Boardable" tag
            if (hit.collider.CompareTag("Boardable"))
            {
                Debug.Log($"Boardable target detected: {hit.collider.gameObject.name}");
                return hit.collider.transform;
            }
            else
            {
                Debug.Log($"Clicked on {hit.collider.gameObject.name}, but it is not boardable.");
            }
        }
        else
        {
            Debug.Log("No object hit by raycast.");
        }

        return null;
    }

    private bool IsButtonClicked(GameObject button)
    {
        if (button == null)
        {
            return false;
        }

        // Get the RectTransform of the button
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            Debug.LogWarning("Board button does not have a RectTransform component");
            return false;
        }

        // Get the current mouse position
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Check if the mouse position is within the button's rect
        return RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePos);
    }

    private void ToggleBoardButton()
    {
        if (boardButtonComponent != null)
        {
            boardButtonComponent.interactable = !boardButtonComponent.interactable;
            isBoardingEnabled = !boardButtonComponent.interactable;

            if (isBoardingEnabled)
            {
                Debug.Log("Board button toggled ON - Boarding enabled");
            }
            else
            {
                Debug.Log("Board button toggled OFF - Boarding disabled");
            }
        }
    }

    private void HandleBoardingCommand(Transform boardableTarget = null)
    {
        // Ensure prefab and player ship are assigned
        if (breacherSoldierPrefab == null || playerShip == null)
        {
            Debug.LogWarning("BreacherSoldier Prefab or Player Ship not assigned to InputManager");
            return;
        }

        // Use the detected boardable target, or fall back to the default target ship
        Transform targetForSoldier = boardableTarget ?? targetShip;

        if (targetForSoldier == null)
        {
            Debug.LogWarning("No target ship assigned or detected");
            return;
        }

        // Spawn the BreacherSoldier at the player ship position
        spawnedSoldier = Instantiate(breacherSoldierPrefab, playerShip.position, Quaternion.identity);
        BreacherSoldier breacherSoldier = spawnedSoldier.GetComponentInChildren<BreacherSoldier>();

        // Set the target ship destination
        breacherSoldier.SetTargetShip(targetForSoldier);
        breacherSoldier.SetPlayerShip(playerShip);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;

        if (isGamePaused)
        {
            Debug.Log("Game paused");
        }
        else
        {
            Debug.Log("Game resumed");
        }
    }
}
