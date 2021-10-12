using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    #region Variables

    #region Parameters 

    #region Look Rotation
    [Header("Look Rotation")]
    
    [Tooltip("How sensitive the look/aim rotation is regarding the mouse input."), Range(0.1f, 10f)]
    public float mouseSensitivity = 3f;

    // The current XY rotation
    private Vector2 lookRotation = Vector2.zero;
    #endregion

    #region Camera Follow
    [Header("Camera Follow")]

    [Tooltip("How closely the camera follows the player head position, rotation is not smoothed."), Range(0f, 50f)]
    public float followStrength = 25f;
    #endregion

    #region Head Bouncing
    [Header("Head Bouncing")]

    [Tooltip("How fast the head bounce animation is played."), Range(0f, 10f)]
    public float speedScale = 1f;

    [Tooltip("The maximum horizontal distance the head can bounce."), Range(0f, 1f)]
    public float maxHorizontalDistance = 0.5f;

    [Tooltip("The maximum vertical distance the head can bounce."), Range(0f, 1f)]
    public float maxVerticalDistance = 0.25f;

    [Tooltip("How much faster the head bounce animation plays when sprinting. Crouch scaling is automatic."), Range(1f, 3f)]
    public float headBounceSprintMultiplier = 1.5f;

    [Tooltip("How closely the smooth velocity resembles the actual velocity. Used for smoothing head bob animation when the walk velocity rapidly changes (ie. walking into a wall)."), Range(0f, 100f)]
    public float velocitySmoothing = 50f;
    
    // How fast the player is actually moving and their smoothed speed.
    private float walkSpeedSmooth;
    private float currentSpeedSmooth;

    // Proportional to the distance the player has moved so far, used an input for the head bounce functions
    private float moveTime = 0f;
    #endregion

    #endregion

    #region Options
    [Header("Options")]

    [Tooltip("Toggles head bounce animation on / off.")]
    public bool allowHeadBounce = true;

    [Tooltip("Scales the intensity of screenshake effects within the game."), Range(0f, 1f)]
    public float screenShakeIntensity = 1f;
    #endregion

    #region Components / References
    [Header("Components")]

    public Transform playerTransform;
    public Transform headTransform;
    public Transform cameraTransform;
    public Transform holderTransform;
    public Movement playerMovement;
    public Rigidbody playerRigidbody;
    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        // Make the cursor invisible and stop it from leaving the window
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        #region Look/Aim Rotation

        //Note: deltaTime is NOT required for this, as GetAxis refers to the distance moved this frame
        //Update Rotation (Left/Right - Turret)
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        lookRotation.y += mouseX;

        //Update Rotation (Up/Down - Gun)
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        lookRotation.x -= mouseY;
        lookRotation.x = Mathf.Clamp(lookRotation.x, -85f, 85f);

        //Set Rotation (All)
        playerTransform.localEulerAngles = new Vector3(0f, lookRotation.y, 0f);
        holderTransform.localEulerAngles = new Vector3(lookRotation.x, lookRotation.y, 0f);

        #endregion

        // Calculate position to follow player from
        Vector3 followPosition = (headTransform.position - holderTransform.position) * followStrength * Time.deltaTime;
        followPosition += holderTransform.position;

        #region Camera Motion Effects

        #region Head Bounce

        Vector3 headBounceOffset = Vector3.zero;

        // If the option for head bounce effects is enabled
        if (allowHeadBounce)
        {
            // 0 to 1 representing our % of the max speed, smoothed over time
            float velocityScale = walkSpeedSmooth / (playerMovement.baseMoveSpeed * headBounceSprintMultiplier);
            velocityScale = Mathf.Clamp(velocityScale, 0f, 1f);
        
            // Only animate when grounded and not sliding, otherwise pause
            if (Movement.isSliding == false && Movement.isGrounded)
            {
                moveTime += velocityScale * Time.deltaTime;
            }

            float bounceHeight = (Mathf.Cos(2f * moveTime * speedScale) - 1f) * maxVerticalDistance * velocityScale;
            float bounceLength = Mathf.Sin(moveTime * speedScale) * maxHorizontalDistance * velocityScale;

            headBounceOffset = (Vector3.up * bounceHeight) + (holderTransform.right * bounceLength);
        }

        #endregion

        #region Screenshake



        #endregion

        // Apply combined effects to camera
        Vector3 effectPosition = headBounceOffset;
        cameraTransform.localPosition = effectPosition;

        #endregion

        // The holder follows the player
        holderTransform.position = followPosition;
    }

    void FixedUpdate()
    {
        #region Smooth Speed Readouts

        // Increase/Decrease by limited amount, forcefully linearly smoothing results
        float actualSpeed = playerRigidbody.velocity.magnitude;
        float addition = Mathf.Min(Mathf.Abs(actualSpeed - currentSpeedSmooth), velocitySmoothing * Time.fixedDeltaTime);
        currentSpeedSmooth += (actualSpeed > currentSpeedSmooth) ? addition : -addition;

        // Velocity without y component
        Vector3 walkVelocity = playerRigidbody.velocity;
        walkVelocity.y = 0f;

        float actualWalkSpeed = walkVelocity.magnitude;
        float walkAddition = Mathf.Min(Mathf.Abs(actualWalkSpeed - walkSpeedSmooth), velocitySmoothing * Time.fixedDeltaTime);
        walkSpeedSmooth += (actualWalkSpeed > walkSpeedSmooth) ? walkAddition : -walkAddition;

        #endregion
    }
}
