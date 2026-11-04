using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace StreamerGame.Editor
{
    public static class PBRTextureGenerator
    {
        private const string TextureDir = "Assets/Textures";

        public static void GenerateAllTextures()
        {
            if (!Directory.Exists(TextureDir))
            {
                Directory.CreateDirectory(TextureDir);
            }

            GenerateWoodParquet();
            GenerateAcousticWall();
            GenerateMarbleCounter();
            GeneratePlasterWall();
            GenerateMonitorScreens();
            GenerateFabricLinen();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImporter();
        }

        private static void GenerateWoodParquet()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int plankWidth = 128;
            int plankLength = 512;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plankX = x / plankWidth;
                    int plankY = y / plankLength;
                    int localX = x % plankWidth;
                    int localY = y % plankLength;

                    // Plank tone variation
                    float plankHash = Mathf.Abs(Mathf.Sin(plankX * 12.9898f + plankY * 78.233f));
                    float baseTone = 0.40f + plankHash * 0.15f;

                    // Wood grain noise
                    float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.005f) * 0.12f;
                    float fineGrain = Mathf.PerlinNoise(x * 0.2f, y * 0.02f) * 0.05f;

                    // Plank border groove
                    bool isBorder = (localX <= 2 || localX >= plankWidth - 3 || localY <= 2 || localY >= plankLength - 3);
                    float edgeShadow = isBorder ? 0.35f : 1f;

                    float r = (baseTone * 1.1f + grain + fineGrain) * edgeShadow;
                    float g = (baseTone * 0.75f + grain * 0.8f + fineGrain) * edgeShadow;
                    float b = (baseTone * 0.45f + grain * 0.6f + fineGrain) * edgeShadow;

                    albedo.SetPixel(x, y, new Color(r, g, b, 1f));
                    float h = isBorder ? 0.1f : (0.8f + grain * 0.5f);
                    height.SetPixel(x, y, new Color(h, h, h, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/WoodParquet_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 2.5f);
            SaveTexture(normal, $"{TextureDir}/WoodParquet_Normal.png");
        }

        private static void GenerateAcousticWall()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int slatWidth = 32;
            int slatGap = 16;
            int period = slatWidth + slatGap;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int pos = x % period;
                    bool isSlat = pos < slatWidth;

                    if (isSlat)
                    {
                        // Modern light oak slat
                        float woodNoise = Mathf.PerlinNoise(x * 0.1f, y * 0.01f) * 0.08f;
                        albedo.SetPixel(x, y, new Color(0.68f + woodNoise, 0.48f + woodNoise, 0.32f + woodNoise, 1f));
                        height.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        // Dark felt backing
                        float feltNoise = (UnityEngine.Random.value - 0.5f) * 0.04f;
                        albedo.SetPixel(x, y, new Color(0.08f + feltNoise, 0.09f + feltNoise, 0.12f + feltNoise, 1f));
                        height.SetPixel(x, y, Color.black);
                    }
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/AcousticWall_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 3.5f);
            SaveTexture(normal, $"{TextureDir}/AcousticWall_Normal.png");
        }

        private static void GenerateMarbleCounter()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;

                    // Vein turbulence
                    float vein1 = Mathf.Sin((nx * 6f + Mathf.PerlinNoise(nx * 4f, ny * 4f) * 5f) * Mathf.PI);
                    float vein2 = Mathf.Sin((ny * 5f + Mathf.PerlinNoise(nx * 6f, ny * 6f) * 4f) * Mathf.PI);
                    float v = Mathf.Clamp01(Mathf.Abs(vein1 * vein2));

                    float baseBright = 0.92f + Mathf.PerlinNoise(nx * 10f, ny * 10f) * 0.06f;
                    float veinDarkness = Mathf.Lerp(baseBright, 0.35f, Mathf.Pow(1f - v, 12f));

                    albedo.SetPixel(x, y, new Color(veinDarkness, veinDarkness * 0.98f, veinDarkness * 0.95f, 1f));
                    height.SetPixel(x, y, new Color(veinDarkness, veinDarkness, veinDarkness, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/MarbleCounter_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 1.2f);
            SaveTexture(normal, $"{TextureDir}/MarbleCounter_Normal.png");
        }

        private static void GeneratePlasterWall()
        {
            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.04f;
                    float fine = (UnityEngine.Random.value - 0.5f) * 0.02f;
                    float col = 0.92f + n + fine;

                    albedo.SetPixel(x, y, new Color(col, col, col * 1.02f, 1f));
                    height.SetPixel(x, y, new Color(n + fine, n + fine, n + fine, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/ModernPlaster_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 1.0f);
            SaveTexture(normal, $"{TextureDir}/ModernPlaster_Normal.png");
        }

        private static void GenerateFabricLinen()
        {
            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool cross = ((x / 2 + y / 2) % 2 == 0);
                    float weave = cross ? 0.08f : -0.08f;
                    float fold = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.15f;

                    float col = 0.82f + weave + fold;
                    albedo.SetPixel(x, y, new Color(col * 0.85f, col * 0.90f, col, 1f));
                    height.SetPixel(x, y, new Color(weave, weave, weave, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/FabricLinen_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 1.8f);
            SaveTexture(normal, $"{TextureDir}/FabricLinen_Normal.png");
        }

        private static void GenerateMonitorScreens()
        {
            // Center Monitor: Main Stream & Game Broadcast
            int w = 1024, h = 576;
            Texture2D center = new Texture2D(w, h, TextureFormat.RGBA32, true);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;

                    // Cyberpunk / Modern Game aesthetic background
                    float bg = Mathf.Sin(u * 10f + v * 5f) * 0.15f + 0.25f;
                    Color c = new Color(bg * 0.3f, bg * 0.6f, bg * 1.2f, 1f);

                    // Top stream status bar
                    if (v > 0.92f) c = new Color(0.05f, 0.05f, 0.08f, 1f);
                    // Red LIVE indicator
                    if (v > 0.93f && v < 0.97f && u > 0.03f && u < 0.08f) c = Color.red;
                    // Facecam box (Bottom right)
                    if (u > 0.72f && u < 0.97f && v > 0.05f && v < 0.38f)
                    {
                        bool border = (u < 0.73f || u > 0.96f || v < 0.06f || v > 0.37f);
                        c = border ? new Color(0f, 0.8f, 1f) : new Color(0.2f, 0.25f, 0.35f);
                    }

                    center.SetPixel(x, y, c);
                }
            }
            center.Apply();
            SaveTexture(center, $"{TextureDir}/Monitor_CenterScreen.png");

            // Left Monitor: Chat feed & analytics
            Texture2D left = new Texture2D(512, 1024, TextureFormat.RGBA32, true);
            for (int y = 0; y < 1024; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    float u = (float)x / 512f;
                    float v = (float)y / 1024f;
                    Color c = new Color(0.06f, 0.07f, 0.10f, 1f);

                    // Chat lines simulation
                    int line = Mathf.FloorToInt(v * 24f);
                    if (line % 2 == 0 && u > 0.05f && u < 0.92f)
                    {
                        float lineHash = Mathf.Sin(line * 45.12f);
                        c = lineHash > 0f ? new Color(0.12f, 0.15f, 0.22f) : new Color(0.18f, 0.12f, 0.22f);
                    }

                    left.SetPixel(x, y, c);
                }
            }
            left.Apply();
            SaveTexture(left, $"{TextureDir}/Monitor_LeftChat.png");

            // Right Monitor: Audio Mixer & Music
            Texture2D right = new Texture2D(1024, 576, TextureFormat.RGBA32, true);
            for (int y = 0; y < 576; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    float u = (float)x / 1024f;
                    float v = (float)y / 576f;
                    Color c = new Color(0.08f, 0.09f, 0.12f, 1f);

                    // Audio EQ bands
                    int band = Mathf.FloorToInt(u * 16f);
                    float bandHeight = Mathf.Abs(Mathf.Sin(band * 3.14f * 0.3f)) * 0.6f + 0.15f;
                    if (v < bandHeight && v > 0.1f && (x % 64 < 48))
                    {
                        c = Color.Lerp(Color.cyan, Color.magenta, v / 0.8f);
                    }

                    right.SetPixel(x, y, c);
                }
            }
            right.Apply();
            SaveTexture(right, $"{TextureDir}/Monitor_RightMixer.png");
        }

        private static Texture2D GenerateNormalMapFromHeight(Texture2D heightMap, float strength)
        {
            int w = heightMap.width;
            int h = heightMap.height;
            Texture2D normal = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int xL = (x - 1 + w) % w;
                    int xR = (x + 1) % w;
                    int yD = (y - 1 + h) % h;
                    int yU = (y + 1) % h;

                    float hL = heightMap.GetPixel(xL, y).r;
                    float hR = heightMap.GetPixel(xR, y).r;
                    float hD = heightMap.GetPixel(x, yD).r;
                    float hU = heightMap.GetPixel(x, yU).r;

                    float dX = (hR - hL) * strength;
                    float dY = (hU - hD) * strength;

                    Vector3 n = new Vector3(-dX, -dY, 1f).normalized;
                    Color nCol = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    normal.SetPixel(x, y, nCol);
                }
            }
            normal.Apply();
            return normal;
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureTextureImporter()
        {
            string[] normalFiles = new string[]
            {
                $"{TextureDir}/WoodParquet_Normal.png",
                $"{TextureDir}/AcousticWall_Normal.png",
                $"{TextureDir}/MarbleCounter_Normal.png",
                $"{TextureDir}/ModernPlaster_Normal.png",
                $"{TextureDir}/FabricLinen_Normal.png"
            };

            foreach (string file in normalFiles)
            {
                if (File.Exists(file))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.wrapMode = TextureWrapMode.Repeat;
                        importer.anisoLevel = 4;
                        importer.SaveAndReimport();
                    }
                }
            }

            string[] albedoFiles = new string[]
            {
                $"{TextureDir}/WoodParquet_Albedo.png",
                $"{TextureDir}/AcousticWall_Albedo.png",
                $"{TextureDir}/MarbleCounter_Albedo.png",
                $"{TextureDir}/ModernPlaster_Albedo.png",
                $"{TextureDir}/FabricLinen_Albedo.png"
            };

            foreach (string file in albedoFiles)
            {
                if (File.Exists(file))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer != null)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        importer.anisoLevel = 4;
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
