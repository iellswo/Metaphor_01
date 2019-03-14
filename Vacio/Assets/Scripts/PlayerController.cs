using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerController : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Half of the player's hitbox size.")]
    public Vector2 characterHalfSize = new Vector2(0.25f, .5f);
    public float maxSeekToGroundDistance = 0.33333f;
    public Animator spriteAnimator;
    public string animationStateIdle = "anim_idle";
    public string animationStateWalking = "anim_walk";
    public string animationStateJumping = "anim_jump";
    public string animationStateFalling = "anim_fall";
    public string animationStateHardLanding = "hard_fall";
    public string animationStateLaying = "anim_laying";
    public string animationStateStandUp = "anim_stand";
    public string animationStateFloating = "anim_float_idle";
    public string animationStateFloatyWalk = "anim_new_float_walk";
    public string animationStateFloatyJump = "anim_float_jump";
    public string animationStateFloatyFalling = "anim_float_fall";
    public string animationStateAirWalkAnimation = "anim_slide_walk";
    public string animationStateFlightIdle = "anim_fly_idle";
    public string animationStateFlightMove = "anim_fly_move";
    public string animationStatePowerupPickup = "anim_grab_pwup";

    [Tooltip("The position of the hand, used to move the powerup sprite during grab powerup.")]
    public GameObject handBone;

    [Tooltip("List here all sprite renderers that you want to flip when the player reverses direction.")]
    public List<SpriteRenderer> BodySprites = new List<SpriteRenderer>();
    public float maxPowerUpBarChangeRate = 1.0f;
    [Tooltip("Maximum value of a powerup, used for fill.")]
    public float maxPowerUpValue = 15f;
    public float powerupGrabAnimationLength = 1f;
    public float deathFadeLength = 1f;
    public float standTimeLength = .5f;
    public float hardLandTimeLength = 1.2f;

    [Header("Ground Movement Data")]
    [Tooltip("How quickly the player accelerates from standing to running on the ground (m/s/s).")]
    public float groundForwardAcceleration = 1.0f;
    [Tooltip("How quickly the player skids to a stop when they press in the opposite direction (m/s/s)")]
    public float groundReverseAcceleration = 2.0f;
    [Tooltip("How quickly the player slows to a stop when they let go of the input (m/s/s)")]
    public float groundRunningFriction = 0.2f;
    [Tooltip("The player's max speed while running on the ground (m/s)")]
    public float groundMaxSpeed = 5.0f;
    [Tooltip("How fast the player animates walking.")]
    public float walkingAnimationVelocity = 2.0f;

    [Header("Jumping Movement Data")]
    public float airForwardAcceleration = 0.0f;
    public float airReverseAcceleration = 1.0f;
    public float airRunningFriction = 0.0f;
    public float airMaxSpeed = float.PositiveInfinity;
    [Tooltip("How quickly the player rises when they begin to jump (m/s)")]
    public float jumpRisingVelocity = 10.0f;
    
    [Tooltip("How much speed is added to the player's forward velocity when they jump. (m/s)")]
    public float forwardJumpVelocityBoost = 1.0f;
    [Tooltip("How quickly the player gains downwards velocity whilte airborne (m/s/s)")]
    public float gravity = 10.0f;
    [Tooltip("Player's max speed when falling (m/s)")]
    public float maxFallSpeed = 10.0f;

    [Tooltip("The speed at which we play the fall animation.")]
    public float fallAnimSpeed = .75f;

    [Header("Air Walk Powerup")]
    public Color airWalkPowerUpBarColor = Color.red;
    [Tooltip("How far the player can move horizontally with the air walk powerup.")]
    public float maxAirWalkDistance = 15.0f;
    [Tooltip("Effects how quickly walking on air reduces the fill amount.")]
    public float airWalkReduction = 1f;
    [Tooltip("How much air walk power the player loses when they jump while air walking.")]
    public float airWalkJumpLoss = 2.0f;

    [Header("Low Gravity Powerup")]
    public Color lowGravityPowerUpBarColor = Color.blue;
    public float maxLowGravityTime = 15.0f;
    public float lowGravityTimeLossRate = 1.0f;
    public float lowGravityDistanceLossRate = 1.0f;
    public float lowGravityJumpRisingVelocity = 10.0f;
    public float lowGravityGravity = 2.5f;
    public float lowGravityAirControl = 1.0f;
    public float lowGravityGroundForwardAcceleration = 0.5f;
    public float lowGravityGroundReverseAcceleration = 1.0f;
    public float lowGravityGroundRunningFriction = 0.1f;
    [Tooltip("How fast the player animates 'floaty' walking.")]
    public float floatyWalkingAnimationVelocity = .5f;

    [Header("Flight Powerup")]
    public Color flyingPowerUpBarColor = Color.white;
    public float flyingMaxDuration = 6.0f;
    public float flyingRisingAcceleration = 10.0f;
    public float flyingDescendingAcceleration = 10.0f;
    public float flyingAirControl = 1.0f;
    public float flyingMaxHorizontalSpeed = 100.0f;
    public float flyingMaxRiseSpeed = 100.0f;
    public float flyingMaxFallSpeed = 10.0f;
    public float flyingAirFriction = 1.0f;

    [Header("AI Helper")]
    public float climbUpToHelperSpeed = 1.0f;
    public float pushHelperUpTime = 1.0f;
    public float DistanceToTrailBehindPlayer = 3f;
    public float TrailBehindPlayerFloatSpeed = 3f;

    [Header("GameObject Connections")]
    [Tooltip("The power-up bar that appears when the player has a powerup.")]
    public Transform powerUpBar;
    public SpriteRenderer powerUpBarGraphic;
    [Tooltip("The particle system on the player that makes SFX for air walking.")]
    public ParticleSystem airWalkEmitter;

    private Vector3 CurrentAIPosition = new Vector3();

    private struct SInput
    {
        public bool down, up, jumpDown, jumpHeld, resetDown, interactDown;
        public float left, right;
        public static SInput GetCurrentInput()
        {
            SInput ret = new SInput();
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            ret.left = horizontal;
            ret.right = horizontal;
            ret.down = vertical < 0.0f;
            ret.up = vertical > 0.0f;
            ret.jumpDown = Input.GetButtonDown("Jump");
            ret.jumpHeld = Input.GetButton("Jump");
            ret.interactDown = Input.GetButtonDown("Interact");
            // TODO: remove reset eventually.
            ret.resetDown = Input.GetKeyDown(KeyCode.R);
            return ret;
        }
    }

    public enum ECurrentMovementState
    {
        Grounded,
        Airborne,
        Dead,
        Laying,
        Standing,
        Interacting,
        LongFall,
        HardLand,
        HelperIsPullingPlayerUpToLedge,
        HelperIsLiftingPlayerUpToLedge,
        PlayerIsLiftingHelperToLedge,
    }

    [HideInInspector]
    public ECurrentMovementState currentMovementState = ECurrentMovementState.Grounded;
    private float timeInCurrentState = 0.0f;

    private float currentPowerupMeterMaxValue = 0.0f;
    private float currentAirWalkPowerUpMeter = 0.0f;
    private bool wasUsingAirWalkLastFrame = false;
    private bool isUsingFlightPowerup = false;
    private float currentLowGravityPowerUpMeter = 0.0f;
    private float currentFlyingPowerUpMeter = 0.0f;

    [HideInInspector]
    public Vector2 currentVelocity = Vector2.zero;
    private Vector3 climbToLedgeTarget = Vector3.zero;

    [HideInInspector]
    public Vector2 lastRespawnPoint = Vector2.zero;

    private float fillMax = .696f;
    private float fillMin = .507f;
    private float currentInteractionDuration = 0;
    private GameObject currentInteractionProxy = null;

    private bool hasPlayedAnimationThisFrame = false;
    private bool respawnInProgress = false;
    
    //private List<CameraZone> currentCameraZones = new List<CameraZone>();

    void Start()
    {
        lastRespawnPoint = transform.position;
        //Steamworks.SteamAPI.Init(); // TODO SHould move this to a GameManager.
        //Steamworks.SteamController.Init("");
        CurrentAIPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        hasPlayedAnimationThisFrame = false;

        timeInCurrentState += Time.deltaTime;

        SInput currentInput = SInput.GetCurrentInput();

        float horizInput = currentInput.left + currentInput.right;
        bool isUsingAirWalk = false;
        bool canJump;
        bool isOnGround;

        // Handle the logic of the long fall cutscene
        if (currentMovementState == ECurrentMovementState.LongFall)
        {
            HandleMovement(currentInput, out canJump, out isOnGround);
            if (isOnGround)
            {
                Debug.Log("We've hit the ground.");
                SetCurrentState(ECurrentMovementState.HardLand);
            }
        }
        // Allow the hard landing animation to play and then wait for input.
        else if(currentMovementState == ECurrentMovementState.HardLand)
        {
            if (timeInCurrentState < hardLandTimeLength)
            {
                return;
            }

            // If the animation has finished we get up if the player presses either up, jump, or interact.
            if (currentInput.up || currentInput.jumpDown || currentInput.interactDown)
            {
                SetCurrentState(ECurrentMovementState.Standing);
            }
        }
        // If we are in a standing state check to see if we've finished standing up
        else if (currentMovementState == ECurrentMovementState.Standing)
        {
            if (timeInCurrentState >= standTimeLength)
            {
                SetCurrentState(ECurrentMovementState.Grounded);
            }
        }
        // read input and handle the movement of the character.
        else if (currentMovementState != ECurrentMovementState.Interacting)
        {
            HandleMovement(currentInput, out canJump, out isOnGround);

            // State changes.
            if (currentMovementState != ECurrentMovementState.Dead && currentMovementState != ECurrentMovementState.Laying)
            {
                // TODO Use nonalloc
                //currentCameraZones.Clear();
                Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, characterHalfSize, 0.0f, 4); // 4 is IgnoreRaycast for triggers.
                foreach (Collider2D col in colliders)
                {
                    if (col.GetComponent<KillPlayerZone>())
                    {
                        // Report that we have died.
                        SetCurrentState(ECurrentMovementState.Dead);
                        canJump = false;
                        break;
                    }
                    //else if (col.GetComponent<CameraZone>())
                    //{
                    //    currentCameraZones.Add(col.GetComponent<CameraZone>());
                    //}
                    else if (col.GetComponent<PlayerCheckpoint>() && lastRespawnPoint != (Vector2)col.transform.position)
                    {
                        // Check if we're resetting the respawn point.
                        lastRespawnPoint = col.transform.position;
                        Debug.Log("Set respawn point to " + lastRespawnPoint + ". TODO: Particle effect for checkpoints?");
                    }
                    else if (col.GetComponent<PowerUp>())
                    {
                        if (CheckGetPowerUp(col.GetComponent<PowerUp>(), currentInput)) return;
                    }
                    else if (col.GetComponent<SceneTransitionManager>())
                    {
                        col.GetComponent<SceneTransitionManager>().TriggerLoadScene();
                    }
                    else if (col.GetComponent<LongFallTriggerZone>())
                    {
                        SetCurrentState(ECurrentMovementState.LongFall);
                        currentVelocity.x = 0;
                    }
                    else if (col.GetComponent<NPCIntroController>())
                    {
                        col.GetComponent<NPCIntroController>().ZoneEntered();
                    }
                }
            }
            // This bracket is reached if the player is dead or reset is pressed.  Respawn all objects and player.
            else
            {
                if (!respawnInProgress && timeInCurrentState >= deathFadeLength)
                {
                    currentLowGravityPowerUpMeter = 0f;
                    currentAirWalkPowerUpMeter = 0f;
                    currentFlyingPowerUpMeter = 0f;
                    transform.position = lastRespawnPoint;
                    currentVelocity = Vector2.zero;

                    SpawnTracker.TriggerReset();

                    Initiate.FadeIn(Color.black, 1f);
                    respawnInProgress = true;

                    SetCurrentState(ECurrentMovementState.Laying);
                }
                if (respawnInProgress && timeInCurrentState >= 1.25 * deathFadeLength)
                {
                    //PlayAnimation(animationStateJumping);

                    SetCurrentState(ECurrentMovementState.Standing);
                    respawnInProgress = false;
                }

                PlayAnimation(animationStateLaying);
            }

            if (canJump && currentInput.jumpDown)
            {
                float jumpVelocity = currentLowGravityPowerUpMeter <= 0.0f ? jumpRisingVelocity : lowGravityJumpRisingVelocity;
                // Jump action.
                SetCurrentState(ECurrentMovementState.Airborne);
                currentVelocity.y = jumpVelocity;
                currentVelocity.x += horizInput * forwardJumpVelocityBoost;
                if (wasUsingAirWalkLastFrame)
                {
                    currentAirWalkPowerUpMeter -= airWalkJumpLoss;
                }
            }
            else if (isOnGround && currentMovementState == ECurrentMovementState.Airborne)
            {
                // Land on ground.
                SetCurrentState(ECurrentMovementState.Grounded);
            }
            else if (currentMovementState == ECurrentMovementState.Airborne && currentVelocity.y <= 0.0f && currentAirWalkPowerUpMeter > 0.0f)
            {
                // Air walk after jump.
                SetCurrentState(ECurrentMovementState.Grounded);
                isUsingAirWalk = true;
            }
            else if (!isOnGround && currentMovementState == ECurrentMovementState.Grounded)
            {
                // Fall off cliffs.
                if (currentAirWalkPowerUpMeter <= 0.0f)
                {
                    SetCurrentState(ECurrentMovementState.Airborne);
                }
                else
                {
                    isUsingAirWalk = true;
                }
            }

            wasUsingAirWalkLastFrame = isUsingAirWalk;
            if (isUsingAirWalk)
            {
                currentAirWalkPowerUpMeter -= Time.deltaTime * Mathf.Abs(currentVelocity.x / groundMaxSpeed) * airWalkReduction;
                airWalkEmitter.Emit(1);
            }
            if (isUsingFlightPowerup)
            {
                currentFlyingPowerUpMeter -= Time.deltaTime;
                if (currentFlyingPowerUpMeter <= 0.0f)
                {
                    isUsingFlightPowerup = false;
                }
            }

            // Powerup bar
            ControlPowerupFill();

            // Animator
            ControlAnimator(isOnGround, isUsingAirWalk, isUsingFlightPowerup);

            transform.position = new Vector3(transform.position.x, transform.position.y, -1);
        }
        else
        {
            if (currentInteractionProxy != null)
            {
                currentInteractionProxy.transform.position =
                    handBone != null
                    ? handBone.transform.position
                    : Vector3.Lerp(currentInteractionProxy.transform.position, transform.position, 4 * Time.deltaTime);
            }

            if (timeInCurrentState > currentInteractionDuration)
            {
                if (currentInteractionProxy != null) Destroy(currentInteractionProxy);
                currentInteractionProxy = null;
                SetCurrentState(ECurrentMovementState.Grounded);
            }
        }

        float desiredX = Mathf.MoveTowards(CurrentAIPosition.x, 
            transform.position.x - Mathf.Sign(currentVelocity.x) * DistanceToTrailBehindPlayer, 
            Mathf.Abs(currentVelocity.x) * Time.deltaTime * TrailBehindPlayerFloatSpeed);

        float delta = desiredX - CurrentAIPosition.x;

        if (Mathf.Sign(delta) != Mathf.Sign(currentVelocity.x))
        {
            desiredX = CurrentAIPosition.x;
        }

        desiredX = Mathf.Clamp(desiredX, transform.position.x - DistanceToTrailBehindPlayer, transform.position.x + DistanceToTrailBehindPlayer);

        CurrentAIPosition = new Vector3(desiredX, transform.position.y, transform.position.z);
    }

    private void HandleMovement(SInput currentInput, out bool canJump, out bool isOnGround)
    {
        if (currentInput.resetDown)
        {
            SetCurrentState(ECurrentMovementState.Dead);
        }

        float horizInput = currentInput.left + currentInput.right;

        float sameDirectionCheckValue = currentVelocity.x * horizInput;
        float forwardAcceleration, skidAcceleration, frictionAcceleration;
        if (currentMovementState == ECurrentMovementState.Grounded)
        {
            forwardAcceleration = currentLowGravityPowerUpMeter <= 0.0f ? groundForwardAcceleration : lowGravityGroundForwardAcceleration;
            skidAcceleration = currentLowGravityPowerUpMeter <= 0.0f ? groundReverseAcceleration : lowGravityGroundReverseAcceleration;
            frictionAcceleration = currentLowGravityPowerUpMeter <= 0.0f ? groundRunningFriction : lowGravityGroundRunningFriction;
        }
        else if (currentMovementState == ECurrentMovementState.Airborne)
        {
            forwardAcceleration = airForwardAcceleration;
            if (currentLowGravityPowerUpMeter > 0.0f)
            {
                forwardAcceleration = lowGravityAirControl;

                currentLowGravityPowerUpMeter -= lowGravityTimeLossRate * Time.deltaTime;
                currentLowGravityPowerUpMeter -= lowGravityDistanceLossRate * Mathf.Abs(currentVelocity.x) * Time.deltaTime;
            }
            else if (currentFlyingPowerUpMeter > 0.0f && (currentInput.up || currentInput.down))
            {
                forwardAcceleration = flyingAirControl;
            }
            skidAcceleration = airReverseAcceleration;
            frictionAcceleration = airRunningFriction;
        }
        else
        {
            forwardAcceleration = 0;
            skidAcceleration = 0;
            frictionAcceleration = float.PositiveInfinity;
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

        float maxSpeed = groundMaxSpeed * Mathf.Abs(horizInput);
        if (currentMovementState == ECurrentMovementState.Airborne)
        {
            maxSpeed = airMaxSpeed;
            if (timeInCurrentState > 0.0f && currentInput.up && currentFlyingPowerUpMeter > 0.0f)
            {
                isUsingFlightPowerup = true;
                maxSpeed = flyingMaxHorizontalSpeed;
            }
        }
        // Cap speed in both directions.
        if (currentVelocity.x > maxSpeed)
            currentVelocity.x = maxSpeed;
        else if (currentVelocity.x < -maxSpeed)
            currentVelocity.x = -maxSpeed;

        // Move the player with collision.
        Vector3 currentOffset = currentVelocity * Time.deltaTime;

        RaycastHit2D hit;
        int playerWorldCollisionMask = ~4 & ~(1 << 8); // include All, exclude IgnoreRaycast
        int playerFloorCollisionMask = (1 << 8) | ~4;
        Vector3 currentPosition = transform.position;
        canJump = false;
        isOnGround = true;
        switch (currentMovementState)
        {
            case ECurrentMovementState.Grounded:
                isUsingFlightPowerup = false;

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
                hit = Physics2D.Raycast(currentPosition, Vector2.down, maxSeekToGroundDistance + characterHalfSize.y, playerFloorCollisionMask);
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
            case ECurrentMovementState.LongFall:
                isOnGround = false;
                float currentGravity = gravity;
                float maxRise = float.PositiveInfinity;
                float maxFall = -maxFallSpeed;
                if (currentLowGravityPowerUpMeter > 0.0f)
                {
                    currentGravity = lowGravityGravity;
                }
                else if (isUsingFlightPowerup)
                {
                    currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0.0f, Time.deltaTime * flyingAirFriction);
                    currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, 0.0f, Time.deltaTime * flyingAirFriction);
                    currentGravity = 0.0f;
                    maxFall = -flyingMaxFallSpeed;
                    maxRise = flyingMaxRiseSpeed;
                    currentVelocity += Vector2.right * horizInput * flyingAirControl * Time.deltaTime;
                    if (currentInput.up)
                    {
                        currentVelocity += Vector2.up * flyingRisingAcceleration * Time.deltaTime;
                    }
                    else if (currentInput.down)
                    {
                        currentVelocity += Vector2.down * flyingDescendingAcceleration * Time.deltaTime;
                    }
                }
                currentVelocity.y -= currentGravity * Time.deltaTime;
                currentVelocity.y = Mathf.Clamp(currentVelocity.y, maxFall, maxRise);
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
                    int mask = currentVelocity.y > 0 ? playerWorldCollisionMask : playerFloorCollisionMask;
                    hit = Physics2D.Raycast(currentPosition, Vector2.up * currentVelocity.y, Mathf.Abs(offset.y) + characterHalfSize.y, mask);

                    if (hit.collider != null && hit.collider.GetComponent<Platform>())
                    {
                        hit.collider.GetComponent<Platform>().ReportLandedOn();
                    }

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
                canJump = false; // TODO Ghost jump.
                break;
            case ECurrentMovementState.HelperIsPullingPlayerUpToLedge:
            case ECurrentMovementState.HelperIsLiftingPlayerUpToLedge:
                currentVelocity = Vector2.zero;
                // Ignore Z-depth, it can mess up this calculation
                currentPosition.x = Mathf.MoveTowards(transform.position.x, climbToLedgeTarget.x, climbUpToHelperSpeed * Time.deltaTime);
                currentPosition.y = Mathf.MoveTowards(transform.position.y, climbToLedgeTarget.y, climbUpToHelperSpeed * Time.deltaTime);
                transform.position = currentPosition;
                if (transform.position.x == climbToLedgeTarget.x && transform.position.y == climbToLedgeTarget.y)
                {
                    SetCurrentState(ECurrentMovementState.Grounded);
                }
                break;
            case ECurrentMovementState.PlayerIsLiftingHelperToLedge:
                if (timeInCurrentState > pushHelperUpTime)
                {
                    SetCurrentState(ECurrentMovementState.Grounded);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Picks up the powerup and applies its effect.
    /// </summary>
    /// <param name="powerUp">The powerup being picked up.</param>
    private bool CheckGetPowerUp(PowerUp powerUp, SInput currentInput)
    {
        // Pick up the powerup on the frame the player presses the interact button.
        if (currentInput.interactDown)
        {
            float? overrideDuration = powerUp.overrideDuration;
            switch (powerUp.powerUpType)
            {
                case PowerUp.EPowerUpType.AirWalk:
                    if (currentAirWalkPowerUpMeter < 0f) currentAirWalkPowerUpMeter = 0f;
                    currentAirWalkPowerUpMeter += overrideDuration.HasValue ? overrideDuration.Value : maxAirWalkDistance;
                    currentLowGravityPowerUpMeter = 0f;
                    currentFlyingPowerUpMeter = 0f;
                    currentPowerupMeterMaxValue = maxAirWalkDistance;
                    break;
                case PowerUp.EPowerUpType.LowGravity:
                    if (currentLowGravityPowerUpMeter < 0f) currentLowGravityPowerUpMeter = 0f;
                    currentLowGravityPowerUpMeter += overrideDuration.HasValue ? overrideDuration.Value : maxLowGravityTime;
                    currentAirWalkPowerUpMeter = 0f;
                    currentFlyingPowerUpMeter = 0f;
                    currentPowerupMeterMaxValue = maxLowGravityTime;
                    break;
                case PowerUp.EPowerUpType.Flying:
                    if (currentFlyingPowerUpMeter < 0f) currentFlyingPowerUpMeter = 0f;
                    currentFlyingPowerUpMeter += overrideDuration.HasValue ? overrideDuration.Value : flyingMaxDuration;
                    currentAirWalkPowerUpMeter = 0f;
                    currentLowGravityPowerUpMeter = 0f;
                    currentPowerupMeterMaxValue = flyingMaxDuration;
                    break;
                default:
                    Debug.Log("Powerup type not implemented.");
                    break;
            }
            powerUp.DeSpawn();

            SetCurrentState(ECurrentMovementState.Interacting);
            currentInteractionDuration = powerupGrabAnimationLength;

            currentInteractionProxy = Instantiate(powerUp.proxyPrefab, powerUp.transform.position, Quaternion.identity);

            return true;
        }
        // TODO: Show prompt if the player is not pressing the interact button.

        return false;
    }

    /// <summary>
    /// Sets the current state and handles transitional animations.
    /// </summary>
    /// <param name="state"></param>
    private void SetCurrentState(ECurrentMovementState state)
    {
        currentMovementState = state;
        timeInCurrentState = 0.0f;
        string animatorState = animationStateWalking;
        float animSpeed = 1f;
        switch (state)
        {
            case ECurrentMovementState.Standing:
                animatorState = animationStateStandUp;
                animSpeed = 0.3f;
                break;
            case ECurrentMovementState.Grounded:
                animatorState = animationStateIdle;
                break;
            case ECurrentMovementState.Airborne:
                animatorState = currentLowGravityPowerUpMeter > 0f ? animationStateFloatyJump : animationStateJumping;
                break;
            case ECurrentMovementState.Dead:
                Initiate.FadeOut(Color.black, 2f);
                animatorState = animationStateLaying;
                break;
            case ECurrentMovementState.Interacting:
                currentVelocity = Vector2.zero;
                animatorState = animationStatePowerupPickup;
                break;
            case ECurrentMovementState.LongFall:
                animatorState = animationStateFalling;
                break;
            case ECurrentMovementState.HardLand:
                animatorState = animationStateHardLanding;
                break;
            case ECurrentMovementState.Laying:
                animatorState = animationStateLaying;
                break;
        }
        PlayAnimation(animatorState, animSpeed);
    }

    /// <summary>
    /// Handles the animator code, making sure the animator is playing the proper animation.
    /// </summary>
    /// <param name="isOnGround">Is the player on the ground.</param>
    /// <param name="isUsingAirWalk">Is the player using Air Walk.</param>
    private void ControlAnimator(bool isOnGround, bool isUsingAirWalk, bool isUsingFlight)
    {
        // Animator facing
        string animToPlay;
        float velocityX = currentVelocity.x;
        float velocityY = currentVelocity.y;

        if (currentVelocity.x > 0.0f)
        {
            foreach (SpriteRenderer s in BodySprites)
            {
                s.flipX = false;
            }
        }
        else if (currentVelocity.x < 0.0f)
        {
            foreach (SpriteRenderer s in BodySprites)
            {
                s.flipX = true;
            }
        }
        velocityX = Mathf.Abs(velocityX);
        velocityY = Mathf.Abs(velocityY);
        if (isOnGround)
        {
            if (velocityX <= 0.05f)
            {
                animToPlay = currentLowGravityPowerUpMeter > 0f ? animationStateFloating : animationStateIdle;

                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay);
                }
            }
            else
            {
                animToPlay = currentLowGravityPowerUpMeter > 0f ? animationStateFloatyWalk : animationStateWalking;

                var speed = (currentLowGravityPowerUpMeter > 0f ? floatyWalkingAnimationVelocity : walkingAnimationVelocity);
                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay, speed);
                }
            }
        }
        else if (isUsingAirWalk)
        {
            if (velocityX <= 0.05f)
            {
                animToPlay = animationStateIdle;

                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay);
                }
            }
            else
            {
                animToPlay = animationStateAirWalkAnimation;

                var speed = (currentLowGravityPowerUpMeter > 0f ? floatyWalkingAnimationVelocity : walkingAnimationVelocity);
                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay, speed);
                }
            }
        }
        else if (isUsingFlight)
        {
            if (velocityY <= 0.05f)
            {
                animToPlay = animationStateFlightIdle;

                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay);
                }
            }
            else
            {
                animToPlay = animationStateFlightMove;

                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    PlayAnimation(animToPlay);
                }
            }

        }
        else
        {
            // we are falling
            if (!(spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateJumping)     ||
                   spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateFalling)    ||
                   spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateFloatyJump) ||
                   spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateFloatyFalling)))
            {
                PlayAnimation(currentLowGravityPowerUpMeter > 0f ? animationStateFloatyFalling : animationStateFalling, fallAnimSpeed);
            }
        }
    }

    /// <summary>
    /// Checks how much of the powerup we have left and fills the sprite.
    /// </summary>
    private void ControlPowerupFill()
    {
        // Powerup bar
        // Vector3 scale = powerUpBar.transform.localScale;
        if (currentAirWalkPowerUpMeter > 0.0f)
        {
            powerUpBar.gameObject.SetActive(true);
            powerUpBarGraphic.color = airWalkPowerUpBarColor;
            float targetScale = currentAirWalkPowerUpMeter / currentPowerupMeterMaxValue;
            //scale.x = Mathf.MoveTowards(scale.x, targetScale, maxPowerUpBarChangeRate * Time.deltaTime);
            AdjustFill(targetScale);
        }
        else if (currentLowGravityPowerUpMeter > 0.0f)
        {
            powerUpBar.gameObject.SetActive(true);
            powerUpBarGraphic.color = lowGravityPowerUpBarColor;
            float targetScale = currentLowGravityPowerUpMeter / currentPowerupMeterMaxValue;
            //scale.x = Mathf.MoveTowards(scale.x, targetScale, maxPowerUpBarChangeRate * Time.deltaTime);
            AdjustFill(targetScale);
        }
        else if (currentFlyingPowerUpMeter > 0.0f)
        {
            powerUpBar.gameObject.SetActive(true);
            powerUpBarGraphic.color = flyingPowerUpBarColor;
            float targetScale = currentFlyingPowerUpMeter / currentPowerupMeterMaxValue;
            //scale.x = Mathf.MoveTowards(scale.x, targetScale, maxPowerUpBarChangeRate * Time.deltaTime);
            AdjustFill(targetScale);
        }
        else
        {
            powerUpBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Helper function that directly manipulates the sprite shader to show the fill effect.
    /// </summary>
    /// <param name="percent">Percentage we should fill.</param>
    private void AdjustFill(float percent)
    {
        float fillAmount = fillMin + (percent * (fillMax - fillMin));

        Renderer rend = powerUpBarGraphic.GetComponent<Renderer>();
        float curFill = rend.material.GetFloat("_Fill");

        float destFill = Mathf.MoveTowards(curFill, fillAmount, maxPowerUpBarChangeRate * Time.deltaTime);

        rend.material.SetFloat("_Fill", destFill);
    }

    /// <summary>
    /// Plays an animation, only allowing a single animation to be begun each frame.
    /// </summary>
    /// <param name="animationToPlay">The name of the animation that will be played.</param>
    /// <param name="animationSpped">The speed the animation should be played at.</param>
    private void PlayAnimation(string animationToPlay, float animationSpped = 1f)
    {
        if (hasPlayedAnimationThisFrame) return;

        spriteAnimator.speed = animationSpped;
        spriteAnimator.CrossFade(animationToPlay, 0f);
        hasPlayedAnimationThisFrame = true;
    }

    public bool IsPressingAiHelperButton()
    {
        return SInput.GetCurrentInput().interactDown;
    }

    public void AiHelperPullsPlayerUpOntoLedge(Vector3 segmentEndPosition)
    {
        SetCurrentState(ECurrentMovementState.HelperIsPullingPlayerUpToLedge);
        climbToLedgeTarget = segmentEndPosition + Vector3.up * characterHalfSize.y;
    }

    public void AiHelperLiftsPlayerUpOntoLedge(Vector3 segmentEndPosition)
    {
        SetCurrentState(ECurrentMovementState.HelperIsLiftingPlayerUpToLedge);
        climbToLedgeTarget = segmentEndPosition + Vector3.up * characterHalfSize.y;
    }

    public void LiftAiHelperOntoLedge(Vector3 position)
    {
        SetCurrentState(ECurrentMovementState.PlayerIsLiftingHelperToLedge);
    }

    public Vector3 GetAITargetPosition()
    {
        return CurrentAIPosition;
    }
}
