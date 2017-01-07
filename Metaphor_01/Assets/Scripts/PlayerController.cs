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
    public string animationStateStanding = "Standing";
    public string animationStateWalking = "Walking";
    public string animationStateJumping = "Jumping";

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

    public float fallingAnimationStartVelocity = -1.0f;
    public float fallingAnimationEndVelocity = 1.0f;

    [Header("Air Walk Powerup")]
    [Tooltip("How far the player can move horizontally with the air walk powerup.")]
    public float maxAirWalkDistance = 2.0f;
    [Tooltip("How much air walk power the player loses when they jump while air walking.")]
    public float airWalkJumpLoss = 2.0f;

    [Header("GameObject Connections")]
    [Tooltip("The power-up bar that appears when the player has a powerup.")]
    public Transform powerUpBar;
    [Tooltip("The particle system on the player that makes SFX for air walking.")]
    public ParticleSystem airWalkEmitter;

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

    private float currentAirWalkPowerUpMeter = 0.0f;

    private Vector2 currentVelocity = Vector2.zero;

    private Vector2 lastRespawnPoint = Vector2.zero;

    private List<CameraZone> currentCameraZones = new List<CameraZone>();

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
        if (currentMovementState == ECurrentMovementState.Airborne)
        {
            maxSpeed = airMaxSpeed;
        }
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

        bool isUsingAirWalk = false;

        // State changes.
        if (currentMovementState != ECurrentMovementState.Dead)
        {
            // TODO Use nonalloc
            currentCameraZones.Clear();
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, characterHalfSize, 0.0f, 4); // 4 is IgnoreRaycast for triggers.
            foreach (Collider2D col in colliders)
            {
                if (col.GetComponent<KillPlayerZone>())
                {
                    // Check if we've died.
                    SetCurrentState(ECurrentMovementState.Dead);
                    canJump = false;
                    break;
                }
                else if (col.GetComponent<CameraZone>())
                {
                    currentCameraZones.Add(col.GetComponent<CameraZone>());
                }
                else if (col.GetComponent<PlayerCheckpoint>() && lastRespawnPoint != (Vector2)col.transform.position)
                {
                    // Check if we're resetting the respawn point.
                    lastRespawnPoint = col.transform.position;
                    Debug.Log("Set respawn point to " + lastRespawnPoint + ". TODO: Particle effect for checkpoints?");
                }
                else if (col.GetComponent<PowerUp>())
                {
                    GetPowerUp(col.GetComponent<PowerUp>());
                }
            }
        }
        else
        {
            transform.position = lastRespawnPoint;
            currentVelocity = Vector2.zero;
            SetCurrentState(ECurrentMovementState.Airborne);
            spriteAnimator.CrossFade(animationStateJumping, 0.0f);
        }
        if (canJump && currentInput.jumpDown)
        {
            // Jump action.
            SetCurrentState(ECurrentMovementState.Airborne);
            currentVelocity.y = jumpRisingVelocity;
            currentVelocity.x += horizInput * forwardJumpVelocityBoost;
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
            currentAirWalkPowerUpMeter -= airWalkJumpLoss;
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

        if (isUsingAirWalk)
        {
            currentAirWalkPowerUpMeter -= Time.deltaTime * Mathf.Abs(currentVelocity.x / groundMaxSpeed);
            airWalkEmitter.Emit(1);
        }

        // Powerup bar
        if (currentAirWalkPowerUpMeter > 0.0f)
        {
            powerUpBar.gameObject.SetActive(true);
            Vector3 scale = powerUpBar.transform.localScale;
            scale.x = currentAirWalkPowerUpMeter / maxAirWalkDistance;
            powerUpBar.transform.localScale = scale;
        }
        else
        {
            powerUpBar.gameObject.SetActive(false);
        }

        // Cameras
        CameraZone cameraZone = null;
        // TODO Camera smoothing.
        foreach (CameraZone camera in currentCameraZones)
        {
            if (cameraZone == null || camera.priority > cameraZone.priority)
            {
                cameraZone = camera;
            }
        }
        if (cameraZone != null)
        {
            Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, cameraZone.screenHeight / 2.0f, Time.deltaTime * cameraZone.screenHeightAdjustSpeed);
            Vector2 cameraSize = new Vector2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);
            Vector3 cameraPosition = transform.position + Vector3.back * 10.0f;
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, cameraZone.left + cameraSize.x, cameraZone.right - cameraSize.x);
            cameraPosition.y = Mathf.Clamp(cameraPosition.y, cameraZone.bottom + cameraSize.y, cameraZone.top - cameraSize.y);
            Camera.main.transform.position = cameraPosition;
        }
        else
        {
            Camera.main.transform.position = transform.position + Vector3.back * 10.0f;
        }

        // Animator facing
        float velocity = currentVelocity.x;
        if (currentVelocity.x > 0.0f)
        {
            spriteAnimator.GetComponent<SpriteRenderer>().flipX = false;
        }
        else if (currentVelocity.x < 0.0f)
        {
            spriteAnimator.GetComponent<SpriteRenderer>().flipX = true;
        }
        velocity = Mathf.Abs(velocity);
        if (isOnGround || isUsingAirWalk)
        {
            if (velocity <= 0.05f)
            {
                spriteAnimator.speed = 1.0f;
                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateStanding))
                {
                    spriteAnimator.CrossFade(animationStateStanding, 0.0f);
                }
            }
            else
            {
                spriteAnimator.speed = velocity * walkingAnimationVelocity;
                if (!spriteAnimator.GetCurrentAnimatorStateInfo(layerIndex: 0).IsName(animationStateWalking))
                {
                    spriteAnimator.CrossFade(animationStateWalking, 0.0f);
                }
            }
        }
        else
        {
            spriteAnimator.speed = 0.0f;
            spriteAnimator.ForceStateNormalizedTime(Mathf.InverseLerp(fallingAnimationStartVelocity, fallingAnimationEndVelocity, currentVelocity.y));
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, -1);
    }

    public void GetPowerUp(PowerUp powerUp)
    {
        switch (powerUp.powerUpType)
        {
            case PowerUp.EPowerUpType.AirWalk:
                currentAirWalkPowerUpMeter = maxAirWalkDistance;
                break;
            default:
                Debug.Log("Powerup type not implemented.");
                break;
        }
        powerUp.DeSpawn();
    }

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
}
