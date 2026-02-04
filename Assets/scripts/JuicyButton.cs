using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Adds satisfying press animations to UI buttons - scale and color changes only
/// </summary>
[RequireComponent(typeof(Button))]
public class JuicyButton : MonoBehaviour, IPointerDownHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float pressDownDuration = 0.1f;
    [SerializeField] private float pressUpDuration = 0.2f;

    [Header("Punch Animation")]
    [SerializeField] private bool enablePunch = true;
    [SerializeField] private float punchScale = 0.15f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("Color Animation")]
    [SerializeField] private bool enableColorChange = true;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private Button _button;
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Image _image;
    private Color _originalColor;
    private Coroutine _currentAnimation;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();

        _originalScale = _rectTransform.localScale;

        if (_image != null)
        {
            _originalColor = _image.color;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        // Stop any existing animation
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        // Start the full press animation
        _currentAnimation = StartCoroutine(AnimatePress());
    }

    private IEnumerator AnimatePress()
    {
        Vector3 startScale = _rectTransform.localScale;
        Vector3 targetScale = _originalScale * pressScale;
        Color startColor = _image != null ? _image.color : Color.white;

        // Press down phase
        float elapsed = 0f;
        while (elapsed < pressDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pressDownDuration);

            _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (enableColorChange && _image != null)
            {
                _image.color = Color.Lerp(startColor, pressedColor, t);
            }

            yield return null;
        }

        // Release phase with bounce
        elapsed = 0f;
        while (elapsed < pressUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pressUpDuration);
            float easedT = EaseOutBack(t);

            _rectTransform.localScale = Vector3.Lerp(targetScale, _originalScale, easedT);

            if (enableColorChange && _image != null)
            {
                _image.color = Color.Lerp(pressedColor, _originalColor, easedT);
            }

            yield return null;
        }

        // Punch effect (bouncy overshoot)
        if (enablePunch)
        {
            elapsed = 0f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / punchDuration;

                // Sine wave that diminishes over time
                float punchAmount = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * punchScale;
                _rectTransform.localScale = _originalScale + (Vector3.one * punchAmount);

                yield return null;
            }
        }

        // Ensure final state
        _rectTransform.localScale = _originalScale;
        if (_image != null)
        {
            _image.color = _originalColor;
        }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void OnDisable()
    {
        // Stop any running animation
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _currentAnimation = null;
        }

        // Reset to original state
        _rectTransform.localScale = _originalScale;
        if (_image != null)
        {
            _image.color = _originalColor;
        }
    }
}
