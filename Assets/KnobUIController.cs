using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the knob UI animation under the Canvas.
/// Keeps it hidden unless explicitly played.
/// </summary>
public class KnobUIController : MonoBehaviour
{
    public static KnobUIController instance;

    [Header("Alignment")]
    [SerializeField] private bool _lockToWorldPosition = true;
    // Calibrated for this scene: where the knob center is in world units (orthographic camera).
    // This keeps the overlay aligned across different build resolutions/aspect ratios.
    [SerializeField] private Vector3 _knobWorldPosition = new Vector3(-0.18f, -2.96f, 0f);

    [Header("Presentation")]
    [SerializeField] private float _playScaleMultiplier = 1.15f;

    [SerializeField] private Animator _animator;
    [SerializeField] private Image _image;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _knobTurningSFX;
    [SerializeField] private float _knobTurningVolume = 0.65f;

    [SerializeField] private float _fallbackDurationSeconds = 0.5f;
    [SerializeField] private float _spinDegrees = -360f;

    private Coroutine _playRoutine;
    private Color _playColor = Color.white;
    private RectTransform _rt;
    private RectTransform _canvasRT;
    private Vector3 _baseScale = Vector3.one;

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

        if (_animator == null) _animator = GetComponent<Animator>();
        if (_image == null) _image = GetComponent<Image>();
        EnsureAudioSource();
        _rt = GetComponent<RectTransform>();
        if (_rt != null) _baseScale = _rt.localScale;

        Canvas canvas = GetComponentInParent<Canvas>();
        _canvasRT = canvas != null ? canvas.transform as RectTransform : null;

        // Cache a sensible "visible" color for play. If the scene tint is fully transparent
        // (and often black), use white so the sprite isn't rendered as a black silhouette.
        if (_image != null)
        {
            _playColor = _image.color;
            if (_playColor.a <= 0.001f) _playColor.a = 1f;
            if (_playColor.r <= 0.001f && _playColor.g <= 0.001f && _playColor.b <= 0.001f)
            {
                _playColor.r = 1f;
                _playColor.g = 1f;
                _playColor.b = 1f;
            }
        }

        Hide();
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f; // UI sound should be 2D
        _audioSource.volume = Mathf.Clamp01(_knobTurningVolume);
    }

    private void StartTurningSound()
    {
        if (_audioSource == null) return;
        if (_knobTurningSFX == null) return;

        _audioSource.volume = Mathf.Clamp01(_knobTurningVolume);
        if (_audioSource.clip != _knobTurningSFX) _audioSource.clip = _knobTurningSFX;
        if (!_audioSource.isPlaying) _audioSource.Play();
    }

    private void StopTurningSound()
    {
        if (_audioSource == null) return;
        if (_audioSource.isPlaying) _audioSource.Stop();
    }

    public static KnobUIController GetOrFindInstance()
    {
        if (instance != null) return instance;

        KnobUIController[] all = Resources.FindObjectsOfTypeAll<KnobUIController>();
        foreach (KnobUIController k in all)
        {
            if (k == null) continue;
            if (!k.gameObject.scene.IsValid()) continue;
            instance = k;
            return instance;
        }

        return null;
    }

    public void PlayOnce()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
        StopTurningSound();

        UpdateAlignment();
        Show();

        float duration = GetClipDurationSeconds();
        _playRoutine = StartCoroutine(PlayAndHide(duration));
    }

    private float GetClipDurationSeconds()
    {
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            float max = 0f;
            var clips = _animator.runtimeAnimatorController.animationClips;
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null) max = Mathf.Max(max, clips[i].length);
                }
            }
            if (max > 0f) return max;
        }

        return Mathf.Max(0.05f, _fallbackDurationSeconds);
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Hide();
        _playRoutine = null;
    }

    private IEnumerator PlayAndHide(float seconds)
    {
        float duration = Mathf.Max(0.05f, seconds);
        StartTurningSound();

        // Try playing the Animator first (works if the clip targets UI.Image sprite).
        Sprite initialSprite = _image != null ? _image.sprite : null;

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Rebind();
            _animator.Update(0f);
            _animator.Play(0, 0, 0f);
        }

        // Wait a bit to see whether the UI Image sprite actually changes.
        // (If the clip is accidentally bound to SpriteRenderer, Image won't change.)
        yield return new WaitForSeconds(Mathf.Min(0.12f, duration * 0.5f));

        bool spriteIsAnimating = (_image != null && _image.sprite != initialSprite);

        if (spriteIsAnimating)
        {
            // Let the animation finish, then hide.
            float remaining = Mathf.Max(0f, duration - 0.12f);
            if (remaining > 0f) yield return new WaitForSeconds(remaining);

            Hide();
            _playRoutine = null;
            yield break;
        }

        // Fallback: drive a visible rotation spin in code.
        if (_animator != null) _animator.enabled = false;

        float elapsed = 0f;
        float startZ = transform.localEulerAngles.z;
        float endZ = startZ + _spinDegrees;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float z = Mathf.Lerp(startZ, endZ, t);
            transform.localEulerAngles = new Vector3(0f, 0f, z);
            yield return null;
        }

        transform.localEulerAngles = new Vector3(0f, 0f, startZ);

        Hide();
        _playRoutine = null;
    }

    private void Show()
    {
        if (_rt != null)
        {
            float mult = Mathf.Max(0.01f, _playScaleMultiplier);
            _rt.localScale = _baseScale * mult;
        }

        if (_image != null)
        {
            _image.color = _playColor;
            _image.enabled = true;
        }
    }

    private void UpdateAlignment()
    {
        if (!_lockToWorldPosition) return;
        if (_rt == null || _canvasRT == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(_knobWorldPosition);
        if (screen.z < 0f) return; // behind camera

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screen, null, out Vector2 localPoint))
        {
            // Assumes this UI element is anchored around the center (as it is in this scene).
            _rt.anchoredPosition = localPoint;
        }
    }

    private void Hide()
    {
        StopTurningSound();
        if (_animator != null) _animator.enabled = false;
        if (_image != null) _image.enabled = false;
        if (_rt != null) _rt.localScale = _baseScale;
    }
}

