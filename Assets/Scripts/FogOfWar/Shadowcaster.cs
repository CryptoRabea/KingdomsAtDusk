using System.Collections.Generic;
using UnityEngine;

namespace RTS.FogOfWar
{
    /// <summary>
    /// Shadowcasting Fog of War solver (grid-based).
    /// Visuals are handled by terrain shader.
    /// This class ONLY manages visibility data.
    /// </summary>
    public class Shadowcaster
    {
        // =============================
        // TILE VISIBILITY
        // =============================

        public enum TileVisibility : byte
        {
            Hidden = 0,
            Revealed = 1,
            PreviouslyRevealed = 2
        }

        // =============================
        // FOG FIELD
        // =============================

        public TileVisibility[,] fogField;

        private int width;
        private int height;

        // =============================
        // INITIALIZATION
        // =============================

        public void Initialize(int width, int height)
        {
            this.width = width;
            this.height = height;

            fogField = new TileVisibility[width, height];

            Reset();
        }

        public void Reset(bool keepMemory = false)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (keepMemory && fogField[x, y] == TileVisibility.Revealed)
                        fogField[x, y] = TileVisibility.PreviouslyRevealed;
                    else
                        fogField[x, y] = TileVisibility.Hidden;
                }
            }
        }

        // =============================
        // PUBLIC API
        // =============================

        public void Reveal(Vector2Int origin, int radius)
        {
            if (!InBounds(origin))
                return;

            SetVisible(origin.x, origin.y);

            for (int oct = 0; oct < 8; oct++)
            {
                CastLight(
                    origin.x,
                    origin.y,
                    1,
                    1.0f,
                    0.0f,
                    radius,
                    OCTANTS[oct, 0],
                    OCTANTS[oct, 1],
                    OCTANTS[oct, 2],
                    OCTANTS[oct, 3]
                );
            }
        }

        public bool IsVisible(Vector2Int p)
        {
            if (!InBounds(p))
                return false;

            return fogField[p.x, p.y] == TileVisibility.Revealed;
        }

        // =============================
        // SHADOWCASTING CORE
        // =============================

        private static readonly int[,] OCTANTS =
        {
            { 1,  0,  0,  1 },
            { 0,  1,  1,  0 },
            { -1, 0,  0,  1 },
            { 0, -1, 1,  0 },
            { -1, 0,  0, -1 },
            { 0, -1,-1,  0 },
            { 1,  0,  0, -1 },
            { 0,  1,-1,  0 }
        };

        private void CastLight(
            int cx,
            int cy,
            int row,
            float startSlope,
            float endSlope,
            int radius,
            int xx,
            int xy,
            int yx,
            int yy)
        {
            if (startSlope < endSlope)
                return;

            float nextStartSlope = startSlope;

            for (int distance = row; distance <= radius; distance++)
            {
                bool blocked = false;

                for (int deltaY = -distance; deltaY <= 0; deltaY++)
                {
                    int dx = -distance;
                    int dy = deltaY;

                    float leftSlope = (deltaY - 0.5f) / (dx + 0.5f);
                    float rightSlope = (deltaY + 0.5f) / (dx - 0.5f);

                    if (rightSlope > startSlope)
                        continue;
                    if (leftSlope < endSlope)
                        break;

                    int mapX = cx + dx * xx + dy * xy;
                    int mapY = cy + dx * yx + dy * yy;

                    Vector2Int p = new Vector2Int(mapX, mapY);

                    if (!InBounds(p))
                        continue;

                    SetVisible(mapX, mapY);

                    bool isBlocking = false; // <-- hook LOS blockers here later

                    if (blocked)
                    {
                        if (isBlocking)
                        {
                            nextStartSlope = rightSlope;
                            continue;
                        }
                        else
                        {
                            blocked = false;
                            startSlope = nextStartSlope;
                        }
                    }
                    else
                    {
                        if (isBlocking && distance < radius)
                        {
                            blocked = true;
                            CastLight(cx, cy, distance + 1, startSlope, leftSlope, radius, xx, xy, yx, yy);
                            nextStartSlope = rightSlope;
                        }
                    }
                }

                if (blocked)
                    break;
            }
        }

        // =============================
        // HELPERS
        // =============================

        private void SetVisible(int x, int y)
        {
            if (fogField[x, y] == TileVisibility.Hidden)
                fogField[x, y] = TileVisibility.Revealed;
        }

        private bool InBounds(Vector2Int p)
        {
            return p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
        }
    }
}
