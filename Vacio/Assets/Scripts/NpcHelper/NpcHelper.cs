using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcHelper : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float hopSpeed = 1.0f;
    public AnimationCurve hopHeight;
    public float lookAheadForNextSegment = 0.5f;
    public bool debug = true;

    private AiMovementNodeManager.SNodeConnection currentSegment;
    private int currentConnectionUsageFlag = 0; // Ticks up to indicate whether we have interacted with the current connection or not.
    private PlayerController playerTarget = null;

    private float jumpTimeToReachEnd = 0.0f;
    private Vector3 jumpStartPosition = Vector3.zero;
    private Vector3 jumpEndPosition = Vector3.zero;

    #region State Machine

    private enum EHelperState
    {
        Start,
        JustAttachedToNewSegment,
        Walking,
        JumpingToEndOfSegment,
        //ReadyToInteractWithPlayer,
        //BoostingPlayerLeftToRight,
        ////PullingPlayerLeftToRight,
        ////BoostingPlayerRightToLeft,
        ////PullingPlayerRightToLeft,
        //LiftingPlayer,
        //LiftingSelf,
    }
    private EHelperState internal_currentHelperState = EHelperState.Start;
    private float timeInCurrentState = 0.0f;
    private EHelperState CurrentHelperState
    {
        get
        {
            return internal_currentHelperState;
        }
        set
        {
            if (debug)
            {
                Debug.LogFormat(gameObject, "NpcHelper is switching states from {0} to {1}.", internal_currentHelperState, value);
            }
            HelperState_Exit(internal_currentHelperState);
            internal_currentHelperState = value;
            timeInCurrentState = 0.0f;
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
        timeInCurrentState += Time.deltaTime;
        Vector3 targetPosition = playerTarget.transform.position;
        AiMovementNodeManager aiManager;
        AiMovementNodeManager.SNodeConnection nextSegment;
        if (AiMovementNodeManager.TryGetInstance(out aiManager))
        {
            switch (state)
            {
                case EHelperState.Start:
                    currentSegment = aiManager.GetNearestConnection(transform.position);
                    transform.position = currentSegment.GetClosestPointOnLine(transform.position, 0.0f);
                    goto case EHelperState.JustAttachedToNewSegment;
                case EHelperState.JustAttachedToNewSegment:
                    switch (currentSegment.connectionType)
                    {
                        case AiMovementNodeManager.EConnectionType.Walking:
                            transform.position = currentSegment.GetClosestPointOnLine(transform.position, 0.0f);
                            CurrentHelperState = EHelperState.Walking;
                            break;
                        case AiMovementNodeManager.EConnectionType.ShortHop:
                            CurrentHelperState = EHelperState.JumpingToEndOfSegment;
                            jumpStartPosition = currentSegment.GetCloserNodePosition(transform.position);
                            jumpEndPosition = currentSegment.GetFartherNodePosition(transform.position);
                            jumpTimeToReachEnd = (jumpStartPosition - jumpEndPosition).magnitude * hopSpeed;
                            break;
                        default:
                            Debug.LogError("HelperState_Update doesn't know what to do when attaching to segment of type " + currentSegment.connectionType, gameObject);
                            break;
                    }
                    break;
                case EHelperState.Walking:
                    float horizontalPositionToMoveTo = Mathf.MoveTowards(transform.position.x, targetPosition.x, moveSpeed * Time.deltaTime);
                    Vector3 positionToMoveTo = new Vector3(horizontalPositionToMoveTo, transform.position.y, transform.position.z);
                    nextSegment = aiManager.GetNearestConnection(positionToMoveTo);
                    if (nextSegment == currentSegment || nextSegment == null) // We're still on the same segment, so just walk.
                    {
                        positionToMoveTo = currentSegment.GetClosestPointOnLine(positionToMoveTo, 0.0f);
                        transform.position = positionToMoveTo;
                    }
                    else // Transfer to the new segment.
                    {
                        transform.position = nextSegment.GetClosestPointOnLine(transform.position, 0.0f);
                        currentSegment = nextSegment;
                        CurrentHelperState = EHelperState.JustAttachedToNewSegment;
                    }
                    break;
                case EHelperState.JumpingToEndOfSegment:
                    float normalizedCurrentTime = timeInCurrentState / jumpTimeToReachEnd;
                    Vector3 position = Vector3.Lerp(jumpStartPosition, jumpEndPosition, normalizedCurrentTime);
                    // Give it a bit of an arc. Could be an AnimationCurve instead.
                    position += Vector3.up * hopHeight.Evaluate(normalizedCurrentTime);
                    transform.position = position;
                    if (normalizedCurrentTime > 1f && aiManager.GetNearestConnection(targetPosition) != currentSegment)
                    {
                        float forwardSign = Mathf.Sign(jumpEndPosition.x - jumpStartPosition.x);
                        Vector3 positionToLookForNewSegment = position + forwardSign * lookAheadForNextSegment * Vector3.right;
                        nextSegment = aiManager.GetNearestConnection(positionToLookForNewSegment);
                        if (nextSegment != currentSegment && nextSegment != null)
                        {
                            currentSegment = nextSegment;
                            CurrentHelperState = EHelperState.JustAttachedToNewSegment;
                        }
                    }
                    break;
                default:
                    Debug.LogError("No Update tick handling for state " + state, gameObject);
                    break;
            }
        }
        else
        {
            Debug.LogError("No AiMovementNodeManager in scene.", gameObject);
        }
    }

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
        if (debug)
        {
            Debug.DrawLine(currentSegment.first.transform.position, currentSegment.second.transform.position);
        }
    }
}
