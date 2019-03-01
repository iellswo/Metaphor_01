using UnityEngine;

public class JumpPoint : MonoBehaviour
{
    public float gizmoYoffset = 0;
    public float gizmoRadius = .2f;

    private void OnDrawGizmos()
    {
        Vector3 position = this.transform.position - new Vector3(0, gizmoYoffset);
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(position, gizmoRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(position, gizmoRadius);
    }
}
