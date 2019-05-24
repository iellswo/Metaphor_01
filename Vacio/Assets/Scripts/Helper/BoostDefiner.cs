using UnityEngine;

public class BoostDefiner : MonoBehaviour
{
    private float gizmoRadius = .5f;

    /// <summary>
    /// Described the offset from this objects position where the Player will move towards
    /// before squatting down to boost the Helper.
    /// </summary>
    public Vector3 squatLocation;

    public Vector3 liftLocation;

    private void OnDrawGizmos()
    {
        Vector3 drawSquatLocation = transform.position + squatLocation;
        Vector3 drawLiftLocation = transform.position + liftLocation;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(drawSquatLocation, gizmoRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(drawLiftLocation, gizmoRadius);
    }
}
