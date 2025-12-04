using UnityEngine;
using System.Collections.Generic;
using FlowField.Movement;
using FlowField.Core;

namespace FlowField.Formation
{
    /// <summary>
    /// Controls formation movement for groups of units using flow fields
    /// Integrates with existing FormationManager for RTS-style unit groups
    /// </summary>
    public class FlowFieldFormationController : MonoBehaviour
    {
        [Header("Formation Settings")]
        [SerializeField] private float unitSpacing = 2.5f;

        [Header("Performance")]
        [SerializeField] private bool useMultiGoalPathfinding = false; // Use multi-goal flow field for formations

        // Formation types (matches existing FormationType enum)
        public enum FormationType
        {
            None,
            Line,
            Column,
            Box,
            Wedge,
            Circle,
            Scatter
        }

        private FormationType currentFormation = FormationType.Box;

        /// <summary>
        /// Move units in formation to destination
        /// </summary>
        public void MoveUnitsInFormation(
            List<FlowFieldFollower> units,
            Vector3 destination,
            FormationType formation = FormationType.Box)
        {
            if (units == null || units.Count == 0)
                return;

            currentFormation = formation;

            // Calculate formation positions
            List<Vector3> formationPositions = CalculateFormationPositions(
                destination,
                units.Count,
                formation,
                GetFacingDirection(units, destination)
            );

            if (useMultiGoalPathfinding && units.Count > 5)
            {
                // Use multi-goal flow field (all units converge to their positions)
                FlowFieldManager.Instance.GenerateFlowField(formationPositions);

                // Assign formation offsets
                for (int i = 0; i < units.Count; i++)
                {
                    Vector3 offset = formationPositions[i] - destination;
                    units[i].SetFormationOffset(offset);
                    units[i].SetDestination(destination);
                }
            }
            else
            {
                // Use single flow field with formation offsets
                FlowFieldManager.Instance.GenerateFlowField(destination);

                // Assign each unit its formation offset
                for (int i = 0; i < units.Count; i++)
                {
                    Vector3 offset = formationPositions[i] - destination;
                    units[i].SetFormationOffset(offset);
                    units[i].SetDestination(destination);
                }
            }
        }

        /// <summary>
        /// Calculate formation positions around a center point
        /// </summary>
        private List<Vector3> CalculateFormationPositions(
            Vector3 center,
            int unitCount,
            FormationType formation,
            Vector3 facingDirection)
        {
            List<Vector3> positions = new List<Vector3>(unitCount);

            switch (formation)
            {
                case FormationType.None:
                    // All units go to same point
                    for (int i = 0; i < unitCount; i++)
                        positions.Add(center);
                    break;

                case FormationType.Line:
                    positions = CalculateLineFormation(center, unitCount, facingDirection);
                    break;

                case FormationType.Column:
                    positions = CalculateColumnFormation(center, unitCount, facingDirection);
                    break;

                case FormationType.Box:
                    positions = CalculateBoxFormation(center, unitCount, facingDirection);
                    break;

                case FormationType.Wedge:
                    positions = CalculateWedgeFormation(center, unitCount, facingDirection);
                    break;

                case FormationType.Circle:
                    positions = CalculateCircleFormation(center, unitCount);
                    break;

                case FormationType.Scatter:
                    positions = CalculateScatterFormation(center, unitCount);
                    break;
            }

            return positions;
        }

        private List<Vector3> CalculateLineFormation(Vector3 center, int count, Vector3 facing)
        {
            List<Vector3> positions = new List<Vector3>(count);
            Vector3 right = Vector3.Cross(facing, Vector3.up).normalized;

            float totalWidth = (count - 1) * unitSpacing;
            Vector3 startPos = center - right * (totalWidth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                positions.Add(startPos + right * (i * unitSpacing));
            }

            return positions;
        }

