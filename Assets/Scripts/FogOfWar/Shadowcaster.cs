

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FischlWorks_FogWar
{
    public class Shadowcaster
    {
        #region FogField

        public class FogField
        {
            private List<LevelColumn> levelRow = new List<LevelColumn>();
            private Color32[] cachedColors;

            public void AddColumn(LevelColumn column)
            {
                levelRow.Add(column);
            }

            public void Reset(csFogWar fogWar)
            {
                foreach (var col in levelRow)
                    col.Reset(fogWar);
            }

            public LevelColumn this[int index] => levelRow[index];

            public Color32[] GetColors(float fogPlaneAlpha, csFogWar fogWar)
            {
                int width = levelRow.Count;
                int height = levelRow[0].Count;

                if (cachedColors == null || cachedColors.Length != width * height)
                    cachedColors = new Color32[width * height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int vis = (int)levelRow[x][y];
                        float alpha = 1f - vis;

                        if (fogWar.keepRevealedTiles &&
                            vis == (int)LevelColumn.ETileVisibility.PreviouslyRevealed)
                        {
                            alpha = fogWar.revealedTileOpacity;
                        }

                        cachedColors[y * width + x] =
                            new Color(1f, 1f, 1f, alpha * fogPlaneAlpha);
                    }
                }

                return cachedColors;
            }
        }

        #endregion

        #region LevelColumn

        public class LevelColumn
        {
            public enum ETileVisibility
            {
                Hidden,
                Revealed,
                PreviouslyRevealed
            }

            private readonly List<ETileVisibility> tiles;

            public LevelColumn(IEnumerable<ETileVisibility> initial)
            {
                tiles = new List<ETileVisibility>(initial);
            }

            public int Count => tiles.Count;

            public ETileVisibility this[int index]
            {
                get => tiles[index];
                set => tiles[index] = value;
            }

            public void Reset(csFogWar fogWar)
            {
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (!fogWar.keepRevealedTiles)
                    {
                        tiles[i] = ETileVisibility.Hidden;
                    }
                    else if (tiles[i] == ETileVisibility.Revealed)
                    {
                        tiles[i] = ETileVisibility.PreviouslyRevealed;
                    }
                }
            }
        }

        #endregion

        #region Internals

        private csFogWar fogWar;
        public FogField fogField { get; private set; } = new FogField();

        private struct Quadrant
        {
            public int xx, xy, yx, yy;
        }

        private static readonly Quadrant[] quadrants =
        {
            new Quadrant { xx = 1, xy = 0, yx = 0, yy = 1 },
            new Quadrant { xx = 0, xy = 1, yx = 1, yy = 0 },
            new Quadrant { xx = -1, xy = 0, yx = 0, yy = 1 },
            new Quadrant { xx = 0, xy = -1, yx = 1, yy = 0 }
        };

        #endregion

        #region Initialization

        public void Initialize(csFogWar fogWar)
        {
            this.fogWar = fogWar;

            fogField = new FogField();

            for (int x = 0; x < fogWar.levelData.levelDimensionX; x++)
            {
                fogField.AddColumn(
                    new LevelColumn(
                        Enumerable.Repeat(
                            LevelColumn.ETileVisibility.Hidden,
                            fogWar.levelData.levelDimensionY
                        )
                    )
                );
            }
        }

        public void ResetTileVisibility()
        {
            fogField.Reset(fogWar);
        }

        #endregion

        #region Shadowcasting Core

        public void ProcessLevelData(Vector2Int origin, int radius)
        {
            Reveal(origin);

            foreach (var q in quadrants)
            {
                CastLight(origin.x, origin.y, 1, 1.0f, 0.0f, radius,
                    q.xx, q.xy, q.yx, q.yy);
            }
        }

        private void CastLight(
            int cx, int cy,
            int row,
            float startSlope,
            float endSlope,
            int radius,
            int xx, int xy,
            int yx, int yy)
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

                    float lSlope = (deltaY - 0.5f) / (dx + 0.5f);
                    float rSlope = (deltaY + 0.5f) / (dx - 0.5f);

                    if (rSlope > startSlope)
                        continue;
                    if (lSlope < endSlope)
                        break;

                    int mapX = cx + dx * xx + dy * xy;
                    int mapY = cy + dx * yx + dy * yy;

                    if (!fogWar.CheckLevelGridRange(new Vector2Int(mapX, mapY)))
                        continue;

                    Reveal(mapX, mapY);

                    bool isBlocked =
                        fogWar.levelData[mapX][mapY] ==
                        csFogWar.LevelColumn.ETileState.Obstacle;

                    if (blocked)
                    {
                        if (isBlocked)
                        {
                            nextStartSlope = rSlope;
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
                        if (isBlocked && distance < radius)
                        {
                            blocked = true;
                            CastLight(cx, cy, distance + 1,
                                startSlope, lSlope, radius,
                                xx, xy, yx, yy);
                            nextStartSlope = rSlope;
                        }
                    }
                }

                if (blocked)
                    break;
            }
        }

        #endregion

        #region Reveal Helpers

        private void Reveal(Vector2Int p)
        {
            if (!fogWar.CheckLevelGridRange(p))
                return;

            fogField[p.x][p.y] = LevelColumn.ETileVisibility.Revealed;
        }

        private void Reveal(int x, int y)
        {
            if (!fogWar.CheckLevelGridRange(new Vector2Int(x, y)))
                return;

            fogField[x][y] = LevelColumn.ETileVisibility.Revealed;
        }

        #endregion
    }
}
