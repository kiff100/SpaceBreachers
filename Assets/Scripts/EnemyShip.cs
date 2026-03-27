using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EnemyShip : MonoBehaviour
{
    [SerializeField] private GameObject enemyBreacherPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform playerShip;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private int minSoldierCount = 0;
    [SerializeField] private int maxSoldierCount = 5;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Material enemyMaterial;

    private List<GameObject> spawnedSoldiers = new List<GameObject>();
    private float lastSpawnTime = 0f;
    private bool isPlayerInRange = false;

    void Start()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
            Debug.LogWarning("Spawn point not assigned, using EnemyShip position");
        }

        if (enemyBreacherPrefab == null)
        {
            Debug.LogError("Enemy breacher prefab not assigned to EnemyShip!");
        }

        if (playerShip == null)
        {
            Debug.LogError("Player ship not assigned to EnemyShip!");
        }

        lastSpawnTime = -spawnInterval;
    }

    void Update()
    {
        // Check if player is within detection range
        DetectPlayerPresence();

        // Spawn soldiers if player is in range
        if (isPlayerInRange)
        {
            ManageSoldierSpawning();
        }

        // Clean up destroyed soldiers from list
        CleanupDestroyedSoldiers();
    }

    private void DetectPlayerPresence()
    {
        if (playerShip == null)
        {
            isPlayerInRange = false;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerShip.position);
        isPlayerInRange = distanceToPlayer <= detectionRange;

        if (isPlayerInRange)
        {
            Debug.Log($"Player detected! Distance: {distanceToPlayer:F2}, Detection range: {detectionRange}");
        }
    }

    private void ManageSoldierSpawning()
    {
        // Check if we can spawn a new soldier
        if (spawnedSoldiers.Count < maxSoldierCount && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnSoldier();
            lastSpawnTime = Time.time;
        }

        // Maintain minimum soldier count
        if (spawnedSoldiers.Count < minSoldierCount && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnSoldier();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnSoldier()
    {
        if (enemyBreacherPrefab == null)
        {
            Debug.LogError("Cannot spawn soldier - prefab is null!");
            return;
        }

        GameObject soldier = Instantiate(enemyBreacherPrefab, spawnPoint.position, Quaternion.identity);
        Renderer renderer = soldier.GetComponentInChildren<Renderer>();
        renderer.material = enemyMaterial;
        spawnedSoldiers.Add(soldier);
        soldier.GetComponent<BreacherSoldier>().SetTargetShip(playerShip);
        Debug.Log($"Spawned enemy soldier #{spawnedSoldiers.Count}. Total soldiers: {spawnedSoldiers.Count}/{maxSoldierCount}");
    }

    private void CleanupDestroyedSoldiers()
    {
        spawnedSoldiers.RemoveAll(soldier => soldier == null);
    }

    public int GetSoldierCount()
    {
        return spawnedSoldiers.Count;
    }

    public bool IsPlayerInDetectionRange()
    {
        return isPlayerInRange;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
