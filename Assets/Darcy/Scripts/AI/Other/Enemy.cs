using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Author: Darcy Matheson
// Purpose: Base class for all enemies, controls taking damage, spawning, animations, etc

public class Enemy : MonoBehaviour
{
    #region Variables

    #region Internal

    [HideInInspector]
    public int currentHealth;

    [HideInInspector]
    public bool isBurrowing;

    #endregion

    #region Parameters
    [Header("Generic Enemy Setup")]

    [Tooltip("Displayed on the enemy's health bar.")]
    public string enemyName;

    [Tooltip("The starting health of the enemy at base difficulty."), Range(1, 5000)]
    public int baseMaxHealth;
    [HideInInspector]
    public int maxHealth;

    [Tooltip("The starting damage of the enemy at base difficulty."), Range(1, 100)]
    public int baseDamage;
    protected int scaledDamage;

    [Tooltip("The traversal distance at which the enemy despawns."), Range(50, 1000)]
    public int despawnDistance;

    [Tooltip("The internal spawn cost of creating this enemy."), Range(1, 10)]
    public int spawnCost;

    [Tooltip("The rate at which the damage of this enemy increases with the game difficulty (lower = faster)."), Range(0.1f, 1f)]
    public float damageScaler;

    [Tooltip("The rate at which the health of this enemy increases with the game difficulty (lower = faster)."), Range(0.1f, 1f)]
    public float healthScaler;

    [Tooltip("The base amount of score this enemy awards when killed (scales with difficulty)."), Range(1, 100)]
    public int baseScoreValue;

    [Tooltip("The base amount of fibre this enemy awards when killed (scales with difficulty)."), Range(1, 10)]
    public int baseFibreValue;

    public LayerMask environmentLayers;
    public LayerMask playerLayer;
    #endregion

    #region Components

    public GameObject damageNumberPrefab;
    protected Transform damageNumberParentTransform;

    protected Transform playerTransform;
    protected Transform mainCameraTransform;
    protected Transform enemyTransform;

    protected CapsuleCollider enemyCollider;

    protected NavMeshAgent enemyAgent;

    protected PlayerStats playerStats;
    #endregion

    #endregion

    protected void Configure()
    {
        // Auto-assignments
        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        mainCameraTransform = Camera.main.GetComponent<Transform>();
        enemyTransform = GetComponent<Transform>();
        enemyAgent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponent<CapsuleCollider>();
        damageNumberParentTransform = GameObject.FindGameObjectWithTag("DamageNumberParent").GetComponent<Transform>();

        // Set health and damage
        maxHealth = baseMaxHealth + Mathf.FloorToInt(baseMaxHealth * (playerStats.difficultyLevel * healthScaler));
        currentHealth = maxHealth;
        scaledDamage = baseDamage + Mathf.FloorToInt(baseDamage * (playerStats.difficultyLevel * damageScaler));

        // Default values
        isBurrowing = false;
    }

    // Responsible for removing health from the enemy and spawning damage numbers when damage is dealt
    public virtual void TakeDamage(int damage, int expectedDamage, Vector3 position, bool ignoreArmour)
    {
        int startingHealth = currentHealth;

        // Determine the damage the enemy will take, cut it off if it brings them below 0
        int damageTaken = damage;
        damageTaken = Mathf.Clamp(damageTaken, 0, currentHealth);

        // Take damage and reduce health
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

    protected float CalculatePathLength(NavMeshPath path)
    {
        float length = 0f;

        // If the path exists and it has multiple nodes
        if ((path.status != NavMeshPathStatus.PathInvalid) && (path.corners.Length > 1))
        {
            // For each node in the path, do pythagorus theorum to find distance between one node and the next
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                // Add distance between this node and the next one
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
        }

        return length;
    }

    public virtual void Die()
    {
        // Stop moving
        enemyAgent.isStopped = true;
        enemyAgent.ResetPath();

        WaveManager.eliminatedWaveEnemies += 1;
        enemyCollider.enabled = false;
        enemyAgent.enabled = false;

        playerStats.AddScore(baseScoreValue);
        playerStats.AddFibre(baseFibreValue);

        Destroy(gameObject, 0.5f);
    }
}
