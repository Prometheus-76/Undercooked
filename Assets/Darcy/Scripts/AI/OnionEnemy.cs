using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Author: Darcy Matheson
// Purpose: Onion specific enemy class that controls unique enemy behaviour and overrides taking damage with armour mechanic

public class OnionEnemy : Enemy
{
    private NavMeshPath pathToPlayer;

    public float chargeRange;
    public float chargeSpeed;
    public float chargeDuration;
    private float chargeTimer;
    public float chargeTurningSpeed;
    private Quaternion targetRotation;

    public float stunDuration;
    private float stunTimer;

    public float windupTurningSpeed;
    public float windupDuration;
    private float windupTimer;

    public int outerLayerDefense;
    public int innerLayerDefense;
    public int outerLayerPercentage;
    public int innerLayerPercentage;

    private int currentLayer;

    // Start is called before the first frame update
    void Start()
    {
        base.Configure();
        pathToPlayer = new NavMeshPath();
        currentLayer = 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth > 0 && isBurrowing == false)
        {
            // Calculate absolute and walking distances between enemy and player
            float absoluteDistanceToPlayer = Vector3.Distance(playerTransform.position, enemyTransform.position);
            enemyAgent.CalculatePath(playerTransform.position, pathToPlayer);
            float traversalDistanceToPlayer = CalculatePathLength(pathToPlayer);

            // Assign current armour layer
            currentLayer = RemainingArmourLayers();

            #region Behaviour Tree

            // Determine next action
            if (stunTimer > 0f)
            {
                #region Stunned

                stunTimer -= Time.deltaTime;

                // Stop movement
                enemyAgent.isStopped = true;
                enemyAgent.ResetPath();
                chargeTimer = 0f;
                windupTimer = 0f;

                if (stunTimer < 0f)
                {
                    // Stop the stun
                    stunTimer = 0f;
                }

                #endregion
            }
            else if (chargeTimer > 0f)
            {
                #region Charging

                chargeTimer -= Time.deltaTime;

                if (chargeTimer > 0f)
                {
                    // Charge at the player
                    enemyAgent.isStopped = true;
                    enemyAgent.ResetPath();

                    // Assign the target position to charge towards
                    Vector3 chargeDirection = playerTransform.position - enemyTransform.position;
                    chargeDirection.y = 0f;

                    // Slowly rotate towards the player
                    targetRotation = Quaternion.LookRotation(chargeDirection);
                    enemyTransform.rotation = Quaternion.Lerp(enemyTransform.rotation, targetRotation, Time.deltaTime * chargeTurningSpeed);

                    Vector3 newPosition = enemyTransform.forward * chargeSpeed * Time.deltaTime;
                    newPosition.y = enemyAgent.height / 2f;

                    // The enemy hasn't collided with anything
                    enemyAgent.Move(newPosition);
                }
                else
                {
                    // Charge has timed out
                    chargeTimer = 0f;
                }

                #endregion
            }
            else if (windupTimer > 0f)
            {
                #region Aiming

                windupTimer -= Time.deltaTime;

                // Stop movement while aiming
                enemyAgent.isStopped = true;
                enemyAgent.ResetPath();

                // Assign the target position to aim towards
                Vector3 chargeDirection = playerTransform.position - enemyTransform.position;
                chargeDirection.y = 0f;

                // Slowly rotate towards the player
                targetRotation = Quaternion.LookRotation(chargeDirection);
                enemyTransform.rotation = Quaternion.Lerp(enemyTransform.rotation, targetRotation, Time.deltaTime * windupTurningSpeed);

                if (windupTimer < 0f)
                {
                    // The charge windup has completed, commence the charge
                    windupTimer = 0f;
                    chargeTimer = chargeDuration;

                    // Assign the target position to charge towards
                    chargeDirection = enemyTransform.forward;
                }

                #endregion
            }
            else
            {
                #region Navigation

                // Navigate to the player
                enemyAgent.SetDestination(playerTransform.position);
                enemyAgent.isStopped = false;
                chargeTimer = 0f;
                windupTimer = 0f;
                stunTimer = 0f;

                // Start aiming if within range and line of sight
                if (traversalDistanceToPlayer <= chargeRange)
                {
                    windupTimer = windupDuration;
                }

                #endregion
            }

            #region Interrupt Aim

            // If the path to the player is not a straight line
            if (windupTimer > 0f)
            {
                NavMeshHit node;
                NavMesh.FindClosestEdge(playerTransform.position, out node, NavMesh.AllAreas);

                // If the player is off the navmesh, allow charges without strict projected line of sight
                if (Mathf.Abs(absoluteDistanceToPlayer - traversalDistanceToPlayer) > 0.1f && (node.distance < 0.01f && Mathf.Abs(absoluteDistanceToPlayer - traversalDistanceToPlayer) < 1f) == false)
                {
                    // Interrupt attack charging
                    windupTimer = 0f;
                }
            }

            #endregion

            #region Interrupt Charge

            // If currently charging
            if (chargeTimer > 0f)
            {
                // If the enemy hit the player this frame
                if (Physics.OverlapSphere(enemyTransform.position + (Vector3.up * (enemyAgent.height / 2f)), enemyAgent.radius + 0.1f, playerLayer).Length > 0)
                {
                    // Stop moving
                    enemyAgent.isStopped = true;
                    enemyAgent.ResetPath();
                    chargeTimer = 0f;

                    // Damage the player
                    playerStats.TakeDamage(scaledDamage);
                }

                // Try to find the closest edge on the NavMesh
                NavMeshHit closestNode;
                if (enemyAgent.FindClosestEdge(out closestNode))
                {
                    // If the edge is within range
                    if (closestNode.distance < 0.1f)
                    {
                        // If the enemy is facing the edge
                        Vector3 closestPoint = closestNode.position;
                        Vector3 directionToEdge = (closestPoint - enemyTransform.position).normalized;
                        if (Vector3.Dot(directionToEdge, enemyTransform.forward) > 0.7f)
                        {
                            Stun();
                        }
                    }
                }
            }

            #endregion

            #endregion
        }
    }

    // Responsible for removing health from the enemy and spawning damage numbers when damage is dealt
    public override void TakeDamage(int damage, int expectedDamage, Vector3 position, bool ignoreArmour)
    {
        int startingHealth = currentHealth;

        int damageTaken = 0;
        currentLayer = RemainingArmourLayers();

        // Determine level of armour for this layer
        int layerDefense = 0;
        switch (currentLayer)
        {
            case 2:
                layerDefense = outerLayerDefense;
                break;
            case 1:
                layerDefense = innerLayerDefense;
                break;
            case 0:
                layerDefense = 0;
                break;
        }

        if (ignoreArmour == false)
        {
            // Calculate the damage to deal with the current armour layer
            damageTaken = Mathf.CeilToInt((float)damage * ((100f - (float)layerDefense) / 100f));
        }
        else
        {
            damageTaken = damage;
        }

        // Determine the damage the enemy will take, cut it off if it brings them below 0
        damageTaken = Mathf.Clamp(damageTaken, 0, currentHealth);
        currentHealth -= damageTaken;

        // If the enemy has taken damage
        if (damageTaken > 0)
        {
            // Draw damage numbers
            GameObject damageNumberInstance = Instantiate<GameObject>(damageNumberPrefab, damageNumberParentTransform);
            damageNumberInstance.GetComponent<DamageNumber>().SetupDamageNumber(damageTaken.ToString(), position, (damage == expectedDamage));
        }

        // If the enemy has died
        if (startingHealth > 0 && currentHealth <= 0)
        {
            Die();
        }
    }

    // Stuns the enemy
    void Stun()
    {
        // Stop the enemy, start their timer and stop pathfinding
        stunTimer = stunDuration;
        enemyAgent.ResetPath();
        enemyAgent.isStopped = true;
        enemyAgent.velocity = Vector3.zero;

        // Reset current actions
        chargeTimer = 0f;
        windupTimer = 0f;
    }

    // Returns how many armour layers remain, from 0-2
    int RemainingArmourLayers()
    {
        int result = 0;

        float healthPercentage = ((float)currentHealth / (float)baseMaxHealth) * 100f;
        if (healthPercentage > outerLayerPercentage)
        {
            // Outer layer
            result = 2;
        }
        else if (healthPercentage > innerLayerPercentage)
        {
            // Inner layer
            result = 1;
        }

        return result;
    }
}
