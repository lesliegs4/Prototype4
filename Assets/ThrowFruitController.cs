using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowFruitController : MonoBehaviour
{
    public static ThrowFruitController instance;

    public GameObject CurrentFruit { get; set; }
    [SerializeField] private Transform _fruitTransform;
    [SerializeField] private Transform _parentAfterThrow;
    [SerializeField] private FruitSelector _selector;
    [SerializeField] private float _edgePadding = 0.12f;

    private PlayerController _playerController;

    private Rigidbody2D _rb;
    private CircleCollider2D _circleCollider;

    public Bounds Bounds { get; private set; }

    public bool CanThrow { get; set; } = true;

    public Transform FruitContainer => _parentAfterThrow;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        _playerController = GetComponent<PlayerController>();

        SpawnAFruit(_selector.PickRandomFruitForThrow());
    }

    private void Update()
    {
        if (UserInput.IsThrowPressed && CanThrow)
        {
            SpriteIndex index = CurrentFruit.GetComponent<SpriteIndex>();
            Quaternion rot = CurrentFruit.transform.rotation;

            GameObject go = Instantiate(FruitSelector.instance.Fruits[index.Index], CurrentFruit.transform.position, rot);
            go.transform.SetParent(_parentAfterThrow);

            Destroy(CurrentFruit);

            CanThrow = false;

            if (GameManager.instance != null)
            {
                GameManager.instance.NotifyFruitPlacedInContainer();
            }
        }
    }

    public void SpawnAFruit(GameObject fruit)
    {
        // Spawn exactly at the throw transform, ignoring any saved prefab offsets.
        GameObject go = Instantiate(fruit, _fruitTransform.position, Quaternion.identity, _fruitTransform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        CurrentFruit = go;
        _circleCollider = CurrentFruit.GetComponent<CircleCollider2D>();
        Bounds = _circleCollider.bounds;

        _playerController.ChangeBoundary(_edgePadding);
    }

    public void EliminateAllFruitsOfIndex(int fruitIndex)
    {
        if (_parentAfterThrow == null) return;

        // Some fruit prefabs may put FruitInfo on a nested child; destroy the object that
        // actually owns FruitInfo to ensure the whole fruit is removed.
        FruitInfo[] infos = _parentAfterThrow.GetComponentsInChildren<FruitInfo>(true);
        HashSet<GameObject> toDestroy = new HashSet<GameObject>();
        foreach (FruitInfo info in infos)
        {
            if (info == null) continue;
            if (info.FruitIndex != fruitIndex) continue;
            toDestroy.Add(info.gameObject);
        }

        int eliminated = 0;
        foreach (GameObject go in toDestroy)
        {
            if (go == null) continue;
            Destroy(go);
            eliminated++;
        }

        Debug.Log($"{nameof(ThrowFruitController)}: Eliminated {eliminated} fruits of index {fruitIndex}.");
    }
}
