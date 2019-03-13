using UnityEngine;

public class PlayerCheckpoint : MonoBehaviour
{
    public bool spawnNPC = false;
    public float npcSpawnOffset = -3f;

    private float gizmoYoffset = .9f;
    private float gizmoRadius = .5f;

    private void OnDrawGizmos()
    {
        Vector3 position = this.transform.position - new Vector3(0, gizmoYoffset);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, gizmoRadius);

        if (spawnNPC)
        {
            position = this.transform.position - new Vector3(-npcSpawnOffset, gizmoYoffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(position, new Vector3(gizmoRadius * 1.5f, gizmoRadius * 1.5f));
        }
    }
}
