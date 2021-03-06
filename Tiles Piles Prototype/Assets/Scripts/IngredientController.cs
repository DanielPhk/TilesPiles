﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientController : MonoBehaviour
{

    GridManager grid;
    
    void Start()
    {
        grid = GridManager.instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Ingredient"))
        {
            grid.rotDone = true;
        }
    }

}
