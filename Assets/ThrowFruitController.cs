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
}
