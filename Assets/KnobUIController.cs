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

    [SerializeField] private Animator _animator;
    [SerializeField] private Image _image;

    [SerializeField] private float _fallbackDurationSeconds = 0.5f;
    [SerializeField] private float _spinDegrees = -360f;

    private Coroutine _playRoutine;
    private Color _playColor = Color.white;

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
        if (_image != null)
        {
            _image.color = _playColor;
            _image.enabled = true;
        }
    }

    private void Hide()
    {
        if (_animator != null) _animator.enabled = false;
        if (_image != null) _image.enabled = false;
    }
}

