using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Controls the interaction system, player's health, OSP thresholds, ultimate charge, run duration and other related stats

public class PlayerStats : MonoBehaviour
{
    public float screenshakeDamageScaler;
    public int baseHealth;
    [HideInInspector]
    public int maxHealth;
    [HideInInspector]
    public int currentHealth;
    public float healthMultiplierPerLevel;
    public float timeBeforeRegeneration;
    private float immuneRegenerationTimer;
    public float percentRegenerationPerSecond;
    private float regenerationTimer;
    public float fibrePerHealthPoint;

    public int healthUpgradeCostBase;
    public float healthUpgradeCostMultiplierPerLevel;

    public int maxOneShotProtectionThreshold;
    public int minOneShotProtectionThreshold;

    private float ultimateCharge;
    public float passiveUltimateChargeRate;

    [HideInInspector]
    public float currentRunTime { get; private set; }
    private ulong currentScore;
    [HideInInspector]
    public ulong currentFibre;

    public float difficultyWaveFactor;
    public float difficultyTimeFactor;
    public float difficultyLevel { get; private set; }

    public float interactRange;
    public LayerMask interactiveLayer;
    public LayerMask environmentLayers;

    public Movement playerMovement;
    public GunController[] gunControllers;

    [HideInInspector]
    public int playerLevel;

    public static bool gamePaused;

    public static bool allowUltimateGeneration;
    private Transform mainCameraTransform;
    private InteractiveObject currentInteraction;

    private float interactTimer;
    private bool interactPresent;

    // Start is called before the first frame update
    void Start()
    {
        maxHealth = baseHealth;
        currentHealth = maxHealth;

        currentScore = 0;
        currentFibre = 0;

        gamePaused = false;

        playerLevel = 1;

        allowUltimateGeneration = true;

        mainCameraTransform = Camera.main.GetComponent<Transform>();
        interactTimer = 0f;
    }

    // Take damage
    public void TakeDamage(int damage)
    {
        int damageTaken = damage;

        // If the player is above the OSP threshold
        if (((float)currentHealth / (float)maxHealth) >= (float)maxOneShotProtectionThreshold / 100f)
        {
            // If the damage brings the player below the minimum threshold
            if (currentHealth - damage < maxHealth * ((float)minOneShotProtectionThreshold / 100f))
            {
                damageTaken = currentHealth - Mathf.CeilToInt(maxHealth * ((float)minOneShotProtectionThreshold / 100f));
            }
        }

        // Take damage
        currentHealth -= damageTaken;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Refresh timer
        immuneRegenerationTimer = timeBeforeRegeneration;
        regenerationTimer = 0f;

        // Screenshake if sufficient damage is dealt in an instance
        float shakeTrauma = ((float)damageTaken / maxHealth);
        CameraController.AddTrauma(shakeTrauma * screenshakeDamageScaler);
    }

    void Die()
    {

    }

