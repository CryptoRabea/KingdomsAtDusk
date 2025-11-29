using UnityEngine;
using TMPro;
using System.Collections;

namespace RTS.UI.FloatingNumbers
{
    /// <summary>
    /// Individual floating number that animates and fades out
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fadeDelay = 0.5f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.8f);

        private float elapsedTime = 0f;
        private Vector3 startPosition;
        private Vector3 randomOffset;

        public void Initialize(string text, Color color, Vector3 worldPosition)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            transform.position = worldPosition;
            startPosition = transform.position;
            randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            elapsedTime = 0f;

            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            while (elapsedTime < lifetime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / lifetime;

                // Move upward with curve
                float moveProgress = moveCurve.Evaluate(normalizedTime);
                Vector3 offset = (Vector3.up * moveSpeed + randomOffset) * moveProgress;
                transform.position = startPosition + offset;

                // Scale animation
                float scale = scaleCurve.Evaluate(normalizedTime);
                transform.localScale = Vector3.one * scale;

                // Fade out after delay
                if (elapsedTime > fadeDelay && canvasGroup != null)
                {
                    float fadeProgress = (elapsedTime - fadeDelay) / (lifetime - fadeDelay);
                    canvasGroup.alpha = 1f - fadeProgress;
                }

                yield return null;
            }

            // Return to pool
            FloatingNumbersManager.Instance?.ReturnToPool(this);
        }

        private void OnValidate()
        {
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }
}
