using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Interactive object that allows the player to interact, summoning the upgrade/refill menu

public class UpgradeStation : InteractiveObject
{
    public void Start()
    {
        isInteractable = false;
    }

    public override int GetFibreCost()
    {
        // Calculate the cost with difficulty scaling here
        return baseCost;
    }

    public override void Interact()
    {
        PlayerStats.gamePaused = true;
        UpgradeStationHUD.showUpgradeHUD = true;
        UserInterfaceHUD.showHUD = false;

        // Make the cursor visible and confine it to the window
        Cursor.lockState = CursorLockMode.Confined;
    }
}
