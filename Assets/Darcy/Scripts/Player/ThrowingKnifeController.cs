using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Author: Darcy Matheson
// Purpose: Controls the movement of the knife ultimate, as well as other aspects like player healing on kill

public class ThrowingKnifeController : MonoBehaviour
{
    public float moveSpeed;
    public float spinSpeed;

    public int maxDamage;
    private int damageDealt;
    public int maxBounces;
    private int bounceCount;
    public float throwDuration;
    private float airtimeTimer;

    private float bounceDuration;
    private float bounceTimer;

    public float throwDetectionRadius;
    public float bounceDetectionRadius;

    public float minimumThrowSimilarity;

    public int healthPercentRestorePerKill;

    public LayerMask wallLayers;
    public LayerMask enemyLayers;

    public Transform holderTransform;
    public Transform pivotTransform;
    public GameObject knifeModel;
    private Transform currentTargetTransform;
    private float halfTargetHeight;
    private Enemy currentTargetEnemy;

    public Vector3 stowedPosition;
    private bool isStowing;
    private bool isDrawing;
    private bool isStowed;
    private bool isFullyDrawn;
    public float switchDuration;
    private float switchTimer;    
    
    public float lingerDuration;
    private float lingerTimer;

    public TrailRenderer bladeTrail;
    public TrailRenderer handleTrail;
    public Transform parentHolsterTransform;

    private Transform mainCameraTransform;
    private Transform preferredThrowPoint;

    private Vector3 lastBouncePosition;

    private PlayerStats playerStats;

    public static bool isAirborn { get; private set; }

    private void Start()
    {
        isAirborn = false;
        damageDealt = 0;
        bounceCount = 0;

        mainCameraTransform = Camera.main.transform;

        isStowing = false;
        isDrawing = false;
        isFullyDrawn = false;
        isStowed = true;
        holderTransform.localPosition = stowedPosition;
        knifeModel.SetActive(false);

        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
    }

    void Update()
    {
        #region Input 

        if (isAirborn == false && lingerTimer <= 0f )
        {
            if (Input.GetKey(KeyCode.Q) && playerStats.GetSuperStatus() && PlayerStats.gamePaused == false)
            {
                if (isDrawing == false && isFullyDrawn == false)
                {
                    DrawKnife();
                }
            }
            else if (isFullyDrawn)
            {
                if (isFullyDrawn && preferredThrowPoint != null)
                {
                    Throw(preferredThrowPoint);
                }
                else if (isAirborn == false && isStowing == false && isStowed == false)
                {
                    StowKnife();
                }
            }
        }

        #endregion

        #region Stow / Draw Animation

        if (isFullyDrawn == false && isStowed == false)
        {
            // Animate the weapon stow/draw
            if (isStowing)
            {
                switchTimer -= Time.deltaTime;
                
                float progress = 1f - WeaponSwitchCurve(Mathf.Clamp01(switchTimer / switchDuration));
                Vector3 newPosition = Vector3.Lerp(Vector3.zero, stowedPosition, progress);
                holderTransform.localPosition = newPosition;

                // If the animation is completed
                if (progress >= 1f)
                {
                    isStowed = true;
                    isStowing = false;

                    knifeModel.SetActive(false);
                }
            }
            else if (isDrawing)
            {
                knifeModel.SetActive(true);
                switchTimer -= Time.deltaTime;

                float progress = 1f - WeaponSwitchCurve(Mathf.Clamp01(switchTimer / switchDuration));
                Vector3 newPosition = Vector3.Lerp(stowedPosition, Vector3.zero, progress);
                holderTransform.localPosition = newPosition;

                // If the animation is completed
                if (progress >= 1f)
                {
                    isFullyDrawn = true;
                    isDrawing = false;
                }
            }

        }

        #endregion

        #region Linger After Bounce End

        // The period in which the knife stays at the last target to let the trails catch up, before coming back to the player
        if (lingerTimer > 0f)
        {
            lingerTimer -= Time.deltaTime;
            knifeModel.SetActive(false);

            if (lingerTimer <= 0f)
            {
                // Turn trail effects off
                bladeTrail.emitting = false;
                handleTrail.emitting = false;

                // Reset the knife back to the player's hand in a stowed position
                isStowing = false;
                isDrawing = false;
                isFullyDrawn = false;
                isStowed = true;

                holderTransform.parent = parentHolsterTransform;
                holderTransform.localPosition = Vector3.zero;
                holderTransform.localRotation = Quaternion.identity;
                pivotTransform.localPosition = Vector3.zero;
                pivotTransform.localRotation = Quaternion.identity;
            }
        }

        #endregion
    }

