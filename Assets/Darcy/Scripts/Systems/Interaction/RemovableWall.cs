using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Author: Darcy Matheson
// Purpose: Interactive object that is destroyed when fibre is spent on it

public class RemovableWall : InteractiveObject
{
    public void Start()
    {
        base.Configure();
    }

    public override int GetFibreCost()
    {
        // Calculate the cost with difficulty scaling here
        return baseCost;
    }

    public override void Interact()
    {
        Destroy(gameObject);
    }
}
