using UnityEngine;
using System.Collections;
using RTS.Core.Services;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Component that makes wounded units drip blood periodically.
    /// Automatically added/removed based on health percentage.
    /// </summary>
    public class BloodDripper : MonoBehaviour
    {
        private System.Func<float> getCurrentHealth;
        private System.Func<float> getMaxHealth;
        private FloatingNumbersSettings settings;
        private IFloatingNumberService floatingNumberService;

        private float nextDripTime;
        private bool isDripping;

        public void Initialize(
            System.Func<float> getCurrentHealth,
            System.Func<float> getMaxHealth,
            FloatingNumbersSettings settings,
            IFloatingNumberService floatingNumberService)
        {
            this.getCurrentHealth = getCurrentHealth;
            this.getMaxHealth = getMaxHealth;
            this.settings = settings;
            this.floatingNumberService = floatingNumberService;

            isDripping = true;
            nextDripTime = Time.time + Random.Range(0f, 1f / settings.BloodDripRate);

            StartCoroutine(DripBloodCoroutine());
        }

        private IEnumerator DripBloodCoroutine()
        {
            while (isDripping)
            {
                // Check if still wounded enough to drip
                if (getCurrentHealth != null && getMaxHealth != null)
                {
                    float healthPercent = getCurrentHealth() / getMaxHealth();

                    if (healthPercent > settings.BloodDrippingThreshold || healthPercent <= 0f)
                    {
                        // No longer wounded enough or dead
                        StopDripping();
                        yield break;
                    }
                }
                else
                {
                    // Health functions not available
                    StopDripping();
                    yield break;
                }

                // Wait for next drip
                float waitTime = 1f / settings.BloodDripRate;
                yield return new WaitForSeconds(waitTime);

                // Drip blood
                if (isDripping && floatingNumberService != null)
                {
                    Vector3 dripPosition = transform.position + Vector3.down * 0.5f;

                    // Cast ray to find ground
                    if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
                    {
                        dripPosition = hit.point;
                    }

                    // Create small blood gush downward
                    floatingNumberService.ShowBloodGush(dripPosition, Vector3.down, 3);

                    // Create blood decal on ground
                    floatingNumberService.ShowBloodDecal(dripPosition);
                }
            }
        }

        public void StopDripping()
        {
            isDripping = false;
            StopAllCoroutines();
            Destroy(this);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            isDripping = false;
        }

        private void OnDisable()
        {
            StopDripping();
        }
    }
}