    // Update is called once per frame
    void LateUpdate()
    {
        #region Enemy Lock-On (Pre-Bounce)

        if (isFullyDrawn)
        {
            // Aim at enemies
            Collider[] enemiesInRange = Physics.OverlapSphere(mainCameraTransform.position, throwDetectionRadius, enemyLayers);
            
            Transform closestPoint = null;
            float closestSimilarity = -1f;
            foreach (Collider col in enemiesInRange)
            {
                float enemyCentreHeight = col.gameObject.GetComponent<NavMeshAgent>().height / 2f;
                Vector3 enemyPosition = col.transform.position + (Vector3.up * halfTargetHeight);

                // Calculate vector from camera to enemy
                Vector3 directionToPoint = (enemyPosition - mainCameraTransform.position);

                // If there is a clear line of sight to the enemy
                if (Physics.Linecast(mainCameraTransform.position, enemyPosition, wallLayers) == false)
                {
                    float similarity = Vector3.Dot(directionToPoint.normalized, mainCameraTransform.forward);

                    // If this is the new closest enemy to the player's look vector
                    if (similarity > closestSimilarity)
                    {
                        halfTargetHeight = enemyCentreHeight;
                        closestPoint = col.transform;
                        closestSimilarity = similarity;
                    }
                }
            }

            // If there is a viable enemy target
            if (closestSimilarity >= minimumThrowSimilarity)
            {
                // Highlight the closest enemy with UI effect
                preferredThrowPoint = closestPoint;
            }
            else
            {
                // Stop highlighting the closest enemy
                preferredThrowPoint = null;
            }
        }

        #endregion

        #region Bounces

        if (isAirborn)
        {
            // Spin animation
            pivotTransform.localRotation = Quaternion.Euler(spinSpeed * 360f * (throwDuration - airtimeTimer), 0f, 0f);

            airtimeTimer -= Time.deltaTime;

            if (currentTargetTransform == null || currentTargetEnemy == null || currentTargetEnemy.currentHealth <= 0f)
            {
                // Find a new target
                Collider[] enemiesWithinRange = Physics.OverlapSphere(holderTransform.position, bounceDetectionRadius, enemyLayers);

                if (enemiesWithinRange.Length > 0)
                {
                    Transform closestTransform = null;
                    float closestDistance = Mathf.Infinity;

                    // For each of the enemies within range
                    for (int i = 0; i < enemiesWithinRange.Length; i++)
                    {
                        float distanceToEnemy = Vector3.Distance(holderTransform.position, enemiesWithinRange[i].transform.position);

                        // If this is the closest transform so far
                        if (distanceToEnemy < closestDistance)
                        {
                            halfTargetHeight = enemiesWithinRange[i].GetComponent<NavMeshAgent>().height / 2f;
                            Vector3 enemyCentre = enemiesWithinRange[i].transform.position + (Vector3.up * halfTargetHeight);

                            // If the line of sight from the knife to the enemy is clear
                            if (Physics.Linecast(holderTransform.position, enemyCentre, wallLayers) == false)
                            {
                                // If the enemy has health
                                if (enemiesWithinRange[i].GetComponent<Enemy>().currentHealth > 0)
                                {
                                    // The most valid transform so far
                                    closestTransform = enemiesWithinRange[i].transform;
                                    closestDistance = distanceToEnemy;
                                }
                            }
                        }
                    }

                    // If there is a valid contender, assign it to the knife
                    currentTargetTransform = closestTransform;
                    if (currentTargetTransform != null)
                    {
                        currentTargetEnemy = currentTargetTransform.GetComponent<Enemy>();

                        lastBouncePosition = holderTransform.position;
                        bounceDuration = closestDistance;
                        bounceTimer = closestDistance;
                    }
                    else
                    {
                        StopBouncing();
                    }
                }
                else
                {
                    StopBouncing();
                }
            }

            if (isAirborn && currentTargetTransform != null && currentTargetEnemy != null && currentTargetEnemy.currentHealth > 0f)
            {
                // Move towards target
                float progress = BounceSmoothingCurve(Mathf.Clamp01(bounceTimer / bounceDuration));
                Vector3 knifePosition = Vector3.Lerp(lastBouncePosition, currentTargetTransform.position + (Vector3.up * halfTargetHeight), 1f - progress);
                holderTransform.position = knifePosition;
                bounceTimer -= Time.deltaTime * moveSpeed;

                // Rotate towards target
                if (Vector3.Distance(holderTransform.position, currentTargetTransform.position) > 1f)
                {
                    holderTransform.localRotation = Quaternion.LookRotation(currentTargetTransform.position - holderTransform.position);
                }

                // If target is reached
                if (progress <= 0f)
                {
                    // Execute target
                    damageDealt += currentTargetEnemy.currentHealth;
                    bounceCount += 1;
                    int damageToDeal = currentTargetEnemy.currentHealth;
                    currentTargetEnemy.TakeDamage(damageToDeal, damageToDeal, currentTargetTransform.position + (Vector3.up * halfTargetHeight), true);
                    playerStats.currentHealth += Mathf.CeilToInt((playerStats.maxHealth / 100f) * healthPercentRestorePerKill);

                    currentTargetTransform = null;
                    currentTargetEnemy = null;
                }
            }

            // Compare time, bounce count and damage
            // If any conditions are met, end throw
            if (airtimeTimer <= 0f || bounceCount >= maxBounces || damageDealt >= maxDamage)
            {
                StopBouncing();
            }
        }

        #endregion
    }

