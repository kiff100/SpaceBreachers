using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DebrisManager : MonoBehaviour
{
    public GameObject debrisSpawnZones; // Parent object containing all spawn zones
    public List<GameObject> debrisPrefabs; // List of debris prefabs to spawn
    public List<PolygonCollider2D> spawnZones; // List of polygon colliders defining spawn zones
    public float minDebrisDistance = 2f; // Minimum distance between debris pieces
    public float minAngularVelocity = -360f; // Minimum rotation speed (degrees per second)
    public float maxAngularVelocity = 360f; // Maximum rotation speed (degrees per second)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debrisSpawnZones = GameObject.Find("DebrisSpawnZones");
        if (debrisSpawnZones == null)
        {
            Debug.LogError("DebrisSpawnZones object not found in the scene!");
            return;
        }
        else
        {
            debrisSpawnZones.GetComponentsInChildren<PolygonCollider2D>(spawnZones);

            if (spawnZones.Count == 0)
            {
                Debug.LogError("No PolygonCollider2D components found in DebrisSpawnZones children!");
            }
        }
        if (debrisPrefabs == null || debrisPrefabs.Count == 0)
        {
            Debug.LogError("No debris prefabs assigned in the inspector!");
        }

        if (spawnZones.Count > 0 && debrisPrefabs.Count > 0)
        {
            SpawnDebris();
        }
    }

    private void SpawnDebris()
    {
        List<Vector3> spawnedPositions = new List<Vector3>();

        foreach (PolygonCollider2D spawnZone in spawnZones)
        {
            // Get the number of debris to spawn from the DebrisSpawnZone component attached to the same GameObject as the PolygonCollider2D
            spawnZone.gameObject.TryGetComponent<DebrisSpawnZone>(out DebrisSpawnZone debrisSpawnZone);
            if (debrisSpawnZone != null)
            {                 
                int numberDebrisToSpawn = debrisSpawnZone.numberDebrisToSpawn;
                for (int i = 0; i < numberDebrisToSpawn; i++)
                {
                    Vector3 randomPoint = GetRandomPointInPolygon(spawnZone, spawnedPositions);
                    
                    if (randomPoint != Vector3.zero)
                    {
                        GameObject debrisPrefab = debrisPrefabs[UnityEngine.Random.Range(0, debrisPrefabs.Count)];
                        float randomRotation = UnityEngine.Random.Range(0f, 360f);
                        Quaternion randomQuaternion = Quaternion.Euler(0, 0, randomRotation);
                        
                        GameObject spawnedDebris = Instantiate(debrisPrefab, randomPoint, randomQuaternion);
                        
                        Rigidbody2D rb = spawnedDebris.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            float randomAngularVelocity = UnityEngine.Random.Range(minAngularVelocity, maxAngularVelocity);
                            rb.angularVelocity = randomAngularVelocity;
                        }
                        
                        spawnedPositions.Add(randomPoint);
                    }
                }
            }
        }
    }

    private Vector3 GetRandomPointInPolygon(PolygonCollider2D spawnZone, List<Vector3> spawnedPositions)
    {
        Bounds bounds = spawnZone.bounds;
        Vector3 randomPoint;
        int maxAttempts = 30; // Prevent infinite loops
        int attempts = 0;

        do
        {
            randomPoint = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                spawnZone.transform.position.z
            );
            attempts++;

            // Check if point is inside polygon and far enough from other debris
            if (spawnZone.OverlapPoint(randomPoint) && IsPointFarEnough(randomPoint, spawnedPositions))
            {
                return randomPoint;
            }
        } while (attempts < maxAttempts);

        return Vector3.zero; // Return zero if no valid point found
    }

    private bool IsPointFarEnough(Vector2 point, List<Vector3> spawnedPositions)
    {
        foreach (Vector2 spawnedPos in spawnedPositions)
        {
            if (Vector2.Distance(point, spawnedPos) < minDebrisDistance)
            {
                return false;
            }
        }
        return true;
    }
}
