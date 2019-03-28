using UnityEngine;

public class BoostDefiner : MonoBehaviour
{
    private float gizmoRadius = .5f;

    /// <summary>
    /// Described the offset from this objects position where the Player will move towards
    /// before squatting down to boost the Helper.
    /// </summary>
    public Vector3 squatLocation;

    private void OnDrawGizmos()
    {
        Vector3 drawSquatLocation = transform.position + squatLocation;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(drawSquatLocation, gizmoRadius);
    }
}
