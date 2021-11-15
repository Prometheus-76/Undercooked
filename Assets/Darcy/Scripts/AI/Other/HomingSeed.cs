using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Controls the homing projectiles shot by the strawberry enemy

public class HomingSeed : MonoBehaviour
{
    public float steeringSpeed;
    public float movementSpeed;
    public float hitboxRadius;
    public float attackRadius;
    public float homingMinDistance;
    public float maxLifetime;
    public float modelTwistSpeed;

    private float lifetimeTimer;
    private int contactDamage;
    private bool isDestroyed;

    public LayerMask environmentLayers;
    public LayerMask playerLayer;

    private Quaternion targetRotation;
    private Transform seedTransform;
    public Transform modelPivotTransform;
    private Transform playerTransform;
    private CapsuleCollider playerCollider;
    private PlayerStats playerStats;
    public GameObject seedModel;

    // Update is called once per frame
    void Update()
    {
        // If the seed is alive
        if (lifetimeTimer > 0f)
        {
            lifetimeTimer -= Time.deltaTime;

            #region Homing Towards Player

            if ((seedTransform.position - playerTransform.position).magnitude > homingMinDistance)
            {
                Vector3 playerCentrePosition = playerTransform.position + (Vector3.up * (playerCollider.height / 2f));

                // Slowly rotate towards the player
                targetRotation = Quaternion.LookRotation(seedTransform.position - playerCentrePosition);
                seedTransform.rotation = Quaternion.Lerp(seedTransform.rotation, targetRotation, Time.deltaTime * steeringSpeed);
            }

            modelPivotTransform.Rotate(0f, 0f, Time.deltaTime * modelTwistSpeed, Space.Self);

            // Move in forward direction
            Vector3 potentialPosition = seedTransform.position - (seedTransform.forward * movementSpeed * Time.deltaTime);

            if (Physics.OverlapSphere(potentialPosition, attackRadius, playerLayer).Length <= 0)
            {
                // If the seed won't collide with the player next frame
                if (Physics.OverlapSphere(potentialPosition, hitboxRadius, environmentLayers).Length <= 0)
                {
                    // The seed hasn't collided with anything
                    seedTransform.position = potentialPosition;
                }
                else
                {
                    // The seed has collided with the environment
                    DestroySeed();
                }
            }
            else
            {
                // The seed has collided with the player
                playerStats.TakeDamage(contactDamage);
                DestroySeed();
            }

            #endregion
        }
        else
        {
            DestroySeed();
        }
    }

    // Called after the seed is instantiated
    public void SetupSeed(int damage)
    {
        // Initialisation
        seedTransform = GetComponent<Transform>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        playerCollider = GameObject.FindGameObjectWithTag("Player").GetComponent<CapsuleCollider>();
        lifetimeTimer = maxLifetime;

        // Point at the player to start with
        Vector3 playerCentrePosition = playerTransform.position + (Vector3.up * (playerCollider.height / 2f));
        targetRotation = Quaternion.LookRotation(seedTransform.position - playerCentrePosition);
        seedTransform.rotation = targetRotation;

        // Set damage per hit
        contactDamage = damage;
        isDestroyed = false;
    }

    void DestroySeed()
    {
        if (isDestroyed == false)
        {
            isDestroyed = true;

            lifetimeTimer = 0f;
            seedModel.SetActive(false);
            Destroy(gameObject, 1f);
        }
    }
}
