using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Boarding mode. While active it suppresses turret fire; releasing Fire over a
/// "Boardable" target spawns a <see cref="BreacherSoldier"/> and sends it to that target.
/// </summary>
public class BoardingButtonAction : ButtonActionBase
{
    private readonly GameObject soldierPrefab;
    private readonly Transform playerShip;
    private readonly Transform defaultTargetShip;

    public BoardingButtonAction(GameObject soldierPrefab, Transform playerShip, Transform defaultTargetShip)
    {
        this.soldierPrefab = soldierPrefab;
        this.playerShip = playerShip;
        this.defaultTargetShip = defaultTargetShip;
    }

    public override bool SuppressesFire => true;

    public override void OnActivated()
    {
        Debug.Log("Boarding enabled");
    }

    public override void OnDeactivated()
    {
        Debug.Log("Boarding disabled");
    }

    public override void OnFireReleased()
    {
        Transform boardableTarget = DetectBoardableTarget();
        if (boardableTarget != null)
        {
            HandleBoardingCommand(boardableTarget);
        }
    }

    private Transform DetectBoardableTarget()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("No main camera found for boardable target detection.");
            return null;
        }

        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Vector2 rayOrigin = new Vector2(ray.origin.x, ray.origin.y);
        Vector2 rayDirection = new Vector2(ray.direction.x, ray.direction.y).normalized;

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Boardable"))
            {
                Debug.Log($"Boardable target detected: {hit.collider.gameObject.name}");
                return hit.collider.transform;
            }

            Debug.Log($"Clicked on {hit.collider.gameObject.name}, but it is not boardable.");
        }
        else
        {
            Debug.Log("No object hit by raycast.");
        }

        return null;
    }

    private void HandleBoardingCommand(Transform boardableTarget)
    {
        if (soldierPrefab == null || playerShip == null)
        {
            Debug.LogWarning("BreacherSoldier Prefab or Player Ship not assigned to BoardingButtonAction");
            return;
        }

        Transform targetForSoldier = boardableTarget != null ? boardableTarget : defaultTargetShip;
        if (targetForSoldier == null)
        {
            Debug.LogWarning("No target ship assigned or detected");
            return;
        }

        GameObject spawnedSoldier = Object.Instantiate(soldierPrefab, playerShip.position, Quaternion.identity);
        BreacherSoldier breacherSoldier = spawnedSoldier.GetComponentInChildren<BreacherSoldier>();
        if (breacherSoldier == null)
        {
            Debug.LogWarning("Spawned soldier has no BreacherSoldier component.");
            return;
        }

        breacherSoldier.SetTargetShip(targetForSoldier);
        breacherSoldier.SetPlayerShip(playerShip);
    }
}
