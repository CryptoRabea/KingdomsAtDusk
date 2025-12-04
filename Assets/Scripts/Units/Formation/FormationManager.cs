using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Formation
{
    /// <summary>
    /// Manages unit formation calculations for group movement.
    /// Provides various formation types (Line, Column, Box, Wedge) with proper spacing.
    /// </summary>
    public static class FormationManager
    {
        /// <summary>
        /// Calculate formation positions for a group of units.
        /// </summary>
        /// <param name="centerPosition">Center point of the formation</param>
        /// <param name="unitCount">Number of units in the formation</param>
        /// <param name="formationType">Type of formation to use</param>
        /// <param name="spacing">Distance between units</param>
        /// <param name="facingDirection">Direction the formation should face (optional)</param>
        /// <returns>List of positions for each unit</returns>
        public static List<Vector3> CalculateFormationPositions(
            Vector3 centerPosition,
            int unitCount,
            FormationType formationType,
            float spacing = 2f,
            Vector3? facingDirection = null)
        {
            List<Vector3> positions = new List<Vector3>();

            if (unitCount <= 0)
                return positions;

            // Single unit - just use center position
            if (unitCount == 1)
            {
                positions.Add(centerPosition);
                return positions;
            }

            // Calculate positions based on formation type
            switch (formationType)
            {
                case FormationType.Line:
                    positions = CalculateLineFormation(centerPosition, unitCount, spacing, facingDirection);
                    break;

                case FormationType.Column:
                    positions = CalculateColumnFormation(centerPosition, unitCount, spacing, facingDirection);
                    break;

                case FormationType.Box:
                    positions = CalculateBoxFormation(centerPosition, unitCount, spacing);
                    break;

                case FormationType.Wedge:
                    positions = CalculateWedgeFormation(centerPosition, unitCount, spacing, facingDirection);
                    break;

                case FormationType.Circle:
                    positions = CalculateCircleFormation(centerPosition, unitCount, spacing);
                    break;

                default:
                    // Fallback to scatter if unknown type
                    positions = CalculateScatterFormation(centerPosition, unitCount, spacing);
                    break;
            }

            return positions;
        }

        #region Formation Calculations

        private static List<Vector3> CalculateLineFormation(Vector3 center, int count, float spacing, Vector3? facingDirection)
        {
            List<Vector3> positions = new List<Vector3>();

            // Determine perpendicular direction to facing
            Vector3 facing = facingDirection ?? Vector3.forward;
            facing.y = 0;
            facing.Normalize();

            // Get right vector (perpendicular to facing)
            Vector3 right = Vector3.Cross(Vector3.up, facing);

            // Calculate starting position (leftmost unit)
            float totalWidth = (count - 1) * spacing;
            Vector3 startPos = center - right * (totalWidth / 2f);

            // Place units in a line
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = startPos + right * (i * spacing);
                positions.Add(pos);
            }

            return positions;
        }

        private static List<Vector3> CalculateColumnFormation(Vector3 center, int count, float spacing, Vector3? facingDirection)
        {
            List<Vector3> positions = new List<Vector3>();

            // Determine facing direction
            Vector3 facing = facingDirection ?? Vector3.forward;
            facing.y = 0;
            facing.Normalize();

            // Calculate starting position (frontmost unit)
            float totalDepth = (count - 1) * spacing;
            Vector3 startPos = center + facing * (totalDepth / 2f);

            // Place units in a column
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = startPos - facing * (i * spacing);
                positions.Add(pos);
            }

            return positions;
        }

        private static List<Vector3> CalculateBoxFormation(Vector3 center, int count, float spacing)
        {
            List<Vector3> positions = new List<Vector3>();

            // Calculate rows and columns for box formation
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);

            // Calculate starting position (top-left corner)
            float totalWidth = (columns - 1) * spacing;
            float totalDepth = (rows - 1) * spacing;
            Vector3 startPos = center - new Vector3(totalWidth / 2f, 0, totalDepth / 2f);

            // Place units in a grid
            int unitIndex = 0;
            for (int row = 0; row < rows && unitIndex < count; row++)
            {
                for (int col = 0; col < columns && unitIndex < count; col++)
                {
                    Vector3 pos = startPos + new Vector3(col * spacing, 0, -row * spacing);
                    positions.Add(pos);
                    unitIndex++;
                }
            }

            return positions;
        }

        private static List<Vector3> CalculateWedgeFormation(Vector3 center, int count, float spacing, Vector3? facingDirection)
        {
            List<Vector3> positions = new List<Vector3>();

            // Determine facing direction
            Vector3 facing = facingDirection ?? Vector3.forward;
            facing.y = 0;
            facing.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, facing);

            // Place leader at front
            positions.Add(center);

            int remainingUnits = count - 1;
            int currentRow = 1;
            int unitsPlaced = 1;

            // Build wedge rows
            while (unitsPlaced < count)
            {
                int unitsInRow = Mathf.Min(currentRow * 2, remainingUnits);
                float rowDepth = currentRow * spacing;
                float rowWidth = (unitsInRow - 1) * spacing / 2f;

                Vector3 rowCenter = center - facing * rowDepth;
                Vector3 rowStart = rowCenter - right * rowWidth;

                for (int i = 0; i < unitsInRow && unitsPlaced < count; i++)
                {
                    Vector3 pos = rowStart + right * (i * spacing / 2f);
                    positions.Add(pos);
                    unitsPlaced++;
                }

                remainingUnits -= unitsInRow;
                currentRow++;
            }

            return positions;
        }

        private static List<Vector3> CalculateCircleFormation(Vector3 center, int count, float spacing)
        {
            List<Vector3> positions = new List<Vector3>();

            // Calculate radius based on spacing and count
            float circumference = count * spacing;
            float radius = circumference / (2f * Mathf.PI);

            // Place units evenly around circle
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = center.x + radius * Mathf.Cos(angle);
                float z = center.z + radius * Mathf.Sin(angle);
                positions.Add(new Vector3(x, center.y, z));
            }

            return positions;
        }

        private static List<Vector3> CalculateScatterFormation(Vector3 center, int count, float spacing)
        {
            List<Vector3> positions = new List<Vector3>();

            // Random scatter within radius
            float scatterRadius = spacing * Mathf.Sqrt(count);

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
                Vector3 pos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
                positions.Add(pos);
            }

            return positions;
        }

        #endregion

        /// <summary>
        /// Get recommended spacing for a unit based on its size.
        /// </summary>
        public static float GetRecommendedSpacing(float unitRadius)
        {
            // Add buffer space for avoidance
            return unitRadius * 2.5f;
        }

        /// <summary>
        /// Validate formation positions on NavMesh and adjust if needed.
        /// </summary>
        public static List<Vector3> ValidateFormationPositions(List<Vector3> positions, float maxDistanceFromOriginal = 5f)
        {
            List<Vector3> validatedPositions = new List<Vector3>();

            foreach (Vector3 pos in positions)
            {
                // Try to find nearest valid NavMesh position
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out UnityEngine.AI.NavMeshHit hit, maxDistanceFromOriginal, UnityEngine.AI.NavMesh.AllAreas))
                {
                    validatedPositions.Add(hit.position);
                }
                else
                {
                    // If no valid position found, use original (unit will handle it)
                    validatedPositions.Add(pos);
                }
            }

            return validatedPositions;
        }
    }

    /// <summary>
    /// Available formation types for unit groups.
    /// </summary>
    public enum FormationType
    {
        None,       // No formation - all units go to same point
        Line,       // Horizontal line
        Column,     // Vertical column
        Box,        // Grid/rectangle
        Wedge,      // V-shape pointing forward
        Circle,     // Units in a circle
        Scatter     // Random positions around center
    }
}
