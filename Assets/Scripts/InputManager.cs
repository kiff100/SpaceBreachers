using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class InputManager : MonoBehaviour
{
    // Number of selectable digit slots (keys 0 through 9).
    private const int SlotCount = 10;
    // The digit label of the boarding button.
    private const int BoardSlot = 2;

    [SerializeField] private GameObject breacherSoldierPrefab;
    [SerializeField] private Transform playerShip;
    [SerializeField] private Transform targetShip;

    private TurretControls turretControls;
    private bool isBoardingEnabled = false;
    private GameObject spawnedSoldier;
    private float fireHoldStartTime;
    private bool wasFirePressed;

    private InputAction interactAction;
    private InputAction pauseAction;
    private bool isGamePaused = false;

    // Digit-indexed slots (0-9). A slot is null when no button carries that label.
    private readonly Button[] _slotButtons = new Button[SlotCount];
    private readonly InputAction[] _slotActions = new InputAction[SlotCount];
    private readonly System.Action<InputAction.CallbackContext>[] _slotKeyHandlers =
        new System.Action<InputAction.CallbackContext>[SlotCount];
    private readonly UnityAction[] _slotClickHandlers = new UnityAction[SlotCount];

    // Index of the currently active slot, or -1 when none is active.
    private int _activeSlot = -1;
    // Guards against re-entrancy when programmatically invoking a button's onClick.
    private bool _isSelecting;

    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Fire");
        if (interactAction != null)
        {
            interactAction.started += OnInteractPerformed;
            interactAction.canceled += OnInteractCanceled;
            interactAction.Enable();
        }

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

        if (targetShip != null)
        {
            turretControls = targetShip.GetComponentInChildren<TurretControls>();
        }

        SetupSlots();
    }

    void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteractPerformed;
            interactAction.canceled -= OnInteractCanceled;
        }

        if (pauseAction != null)
        {
            pauseAction.started -= OnPausePerformed;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (_slotActions[i] != null && _slotKeyHandlers[i] != null)
            {
                _slotActions[i].performed -= _slotKeyHandlers[i];
            }

            if (_slotButtons[i] != null && _slotClickHandlers[i] != null)
            {
                _slotButtons[i].onClick.RemoveListener(_slotClickHandlers[i]);
            }
        }
    }

    // Discovers numbered buttons in the overlay and wires both mouse clicks and
    // keyboard digit actions through the same single-selection entry point.
    private void SetupSlots()
    {
        GameObject canvasOverlay = GameObject.Find("CanvasOverlay");
        if (canvasOverlay != null)
        {
            Button[] buttons = canvasOverlay.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label == null) continue;

                if (!int.TryParse(label.text.Trim(), out int digit)) continue;
                if (digit < 0 || digit >= SlotCount) continue;

                _slotButtons[digit] = button;

                // Route mouse clicks through the unified selection handler.
                int captured = digit;
                UnityAction clickHandler = () => OnSlotSelected(captured);
                _slotClickHandlers[digit] = clickHandler;
                button.onClick.AddListener(clickHandler);
            }
        }

        // Subscribe each digit key. A key simulates a click on its matching button,
        // so keyboard and mouse share the exact same activation path.
        for (int i = 0; i < SlotCount; i++)
        {
            int captured = i;
            _slotKeyHandlers[i] = ctx => OnSlotKeyPressed(captured);
            _slotActions[i] = InputSystem.actions.FindAction("SelectSlot" + i);
            if (_slotActions[i] != null)
            {
                _slotActions[i].performed += _slotKeyHandlers[i];
                _slotActions[i].Enable();
            }
        }
    }

    // Keyboard entry point: forwards to the matching button's click, if one exists.
    private void OnSlotKeyPressed(int digit)
    {
        Button button = _slotButtons[digit];
        if (button == null) return;

        // Invoking onClick triggers OnSlotSelected (and any game listeners),
        // mirroring a real mouse click exactly.
        button.onClick.Invoke();
    }

    // Single source of truth for activation. Enforces that only one slot is active
    // at a time, regardless of whether the trigger was a mouse click or a key press.
    private void OnSlotSelected(int digit)
    {
        if (_isSelecting) return;
        _isSelecting = true;

        try
        {
            if (_activeSlot == digit)
            {
                // Re-selecting the active slot toggles it off.
                Deactivate(digit);
                _activeSlot = -1;
            }
            else
            {
                if (_activeSlot >= 0)
                {
                    Deactivate(_activeSlot);
                }

                _activeSlot = digit;
                Activate(digit);
            }
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private void Activate(int digit)
    {
        if (digit == BoardSlot)
        {
            isBoardingEnabled = true;
            Debug.Log("Boarding enabled");
        }

        Button button = _slotButtons[digit];
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void Deactivate(int digit)
    {
        if (digit == BoardSlot)
        {
            isBoardingEnabled = false;
            Debug.Log("Boarding disabled");
        }

        Button button = _slotButtons[digit];
        if (button != null && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // While boarding is active, the fire/turret action is suppressed.
        if (isBoardingEnabled)
        {
            Debug.Log("Fire blocked - Boarding mode is active");
            return;
        }

        if (turretControls != null)
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
