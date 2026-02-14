using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInformer : MonoBehaviour
{
    public bool WasCombinedIn { get; set; }

    private bool _hasCollided;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hasCollided && !WasCombinedIn)
        {
            _hasCollided = true;
            ThrowFruitController.instance.CanThrow = true;
            GameObject next = FruitSelector.instance.PickRandomFruitForThrow();
            if (next != null)
            {
                ThrowFruitController.instance.SpawnAFruit(next);
            }
            Destroy(this);
        }
    }
}
