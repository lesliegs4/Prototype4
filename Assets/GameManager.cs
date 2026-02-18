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
    [SerializeField] private float _endScreenHoldTime = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _gameOverSFX;
    [SerializeField] private AudioClip _winSFX;

    public float TimeTillGameOver = 1.5f;

    private bool _hasPlacedFruitInContainer = false;
    private bool _isEnding = false;

    /// <summary>
    /// Fired any time the player's total coins (score) changes.
    /// </summary>
    public event Action<int> TotalCoinsChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (_gameOverPanel != null)
        {
            // Ensure the game-over fade panel is hidden at game start.
            Color c = _gameOverPanel.color;
            c.a = 0f;
            _gameOverPanel.color = c;
            _gameOverPanel.gameObject.SetActive(false);

            // Ensure only the Game Over image is enabled by default.
            Transform gameOverT = _gameOverPanel.transform.Find("GameOverImage");
            if (gameOverT != null) gameOverT.gameObject.SetActive(true);
            Transform youWinT = _gameOverPanel.transform.Find("YouWinImage");
            if (youWinT != null) youWinT.gameObject.SetActive(false);
        }

        SetTotalCoins(CurrentScore);
    }

    private void Start()
    {
        EnsureCoinsPerSpinStickerExists();
        EnsureEliminateFruitPopupExists();
        EnsureKnobUIControllerExists();
    }

    private void Update()
    {
        if (_isEnding) return;
        if (!_hasPlacedFruitInContainer) return;

        Transform container = ThrowFruitController.instance != null ? ThrowFruitController.instance.FruitContainer : null;
        if (container == null) return;

        // "No more fruits left" = no FruitInfo present under the container.
        FruitInfo anyInfo = container.GetComponentInChildren<FruitInfo>(true);
        if (anyInfo == null)
        {
            YouWin();
        }
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

    private void EnsureKnobUIControllerExists()
    {
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) return;

        Transform knobT = null;
        foreach (Transform t in canvasGO.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == "KnobUI")
            {
                knobT = t;
                break;
            }
        }

        if (knobT == null) return;
        GameObject knobGO = knobT.gameObject;

        if (knobGO.GetComponent<KnobUIController>() == null)
        {
            knobGO.AddComponent<KnobUIController>();
        }
    }

    public void GameOver()
    {
        if (_isEnding) return;
        
        if (_audioSource != null && _gameOverSFX != null) _audioSource.PlayOneShot(_gameOverSFX);

        _isEnding = true;
        StartCoroutine(ResetGame(showWin: false));
    }

    public void YouWin()
    {
        if (_isEnding) return;
        if (_audioSource != null && _winSFX != null) _audioSource.PlayOneShot(_winSFX);
        _isEnding = true;
        StartCoroutine(ResetGame(showWin: true));
    }

    public void NotifyFruitPlacedInContainer()
    {
        _hasPlacedFruitInContainer = true;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private IEnumerator ResetGame(bool showWin)
    {
        if (_gameOverPanel == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        _gameOverPanel.gameObject.SetActive(true);

        Transform gameOverT = _gameOverPanel.transform.Find("GameOverImage");
        if (gameOverT != null) gameOverT.gameObject.SetActive(!showWin);
        Transform youWinT = _gameOverPanel.transform.Find("YouWinImage");
        if (youWinT != null) youWinT.gameObject.SetActive(showWin);

        Image gameOverImg = gameOverT != null ? gameOverT.GetComponent<Image>() : null;
        Image youWinImg = youWinT != null ? youWinT.GetComponent<Image>() : null;

        Color startColor = _gameOverPanel.color;
        startColor.a = 0f;
        _gameOverPanel.color = startColor;

        // Fade the active message image along with the panel.
        SetAlpha(gameOverImg, 0f);
        SetAlpha(youWinImg, 0f);

        float elapsedTime = 0f;
        while(elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;

            float newAlpha = Mathf.Lerp(0f, 1f, (elapsedTime / _fadeTime));
            startColor.a = newAlpha;
            _gameOverPanel.color = startColor;

            if (showWin)
            {
                SetAlpha(youWinImg, newAlpha);
            }
            else
            {
                SetAlpha(gameOverImg, newAlpha);
            }

            yield return null;
        }

        if (_endScreenHoldTime > 0f)
        {
            yield return new WaitForSeconds(_endScreenHoldTime);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
