using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to the coin "sticker" UI object on the right.
/// Shows a Coins-Per-Spin value that doubles every time the player's total coins increase by 5.
/// Clicking the sticker spends that many coins from the total.
/// </summary>
public class CoinsPerSpinSticker : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _coinsPerSpinText;

    [Header("Tuning")]
    [SerializeField] private int _startingCoinsPerSpin = 1;
    [SerializeField] private int _coinsEarnedPerDouble = 5;
    [SerializeField] private float _textYOffset = 20f;

    private int _coinsPerSpin;
    private int _prevTotalCoins;
    private int _earnedCoinsAccumulator;

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.TotalCoinsChanged += HandleTotalCoinsChanged;
        }
    }

    private void Start()
    {
        EnsureTextExists();

        _coinsPerSpin = Mathf.Max(1, _startingCoinsPerSpin);
        _prevTotalCoins = GameManager.instance != null ? GameManager.instance.CurrentScore : 0;
        _earnedCoinsAccumulator = 0;
        RefreshText();
    }

    private void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.TotalCoinsChanged -= HandleTotalCoinsChanged;
        }
    }

    private void HandleTotalCoinsChanged(int newTotalCoins)
    {
        int delta = newTotalCoins - _prevTotalCoins;
        _prevTotalCoins = newTotalCoins;

        // Only count positive deltas as "coins gained" for doubling.
        if (delta <= 0) return;

        _earnedCoinsAccumulator += delta;

        if (_coinsEarnedPerDouble <= 0) _coinsEarnedPerDouble = 5;
        int doubles = _earnedCoinsAccumulator / _coinsEarnedPerDouble;
        if (doubles <= 0) return;

        _earnedCoinsAccumulator %= _coinsEarnedPerDouble;

        for (int i = 0; i < doubles; i++)
        {
            // Avoid overflow; cap at int.MaxValue.
            if (_coinsPerSpin > int.MaxValue / 2)
            {
                _coinsPerSpin = int.MaxValue;
                break;
            }
            _coinsPerSpin *= 2;
        }

        RefreshText();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ShowEliminatePopup();
    }

    private void ShowEliminatePopup()
    {
        Debug.Log($"{nameof(CoinsPerSpinSticker)}: Sticker clicked. CoinsPerSpin={_coinsPerSpin} TotalCoins={(GameManager.instance != null ? GameManager.instance.CurrentScore : -1)}");

        EliminateFruitPopup popup = EliminateFruitPopup.GetOrFindInstance();
        if (popup == null)
        {
            Debug.LogWarning($"{nameof(CoinsPerSpinSticker)}: Could not find {nameof(EliminateFruitPopup)} in scene.");
            return;
        }

        popup.Show(fruitIndex => PurchaseSpinAndEliminate(fruitIndex));
    }

    // Separate function: user action triggers this purchase + elimination.
    private void PurchaseSpinAndEliminate(int fruitIndex)
    {
        if (GameManager.instance == null) return;

        // Only decrease the total by _coinsPerSpin.
        if (!GameManager.instance.TrySpendCoins(_coinsPerSpin))
        {
            Debug.Log($"{nameof(CoinsPerSpinSticker)}: Not enough coins to purchase spin. Cost={_coinsPerSpin} Total={GameManager.instance.CurrentScore}");
            return;
        }

        Debug.Log($"{nameof(CoinsPerSpinSticker)}: Purchased spin for {_coinsPerSpin} coins. NewTotal={GameManager.instance.CurrentScore}. Eliminating fruitIndex={fruitIndex}");

        if (ThrowFruitController.instance != null)
        {
            ThrowFruitController.instance.EliminateAllFruitsOfIndex(fruitIndex);
        }
    }

    private void RefreshText()
    {
        if (_coinsPerSpinText == null) return;
        _coinsPerSpinText.text = _coinsPerSpin.ToString("0");
    }

    private void EnsureTextExists()
    {
        if (_coinsPerSpinText == null)
        {
            _coinsPerSpinText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (_coinsPerSpinText == null)
        {
            // Create an overlay TMP text child at runtime.
            GameObject textGO = new GameObject("CoinsPerSpinText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(transform, false);

            RectTransform rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            _coinsPerSpinText = textGO.GetComponent<TextMeshProUGUI>();
            _coinsPerSpinText.raycastTarget = false;
            _coinsPerSpinText.alignment = TextAlignmentOptions.Center;
            _coinsPerSpinText.fontSize = 72;
            _coinsPerSpinText.enableAutoSizing = true;
            _coinsPerSpinText.fontSizeMin = 24;
            _coinsPerSpinText.fontSizeMax = 96;
        }

        // Nudge the amount up so it doesn't block sticker text.
        if (_coinsPerSpinText != null)
        {
            RectTransform textRT = _coinsPerSpinText.rectTransform;
            textRT.anchoredPosition = new Vector2(textRT.anchoredPosition.x, _textYOffset);
        }

        // If the sticker has an Image, make sure it receives clicks.
        Image img = GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }
}

