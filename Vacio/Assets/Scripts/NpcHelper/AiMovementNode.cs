using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMovementNode : MonoBehaviour
{
    [System.Serializable]
    public struct SConnectionData
    {
        public AiMovementNode otherNode;
        public AiMovementNodeManager.EConnectionType connectionType;
    }
    [Tooltip("Adjacent nodes. Double-linking is not necessary, as it will be set up at runtime.")]
    public SConnectionData[] neighborsData;

    // Use this for initialization
    void Start()
    {
        AiMovementNodeManager manager;
        if (AiMovementNodeManager.TryGetInstance(out manager))
        {
            foreach (SConnectionData data in neighborsData)
            {
                manager.AddConnection(this, data.otherNode, data.connectionType);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        float kStartConnectionBoxSize = 0.25f;
        float kConnectionBoxAntiOverlapOffset = 0.25f;
        if (!Application.isPlaying && neighborsData != null)
        {
            float currentConnectionBoxSize = kStartConnectionBoxSize;
            foreach (SConnectionData data in neighborsData)
            {
                if (data.otherNode != null)
                {
                    Color gizmoColor;
                    switch (data.connectionType)
                    {
                        case AiMovementNodeManager.EConnectionType.Walking:
                            gizmoColor = Color.green;
                            break;
                        case AiMovementNodeManager.EConnectionType.ShortHop:
                            gizmoColor = Color.yellow;
                            break;
                        case AiMovementNodeManager.EConnectionType.LiftPlayerFirst:
                            gizmoColor = Color.cyan;
                            break;
                        case AiMovementNodeManager.EConnectionType.LiftSelfFirst:
                            gizmoColor = Color.red;
                            break;
                        default:
                            gizmoColor = Color.white;
                            break;
                    }
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireCube(this.transform.position, Vector3.one * currentConnectionBoxSize);
                    Gizmos.DrawLine(this.transform.position, data.otherNode.transform.position);
                    Gizmos.DrawWireSphere(data.otherNode.transform.position, kStartConnectionBoxSize / 2.0f);
                    currentConnectionBoxSize += kConnectionBoxAntiOverlapOffset; // Don't let the gizmos overlap, make each connection draw its box a little bigger.
                }
            }
        }
    }
#endif

}
