using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: A base class for the guns used in the game, allowing all kinds of configuration which can convert guns between archetypes.

public class GunController : MonoBehaviour
{
    #region Variables

    #region Internal

    public bool isRecoiling { get; private set; }
    public bool isReloading { get; private set; }

    public enum WeaponType
    {
        MicrowaveGun,
        ASaltRifle,
        PepperShotgun
    }
    
    [HideInInspector]
    public bool isCurrentlyEquipped;

    private bool inputAcknowledged;
    [HideInInspector]
    public int currentAmmoInMagazine;
    [HideInInspector]
    public int currentAmmoInReserves;

    private float shotTimeInterval;
    private float shotIntervalTimer;

    private float recoilTimer;
    private Vector3 startingPosition;
    private Vector3 targetPosition;

    private float reloadTimer;

    private float baseShotVolume;

    #endregion

    #region Parameters

    #region Configuration
    [Header("Configuration")]

    [Tooltip("Which weapon type this script is on.")]
    public WeaponType weaponType;

    [Tooltip("The layers which bullets interact with, should include enemies and all layers which can block shots.")]
    public LayerMask bulletInteractionLayers;
    #endregion

    #region General Stats
    [Header("General Stats")]

    [Tooltip("The time it takes to stow and draw the weapon. Added together with the weapon we are switch to, this is the total time taken to switch."), Range(0.1f, 1f)]
    public float swapTime;

    [Tooltip("The rate at which the weapon rotates towards a grapple point while using the hookshot."), Range(1, 25)]
    public int grappleRotationSpeed;

    [Tooltip("Whether or not the shots from this weapon should pierce through enemies or not.")]
    public bool piercingBullets;

    [Tooltip("Only applies if above is true, the maximum number of enemies that can be damaged at a time."), Range(2, 10)]
    public int maximumPenetrationCount;

    [Tooltip("The base damage dealt by each bullet of this weapon."), Range(1, 1000)]
    public int baseDamagePerBullet;
    [HideInInspector]
    public int scaledDamagePerBullet { get; private set; }

    [Tooltip("The damage multiplier applied when dealing maximum damage with this weapon (at closer range)."), Range(1f, 2f)]
    public float damageMultiplier;

    [Tooltip("The furthest angle the bullets from this weapon can stray from the forward vector."), Range(0f, 45f)]
    public float bulletSpreadAngle;

    [Tooltip("The maximum amount of ultimate charge awarded per hit with this weapon (0 - 100, in percent)."), Range(0f, 1f)]
    public float ultimateChargePerHit;
    #endregion

    #region Upgrades
    [Header("Upgrades")]

    [Tooltip("How much more damage each sequential level of the weapon deals (multiplies the damage at the previous level)."), Range(1f, 2f)]
    public float damageMultiplierPerLevel;

    [Tooltip("The starting cost of upgrading this weapon."), Range(1, 500)]
    public int weaponUpgradeCostBase;

    [Tooltip("How much more expensive each sequential upgrade of the weapon costs (multiplies the damage at the previous level)."), Range(1f, 2f)]
    public float weaponUpgradeCostMultiplierPerLevel;

    [HideInInspector]
    public int gunUpgradeLevel { get; private set; }
    #endregion

    #region Range and Fire Rate
    [Header("Range and Fire Rate")]

    [Tooltip("Whether or not the weapon will fire each consecutive shot automatically.")]
    public bool fullAutoFiring;

    [Tooltip("How many bullets are fired from the weapon for each round."), Range(1, 10)]
    public int shotsPerRound;

    [Tooltip("How many rounds are fired from this weapon per minute, its maximum fire rate."), Range(60, 1000)]
    public int roundsPerMinute;

    [Tooltip("The maximum distance in world units that this weapon is able to deal damage (should be greater than dropoff range)."), Range(0, 250)]
    public int maxRange;

    [Tooltip("The range at which damage starts to fall off from the maximum."), Range(0, 100)]
    public int dropoffRange;
    #endregion

    #region Ammo
    [Header("Ammo")]

    [Tooltip("How much ammo the magazine can hold."), Range(1, 100)]
    public int magazineSize;

    [Tooltip("How much ammo this weapon can hold in reserves, -1 sets the reserves to ininite."), Range(-1, 1000)]
    public int ammoTotalReserves;

    [Tooltip("How much ammo is consumed for every round that is fired from this weapon, should usually be 1."), Range(1, 10)]
    public int ammoPerRound;

    [Tooltip("The cost for buying a bullet for this weapon at an upgrade station."), Range(0f, 10f)]
    public float costPerBullet;
    #endregion

