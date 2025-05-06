// 4/24/2025 AI-Tag
// This was created with assistance from Muse, a Unity Artificial Intelligence product

using System;
using UnityEditor;
using UnityEngine;

public static class GridSweepRaycast
{
    /// <summary>
    /// Performs a grid-sweep raycast over the second and third quadrants of the camera's viewport.
    /// </summary>
    /// <param name="camera">The camera to cast rays from.</param>
    /// <param name="hit">Out parameter to store the RaycastHit if an object is hit.</param>
    /// <param name="visualize">Whether to visualize the rays in the Game View.</param>
    /// <returns>Returns true if a ray hits an object with the "player" layer; otherwise, false.</returns>
    public static bool GridSweep(Camera camera, out RaycastHit hit, bool visualize = false)
    {
        hit = default; // Initialize the RaycastHit to its default _value

        // Define the layer mask for the "player" layer
        int playerLayer = LayerMask.GetMask("player");

        // Get the camera's viewport dimensions
        int gridResolution = 10; // Adjust this for finer or coarser grid sweep
        float step = 1.0f / gridResolution;

        // Iterate over the second and third quadrants of the viewport
        for (float y = 0.5f; y <= 1.0f; y += step) // Sweep second quadrant (top-left to bottom-left)
        {
            for (float x = 0; x < 0.5f; x += step)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0));
                if (visualize)
                {
                    Debug.DrawRay(ray.origin, ray.direction * 50f, Color.blue, 0.1f); // Draw debug rays in blue
                }

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerLayer))
                {
                    if (visualize)
                    {
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 1f); // Draw hit point in green
                    }
                    return true; // Return true if a "player" layer object is hit
                }
            }
        }

        for (float y = 0.5f; y >= 0.0f; y -= step) // Sweep third quadrant (bottom-left to bottom-right)
        {
            for (float x = 0; x < 0.5f; x += step)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(x, y, 0));
                if (visualize)
                {
                    Debug.DrawRay(ray.origin, ray.direction * 50f, Color.red, 0.1f); // Draw debug rays in red
                }

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerLayer))
                {
                    if (visualize)
                    {
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 1f); // Draw hit point in green
                    }
                    return true; // Return true if a "player" layer object is hit
                }
            }
        }

        return false; // Return false if no ray hits an object with the "player" layer
    }
}
