using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcHelper : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float height = .5f;

    public bool debug = true;

    private PlayerController playerTarget = null;


    #region State Machine

    private enum EHelperState
    {
        Walking,
    }
    private EHelperState CurrentHelperState
    {
        get
        {
            return internal_currentHelperState;
        }
        set
        {
            HelperState_Exit(internal_currentHelperState);
            internal_currentHelperState = value;
            HelperState_Enter(internal_currentHelperState);
        }
    }

    private void HelperState_Enter(EHelperState state)
    {
        switch (state)
        {
            case EHelperState.Walking:
                break;
            default:
                break;
        }
    }

    private void HelperState_Exit(EHelperState state)
    {
        switch (state)
        {
            case EHelperState.Walking:
                break;
            default:
                break;
        }
    }

    private void HelperState_Update(EHelperState state)
    {
        switch (state)
        {
            case EHelperState.Walking:
                AiMovementNodeManager manager;
                if (AiMovementNodeManager.TryGetInstance(out manager))
                {
                    float xOffset = Mathf.Clamp(playerTarget.transform.position.x - this.transform.position.x, -1.0f, 1.0f);
                    xOffset *= moveSpeed * Time.deltaTime;
                    Vector3 targetPosition = transform.position + Vector3.right * xOffset - Vector3.up * height;
                    AiMovementNodeManager.SNodeConnection connection = manager.GetNearestConnection(targetPosition);
                    targetPosition = connection.GetClosestPointOnLine(targetPosition, clampEndsAmount: 0.25f);
                    float targetX = targetPosition.x;
                    if (targetX < connection.GetLeftmost().x)
                    {
                        targetPosition.y = connection.GetLeftmost().y;
                    }
                    else if (targetX > connection.GetRightmost().x)
                    {
                        targetPosition.y = connection.GetRightmost().y;
                    }
                    transform.position = targetPosition + Vector3.up * height;
                    if (debug)
                    {
                        Debug.DrawLine(connection.GetLeftmost(), connection.GetRightmost());
                    }
                }
                break;
            default:
                break;
        }
    }

    private EHelperState internal_currentHelperState = EHelperState.Walking;

    #endregion

    private void Start()
    {
        playerTarget = GameObject.FindObjectOfType<PlayerController>();
        if (playerTarget == null)
        {
            Debug.LogError("Could not find playerTarget", gameObject);
        }
    }

    void Update()
    {
        HelperState_Update(CurrentHelperState);
    }
}
