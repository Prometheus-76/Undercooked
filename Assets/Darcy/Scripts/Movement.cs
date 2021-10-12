﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson

public class Movement : MonoBehaviour
{
    #region Variables

    #region Internal

    // Represents the player's WASD input on the horizontal and vertical axes
    Vector2 inputVector;

    // Used for tracking velocity between FixedUpdate() iterations
    Vector3 currentVelocity;
    Vector3 newVelocity;
    Vector3 walkVelocity;
    Vector3 maxCurrentVelocity;
    float maxCurrentVelocityMagnitude;

    // Movement State Tracking
    public static bool applyingGravity { get; private set; }
    public static bool isGrounded { get; private set; }
    public static bool isSprinting { get; private set; }
    public static bool isCrouching { get; private set; }
    public static bool isMantling { get; private set; }
    public static bool isSliding { get; private set; }
    public static bool isGrappling { get; private set; }

    private bool lastGroundedState = true;
    private bool lastCrouchState = false;
    private bool attemptingCrouch = false;
    private bool canJump = true;
    private bool waitingToJump = false;
    private bool canMantle = true;
    private bool startedSliding = false;
    private bool isSlopeSliding = false;
    private bool isSlideJumping = false;
    private bool sprintInputAcknowledged = false;
    private bool canGrapple = true;
    private bool waitingToAttemptGrapple = false;

    #endregion

    #region Parameters

    #region Crouch / Walk / Sprint Movement
    [Header("Crouch / Walk / Sprint Movement")]

    [Tooltip("The player's maximum base walking speed."), Range(0f, 20f)]
    public float baseMoveSpeed = 6f;

    [Tooltip("How fast the player accelerates to their top speed when walking/sprinting/crouching on the ground."), Range(1f, 100f)]
    public float groundAcceleration = 50f;

    [Tooltip("How fast the player accelerates when airborn (does not affect gravity acceleration)."), Range(1f, 100f)]
    public float airAcceleration = 10f;

    [Tooltip("How much faster the player accelerates to their top speed when changing direction. Scales based on how different the movement direction is."), Range(1f, 3f)]
    public float directionChangeCoefficient = 2f;

    [Tooltip("How much faster sprinting is than walking."), Range(1f, 3f)]
    public float sprintMoveSpeedCoefficient = 2f;

    [Tooltip("How much slower crawling is than walking."), Range(0f, 1f)]
    public float crouchMoveSpeedCoefficient = 0.5f;

    [Tooltip("How quickly the player decelerates when coming to a stop on the ground."), Range(0f, 25f)]
    public float groundDragCoefficient = 10f;

    [Tooltip("How quickly the player decelerates when coming to a stop in the air."), Range(0f, 5f)]
    public float airDragCoefficient = 1f;
    #endregion

    #region Ground Validation
    [Header("Ground Validation")]

    [Tooltip("Which layers are identified as ground for the player. This is also used for all other kinds of platforming interaction.")]
    public LayerMask groundLayerMask;

    [Tooltip("How detailed the ground check is."), Range(4f, 10f)]
    public int groundCheckResolution = 8;

    [Tooltip("How far ground can be detected below the player. Values smaller than 0.1 can risk the validation failing."), Range(0f, 1f)]
    public float groundCheckDistance = 0.1f;

    [Tooltip("How long ground checks are disabled for after jumping, smallest functioning value above 0 is recommended. Used to prevent janky interactions when jumping up slopes."), Range(0f, 0.2f)]
    public float groundCheckCooldown = 0.1f;

    // The timer float used by groundCheckCooldown
    private float groundCheckTimer = 0f;
    #endregion

    #region Gravity
    [Header("Gravity")]

    [Tooltip("The strength of the player's acceleration due to custom gravity. Lower values means longer, floatier jumps for the player. Note: Jump height will remain the same."), Range(0f, 50f)]
    public float gravityAcceleration = 40f;
    #endregion

    #region Jump
    [Header("Jump")]

    [Tooltip("How high the player will jump in world units, regardless of gravity setting."), Range(0f, 5f)]
    public float jumpHeight = 3f;

    [Tooltip("How long the pre-ground jump input buffer is. Each value represents 1/100 of a second."), Range(0f, 100f)]
    public int jumpInputBufferDepth = 20;

    [Tooltip("How long the coyote time (post-ground) jump buffer is. Each value represents 1/100 of a second."), Range(0f, 100f)]
    public int groundContactBufferDepth = 30;

    // FixedUpdate() frame buffers to assist pre-ground jumping and coyote time (post-ground) jumping
    private Queue<bool> jumpInputBuffer;
    private Queue<bool> groundContactBuffer;
    #endregion

    #region Surface Magnetism
    [Header("Surface Magnetism")]

