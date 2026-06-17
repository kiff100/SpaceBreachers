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
    private float fireHoldStartTime;
    private bool wasFirePressed;
    // True while Fire is held and routed to a fire-suppressing action (e.g. the laser).
    private bool isSuppressedFireHeld;

    private InputAction interactAction;
    private InputAction pauseAction;
    private bool isGamePaused = false;

    // Digit-indexed slots (0-9). An entry is null when no button carries that label.
    private readonly Button[] _slotButtons = new Button[SlotCount];
    private readonly IButtonAction[] _buttonActions = new IButtonAction[SlotCount];
    private readonly InputAction[] _slotInputActions = new InputAction[SlotCount];
    private readonly System.Action<InputAction.CallbackContext>[] _slotKeyHandlers =
        new System.Action<InputAction.CallbackContext>[SlotCount];
    private readonly UnityAction[] _slotClickHandlers = new UnityAction[SlotCount];

    // Index of the currently active slot, or -1 when none is active.
    private int _activeSlot = -1;
    // Guards against re-entrancy when programmatically invoking a button's onClick.
    private bool _isSelecting;

    // The behavior of the currently active button, or null when none is selected.
    private IButtonAction ActiveAction => _activeSlot >= 0 ? _buttonActions[_activeSlot] : null;

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

    void Update()
    {
        if (!isSuppressedFireHeld)
        {
            return;
        }

        // Forward the per-frame held event to the active continuous action (e.g. the laser).
        // If the active action stopped suppressing fire (e.g. was deselected), end the hold.
        if (ActiveAction != null && ActiveAction.SuppressesFire)
        {
            ActiveAction.OnFireHeld();
        }
        else
        {
            isSuppressedFireHeld = false;
        }
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
            if (_slotInputActions[i] != null && _slotKeyHandlers[i] != null)
            {
                _slotInputActions[i].performed -= _slotKeyHandlers[i];
            }

            if (_slotButtons[i] != null && _slotClickHandlers[i] != null)
            {
                _slotButtons[i].onClick.RemoveListener(_slotClickHandlers[i]);
            }
        }
    }

    // Discovers numbered buttons in the overlay, creates each button's action, and
    // wires mouse clicks and keyboard digit keys through the same selection entry point.
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
                _buttonActions[digit] = CreateAction(digit);

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
            _slotInputActions[i] = InputSystem.actions.FindAction("SelectSlot" + i);
            if (_slotInputActions[i] != null)
            {
                _slotInputActions[i].performed += _slotKeyHandlers[i];
                _slotInputActions[i].Enable();
            }
        }
    }

    // Maps a button's digit label to its behavior implementation.
    private IButtonAction CreateAction(int digit)
    {
        switch (digit)
        {
            case 1: return new ToolSelectButtonAction();
            case BoardSlot: return new BoardingButtonAction(breacherSoldierPrefab, playerShip, targetShip);
            case 3: return new LaserButtonAction(targetShip);
            case 4: return new DroneButtonAction();
            case 5: return new SpearButtonAction();
            case 6: return new WarpButtonAction();
            default: return null;
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
        _buttonActions[digit]?.OnActivated();

        Button button = _slotButtons[digit];
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void Deactivate(int digit)
    {
        _buttonActions[digit]?.OnDeactivated();

        Button button = _slotButtons[digit];
        if (button != null && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // While an action suppresses fire (e.g. boarding, laser), the turret is disabled and
        // the action handles the press/hold/release itself.
        if (ActiveAction != null && ActiveAction.SuppressesFire)
        {
            isSuppressedFireHeld = true;
            ActiveAction.OnFirePressed();
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
        // When the active action suppresses fire, let it handle the release instead.
        if (ActiveAction != null && ActiveAction.SuppressesFire)
        {
            isSuppressedFireHeld = false;
            ActiveAction.OnFireReleased();
            return;
        }

        if (wasFirePressed && turretControls != null)
        {
            float holdDuration = Time.time - fireHoldStartTime;
            turretControls.OnFireReleased(holdDuration);
            Debug.Log($"Fire button released after {holdDuration:F2} seconds");
            wasFirePressed = false;
        }
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
