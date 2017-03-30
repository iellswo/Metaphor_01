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

    // Use this for initialization
    void Start ()
    {
		
	}

    // LateUpdate is called after Update each frame
    void LateUpdate ()
    {
        // Handle orthographic size smoothing (zoom)
        Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, MainBoundingBox.screenHeight / 2.0f, Time.deltaTime * MainBoundingBox.screenHeightAdjustSpeed);
        Vector2 cameraSize = new Vector2(Camera.main.orthographicSize * Camera.main.aspect, Camera.main.orthographicSize);

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 playerPosition = Player.gameObject.transform.position;

        cameraPosition.z = (Vector3.back * 10f).z;

        // Handle x movement
        cameraPosition.x = Lerp(cameraPosition.x, playerPosition.x, LerpSpeedX);

        // Handle y movement
        if (Player.currentMovementState == PlayerController.ECurrentMovementState.Grounded)
        {
            cameraPosition.y = Lerp(cameraPosition.y, playerPosition.y, LerpSpeedY);
        }

        // Handle Clamping
        cameraPosition.x = Mathf.Clamp(cameraPosition.x, MainBoundingBox.left + cameraSize.x, MainBoundingBox.right - cameraSize.x);
        cameraPosition.y = Mathf.Clamp(cameraPosition.y, MainBoundingBox.bottom + cameraSize.y, MainBoundingBox.top - cameraSize.y);

        // Set camera position to calculated location.
        Camera.main.transform.position = cameraPosition;
	}

    private float Lerp (float a, float b, float speed)
    {
        return a + (Time.deltaTime * speed * (b - a));
    }
}
