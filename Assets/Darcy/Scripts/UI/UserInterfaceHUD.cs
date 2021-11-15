using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Author: Darcy Matheson
// Purpose: Controls the heads up display of the player with globally accessible static variables

public class UserInterfaceHUD : MonoBehaviour
{
    #region General 
    [Header("General")]

    public Canvas HUDCanvas;

    public static bool showHUD;
    #endregion

    #region Reticles
    [Header("Reticles")]

    public Image microwaveGunReticleImage;
    public Image aSaltRifleReticleImage;
    public Image pepperShotgunReticleImage;
    #endregion

    #region Reloading
    [Header("Reloading")]

    public Image reloadProgressImage;
    public Image reloadUnderlayImage;

    public static float reloadProgress; // The progress of the current weapon's reload animation
    #endregion

    #region Wave Counter and Progress
    [Header("Wave Counter and Progress")]

    public Image waveProgressImage;
    public TextMeshProUGUI waveStatusText;
    public TextMeshProUGUI waveProgressText;

    public static float waveProgress; // The progress of the current wave
    public static float intermissionProgress; // The progress of the current intermission
    public static int waveNumber; // The number of the current wave
    public static int intermissionDuration; // The length in seconds of the intermission
    #endregion

    #region Player Health
    [Header("Player Health")]

    public static int playerCurrentHealth;
    public static int playerMaxHealth;

    public Image healthProgressImage;
    public Image healthDelayedProgressImage;
    public TextMeshProUGUI healthText;
    private string colourCodeHealthBar;
    #endregion

    #region Weapon Details
    [Header("Weapon Details")]

    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI currentWeaponText;
    public Image weaponDividerImage;

    public static int ammoInMagazine;
    public static int ammoInReserves;
    public static int equippedWeapon;
    #endregion

    #region Ultimate Ability
    [Header("Ultimate Ability")]

    public Image minorFillImage;
    public Image majorFillImage;
    public TextMeshProUGUI ultimateProgressText;

    public static float ultimateCharge;
    #endregion

    #region Run Timer
    [Header("Run Timer")]

    public TextMeshProUGUI runDurationText;

    public static float currentRunDuration;
    #endregion

    #region Score and Fibre
    [Header("Score and Fibre")]

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI fibreText;

    public static ulong currentScore;
    public static ulong currentFibre;
    #endregion

    #region Interaction
    [Header("Interaction")]

    public TextMeshProUGUI interactPromptText;
    public TextMeshProUGUI interactCostText;
    public Image interactUnderlayImage;
    public Image interactProgressImage;

    public static float interactProgress;
    public static int interactCost;
    public static string interactPrompt;
    public static bool interactPresent;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        reloadProgress = 0f;
        equippedWeapon = -1;

        // Grab the colour of the health bar and automatically use it for text
        colourCodeHealthBar = ColorUtility.ToHtmlStringRGBA(healthProgressImage.color);

