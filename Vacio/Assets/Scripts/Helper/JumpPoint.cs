using UnityEngine;

public class JumpPoint : MonoBehaviour
{
    public float jumpForwardVelocity = 3.5f;
    
    private float gizmoYoffset = 0;
    private float gizmoRadius = .4f;

    private void OnDrawGizmos()
    {
        Vector3 position = this.transform.position - new Vector3(0, gizmoYoffset);
        //Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(position, gizmoRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(position, new Vector3(gizmoRadius * 1.5f, gizmoRadius * 1.5f));
    }
}
