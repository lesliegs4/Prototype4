using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderInformer : MonoBehaviour
{
    public bool WasCombinedIn { get; set; }

    private bool _hasCollided;
    private const float MinLandingImpact = 0.15f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_hasCollided)
        {
            _hasCollided = true;

            // Only play landing sound for thrown fruits (not merged spawns),
            // and avoid "instant contact" sounding like a landing.
            if (!WasCombinedIn && GameManager.instance != null)
            {
                if (collision != null && collision.relativeVelocity.sqrMagnitude >= (MinLandingImpact * MinLandingImpact))
                {
                    GameManager.instance.PlayLandingSound();
                }
            }

            if (!WasCombinedIn)
            {
                ThrowFruitController.instance.CanThrow = true;
                GameObject next = FruitSelector.instance.PickRandomFruitForThrow();
                if (next != null)
                {
                    ThrowFruitController.instance.SpawnAFruit(next);
                }
            }
            Destroy(this);
        }
    }
}
