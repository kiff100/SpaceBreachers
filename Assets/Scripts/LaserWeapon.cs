using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Continuous laser beam fired from the cargo ship toward the mouse position.
/// While firing it:
///  - raycasts up to <see cref="maxDistance"/> and draws a beam via a <see cref="LineRenderer"/>,
///  - applies damage-per-second to any <see cref="IDamageable"/> it hits (never the owning ship),
///    with damage falling off the further the impact is from the ship,
///  - drains energy from <see cref="ShipStats"/> while active,
///  - sputters and shuts off when energy is depleted.
/// A new fire attempt requires at least <see cref="minEnergyFractionToFire"/> of max energy;
/// otherwise it refuses to fire and flashes the energy meter.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LaserWeapon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Origin of the beam. Falls back to this transform if unassigned.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Energy source drained while firing. Falls back to a ShipStats on this object/parents.")]
    [SerializeField] private ShipStats shipStats;

    [Header("Range & Damage")]
    [Tooltip("Maximum distance the laser can reach, in world units.")]
    [SerializeField] private float maxDistance = 15f;

    [Tooltip("Damage per second applied at point-blank range (distance ~0).")]
    [SerializeField] private float baseDamagePerSecond = 40f;

    [Tooltip("Damage multiplier applied at maximum range. Damage scales linearly from 1 (close) to this value (far).")]
    [Range(0f, 1f)]
    [SerializeField] private float minDamageMultiplierAtMaxRange = 0.25f;

    [Tooltip("Layers the laser can hit. Exclude the cargo ship's layer to avoid self-blocking.")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Energy")]
    [Tooltip("Energy drained per second while the laser is firing.")]
    [SerializeField] private float energyDrainPerSecond = 25f;

    [Tooltip("Fraction of max energy (0..1) required to START firing again after stopping.")]
    [Range(0f, 1f)]
    [SerializeField] private float minEnergyFractionToFire = 0.2f;

    [Header("Sputter")]
    [Tooltip("How long the beam flickers when energy runs out before fully turning off.")]
    [SerializeField] private float sputterDuration = 0.35f;

    private LineRenderer lineRenderer;
    private bool isFiring;
    private bool isSputtering;
    private Camera mainCamera;
    private Coroutine sputterRoutine;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;

        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (shipStats == null)
        {
            shipStats = GetComponentInParent<ShipStats>();
        }
    }

    private void OnEnable()
    {
        if (shipStats != null)
        {
            shipStats.EnergyDepleted += OnEnergyDepleted;
        }
    }

    private void OnDisable()
    {
        if (shipStats != null)
        {
            shipStats.EnergyDepleted -= OnEnergyDepleted;
        }
        StopFiring();
    }

    /// <summary>Attempts to start firing. Returns false (and flashes the meter) if energy is too low.</summary>
    public bool TryBeginFire()
    {
        if (isFiring || isSputtering)
        {
            return isFiring;
        }

        if (shipStats != null && !shipStats.HasEnergyFraction(minEnergyFractionToFire))
        {
            // Not enough power to start a new burst: refuse and flash the energy meter.
            shipStats.SignalInsufficientEnergy();
            return false;
        }

        isFiring = true;
        lineRenderer.enabled = true;
        return true;
    }

    /// <summary>Sustains the beam for one frame. Call every frame while the fire input is held.</summary>
    public void TickFire()
    {
        if (!isFiring)
        {
            return;
        }

        FireBeam(Time.deltaTime, applyDamage: true, drainEnergy: true);
    }

    /// <summary>Stops firing and hides the beam (unless a sputter is currently playing).</summary>
    public void StopFiring()
    {
        if (!isFiring)
        {
            return;
        }

        isFiring = false;
        if (!isSputtering)
        {
            lineRenderer.enabled = false;
        }
    }

    private void FireBeam(float deltaTime, bool applyDamage, bool drainEnergy)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector3 origin = firePoint.position;
        Vector2 direction = GetAimDirection(origin);

        // RaycastAll returns hits sorted by distance; pick the closest collider that is not
        // part of the cargo ship so the beam is never blocked by the ship's own colliders.
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance, hitMask);
        RaycastHit2D chosen = default;
        foreach (var h in hits)
        {
            if (h.collider == null || IsOwnShip(h.collider))
            {
                continue;
            }
            chosen = h;
            break;
        }

        Vector3 endPoint;
        if (chosen.collider != null)
        {
            endPoint = chosen.point;

            if (applyDamage)
            {
                IDamageable damageable = chosen.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    float distance = Vector2.Distance(origin, chosen.point);
                    float falloff = Mathf.Lerp(1f, minDamageMultiplierAtMaxRange,
                        maxDistance > 0f ? Mathf.Clamp01(distance / maxDistance) : 0f);
                    damageable.TakeDamage(baseDamagePerSecond * falloff * deltaTime, chosen.point);
                }
            }
        }
        else
        {
            endPoint = origin + (Vector3)(direction * maxDistance);
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        if (drainEnergy && shipStats != null)
        {
            shipStats.DrainEnergy(energyDrainPerSecond * deltaTime);
        }
    }

    private Vector2 GetAimDirection(Vector3 origin)
    {
        if (mainCamera == null || Mouse.current == null)
        {
            return transform.right;
        }

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = -mainCamera.transform.position.z;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 dir = ((Vector2)(mouseWorldPos - origin));
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : (Vector2)transform.right;
    }

    private bool IsOwnShip(Collider2D other)
    {
        // The laser must never damage or be blocked by its own ship: any collider that
        // shares the laser's root transform belongs to the cargo ship.
        return other.transform.root == transform.root;
    }

    private void OnEnergyDepleted()
    {
        if (isFiring && !isSputtering)
        {
            if (sputterRoutine != null)
            {
                StopCoroutine(sputterRoutine);
            }
            sputterRoutine = StartCoroutine(SputterAndStop());
        }
    }

    private IEnumerator SputterAndStop()
    {
        isSputtering = true;
        isFiring = false;

        float elapsed = 0f;
        while (elapsed < sputterDuration)
        {
            elapsed += Time.deltaTime;
            // Flicker the beam on/off rapidly to read as a power sputter.
            lineRenderer.enabled = Random.value > 0.5f;
            yield return null;
        }

        lineRenderer.enabled = false;
        isSputtering = false;
        sputterRoutine = null;
    }
}
