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
    public string animationStateStanding = "anim_idle";
    public string animationStateWalking = "anim_walk";
    public string animationStateJumping = "anim_jump";
    public string animationStateFalling = "anim_fall";
    public string animationStateFloating = "anim_float_idle";
    public string animationStateFloatyWalk = "anim_float_walk";
    
    [Tooltip("List here all sprite renderers that you want to flip when the player reverses direction.")]
    public List<SpriteRenderer> BodySprites = new List<SpriteRenderer>();
    public float maxPowerUpBarChangeRate = 1.0f;
    [Tooltip("Maximum value of a powerup, used for fill.")]
    public float maxPowerUpValue = 15f;

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
    public float maxAirWalkDistance = 2.0f;
    [Tooltip("How much air walk power the player loses when they jump while air walking.")]
    public float airWalkJumpLoss = 2.0f;

    [Header("Low Gravity Powerup")]
    public Color lowGravityPowerUpBarColor = Color.blue;
    public float maxLowGravityTime = 8.0f;
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
    public float flyingMaxDuration = 5.0f;
    public float flyingRisingAcceleration = 10.0f;
    public float flyingDescendingAcceleration = 10.0f;
    public float flyingAirControl = 1.0f;
    public float flyingMaxHorizontalSpeed = 100.0f;
    public float flyingMaxRiseSpeed = 100.0f;
    public float flyingMaxFallSpeed = 10.0f;
    public float flyingAirFriction = 1.0f;

    [Header("GameObject Connections")]
    [Tooltip("The power-up bar that appears when the player has a powerup.")]
    public Transform powerUpBar;
    public SpriteRenderer powerUpBarGraphic;
    [Tooltip("The particle system on the player that makes SFX for air walking.")]
    public ParticleSystem airWalkEmitter;

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
            ret.down =  vertical < 0.0f;
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

    private Vector2 lastRespawnPoint = Vector2.zero;
    
    private float fillMax = .696f;
    private float fillMin = .507f;

    //private List<CameraZone> currentCameraZones = new List<CameraZone>();

    void Awake()
    {
        lastRespawnPoint = transform.position;
        //Steamworks.SteamAPI.Init(); // TODO SHould move this to a GameManager.
        //Steamworks.SteamController.Init("");
    }

    // Update is called once per frame
    void Update()
    {
        timeInCurrentState += Time.deltaTime;

        SInput currentInput = SInput.GetCurrentInput();

        float horizInput = currentInput.left + currentInput.right;
        bool isUsingAirWalk = false;
        bool canJump;
        bool isOnGround;

        // read input and handle the movement of the character.
        HandleMovemet(currentInput, out canJump, out isOnGround);

        // State changes.
        if (currentMovementState != ECurrentMovementState.Dead)
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
                    CheckGetPowerUp(col.GetComponent<PowerUp>(), currentInput);
                }
                else if (col.GetComponent<SceneTransitionManager>())
                {
                    col.GetComponent<SceneTransitionManager>().TriggerLoadScene();
                }
            }
        }
        // This bracket is reached if the player is dead or reset is pressed.  Respawn all objects and player.
        else
        {
            transform.position = lastRespawnPoint;
            currentVelocity = Vector2.zero;
            SetCurrentState(ECurrentMovementState.Airborne);
            spriteAnimator.CrossFade(animationStateJumping, 0.0f);

            SpawnTracker.TriggerReset();
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
            currentAirWalkPowerUpMeter -= Time.deltaTime * Mathf.Abs(currentVelocity.x / groundMaxSpeed);
            airWalkEmitter.Emit(1);
        }
        if (isUsingFlightPowerup)
        {
            currentFlyingPowerUpMeter -= Time.deltaTime * currentVelocity.magnitude;
            if (currentFlyingPowerUpMeter <= 0.0f)
            {
                isUsingFlightPowerup = false;
            }
        }

        // Powerup bar
        ControlPowerupFill();

        // Animator
        ControlAnimator(isOnGround, isUsingAirWalk, Mathf.Abs(horizInput));

        currentLowGravityPowerUpMeter -= lowGravityTimeLossRate * Time.deltaTime;
        currentLowGravityPowerUpMeter -= lowGravityDistanceLossRate * Mathf.Abs(currentVelocity.x) * Time.deltaTime;

        transform.position = new Vector3(transform.position.x, transform.position.y, -1);
    }

    private void HandleMovemet(SInput currentInput, out bool canJump, out bool isOnGround)
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
        else
        {
            forwardAcceleration = airForwardAcceleration;
            if (currentLowGravityPowerUpMeter > 0.0f)
            {
                forwardAcceleration = lowGravityAirControl;
            }
            else if (currentFlyingPowerUpMeter > 0.0f && currentInput.jumpHeld)
            {
                forwardAcceleration = flyingAirControl;
            }
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

        float maxSpeed = groundMaxSpeed * Mathf.Abs(horizInput);
        if (currentMovementState == ECurrentMovementState.Airborne)
        {
            maxSpeed = airMaxSpeed;
            if (timeInCurrentState > 0.0f && currentInput.jumpDown && currentFlyingPowerUpMeter > 0.0f)
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
                    if (currentInput.jumpHeld)
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
    }

    /// <summary>
    /// Picks up the powerup and applies its effect.
    /// </summary>
    /// <param name="powerUp">The powerup being picked up.</param>
    private void CheckGetPowerUp(PowerUp powerUp, SInput currentInput)
    {
        // Pick up the powerup on the frame the player presses the interact button.
        if (currentInput.interactDown)
        {
            float? overrideDuration = powerUp.overrideDuration;
            switch (powerUp.powerUpType)
            {
                case PowerUp.EPowerUpType.AirWalk:
                    currentAirWalkPowerUpMeter = overrideDuration.HasValue ? overrideDuration.Value : maxAirWalkDistance;
                    currentPowerupMeterMaxValue = maxPowerUpValue;
                    break;
                case PowerUp.EPowerUpType.LowGravity:
                    currentLowGravityPowerUpMeter = overrideDuration.HasValue ? overrideDuration.Value : maxLowGravityTime;
                    currentPowerupMeterMaxValue = maxPowerUpValue;
                    break;
                case PowerUp.EPowerUpType.Flying:
                    currentFlyingPowerUpMeter = overrideDuration.HasValue ? overrideDuration.Value : flyingMaxDuration;
                    currentPowerupMeterMaxValue = maxPowerUpValue;
                    break;
                default:
                    Debug.Log("Powerup type not implemented.");
                    break;
            }
            powerUp.DeSpawn();
        }
        // TODO: Show prompt if the player is not pressing the interact button.
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
        switch (state)
        {
            case ECurrentMovementState.Grounded:
                animatorState = animationStateWalking;
                break;
            case ECurrentMovementState.Airborne:
                animatorState = animationStateJumping;
                break;
            case ECurrentMovementState.Dead:
                break;
        }
        spriteAnimator.CrossFade(animatorState, 0.0f);
    }

    /// <summary>
    /// Handles the animator code, making sure the animator is playing the proper animation.
    /// </summary>
    /// <param name="isOnGround">Is the player on the ground.</param>
    /// <param name="isUsingAirWalk">Is the player using Air Walk.</param>
    private void ControlAnimator(bool isOnGround, bool isUsingAirWalk, float inputPercentage = 1f)
    {
        // Animator facing
        string animToPlay;
        float velocity = currentVelocity.x;

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
        velocity = Mathf.Abs(velocity);
        if (isOnGround || isUsingAirWalk)
        {
            if (velocity <= 0.05f)
            {
                spriteAnimator.speed = 1.0f;

                animToPlay = currentLowGravityPowerUpMeter > 0f ? animationStateFloating : animationStateStanding;

                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    spriteAnimator.CrossFade(animToPlay, 0.0f);
                }
            }
            else
            {
                animToPlay = currentLowGravityPowerUpMeter > 0f ? animationStateFloatyWalk : animationStateWalking;

                spriteAnimator.speed = inputPercentage * (currentLowGravityPowerUpMeter > 0f ? floatyWalkingAnimationVelocity : walkingAnimationVelocity);
                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animToPlay))
                {
                    spriteAnimator.CrossFade(animToPlay, 0.0f);
                }
            }
        }
        else
        {
            // we are falling
            spriteAnimator.speed = fallAnimSpeed;
            if (!(spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateJumping) ||
                   spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateFalling)))
            {
                spriteAnimator.CrossFade(animationStateFalling, 0.0f);
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
}
