using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Responsible for managing weapon inventory, unlocks and swapping

public class WeaponCoordinator : MonoBehaviour
{
    #region Variables

    #region Internal

    public static bool switchingWeapons { get; private set; }

    private Quaternion targetWeaponRotation; // The rotation the gun is interpolating towards

    private bool[] unlockedGuns; // MicrowaveGun, ASaltRifle, PepperShotgun
    public static bool knifeUnlocked { get; private set; }
    private Transform[] gunTransforms;
    private int gunCount = 0; // How many guns are unlocked
    private int currentGunIndex = -1; // -1 before weapons are unlocked, afterward, referring to last used gun

    private float weaponStowTimer = 0f;
    private float weaponStowDuration = 0f; // Not decreased
    private float weaponDrawTimer = 0f;
    private float weaponDrawDuration = 0f; // Not decreased

    private Transform currentWeaponTransform; // The weapon we are holding
    private Transform newWeaponTransform; // The weapon we are swapping to

    public GunController[] gunControllers { get; private set; } // Array of weapon controllers

    private float[] weaponRotationSpeeds; // The set of weapon rotation speeds, taken from their respective GunController scripts
    private int newWeaponIndex;

    #endregion

    #region Parameters
    [Header("Configuration")]

    [Tooltip("The offset from the drawn resting position that stowed weapons move towards.")]
    public Vector3 stowedPositionOffset;

    [Tooltip("The curve for the weapon stow animation, should go from 0 to 1.")]
    public AnimationCurve stowAnimationEasing;
    
    [Tooltip("The curve for the weapon draw animation, should go from 1 to 0.")]
    public AnimationCurve drawAnimationEasing;
    #endregion

    #region Components
    [Header("Components")]

    public Movement playerMovement;

    public Transform microwaveGunTransform;
    public Transform aSaltRifleTransform;
    public Transform pepperShotgunTransform;

    [Tooltip("0 - MicrowaveGun, 1 - ASaltRifle, 2 - PepperShotgun")]
    public Transform[] weaponModelTransforms;
    
    [Tooltip("0 - MicrowaveGun, 1 - ASaltRifle, 2 - PepperShotgun")]
    public Transform[] grapplePointTransforms;

    public GunController microwaveGunController;
    public GunController aSaltRifleController;
    public GunController pepperShotgunController;

    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Initialisation

        unlockedGuns = new bool[3];

        // Set all ranged weapons to locked
        for (int i = 0; i < unlockedGuns.Length; i++)
        {
            unlockedGuns[i] = false;
        }

        // Setup arrays for Transforms and GunController scripts
        gunControllers = new GunController[3];
        gunControllers[0] = microwaveGunController;
        gunControllers[1] = aSaltRifleController;
        gunControllers[2] = pepperShotgunController;

        gunTransforms = new Transform[3];
        gunTransforms[0] = microwaveGunTransform;
        gunTransforms[1] = aSaltRifleTransform;
        gunTransforms[2] = pepperShotgunTransform;

        // Set the grapple rotation speed of each of the weapons
        weaponRotationSpeeds = new float[3];
        for (int i = 0; i < unlockedGuns.Length; i++)
        {
            weaponRotationSpeeds[i] = gunControllers[i].grappleRotationSpeed;
        }

        newWeaponIndex = 0;

        #endregion

