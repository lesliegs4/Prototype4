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

                        if (_info.FruitIndex == FruitSelector.instance.Fruits.Length -1)
                        {
                            Destroy(collision.gameObject);
                            Destroy(gameObject);
                        }

                        else
                        {
                            Vector3 middlePosition = (transform.position + collision.transform.position) / 2f;
                            GameObject go = Instantiate(SpawnCombinedFruit(_info.FruitIndex), GameManager.instance.transform);
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

    private GameObject SpawnCombinedFruit(int index)
    {
        GameObject go = FruitSelector.instance.Fruits[index + 1];
        return go;
    }
}
