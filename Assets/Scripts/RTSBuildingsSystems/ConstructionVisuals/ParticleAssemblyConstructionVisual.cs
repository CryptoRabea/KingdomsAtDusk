using UnityEngine;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// Construction visual where particles swarm and assemble the building.
    /// Particles fly in from random directions and settle into place.
    /// The building gradually becomes more opaque as more particles arrive.
    /// </summary>
    public class ParticleAssemblyConstructionVisual : BaseConstructionVisual
    {
        [Header("Particle Settings")]
        [SerializeField] private GameObject particlePrefab; // Particle that assembles the building
        [SerializeField] private int particleCount = 100;
        [SerializeField] private float particleSpeed = 5f;
        [SerializeField] private float spawnRadius = 10f; // How far away particles spawn
        [SerializeField] private Color particleColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Header("Assembly Animation")]
        [SerializeField] private AnimationCurve particleSpawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float particleLifetime = 2f;
        [SerializeField] private bool usePhysics = false;

        [Header("Building Reveal")]
        [SerializeField] private float startAlpha = 0f;
        [SerializeField] private float endAlpha = 1f;
        [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private List<AssemblyParticle> particles = new List<AssemblyParticle>();
        private MaterialPropertyBlock propertyBlock;
        private int particlesToSpawn = 0;
        private float lastProgress = 0f;

        // Shader property IDs
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        private class AssemblyParticle
        {
            public GameObject gameObject;
            public Vector3 startPosition;
            public Vector3 targetPosition;
            public float spawnTime;
            public float arrivalTime;
            public bool hasArrived;
        }

        protected override void Initialize()
        {
            propertyBlock = new MaterialPropertyBlock();

            // Clear any existing particles
            ClearParticles();

            // Set building to transparent initially
            SetBuildingAlpha(startAlpha);

            // Calculate particles to spawn
            particlesToSpawn = 0;
            lastProgress = 0f;

            UpdateVisual(0f);
        }

        protected override void UpdateVisual(float progress)
        {
            // Determine how many particles should exist at this progress
            int targetParticleCount = Mathf.RoundToInt(particleSpawnCurve.Evaluate(progress) * particleCount);

            // Spawn new particles if needed
            while (particlesToSpawn < targetParticleCount)
            {
                SpawnParticle();
                particlesToSpawn++;
            }

            // Update existing particles
            UpdateParticles();

            // Update building opacity based on progress
            float alpha = Mathf.Lerp(startAlpha, endAlpha, revealCurve.Evaluate(progress));
            SetBuildingAlpha(alpha);

            lastProgress = progress;
        }

        private void SpawnParticle()
        {
            // Random spawn position outside the building
            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 spawnPos = combinedBounds.center + randomDirection * spawnRadius;

            // Random target position within building bounds
            Vector3 targetPos = new Vector3(
                Random.Range(combinedBounds.min.x, combinedBounds.max.x),
                Random.Range(combinedBounds.min.y, combinedBounds.max.y),
                Random.Range(combinedBounds.min.z, combinedBounds.max.z)
            );

            GameObject particleObj;

            if (particlePrefab != null)
            {
                // Use custom particle prefab
                particleObj = Instantiate(particlePrefab, spawnPos, Quaternion.identity);
                particleObj.transform.SetParent(transform);
            }
            else
            {
                // Create simple cube particle
                particleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                particleObj.transform.position = spawnPos;
                particleObj.transform.SetParent(transform);
                particleObj.transform.localScale = Vector3.one * 0.1f;

                // Set color
                Renderer rend = particleObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = particleColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", particleColor * 0.5f);
                    rend.material = mat;
                }

                // Remove collider
                Collider col = particleObj.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }
            }

            // Add to tracking list
            AssemblyParticle particle = new AssemblyParticle
            {
                gameObject = particleObj,
                startPosition = spawnPos,
                targetPosition = targetPos,
                spawnTime = Time.time,
                arrivalTime = Time.time + particleLifetime,
                hasArrived = false
            };

            particles.Add(particle);

            // Add physics if enabled
            if (usePhysics)
            {
                Rigidbody rb = particleObj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = particleObj.AddComponent<Rigidbody>();
                }
                rb.useGravity = false;
                rb.drag = 2f;
            }
        }

        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                AssemblyParticle particle = particles[i];

                if (particle.gameObject == null)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                if (!particle.hasArrived)
                {
                    // Move particle toward target
                    float elapsed = Time.time - particle.spawnTime;
                    float t = elapsed / particleLifetime;

                    if (t >= 1f)
                    {
                        // Particle has arrived
                        particle.hasArrived = true;
                        particle.gameObject.transform.position = particle.targetPosition;

                        // Add small visual effect on arrival
                        particle.gameObject.transform.localScale *= 0.5f;

                        // Destroy after a short delay
                        Destroy(particle.gameObject, 0.5f);
                    }
                    else
                    {
                        // Smooth movement
                        if (usePhysics)
                        {
                            Rigidbody rb = particle.gameObject.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                Vector3 direction = (particle.targetPosition - particle.gameObject.transform.position).normalized;
                                rb.velocity = direction * particleSpeed;
                            }
                        }
                        else
                        {
                            // Ease-in-out movement
                            float easedT = Mathf.SmoothStep(0f, 1f, t);
                            particle.gameObject.transform.position = Vector3.Lerp(
                                particle.startPosition,
                                particle.targetPosition,
                                easedT
                            );

                            // Rotate particle for visual interest
                            particle.gameObject.transform.Rotate(Vector3.up, Time.deltaTime * 360f);
                        }
                    }
                }
            }
        }

        private void SetBuildingAlpha(float alpha)
        {
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);

                Color color = Color.white;
                color.a = alpha;

                propertyBlock.SetColor(ColorPropertyID, color);
                propertyBlock.SetColor(BaseColorID, color);

                rend.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearParticles()
        {
            foreach (var particle in particles)
            {
                if (particle.gameObject != null)
                {
                    Destroy(particle.gameObject);
                }
            }

            particles.Clear();
        }

        protected override void Cleanup()
        {
            // Clear all particles
            ClearParticles();

            // Reset building alpha
            SetBuildingAlpha(1f);
        }

        private void OnDestroy()
        {
            ClearParticles();
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw spawn radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(combinedBounds.center, spawnRadius);

            // Draw particle target positions
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                foreach (var particle in particles)
                {
                    if (!particle.hasArrived)
                    {
                        Gizmos.DrawLine(particle.gameObject.transform.position, particle.targetPosition);
                        Gizmos.DrawSphere(particle.targetPosition, 0.1f);
                    }
                }
            }
        }
#endif
    }
}
