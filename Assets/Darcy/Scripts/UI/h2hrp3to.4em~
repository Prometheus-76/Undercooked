using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Author: Darcy Matheson
// Purpose: Controls the pricing of the different upgrades/resupplies at the upgrade station and the HUD when using it

public class UpgradeStationHUD : MonoBehaviour
{
    #region General
    [Header("General")]

    public Canvas upgradeStationCanvas;
    public GameObject blurEffect;
    public TextMeshProUGUI currentFibreText;
    public static bool showUpgradeHUD;

    private PlayerStats playerStats;

    #endregion

    #region Health
    [Header("Health")]

    public TextMeshProUGUI healthUpgradeCostText;
    public TextMeshProUGUI healthUpgradeStatText;
    public TextMeshProUGUI healthRefillCostText;
    public TextMeshProUGUI healthRefillStatText;

    public static int currentHealth;
    public static int maxHealth;
    private int newMaxHealth;
    private int healthUpgradeCost;

    #endregion

    #region Pepper Shotgun
    [Header("Pepper Shotgun")]

    public TextMeshProUGUI pepperShotgunUpgradeCostText;
    public TextMeshProUGUI pepperShotgunUpgradeStatText;
    public TextMeshProUGUI pepperShotgunRefillCostText;
    public TextMeshProUGUI pepperShotgunRefillStatText;

    public GunController pepperShotgunController;

    private int pepperShotgunCurrentDamage;
    private int pepperShotgunNextDamage;
    private int pepperShotgunLevel;
    private int pepperShotgunCurrentAmmo;
    private int pepperShotgunMaxAmmo;
    private int pepperShotgunUpgradeCost;
    private int pepperShotgunRefillCost;

    #endregion

    #region A-Salt Rifle
    [Header("A-Salt Rifle")]

    public TextMeshProUGUI aSaltRifleUpgradeCostText;
    public TextMeshProUGUI aSaltRifleUpgradeStatText;
    public TextMeshProUGUI aSaltRifleRefillCostText;
    public TextMeshProUGUI aSaltRifleRefillStatText;

    public GunController aSaltRifleController;

    private int aSaltRifleCurrentDamage;
    private int aSaltRifleNextDamage;
    private int aSaltRifleLevel;
    private int aSaltRifleCurrentAmmo;
    private int aSaltRifleMaxAmmo;
    private int aSaltRifleUpgradeCost;
    private int aSaltRifleRefillCost;

    #endregion

    #region Microwave Gun
    [Header("Microwave Gun")]

    public TextMeshProUGUI microwaveGunUpgradeCostText;
    public TextMeshProUGUI microwaveGunUpgradeStatText;

    public GunController microwaveGunController;

