using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles segment-based construction for walls.
    /// Supports construction with or without worker assignment.
    /// </summary>
    public class WallSegmentConstructor : MonoBehaviour
    {
        [Header("Construction Settings")]
        [SerializeField] private ConstructionMode constructionMode = ConstructionMode.Timed;
        [SerializeField] private float segmentConstructionTime = 5f; // Time per segment
        [SerializeField] private bool showConstructionProgress = true;

        [Header("Visual Settings")]
        [SerializeField] private GameObject constructionVisual; // Optional: shows during construction
        [SerializeField] private Color incompleteColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color completeColor = Color.white;

        // Segment tracking
        private List<WallSegment> segments = new List<WallSegment>();
        private int completedSegments = 0;
        private bool allSegmentsComplete = false;

        // Building component reference
        private Building buildingComponent;

        private void Awake()
        {
            buildingComponent = GetComponent<Building>();
        }

        private void Start()
        {
            InitializeSegment();
        }

        private void Update()
        {
            if (allSegmentsComplete) return;

            // Update construction based on mode
            switch (constructionMode)
            {
                case ConstructionMode.Instant:
                    CompleteAllSegments();
                    break;

                case ConstructionMode.Timed:
                    UpdateTimedConstruction();
                    break;

                case ConstructionMode.SegmentWithoutWorkers:
                    UpdateSegmentConstructionWithoutWorkers();
                    break;

                case ConstructionMode.SegmentWithWorkers:
                    UpdateSegmentConstructionWithWorkers();
                    break;
            }
        }

        #region Initialization

        private void InitializeSegment()
        {
            // For now, treat this building as a single segment
            // In future, could expand to handle multi-segment buildings
            WallSegment segment = new WallSegment
            {
                segmentIndex = 0,
                constructionProgress = 0f,
                isComplete = false,
                assignedWorker = null,
                renderers = GetComponentsInChildren<Renderer>()
            };

            segments.Add(segment);

            // Set initial visual state
            UpdateSegmentVisuals(segment);

            // Show construction visual if available
            if (constructionVisual != null && constructionMode != ConstructionMode.Instant)
            {
                constructionVisual.SetActive(true);
            }
        }

        #endregion

        #region Construction Updates

        private void UpdateTimedConstruction()
        {
            if (segments.Count == 0 || segments[0].isComplete) return;

            WallSegment segment = segments[0];
            segment.constructionProgress += Time.deltaTime;

            if (segment.constructionProgress >= segmentConstructionTime)
            {
                CompleteSegment(segment);
            }
            else if (showConstructionProgress)
            {
                UpdateSegmentVisuals(segment);
            }
        }

        private void UpdateSegmentConstructionWithoutWorkers()
        {
            // Same as timed construction for single segment buildings
            UpdateTimedConstruction();
        }

        private void UpdateSegmentConstructionWithWorkers()
        {
            if (segments.Count == 0 || segments[0].isComplete) return;

            WallSegment segment = segments[0];

            // Only progress if worker is assigned
            if (segment.assignedWorker != null)
            {
                segment.constructionProgress += Time.deltaTime;

                if (segment.constructionProgress >= segmentConstructionTime)
                {
                    CompleteSegment(segment);
                }
                else if (showConstructionProgress)
                {
                    UpdateSegmentVisuals(segment);
                }
            }
        }

        #endregion

        #region Segment Completion

        private void CompleteSegment(WallSegment segment)
        {
            segment.isComplete = true;
            segment.constructionProgress = segmentConstructionTime;
            completedSegments++;

            // Update visuals
            UpdateSegmentVisuals(segment);

            Debug.Log($"Wall segment {segment.segmentIndex} construction complete!");

            // Check if all segments are complete
            if (completedSegments >= segments.Count)
            {
                CompleteConstruction();
            }
        }

        private void CompleteAllSegments()
        {
            foreach (var segment in segments)
            {
                if (!segment.isComplete)
                {
                    segment.isComplete = true;
                    segment.constructionProgress = segmentConstructionTime;
                    UpdateSegmentVisuals(segment);
                }
            }

            completedSegments = segments.Count;
            CompleteConstruction();
        }

        private void CompleteConstruction()
        {
            allSegmentsComplete = true;

            // Hide construction visual
            if (constructionVisual != null)
            {
                constructionVisual.SetActive(false);
            }

            // Complete the building component if using instant mode
            if (constructionMode == ConstructionMode.Instant && buildingComponent != null)
            {
                buildingComponent.InstantComplete();
            }

            Debug.Log("Wall construction fully complete!");
        }

        #endregion

        #region Visual Updates

        private void UpdateSegmentVisuals(WallSegment segment)
        {
            if (segment.renderers == null || segment.renderers.Length == 0) return;

            Color targetColor = segment.isComplete ? completeColor : incompleteColor;

            // Lerp color based on construction progress if showing progress
            if (showConstructionProgress && !segment.isComplete)
            {
                float progress = segment.constructionProgress / segmentConstructionTime;
                targetColor = Color.Lerp(incompleteColor, completeColor, progress);
            }

            foreach (var renderer in segment.renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = targetColor;
                }
            }
        }

        #endregion

        #region Worker Assignment

        /// <summary>
        /// Assign a worker to a specific segment.
        /// </summary>
        public bool AssignWorkerToSegment(int segmentIndex, GameObject worker)
        {
            if (constructionMode != ConstructionMode.SegmentWithWorkers)
            {
                Debug.LogWarning("Cannot assign workers in this construction mode!");
                return false;
            }

            if (segmentIndex < 0 || segmentIndex >= segments.Count)
            {
                Debug.LogError($"Invalid segment index: {segmentIndex}");
                return false;
            }

            WallSegment segment = segments[segmentIndex];

            if (segment.isComplete)
            {
                Debug.LogWarning("Cannot assign worker to completed segment!");
                return false;
            }

            if (segment.assignedWorker != null)
            {
                Debug.LogWarning($"Segment {segmentIndex} already has a worker assigned!");
                return false;
            }

            segment.assignedWorker = worker;
            Debug.Log($"Worker assigned to segment {segmentIndex}");
            return true;
        }

        /// <summary>
        /// Remove worker from a specific segment.
        /// </summary>
        public bool RemoveWorkerFromSegment(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segments.Count)
            {
                Debug.LogError($"Invalid segment index: {segmentIndex}");
                return false;
            }

            WallSegment segment = segments[segmentIndex];

            if (segment.assignedWorker == null)
            {
                Debug.LogWarning($"Segment {segmentIndex} has no worker assigned!");
                return false;
            }

            segment.assignedWorker = null;
            Debug.Log($"Worker removed from segment {segmentIndex}");
            return true;
        }

        /// <summary>
        /// Get the worker assigned to a segment, if any.
        /// </summary>
        public GameObject GetAssignedWorker(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= segments.Count)
                return null;

            return segments[segmentIndex].assignedWorker;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the construction mode.
        /// </summary>
        public void SetConstructionMode(ConstructionMode mode)
        {
            constructionMode = mode;

            if (mode == ConstructionMode.Instant && !allSegmentsComplete)
            {
                CompleteAllSegments();
            }
        }

        /// <summary>
        /// Get the current construction mode.
        /// </summary>
        public ConstructionMode GetConstructionMode() => constructionMode;

        /// <summary>
        /// Get the total construction progress (0-1).
        /// </summary>
        public float GetTotalProgress()
        {
            if (segments.Count == 0) return 0f;

            float totalProgress = 0f;
            foreach (var segment in segments)
            {
                totalProgress += Mathf.Clamp01(segment.constructionProgress / segmentConstructionTime);
            }

            return totalProgress / segments.Count;
        }

        /// <summary>
        /// Get the number of completed segments.
        /// </summary>
        public int GetCompletedSegmentsCount() => completedSegments;

        /// <summary>
        /// Get the total number of segments.
        /// </summary>
        public int GetTotalSegmentsCount() => segments.Count;

        /// <summary>
        /// Check if construction is complete.
        /// </summary>
        public bool IsConstructionComplete() => allSegmentsComplete;

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!showConstructionProgress || allSegmentsComplete) return;

            // Draw construction progress bar
            Vector3 position = transform.position + Vector3.up * 3f;
            float progress = GetTotalProgress();

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(position, new Vector3(2f, 0.2f, 0.1f));

            Gizmos.color = Color.green;
            Gizmos.DrawCube(position - Vector3.right * (1f - progress), new Vector3(2f * progress, 0.2f, 0.1f));
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Represents a single wall segment.
        /// </summary>
        [System.Serializable]
        private class WallSegment
        {
            public int segmentIndex;
            public float constructionProgress;
            public bool isComplete;
            public GameObject assignedWorker;
            public Renderer[] renderers;
        }

        #endregion
    }
}
