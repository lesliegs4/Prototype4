using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinFlyoutEffect : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private ParticleSystem _mergeBurstPrefab;
    [SerializeField] private Image _flyingCoinPrefab;

    [Header("Scene References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private RectTransform _canvasRoot;
    [SerializeField] private RectTransform _coinTarget;

    [Header("Motion")]
    [SerializeField] private float _popUpPixels = 80f;
    [SerializeField] private float _spreadPixels = 40f;
    [SerializeField] private float _flightTime = 0.7f;
    [SerializeField] private AnimationCurve _ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Coin Visuals")]
    [SerializeField] private float _coinScale = 1f / 60f;

    public void SetCoinTarget(RectTransform target)
    {
        _coinTarget = target;
    }

    public void PlayMerge(Vector3 mergeWorldPos, int coinsGained, Action<int> addCoinsOverTime)
    {
        if (_mergeBurstPrefab != null)
        {
            ParticleSystem ps = Instantiate(_mergeBurstPrefab, mergeWorldPos, Quaternion.identity);
            var main = ps.main;
            main.loop = false;
            ps.Play(true);
            Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.5f);
        }

        if (_flyingCoinPrefab == null) return;
        EnsureRefs();

        if (_canvasRoot == null || _worldCamera == null || _coinTarget == null) return;
        StartCoroutine(FlyCoinRoutine(mergeWorldPos, coinsGained, addCoinsOverTime));
    }

    private void EnsureRefs()
    {
        if (_worldCamera == null) _worldCamera = Camera.main;

        if (_canvas == null)
        {
            GameObject canvasGO = GameObject.Find("Canvas");
            if (canvasGO != null) _canvas = canvasGO.GetComponent<Canvas>();
            if (_canvas == null) _canvas = FindFirstObjectByType<Canvas>();
        }

        if (_canvasRoot == null && _canvas != null)
        {
            _canvasRoot = _canvas.GetComponent<RectTransform>();
        }
    }

    private IEnumerator FlyCoinRoutine(Vector3 mergeWorldPos, int coinsGained, Action<int> addCoinsOverTime)
    {
        Image coin = Instantiate(_flyingCoinPrefab, _canvasRoot);
        RectTransform coinRT = coin.rectTransform;
        coin.raycastTarget = false;
        coinRT.localScale = Vector3.one * Mathf.Clamp(_coinScale, 0.0001f, 10f);
        coin.transform.SetAsLastSibling();

        Camera uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (_canvas.worldCamera != null ? _canvas.worldCamera : _worldCamera)
            : null;

        Vector2 startLocal = WorldToCanvasLocal(mergeWorldPos, uiCam);
        coinRT.anchoredPosition = startLocal;

        Vector2 targetLocal = RectTransformToCanvasLocal(_coinTarget, uiCam);

        Vector2 random = new Vector2(
            UnityEngine.Random.Range(-_spreadPixels, _spreadPixels),
            UnityEngine.Random.Range(-_spreadPixels, _spreadPixels)
        );

        Vector2 control = startLocal + new Vector2(0f, _popUpPixels) + random;

        float t = 0f;
        int lastAdded = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, _flightTime);
            float e = _ease.Evaluate(Mathf.Clamp01(t));

            Vector2 p0 = startLocal;
            Vector2 p1 = control;
            Vector2 p2 = targetLocal;

            Vector2 pos = (1 - e) * (1 - e) * p0 + 2 * (1 - e) * e * p1 + e * e * p2;
            coinRT.anchoredPosition = pos;

            int shouldHaveAdded = Mathf.FloorToInt(coinsGained * e);
            int delta = shouldHaveAdded - lastAdded;
            if (delta > 0)
            {
                addCoinsOverTime?.Invoke(delta);
                lastAdded += delta;
            }

            yield return null;
        }

        int remaining = coinsGained - lastAdded;
        if (remaining > 0) addCoinsOverTime?.Invoke(remaining);

        Destroy(coin.gameObject);
    }

    private Vector2 WorldToCanvasLocal(Vector3 worldPos, Camera uiCam)
    {
        Vector3 screen = _worldCamera.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRoot,
            screen,
            uiCam,
            out Vector2 local
        );

        return local;
    }

    private Vector2 RectTransformToCanvasLocal(RectTransform target, Camera uiCam)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, target.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRoot,
            screen,
            uiCam,
            out Vector2 local
        );

        return local;
    }
}

