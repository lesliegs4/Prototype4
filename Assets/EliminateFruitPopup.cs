using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup panel that lets the player choose which fruit type to eliminate.
/// Place a disabled UI Image named "EliminateFruitPopup" under the Canvas (similar to FadePanel).
/// </summary>
public class EliminateFruitPopup : MonoBehaviour
{
    public static EliminateFruitPopup instance;

    private Action<int> _onSelected;
    private bool _built;

    // Runtime-built UI refs
    private RectTransform _dialogRT;
    private Transform _gridRoot;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        // Safety: ensure static instance is set even if created/enabled later.
        if (instance == null) instance = this;
    }

    public static EliminateFruitPopup GetOrFindInstance()
    {
        if (instance != null) return instance;

        // Works for inactive objects too.
        EliminateFruitPopup[] all = Resources.FindObjectsOfTypeAll<EliminateFruitPopup>();
        foreach (EliminateFruitPopup p in all)
        {
            if (p == null) continue;
            if (!p.gameObject.scene.IsValid()) continue;
            instance = p;
            return instance;
        }

        return null;
    }

    public void Show(Action<int> onSelected)
    {
        _onSelected = onSelected;
        if (!_built) BuildUI();
        RefreshButtons();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        _built = true;

        // Dim background
        Image bg = GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = true;
        }

        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null)
        {
            rootRT = gameObject.AddComponent<RectTransform>();
        }
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(2000, 2000);

        // Dialog
        GameObject dialog = new GameObject("Dialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialog.transform.SetParent(transform, false);
        _dialogRT = dialog.GetComponent<RectTransform>();
        _dialogRT.anchorMin = new Vector2(0.5f, 0.5f);
        _dialogRT.anchorMax = new Vector2(0.5f, 0.5f);
        _dialogRT.anchoredPosition = Vector2.zero;
        _dialogRT.sizeDelta = new Vector2(650, 420);

        Image dialogImg = dialog.GetComponent<Image>();
        dialogImg.color = new Color(1f, 1f, 1f, 0.95f);
        dialogImg.raycastTarget = true;

        // Title
        TextMeshProUGUI title = CreateTMP("Title", dialog.transform, "Select a fruit to eliminate");
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 36;
        title.rectTransform.anchorMin = new Vector2(0, 1);
        title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.pivot = new Vector2(0.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -18);
        title.rectTransform.sizeDelta = new Vector2(0, 60);

        // Grid root
        GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(dialog.transform, false);
        RectTransform gridRT = grid.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = new Vector2(0, -10);
        gridRT.sizeDelta = new Vector2(560, 260);
        _gridRoot = grid.transform;

        GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(90, 90);
        glg.spacing = new Vector2(12, 12);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.MiddleCenter;

        // Cancel
        Button cancel = CreateButton(dialog.transform, "Cancel");
        RectTransform cancelRT = cancel.GetComponent<RectTransform>();
        cancelRT.anchorMin = new Vector2(0.5f, 0);
        cancelRT.anchorMax = new Vector2(0.5f, 0);
        cancelRT.pivot = new Vector2(0.5f, 0);
        cancelRT.anchoredPosition = new Vector2(0, 18);
        cancelRT.sizeDelta = new Vector2(200, 60);
        cancel.onClick.AddListener(Hide);

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (_gridRoot == null) return;

        // Clear existing buttons (dynamic per current container contents).
        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_gridRoot.GetChild(i).gameObject);
        }

        FruitSelector selector = FruitSelector.instance;
        if (selector == null || selector.Fruits == null) return;

        HashSet<int> present = GetPresentFruitIndices();
        if (present.Count == 0)
        {
            // If nothing is in the container yet, show nothing (player can cancel).
            return;
        }

        List<int> presentList = new List<int>(present);
        presentList.Sort();

        Debug.Log($"{nameof(EliminateFruitPopup)}: Present fruit indices in container: {string.Join(",", presentList)}");

        foreach (int fruitIndex in presentList)
        {
            if (fruitIndex < 0 || fruitIndex >= selector.Fruits.Length)
            {
                Debug.LogWarning($"{nameof(EliminateFruitPopup)}: FruitIndex {fruitIndex} present but out of range for FruitSelector.Fruits (len={selector.Fruits.Length}).");
                continue;
            }

            (Sprite sprite, Color color) = GetFruitIcon(selector.Fruits[fruitIndex]);

            Button b = CreateIconButton(_gridRoot, sprite, color);
            b.onClick.AddListener(() =>
            {
                Hide();
                _onSelected?.Invoke(fruitIndex);
            });
        }
    }

    private HashSet<int> GetPresentFruitIndices()
    {
        HashSet<int> present = new HashSet<int>();

        Transform container = ThrowFruitController.instance != null ? ThrowFruitController.instance.FruitContainer : null;
        if (container == null) return present;

        // Scan all FruitInfo components under the container (not just direct children),
        // since some prefabs may place FruitInfo on a nested child transform.
        FruitInfo[] infos = container.GetComponentsInChildren<FruitInfo>(true);
        foreach (FruitInfo info in infos)
        {
            if (info == null) continue;
            present.Add(info.FruitIndex);
        }

        return present;
    }

    private (Sprite sprite, Color color) GetFruitIcon(GameObject prefab)
    {
        if (prefab == null) return (null, Color.white);

        SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return (null, Color.white);
        return (sr.sprite, sr.color);
    }

    private TextMeshProUGUI CreateTMP(string name, Transform parent, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateButton(Transform parent, string label)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();

        TextMeshProUGUI tmp = CreateTMP("Label", go.transform, label);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 30;
        tmp.rectTransform.anchorMin = Vector2.zero;
        tmp.rectTransform.anchorMax = Vector2.one;
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }

    private Button CreateIconButton(Transform parent, Sprite sprite, Color tint)
    {
        GameObject go = new GameObject("FruitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = tint;
        img.preserveAspect = true;
        img.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        return btn;
    }
}