        // Default weapon
        currentGunIndex = -1;
        switchingWeapons = false;
    }

    // Update is called once per frame
    void Update()
    {
        #region Start Switching Weapons

        // Only allow one swap at a time, not while recoiling
        if (gunCount > 0 && switchingWeapons == false && Movement.isGrappling == false && gunControllers[currentGunIndex].isRecoiling == false)
        {
            if (Input.mouseScrollDelta.y > 0f)
            {
                // Swap to previous weapon
                if (gunCount > 1)
                {
                    // Stow current gun
                    // Swap to previous gun in list

                    // The index of the weapon we will switch to
                    newWeaponIndex = currentGunIndex;

                    do
                    {
                        // Attempt to find a previous weapon
                        newWeaponIndex -= 1;

                        // Loop around if out of range of weapons list
                        if (newWeaponIndex < 0)
                        {
                            newWeaponIndex = unlockedGuns.Length - 1;
                        }
                    }
                    while (unlockedGuns[newWeaponIndex] == false);

                    // Swap to previous weapon
                    SwapToWeapon(newWeaponIndex);
                }
            }
            else if (Input.mouseScrollDelta.y < 0f)
            {
                // Swap to next weapon
                if (gunCount > 1)
                {
                    // Stow current gun
                    // Swap to next gun in list

                    // The index of the weapon we will switch to
                    newWeaponIndex = currentGunIndex;

                    do
                    {
                        // Attempt to find a next weapon
                        newWeaponIndex += 1;

                        // Loop around if out of range of weapons list
                        if (newWeaponIndex > unlockedGuns.Length - 1)
                        {
                            newWeaponIndex = 0;
                        }
                    }
                    while (unlockedGuns[newWeaponIndex] == false);

                    // Swap to next weapon
                    SwapToWeapon(newWeaponIndex);
                }
            }

            // Swap to weapon with number keys
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwapToWeapon(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwapToWeapon(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwapToWeapon(2);
            }
        }

        #endregion

        #region Swap Weapon Animation

        if (switchingWeapons)
        {
            if (weaponStowTimer > 0f)
            {
                // Decrease timer
                weaponStowTimer = Mathf.Clamp(weaponStowTimer - Time.deltaTime, 0f, Mathf.Infinity);

                // Lerp to new point this frame
                Vector3 newPosition = stowedPositionOffset * stowAnimationEasing.Evaluate(1f - (weaponStowTimer / weaponStowDuration));
                currentWeaponTransform.localPosition = newPosition;

                // If timer has ended, snap to position
                if (weaponStowTimer <= 0f)
                {
                    currentWeaponTransform.localPosition = stowedPositionOffset;

                    weaponStowTimer = 0f;
                    weaponStowDuration = 0f;

                    // Hide weapon
                    weaponModelTransforms[currentGunIndex].gameObject.SetActive(false);
                }
            }
            else if (weaponDrawTimer > 0f)
            {
                // Decrease timer
                weaponDrawTimer = Mathf.Clamp(weaponDrawTimer - Time.deltaTime, 0f, Mathf.Infinity);

                // Show weapon
                weaponModelTransforms[newWeaponIndex].gameObject.SetActive(true);

                // Lerp to new point this frame
                Vector3 newPosition = stowedPositionOffset * drawAnimationEasing.Evaluate(1f - (weaponDrawTimer / weaponDrawDuration));
                newWeaponTransform.localPosition = newPosition;

                // If timer has ended, snap to position
                if (weaponDrawTimer <= 0f)
                {
                    newWeaponTransform.localPosition = Vector3.zero;

                    weaponDrawTimer = 0f;
                    weaponDrawDuration = 0f;
                }

                // The weapon swap has ended
                if (weaponDrawTimer <= 0f)
                {
                    switchingWeapons = false;
                }
            }
        }

        #endregion

        #region Weapon Grapple Rotation

        // Rotates all guns at once to ensure they are not misaligned when drawn from the inventory
        for (int i = 0; i < weaponModelTransforms.Length; i++)
        {
            // If the weapon is not currently rotating from a reload 
            if (gunControllers[i].isReloading == false)
            {
                if (Movement.isGrappling)
                {
                    // Rotate towards the grappling point
                    targetWeaponRotation = Quaternion.LookRotation(playerMovement.currentGrapplePoint.position - grapplePointTransforms[i].position);
                    weaponModelTransforms[i].rotation = Quaternion.Lerp(weaponModelTransforms[i].rotation, targetWeaponRotation, Time.deltaTime * weaponRotationSpeeds[i]);
                }
                else
                {
                    // Rotate towards the resting position
                    targetWeaponRotation = weaponModelTransforms[i].parent.localRotation;
                    weaponModelTransforms[i].localRotation = Quaternion.Lerp(weaponModelTransforms[i].localRotation, targetWeaponRotation, Time.deltaTime * weaponRotationSpeeds[i]);

                    // Snaps rotation to neutral if the angle is effectively neutral
                    if (Quaternion.Angle(weaponModelTransforms[i].localRotation, Quaternion.identity) < 0.1f)
                    {
                        weaponModelTransforms[i].localRotation = Quaternion.identity;
                    }
                }
            }
        }

        #endregion
    }

    // Unlock a weapon and attempt to swap to it
    public void UnlockWeapon(int weaponIndex)
    {
        // If the index refers to a valid weapon
        if (weaponIndex >= 0 && weaponIndex <= 2)
        {
            // If the weapon is currently locked and not available to the player
            if (unlockedGuns[weaponIndex] == false)
            {
                unlockedGuns[weaponIndex] = true;
                gunCount += 1;

                // Swap to this new weapon, fails if a weapon switch is currently already happening
                SwapToWeapon(weaponIndex);
            }
        }
    }

    public void UnlockKnife()
    {
        knifeUnlocked = true;
    }

    // Attempt to swap to a weapon, specified by its index
    void SwapToWeapon(int weaponIndex)
    {
        // Do not allow another switch while currently switching weapons
        if (switchingWeapons || unlockedGuns[weaponIndex] == false || weaponIndex == currentGunIndex || UpgradeStationHUD.showUpgradeHUD)
        {
            return;
        }

        // If the index refers to a gun which has been unlocked
        if (weaponIndex >= 0 && weaponIndex <= 2 && unlockedGuns[weaponIndex] == true)
        {
            // Prevent further switching until the animation is complete
            switchingWeapons = true;
            newWeaponIndex = weaponIndex;

            if (currentGunIndex != -1)
            {
                // The weapon being stowed
                weaponStowTimer = gunControllers[currentGunIndex].swapTime;
                weaponStowDuration = gunControllers[currentGunIndex].swapTime;
                
                currentWeaponTransform = gunTransforms[currentGunIndex];
                
                gunControllers[currentGunIndex].isCurrentlyEquipped = false;
            }

            // The weapon being drawn
            weaponDrawTimer = gunControllers[weaponIndex].swapTime;
            weaponDrawDuration = gunControllers[weaponIndex].swapTime;

            newWeaponTransform = gunTransforms[weaponIndex];

            gunControllers[weaponIndex].isCurrentlyEquipped = true;

            // Swap to gun
            currentGunIndex = weaponIndex;

            // Assign grappling points with player movement script
            playerMovement.weaponTransform = weaponModelTransforms[currentGunIndex];
            playerMovement.weaponGrapplePointTransform = grapplePointTransforms[currentGunIndex];
        }
    }
}
