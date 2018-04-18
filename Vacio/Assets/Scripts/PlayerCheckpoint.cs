using UnityEngine;

public class PlayerCheckpoint : MonoBehaviour
{
    public float gizmoYoffset = 0;
    public float gizmoRadius = .5f;

    private void OnDrawGizmos()
    {
        Vector3 position = this.transform.position - new Vector3(0, gizmoYoffset);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, gizmoRadius);
    }
}
