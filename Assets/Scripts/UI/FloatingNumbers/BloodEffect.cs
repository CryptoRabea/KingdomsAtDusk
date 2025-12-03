using UnityEngine;
using System.Collections;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Blood particle effect that gushes when units take damage.
    /// Pooled for performance.
    /// </summary>
    public class BloodEffect : MonoBehaviour
    {
        private ParticleSystem particleSystem;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.EmissionModule emissionModule;
        private float lifetime;
        private System.Action<BloodEffect> onComplete;
        private bool isPlaying;

        private void Awake()
        {
            particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = gameObject.AddComponent<ParticleSystem>();
                ConfigureParticleSystem();
            }

            mainModule = particleSystem.main;
            emissionModule = particleSystem.emission;
        }

        private void ConfigureParticleSystem()
        {
            var main = particleSystem.main;
            main.startLifetime = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.gravityModifier = 2f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;

            var emission = particleSystem.emission;
            emission.rateOverTime = 0;
            emission.enabled = true;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            }
        }

        /// <summary>
        /// Initialize and play the blood gush effect.
        /// </summary>
        public void Initialize(
            Vector3 position,
            Vector3 direction,
            Color bloodColor,
            int particleCount,
            System.Action<BloodEffect> onComplete)
        {
            transform.position = position;
            this.onComplete = onComplete;

            // Update particle color
            mainModule.startColor = bloodColor;

            // Set direction
            var shape = particleSystem.shape;
            shape.rotation = Quaternion.LookRotation(direction).eulerAngles;

            // Emit particles in a burst
            emissionModule.enabled = false;
            particleSystem.Clear();
            particleSystem.Emit(particleCount);

            lifetime = mainModule.startLifetime.constantMax;
            isPlaying = true;
            gameObject.SetActive(true);

            StartCoroutine(WaitForCompletion());
        }

        private IEnumerator WaitForCompletion()
        {
            yield return new WaitForSeconds(lifetime + 0.5f);

            // Make sure all particles are dead
            while (particleSystem.particleCount > 0)
            {
                yield return null;
            }

            isPlaying = false;
            gameObject.SetActive(false);
            onComplete?.Invoke(this);
        }

        /// <summary>
        /// Force stop the effect and return to pool.
        /// </summary>
        public void ForceStop()
        {
            if (isPlaying)
            {
                StopAllCoroutines();
                particleSystem.Clear();
                isPlaying = false;
                gameObject.SetActive(false);
                onComplete?.Invoke(this);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (particleSystem != null)
            {
                particleSystem.Clear();
            }
            isPlaying = false;
        }
    }
}