    [Tooltip("How strongly the player is attracted to surfaces they are standing on (used to improve physics interactions on slopes). Making this too strong will cause noticable issues at the top and bottom of ramps."), Range(0f, 10f)]
    public float magnetismStrengthCoefficient = 3f;

    [Tooltip("The minimum speed the player must be moving in order for surface magnetism to activate."), Range(0f, 10f)]
    public float magnetismMinVelocityThreshold = 2f;

    [Tooltip("How far off the ground the player can be attracted to surfaces."), Range(0f, 5f)]
    public float magnetismRange = 2f;

    [Tooltip("How much the player velocity affects the strength of magnetism (higher values means more strength)."), Range(0f, 1f)]
    public float magnetismVelocityScaling = 0.1f;
    #endregion

    #region Ledge Mantle
    [Header("Ledge Mantle")]

    [Tooltip("The player relative velocity when mantling up a ledge. Represented as ([right], [up], [forward]).")]
    public Vector3 mantleVelocity; // Default: (right: 0, up: 8, forward: 10)

    [Tooltip("Shoots a ray straight out of the player at this height, must not hit a wall in order to mantle."), Range(2f, 5f)]
    public float mantleHeadCheckHeight = 2.5f;
    
    [Tooltip("Shoots a ray straight out of the player at this height, must hit a wall in order to mantle."), Range(1f, 2f)]
    public float mantleFootholdHeight = 1.5f;

    [Tooltip("Shoots a ray straight out of the player at this height, the mantle ends when this does not hit a wall."), Range(-1f, 1f)]
    public float mantleClearedHeight = -0.1f;
    #endregion

    #region Crouch
    [Header("Crouch")]

    [Tooltip("How long it takes to go from fully standing to crouched, and vice versa."), Range(0f, 0.5f)]
    public float crouchTransitionTime = 0.1f;

    [Tooltip("How tall the player is when crouched, must be lower than their standard height."), Range(1f, 2f)]
    public float crouchColliderHeight = 1f;

    // The player's default height, assigned on scene load from reading the player collider height property
    private float standardHeight = 2f;
    private float headPointHeight = 2f;
    #endregion

    #region Sliding
    [Header("Sliding")]

    [Tooltip("The move speed boost you get when sliding."), Range(1f, 5f)]
    public float slideInitialVelocityCoefficient = 1.2f;

    [Tooltip("The amount of drag you experience when sliding on the ground."), Range(0f, 25f)]
    public float slideGroundDragCoefficient = 2.5f;

    [Tooltip("The amount of drag you experience when jumping after sliding."), Range(0f, 10f)]
    public float slideAirDragCoefficient = 2f;

    [Tooltip("The force applied to the player in the direction of their slide down a ramp."), Range(0f, 100f)]
    public float slideSlopeForceCoefficient = 25f;

    [Tooltip("The amount of drag you experience when sliding down a slope."), Range(0f, 5f)]
    public float slideSlopeDragCoefficient = 0.5f;

    [Tooltip("The minimum slope angle required to slide (in degrees, 0 is flat and 90 is vertical)."), Range(0f, 90f)]
    public float slideSlopeMinAngle = 10f;

    [Tooltip("The maximum slope angle the player is able to slide down (in degrees, 0 is flat and 90 is vertical)."), Range(0f, 90f)]
    public float slideSlopeMaxAngle = 40f;
    #endregion

    #region Grapple Hookshot
    [Header("Grapple Hookshot")]

    [Tooltip("The smallest distance the player is able to grapple to."), Range(0f, 10f)]
    public float grappleMinRange = 3f;

    [Tooltip("The largest distance the player is able to grapple to."), Range(0f, 50f)]
    public float grappleMaxRange = 15f;

    [Tooltip("How high above the camera the grapple must be."), Range(-3f, 3f)]
    public float grappleMinHeightOffset = 1f;

    [Tooltip("How close the player is able to aim to a grapple point before it is recognised (-1 is 180 degrees away from and 1 is directly towards)."), Range(0f, 1f)]
    public float minimumAngularSimilarity = 0.9f;

    [Tooltip("The Unity Engine layer which grapple points are on.")]
    public LayerMask grapplePointLayerMask;

    [Tooltip("How fast the player moves towards a grapple point."), Range(1f, 100f)]
    public float grappleVelocity = 40f;

    [Tooltip("How high above a grapple point the player camera should end at when finishing a grapple."), Range(0f, 3f)]
    public float grapplePointVerticalOffset = 1f;

    // The point which we are grappling to
    private Transform grapplePoint;

    // Set to true after grapple to automatically mantle on ledges, removes the need for moving forward
    private bool grappleAutoMantle = false;
    #endregion

    #region Rope Wobble Effect
    [Header("Rope Wobble Effect")]

    [Tooltip("How many points the grapple line is made up of."), Range(2, 1000)]
    public int grappleLineResolution = 500;