    void Update()
    {
        // If the timer isn't paused (between rounds or when game is paused)
        if (gamePaused == false)
        {
            // If the game has started (due to picking up the first weapon)
            if (WaveManager.gameStarted)
            {
                currentRunTime += Time.deltaTime;

                // Allow passive ultimate charge
                AddUltimateCharge(Time.deltaTime * passiveUltimateChargeRate * 100f);

                if (currentHealth > 0)
                {
                    if (immuneRegenerationTimer > 0f)
                    {
                        immuneRegenerationTimer -= Time.deltaTime;
                    }
                    else
                    {
                        immuneRegenerationTimer = 0f;

                        if (regenerationTimer < 0f)
                        {
                            // Add some health
                            int regeneratedHealth = Mathf.Min(Mathf.CeilToInt((percentRegenerationPerSecond / 100f) * maxHealth), maxHealth - currentHealth);
                            currentHealth += regeneratedHealth;
                            regenerationTimer = 1f;
                        }
                        else
                        {
                            regenerationTimer -= Time.deltaTime;
                        }
                    }
                }
            }

            #region Item Interaction

            interactPresent = false;
            Collider[] interactsInProximity = Physics.OverlapSphere(mainCameraTransform.position, interactRange, interactiveLayer);
            if (interactsInProximity.Length > 0)
            {
                RaycastHit interactHit;
                Physics.Raycast(mainCameraTransform.position, mainCameraTransform.forward, out interactHit, 10f + interactRange, interactiveLayer);

                bool aimingAtColliderInProximity = false;
                for (int i = 0; i < interactsInProximity.Length; i++)
                {
                    if (interactsInProximity[i] == interactHit.collider)
                    {
                        aimingAtColliderInProximity = true;
                        break;
                    }
                }

                if (interactHit.transform != null && aimingAtColliderInProximity && Physics.Linecast(mainCameraTransform.position, interactHit.point, environmentLayers) == false)
                {
                    // Cache interactive object script
                    currentInteraction = interactHit.transform.gameObject.GetComponent<InteractiveObject>();

                    if (currentInteraction.isInteractable)
                    {
                        // Display interact info
                        interactPresent = true;

                        if (Input.GetKeyDown(KeyCode.E) && (ulong)currentInteraction.GetFibreCost() <= currentFibre)
                        {
                            // Start interaction timer
                            if (currentInteraction != null)
                            {
                                interactTimer = currentInteraction.interactDuration;

                                // If interact is instant
                                if (interactTimer <= 0f)
                                {
                                    currentInteraction.Interact();
                                    currentFibre -= (ulong)currentInteraction.GetFibreCost();
                                    interactTimer = 0f;
                                }
                            }
                        }                        
                    }
                }
            }

            // Progress timer
            if (Input.GetKey(KeyCode.E) && interactTimer > 0f && interactPresent)
            {
                interactTimer -= Time.deltaTime;

                // The wait timer has passed
                if (interactTimer <= 0f)
                {
                    currentInteraction.Interact();
                    currentFibre -= (ulong)currentInteraction.GetFibreCost();
                    interactTimer = 0f;
                }
            }
            else
            {
                // Interrupt wait timer
                interactTimer = 0f;
            }

        #endregion
        }

        #region Update UI

        UserInterfaceHUD.playerCurrentHealth = currentHealth;    
        UserInterfaceHUD.playerMaxHealth = maxHealth;

        UserInterfaceHUD.ultimateCharge = Mathf.Floor(Mathf.Clamp01(ultimateCharge) * 1000f) / 1000f;

        UserInterfaceHUD.currentRunDuration = currentRunTime;

        UserInterfaceHUD.currentScore = currentScore;
        UserInterfaceHUD.currentFibre = currentFibre;

        UserInterfaceHUD.interactPresent = interactPresent;
        
        if (interactPresent && currentInteraction != null)
        {
            UserInterfaceHUD.interactProgress = Mathf.Clamp01(1f - (interactTimer / currentInteraction.interactDuration));
            UserInterfaceHUD.interactPrompt = currentInteraction.interactPrompt;
            UserInterfaceHUD.interactCost = currentInteraction.GetFibreCost();
        }

        #endregion
    }

    public void UpgradeHealth()
    {
        ulong upgradeCost = (ulong)CalculateHealthUpgradeCost();
        if (upgradeCost <= currentFibre)
        {
            int healthMissing = maxHealth - currentHealth;

            maxHealth = CalculateHealthUpgrade();
            currentHealth = Mathf.CeilToInt(maxHealth - healthMissing);
        
            playerLevel += 1;
            currentFibre -= upgradeCost;
        }
    }

    public int CalculateHealthUpgrade()
    {
        float newMaxHealth = baseHealth;
        for (int i = 0; i < playerLevel; i++)
        {
            newMaxHealth *= healthMultiplierPerLevel;
        }

        return Mathf.CeilToInt(newMaxHealth);
    }

    public int CalculateHealthUpgradeCost()
    {
        float scaledCost = healthUpgradeCostBase;
        for (int i = 0; i < playerLevel - 1; i++)
        {
            scaledCost *= healthUpgradeCostMultiplierPerLevel;
        }

        return Mathf.CeilToInt(scaledCost);
    }

    public void AddUltimateCharge(float chargeAmount)
    {
        if (allowUltimateGeneration && WeaponCoordinator.knifeUnlocked)
        {
            ultimateCharge += chargeAmount / 100f;
            ultimateCharge = Mathf.Clamp01(ultimateCharge);
        }
    }

    public bool GetSuperStatus()
    {
        return ultimateCharge >= 1f;
    }

    public void ConsumeSuper()
    {
        ultimateCharge = 0f;
    }

    public void UpgradeWeapon(int weaponIndex)
    {
        gunControllers[weaponIndex].UpgradeWeapon();
    }

    public void CalculateDifficultyLevel()
    {
        if (currentRunTime >= 0f && WaveManager.waveNumber > 0)
        {
            float waveDifficulty = ((float)WaveManager.waveNumber / 2f);
            float timeDifficulty = (currentRunTime / (60f * 3f));

            difficultyLevel = waveDifficulty + timeDifficulty;
        }
    }

    public void AddScore(int baseScore)
    {
        currentScore += (ulong)Mathf.CeilToInt(baseScore + (baseScore * difficultyLevel));
    }

    public void AddFibre(int baseFibre)
    {
        currentFibre += (ulong)Mathf.CeilToInt(baseFibre + (baseFibre * difficultyLevel * 0.1f));
    }
}
