using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Controls the banana group enemy, splitting into numerous smaller bananas when killed

public class BananaBunchEnemy : Enemy
{
    public int bananaCount;
    public GameObject bananaPrefab;
    private GameObject[] bananas;

    public float attackInterval;
    private float attackTimer;
    public float attackRange;

    private Transform enemyHolderTransform;
    
    // Start is called before the first frame update
    void Start()
    {
        base.Configure();
        enemyHolderTransform = enemyTransform.parent;
        bananas = new GameObject[bananaCount];
        for (int i = 0; i < bananaCount; i++)
        {
            // Create the bananas and disable them
            bananas[i] = Instantiate<GameObject>(bananaPrefab, enemyTransform.position, Quaternion.identity, enemyTransform);
            bananas[i].SetActive(false);
        }
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

    public override void Die()
    {
        // Spawn the children in and change their parent
        for (int i = 0; i < bananas.Length; i++)
        {
            bananas[i].SetActive(true);
            bananas[i].transform.parent = enemyHolderTransform;
        }

        base.Die();
    }
}
