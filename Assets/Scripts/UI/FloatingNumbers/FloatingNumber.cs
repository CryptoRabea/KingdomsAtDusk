using UnityEngine;
using TMPro;
using System.Collections;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Animated floating number that displays damage, healing, resources, etc.
    /// Pooled for performance.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FloatingNumber : MonoBehaviour
    {
        private TextMeshProUGUI textMesh;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float elapsedTime;
        private float duration;

        private AnimationCurve scaleCurve;
        private AnimationCurve fadeCurve;

        private bool isAnimating;
        private System.Action<FloatingNumber> onComplete;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();

            // Add CanvasGroup if it doesn't exist
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// Initialize and start animating the floating number.
        /// </summary>
        public void Initialize(
            string text,
            Vector2 screenPosition,
            Color color,
            int fontSize,
            float duration,
            float floatHeight,
            AnimationCurve scaleCurve,
            AnimationCurve fadeCurve,
            System.Action<FloatingNumber> onComplete)
        {
            // Set text properties
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = fontSize;

            // Set position
            startPosition = screenPosition;
            targetPosition = screenPosition + Vector2.up * floatHeight;
            rectTransform.anchoredPosition = startPosition;

            // Set animation properties
            this.duration = duration;
            this.scaleCurve = scaleCurve;
            this.fadeCurve = fadeCurve;
            this.onComplete = onComplete;

            elapsedTime = 0f;
            isAnimating = true;

            // Reset state
            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);

            StartCoroutine(AnimateCoroutine());
        }

        private IEnumerator AnimateCoroutine()
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                // Update position
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

                // Update scale
                float scaleValue = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.one * scaleValue;

                // Update alpha
                float alphaValue = fadeCurve.Evaluate(t);
                canvasGroup.alpha = alphaValue;

                yield return null;
            }

            // Animation complete
            isAnimating = false;
            gameObject.SetActive(false);
            onComplete?.Invoke(this);
        }

        /// <summary>
        /// Force stop the animation and return to pool.
        /// </summary>
        public void ForceStop()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
                gameObject.SetActive(false);
                onComplete?.Invoke(this);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isAnimating = false;
        }
    }
}
