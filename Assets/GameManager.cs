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
    [Header("Merge VFX")]
    [SerializeField] private CoinFlyoutEffect _coinFlyoutEffect;
    [SerializeField] private Image _gameOverPanel;
    [SerializeField] private float _fadeTime = 2f;
    [SerializeField] private float _endScreenHoldTime = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioSource _mergeAudioSource;
    [SerializeField] private AudioClip _gameOverSFX;
    [SerializeField] private AudioClip _winSFX;
    [SerializeField] private AudioClip _dropSFX; // Landing SFX
    [SerializeField] private AudioClip _mergeSFX;
    [SerializeField] private float _mergePlaybackSeconds = 0.95f;
    [SerializeField] private float _mergeVolume = 0.65f;

    public float TimeTillGameOver = 1.5f;

    private bool _hasPlacedFruitInContainer = false;
    private bool _isEnding = false;
    private Coroutine _mergeStopRoutine;

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

        EnsureMainAudioSource();
        EnsureMergeAudioSource();

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

        // If a CoinFlyoutEffect is assigned but not configured, default the target to the score text.
        if (_coinFlyoutEffect != null && _scoreText != null)
        {
            _coinFlyoutEffect.SetCoinTarget(_scoreText.rectTransform);
        }
    }

    private void EnsureMainAudioSource()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        if (_audioSource.volume <= 0f) _audioSource.volume = 1f;
        _audioSource.mute = false;
    }

    private void EnsureMergeAudioSource()
    {
        // Keep merge playback isolated so stopping it early doesn't cut off other SFX.
        if (_mergeAudioSource != null) return;
        EnsureMainAudioSource();

        _mergeAudioSource = gameObject.AddComponent<AudioSource>();
        _mergeAudioSource.playOnAwake = false;
        _mergeAudioSource.loop = false;
        _mergeAudioSource.spatialBlend = 0f;

        if (_audioSource != null)
        {
            _mergeAudioSource.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
            _mergeAudioSource.volume = _audioSource.volume;
        }
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

    public RectTransform GetScoreRectTransform()
    {
        return _scoreText != null ? _scoreText.rectTransform : null;
    }

    /// <summary>
    /// Call this at the merge position to spawn merge VFX and animate the coin count increasing.
    /// Falls back to a normal score increase if VFX isn't wired.
    /// </summary>
    public void AwardCoinsFromMerge(Vector3 mergeWorldPos, int amount)
    {
        if (amount <= 0) return;

        if (_coinFlyoutEffect == null)
        {
            IncreaseScore(amount);
            return;
        }

        if (_scoreText != null)
        {
            _coinFlyoutEffect.SetCoinTarget(_scoreText.rectTransform);
        }

        _coinFlyoutEffect.PlayMerge(mergeWorldPos, amount, delta => IncreaseScore(delta));
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
        
        EnsureMainAudioSource();
        if (_audioSource != null && _gameOverSFX != null) _audioSource.PlayOneShot(_gameOverSFX);

        _isEnding = true;
        StartCoroutine(ResetGame(showWin: false));
    }

    public void YouWin()
    {
        if (_isEnding) return;
        EnsureMainAudioSource();
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

public void PlayDropSound()
{
    PlayLandingSound();
}

public void PlayLandingSound()
{
    EnsureMainAudioSource();
    if (_audioSource == null || _dropSFX == null) return;

    _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
    _audioSource.PlayOneShot(_dropSFX);
    _audioSource.pitch = 1f;
}

public void PlayMergeSound()
{
    if (_mergeSFX == null) return;
    EnsureMergeAudioSource();
    if (_mergeAudioSource == null) return;

    if (_mergeStopRoutine != null)
    {
        StopCoroutine(_mergeStopRoutine);
        _mergeStopRoutine = null;
    }

    _mergeAudioSource.Stop();
    _mergeAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
    _mergeAudioSource.volume = Mathf.Clamp01(_mergeVolume);
    _mergeAudioSource.clip = _mergeSFX;
    _mergeAudioSource.Play();
    _mergeAudioSource.pitch = 1f;

    float seconds = Mathf.Clamp(_mergePlaybackSeconds, 0.05f, 10f);
    _mergeStopRoutine = StartCoroutine(StopMergeAfter(seconds));
}

private IEnumerator StopMergeAfter(float seconds)
{
    yield return new WaitForSeconds(seconds);
    if (_mergeAudioSource != null) _mergeAudioSource.Stop();
    _mergeStopRoutine = null;
}

}