using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Gate animation type enumeration.
    /// </summary>
    public enum GateAnimationType
    {
        VerticalSlide,     // Slides straight up
        AnglePull,         // Pulls up at an angle (drawbridge style)
        RotateLeft,        // Rotates left (single door)
        RotateRight,       // Rotates right (single door)
        RotateBoth,        // Two doors rotating outward
        HorizontalSlide    // Slides horizontally (like barn doors)
    }

    /// <summary>
    /// ScriptableObject for gate-specific configuration.
    /// Extends BuildingDataSO with gate opening/closing properties.
    /// Create via: Right-click in Project > Create > RTS > GateData
    /// </summary>
    [CreateAssetMenu(fileName = "GateData", menuName = "RTS/GateData")]
    public class GateDataSO : BuildingDataSO
    {
        [Header("Gate Properties")]
        [Tooltip("Type of gate opening animation")]
        public GateAnimationType animationType = GateAnimationType.VerticalSlide;

        [Header("Auto-Open Settings")]
        [Tooltip("Enable automatic opening when friendly units approach")]
        public bool enableAutoOpen = true;

        [Tooltip("Distance at which gate auto-opens for friendly units")]
        public float autoOpenRange = 5f;

        [Tooltip("Distance at which gate auto-closes when units leave")]
        public float autoCloseRange = 7f;

        [Tooltip("Layers considered 'friendly' for auto-opening (e.g., Player layer)")]
        public LayerMask friendlyLayers;

        [Tooltip("How often to check for nearby units (in seconds)")]
        public float detectionInterval = 0.5f;

        [Header("Manual Control Settings")]
        [Tooltip("Can players manually open/close this gate?")]
        public bool allowManualControl = true;

        [Header("Animation Settings")]
        [Tooltip("How long the gate takes to open (seconds)")]
        public float openDuration = 2f;

        [Tooltip("How long the gate takes to close (seconds)")]
        public float closeDuration = 2f;

        [Tooltip("For VerticalSlide: how high the gate moves")]
        public float slideHeight = 4f;

        [Tooltip("For AnglePull: rotation angle in degrees")]
        public float pullAngle = 85f;

        [Tooltip("For Rotate types: rotation angle in degrees")]
        public float rotationAngle = 90f;

        [Tooltip("For HorizontalSlide: how far the doors slide apart")]
        public float slideDistance = 3f;

        [Header("Door Objects")]
        [Tooltip("Main door object to animate (for single door types)")]
        public string doorObjectName = "Door";

        [Tooltip("Left door object name (for two-door types)")]
        public string leftDoorObjectName = "LeftDoor";

        [Tooltip("Right door object name (for two-door types)")]
        public string rightDoorObjectName = "RightDoor";

        [Header("Wall Replacement")]
        [Tooltip("Can this gate be placed on walls?")]
        public bool canReplaceWalls = true;

        [Tooltip("Snap distance to walls for placement")]
        public float wallSnapDistance = 2f;

        /// <summary>
        /// Get gate description with opening type.
        /// </summary>
        public override string GetFullDescription()
        {
            string baseDesc = base.GetFullDescription();

            baseDesc += $"\n\n--- Gate Stats ---";
            baseDesc += $"\nOpening Type: {animationType}";
            baseDesc += $"\nAuto-Open: {(enableAutoOpen ? "Yes" : "No")}";
            if (enableAutoOpen)
            {
                baseDesc += $"\nAuto-Open Range: {autoOpenRange}m";
            }
            baseDesc += $"\nManual Control: {(allowManualControl ? "Yes" : "No")}";
            baseDesc += $"\nOpen Duration: {openDuration}s";
            baseDesc += $"\nCan Replace Walls: {(canReplaceWalls ? "Yes" : "No")}";

            return baseDesc;
        }
    }
}
