using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Basic interactive item which unlocks a weapon/ability for the player when picked up. Microwave gun starts the run.

public class UnlockItem : InteractiveObject
{
    public enum ItemType
    {
        MicrowaveGun,
        ASaltRifle,
        PepperShotgun,
        ThrowingKnife,
        GrappleAbility
    }

    public ItemType unlockItem;
    public WeaponCoordinator weaponCoordinator;
    private Movement playerMovement;
    private WaveManager waveManager;

    private void Start()
    {
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<Movement>();
        waveManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<WaveManager>();

        base.Configure();
    }

    public override int GetFibreCost()
    {
        // Calculate the cost with difficulty scaling here
        return baseCost;
    }

    public override void Interact()
    {
        switch (unlockItem)
        {
            case ItemType.MicrowaveGun:
                weaponCoordinator.UnlockWeapon(0);
                waveManager.StartWaveCycle();
                break;
            case ItemType.ASaltRifle:
                weaponCoordinator.UnlockWeapon(1);
                break;
            case ItemType.PepperShotgun:
                weaponCoordinator.UnlockWeapon(2);
                break;
            case ItemType.ThrowingKnife:
                weaponCoordinator.UnlockKnife();
                break;
            case ItemType.GrappleAbility:
                playerMovement.UnlockGrapple();
                break;
        }

        Destroy(gameObject);
    }
}
