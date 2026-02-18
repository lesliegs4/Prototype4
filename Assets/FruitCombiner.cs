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
                        if (GameManager.instance != null)
                        {
                            GameManager.instance.PlayMergeSound();
                        }

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

                        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                        if (rb == null) rb = go.GetComponentInChildren<Rigidbody2D>();
                        GameObject target = rb != null ? rb.gameObject : go;
                        if (rb == null)
                        {
                            Collider2D c = go.GetComponent<Collider2D>();
                            if (c == null) c = go.GetComponentInChildren<Collider2D>();
                            if (c != null) target = c.gameObject;
                        }

                        ColliderInformer informer = target.GetComponent<ColliderInformer>();
                        if (informer == null) informer = target.AddComponent<ColliderInformer>();
                        informer.WasCombinedIn = true;

                        Destroy(collision.gameObject);
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
