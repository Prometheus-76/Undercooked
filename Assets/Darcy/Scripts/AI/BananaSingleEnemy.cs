using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Controls the small banana enemies, which follow the player and deal contact damage on a short cooldown

public class BananaSingleEnemy : Enemy
{
    public float attackInterval;
    private float attackTimer;
    public float attackRange;

    // Start is called before the first frame update
    void Start()
    {
        base.Configure();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth > 0 && isBurrowing == false)
        {
            #region Behaviour Tree

            if (attackTimer > 0f)
            {
                #region Attack Cooldown

                // Attack is on cooldown
                attackTimer -= Time.deltaTime;

                enemyAgent.isStopped = true;
                enemyAgent.ResetPath();

                #endregion
            }
            else
            {
                #region Navigate to Player

                // Move towards the player
                enemyAgent.isStopped = false;
                enemyAgent.SetDestination(playerTransform.position);

                #endregion

                #region Start Attacking

                Vector3 directionToPlayer = playerTransform.position - enemyTransform.position;
                if (directionToPlayer.magnitude < attackRange && Physics.Linecast(enemyTransform.position + (Vector3.up * (enemyAgent.height - 0.2f)), mainCameraTransform.position, environmentLayers) == false)
                {
                    // Deal damage to the player
                    attackTimer = attackInterval;
                    playerStats.TakeDamage(scaledDamage);

                    enemyAgent.isStopped = true;
                    enemyAgent.ResetPath();
                }

                #endregion
            }

            #endregion
        }
    }
}