    #region Reloading
    [Header("Reloading")]

    [Tooltip("How long it takes to reload this weapon (in seconds)."), Range(0.1f, 5f)]
    public float reloadDuration;

    [Tooltip("How many rotations the gun completes while reloading."), Range(1, 10)]
    public int reloadRotations;

    [Tooltip("Controls the direction of the we rotation when reloading.")]
    public bool rotateBackwards;
    #endregion

    #region Firing Recoil
    [Header("Firing Recoil")]

    [Tooltip("The maximum distance the weapon pulls back to while recoiling."), Range(0f, 1f)]
    public float weaponRecoilDistance;

    [Tooltip("How long it takes to pull back all the way while recoiling from a resting position (in seconds)."), Range(0f, 1f)]
    public float recoilEffectTime;

    [Tooltip("How long it takes to recover from a fully recoiled position (in seconds)."), Range(0, 3f)]
    public float recoilRecoveryTime;

    [Tooltip("How intensely the camera is able to recoil while firing this weapon (measured in mouse delta per frame).")]
    public Vector2 cameraRecoil;
    #endregion

    #region Sounds

    #region Sound Clips
    [Header("Sound Clips")]

    [Tooltip("The sound that plays at the end of a reload.")]
    public AudioClip reloadSound;

    [Tooltip("The various sounds that can play per bullet shot. If the gun is looping a sound instead, only the first one will be used.")]
    public AudioClip[] firingSounds;
    #endregion

    #region Sound Looping
    [Header("Sound Looping")]

    [Tooltip("Whether the sound should be looped or played per shot.")]
    public bool loopFiringSound;

    [Tooltip("Only applies if box above is checked. Affects how fast the volume of looping weapons drops off when stopping firing."), Range(0f, 10f)]
    public float loopingVolumeDropoffRate;
    #endregion

    #region Sound Pitching
    [Header("Sound Pitching")]

    [Tooltip("Whether to change the pitch of the weapon based on the ammo remaining in the magazine.")]
    public bool pitchAmmoDepletion;

    [Tooltip("Only applies if box above is checked. As ammo runs out in the magazine, pitch the shot sound up according to this curve.")]
    public AnimationCurve ammoDepletionSoundCurve;

    [Tooltip("Only applies if box above is checked. Pitch ranges between 1 and this number as ammo runs out."), Range(0f, 1f)]
    public float maximumPitch;
    #endregion

    #endregion

    #endregion

    #region Components
    [Header("Components")]

    public TrailRenderer reloadSmokeTrail;

    public Transform recoilHolder;
    public Transform weaponContainerTransform;
    public Transform weaponModelTransform;
    public Transform weaponFirePoint;

    public Transform damageNumberParentTransform;
    public GameObject damageNumberPrefab;
    
    public CameraController cameraController;

    public AudioSource shootingAudioSource;
    public AudioSource alternateAudioSource;

    private Transform mainCameraTransform;

    private Movement playerMovement;
    private PlayerStats playerStats;
    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Initialisation

        // Start with gun full of ammo
        currentAmmoInMagazine = magazineSize;
        currentAmmoInReserves = ammoTotalReserves;

        // Calculate shot interval from rounds per minute
        shotTimeInterval = (float)(60f / roundsPerMinute);

        // Get camera
        mainCameraTransform = Camera.main.transform;

        // Get player scripts
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<Movement>();
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();

        // Save the original volume
        baseShotVolume = shootingAudioSource.volume;

        // Determine the starting damage per shot
        scaledDamagePerBullet = baseDamagePerBullet;
        gunUpgradeLevel = 1;

