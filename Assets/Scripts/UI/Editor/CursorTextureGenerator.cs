using UnityEngine;
using UnityEditor;
using System.IO;

namespace KingdomsAtDusk.UI.Editor
{
    /// <summary>
    /// Utility to generate basic cursor textures for the CursorStateManager
    /// </summary>
    public class CursorTextureGenerator : EditorWindow
    {
        [MenuItem("Tools/Generate Cursor Textures")]
        public static void GenerateCursors()
        {
            string folderPath = "Assets/Textures/Cursors";

            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "Cursors");
            }

            // Generate cursor textures
            GenerateNormalCursor(folderPath);
            GenerateMoveCursor(folderPath);
            GenerateAttackCursor(folderPath);
            GenerateInvalidCursor(folderPath);
            GenerateSelectUnitCursor(folderPath);
            GenerateSelectBuildingCursor(folderPath);

            AssetDatabase.Refresh();
        }

        private static void GenerateNormalCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw a simple arrow cursor (white with black outline)
            DrawArrow(pixels, size, Color.black, 1);
            DrawArrow(pixels, size, Color.white, 0);

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorNormal.png");
        }

        private static void GenerateMoveCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw move cursor (4-way arrows in green)
            DrawCross(pixels, size, Color.black, 1);
            DrawCross(pixels, size, Color.green, 0);

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorMove.png");
        }

        private static void GenerateAttackCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw attack cursor (crosshair in red)
            DrawCrosshair(pixels, size, Color.black, 1);
            DrawCrosshair(pixels, size, Color.red, 0);

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorAttack.png");
        }

        private static void GenerateInvalidCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw invalid cursor (X in gray)
            DrawX(pixels, size, Color.black, 1);
            DrawX(pixels, size, Color.gray, 0);

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorInvalid.png");
        }

        private static void DrawArrow(Color[] pixels, int size, Color color, int offset)
        {
            int centerX = size / 2 + offset;
            int centerY = size / 2 + offset;

            // Simple arrow pointing up-left
            for (int y = 0; y < 16; y++)
            {
                SetPixel(pixels, size, 2 + offset, 2 + y + offset, color);
                SetPixel(pixels, size, 3 + offset, 2 + y + offset, color);
            }
            for (int x = 0; x < 10; x++)
            {
                SetPixel(pixels, size, 2 + x + offset, 2 + offset, color);
                SetPixel(pixels, size, 2 + x + offset, 3 + offset, color);
            }
        }

        private static void DrawCross(Color[] pixels, int size, Color color, int offset)
        {
            int center = size / 2;

            // Horizontal line
            for (int x = 6; x < size - 6; x++)
            {
                SetPixel(pixels, size, x + offset, center + offset, color);
                SetPixel(pixels, size, x + offset, center + offset + 1, color);
            }

            // Vertical line
            for (int y = 6; y < size - 6; y++)
            {
                SetPixel(pixels, size, center + offset, y + offset, color);
                SetPixel(pixels, size, center + offset + 1, y + offset, color);
            }

            // Arrow heads
            for (int i = 0; i < 4; i++)
            {
                SetPixel(pixels, size, center - 2 + i + offset, 6 + i + offset, color);
                SetPixel(pixels, size, center - 2 + i + offset, size - 6 - i + offset, color);
                SetPixel(pixels, size, 6 + i + offset, center - 2 + i + offset, color);
                SetPixel(pixels, size, size - 6 - i + offset, center - 2 + i + offset, color);
            }
        }

        private static void DrawCrosshair(Color[] pixels, int size, Color color, int offset)
        {
            int center = size / 2;
            int radius = 10;

            // Circle
            for (int angle = 0; angle < 360; angle += 10)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = center + (int)(Mathf.Cos(rad) * radius);
                int y = center + (int)(Mathf.Sin(rad) * radius);
                SetPixel(pixels, size, x + offset, y + offset, color);
            }

            // Cross lines
            for (int i = -8; i <= 8; i++)
            {
                if (i > -3 && i < 3) continue; // Gap in center
                SetPixel(pixels, size, center + i + offset, center + offset, color);
                SetPixel(pixels, size, center + offset, center + i + offset, color);
            }
        }

        private static void DrawX(Color[] pixels, int size, Color color, int offset)
        {
            // Diagonal lines forming an X
            for (int i = 6; i < size - 6; i++)
            {
                SetPixel(pixels, size, i + offset, i + offset, color);
                SetPixel(pixels, size, i + 1 + offset, i + offset, color);
                SetPixel(pixels, size, i + offset, i + 1 + offset, color);

                SetPixel(pixels, size, i + offset, size - 1 - i + offset, color);
                SetPixel(pixels, size, i + 1 + offset, size - 1 - i + offset, color);
                SetPixel(pixels, size, i + offset, size - 2 - i + offset, color);
            }
        }

        private static void GenerateSelectUnitCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw select unit cursor (hand pointer in cyan)
            DrawHandPointer(pixels, size, Color.black, 1);
            DrawHandPointer(pixels, size, Color.cyan, 0);

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorSelectUnit.png");
        }

        private static void GenerateSelectBuildingCursor(string folder)
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            // Transparent background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw select building cursor (house icon in yellow/orange)
            DrawHouseIcon(pixels, size, Color.black, 1);
            DrawHouseIcon(pixels, size, new Color(1f, 0.7f, 0f), 0); // Orange

            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexture(tex, folder + "/CursorSelectBuilding.png");
        }

        private static void DrawHandPointer(Color[] pixels, int size, Color color, int offset)
        {
            int centerX = size / 2 + offset;
            int centerY = size / 2 + offset;

            // Draw a simple hand pointer (finger pointing up)
            // Finger
            for (int y = 4; y < 18; y++)
            {
                SetPixel(pixels, size, centerX + offset, y + offset, color);
                SetPixel(pixels, size, centerX + 1 + offset, y + offset, color);
            }

            // Thumb
            for (int x = -2; x <= 2; x++)
            {
                SetPixel(pixels, size, centerX + x + offset, 18 + offset, color);
                SetPixel(pixels, size, centerX + x + offset, 19 + offset, color);
            }

            // Palm
            for (int y = 18; y < 24; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    SetPixel(pixels, size, centerX + x + offset, y + offset, color);
                }
            }
        }

        private static void DrawHouseIcon(Color[] pixels, int size, Color color, int offset)
        {
            int centerX = size / 2;
            int centerY = size / 2;

            // Roof (triangle)
            for (int y = 0; y < 8; y++)
            {
                int width = y + 1;
                for (int x = -width; x <= width; x++)
                {
                    SetPixel(pixels, size, centerX + x + offset, 6 + y + offset, color);
                }
            }

            // Walls (rectangle)
            for (int y = 14; y < 26; y++)
            {
                for (int x = -6; x <= 6; x++)
                {
                    // Outer walls
                    if (x == -6 || x == 6 || y == 14 || y == 25)
                    {
                        SetPixel(pixels, size, centerX + x + offset, y + offset, color);
                    }
                }
            }

            // Door
            for (int y = 18; y < 26; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    SetPixel(pixels, size, centerX + x + offset, y + offset, color);
                }
            }
        }

        private static void SetPixel(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
            {
                pixels[y * size + x] = color;
            }
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            // Import settings for cursor
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}
