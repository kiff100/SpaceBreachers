using System;
using UnityEngine;

public class TetherLine : MonoBehaviour
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    private LineRenderer lineRenderer;

    public void UpdatePosition(Vector3 position1, Vector3 position2)
    {
        // set the positions of the line renderer to match the start and end points
        startPoint = position1;
        endPoint = position2;
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("TetherLine requires a LineRenderer component!");
            return;
        }
        lineRenderer.positionCount = 2; // We only need two points for the tether
    }
    void Update()
    {
        Debug.Log($"TetherLine Update: Start {startPoint}, End {endPoint}");
        if (startPoint != null && endPoint != null)
        {
            Debug.Log($"Updating tether line: Start {startPoint}, End {endPoint}");
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
        }
    }
}