        #endregion
    }

    // Update is called once per frame
    void Update()
    {
        if (isCurrentlyEquipped)
        {
            #region Player Inputs

            #region Firing and Reloading

            if (isCurrentlyEquipped && WeaponCoordinator.switchingWeapons == false && Movement.isGrappling == false && PlayerStats.gamePaused == false)
            {
                if (isReloading == false && currentAmmoInMagazine == 0 && (currentAmmoInReserves > 0 || ammoTotalReserves == -1))
                {
                    // Ensure rotation is reset
                    if (weaponModelTransform.localRotation == Quaternion.identity)
                    {
                        // No ammo, auto reload
                        isReloading = true;
                        reloadTimer = reloadDuration;
                    }
                }
                else if (Input.GetKey(KeyCode.R) && isReloading == false && currentAmmoInMagazine < magazineSize && (currentAmmoInReserves > 0 || ammoTotalReserves == -1))
                {
                    // Ensure rotation is reset
                    if (weaponModelTransform.localRotation == Quaternion.identity)
                    {
                        // Some ammo, manual reload
                        isReloading = true;
                        reloadTimer = reloadDuration;
                    }
                }
                else if (Input.GetMouseButton(0) && currentAmmoInMagazine > 0 && shotIntervalTimer <= 0f && isReloading == false)
                {
                    // Some ammo, shooting
                    if (fullAutoFiring)
                    {
                        // Firing full auto
                        shotIntervalTimer = shotTimeInterval;
                    }
                    else if (fullAutoFiring == false && inputAcknowledged == false)
                    {
                        // Firing manually
                        shotIntervalTimer = shotTimeInterval;
                        inputAcknowledged = true;
                    }

                    // Shooting / Sprinting iterruption
                    if (Input.GetMouseButtonDown(0) && Movement.isSprinting)
                    {
                        // Stop sprinting
                        playerMovement.InterruptSprint();
                    }
                    else if (Input.GetMouseButton(0) && Movement.isSprinting)
                    {
                        // Stop shooting
                        shotIntervalTimer = -1f;
                    }
                }
                else if (Input.GetMouseButtonUp(0) && inputAcknowledged && fullAutoFiring == false)
                {
                    // The shoot button has been released, allow another manual shot
                    inputAcknowledged = false;
                }
            }

            #endregion

            #region Looped Sounds

            // If the sound should loop
            if (loopFiringSound && Input.GetMouseButton(0) && isReloading == false && Movement.isGrappling == false && PlayerStats.gamePaused == false)
            {
                // Pitch up as magazine begins to end
                shootingAudioSource.pitch = pitchAmmoDepletion ? ((ammoDepletionSoundCurve.Evaluate(1f - ((float)currentAmmoInMagazine / (float)magazineSize)) * maximumPitch) + 1f) : 1f;

                // If the sound isn't playing currently
                if (shootingAudioSource.isPlaying == false)
                {
                    shootingAudioSource.clip = firingSounds[0];
                    shootingAudioSource.Play();
                }
            }
            else if (loopFiringSound && (isReloading || Input.GetMouseButton(0) == false || Movement.isGrappling || PlayerStats.gamePaused))
            {
                // If we stop firing for some reason
                if (shootingAudioSource.isPlaying)
                {
                    // Lower volume over time
                    shootingAudioSource.volume -= loopingVolumeDropoffRate * Time.deltaTime;

                    // If gun is silent, stop playing
                    if (shootingAudioSource.volume <= 0f)
                    {
                        shootingAudioSource.volume = 0f;
                        shootingAudioSource.Stop();
                    }
                }
            }

            #endregion

            #endregion

            #region Reloading

            if (isReloading)
            {
                // Play reload sound at the start of the reload
                if (reloadTimer == reloadDuration)
                {
                    alternateAudioSource.PlayOneShot(reloadSound);
                }

                // Decrement timer
                reloadTimer -= Time.deltaTime;
            
                // Smoke effect on
                reloadSmokeTrail.emitting = true;

                // Calculate reload progress
                float progress = Mathf.Clamp01(1f - (reloadTimer / reloadDuration));

                // Evaluate progress on a curve
                float totalAnglularRotation = 360f * (float)reloadRotations;
                float newRotation = (ReloadSpinCurve(progress) * totalAnglularRotation);
                Quaternion spinRotation = Quaternion.Euler(Vector3.right * newRotation * (rotateBackwards ? -1f : 1f));
                weaponContainerTransform.localRotation = spinRotation;

                // When reload has ended
                if (reloadTimer <= 0f)
                {
                    isReloading = false;

                    // The maximum amount of extra ammo that can be put in the magazine out of reserves
                    if (ammoTotalReserves == -1)
                    {
                        // Infinite reserves
                        currentAmmoInMagazine = magazineSize;
                    }
                    else
                    {
                        // Limited ammo in reserves
                        int missingAmmo = Mathf.Clamp(magazineSize - currentAmmoInMagazine, 0, currentAmmoInReserves);
                        currentAmmoInReserves -= missingAmmo;
                        currentAmmoInMagazine += missingAmmo;
                    }

                    // Snap rotation to finished position
                    weaponContainerTransform.localRotation = Quaternion.identity;
                
                    // Smoke effect off
                    reloadSmokeTrail.emitting = false;
                }
            }

            #endregion

            #region Weapon Recoil

            if (recoilTimer > recoilRecoveryTime)
            {
                // Recoil backwards
                float recoilProgress = Mathf.Clamp01((recoilTimer - recoilRecoveryTime) / recoilEffectTime);
                Vector3 recoilPosition = startingPosition + (RecoilCurve(1f - recoilProgress) * (targetPosition - startingPosition));
                recoilHolder.localPosition = recoilPosition;

                recoilTimer -= Time.deltaTime;
            }
            else if (recoilTimer > 0f)
            {
                // Recover from recoil
                float recoveryProgress = Mathf.Clamp01(recoilTimer / recoilRecoveryTime);
                Vector3 recoveryPosition = startingPosition + (RecoveryCurve(recoveryProgress) * (targetPosition - startingPosition));
                recoilHolder.localPosition = recoveryPosition;

                recoilTimer -= Time.deltaTime;
            }
            else if (recoilTimer <= 0f)
            {
                // Reset position
                recoilHolder.localPosition = startingPosition;
                isRecoiling = false;
            }

            #endregion

            #region Update UI

            if (isCurrentlyEquipped)
            {
                // Reloading indicator
                float reloadProgress = Mathf.Clamp01(reloadTimer / reloadDuration);
                UserInterfaceHUD.reloadProgress = reloadProgress;

                // Ammo display
                UserInterfaceHUD.ammoInMagazine = currentAmmoInMagazine;
                UserInterfaceHUD.ammoInReserves = currentAmmoInReserves;
                UserInterfaceHUD.equippedWeapon = (int)weaponType;
            }

            #endregion
        }
    }

    // FixedUpdate is called once every physics iteration
    void FixedUpdate()
    {
        #region Shooting

        if (shotIntervalTimer > 0f)
        {
            // Fire weapon immediately
            if (shotIntervalTimer == shotTimeInterval)
            {
                ShootWeapon();
            }

            // Decrement timer
            shotIntervalTimer -= Time.fixedDeltaTime;
        }

        #endregion
    }

    // Fires a single round from the weapon
    void ShootWeapon()
    {
        // Ensure volume is at the intended level
        if (loopFiringSound)
        {
            shootingAudioSource.volume = baseShotVolume;
        }

        // Subtract ammo per round
        currentAmmoInMagazine -= ammoPerRound;
        currentAmmoInMagazine = Mathf.Clamp(currentAmmoInMagazine, 0, magazineSize);

        #region Raycast Shots

        // For each bullet in the round
        for (int bulletNumber = 0; bulletNumber < shotsPerRound; bulletNumber++)
        {
            // Calculate raycast direction
            Vector3 bulletDirection = mainCameraTransform.forward;

            if (bulletSpreadAngle > 0f)
            {
                float spreadInstance = Random.Range(0f, bulletSpreadAngle);
                Vector3 unrotatedDirection = Quaternion.AngleAxis(spreadInstance, mainCameraTransform.up).normalized * mainCameraTransform.forward;

                float rotationInstance = Random.Range(0f, 360f);
                bulletDirection = Quaternion.AngleAxis(rotationInstance, mainCameraTransform.forward) * unrotatedDirection;
                bulletDirection.Normalize();
            }

            // Draw bullet graphics


            // Fire from camera
            // Get hit info (RaycastAll for piercing ammo)
            if (piercingBullets)
            {
                RaycastHit[] hits;
                hits = Physics.RaycastAll(mainCameraTransform.position, bulletDirection, maxRange, bulletInteractionLayers);

                // Sort by distance
                System.Array.Sort(hits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));

                // Compare from shortest to furthest
                for (int i = 0; i < Mathf.Min(hits.Length, maximumPenetrationCount); i++)
                {
                    if (hits[i].transform != null && hits[i].collider.tag == "Enemy")
                    {
                        // We hit an enemy, proceed with damage calculation
                        int damage = CalculateDamageDropoff(scaledDamagePerBullet, hits[i].distance);

                        // Deal damage to enemy script and give player ultimate charge
                        if (hits[i].collider.gameObject.GetComponent<Enemy>())
                        {
                            hits[i].collider.gameObject.GetComponent<Enemy>().TakeDamage(Mathf.CeilToInt(damage * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f)), Mathf.CeilToInt(scaledDamagePerBullet * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f)), hits[i].point, false);
                            playerStats.AddUltimateCharge(ultimateChargePerHit * ((float)damage / (float)scaledDamagePerBullet) * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                RaycastHit hit;
                Physics.Raycast(mainCameraTransform.position, bulletDirection, out hit, maxRange, bulletInteractionLayers);

                if (hit.transform != null && hit.collider.tag == "Enemy")
                {
                    // We hit an enemy, proceed with damage calculation
                    int damage = CalculateDamageDropoff(scaledDamagePerBullet, hit.distance);

                    // Deal damage to enemy script
                    if (hit.collider.gameObject.GetComponent<Enemy>())
                    {
                        hit.collider.gameObject.GetComponent<Enemy>().TakeDamage(Mathf.CeilToInt(damage * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f)), Mathf.CeilToInt(scaledDamagePerBullet * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f)), hit.point, false);
                        playerStats.AddUltimateCharge(ultimateChargePerHit * ((float)damage / (float)scaledDamagePerBullet) * ((damage == scaledDamagePerBullet) ? damageMultiplier : 1f));
                    }
                }
            }
        }

        #endregion

        AddRecoil();

        // Play shooting sound per shot
        if (loopFiringSound == false && firingSounds.Length > 0)
        {
            int randomSoundIndex = Random.Range(0, firingSounds.Length);
            shootingAudioSource.pitch = pitchAmmoDepletion ? ((ammoDepletionSoundCurve.Evaluate(1f - ((float)currentAmmoInMagazine / (float)magazineSize)) * maximumPitch) + 1f) : 1f;
            shootingAudioSource.PlayOneShot(firingSounds[randomSoundIndex]);
        }
    }

    // Recoils the gun and camera
    void AddRecoil()
    {
        // Set positions for lerp
        startingPosition = Vector3.zero;
        targetPosition = Vector3.back * weaponRecoilDistance;

        // Evaluate how far through current recoil animation based on time
        float currentProgress = 0f; // 0 is resting position, 1 is fully recoiled
        currentProgress = (recoilHolder.localPosition.z - startingPosition.z) / (-weaponRecoilDistance);

        // Set timer for effect
        recoilTimer = (recoilEffectTime * (1f - currentProgress)) + recoilRecoveryTime;

        // Add camera recoil
        cameraController.AddRecoil(cameraRecoil.x, cameraRecoil.y);
        isRecoiling = true;
    }

    // Calculate the damage of a bullet after accounting for damage dropoff
    int CalculateDamageDropoff(float baseDamage, float range)
    {
        int result = 0;

        if (range <= dropoffRange)
        {
            result = Mathf.CeilToInt(baseDamage);
        }
        else if (range < maxRange)
        {
            // Damage dropoff
            result = Mathf.CeilToInt(baseDamage - (baseDamage * Mathf.Sqrt(range - dropoffRange) / Mathf.Sqrt(maxRange - dropoffRange)));
        }
        else
        {
            // Shot is out of range (deal no damage)
            result = 0;
        }

        return result;
    }

    // Upgrade the weapon
    public void UpgradeWeapon()
    {
        ulong upgradeCost = (ulong)CalculateWeaponUpgradeCost();

        if (upgradeCost <= playerStats.currentFibre)
        {
            scaledDamagePerBullet = CalculateUpgradeDamage();

            gunUpgradeLevel += 1;
            playerStats.currentFibre -= upgradeCost;
        }
    }

    // Calculates the weapon's damage based on its current level
    public int CalculateUpgradeDamage()
    {
        float newDamagePerBullet = baseDamagePerBullet;
        for (int i = 0; i < gunUpgradeLevel; i++)
        {
            newDamagePerBullet *= damageMultiplierPerLevel;
        }

        return Mathf.CeilToInt(newDamagePerBullet);
    }

    // Calculates the cost of the upgrade
    public int CalculateWeaponUpgradeCost()
    {
        float scaledCost = weaponUpgradeCostBase;
        for (int i = 0; i < gunUpgradeLevel - 1; i++)
        {
            scaledCost *= weaponUpgradeCostMultiplierPerLevel;
        }

        return Mathf.CeilToInt(scaledCost);
    }

    // The curve used for the spin easing
    float ReloadSpinCurve(float progress)
    {
        float result = 0f;

        result = (Mathf.Cos(Mathf.PI * (progress + 1f)) + 1f) / 2f;
        result = Mathf.Clamp01(result);

        return result;
    }

    // The curve used for pulling the weapon back when recoiling
    float RecoilCurve(float progress)
    {
        float result = 0f;
        result = 1f - ((progress - 1f) * (progress - 1f));

        return result;
    }

    // The curve used for easing the weapon back a resting position
    float RecoveryCurve(float progress)
    {
        float result = 0f;
        result = (Mathf.Cos((Mathf.PI * progress) - Mathf.PI) + 1f) / 2f;

        return result;
    }
}
