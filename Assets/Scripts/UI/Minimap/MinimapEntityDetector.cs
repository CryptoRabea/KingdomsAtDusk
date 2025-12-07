using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Flexible detection system for determining entity ownership on the minimap.
    /// Supports multiple detection methods with fallbacks.
    /// </summary>
    public static class MinimapEntityDetector
    {
        public enum DetectionMethod
        {
            Layer,              // Use Unity layer (simple, current method)
            Tag,                // Use Unity tag
            Component,          // Use IMinimapEntity component (most flexible)
            PlayerComparison,   // Compare with player ID
            Auto                // Try all methods in order
        }

        /// <summary>
        /// Detect if an entity is enemy based on the specified detection method.
        /// </summary>
        public static bool IsEnemy(GameObject entity, DetectionMethod method = DetectionMethod.Auto)
        {
            if (entity == null) return false;

            switch (method)
            {
                case DetectionMethod.Layer:
                    return IsEnemyByLayer(entity);

                case DetectionMethod.Tag:
                    return IsEnemyByTag(entity);

                case DetectionMethod.Component:
                    return IsEnemyByComponent(entity);

                case DetectionMethod.PlayerComparison:
                    return IsEnemyByPlayerComparison(entity);

                case DetectionMethod.Auto:
                default:
                    // Try component first (most flexible)
                    if (entity.TryGetComponent<IMinimapEntity>(out var minimapEntity))
                    {
                        return minimapEntity.GetOwnership() == MinimapEntityOwnership.Enemy;
                    }

                    // Fallback to tag (with safe checking)
                    if (SafeHasTag(entity, "Enemy") || SafeHasTag(entity, "Friendly") || SafeHasTag(entity, "Neutral"))
                    {
                        return IsEnemyByTag(entity);
                    }

                    // Fallback to layer
                    return IsEnemyByLayer(entity);
            }
        }

        /// <summary>
        /// Get the ownership of an entity using the specified detection method.
        /// </summary>
        public static MinimapEntityOwnership GetOwnership(GameObject entity, DetectionMethod method = DetectionMethod.Auto)
        {
            if (entity == null) return MinimapEntityOwnership.Neutral;

            switch (method)
            {
                case DetectionMethod.Component:
                    if (entity.TryGetComponent<IMinimapEntity>(out var minimapEntity))
                    {
                        return minimapEntity.GetOwnership();
                    }
                    break;

                case DetectionMethod.Tag:
                    if (SafeCompareTag(entity, "Friendly")) return MinimapEntityOwnership.Friendly;
                    if (SafeCompareTag(entity, "Enemy")) return MinimapEntityOwnership.Enemy;
                    if (SafeCompareTag(entity, "Neutral")) return MinimapEntityOwnership.Neutral;
                    if (SafeCompareTag(entity, "Ally")) return MinimapEntityOwnership.Ally;
                    break;

                case DetectionMethod.Layer:
                    if (entity.layer == LayerMask.NameToLayer("Enemy"))
                        return MinimapEntityOwnership.Enemy;
                    return MinimapEntityOwnership.Friendly;

                case DetectionMethod.Auto:
                    // Try component first
                    if (entity.TryGetComponent<IMinimapEntity>(out var component))
                    {
                        return component.GetOwnership();
                    }

                    // Try tag (with safe checking)
                    if (SafeCompareTag(entity, "Friendly")) return MinimapEntityOwnership.Friendly;
                    if (SafeCompareTag(entity, "Enemy")) return MinimapEntityOwnership.Enemy;
                    if (SafeCompareTag(entity, "Neutral")) return MinimapEntityOwnership.Neutral;
                    if (SafeCompareTag(entity, "Ally")) return MinimapEntityOwnership.Ally;

                    // Fallback to layer
                    if (entity.layer == LayerMask.NameToLayer("Enemy"))
                        return MinimapEntityOwnership.Enemy;

                    return MinimapEntityOwnership.Friendly;
            }

            return MinimapEntityOwnership.Neutral;
        }

        private static bool IsEnemyByLayer(GameObject entity)
        {
            return entity.layer == LayerMask.NameToLayer("Enemy");
        }

        private static bool IsEnemyByTag(GameObject entity)
        {
            return SafeCompareTag(entity, "Enemy");
        }

        private static bool IsEnemyByComponent(GameObject entity)
        {
            if (entity.TryGetComponent<IMinimapEntity>(out var minimapEntity))
            {
                return minimapEntity.GetOwnership() == MinimapEntityOwnership.Enemy;
            }
            return false;
        }

        private static bool IsEnemyByPlayerComparison(GameObject entity)
        {
            // TODO: Implement player ID comparison when player system exists
            // Example:
            // var ownership = entity.GetComponent<UnitOwnership>();
            // return ownership != null && ownership.playerId != GameManager.LocalPlayerId;
            return IsEnemyByLayer(entity);
        }

        /// <summary>
        /// Get color for an entity based on ownership.
        /// </summary>
        public static Color GetColorForOwnership(MinimapEntityOwnership ownership, MinimapConfig config)
        {
            switch (ownership)
            {
                case MinimapEntityOwnership.Friendly:
                    return config.friendlyUnitColor;

                case MinimapEntityOwnership.Enemy:
                    return config.enemyUnitColor;

                case MinimapEntityOwnership.Neutral:
                    return Color.gray;

                case MinimapEntityOwnership.Ally:
                    return Color.cyan;

                case MinimapEntityOwnership.Player1:
                    return Color.blue;

                case MinimapEntityOwnership.Player2:
                    return Color.red;

                case MinimapEntityOwnership.Player3:
                    return Color.green;

                case MinimapEntityOwnership.Player4:
                    return Color.yellow;

                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Safe version of CompareTag that doesn't throw if tag is not defined.
        /// </summary>
        private static bool SafeCompareTag(GameObject entity, string tag)
        {
            try
            {
                return entity.CompareTag(tag);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safe check if entity has a specific tag without throwing.
        /// </summary>
        private static bool SafeHasTag(GameObject entity, string tag)
        {
            return SafeCompareTag(entity, tag);
        }
    }
}
