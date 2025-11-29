using UnityEngine;
using System.Collections.Generic;

namespace RTS.UI.FloatingNumbers
{
    /// <summary>
    /// Manages floating number pooling and spawning
    /// Singleton pattern for easy access from anywhere
    /// </summary>
    public class FloatingNumbersManager : MonoBehaviour
    {
        public static FloatingNumbersManager Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private GameObject floatingNumberPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private Transform poolParent;

        [Header("Presets")]
        [SerializeField] private FloatingNumberPreset damagePreset = new FloatingNumberPreset
        {
            color = new Color(1f, 0.3f, 0.3f), // Red
            prefix = "-",
            fontSize = 24
        };

        [SerializeField] private FloatingNumberPreset healPreset = new FloatingNumberPreset
        {
            color = new Color(0.3f, 1f, 0.3f), // Green
            prefix = "+",
            fontSize = 24
        };

        [SerializeField] private FloatingNumberPreset criticalPreset = new FloatingNumberPreset
        {
            color = new Color(1f, 0.8f, 0f), // Gold
            prefix = "CRIT! -",
            fontSize = 32
        };

        private Queue<FloatingNumber> pool = new Queue<FloatingNumber>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (poolParent == null)
            {
                poolParent = transform;
            }

            InitializePool();
        }

        private void InitializePool()
        {
            if (floatingNumberPrefab == null)
            {
                Debug.LogError("FloatingNumbersManager: No prefab assigned!");
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewFloatingNumber();
            }
        }

        private FloatingNumber CreateNewFloatingNumber()
        {
            GameObject obj = Instantiate(floatingNumberPrefab, poolParent);
            FloatingNumber floatingNumber = obj.GetComponent<FloatingNumber>();

            if (floatingNumber == null)
            {
                Debug.LogError("FloatingNumbersManager: Prefab doesn't have FloatingNumber component!");
                Destroy(obj);
                return null;
            }

            obj.SetActive(false);
            pool.Enqueue(floatingNumber);
            return floatingNumber;
        }

        private FloatingNumber GetFromPool()
        {
            if (pool.Count == 0)
            {
                return CreateNewFloatingNumber();
            }

            FloatingNumber floatingNumber = pool.Dequeue();
            floatingNumber.gameObject.SetActive(true);
            return floatingNumber;
        }

        public void ReturnToPool(FloatingNumber floatingNumber)
        {
            if (floatingNumber == null) return;

            floatingNumber.gameObject.SetActive(false);
            floatingNumber.transform.SetParent(poolParent);
            pool.Enqueue(floatingNumber);
        }

        #region Public Spawn Methods

        public void SpawnDamage(float amount, Vector3 worldPosition)
        {
            SpawnNumber(amount.ToString("F0"), damagePreset.color, worldPosition);
        }

        public void SpawnHeal(float amount, Vector3 worldPosition)
        {
            SpawnNumber($"{healPreset.prefix}{amount:F0}", healPreset.color, worldPosition);
        }

        public void SpawnCritical(float amount, Vector3 worldPosition)
        {
            SpawnNumber($"{criticalPreset.prefix}{amount:F0}", criticalPreset.color, worldPosition);
        }

        public void SpawnNumber(string text, Color color, Vector3 worldPosition)
        {
            FloatingNumber floatingNumber = GetFromPool();
            if (floatingNumber != null)
            {
                // Offset position slightly above the target
                Vector3 spawnPosition = worldPosition + Vector3.up * 2f;
                floatingNumber.Initialize(text, color, spawnPosition);
            }
        }

        public void SpawnCustom(float amount, FloatingNumberPreset preset, Vector3 worldPosition)
        {
            string text = preset.prefix + amount.ToString("F0");
            SpawnNumber(text, preset.color, worldPosition);
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    [System.Serializable]
    public class FloatingNumberPreset
    {
        public Color color = Color.white;
        public string prefix = "";
        public int fontSize = 24;
    }
}