    [Tooltip("The curve which dictates the shape of the grapple effect.")]
    public AnimationCurve grappleRopeEffectCurve;

    [Tooltip("The magnitude (strength) of the grapple rope effect."), Range(0f, 2f)]
    public float grappleEffectWaveHeight = 1f;

    [Tooltip("How many waves occur within the grapple rope effect."), Range(0f, 5f)]
    public float grappleEffectWaveCount = 2f;

    [Tooltip("How fast the grapple rope effect is shot from the gun."), Range(0f, 50f)]
    public float grappleRopeFireSpeed = 20f;

    [Tooltip("How long it takes for the grapple rope wobble effect to fully diminish."), Range(0f, 1f)]
    public float grappleRopeEffectDuration = 0.3f;

    [Tooltip("How much the rope wobble effect scrolls during the grapple."), Range(0f, 10f)]
    public float grappleRopeEffectScrollSpeed = 5f;

    [Tooltip("The curve which dictates the dropoff of the grapple effect.")]
    public AnimationCurve grappleRopeTimeDropoffCurve;

    // The position from the gun to the grapple point, used to interpolate over time and make the gun to appear to fire the shot
    private Vector3 ropePointPosition;

    // The timer used when diminishing a rope effect
    private float ropeEffectTimer = 0f;
    #endregion

    #region Weapon Grapple Rotation
    [Header("Weapon Grapple Rotation")]

    [Tooltip("How fast the weapon rotates towards its target rotation."), Range(0f, 25f)]
    public float gunRotationSpeed = 5f;

    // The rotation the gun is interpolating towards
    private Quaternion targetWeaponRotation;
    #endregion

    #endregion

    #region Options
    [Header("Options")]
    
    [Tooltip("Is the crouch key toggled or held?")]
    public bool toggleCrouch = false;
    #endregion

    #region Components / References

    [Header("Components")]
    public Transform cameraHolderTransform;
    public Transform headPointTransform;
    public Transform weaponPointTransform;
    public Transform weaponHolderTransform;
    public Transform playerTransform;
    public Rigidbody playerRigidbody;
    public CapsuleCollider playerCollider;
    public LineRenderer grappleLine;

    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Initialisation

        jumpInputBuffer = new Queue<bool>();
        groundContactBuffer = new Queue<bool>();

        canJump = true;

        standardHeight = playerCollider.height;
        headPointHeight = headPointTransform.localPosition.y;

