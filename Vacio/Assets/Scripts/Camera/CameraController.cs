using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("Reference to the player object.")]
    public PlayerController Player;

    [Tooltip("The camera zone that defines the ultimate bounding zone for the main camera.")]
    public CameraZone MainBoundingBox;

    [Tooltip("Changing this will effect how fast we attempt to move the camera's x coord towards the desired position.")]
    public float LerpSpeedX = 0.5f;

    [Tooltip("Changing this will effect how fast we attempt to move the camera's y coord towards the desired position.")]
    public float LerpSpeedY = 0.7f;

    // We get the camera to focus ahead of the player when they are moving so that the player can see where they are going and what is ahead of them.
    // This is accomplished by "tricking" the camera to think that the player is actually ahead of there actual position.  This defines that distance.
    [Tooltip("How far ahead of the player we want our focus point.")]
    public float PlayerLeaderX = 0f;

    [Tooltip("How fast the player needs to move before we start leading the player.")]
    public float PlayerLeaderXMinSpeed = 0.5f;

    public float PlayerWindowYFromEdge = 1.5f;

    private float _lastSafeY = 0f;

    // Use this for initialization
    void Start()
    {
        Vector3 playerPosition = Player.gameObject.transform.position;

        // Set camera position to calculated location.
        Camera.main.transform.position = playerPosition;
    }

    // LateUpdate is called after Update each frame
    void LateUpdate()
    {
        // Handle orthographic size smoothing (zoom)
        Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, MainBoundingBox.screenHeight / 2.0f, Time.deltaTime * MainBoundingBox.screenHeightAdjustSpeed);
        Vector2 cameraSize = new Vector2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 playerPosition = Player.gameObject.transform.position;

        cameraPosition.z = (Vector3.back * 10f).z;

        // Handle x movement
        if (Player.currentVelocity.x > PlayerLeaderXMinSpeed)
        {
            playerPosition.x += PlayerLeaderX;
        }
        else if (Player.currentVelocity.x < -1 * PlayerLeaderXMinSpeed)
        {
            playerPosition.x -= PlayerLeaderX;
        }

        cameraPosition.x = Lerp(cameraPosition.x, playerPosition.x, LerpSpeedX);

        // Handle y movement
        if (Player.currentMovementState == PlayerController.ECurrentMovementState.Grounded)
        {
            _lastSafeY = playerPosition.y;

            cameraPosition.y = Lerp(cameraPosition.y, _lastSafeY, LerpSpeedY);
        }
        else
        {
            float windowDistance = (.5f * MainBoundingBox.screenHeight - PlayerWindowYFromEdge);
            if (Mathf.Abs(cameraPosition.y - playerPosition.y) >= windowDistance)
            {
                float desiredCameraY = cameraPosition.y;
                if ((playerPosition.y - cameraPosition.y) > 0f && Player.currentVelocity.y > 0f)
                {
                    desiredCameraY = playerPosition.y - windowDistance;
                }
                else if ((playerPosition.y - cameraPosition.y) < 0f && Player.currentVelocity.y < 0f)
                {
                    desiredCameraY = playerPosition.y + windowDistance;
                }

                cameraPosition.y = desiredCameraY;
                    //Lerp(cameraPosition.y, desiredCameraY, LerpSpeedY * 2);
            }
        }

        // Handle Clamping
        cameraPosition.x = Mathf.Clamp(cameraPosition.x, MainBoundingBox.left + cameraSize.x, MainBoundingBox.right - cameraSize.x);
        cameraPosition.y = Mathf.Clamp(cameraPosition.y, MainBoundingBox.bottom + cameraSize.y, MainBoundingBox.top - cameraSize.y);

        // Set camera position to calculated location.
        Camera.main.transform.position = cameraPosition;
    }

    private float Lerp(float a, float b, float speed)
    {
        return a + (Time.deltaTime * speed * (b - a));
    }
}
