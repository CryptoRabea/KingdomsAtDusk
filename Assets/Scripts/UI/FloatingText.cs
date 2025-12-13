using UnityEngine;
using TMPro;
using System.Collections;

namespace UI
{
    /// <summary>
    /// Floating text component for displaying damage, healing, and construction progress.
    /// Automatically animates upward and fades out.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private Vector3 moveDirection = Vector3.up;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Randomization")]
        [SerializeField] private bool randomizeDirection = true;
        [SerializeField] private float randomAngle = 30f;
        [SerializeField] private bool randomizeScale = true;
        [SerializeField] private float scaleVariation = 0.2f;

        private TextMeshPro textMesh;
        private Color originalColor;
        private float elapsedTime = 0f;
        private Vector3 startPosition;
        private Vector3 baseScale;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshPro>();
            originalColor = textMesh.color;
            startPosition = transform.position;
            baseScale = transform.localScale;

            // Apply randomization
            if (randomizeDirection)
            {
                float angle = Random.Range(-randomAngle, randomAngle);
                moveDirection = Quaternion.Euler(0, angle, 0) * moveDirection;
            }

            if (randomizeScale)
            {
                float scaleMultiplier = 1f + Random.Range(-scaleVariation, scaleVariation);
                baseScale *= scaleMultiplier;
            }
        }

        private void Start()
        {
            // Start destruction timer
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / lifetime;

            // Move
            transform.position = startPosition + (moveDirection.normalized * moveSpeed * elapsedTime);

            // Scale
            float scaleValue = scaleCurve.Evaluate(progress);
            transform.localScale = baseScale * scaleValue;

            // Fade
            float alpha = alphaCurve.Evaluate(progress);
            Color newColor = originalColor;
            newColor.a = alpha;
            textMesh.color = newColor;

            // Face camera
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }

        /// <summary>
        /// Set the text to display
        /// </summary>
        public void SetText(string text)
        {
            if (textMesh == null)
                textMesh = GetComponent<TextMeshPro>();

            textMesh.text = text;
        }

        /// <summary>
        /// Set the color of the text
        /// </summary>
        public void SetColor(Color color)
        {
            if (textMesh == null)
                textMesh = GetComponent<TextMeshPro>();

            originalColor = color;
            textMesh.color = color;
        }

        /// <summary>
        /// Set the font size
        /// </summary>
        public void SetFontSize(float size)
        {
            if (textMesh == null)
                textMesh = GetComponent<TextMeshPro>();

            textMesh.fontSize = size;
        }

        /// <summary>
        /// Factory method to create floating text
        /// </summary>
        public static FloatingText Create(GameObject prefab, Vector3 position, string text, Color color)
        {
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            FloatingText floatingText = obj.GetComponent<FloatingText>();

            if (floatingText != null)
            {
                floatingText.SetText(text);
                floatingText.SetColor(color);
            }

            return floatingText;
        }
    }
}
