﻿using UnityEngine;
using System.Collections;
using System;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Half of the player's hitbox size.")]
    public Vector2 characterHalfSize = new Vector2(0.25f, .5f);
    [Tooltip("How quickly the player accelerates from standing to running on the ground (m/s/s).")]
    public float groundForwardAcceleration = 1.0f;
    public float airForwardAcceleration = 0.0f;
    [Tooltip("How quickly the player skids to a stop when they press in the opposite direction (m/s/s)")]
    public float groundReverseAcceleration = 2.0f;
    public float airReverseAcceleration = 1.0f;
    [Tooltip("How quickly the player slows to a stop when they let go of the input (m/s/s)")]
    public float groundRunningFriction = 0.2f;
    public float airRunningFriction = 0.0f;
    [Tooltip("The player's max speed while running on the ground (m/s)")]
    public float groundMaxSpeed = 5.0f;
    public float maxSeekToGroundDistance = 0.33333f;
    [Tooltip("How quickly the player rises when they begin to jump (m/s)")]
    public float jumpRisingVelocity = 10.0f;
    [Tooltip("How quickly the player gains downwards velocity whilte airborne (m/s/s)")]
    public float gravity = 10.0f;
    [Tooltip("Player's max speed when falling (m/s)")]
    public float maxFallSpeed = 10.0f;

    private struct SInput
    {
        public bool right, left, jumpDown, jumpHeld;
        public static SInput GetCurrentInput()
        {
            SInput ret = new SInput();
            ret.left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            ret.right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            ret.jumpDown = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
            ret.jumpHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            return ret;
        }
    }

    private enum ECurrentMovementState
    {
        Grounded,
        Airborne,
        Dead,
    }
    private ECurrentMovementState currentMovementState = ECurrentMovementState.Grounded;
    private float timeInCurrentState = 0.0f;

    private Vector2 currentVelocity = Vector2.zero;

    private Vector2 lastRespawnPoint = Vector2.zero;

    void Awake()
    {
        lastRespawnPoint = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            Debug.Log(currentMovementState);
        }

        Camera.main.transform.position = transform.position + Vector3.back * 10.0f;

        timeInCurrentState += Time.deltaTime;

        SInput currentInput = SInput.GetCurrentInput();
        float horizInput = 0.0f;
        if (currentInput.left)
        {
            horizInput -= 1.0f;
        }
        if (currentInput.right)
        {
            horizInput += 1.0f;
        }

        float sameDirectionCheckValue = currentVelocity.x * horizInput;
        float forwardAcceleration, skidAcceleration, frictionAcceleration;
        if (currentMovementState == ECurrentMovementState.Grounded)
        {
            forwardAcceleration = groundForwardAcceleration;
            skidAcceleration = groundReverseAcceleration;
            frictionAcceleration = groundRunningFriction;
        }
        else
        {
            forwardAcceleration = airForwardAcceleration;
            skidAcceleration = airReverseAcceleration;
            frictionAcceleration = airRunningFriction;
        }

        if (horizInput == 0.0f)
        {
            // Decelerate or just stand still.
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0.0f, frictionAcceleration * Time.deltaTime);
        }
        else if (sameDirectionCheckValue >= 0)
        {
            // Accelerate in the direction you're currently moving.
            currentVelocity.x += horizInput * forwardAcceleration * Time.deltaTime;
        }
        else if (sameDirectionCheckValue < 0)
        {
            // Skid quickly to a stop if you press in the opposite direction.
            currentVelocity.x += horizInput * skidAcceleration * Time.deltaTime;
        }

        float maxSpeed = groundMaxSpeed;
        // Cap speed in both directions.
        if (currentVelocity.x > maxSpeed)
            currentVelocity.x = maxSpeed;
        else if (currentVelocity.x < -maxSpeed)
            currentVelocity.x = -maxSpeed;

        // Move the player with collision.
        Vector3 currentOffset = currentVelocity * Time.deltaTime;

        RaycastHit2D hit;
        int playerWorldCollisionMask = -1 + 4; // include All, exclude IgnoreRaycast
        Vector3 currentPosition = transform.position;
        bool canJump = false;
        bool isOnGround = true;
        switch (currentMovementState)
        {
            case ECurrentMovementState.Grounded:
                // Check for the player walking into walls.
                float wallCheckDistance = 0.0f;
                if (currentOffset.x != 0.0f)
                {
                    wallCheckDistance = currentOffset.x + Mathf.Sign(currentOffset.x) * characterHalfSize.x;
                }
                // TODO May be better to decouple the wall push from movement. Move first, then push against walls on all sides (also lets you check for crush).
                if (wallCheckDistance != 0.0f)
                {
                    hit = Physics2D.Raycast(currentPosition, Vector2.right * wallCheckDistance, Mathf.Abs(wallCheckDistance), playerWorldCollisionMask);
                    if (hit.collider != null)
                    {
                        // Push back from the wall so that you're characterHalfSize.x away from it.
                        currentOffset.x = hit.point.x - currentPosition.x;
                        currentOffset.x = currentOffset.x - Mathf.Sign(currentOffset.x) * characterHalfSize.x;
                        currentVelocity.x = 0.0f;
                    }
                }
                currentPosition.x += currentOffset.x;
                // Seek the player to the ground and slopes.
                hit = Physics2D.Raycast(currentPosition, Vector2.down, maxSeekToGroundDistance + characterHalfSize.y, playerWorldCollisionMask);
                isOnGround = hit.collider != null;
                if (isOnGround)
                {
                    currentPosition.y = hit.point.y + characterHalfSize.y;
                }

                transform.position = currentPosition;
                currentVelocity.y = 0.0f; // No y velocity because we're grounded.
                canJump = true;
                break;
            case ECurrentMovementState.Airborne:
                isOnGround = false;
                currentVelocity.y -= gravity * Time.deltaTime;
                currentVelocity.y = Mathf.Max(currentVelocity.y, -maxFallSpeed);
                Vector3 offset = (Vector3)currentVelocity * Time.deltaTime;

                // Move horizontally
                if (currentVelocity.x != 0.0f)
                {
                    hit = Physics2D.Raycast(currentPosition, Vector2.right * currentVelocity.x, Mathf.Abs(offset.x) + characterHalfSize.x, playerWorldCollisionMask);
                    if (hit.collider != null && hit.normal.x != 0.0f)
                    {
                        float targetX = hit.point.x + Mathf.Sign(hit.normal.x) * characterHalfSize.x;
                        offset.x = targetX - transform.position.x;
                    }
                }
                // Move vertically
                if (currentVelocity.y != 0.0f)
                {
                    hit = Physics2D.Raycast(currentPosition, Vector2.up * currentVelocity.y, Mathf.Abs(offset.y) + characterHalfSize.y, playerWorldCollisionMask);
                    if (hit.collider != null && hit.normal.y != 0.0f)
                    {
                        float targetY = hit.point.y + Mathf.Sign(hit.normal.y) * characterHalfSize.y;
                        offset.y = targetY - transform.position.y;
                        // TODO Landing on slopes and sliding on those.
                    }
                    if (hit.normal.y > 0.0f)
                    {
                        // Seek to ground.
                        isOnGround = true;
                    }
                    else if (hit.normal.y < 0.0f && currentVelocity.y > 0.0f)
                    {
                        // Bonk head on ceiling.
                        currentVelocity.y = 0.0f;
                    }
                }

                transform.position = transform.position + offset;
                canJump = false; // TODO Ghost jump.
                break;
            default:
                break;
        }

        // State changes.
        if (currentMovementState != ECurrentMovementState.Dead)
        {
            // TODO Use nonalloc
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, characterHalfSize, 0.0f, 4); // 4 is IgnoreRaycast for triggers.
            foreach (Collider2D col in colliders)
            {
                if (col.GetComponent<KillPlayerZone>())
                {
                    // Check if we've died.
                    currentMovementState = ECurrentMovementState.Dead;
                    canJump = false;
                    break;
                }
                else if (col.GetComponent<PlayerCheckpoint>() && lastRespawnPoint != (Vector2)col.transform.position)
                {
                    // Check if we're resetting the respawn point.
                    lastRespawnPoint = col.transform.position;
                    Debug.Log("Set respawn point to " + lastRespawnPoint + ". TODO: Particle effect for checkpoints?");
                }
            }
        }
        else
        {
            transform.position = lastRespawnPoint;
            currentVelocity = Vector2.zero;
            currentMovementState = ECurrentMovementState.Airborne;
        }
        if (canJump && currentInput.jumpDown)
        {
            // Jump action.
            SetCurrentState(ECurrentMovementState.Airborne);
            currentVelocity.y = jumpRisingVelocity;
        }
        else if (isOnGround && currentMovementState == ECurrentMovementState.Airborne)
        {
            // Land on ground.
            SetCurrentState(ECurrentMovementState.Grounded);
        }
        else if (!isOnGround && currentMovementState == ECurrentMovementState.Grounded)
        {
            // Fall off cliffs.
            SetCurrentState(ECurrentMovementState.Airborne);
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, -1);
    }

    private void SetCurrentState(ECurrentMovementState state)
    {
        currentMovementState = state;
        timeInCurrentState = 0.0f;
    }
}