        showHUD = true;
    }

    // Update is called once per frame
    void Update()
    {
        #region General

        HUDCanvas.enabled = showHUD;

        #endregion

        if (showHUD)
        {
            #region Reload Progress

            // If the weapon is reloading
            if (Mathf.Clamp01(reloadProgress) > 0f)
            {
                // Hide reticle
                microwaveGunReticleImage.enabled = false;
                aSaltRifleReticleImage.enabled = false;
                pepperShotgunReticleImage.enabled = false;

                // Show reload progress
                reloadProgressImage.enabled = true;
                reloadUnderlayImage.enabled = true;

                // Update progress
                reloadProgressImage.fillAmount = Mathf.Clamp01(reloadProgress);
            }
            else
            {
                if (Movement.isGrappling == false)
                {
                    // Show reticle
                    switch (equippedWeapon)
                    {
                        case 0:
                            microwaveGunReticleImage.enabled = true;
                            aSaltRifleReticleImage.enabled = false;
                            pepperShotgunReticleImage.enabled = false;
                            break;
                        case 1:
                            aSaltRifleReticleImage.enabled = true;
                            microwaveGunReticleImage.enabled = false;
                            pepperShotgunReticleImage.enabled = false;
                            break;
                        case 2:
                            pepperShotgunReticleImage.enabled = true;
                            microwaveGunReticleImage.enabled = false;
                            aSaltRifleReticleImage.enabled = false;
                            break;
                    }
                }
                else
                {
                    // Hide reticle
                    microwaveGunReticleImage.enabled = false;
                    aSaltRifleReticleImage.enabled = false;
                    pepperShotgunReticleImage.enabled = false;
                }

                // Hide reload progress
                reloadProgressImage.enabled = false;
                reloadUnderlayImage.enabled = false;
            }

            #endregion

            #region Wave Counter and Progress

            if (intermissionProgress <= 0)
            {
                waveProgressImage.fillAmount = Mathf.Clamp01(waveProgress);
                waveProgressText.text = (Mathf.Clamp01(waveProgress) * 100f).ToString("F1") + "%";
                waveStatusText.text = "Stage " + waveNumber;
            }
            else
            {
                waveProgressImage.fillAmount = Mathf.Clamp01(1f - intermissionProgress);
                waveProgressText.text = (Mathf.Clamp01(1f - intermissionProgress) * intermissionDuration).ToString("F1") + "s";
                waveStatusText.text = "Prepare";
            }

            #endregion

            #region Player Health

            healthText.text = "<color=#" + colourCodeHealthBar + ">" + playerCurrentHealth + "<size=50%><color=white> / " + playerMaxHealth;
            healthProgressImage.fillAmount = Mathf.Clamp01((float)playerCurrentHealth / (float)playerMaxHealth);

            // Update delayed fill
            if (healthProgressImage.fillAmount < healthDelayedProgressImage.fillAmount)
            {
                // Delay rate is faster when the difference between current and delayed is higher
                float newFillAmount = Mathf.Clamp01(healthDelayedProgressImage.fillAmount - ((healthDelayedProgressImage.fillAmount - healthProgressImage.fillAmount) * 5f * Time.deltaTime));
                healthDelayedProgressImage.fillAmount = (newFillAmount - healthProgressImage.fillAmount > 0.002f) ? newFillAmount : healthProgressImage.fillAmount;
            }
            else
            {
                // Reset delayed fill under current fill when regenerating health
                healthDelayedProgressImage.fillAmount = healthProgressImage.fillAmount;
            }

            #endregion

            #region Weapon Details

            // Ammo display
            if ((ammoInReserves > 0 && ammoInReserves != -1) || ammoInReserves == -1 || ammoInMagazine > 0)
            {
                // There is ammo in the weapon
                ammoText.text = ammoInMagazine + " <size=60%><color=#FFFFFFAA>/ " + (ammoInReserves == -1 ? "∞" : ammoInReserves.ToString());
            }
            else
            {
                // There is no ammo in the magazine or in reserves
                ammoText.text = "Empty";
            }

            // Weapon icon
            switch (equippedWeapon)
            {
                case -1:
                    ammoText.enabled = false;
                    weaponDividerImage.enabled = false;
                    currentWeaponText.enabled = false;
                    currentWeaponText.text = "None";
                    break;
                case 0:
                    ammoText.enabled = true;
                    weaponDividerImage.enabled = true;
                    currentWeaponText.enabled = true;
                    currentWeaponText.text = "Microwave Gun";
                    break;
                case 1:
                    ammoText.enabled = true;
                    weaponDividerImage.enabled = true;
                    currentWeaponText.enabled = true;
                    currentWeaponText.text = "A-Salt Rifle";
                    break;
                case 2:
                    ammoText.enabled = true;
                    weaponDividerImage.enabled = true;
                    currentWeaponText.enabled = true;
                    currentWeaponText.text = "Pepper Shotgun";
                    break;
            }

            #endregion

            #region Ultimate Ability

            float ultimateChargePercent = (ultimateCharge * 100f);

            minorFillImage.fillAmount = (ultimateCharge * (25f / 29f)) + (((Mathf.FloorToInt(ultimateChargePercent) - Mathf.FloorToInt(ultimateChargePercent) % 20f) / 20f) * (1f / 29f));
            float majorFillTarget = ((float)(Mathf.FloorToInt(ultimateChargePercent) - (Mathf.FloorToInt(ultimateChargePercent) % 20f)) / 100f);
        
            // Lerp between major fill targets
            if (majorFillImage.fillAmount < majorFillTarget)
            {
                majorFillImage.fillAmount += 0.6f * Time.deltaTime;
                if (majorFillImage.fillAmount > majorFillTarget)
                {
                    // Snap to major fill target
                    majorFillImage.fillAmount = majorFillTarget;
                }
            }

            if (majorFillImage.fillAmount > ultimateCharge)
            {
                majorFillImage.fillAmount = ultimateCharge;
            }

            ultimateProgressText.text = (ultimateCharge < 1f ? ultimateChargePercent.ToString("F1") + "<size=70%>" : "Charged!");

            #endregion

            #region Run Timer

            // The exact time minus the closest floored whole value
            int decimalSeconds = Mathf.FloorToInt((currentRunDuration - Mathf.Floor(currentRunDuration)) * 100f);

            // The floored excess leading up to 60
            int seconds = Mathf.FloorToInt(currentRunDuration % 60f);

            // The floored excess leading up to 3600, minus the second precision and scaled down to within 60 second range
            int minutes = (Mathf.FloorToInt(currentRunDuration % 3600f) - seconds) / 60;

            // The floored timer minus the second-values of seconds and minutes, scaled down to a 3600 second range
            int hours = (Mathf.FloorToInt(currentRunDuration) - (minutes * 60) - (seconds)) / 3600;

            // Format and display string
            runDurationText.text = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2") + "<size=50%><voffset=0.7em><color=#FFFFFF64>." + decimalSeconds.ToString("D2");

            #endregion

            #region Score and Fibre

            string scoreString = currentScore.ToString("D7");
            string tempString = "Score: <b><color=#FFFFFF64>";        
        
            bool startFound = false;
            for (int i = 0; i < scoreString.Length; i++)
            {
                if (scoreString[i] == '0' && startFound == false)
                {
                    tempString += '0';
                }
                else
                {
                    if (startFound == false)
                    {
                        startFound = true;
                        tempString += "<color=white>";
                    }

                    tempString += scoreString[i];
                }
            }

            // Assign completed string
            scoreString = tempString;

            string fibreString = currentFibre.ToString("D5");
            tempString = "F<voffset=0.12em>ibre: </voffset><b><color=#FFFFFF64>";

            startFound = false;
            for (int i = 0; i < fibreString.Length; i++)
            {
                if (fibreString[i] == '0' && startFound == false)
                {
                    tempString += '0';
                }
                else
                {
                    if (startFound == false)
                    {
                        startFound = true;
                        tempString += "<color=white>";
                    }

                    tempString += fibreString[i];
                }
            }

            // Assign completed string
            fibreString = tempString;

            // Assign to text UI
            scoreText.text = scoreString;
            fibreText.text = fibreString;

            #endregion

            #region Interaction

            if (interactProgress > 0f && interactProgress < 1f)
            {
                // Show and update progress bar
                interactProgressImage.enabled = true;
                interactUnderlayImage.enabled = true;

                interactProgressImage.fillAmount = interactProgress;
            }
            else
            {
                interactProgressImage.enabled = false;
                interactUnderlayImage.enabled = false;
            }
        
            if (interactPresent)
            {
                // Show interact text
                interactPromptText.enabled = true;
                interactPromptText.text = interactPrompt + " (" + KeyCode.E.ToString().ToLower() + ")";

                if (interactCost > 0)
                {
                    interactCostText.enabled = true;
                    interactCostText.text = interactCost.ToString() + " Fibre";
                }
            }
            else
            {
                // Hide UI
                interactPromptText.enabled = false;
                interactCostText.enabled = false;

                interactProgressImage.enabled = false;
                interactUnderlayImage.enabled = false;
            }

            #endregion
        }
    }
}
