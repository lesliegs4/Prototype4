using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int CurrentScore { get; set; }

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Image _gameOverPanel;
    [SerializeField] private float _fadeTime = 2f;

    public float TimeTillGameOver = 1.5f;

    /// <summary>
    /// Fired any time the player's total coins (score) changes.
    /// </summary>
    public event Action<int> TotalCoinsChanged;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += FadeGame;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= FadeGame;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        SetTotalCoins(CurrentScore);
    }

    private void Start()
    {
        EnsureCoinsPerSpinStickerExists();
        EnsureEliminateFruitPopupExists();
    }

    public void IncreaseScore(int amount)
    {
        SetTotalCoins(CurrentScore + amount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (CurrentScore < amount) return false;

        SetTotalCoins(CurrentScore - amount);
        return true;
    }

    private void SetTotalCoins(int newTotal)
    {
        CurrentScore = Mathf.Max(0, newTotal);
        if (_scoreText != null)
        {
            _scoreText.text = CurrentScore.ToString("0");
        }
        TotalCoinsChanged?.Invoke(CurrentScore);
    }

    private void EnsureCoinsPerSpinStickerExists()
    {
        // The right-side UI placeholder is named this in the scene.
        GameObject holder = GameObject.Find("NextBubbleHolder");
        if (holder == null) return;

        if (holder.GetComponent<CoinsPerSpinSticker>() == null)
        {
            holder.AddComponent<CoinsPerSpinSticker>();
        }
    }

    private void EnsureEliminateFruitPopupExists()
    {
        // GameObject.Find won't locate inactive objects, so search under Canvas including inactive.
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) return;

        Transform popupT = null;
        foreach (Transform t in canvasGO.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == "EliminateFruitPopup")
            {
                popupT = t;
                break;
            }
        }

        GameObject popupGO = popupT != null ? popupT.gameObject : null;
        if (popupGO == null) return;

        if (popupGO.GetComponent<EliminateFruitPopup>() == null)
        {
            popupGO.AddComponent<EliminateFruitPopup>();
        }
    }

    public void GameOver()
    {
        StartCoroutine(ResetGame());
    }

    private IEnumerator ResetGame()
    {
        _gameOverPanel.gameObject.SetActive(true);

        Color startColor = _gameOverPanel.color;
        startColor.a = 0f;
        _gameOverPanel.color = startColor;

        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(0f, 1f, (elapsedTime / _fadeTime));
            startColor.a = newAlpha;
            _gameOverPanel.color = startColor;

            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void FadeGame(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeGameIn());
    }

    private IEnumerator FadeGameIn()
    {
        _gameOverPanel.gameObject.SetActive(true);
        Color startColor = _gameOverPanel.color;
        startColor.a = 1f;
        _gameOverPanel.color = startColor;

        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(1f, 0f, (elapsedTime / _fadeTime));
            startColor.a = newAlpha;
            _gameOverPanel.color = startColor;

            yield return null;
        }

        _gameOverPanel.gameObject.SetActive(false);
    }
}
