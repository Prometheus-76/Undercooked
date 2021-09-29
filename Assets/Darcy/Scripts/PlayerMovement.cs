using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Variables

    #region Walk / Run

    [Header("Walk / Run")]
    
    //The player always moves exactly in the input direction they are using, relative to their forward vector
    //Acceleration and friction are artifical so the movement direction can be more precise and sliding doesn't occur around corners

    [Tooltip("The option for whether sprint is toggled or held on/off")]
    public bool toggleSprint = true;
    private bool isSprinting = false;

    [Range(0f, 25f)]
    [Tooltip("The player's fastest walking speed")]
    public float baseMoveSpeed = 10f;

    [Range(1f, 100f)]
    [Tooltip("The player's acceleration to walking/sprinting speed")]
    public float baseAcceleration = 10f;

    [Range(1f, 3f)]
    [Tooltip("How much faster sprinting is than walking")]
    public float sprintMultiplier = 1.8f;

    [Range(1f, 5f)]
    [Tooltip("How much faster you accelerate when changing direction")]
    public float turnaroundMultiplier = 2f;

    //WASD represented as a normalised Vector2
    private Vector2 inputVector;

    #endregion

    #region Deceleration

    [Header("Deceleration")]

    //Used for slowing the player down and applying fake air resistance

    [Range(1.01f, 10f)]
    [Tooltip("The force which brings the player to a stop when they are on the ground")]
    public float groundResistance = 2f;

    [Range(1.01f, 1.05f)]
    [Tooltip("The force which brings the player to a stop when they are in the air")]
    public float airResistance = 2f;

    [Range(0f, 0.1f)]
    [Tooltip("The slowest that the player is allowed to walk (used by resistance to cut to 0 velocity)")]
    public float minimumVelocity = 0.05f;

    #endregion

    #region Gravity

    [Header("Gravity")]
    
    //Gravity is applied artificially to give more fluent interaction with the custom physics already in use
    
    [Range(0f, 100f)]
    [Tooltip("The player's downward acceleration due to gravity")]
    public float gravityStrength = 30f;

    [Tooltip("Whether gravity should be applied to the player")]
    public bool applyGravity = true;

    #endregion

    #region Jumping / Landing

    [Header("Jumping / Landing")]

    //Jump is handled as an instantaneous increase to the player's vertical velocity
    //Landing checks are done with 4 raycasts out of the player's feet 

    [Range(0f, 10f)]
    [Tooltip("How high the player can jump in world units (approximately)")]
    public float jumpHeight = 0f;

    [Tooltip("Whether the player is currently considered grounded")]
    public bool isGrounded = true;

    [Range(0f, 0.5f)]
    [Tooltip("How far below the player is checked to see if they are on the ground")]
    public float groundCheckRange = 0f;

    [Tooltip("Object layers that reset grounding on contact")]
    public LayerMask groundLayers;

    private int groundContactCount = 0;

    [Range(1, 50)]
    [Tooltip("How many FixedUpdate frames the buffer is (duration before landing)")]
    public int jumpBufferDepth = 0;
    private bool jumpPressed = false;
    private Queue<bool> jumpQueue;

    #endregion

    #region Grappling Hook

    [Header("Grappling Hook")]

    public float hookRange = 20f;
    public float additionalHeight = 2f;
    public float horizontalMultiplier = 2f;
    public float minimumGrappleHeight = 4f;
    private bool waitingForHook = false;

    #endregion

    #endregion

    #region Components

    [Header("Components")]
    
    public Rigidbody playerRigidbody;
    public Transform playerTransform;
    public CapsuleCollider playerCollider;

    public Camera mainCamera;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        jumpQueue = new Queue<bool>();
    }

    // Update is called once per frame
    void Update()
    {

        #region Sprint Multiplier On/Off

        if (Input.GetKeyDown(KeyCode.LeftShift)) //When shift is pressed
        {
            if (toggleSprint)
            {
                isSprinting = !isSprinting;
            }
            else
            {
                isSprinting = true;
            }
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift)) //When shift is released
        {
            if (toggleSprint == false)
            {
                isSprinting = false;
            }
        }

        #endregion

        #region Input Handling

        //Get the keyboard WASD input and normalise it
        inputVector.x = Input.GetAxisRaw("Horizontal");
        inputVector.y = Input.GetAxisRaw("Vertical");
        inputVector.Normalize();

        //Queue a jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }

        //Wait to hook
        if (Input.GetMouseButtonDown(1))
        {
            waitingForHook = true;
        }

        #endregion
    }

    // FixedUpdate is called once per physics iteration
    void FixedUpdate()
    {
        Vector3 velocity = playerRigidbody.velocity;
        Vector2 walkVelocity = new Vector2(playerRigidbody.velocity.x, playerRigidbody.velocity.z);

        //The fastest the player should be able to move under the current circumstances
        float maxBaseSpeed = (baseMoveSpeed * (isSprinting ? sprintMultiplier : 1f) * Mathf.Sqrt(isGrounded ? groundResistance : airResistance));

        #region Grounded Checks

        groundContactCount = 0;
        Vector3[] rayOrigins = new Vector3[4];

        #region Configure Raycast Origin Points

        rayOrigins[0] = playerTransform.forward * playerCollider.radius;
        rayOrigins[1] = -playerTransform.forward * playerCollider.radius;
        rayOrigins[2] = -playerTransform.right * playerCollider.radius;
        rayOrigins[3] = playerTransform.right * playerCollider.radius;

        for (int i = 0; i < rayOrigins.Length; i++)
        {
            rayOrigins[i].y = playerCollider.radius;
            rayOrigins[i] += playerTransform.position;
        }

        #endregion

        //Cast each of the rays
        for (int i = 0; i < rayOrigins.Length; i++)
        {
            //If the ground intersected with this ray
            if (Physics.Raycast(rayOrigins[i], Vector3.down, 0.5f + groundCheckRange, groundLayers))
            {
                groundContactCount++;
                //Debug.DrawRay(rayOrigins[i], Vector3.down, Color.green);
            }
        }

        isGrounded = (groundContactCount > 0);

        #endregion

        #region WASD Movement

        //Which way the player should move in world space
        Vector3 targetVector = ((playerTransform.forward * inputVector.y) + (playerTransform.right * inputVector.x)).normalized;

        //If the player is trying to move with WASD
        if (inputVector != Vector2.zero)
        {
            Vector3 velocityDelta = targetVector * (baseAcceleration / 100f);

            //Increase velocity delta for this frame if we're changing direction
            velocity += velocityDelta;
        }

        //Stop sprint multiplier if we're not moving
        if (targetVector == Vector3.zero)
        {
            isSprinting = false;
        }

        #endregion

        #region Artificial Gravity

        //Only apply gravity when the player is off the ground
        applyGravity = !isGrounded;

        //Accumulate gravity acceleration from last iteration
        velocity.y = playerRigidbody.velocity.y;
        
        if (applyGravity)
        {
            velocity.y -= gravityStrength * Time.fixedDeltaTime;
        }

        #endregion

        #region Walking/Sprinting Deceleration

        //If the player is moving too fast or isn't applying any input
        if (inputVector == Vector2.zero || walkVelocity.magnitude > maxBaseSpeed)
        {
            Vector3 newVelocity = new Vector3(walkVelocity.x, 0f, walkVelocity.y);
            
            //Is the player is moving below the minimum speed?
            if (walkVelocity.magnitude > minimumVelocity)
            {
                if (isGrounded)
                {
                    //Slide to a stop on the ground
                    newVelocity.x /= groundResistance;
                    newVelocity.z /= groundResistance;
                }
                else
                {
                    //Slide to a stop in the air
                    newVelocity.x /= airResistance;
                    newVelocity.z /= airResistance;
                }
            }
            else
            {
                //Player is moving slower than the min velocity, stop them
                newVelocity.x = 0f;
                newVelocity.z = 0f;
            }

            velocity.x = newVelocity.x;
            velocity.z = newVelocity.z;
        }

        #endregion

        #region Jumping

        //Record a jump or lack thereof from within the Update method
        jumpQueue.Enqueue(jumpPressed);
        jumpPressed = false;

        //Constrain queue to set size
        if (jumpQueue.Count > jumpBufferDepth)
        {
            jumpQueue.Dequeue();
        }

        //If there is an input in the queue and the player has landed on the ground
        if (jumpQueue.Contains(true) && isGrounded)
        {
            //Empty the input buffer
            jumpQueue.Clear();

            //Perform the jump (prior velocity ignored)
            velocity.y = Mathf.Sqrt(2f * gravityStrength * jumpHeight);
        }

        #endregion

        #region Grappling Hook

        //If there is a hook queued up
        if (waitingForHook)
        {
            waitingForHook = false;

            RaycastHit hit;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit, hookRange, groundLayers))
            {
                Vector3 hookPoint = hit.point;

                //The vertical distance from the player to the hookPoint
                float heightDifference = hookPoint.y - playerTransform.position.y;

                if (heightDifference > minimumGrappleHeight)
                {
                    Vector3 positionDifference = hookPoint - playerTransform.position;
                    positionDifference.y = 0f;
                    positionDifference *= horizontalMultiplier;

                    float verticalForce = Mathf.Sqrt((heightDifference + additionalHeight) * 2f * gravityStrength);
                    Vector3 hookForce = new Vector3(positionDifference.x, verticalForce, positionDifference.z);

                    velocity = hookForce;
                }
            }
        }

        #endregion

        //Apply final combined velocity vectors
        playerRigidbody.velocity = velocity;
    }
}