        private List<Vector3> CalculateColumnFormation(Vector3 center, int count, Vector3 facing)
        {
            List<Vector3> positions = new List<Vector3>(count);

            float totalDepth = (count - 1) * unitSpacing;
            Vector3 startPos = center - facing * (totalDepth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                positions.Add(startPos + facing * (i * unitSpacing));
            }

            return positions;
        }

        private List<Vector3> CalculateBoxFormation(Vector3 center, int count, Vector3 facing)
        {
            List<Vector3> positions = new List<Vector3>(count);

            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);

            Vector3 right = Vector3.Cross(facing, Vector3.up).normalized;

            float totalWidth = (columns - 1) * unitSpacing;
            float totalDepth = (rows - 1) * unitSpacing;

            Vector3 startPos = center - right * (totalWidth * 0.5f) - facing * (totalDepth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                Vector3 pos = startPos + right * (col * unitSpacing) + facing * (row * unitSpacing);
                positions.Add(pos);
            }

            return positions;
        }

        private List<Vector3> CalculateWedgeFormation(Vector3 center, int count, Vector3 facing)
        {
            List<Vector3> positions = new List<Vector3>(count);
            Vector3 right = Vector3.Cross(facing, Vector3.up).normalized;

            // Wedge: narrow at front, wide at back
            int currentRow = 0;
            int unitsPlaced = 0;

            while (unitsPlaced < count)
            {
                int unitsInRow = currentRow + 1;

                for (int i = 0; i < unitsInRow && unitsPlaced < count; i++)
                {
                    float rowWidth = unitsInRow * unitSpacing;
                    Vector3 rowCenter = center - facing * (currentRow * unitSpacing);
                    Vector3 rowStart = rowCenter - right * (rowWidth * 0.5f);

                    positions.Add(rowStart + right * (i * unitSpacing));
                    unitsPlaced++;
                }

                currentRow++;
            }

            return positions;
        }

        private List<Vector3> CalculateCircleFormation(Vector3 center, int count)
        {
            List<Vector3> positions = new List<Vector3>(count);

            float radius = (count * unitSpacing) / (2f * Mathf.PI);
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                positions.Add(center + offset);
            }

            return positions;
        }

        private List<Vector3> CalculateScatterFormation(Vector3 center, int count)
        {
            List<Vector3> positions = new List<Vector3>(count);

            float scatterRadius = Mathf.Sqrt(count) * unitSpacing;

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
                Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
                positions.Add(center + offset);
            }

            return positions;
        }

        /// <summary>
        /// Calculate facing direction for formation (from units to destination)
        /// </summary>
        private Vector3 GetFacingDirection(List<FlowFieldFollower> units, Vector3 destination)
        {
            if (units.Count == 0)
                return Vector3.forward;

            // Average unit position
            Vector3 avgPos = Vector3.zero;
            foreach (var unit in units)
            {
                avgPos += unit.transform.position;
            }
            avgPos /= units.Count;

            // Direction from avg position to destination
            Vector3 direction = (destination - avgPos).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude < 0.01f)
                return Vector3.forward;

            return direction.normalized;
        }

        /// <summary>
        /// Dynamically adjust formation spacing based on terrain
        /// </summary>
        public void AdjustFormationSpacing(float newSpacing)
        {
            unitSpacing = Mathf.Max(1f, newSpacing);
        }

        /// <summary>
        /// Check if formation needs reformation (units too spread out)
        /// </summary>
        public bool NeedsReformation(List<FlowFieldFollower> units, float threshold = 5f)
        {
            if (units.Count < 2)
                return false;

            Vector3 avgPos = Vector3.zero;
            foreach (var unit in units)
            {
                avgPos += unit.transform.position;
            }
            avgPos /= units.Count;

            // Check max distance from center
            float maxDistance = 0f;
            foreach (var unit in units)
            {
                float dist = Vector3.Distance(unit.transform.position, avgPos);
                if (dist > maxDistance)
                    maxDistance = dist;
            }

            return maxDistance > threshold;
        }
    }
}
