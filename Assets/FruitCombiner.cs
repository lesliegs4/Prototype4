using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitCombiner : MonoBehaviour
{
    private int _layerIndex;

    private FruitInfo _info;

    private void Awake()
    {
        _info = GetComponent<FruitInfo>();
        _layerIndex = gameObject.layer;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == _layerIndex)
        {
            FruitInfo info = collision.gameObject.GetComponent<FruitInfo>();
            if (info != null)
            {
                if (info.FruitIndex == _info.FruitIndex)
                {
                    int thisID = gameObject.GetInstanceID();
                    int otherID = collision.gameObject.GetInstanceID();

                    if (thisID > otherID)
                    {
                        GameManager.instance.IncreaseScore(_info.PointsWhenAnnihilated);

                        // Spawn the next fruit in the progression at the midpoint.
                        // If we're at the last index (e.g., peach), wrap to the first (banana).
                        Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;

                        int nextIndex = _info.FruitIndex + 1;
                        int len = FruitSelector.instance.Fruits.Length;
                        if (len <= 0) return;
                        if (nextIndex >= len) nextIndex = 0;

                        Transform parent = ThrowFruitController.instance != null ? ThrowFruitController.instance.FruitContainer : null;
                        GameObject go = Instantiate(FruitSelector.instance.Fruits[nextIndex], parent);
                        go.transform.position = middlePosition;

                        ColliderInformer informer = go.GetComponent<ColliderInformer>();
                        if (informer != null)
                        {
                            informer.WasCombinedIn = true;
                        }

                        Destroy(collision.gameObject);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