    public void DrawKnife()
    {
        if (switchTimer > 0f)
        {
            // If a switch animation is already in progress
            switchTimer = switchDuration - switchTimer;
        }
        else
        {
            // Starting a switch from a resting position
            switchTimer = switchDuration;
        }

        isDrawing = true;
        isStowing = false;

        isFullyDrawn = false;
        isStowed = false;
    }

    public void StowKnife()
    {
        if (switchTimer > 0f)
        {
            // If a switch animation is already in progress
            switchTimer = switchDuration - switchTimer;
        }
        else
        {
            // Starting a switch from a resting position
            switchTimer = switchDuration;
        }

        isDrawing = false;
        isStowing = true;

        isFullyDrawn = false;
        isStowed = false;
    }

    public void Throw(Transform firstTarget)
    {
        // Only allow one throw at a time
        if (isAirborn)
        {
            return;
        }

        holderTransform.SetParent(null);
        airtimeTimer = throwDuration;
        damageDealt = 0;
        bounceCount = 0;

        // Switch from held to airborn
        isAirborn = true;

        // Assign the first target
        currentTargetTransform = firstTarget;
        currentTargetEnemy = currentTargetTransform.GetComponent<Enemy>();
        lastBouncePosition = holderTransform.position;

        bounceTimer = Vector3.Distance(lastBouncePosition, firstTarget.position);
        bounceDuration = bounceTimer;

        // Turn trail effects on
        bladeTrail.emitting = true;
        handleTrail.emitting = true;

        playerStats.ConsumeSuper();
        PlayerStats.allowUltimateGeneration = false;
    }

    public void StopBouncing()
    {
        if (isAirborn == false)
        {
            return;
        }

        airtimeTimer = 0f;
        isAirborn = false;
        damageDealt = 0;
        bounceCount = 0;

        currentTargetTransform = null;
        currentTargetEnemy = null;

        lingerTimer = lingerDuration;

        isStowing = false;
        isDrawing = false;
        isFullyDrawn = false;
        isStowed = false;

        PlayerStats.allowUltimateGeneration = true;
    }

    public float BounceSmoothingCurve(float time)
    {
        float result = 0f;

        result = (Mathf.Cos(Mathf.PI * (time + 1f)) + 1f) / 2f;
        return result;
    }

    public float WeaponSwitchCurve(float time)
    {
        float result = 0f;

        result = (Mathf.Cos(Mathf.PI * (time + 1f)) + 1f) / 2f;
        return result;
    }
}
