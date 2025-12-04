using UnityEngine;

namespace FlowField.Core
{
    /// <summary>
    /// Represents a single cell in the flow field grid
    /// </summary>
    public struct GridCell
    {
        public byte cost;              // 0 = unwalkable, 1 = normal, 255 = max cost
        public ushort bestCost;        // Integration field value (distance to goal)
        public Vector2 bestDirection;  // Flow direction (normalized)

        public const byte MAX_COST = 255;
        public const byte UNWALKABLE_COST = 0;
        public const byte DEFAULT_COST = 1;
        public const ushort MAX_INTEGRATION_COST = ushort.MaxValue;

        public bool IsWalkable => cost > 0;
    }

    /// <summary>
    /// Grid position in cell coordinates
    /// </summary>
    public struct GridPosition
    {
        public int x;
        public int z;

        public GridPosition(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public static GridPosition operator +(GridPosition a, GridPosition b)
        {
            return new GridPosition(a.x + b.x, a.z + b.z);
        }

        public static bool operator ==(GridPosition a, GridPosition b)
        {
            return a.x == b.x && a.z == b.z;
        }

        public static bool operator !=(GridPosition a, GridPosition b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is GridPosition other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (z.GetHashCode() << 2);
        }

        public override string ToString()
        {
            return $"({x}, {z})";
        }
    }
}
