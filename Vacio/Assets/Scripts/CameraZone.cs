using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Tooltip("This camera zone's priority. Higher is more important.")]
    public int priority = 0;

    public float top = Mathf.Infinity;
    public float bottom = Mathf.NegativeInfinity;
    public float left = Mathf.NegativeInfinity;
    public float right = Mathf.Infinity;

    public float screenHeight = 10.0f;
    public float screenHeightAdjustSpeed = 1.0f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector2(left, top), new Vector2(left, bottom));
        Gizmos.DrawLine(new Vector2(left, bottom), new Vector2(right, bottom));
        Gizmos.DrawLine(new Vector2(right, bottom), new Vector2(right, top));
        Gizmos.DrawLine(new Vector2(right, top), new Vector2(left, top));
    }
}
