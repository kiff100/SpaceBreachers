using UnityEngine;
using UnityEngine.InputSystem;

public class WASD_Camera_Pan : MonoBehaviour
{
    [Tooltip("Movement speed for keyboard panning")]
    public float panSpeed = 20f;

    void Update()
    {
        // Get input from WASD keys using the new Input System's Keyboard API
        Vector3 moveDirection = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            moveDirection += Vector3.up; // Move forward (along Z axis)
        if (Keyboard.current.sKey.isPressed)
            moveDirection += Vector3.down; // Move backward
        if (Keyboard.current.aKey.isPressed)
            moveDirection += Vector3.left;   // Move left (along X axis)
        if (Keyboard.current.dKey.isPressed)
            moveDirection += Vector3.right;   // Move right

        // Optional: ensure consistent speed when moving diagonally
        if (moveDirection.magnitude > 1)
            moveDirection.Normalize();

        // Use the camera's rotation to ensure movement is relative to the current view
        // TransformDirection converts local direction to world space direction
        Vector3 worldMove = Camera.main.transform.TransformDirection(moveDirection);
        // Keep movement on a flat plane (disable vertical movement if not wanted)
        worldMove.z = 0;

        // Apply movement scaled by speed and time
        transform.position += worldMove * panSpeed * Time.deltaTime;
    }
}