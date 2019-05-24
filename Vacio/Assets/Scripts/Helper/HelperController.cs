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

    [Tooltip("Position of the hand, used for climbing animation.")]
    public GameObject handBone;

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
    public string AnimationClmb = "companion_climb";

    [HideInInspector]
    public PlayerController Player;

    [HideInInspector]
    public HelperState CurrentState;

    [HideInInspector]
    public MovementState CurMovementState;

    // Movement
    private Vector3 currentVelocity;
    private JumpPoint[] jumpPoints;

    private JumpPoint currentJumpPoint;

    private Vector3 currentMovementTarget;

    // Climbing
    private float climbingTime = 1f;
    private float timeClimbing = 0f;

    // Animation
    private bool hasPlayedAnimationThisFrame = false;

    public enum HelperState
    {
        Respawn,
        Idle,
        Following,
        MovingToJump,
        Jumping,
        MovingToBoost,
        BeingBoosted, 
        Climbing,
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
        // Check what state we should be in.
        CheckState();
        // Always face towards the player (unless in some kind of cutscene action)
        if (CurrentState != HelperState.BeingBoosted && CurrentState != HelperState.Climbing)
            UpdateFacing(Player.transform.position.x < transform.position.x);
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

        // If the player is dead, we should stop our current state and movement.
        if (Player.currentMovementState == PlayerController.ECurrentMovementState.Dead)
        {
            CurrentState = HelperState.Respawn;
            currentVelocity = Vector3.zero;
        }
        // If the player has been moved to respawn point, then move us to respawn point.
        else if (CurrentState == HelperState.Respawn && Player.currentMovementState == PlayerController.ECurrentMovementState.Laying)
        {
            // The screen has fully blacked out and we are safe to move the npc for the respawn.
            Vector3 newPosition = Player.lastRespawnPoint;
            newPosition.x -= 6.5f;
            transform.position = newPosition;
            CurrentState = HelperState.Idle;
        }
        // Once the respawn cycle has ended, return to idle state.
        //else if (CurrentState == HelperState.Respawn && Player.currentMovementState == PlayerController.ECurrentMovementState.Standing)
        //{
        //    CurrentState = HelperState.Idle;
        //}
        // Transition out of climbing state.
        else if (CurrentState == HelperState.Climbing && timeClimbing >= climbingTime)
        {
            // TODO: This should transition to a laying down state.
            CurrentState = HelperState.Idle;
        }
        // Update while in Climging state: Position so that the hand bone lines up with the ledge.
        else if (CurrentState == HelperState.Climbing)
        {
            transform.position = currentMovementTarget - handBone.transform.localPosition;
            timeClimbing += Time.deltaTime;
        }
        // Transition from BeingBoosted to Climbing: Once at the top of the boost, climb up the ledge.
        else if (CurrentState == HelperState.BeingBoosted && Player.currentMovementState == PlayerController.ECurrentMovementState.LiftFollowThru)
        {
            CurrentState = HelperState.Climbing;
            currentMovementTarget = Player.liftToPoint;
            timeClimbing = 0f;
            PlayAnimation(AnimationClmb);
        }
        // During BeingBoosted state: Place self at position of the handBone of the Player.
        else if (CurrentState == HelperState.BeingBoosted)
        {
            transform.position = Player.handBone.transform.position + new Vector3(0, characterHalfSize.y);
        }
        // Reached Player's hand, begin being boosted
        else if (CurrentState == HelperState.MovingToBoost && _helperPos.x + (currentVelocity.x * Time.deltaTime) >= currentMovementTarget.x)
        {
            CurrentState = HelperState.BeingBoosted;
            currentVelocity = Vector3.zero;
            transform.position = currentMovementTarget;
            Player.TriggerBoostFlag = true;
        }
        // Player is squatting, begin MovingToBoost state. (Only if player is to our right)
        else if (Player.currentMovementState == PlayerController.ECurrentMovementState.Squatting
                    && CurrentState != HelperState.BeingBoosted
                    && _playerPos.x > _helperPos.x)
        {
            currentMovementTarget = Player.handBone.transform.position + new Vector3(0, characterHalfSize.y);
            CurrentState = HelperState.MovingToBoost;
        }

        // If the player is to the left of us, then stand idly.
        // TODO: If the player is below a ledge we should move to lift them up.
        else if (CurrentState != HelperState.Idle && _playerPos.x < _helperPos.x)
        {
            CurrentState = HelperState.Idle;
        }
        // If there is a Jump Point between us and the player, move to the jump point
        else if (CurrentState == HelperState.Following && GetJumpPointBetweenSelfAndPlayer(_helperPos, _playerPos, out target))
        {
            currentJumpPoint = target;
            CurrentState = HelperState.MovingToJump;
        }
        // If we are moving to a Jump Point and will reach it before the next frame, jump.
        else if (CurrentState == HelperState.MovingToJump && _helperPos.x + (currentVelocity.x * Time.deltaTime) >= currentJumpPoint.transform.position.x)
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
        else if (CurrentState == HelperState.Following && _playerPos.x - _helperPos.x < FollowDistance)
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
            currentVelocity.x = currentJumpPoint.jumpForwardVelocity;
        }
        // If we are supposed to be moving to a point, accelerate
        else if (CurrentState == HelperState.Following || CurrentState == HelperState.MovingToJump || CurrentState == HelperState.MovingToBoost)
        {
            // Follow after the player.
            currentVelocity.x += WalkingAcceleration * Time.deltaTime;
            if (currentVelocity.x > MaxWalkingSpeed)
                currentVelocity.x = MaxWalkingSpeed;
        }

        if (CurrentState == HelperState.BeingBoosted || CurrentState == HelperState.Climbing)
        {
            CurMovementState = MovementState.Falling;
            // If we are in the process of being boosted then completely ignore gravity.
            return;
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
        if (CurrentState == HelperState.Climbing)
        {
        }
        else if (CurMovementState == MovementState.Falling && !spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(AnimationJump))
        {
            PlayAnimation(AnimationFall);
        }
        else if (CurMovementState == MovementState.Grounded && currentVelocity.x > .15f)
        {
            PlayAnimation(AnimationWalk, .75f);
        }
        else if (CurMovementState == MovementState.Grounded)
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
