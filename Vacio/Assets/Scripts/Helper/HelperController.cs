using System.Collections.Generic;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Half of the helper's hitbox size.")]
    public Vector2 characterHalfSize = new Vector2(0.25f, .5f);
    public float maxSeekToGroundDistance = 0.33333f;
    public Animator spriteAnimator;

    [Tooltip("How quickly the player gains downwards velocity whilte airborne (m/s/s)")]
    public float gravity = 10.0f;
    [Tooltip("Player's max speed when falling (m/s)")]
    public float maxFallSpeed = 10.0f;

    [Tooltip("List here all sprite renderers that you want to flip when the character reverses facing.")]
    public List<SpriteRenderer> BodySprites = new List<SpriteRenderer>();

    [Header("Behavior")]
    public float FollowDistance = 2.0f;
    public float MaxWalkingSpeed = 5.1f;
    public float WalkingAcceleration = 1.0f;
    public float JumpVerticalVelocity = 12f;
    [Tooltip("How quickly the player slows to a stop when they let go of the input (m/s/s)")]
    public float groundRunningFriction = 0.2f;

    [Header("Animations")]
    public string AnimationIdle = "companion_idle";
    public string AnimationWalk = "companion_walk";
    public string AnimationFall = "companion_fall";
    public string AnimationJump = "companion_jump";

    [HideInInspector]
    public PlayerController Player;

    [HideInInspector]
    public HelperState CurrentState;

    [HideInInspector]
    public MovementState CurMovementState;

    // Movement
    private Vector3 currentVelocity;
    private JumpPoint[] jumpPoints;

    private JumpPoint currentMovementTarget;

    // Animation
    private bool hasPlayedAnimationThisFrame = false;

    public enum HelperState
    {
        Idle,
        Following,
        MovingToJump,
        Jumping
    }

    public enum MovementState
    {
        Falling,
        Grounded
    }
    
	// Use this for initialization
	void Start ()
    {
        Object[] oJumpPoints;
        Player = FindObjectOfType<PlayerController>();
        oJumpPoints = FindObjectsOfType<JumpPoint>();
        jumpPoints = System.Array.ConvertAll(oJumpPoints, item => item as JumpPoint);
        CurrentState = HelperState.Idle;
        CurMovementState = MovementState.Falling;
        currentVelocity = Vector3.zero;
	}
	
	// Update is called once per frame
	void Update ()
    {
        // Reset HasPlayedAnimationThisFrame flag.
        hasPlayedAnimationThisFrame = false;
        // Always face towards the player.
        UpdateFacing(Player.transform.position.x < transform.position.x);
        // Check what state we should be in.
        CheckState();
        // Handle the movement.
        HandleMovement();
        // Animation control
        ControlAnimator();
	}

    private void UpdateFacing(bool left = false)
    {
        if (!left)
        {
            foreach (SpriteRenderer s in BodySprites)
            {
                s.flipX = false;
            }
        }
        else
        {
            foreach (SpriteRenderer s in BodySprites)
            {
                s.flipX = true;
            }
        }
    }
    
    // This method is to determine if we should change states, handles the decision tree.
    private void CheckState()
    {
        // To save on the number of calls we make to player, grab and cache the player and helper positions
        Vector3 _playerPos = Player.transform.position;
        Vector3 _helperPos = transform.position;
        JumpPoint target;

        // If the player is to the left of us, then stand idly.
        // TODO: If the player is below a ledge we should move to help them up.
        if (CurrentState != HelperState.Idle && _playerPos.x < _helperPos.x)
        {
            CurrentState = HelperState.Idle;
        }
        // If there is a Jump Point between us and the player, move to the jump point
        else if (CurrentState == HelperState.Following && GetJumpPointBetweenSelfAndPlayer(_helperPos, _playerPos, out target))
        {
            currentMovementTarget = target;
            CurrentState = HelperState.MovingToJump;
        }
        // If we are moving to a Jump Point and will reach it before the next frame, jump.
        else if (CurrentState == HelperState.MovingToJump && _helperPos.x + (currentVelocity.x * Time.deltaTime) >= currentMovementTarget.transform.position.x)
        {
            // apply upward velocity to actually effect the jump.
            currentVelocity.y = JumpVerticalVelocity;
            // Set the current state for logic purposes.
            CurrentState = HelperState.Jumping;
            // Set current movement state to "Falling" so that gravity starts working.
            CurMovementState = MovementState.Falling;
            // Play the animation for a jump
            PlayAnimation(AnimationJump);
        }
        // Check for the end of a jump, if we have landed.
        else if (CurrentState == HelperState.Jumping && CurMovementState == MovementState.Grounded)
        {
            // Idle is the default state for the controller, so spend one frame here to decide next action.
            CurrentState = HelperState.Idle;
        }
        // Begin following the player if we are idle and they are far enough away.
        else if (CurrentState == HelperState.Idle && _playerPos.x - _helperPos.x > FollowDistance)
        {
            CurrentState = HelperState.Following;
        }
        // we have caught up to the player, so stop.
        else if (CurrentState == HelperState.Following &&  _playerPos.x - _helperPos.x < FollowDistance)
        {
            CurrentState = HelperState.Idle;
        }
    }

    private bool GetJumpPointBetweenSelfAndPlayer(Vector3 self, Vector3 player, out JumpPoint targetJumpPoint)
    {
        targetJumpPoint = null;

        foreach (JumpPoint jp in jumpPoints)
        {
            float x = jp.transform.position.x;
            if (self.x < x && x < player.x)
            {
                targetJumpPoint = jp;
                return true;
            }
        }

        return false;
    }

    private void HandleMovement()
    {
        Vector3 currentPosition = transform.position;
        RaycastHit2D hit;
        int playerWorldCollisionMask = ~4 & ~(1 << 8); // include All, exclude IgnoreRaycast
        int playerFloorCollisionMask = (1 << 8) | ~4;
        float maxRise = float.PositiveInfinity;
        float maxFall = -maxFallSpeed;
        bool isOnGround = false;

        // Handle Horizontal Movement

        // If we have caught up to the player start stopping.
        if (CurrentState == HelperState.Idle && currentVelocity.x > 0f)
        {
            // Slow down to an eventual stop.
            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0.0f, groundRunningFriction * Time.deltaTime);
        }
        // If we are jumping we should move at the jump velocity defined by the target. 
        // (This lets us customize each jump to make sure the helper can make it safely.)
        else if (CurrentState == HelperState.Jumping)
        {
            currentVelocity.x = currentMovementTarget.jumpForwardVelocity;
        }
        // If we are supposed to be moving to a point, accelerate
        else if (CurrentState == HelperState.Following || CurrentState == HelperState.MovingToJump)
        {
            // Follow after the player.
            currentVelocity.x += WalkingAcceleration * Time.deltaTime;
            if (currentVelocity.x > MaxWalkingSpeed)
                currentVelocity.x = MaxWalkingSpeed;
        }

        switch (CurMovementState)
        {
            // Handle vertical movement
            case MovementState.Falling:
                currentVelocity.y -= gravity * Time.deltaTime;
                currentVelocity.y = Mathf.Clamp(currentVelocity.y, maxFall, maxRise);
                Vector3 offset = currentVelocity * Time.deltaTime;

                // Move vertically
                if (currentVelocity.y != 0.0f)
                {
                    int mask = currentVelocity.y > 0 ? playerWorldCollisionMask : playerFloorCollisionMask;
                    hit = Physics2D.Raycast(currentPosition, Vector2.up * currentVelocity.y, Mathf.Abs(offset.y) + characterHalfSize.y, mask);

                    if (hit.collider != null && hit.normal.y != 0.0f)
                    {
                        float targetY = hit.point.y + Mathf.Sign(hit.normal.y) * characterHalfSize.y;
                        offset.y = targetY - transform.position.y;
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
                break;

            case MovementState.Grounded:
                // Seek the player to the ground and slopes.
                hit = Physics2D.Raycast(currentPosition, Vector2.down, maxSeekToGroundDistance + characterHalfSize.y, playerFloorCollisionMask);
                isOnGround = hit.collider != null;
                if (isOnGround)
                {
                    currentPosition.y = hit.point.y + characterHalfSize.y;
                    currentVelocity.y = 0.0f; // No y velocity because we're grounded.
                }

                transform.position = currentPosition + (currentVelocity * Time.deltaTime);

                break;
        }

        if (isOnGround)
        {
            CurMovementState = MovementState.Grounded;
        }
        else
        {
            CurMovementState = MovementState.Falling;
        }

    }

    private void ControlAnimator()
    {
        if (CurMovementState == MovementState.Falling && !spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(AnimationJump))
        {
            PlayAnimation(AnimationFall);
        }
        if (CurMovementState == MovementState.Grounded && currentVelocity.x > .15f)
        {
            PlayAnimation(AnimationWalk, .75f);
        }
        else if (CurMovementState == MovementState.Grounded && CurrentState == HelperState.Idle)
        {
            PlayAnimation(AnimationIdle);
        }
    }

    /// <summary>
    /// Plays an animation, only allowing a single animation to be begun each frame.
    /// </summary>
    /// <param name="animationToPlay">The name of the animation that will be played.</param>
    /// <param name="animationSpped">The speed the animation should be played at.</param>
    private void PlayAnimation(string animationToPlay, float animationSpped = 1f)
    {
        if (hasPlayedAnimationThisFrame) return;
        if (spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationToPlay)) return;

        spriteAnimator.speed = animationSpped;
        spriteAnimator.CrossFade(animationToPlay, 0f);
        hasPlayedAnimationThisFrame = true;
    }
}