    private int microwaveGunCurrentDamage;
    private int microwaveGunNextDamage;
    private int microwaveGunLevel;
    private int microwaveGunUpgradeCost;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        showUpgradeHUD = false;
    }

    // Update is called once per frame
    void Update()
    {
        upgradeStationCanvas.enabled = showUpgradeHUD;
        blurEffect.SetActive(showUpgradeHUD);

        if (showUpgradeHUD)
        {
            RefreshUI();
            currentFibreText.text = playerStats.currentFibre + " <size=60%>fibre<size=100%><voffset=5> |";

            #region Health

            healthUpgradeStatText.text = "<b>Max HP (lvl " + playerStats.playerLevel + "): </b><size=95%>" + maxHealth + "  <sprite=\"Arrow\" index=0>  " + newMaxHealth;
            
            if (maxHealth - currentHealth > 0)
            {
                healthRefillStatText.text = "<b>Current HP: </b><size=95%>" + currentHealth + " / " + maxHealth + " (+" + (maxHealth - currentHealth) + ")";
            }
            else
            {
                healthRefillStatText.text = "<b>Current HP: </b><size=95%>" + currentHealth + " / " + maxHealth + " (Full!)";
            }

            healthUpgradeCostText.text = "Fibre: " + healthUpgradeCost;
            healthRefillCostText.text = "Fibre: " + Mathf.CeilToInt((maxHealth - currentHealth) * playerStats.fibrePerHealthPoint);

            #endregion

            #region Pepper Shotgun

            pepperShotgunUpgradeStatText.text = "<b> DMG / Shot(lvl " + pepperShotgunLevel + "): </b><size=95%>" + pepperShotgunCurrentDamage + "  <sprite=\"Arrow\" index=0>  " + pepperShotgunNextDamage;
             
            if (pepperShotgunMaxAmmo - pepperShotgunCurrentAmmo > 0)
            {
                pepperShotgunRefillStatText.text = "<b>Ammo: </b><size=95%>" + pepperShotgunCurrentAmmo + " / " + pepperShotgunMaxAmmo + " (+" + (pepperShotgunMaxAmmo - pepperShotgunCurrentAmmo) + ")";
            }
            else
            {
                pepperShotgunRefillStatText.text = "<b>Ammo: </b><size=95%>" + pepperShotgunCurrentAmmo + " / " + pepperShotgunMaxAmmo + " (Full!)";
            }

            pepperShotgunUpgradeCostText.text = "Fibre: " + pepperShotgunUpgradeCost;
            pepperShotgunRefillCostText.text = "Fibre: " + pepperShotgunRefillCost;

            #endregion

            #region A-Salt Rifle

            aSaltRifleUpgradeStatText.text = "<b> DMG / Shot(lvl " + aSaltRifleLevel + "): </b><size=95%>" + aSaltRifleCurrentDamage + "  <sprite=\"Arrow\" index=0>  " + aSaltRifleNextDamage;

            if (aSaltRifleMaxAmmo - aSaltRifleCurrentAmmo > 0)
            {
                aSaltRifleRefillStatText.text = "<b>Ammo: </b><size=95%>" + aSaltRifleCurrentAmmo + " / " + aSaltRifleMaxAmmo + " (+" + (aSaltRifleMaxAmmo - aSaltRifleCurrentAmmo) + ")";
            }
            else
            {
                aSaltRifleRefillStatText.text = "<b>Ammo: </b><size=95%>" + aSaltRifleCurrentAmmo + " / " + aSaltRifleMaxAmmo + " (Full!)";
            }

            aSaltRifleUpgradeCostText.text = "Fibre: " + aSaltRifleUpgradeCost;
            aSaltRifleRefillCostText.text = "Fibre: " + aSaltRifleRefillCost;

            #endregion

            #region Microwave Gun

            microwaveGunUpgradeStatText.text = "<b> DMG / Shot(lvl " + microwaveGunLevel + "): </b><size=95%>" + microwaveGunCurrentDamage + "  <sprite=\"Arrow\" index=0>  " + microwaveGunNextDamage;
            microwaveGunUpgradeCostText.text = "Fibre: " + microwaveGunUpgradeCost;

            #endregion
        }
    }

    public void RefreshUI()
    {
        currentHealth = playerStats.currentHealth;
        maxHealth = playerStats.maxHealth;
        newMaxHealth = playerStats.CalculateHealthUpgrade();
        healthUpgradeCost = playerStats.CalculateHealthUpgradeCost();

        pepperShotgunCurrentDamage = pepperShotgunController.scaledDamagePerBullet;
        pepperShotgunNextDamage = pepperShotgunController.CalculateUpgradeDamage();
        pepperShotgunLevel = pepperShotgunController.gunUpgradeLevel;
        pepperShotgunMaxAmmo = pepperShotgunController.magazineSize + pepperShotgunController.ammoTotalReserves;
        pepperShotgunCurrentAmmo = pepperShotgunController.currentAmmoInMagazine + pepperShotgunController.currentAmmoInReserves;
        pepperShotgunUpgradeCost = pepperShotgunController.CalculateWeaponUpgradeCost();
        pepperShotgunRefillCost = Mathf.CeilToInt((pepperShotgunMaxAmmo - pepperShotgunCurrentAmmo) * pepperShotgunController.costPerBullet);
        pepperShotgunRefillCost += Mathf.CeilToInt(pepperShotgunRefillCost * (playerStats.difficultyLevel / 5f));

        aSaltRifleCurrentDamage = aSaltRifleController.scaledDamagePerBullet;
        aSaltRifleNextDamage = aSaltRifleController.CalculateUpgradeDamage();
        aSaltRifleLevel = aSaltRifleController.gunUpgradeLevel;
        aSaltRifleMaxAmmo = aSaltRifleController.magazineSize + aSaltRifleController.ammoTotalReserves;
        aSaltRifleCurrentAmmo = aSaltRifleController.currentAmmoInMagazine + aSaltRifleController.currentAmmoInReserves;
        aSaltRifleUpgradeCost = aSaltRifleController.CalculateWeaponUpgradeCost();
        aSaltRifleRefillCost = Mathf.CeilToInt(Mathf.CeilToInt((aSaltRifleMaxAmmo - aSaltRifleCurrentAmmo) * aSaltRifleController.costPerBullet));
        aSaltRifleRefillCost += Mathf.CeilToInt(aSaltRifleRefillCost * (playerStats.difficultyLevel / 5f));

        microwaveGunCurrentDamage = microwaveGunController.scaledDamagePerBullet;
        microwaveGunNextDamage = microwaveGunController.CalculateUpgradeDamage();
        microwaveGunLevel = microwaveGunController.gunUpgradeLevel;
        microwaveGunUpgradeCost = microwaveGunController.CalculateWeaponUpgradeCost();
    }

    public void RefillAmmo(int weaponIndex)
    {
        GunController weaponScript = null;
        int ammoCost = 0;
        switch (weaponIndex)
        {
            case 1:
                weaponScript = aSaltRifleController;
                ammoCost = aSaltRifleRefillCost;
                break;
            case 2:
                weaponScript = pepperShotgunController;
                ammoCost = pepperShotgunRefillCost;
                break;
            default:
                return;
        }

        // Purchase ammo and refill weapon
        if ((ulong)ammoCost <= playerStats.currentFibre)
        {
            playerStats.currentFibre -= (ulong)ammoCost;
            weaponScript.currentAmmoInMagazine = weaponScript.magazineSize;
            weaponScript.currentAmmoInReserves = weaponScript.ammoTotalReserves;
        }
        else
        {
            // Insufficient fibre count, do not allow purchase
            return;
        }
    }

    public void RefillHealth()
    {
        int missingHealth = 0;
        int restoreCost = 0;

        missingHealth = maxHealth - currentHealth;
        restoreCost = Mathf.CeilToInt(missingHealth * playerStats.fibrePerHealthPoint);

        // Purchase ammo and refill weapon
        if ((ulong)restoreCost <= playerStats.currentFibre)
        {
            playerStats.currentFibre -= (ulong)restoreCost;
            playerStats.currentHealth = playerStats.maxHealth;
        }
        else
        {
            // Insufficient fibre count, do not allow purchase
            return;
        }
    }

    public void CloseUpgradeStation()
    {
        showUpgradeHUD = false;
        UserInterfaceHUD.showHUD = true;

        // Make the cursor invisible and stop it from leaving the window
        Cursor.lockState = CursorLockMode.Locked;
        PlayerStats.gamePaused = false;
    }
}
