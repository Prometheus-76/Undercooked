using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Author: Darcy Matheson
// Purpose: Strawberry specific enemy class that controls unique enemy behaviour such as shooting and locating the player

public class StrawberryEnemy : Enemy
{
    #region Variables

    #region Internal

    private float shotTimer;
    private int shotsFired;
    private float burstTimer;
    private bool isAttacking;

    #endregion

    #region Parameters
    [Header("Attack Parameters")]

    [Tooltip("The time (in seconds) between firing each shot."), Range(0.5f, 3f)]
    public float timeBetweenShots;

    [Tooltip("The number of seeds fired sequentially in each burst."), Range(1, 5)]
    public int shotsPerBurst;

    [Tooltip("The time (in seconds) between firing a burst of seeds."), Range(0.1f, 5f)]
    public float timeBetweenBursts;

    [Tooltip("The distance the enemy starts attacking the player."), Range(10f, 100f)]
    public float attackRange;
    #endregion

    #region Components

    public GameObject seedPrefab;

    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        base.Configure();
        isAttacking = false;

        burstTimer = 0f;
        shotTimer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth > 0 && isBurrowing == false)
        {
            // Calculate absolute and walking distances between enemy and player
            float absoluteDistanceToPlayer = Vector3.Distance(playerTransform.position, enemyTransform.position);

            #region Behaviour Tree

            if (isAttacking)
            {
                #region Attacking

                enemyAgent.ResetPath();
                enemyAgent.isStopped = true;

                burstTimer -= Time.deltaTime;

                // Continue attacking the player
                if (isAttacking && burstTimer < 0f && shotsFired < shotsPerBurst)
                {
                    shotTimer -= Time.deltaTime;

                    // If the next shot is ready
                    if (shotTimer < 0f)
                    {
                        shotsFired += 1;

                        // Fire shot
                        HomingSeed seed = Instantiate(seedPrefab, enemyTransform.position + (Vector3.up * (enemyAgent.height / 2f)), Quaternion.identity).GetComponent<HomingSeed>();
                        seed.SetupSeed(scaledDamage);

                        // Setup next action
                        if (shotsFired < shotsPerBurst)
                        {
                            // Setup next shot
                            shotTimer = timeBetweenShots;
                        }
                        else
                        {
                            // Setup next burst
                            isAttacking = false;
                            burstTimer = timeBetweenBursts;
                            shotTimer = 0f;
                            shotsFired = 0;
                        }
                    }
                }

                #endregion

                #region Attack Burst Interrupted

                // If the path to the player is not a straight line
                if (Physics.Linecast(enemyTransform.position + (Vector3.up * (enemyAgent.height - 0.2f)), mainCameraTransform.position, environmentLayers))
                {
                    isAttacking = false;
                }

                #endregion
            }
            else if (absoluteDistanceToPlayer <= attackRange && Physics.Linecast(enemyTransform.position + (Vector3.up * (enemyAgent.height - 0.2f)), mainCameraTransform.position, environmentLayers) == false)
            {
                #region Start Attack

                // Lock enemy into attacking mode until burst completes
                isAttacking = true;

                #endregion
            }
            else
            {
                #region Navigation

                // Navigate to the player
                enemyAgent.SetDestination(playerTransform.position);
                enemyAgent.isStopped = false;
                isAttacking = false;

                #endregion
            }

            #endregion
        }
    }
}
