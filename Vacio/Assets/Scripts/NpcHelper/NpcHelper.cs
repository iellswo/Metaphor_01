using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcHelper : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float hopSpeed = 1.0f;
    public float climbSpeed = 0.33f;
    public AnimationCurve hopHeight;
    public float lookAheadForNextSegment = 0.5f;
    public float targetDistanceToDetachFromInteractionSegment = 2.0f;
    public bool debug = true;

    private AiMovementNodeManager.SNodeConnection currentSegment;
    private int currentConnectionUsageFlag = 0; // Ticks up to indicate whether we have interacted with the current connection or not.
    private PlayerController playerTarget = null;

    private float segmentTimeToReachEnd = 0.0f;
    private Vector3 segmentStartPosition = Vector3.zero;
    private Vector3 segmentEndPosition = Vector3.zero;

    #region State Machine

    private enum EHelperState
    {
        Start,
        JustAttachedToNewSegment,
        Walking,
        JumpingToEndOfSegment,
        ClimbingToTopOfSegment,
        WaitingToPullPlayerUp,
        WaitingToLiftPlayerUp,
        WaitingForPlayerToLiftUsUp,
        WaitingForPlayerToPullUsUp,
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
        if (AiMovementNodeManager.TryGetInstance(out aiManager))
        {
            AiMovementNodeManager.SNodeConnection nextSegment;
            float normalizedCurrentTime, targetToBottom, targetToTop;
            Vector3 positionToMoveTo, bottomNode, topNode;
            switch (state)
            {
                case EHelperState.Start:
                    currentSegment = aiManager.GetNearestConnection(transform.position);
                    transform.position = currentSegment.GetClosestPointOnLine(transform.position, 0.0f);
                    goto case EHelperState.JustAttachedToNewSegment;
                case EHelperState.JustAttachedToNewSegment:
                    segmentStartPosition = currentSegment.GetCloserNodePosition(transform.position);
                    segmentEndPosition = currentSegment.GetFartherNodePosition(transform.position);
                    switch (currentSegment.connectionType)
                    {
                        case AiMovementNodeManager.EConnectionType.Walking:
                            transform.position = currentSegment.GetClosestPointOnLine(transform.position, 0.0f);
                            CurrentHelperState = EHelperState.Walking;
                            break;
                        case AiMovementNodeManager.EConnectionType.ShortHop:
                            CurrentHelperState = EHelperState.JumpingToEndOfSegment;
                            segmentTimeToReachEnd = (segmentStartPosition - segmentEndPosition).magnitude * hopSpeed;
                            break;
                        case AiMovementNodeManager.EConnectionType.LiftSelfFirst:
                            if (segmentEndPosition.y > segmentStartPosition.y)
                            {
                                // Wait for the player to push us to the top.
                                CurrentHelperState = EHelperState.WaitingForPlayerToLiftUsUp;
                                segmentTimeToReachEnd = (segmentStartPosition - segmentEndPosition).magnitude * climbSpeed;
                            }
                            else
                            {
                                // Just jump down.
                                goto case AiMovementNodeManager.EConnectionType.ShortHop;
                            }
                            break;
                        case AiMovementNodeManager.EConnectionType.LiftPlayerFirst:
                            if (segmentEndPosition.y > segmentStartPosition.y)
                            {
                                // Wait to push the player to the top.
                                CurrentHelperState = EHelperState.WaitingToLiftPlayerUp;
                            }
                            else
                            {
                                // Just jump down.
                                goto case AiMovementNodeManager.EConnectionType.ShortHop;
                            }
                            break;
                        default:
                            Debug.LogError("HelperState_Update doesn't know what to do when attaching to segment of type " + currentSegment.connectionType, gameObject);
                            break;
                    }
                    break;
                case EHelperState.Walking:
                    float horizontalPositionToMoveTo = Mathf.MoveTowards(transform.position.x, targetPosition.x, moveSpeed * Time.deltaTime);
                    positionToMoveTo = new Vector3(horizontalPositionToMoveTo, transform.position.y, transform.position.z);
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
                    normalizedCurrentTime = timeInCurrentState / segmentTimeToReachEnd;
                    positionToMoveTo = Vector3.Lerp(segmentStartPosition, segmentEndPosition, normalizedCurrentTime);
                    // Give it a bit of an arc. Could be an AnimationCurve instead.
                    positionToMoveTo += Vector3.up * hopHeight.Evaluate(normalizedCurrentTime);
                    transform.position = positionToMoveTo;
                    if (normalizedCurrentTime > 1f && aiManager.GetNearestConnection(targetPosition) != currentSegment)
                    {
                        float forwardSign = Mathf.Sign(segmentEndPosition.x - segmentStartPosition.x);
                        Vector3 positionToLookForNewSegment = positionToMoveTo + forwardSign * lookAheadForNextSegment * Vector3.right;
                        nextSegment = aiManager.GetNearestConnection(positionToLookForNewSegment);
                        if (nextSegment != currentSegment && nextSegment != null)
                        {
                            currentSegment = nextSegment;
                            CurrentHelperState = EHelperState.JustAttachedToNewSegment;
                        }
                    }
                    break;
                case EHelperState.ClimbingToTopOfSegment:
                    normalizedCurrentTime = timeInCurrentState / segmentTimeToReachEnd;
                    positionToMoveTo = Vector3.Lerp(segmentStartPosition, segmentEndPosition, normalizedCurrentTime);
                    transform.position = positionToMoveTo;
                    if (normalizedCurrentTime > 1f)
                    {
                        switch (currentSegment.connectionType)
                        {
                            case AiMovementNodeManager.EConnectionType.LiftSelfFirst:
                                CurrentHelperState = EHelperState.WaitingToPullPlayerUp;
                                break;
                            case AiMovementNodeManager.EConnectionType.LiftPlayerFirst:
                                // Going to this state is safe because we will detach from the ledge and follow when they walk away, or we will jump down if they go back.
                                CurrentHelperState = EHelperState.WaitingToPullPlayerUp;
                                break;
                            default:
                                Debug.LogError("Finished climbing but no idea what to do now.", gameObject);
                                break;
                        }
                    }
                    break;
                case EHelperState.WaitingToPullPlayerUp:
                    bottomNode = currentSegment.GetFartherNodePosition(transform.position);
                    topNode = currentSegment.GetCloserNodePosition(transform.position);
                    targetToBottom = (targetPosition - bottomNode).magnitude;
                    targetToTop = (targetPosition - topNode).magnitude;
                    if (targetToBottom < targetToTop && Mathf.Abs(bottomNode.x - targetPosition.x) > targetDistanceToDetachFromInteractionSegment)
                    {
                        // If the player walks away from the bottom, jump back down
                        CurrentHelperState = EHelperState.JumpingToEndOfSegment;
                        segmentStartPosition = topNode;
                        segmentEndPosition = bottomNode;
                        segmentTimeToReachEnd = (segmentStartPosition - segmentEndPosition).magnitude * hopSpeed;
                    }
                    else if (targetToBottom > targetToTop && Mathf.Abs(topNode.x - targetPosition.x) > targetDistanceToDetachFromInteractionSegment)
                    {
                        // If the player somehow gets up to the top without interacting and is walking away, walk to them
                        CurrentHelperState = EHelperState.Walking;
                    }
                    if (playerTarget.IsPressingAiHelperButton())
                    {
                        playerTarget.AiHelperPullsPlayerUpOntoLedge(segmentEndPosition);
                    }
                    break;
                case EHelperState.WaitingToLiftPlayerUp:
                    bottomNode = currentSegment.GetCloserNodePosition(transform.position);
                    topNode = currentSegment.GetFartherNodePosition(transform.position);
                    targetToBottom = (targetPosition - bottomNode).magnitude;
                    targetToTop = (targetPosition - topNode).magnitude;
                    if (targetToBottom < targetToTop && Mathf.Abs(bottomNode.x - targetPosition.x) > targetDistanceToDetachFromInteractionSegment)
                    {
                        // If the player walks away from the bottom, follow them.
                        CurrentHelperState = EHelperState.Walking;
                    }
                    else if (playerTarget.IsPressingAiHelperButton())
                    {
                        if (targetToBottom < targetToTop)
                        {
                            // Lift the player up
                            playerTarget.AiHelperLiftsPlayerUpOntoLedge(segmentEndPosition);
                        }
                        else
                        {
                            // Get pulled up by the player.
                            playerTarget.LiftAiHelperOntoLedge(transform.position);
                            CurrentHelperState = EHelperState.ClimbingToTopOfSegment;
                            segmentStartPosition = bottomNode;
                            segmentEndPosition = topNode;
                            segmentTimeToReachEnd = (segmentStartPosition - segmentEndPosition).magnitude * climbSpeed;
                        }
                    }
                    break;
                case EHelperState.WaitingForPlayerToLiftUsUp:
                    bottomNode = currentSegment.GetCloserNodePosition(transform.position);
                    topNode = currentSegment.GetFartherNodePosition(transform.position);
                    targetToBottom = (targetPosition - bottomNode).magnitude;
                    targetToTop = (targetPosition - topNode).magnitude;
                    if (targetToBottom < targetToTop && targetToBottom > targetDistanceToDetachFromInteractionSegment)
                    {
                        CurrentHelperState = EHelperState.Walking;
                    }
                    else if (playerTarget.IsPressingAiHelperButton())
                    {
                        playerTarget.LiftAiHelperOntoLedge(transform.position);
                        CurrentHelperState = EHelperState.ClimbingToTopOfSegment;
                        segmentStartPosition = bottomNode;
                        segmentEndPosition = topNode;
                        segmentTimeToReachEnd = (segmentStartPosition - segmentEndPosition).magnitude * climbSpeed;
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
