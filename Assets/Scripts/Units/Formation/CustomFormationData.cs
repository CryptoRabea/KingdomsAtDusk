using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Formation
{
    /// <summary>
    /// Represents a single unit position in a custom formation
    /// </summary>
    [Serializable]
    public class FormationPosition
    {
        public Vector2 position; // Position on the grid (normalized -1 to 1)

        public FormationPosition(Vector2 pos)
        {
            position = pos;
        }

        public FormationPosition(float x, float y)
        {
            position = new Vector2(x, y);
        }
    }

    /// <summary>
    /// Data structure for a custom formation
    /// </summary>
    [Serializable]
    public class CustomFormationData
    {
        public string id; // Unique identifier
        public string name; // User-friendly name
        public List<FormationPosition> positions; // Unit positions
        public DateTime createdDate;
        public DateTime modifiedDate;
        public bool isInQuickList; // Whether this formation appears in the quick access dropdown

        public CustomFormationData()
        {
            id = Guid.NewGuid().ToString();
            name = "New Formation";
            positions = new List<FormationPosition>();
            createdDate = DateTime.Now;
            modifiedDate = DateTime.Now;
            isInQuickList = false; // Not in quick list by default
        }

        public CustomFormationData(string formationName)
        {
            id = Guid.NewGuid().ToString();
            name = formationName;
            positions = new List<FormationPosition>();
            createdDate = DateTime.Now;
            modifiedDate = DateTime.Now;
            isInQuickList = false; // Not in quick list by default
        }

        /// <summary>
        /// Convert normalized grid positions to world positions for units
        /// </summary>
        public List<Vector3> CalculateWorldPositions(Vector3 centerPosition, float spacing, Vector3 facingDirection)
        {
            List<Vector3> worldPositions = new List<Vector3>();

            if (positions.Count == 0)
                return worldPositions;

            // Calculate right vector perpendicular to facing direction
            Vector3 right = Vector3.Cross(Vector3.up, facingDirection).normalized;
            Vector3 forward = facingDirection.normalized;

            // Convert each normalized position to world space
            foreach (var pos in positions)
            {
                // Scale by spacing and apply to center position
                Vector3 offset = (pos.position.x * spacing * right) + (pos.position.y * spacing * forward);
                Vector3 worldPos = centerPosition + offset;
                worldPositions.Add(worldPos);
            }

            return worldPositions;
        }

        /// <summary>
        /// Add a new position to the formation
        /// </summary>
        public void AddPosition(Vector2 position)
        {
            positions.Add(new FormationPosition(position));
            modifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Remove a position at the specified index
        /// </summary>
        public void RemovePosition(int index)
        {
            if (index >= 0 && index < positions.Count)
            {
                positions.RemoveAt(index);
                modifiedDate = DateTime.Now;
            }
        }

        /// <summary>
        /// Update a position at the specified index
        /// </summary>
        public void UpdatePosition(int index, Vector2 newPosition)
        {
            if (index >= 0 && index < positions.Count)
            {
                positions[index].position = newPosition;
                modifiedDate = DateTime.Now;
            }
        }

        /// <summary>
        /// Clear all positions
        /// </summary>
        public void ClearPositions()
        {
            positions.Clear();
            modifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Clone this formation data
        /// </summary>
        public CustomFormationData Clone()
        {
            CustomFormationData clone = new CustomFormationData();
            clone.id = Guid.NewGuid().ToString(); // New ID for clone
            clone.name = name + " (Copy)";
            clone.positions = new List<FormationPosition>();

            foreach (var pos in positions)
            {
                clone.positions.Add(new FormationPosition(pos.position));
            }

            clone.createdDate = DateTime.Now;
            clone.modifiedDate = DateTime.Now;
            clone.isInQuickList = false; // Clones are not in quick list by default

            return clone;
        }

        /// <summary>
        /// Add this formation to the quick access list
        /// </summary>
        public void AddToQuickList()
        {
            isInQuickList = true;
            modifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Remove this formation from the quick access list
        /// </summary>
        public void RemoveFromQuickList()
        {
            isInQuickList = false;
            modifiedDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Container for all custom formations (for serialization)
    /// </summary>
    [Serializable]
    public class CustomFormationsContainer
    {
        public List<CustomFormationData> formations;

        public CustomFormationsContainer()
        {
            formations = new List<CustomFormationData>();
        }
    }
}
