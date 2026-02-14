using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FruitSelector : MonoBehaviour
{
    public static FruitSelector instance;

    public GameObject[] Fruits;
    public GameObject[] NoPhysicsFruits;
    public int HighestStartingIndex = 3;

    [SerializeField] private Image _nextFruitImage;

    private int _nextIndex = -1;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        EnsureNextRolled();
        UpdateNextPreview();
    }

    public GameObject PickRandomFruitForThrow()
    {
        if (NoPhysicsFruits == null || NoPhysicsFruits.Length == 0)
        {
            Debug.LogError($"{nameof(FruitSelector)}: NoPhysicsFruits is empty/unassigned.");
            return null;
        }

        EnsureNextRolled();

        int currentIndex = _nextIndex;
        GameObject currentFruit = (currentIndex >= 0 && currentIndex < NoPhysicsFruits.Length)
            ? NoPhysicsFruits[currentIndex]
            : null;

        // Queue up the next one and refresh the UI preview.
        _nextIndex = RollIndex();
        UpdateNextPreview();

        return currentFruit;
    }

    private void EnsureNextRolled()
    {
        if (_nextIndex < 0)
        {
            _nextIndex = RollIndex();
        }
    }

    private int RollIndex()
    {
        int max = Mathf.Min(HighestStartingIndex, (NoPhysicsFruits?.Length ?? 0) - 1);
        if (max < 0) return -1;
        return Random.Range(0, max + 1);
    }

    private void UpdateNextPreview()
    {
        if (_nextFruitImage == null) return; // No preview UI in this scene; that's fine.

        if (_nextIndex < 0 || NoPhysicsFruits == null || _nextIndex >= NoPhysicsFruits.Length)
        {
            _nextFruitImage.enabled = false;
            return;
        }

        GameObject nextPrefab = NoPhysicsFruits[_nextIndex];
        if (nextPrefab == null)
        {
            _nextFruitImage.enabled = false;
            return;
        }

        // Pull the sprite (and color tint) from the next circle prefab.
        SpriteRenderer sr = nextPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            _nextFruitImage.enabled = false;
            return;
        }

        _nextFruitImage.enabled = true;
        _nextFruitImage.sprite = sr.sprite;
        _nextFruitImage.color = sr.color; // preserves your circle color if you tint sprites
        _nextFruitImage.preserveAspect = true;
    }
}
