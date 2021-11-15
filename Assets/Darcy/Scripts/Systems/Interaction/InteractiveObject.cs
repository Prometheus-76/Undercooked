using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Base class for all interactive objects, facilitates UI pop-up and fibre expenses

public class InteractiveObject : MonoBehaviour
{
    public int baseCost;
    public float difficultyScaling;
    public float interactDuration;
    public string interactPrompt;
    public bool isInteractable; // Only relevant on Upgrade Station script

    public void Configure()
    {
        isInteractable = true;
    }

    public virtual int GetFibreCost()
    {
        return 0;
    }

    public virtual void Interact()
    {

    }
}
