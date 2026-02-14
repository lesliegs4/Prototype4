using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSelector : MonoBehaviour
{
    public static FruitSelector instance;

    public GameObject[] Fruits;
    public GameObject[] NoPhysicsFruits;
    public int HighestStartingIndex = 3;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public GameObject PickRandomFruitForThrow()
    {
        int randomIndex = Random.Range(0, HighestStartingIndex + 1);

        if (randomIndex < NoPhysicsFruits.Length)
        {
            GameObject randomFruit = NoPhysicsFruits[randomIndex];
            return randomFruit;
        }

        return null;
    }
}
