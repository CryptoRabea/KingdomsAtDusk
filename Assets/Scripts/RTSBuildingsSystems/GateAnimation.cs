using UnityEngine;
using System.Collections;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles gate opening/closing animations.
    /// Supports multiple animation types: vertical slide, angle pull, rotation, etc.
    /// </summary>
    public class GateAnimation : MonoBehaviour
    {
        [Header("Door References")]
        [SerializeField] private Transform doorObject;
        [SerializeField] private Transform leftDoorObject;
        [SerializeField] private Transform rightDoorObject;

        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioSource audioSource;

        private GateDataSO gateData;
        private bool isAnimating = false;

        // Stored initial transforms
        private Vector3 doorInitialPosition;
        private Quaternion doorInitialRotation;
        private Vector3 leftDoorInitialPosition;
        private Quaternion leftDoorInitialRotation;
        private Vector3 rightDoorInitialPosition;
        private Quaternion rightDoorInitialRotation;

        private void Awake()
        {
            // Try to find door objects if not assigned
            FindDoorObjects();

            // Store initial transforms
            StoreInitialTransforms();

            // Setup audio source
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        private void FindDoorObjects()
        {
            if (doorObject == null)
            {
                Transform found = transform.Find("Door");
                if (found != null) doorObject = found;
            }

            if (leftDoorObject == null)
            {
                Transform found = transform.Find("LeftDoor");
                if (found != null) leftDoorObject = found;
            }

            if (rightDoorObject == null)
            {
                Transform found = transform.Find("RightDoor");
                if (found != null) rightDoorObject = found;
            }
        }

        private void StoreInitialTransforms()
        {
            if (doorObject != null)
            {
                doorInitialPosition = doorObject.localPosition;
                doorInitialRotation = doorObject.localRotation;
            }

            if (leftDoorObject != null)
            {
                leftDoorInitialPosition = leftDoorObject.localPosition;
                leftDoorInitialRotation = leftDoorObject.localRotation;
            }

            if (rightDoorObject != null)
            {
                rightDoorInitialPosition = rightDoorObject.localPosition;
                rightDoorInitialRotation = rightDoorObject.localRotation;
            }
        }

        public void SetGateData(GateDataSO data)
        {
            gateData = data;

            // Try to find door objects by name from data
            if (gateData != null)
            {
                if (doorObject == null && !string.IsNullOrEmpty(gateData.doorObjectName))
                {
                    Transform found = transform.Find(gateData.doorObjectName);
                    if (found != null)
                    {
                        doorObject = found;
                        doorInitialPosition = doorObject.localPosition;
                        doorInitialRotation = doorObject.localRotation;
                    }
                }

                if (leftDoorObject == null && !string.IsNullOrEmpty(gateData.leftDoorObjectName))
                {
                    Transform found = transform.Find(gateData.leftDoorObjectName);
                    if (found != null)
                    {
                        leftDoorObject = found;
                        leftDoorInitialPosition = leftDoorObject.localPosition;
                        leftDoorInitialRotation = leftDoorObject.localRotation;
                    }
                }

                if (rightDoorObject == null && !string.IsNullOrEmpty(gateData.rightDoorObjectName))
                {
                    Transform found = transform.Find(gateData.rightDoorObjectName);
                    if (found != null)
                    {
                        rightDoorObject = found;
                        rightDoorInitialPosition = rightDoorObject.localPosition;
                        rightDoorInitialRotation = rightDoorObject.localRotation;
                    }
                }
            }
        }

        /// <summary>
        /// Open the gate with animation.
        /// </summary>
        public void Open(System.Action onComplete = null)
        {
            if (isAnimating)
            {
                Debug.LogWarning("Gate is already animating!");
                return;
            }

            if (gateData == null)
            {
                Debug.LogError("Gate data not set!");
                return;
            }

            PlaySound(openSound);
            StartCoroutine(AnimateOpen(onComplete));
        }

        /// <summary>
        /// Close the gate with animation.
        /// </summary>
        public void Close(System.Action onComplete = null)
        {
            if (isAnimating)
            {
                Debug.LogWarning("Gate is already animating!");
                return;
            }

            if (gateData == null)
            {
                Debug.LogError("Gate data not set!");
                return;
            }

            PlaySound(closeSound);
            StartCoroutine(AnimateClose(onComplete));
        }

        private IEnumerator AnimateOpen(System.Action onComplete)
        {
            isAnimating = true;
            float elapsed = 0f;
            float duration = gateData.openDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = openCurve.Evaluate(t);

                ApplyAnimation(curveValue, true);

                yield return null;
            }

            // Ensure final position
            ApplyAnimation(1f, true);

            isAnimating = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateClose(System.Action onComplete)
        {
            isAnimating = false;
            float elapsed = 0f;
            float duration = gateData.closeDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = closeCurve.Evaluate(t);

                ApplyAnimation(1f - curveValue, false);

                yield return null;
            }

            // Ensure final position
            ApplyAnimation(0f, false);

            isAnimating = false;
            onComplete?.Invoke();
        }

        private void ApplyAnimation(float progress, bool opening)
        {
            switch (gateData.animationType)
            {
                case GateAnimationType.VerticalSlide:
                    ApplyVerticalSlide(progress);
                    break;

                case GateAnimationType.AnglePull:
                    ApplyAnglePull(progress);
                    break;

                case GateAnimationType.RotateLeft:
                    ApplyRotation(progress, -1);
                    break;

                case GateAnimationType.RotateRight:
                    ApplyRotation(progress, 1);
                    break;

                case GateAnimationType.RotateBoth:
                    ApplyRotateBoth(progress);
                    break;

                case GateAnimationType.HorizontalSlide:
                    ApplyHorizontalSlide(progress);
                    break;
            }
        }

        #region Animation Types

        private void ApplyVerticalSlide(float progress)
        {
            if (doorObject == null) return;

            Vector3 targetPosition = doorInitialPosition + Vector3.up * gateData.slideHeight * progress;
            doorObject.localPosition = targetPosition;
        }

        private void ApplyAnglePull(float progress)
        {
            if (doorObject == null) return;

            // Rotate around the bottom edge (pivot point)
            Quaternion targetRotation = doorInitialRotation * Quaternion.Euler(gateData.pullAngle * progress, 0, 0);
            doorObject.localRotation = targetRotation;

            // Optionally adjust position to account for rotation
            // This depends on your pivot point setup
        }

        private void ApplyRotation(float progress, int direction)
        {
            if (doorObject == null) return;

            Quaternion targetRotation = doorInitialRotation * Quaternion.Euler(0, gateData.rotationAngle * progress * direction, 0);
            doorObject.localRotation = targetRotation;
        }

        private void ApplyRotateBoth(float progress)
        {
            if (leftDoorObject != null)
            {
                Quaternion leftRotation = leftDoorInitialRotation * Quaternion.Euler(0, -gateData.rotationAngle * progress, 0);
                leftDoorObject.localRotation = leftRotation;
            }

            if (rightDoorObject != null)
            {
                Quaternion rightRotation = rightDoorInitialRotation * Quaternion.Euler(0, gateData.rotationAngle * progress, 0);
                rightDoorObject.localRotation = rightRotation;
            }
        }

        private void ApplyHorizontalSlide(float progress)
        {
            if (leftDoorObject != null)
            {
                Vector3 leftPosition = leftDoorInitialPosition + Vector3.left * gateData.slideDistance * progress;
                leftDoorObject.localPosition = leftPosition;
            }

            if (rightDoorObject != null)
            {
                Vector3 rightPosition = rightDoorInitialPosition + Vector3.right * gateData.slideDistance * progress;
                rightDoorObject.localPosition = rightPosition;
            }
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #region Debug

        [ContextMenu("Test Open Animation")]
        private void TestOpen()
        {
            if (gateData == null)
            {
                Debug.LogWarning("Gate data not set! Assign gateData in Inspector or call SetGateData()");
                return;
            }
            Open();
        }

        [ContextMenu("Test Close Animation")]
        private void TestClose()
        {
            if (gateData == null)
            {
                Debug.LogWarning("Gate data not set! Assign gateData in Inspector or call SetGateData()");
                return;
            }
            Close();
        }

        [ContextMenu("Reset to Initial State")]
        private void ResetToInitial()
        {
            StopAllCoroutines();
            isAnimating = false;

            if (doorObject != null)
            {
                doorObject.localPosition = doorInitialPosition;
                doorObject.localRotation = doorInitialRotation;
            }

            if (leftDoorObject != null)
            {
                leftDoorObject.localPosition = leftDoorInitialPosition;
                leftDoorObject.localRotation = leftDoorInitialRotation;
            }

            if (rightDoorObject != null)
            {
                rightDoorObject.localPosition = rightDoorInitialPosition;
                rightDoorObject.localRotation = rightDoorInitialRotation;
            }
        }

        #endregion
    }
}
