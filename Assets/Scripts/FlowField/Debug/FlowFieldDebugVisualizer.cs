using FlowField.Core;
using FlowField.Movement;
using UnityEngine;

namespace FlowField.Debug
{
    /// <summary>
    /// Debug visualization for flow fields
    /// Shows cost field, integration field, flow directions, and unit paths
    /// </summary>
    public class FlowFieldDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private bool showCostField = false;
        [SerializeField] private bool showIntegrationField = false;
        [SerializeField] private bool showFlowField = true;
        [SerializeField] private bool showUnitVelocities = true;
        [SerializeField] private bool showGridBounds = true;

        [Header("Flow Field Display")]
        [SerializeField] private float arrowLength = 0.5f;
        [SerializeField] private float arrowHeadSize = 0.2f;
        [SerializeField] private int displayEveryNthCell = 2; // Reduce clutter

        [Header("Colors")]
        [SerializeField] private Color walkableColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color unwalkableColor = new Color(1, 0, 0, 0.5f);
        [SerializeField] private Color flowColor = Color.cyan;
        [SerializeField] private Color velocityColor = Color.yellow;

        [Header("Integration Field Gradient")]
        [SerializeField] private Color lowCostColor = Color.green;
        [SerializeField] private Color highCostColor = Color.red;

        private FlowFieldManager flowFieldManager;
        private FlowFieldGrid grid;

        private void Start()
        {
            flowFieldManager = FlowFieldManager.Instance;
            if (flowFieldManager != null)
            {
                grid = flowFieldManager.Grid;
            }
        }

        private void OnDrawGizmos()
        {
            if (flowFieldManager == null || grid == null)
                return;

            if (showGridBounds)
            {
                DrawGridBounds();
            }

            if (showCostField)
            {
                DrawCostField();
            }

            if (showIntegrationField)
            {
                DrawIntegrationField();
            }

            if (showFlowField)
            {
                DrawFlowField();
            }

            if (showUnitVelocities)
            {
                DrawUnitVelocities();
            }
        }

        private void DrawGridBounds()
        {
            Gizmos.color = Color.white;
            Bounds bounds = grid.GetWorldBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void DrawCostField()
        {
            for (int z = 0; z < grid.height; z += displayEveryNthCell)
            {
                for (int x = 0; x < grid.width; x += displayEveryNthCell)
                {
                    GridCell cell = grid.GetCell(x, z);
                    Vector3 worldPos = grid.GridToWorld(x, z);

                    if (cell.IsWalkable)
                    {
                        // Color based on cost
                        float costNormalized = cell.cost / 255f;
                        Gizmos.color = Color.Lerp(walkableColor, Color.yellow, costNormalized);
                    }
                    else
                    {
                        Gizmos.color = unwalkableColor;
                    }

                    Gizmos.DrawCube(worldPos, 
                        0.8f * grid.cellSize * Vector3.one);
                }
            }
        }

        private void DrawIntegrationField()
        {
            for (int z = 0; z < grid.height; z += displayEveryNthCell)
            {
                for (int x = 0; x < grid.width; x += displayEveryNthCell)
                {
                    GridCell cell = grid.GetCell(x, z);
                    Vector3 worldPos = grid.GridToWorld(x, z);

                    if (cell.IsWalkable && cell.bestCost < GridCell.MAX_INTEGRATION_COST)
                    {
                        // Color based on integration cost (distance to goal)
                        float costNormalized = Mathf.Clamp01(cell.bestCost / 100f);
                        Gizmos.color = Color.Lerp(lowCostColor, highCostColor, costNormalized);

                        Gizmos.DrawCube(worldPos, 0.6f * grid.cellSize * Vector3.one);
                    }
                }
            }
        }

        private void DrawFlowField()
        {
            for (int z = 0; z < grid.height; z += displayEveryNthCell)
            {
                for (int x = 0; x < grid.width; x += displayEveryNthCell)
                {
                    GridCell cell = grid.GetCell(x, z);
                    Vector3 worldPos = grid.GridToWorld(x, z);

                    if (cell.IsWalkable && cell.bestDirection != Vector2.zero)
                    {
                        Vector3 direction = new Vector3(
                            cell.bestDirection.x,
                            0,
                            cell.bestDirection.y
                        );

                        DrawArrow(worldPos, direction * arrowLength, flowColor);
                    }
                }
            }
        }

        private void DrawUnitVelocities()
        {
            // Use FindObjectsByType instead of deprecated FindObjectsOfType
            FlowFieldFollower[] units = Object.FindObjectsByType<FlowFieldFollower>(FindObjectsSortMode.None);

            foreach (var unit in units)
            {
                if (unit.IsMoving)
                {
                    Vector3 pos = unit.transform.position + Vector3.up * 0.5f;
                    DrawArrow(pos, unit.CurrentVelocity, velocityColor);

                    // Draw destination line
                    if (unit.HasReachedDestination == false)
                    {
                        Gizmos.color = new Color(1, 1, 0, 0.3f);
                        Gizmos.DrawLine(pos, unit.Destination);
                    }
                }
            }
        }

        private void DrawArrow(Vector3 start, Vector3 direction, Color color)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            Gizmos.color = color;
            Vector3 end = start + direction;

            // Draw main line
            Gizmos.DrawLine(start, end);

            // Draw arrowhead
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;

            Gizmos.DrawLine(end, end + right * arrowHeadSize);
            Gizmos.DrawLine(end, end + left * arrowHeadSize);
        }

        /// <summary>
        /// GUI controls for toggling visualization
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 250));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Flow Field Debug");

            showCostField = GUILayout.Toggle(showCostField, "Show Cost Field");
            showIntegrationField = GUILayout.Toggle(showIntegrationField, "Show Integration Field");
            showFlowField = GUILayout.Toggle(showFlowField, "Show Flow Field");
            showUnitVelocities = GUILayout.Toggle(showUnitVelocities, "Show Unit Velocities");
            showGridBounds = GUILayout.Toggle(showGridBounds, "Show Grid Bounds");

            GUILayout.Space(10);

            GUILayout.Label($"Display Every: {displayEveryNthCell} cells");
            displayEveryNthCell = (int)GUILayout.HorizontalSlider(displayEveryNthCell, 1, 5);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