        #endregion
    }

    // Update is called once per frame
    void Update()
    {
        // Used for telling which frame the player starts a crouch on from within FixedUpdate()
        lastCrouchState = attemptingCrouch;

        #region Input Handling

        #region WASD Input

        // Get the keyboard WASD input and normalise it
        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");
        inputVector.Normalize();

        #endregion

        #region Jump Input

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Only allow a jump if there is clear headroom for it
            Vector3 headRaycastOrigin = new Vector3(playerTransform.position.x, playerTransform.position.y + crouchColliderHeight, playerTransform.position.z);
            if (Physics.Raycast(headRaycastOrigin, Vector3.up, 0.49f + (standardHeight - crouchColliderHeight), groundLayerMask) == false)
            {
                waitingToJump = true;
            }
        }

        #endregion

        #region Crouch / Slide Input

        // Allow crouch when grounded
        if (isGrounded && canJump)
        {
            if (toggleCrouch)
            {
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    // Toggle crouch
                    attemptingCrouch = !attemptingCrouch;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    // Hold crouch
                    attemptingCrouch = true;
                }
                else
                {
                    // Hold crouch
                    attemptingCrouch = false;
                }
            }

            // If we just started crouching this frame
            if (lastCrouchState == false && attemptingCrouch)
            {
                // If we were sprinting previously and moving forward
                if (isSprinting && inputVector.y > 0f)
                {
                    isSliding = true;
                    startedSliding = true;

                    isSprinting = false;
                }
            }
        }


        #endregion

        #region Grapple Input

        // Attempt a grapple hookshot
        if (Input.GetMouseButtonDown(1) && canGrapple)
        {
            // Only allow a grapple hookshot if there is clear headroom for it
            Vector3 headRaycastOrigin = new Vector3(playerTransform.position.x, playerTransform.position.y + crouchColliderHeight, playerTransform.position.z);
            if (Physics.Raycast(headRaycastOrigin, Vector3.up, 0.49f + (standardHeight - crouchColliderHeight), groundLayerMask) == false)
            {
                waitingToAttemptGrapple = true;
            }
        }

        #endregion

        #region Sprint On/Off

        #region Direct Changes

        // Allow sprint when grounded
        if (isGrounded && canJump)
        {
            // Attempt to start sprinting
            if (Input.GetKey(KeyCode.LeftShift) && sprintInputAcknowledged == false)
            {
                // Only allow sprinting when player has room to fully stand up
                Vector3 headRaycastOrigin = new Vector3(playerTransform.position.x, playerTransform.position.y + crouchColliderHeight, playerTransform.position.z);
                if (Physics.Raycast(headRaycastOrigin, Vector3.up, 0.49f + (standardHeight - crouchColliderHeight), groundLayerMask) == false)
                {
                    // Toggle sprint
                    isSprinting = !isSprinting;
                    sprintInputAcknowledged = true;

                    // If crouching, uncrouch and sprint
                    if (isCrouching)
                    {
                        attemptingCrouch = false;
                        isSprinting = true;
                    }
                }
            }
        }
        else if (isSprinting && Input.GetKeyDown(KeyCode.LeftShift) && isSlideJumping == false)
        {
            // Toggle sprint off in air
            isSprinting = false;
            sprintInputAcknowledged = true;
        }

        // Allow the sprint key to be read again
        if (Input.GetKeyUp(KeyCode.LeftShift) && sprintInputAcknowledged)
        {
            sprintInputAcknowledged = false;
        }

        #endregion

        #region Indirect Changes

        // When the player is no longer moving forward
        if (inputVector.y <= 0f)
        {
            isSprinting = false;
        }

        // When the player uses an ability or enters combat
        if (isMantling)
        {
            isSprinting = false;
        }

        #endregion

        #endregion

        #endregion

        #region Weapon Grapple Rotation

        if (isGrappling)
        {
            // Rotate towards the grappling point
            targetWeaponRotation = Quaternion.LookRotation(grapplePoint.position - weaponPointTransform.position);
            weaponHolderTransform.rotation = Quaternion.Lerp(weaponHolderTransform.rotation, targetWeaponRotation, Time.deltaTime * gunRotationSpeed);
        }
        else
        {
            // Rotate towards the resting position
            targetWeaponRotation = weaponHolderTransform.parent.rotation;
            weaponHolderTransform.rotation = Quaternion.Lerp(weaponHolderTransform.rotation, targetWeaponRotation, Time.deltaTime * gunRotationSpeed);
        }

        #endregion
    }

    // Called after Update()
    void LateUpdate()
    {
        #region Initiate Grapple Hookshot

        if (canGrapple)
        {
            #region Find Colliders Within Range

            // Find all colliders within maximum range
            Collider[] collidersInRange = Physics.OverlapSphere(cameraHolderTransform.position, grappleMaxRange, grapplePointLayerMask);

            Transform closestPoint = null;
            float closestSimilarity = -1f;
            foreach (Collider col in collidersInRange)
            {
                // Calculate vector from camera to grapple point
                Vector3 directionToPoint = (col.transform.position - cameraHolderTransform.position);

                // If the point is outside minimum range
                if (directionToPoint.magnitude > grappleMinRange)
                {
                    // If the grapple point is above the minimum height relative to the camera
                    if (col.transform.position.y > cameraHolderTransform.position.y + grappleMinHeightOffset)
                    {
                        // If there is a clear line of sight to the grapple point
                        if (Physics.Raycast(cameraHolderTransform.position, directionToPoint.normalized, directionToPoint.magnitude, groundLayerMask) == false)
                        {
                            // Highlight potential grapple point with UI effect


                            float similarity = Vector3.Dot(directionToPoint.normalized, cameraHolderTransform.forward);

                            // If this is the new closest point to the player's look vector
                            if (similarity > closestSimilarity)
                            {
                                closestPoint = col.transform;
                                closestSimilarity = similarity;
                            }
                        }
                    }
                }
            }

            #endregion

            #region Check Colliders and Start Grapple

            // If there is a viable hookshot point
            if (closestSimilarity >= minimumAngularSimilarity)
            {
                // Highlight the closest point with UI effect


                // If the player is trying to hookshot
                if (waitingToAttemptGrapple)
                {
                    // Confirm grapple point exists
                    if (closestPoint != null)
                    {
                        // Start grapple
                        grapplePoint = closestPoint;

                        canGrapple = false;
                        isGrappling = true;

                        // Grapple interrupts all previous movement states
                        isSliding = false;
                        isSlopeSliding = false;
                        isSlideJumping = false;
                        isSprinting = false;
                        attemptingCrouch = false;

                        // Start cooldown for ground checks
                        groundCheckTimer = groundCheckCooldown;
                        isGrounded = false;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Render Rope Effect

        if (isGrappling)
        {
            // Enable line renderer points
            grappleLine.positionCount = grappleLineResolution;

            // Determine angular perpendicular vectors
            Vector3 gunToPoint = grapplePoint.position - weaponPointTransform.position;
            Vector3 ropeUp = Quaternion.LookRotation(gunToPoint.normalized) * Vector3.up;
            Vector3 ropeRight = Vector3.Cross(Vector3.up, ropeUp);

            // Extend rope to grapple position
            ropePointPosition = Vector3.Lerp(ropePointPosition, grapplePoint.position, Time.deltaTime * grappleRopeFireSpeed);

            // Increment timer for wobble effect dropoff
            ropeEffectTimer = Mathf.Clamp(ropeEffectTimer + Time.deltaTime, 0f, grappleRopeEffectDuration);

            // Loop over every point in the line
            for (int i = 0; i < grappleLineResolution; i++)
            {
                // 0 - 1 representing how far through the line points we currently are
                float progress = i / (float)grappleLineResolution;

                // Used for creating offsets
                float dropoffValue = grappleRopeTimeDropoffCurve.Evaluate(ropeEffectTimer / grappleRopeEffectDuration);
                float effectValue = grappleRopeEffectCurve.Evaluate(progress);
                float trigonometryInput = (progress * grappleEffectWaveCount * Mathf.PI) + (dropoffValue * grappleRopeEffectScrollSpeed);

                // An offset from the straight line which is applied to the rope wobble effect
                Vector3 yOffset = ropeUp * grappleEffectWaveHeight * Mathf.Sin(trigonometryInput) * dropoffValue * effectValue;
                Vector3 xOffset = ropeRight * grappleEffectWaveHeight * Mathf.Cos(trigonometryInput) * dropoffValue * effectValue;
                Vector3 totalOffset = xOffset + yOffset;

                // Set the world space position of the line point
                grappleLine.SetPosition(i, Vector3.Lerp(weaponPointTransform.position, ropePointPosition, progress) + totalOffset);
            }
        }
        else
        {
            grappleLine.positionCount = 0;
            ropePointPosition = weaponPointTransform.position;
            ropeEffectTimer = 0f;
        }

        #endregion
    }

    // FixedUpdate is called every physics iteration
    void FixedUpdate()
    {
        // Read current velocity values
        CheckVelocity();
        newVelocity = currentVelocity;

        #region Ground Contact Checks

        //Update the previous state
        lastGroundedState = isGrounded;

        // If we are not waiting for the check cooldown to end
        if (groundCheckTimer <= 0f)
        {
            int groundContactCount = 0; // How many points of contact are made between the player and the ground

            float angleDelta = 360 / groundCheckResolution; // How many degrees apart each of the angles are 
            float radius = 0.5f; // How far outward the ray origins are from the player transform origin

            for (int pointNumber = 0; pointNumber < groundCheckResolution; pointNumber++)
            {
                float currentAngle = (pointNumber * angleDelta) + playerTransform.localRotation.eulerAngles.y;

                Vector3 rayOrigin;
                rayOrigin.x = (Mathf.Sin(Mathf.Deg2Rad * currentAngle) * radius) + playerTransform.position.x;
                rayOrigin.y = playerTransform.position.y + 0.5f; // Cast from a little above the ground
                rayOrigin.z = (Mathf.Cos(Mathf.Deg2Rad * currentAngle) * radius) + playerTransform.position.z;

                //If the ground intersected with this ray
                if (Physics.Raycast(rayOrigin, Vector3.down, (groundCheckDistance + 0.5f), groundLayerMask))
                {
                    groundContactCount++;
                }
            }

            //Set grounded (true/false) based on contact count
            isGrounded = (groundContactCount > 1);
        }
        else
        {
            // Decrement the timer
            groundCheckTimer -= Time.fixedDeltaTime;

            // Prevent timer going below 0s
            if (groundCheckTimer < 0f)
            {
                groundCheckTimer = 0f;
            }
        }

        //Call grounded state change functions
        if (isGrounded && lastGroundedState == false)
        {
            GroundDetectionEnter();
        }
        else if (isGrounded == false && lastGroundedState)
        {
            GroundDetectionExit();
        }

        #endregion

        #region Walking / Sprinting / Deceleration

        // Which way the player should move in world space
        Vector3 targetVector = ((playerTransform.forward * inputVector.y) + (playerTransform.right * inputVector.x)).normalized;

        // If we aren't sliding, allow WASD movement
        if (isSliding == false)
        {
            // Determine how hard the player is changing direction and scale multiplier towards 0
            float directionChangeMultiplier = ((1f - Vector3.Dot(targetVector, walkVelocity.normalized) / 2f) * directionChangeCoefficient) + 1f;

            // Calculate acceleration force
            Vector3 movementForce = targetVector * directionChangeMultiplier;

            // Apply different acceleration in air and on the ground
            playerRigidbody.AddForce(movementForce * (isGrounded ? groundAcceleration : airAcceleration), ForceMode.Force);
        }

        CheckVelocity();

        // Applying varying deceleration forces
        if (walkVelocity.magnitude > maxCurrentVelocityMagnitude && (isSliding == false && isSlideJumping == false))
        {
            // If the player's walk/sprint speed is currently exceeding its expected maximum
            Vector3 excessVelocityDrag = (maxCurrentVelocity - walkVelocity);

            // Apply speed restraining drag force
            playerRigidbody.AddForce(excessVelocityDrag, ForceMode.Impulse);
        }
        else if (inputVector == Vector2.zero && (isSliding == false && isSlideJumping == false))
        {
            // If the player should come to a natural stop
            Vector3 restingDrag = Vector3.zero - walkVelocity;

            // Change drag in air and on the ground
            restingDrag *= (isGrounded ? groundDragCoefficient : airDragCoefficient);

            // Apply drag
            playerRigidbody.AddForce(restingDrag, ForceMode.Force);
        }
        else if (isSliding || isSlideJumping || isSlopeSliding)
        {
            // If the player should slow down due to sliding or slide jumping
            Vector3 slideDrag = (Vector3.zero - walkVelocity) * (isGrounded ? slideGroundDragCoefficient : slideAirDragCoefficient);

            // Adjust drag when sliding down slope
            slideDrag *= (isSlopeSliding ? slideSlopeDragCoefficient : 1f);

            // Apply drag
            playerRigidbody.AddForce(slideDrag, ForceMode.Force);
        }

        #endregion

        #region Artificial Gravity

        // Only apply gravity when the player is off the ground
        applyingGravity = (isGrounded == false && isMantling == false && isGrappling == false);

        // Accumulate gravity acceleration from last iteration
        newVelocity.y = playerRigidbody.velocity.y;

        if (applyingGravity)
        {
            newVelocity.y -= gravityAcceleration * Time.fixedDeltaTime;
        }

        #endregion

        #region Jump Handling (Coyote Time and Input Buffering)

        #region Coyote Time

        //Record ground contact status
        groundContactBuffer.Enqueue(isGrounded);

        //Constrain queue to set size
        if (groundContactBuffer.Count > groundContactBufferDepth)
        {
            groundContactBuffer.Dequeue();
        }

        //If there is a ground contact in the queue and the player is waiting to jump
        if (groundContactBuffer.Contains(true) && waitingToJump && canJump)
        {
            //Empty the input buffer
            groundContactBuffer.Clear();
            jumpInputBuffer.Clear();
            waitingToJump = false;
            canJump = false;

            //Perform the jump (prior velocity ignored)
            newVelocity.y = Mathf.Sqrt(2f * gravityAcceleration * (jumpHeight - (groundCheckDistance / 2f)));
            attemptingCrouch = false;

            // Start cooldown for ground checks
            groundCheckTimer = groundCheckCooldown;
            isGrounded = false;

            // Sprint out of slide jump
            if (isSliding)
            {
                isSprinting = true;
                isSlideJumping = true;
            }
        }

        #endregion

        #region Input Buffering

        //Record record jump input status
        jumpInputBuffer.Enqueue(waitingToJump);
        waitingToJump = false;

        //Constrain queue to set size
        if (jumpInputBuffer.Count > jumpInputBufferDepth)
        {
            jumpInputBuffer.Dequeue();
        }

        //If there is an input in the queue and the player has landed on the ground
        if (jumpInputBuffer.Contains(true) && isGrounded && canJump)
        {
            //Empty the input buffer
            jumpInputBuffer.Clear();
            groundContactBuffer.Clear();
            canJump = false;

            //Perform the jump (prior velocity ignored)
            newVelocity.y = Mathf.Sqrt(2f * gravityAcceleration * (jumpHeight - (groundCheckDistance / 2f)));
            attemptingCrouch = false;

            // Start cooldown for ground checks
            groundCheckTimer = groundCheckCooldown;
            isGrounded = false;

            // Sprint out of slide jump
            if (isSliding)
            {
                isSprinting = true;
                isSlideJumping = true;
            }
        }

        #endregion

        #endregion

        #region Crouching / Sliding

        #region Crouching

        if (attemptingCrouch)
        {
            // If the player isn't fully crouched
            if (playerCollider.height > crouchColliderHeight)
            {
                // Reduce height
                float iterations = crouchTransitionTime / Time.fixedDeltaTime;
                float scalePerIteration = (standardHeight - crouchColliderHeight) / iterations;
                playerCollider.height -= scalePerIteration;

                // Lower centre point
                Vector3 centrePoint = Vector3.zero;
                centrePoint.y = playerCollider.center.y - (scalePerIteration / 2f);
                playerCollider.center = centrePoint;

                // Lower head point
                Vector3 newHeadPoint = new Vector3(0f, headPointTransform.localPosition.y - scalePerIteration, 0f);
                headPointTransform.localPosition = newHeadPoint;
            }

            // If the player is below the minimum height
            if (playerCollider.height <= crouchColliderHeight)
            {
                // Set to the exact crouch height
                playerCollider.height = crouchColliderHeight;

                // Set centre point of collider
                Vector3 centrePoint = Vector3.zero;
                centrePoint.y = crouchColliderHeight / 2f;
                playerCollider.center = centrePoint;

                // Set head point
                Vector3 crouchHeadPoint = new Vector3(0f, headPointHeight - (standardHeight - crouchColliderHeight), 0f);
                headPointTransform.localPosition = crouchHeadPoint;

                isCrouching = true;
            }
        }
        else
        {
            // If the player hasn't fully stood up
            if (playerCollider.height < standardHeight)
            {
                // Increase height
                float iterations = crouchTransitionTime / Time.fixedDeltaTime;
                float scalePerIteration = (standardHeight - crouchColliderHeight) / iterations;

                // If there is room to stand up
                Vector3 headRaycastOrigin = new Vector3(playerTransform.position.x, playerTransform.position.y + (playerCollider.height - 0.5f), playerTransform.position.z);
                if (Physics.Raycast(headRaycastOrigin, Vector3.up, 0.49f + scalePerIteration, groundLayerMask) == false)
                {
                    playerCollider.height += scalePerIteration;

                    // Raise centre point
                    Vector3 centrePoint = Vector3.zero;
                    centrePoint.y = playerCollider.center.y + (scalePerIteration / 2f);
                    playerCollider.center = centrePoint;

                    // Raise head point
                    Vector3 newHeadPoint = new Vector3(0f, headPointTransform.localPosition.y + scalePerIteration, 0f);
                    headPointTransform.localPosition = newHeadPoint;
                }
            }

            // If the player has stood up too far
            if (playerCollider.height >= standardHeight)
            {
                // Set to the exact standing height
                playerCollider.height = standardHeight;

                // Set centre point of collider
                Vector3 centrePoint = Vector3.zero;
                centrePoint.y = standardHeight / 2f;
                playerCollider.center = centrePoint;

                // Set head point
                Vector3 standingHeadPoint = new Vector3(0f, headPointHeight, 0f);
                headPointTransform.localPosition = standingHeadPoint;

                isCrouching = false;
            }
        }

        #endregion

        #region Sliding

        isSlopeSliding = false;

        // If we are meeting all conditions for sliding
        if ((isCrouching || attemptingCrouch) && isSliding && walkVelocity.magnitude > maxCurrentVelocityMagnitude)
        {
            // If we just started the slide
            if (startedSliding)
            {
                startedSliding = false;

                // Apply initial velocity boost
                playerRigidbody.AddForce(walkVelocity * slideInitialVelocityCoefficient, ForceMode.VelocityChange);
            }

            #region Slope Sliding

            RaycastHit hit;
            if (Physics.Raycast((playerTransform.position + Vector3.up), Vector3.down, out hit, magnetismRange, groundLayerMask))
            {
                // The normal vector of the face directly below the player
                Vector3 faceNormal = hit.normal;

                // Represent the slope from the side (2 dimensional projection)
                // Gradient is the inverse of the normal, so x and y are swapped
                Vector2 slopeGradientSample = Vector2.zero;
                slopeGradientSample.y = Mathf.Sqrt((faceNormal.x * faceNormal.x) + (faceNormal.z * faceNormal.z));
                slopeGradientSample.x = faceNormal.y;
                slopeGradientSample.Normalize();

                // Gradient = Rise / Run
                float slopeGradient = slopeGradientSample.y / slopeGradientSample.x;
                float minGradient = 1f / Mathf.Tan(Mathf.Deg2Rad * (90f - slideSlopeMinAngle));
                float maxGradient = 1f / Mathf.Tan(Mathf.Deg2Rad * (90f - slideSlopeMaxAngle));

                // If the slope has a valid gradient angle
                if (slopeGradient >= minGradient && slopeGradient <= maxGradient)
                {
                    // Find the perpendicular vector to the normal and the vertical axis (the axis at which the ramp is viewed orthographically)
                    Vector3 slopeAxis = Vector3.Cross(faceNormal, Vector3.up).normalized; 

                    // Find the perpendicular vector to the face normal and the slopeAxis (the direction of the slope, facing downward)
                    Vector3 globalSlopeDirection = Vector3.Cross(faceNormal, slopeAxis).normalized;

                    // If the player is sliding toward the down vector of the ramp
                    if (Vector3.Dot(globalSlopeDirection, currentVelocity) > 0f)
                    {
                        isSlopeSliding = true;

                        // Determine magnitude of slide force
                        Vector3 rampSlideForce = globalSlopeDirection * slideSlopeForceCoefficient;

                        // Apply sliding force in direction of ramp
                        playerRigidbody.AddForce(rampSlideForce, ForceMode.Force);
                    }
                }
            }

            #endregion
        }
        else
        {
            // The slide has ended
            isSliding = false;
            startedSliding = false;
        }

        #endregion

        #endregion

        #region Grapple Hookshot

        // Apply continuous grapple velocity
        if (isGrappling)
        {
            Vector3 cameraToPoint = (grapplePoint.position + (Vector3.up * grapplePointVerticalOffset)) - cameraHolderTransform.position;
            newVelocity = cameraToPoint.normalized * grappleVelocity;

            // If the player has reached the end of their grapple
            if (cameraHolderTransform.position.y > grapplePoint.position.y + grapplePointVerticalOffset)
            {
                // Disable the rope effect


                isGrappling = false;
                grapplePoint = null;

                newVelocity = Vector3.zero;
                grappleAutoMantle = true;
            }
        }
        
        waitingToAttemptGrapple = false;

        #endregion

        #region Ledge Mantle

        bool headCleared = false;
        bool footholdValid = false;
        bool mantleCleared = false;

        // If the ledge ahead of the player's head is clear
        if (Physics.Raycast(playerTransform.position + (Vector3.up * mantleHeadCheckHeight), playerTransform.forward, 2f, groundLayerMask) == false)
        {
            headCleared = true;
        }

        // If the cliff face ahead of the player's foothold is present
        if (Physics.Raycast(playerTransform.position + (Vector3.up * mantleFootholdHeight), playerTransform.forward, 2f, groundLayerMask))
        {
            footholdValid = true;
        }

        // If the player has cleared the ledge they were mantling on
        if (Physics.Raycast(playerTransform.position + (Vector3.up * mantleClearedHeight), playerTransform.forward, 2f, groundLayerMask) == false)
        {
            if (isMantling)
            {
                mantleCleared = true;
            }
        }

        // If conditions are met for ledge mantle or we are currently mantling or auto-mantle has been enabled
        if ((headCleared && footholdValid && isGrounded == false && (inputVector.y > 0f || grappleAutoMantle) && (canMantle || grappleAutoMantle)) || isMantling)
        {
            newVelocity = Vector3.zero;
            newVelocity = (playerTransform.forward * mantleVelocity.z) + (playerTransform.right * mantleVelocity.x);
            newVelocity.y = mantleVelocity.y;
            isMantling = true;

            // If we are mantling and the conditions fail
            if (headCleared == false || (footholdValid == false && mantleCleared) || isGrounded)
            {
                isMantling = false;
                canMantle = false;
            }
        }
        else
        {
            // If we were mantling and just stopped
            if (isMantling)
            {
                canMantle = false;
            }

            isMantling = false;
        }

        #endregion

        #region Surface Magnetism

        // If the conditions are met for the player to be attracted to the face normal below
        if (isGrounded && canJump && (currentVelocity.magnitude > magnetismMinVelocityThreshold || inputVector == Vector2.zero))
        {
            RaycastHit hit;

            // If we are within magnetism range
            if (Physics.Raycast((playerTransform.position + Vector3.up), Vector3.down, out hit, magnetismRange, groundLayerMask))
            {
                // Form magnetism force vector from the inverse surface normal directly below the player, scaled by a strength value and the player's current velocity magnitude
                Vector3 magnetismForce = -hit.normal * magnetismStrengthCoefficient * hit.distance * (currentVelocity.magnitude * magnetismVelocityScaling);

                // Apply the magnetism force
                playerRigidbody.AddForce(magnetismForce, ForceMode.Impulse);
            }
        }

        #endregion

        // Adjust modified y-velocity if we are currently capable of jumping/falling
        if (isMantling == false && isGrappling == false)
        {
            CheckVelocity();
            newVelocity.x = currentVelocity.x;
            newVelocity.z = currentVelocity.z;
        }

        // Apply modified velocity
        playerRigidbody.velocity = newVelocity;
    }

    // Read current velocity values from Rigidbody
    void CheckVelocity()
    {
        currentVelocity = playerRigidbody.velocity;

        walkVelocity = currentVelocity;
        walkVelocity.y = 0f;

        // The max speed of the player, whether walking, sprinting or crouch walking
        maxCurrentVelocityMagnitude = baseMoveSpeed * (isSprinting ? sprintMoveSpeedCoefficient : 1f) * ((isCrouching || attemptingCrouch) ? crouchMoveSpeedCoefficient : 1f);
        maxCurrentVelocity = walkVelocity.normalized * maxCurrentVelocityMagnitude;
    }

    //When the player first touches the ground
    void GroundDetectionEnter()
    {
        // Stop sprinting when landing after a slide jump, used to discourage crouch-jumping speed boost abuse
        if (isSlideJumping)
        {
            isSprinting = false;
        }

        canJump = true;
        canMantle = true;
        canGrapple = true;
        isSlideJumping = false;
        grappleAutoMantle = false;
    }

    //When the player first leaves the ground
    void GroundDetectionExit()
    {
        
    }
}
