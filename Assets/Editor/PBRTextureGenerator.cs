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
            GenerateFabricLinen();
            GenerateLeather();
            GenerateMonitorScreens();
            GeneratePosters();
            GenerateUISprites();
            GeneratePhoneAssets();
            GenerateParticleTexture();

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

                    float plankHash = Mathf.Abs(Mathf.Sin(plankX * 12.9898f + plankY * 78.233f));
                    float baseTone = 0.46f + plankHash * 0.16f;

                    // Wood grain noise
                    float grain = Mathf.PerlinNoise(x * 0.04f, y * 0.006f) * 0.14f;
                    float fineGrain = Mathf.PerlinNoise(x * 0.18f, y * 0.025f) * 0.06f;

                    // Plank border groove
                    bool isBorder = (localX <= 2 || localX >= plankWidth - 3 || localY <= 2 || localY >= plankLength - 3);
                    float edgeShadow = isBorder ? 0.38f : 1f;

                    float r = (baseTone * 1.15f + grain + fineGrain) * edgeShadow;
                    float g = (baseTone * 0.88f + grain * 0.85f + fineGrain) * edgeShadow;
                    float b = (baseTone * 0.62f + grain * 0.65f) * edgeShadow;

                    albedo.SetPixel(x, y, new Color(r, g, b, 1f));

                    float h = isBorder ? 0.2f : (0.75f + grain * 0.4f);
                    height.SetPixel(x, y, new Color(h, h, h, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/WoodParquet_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 2.2f);
            SaveTexture(normal, $"{TextureDir}/WoodParquet_Normal.png");
        }

        private static void GenerateAcousticWall()
        {
            int size = 1024;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int slatWidth = 48;
            int gapWidth = 16;
            int period = slatWidth + gapWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int modX = x % period;
                    bool isSlat = modX < slatWidth;

                    if (isSlat)
                    {
                        // Slat oak wood
                        int slatIndex = x / period;
                        float slatTone = 0.48f + Mathf.Abs(Mathf.Sin(slatIndex * 33.1f)) * 0.12f;
                        float grain = Mathf.PerlinNoise(x * 0.08f, y * 0.015f) * 0.08f;

                        bool isSlatEdge = (modX <= 1 || modX >= slatWidth - 2);
                        float edgeShadow = isSlatEdge ? 0.65f : 1f;

                        float r = (slatTone * 1.18f + grain) * edgeShadow;
                        float g = (slatTone * 0.88f + grain) * edgeShadow;
                        float b = (slatTone * 0.60f + grain * 0.8f) * edgeShadow;

                        albedo.SetPixel(x, y, new Color(r, g, b, 1f));
                        height.SetPixel(x, y, new Color(0.85f, 0.85f, 0.85f, 1f));
                    }
                    else
                    {
                        // Felt acoustic backing gap (charcoal dark)
                        float feltNoise = (UnityEngine.Random.value * 0.04f);
                        float c = 0.08f + feltNoise;
                        albedo.SetPixel(x, y, new Color(c, c, c * 1.1f, 1f));
                        height.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, 1f));
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
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Calacatta Marble Veins
                    float n1 = Mathf.PerlinNoise(u * 4f, v * 4f);
                    float n2 = Mathf.PerlinNoise(u * 12f + n1 * 2f, v * 12f + n1 * 2f);
                    float n3 = Mathf.PerlinNoise(u * 28f, v * 28f);

                    float vein = Mathf.Sin((u + v + n1 * 0.6f + n2 * 0.3f) * 14f);
                    float veinIntensity = Mathf.Clamp01(1f - Mathf.Abs(vein) * 4f);

                    float fineVein = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((u * 2f - v * 1.5f + n3 * 0.4f) * 22f)) * 8f);

                    float baseBright = 0.94f + n3 * 0.05f;
                    Color baseCol = new Color(baseBright, baseBright * 0.98f, baseBright * 0.96f);
                    Color veinCol = new Color(0.42f, 0.44f, 0.48f);
                    Color goldVein = new Color(0.68f, 0.58f, 0.38f);

                    Color finalCol = Color.Lerp(baseCol, veinCol, veinIntensity * 0.75f);
                    finalCol = Color.Lerp(finalCol, goldVein, fineVein * 0.4f);

                    albedo.SetPixel(x, y, finalCol);

                    float h = 0.9f - (veinIntensity * 0.15f);
                    height.SetPixel(x, y, new Color(h, h, h, 1f));
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
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float p1 = Mathf.PerlinNoise(u * 32f, v * 32f);
                    float p2 = Mathf.PerlinNoise(u * 96f, v * 96f);
                    float stipple = (p1 * 0.7f + p2 * 0.3f) * 0.08f;

                    float tone = 0.91f + stipple;
                    albedo.SetPixel(x, y, new Color(tone, tone * 0.98f, tone * 0.95f, 1f));

                    float h = 0.5f + stipple * 2.5f;
                    height.SetPixel(x, y, new Color(h, h, h, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/ModernPlaster_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 1.8f);
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
                    bool weft = (x % 4 < 2);
                    bool warp = (y % 4 < 2);
                    float thread = (weft ^ warp) ? 0.95f : 0.82f;

                    float slub = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f;
                    float val = (thread + slub) * 0.9f;

                    albedo.SetPixel(x, y, new Color(val, val * 0.98f, val * 0.94f, 1f));
                    height.SetPixel(x, y, new Color(val, val, val, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/FabricLinen_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 2.5f);
            SaveTexture(normal, $"{TextureDir}/FabricLinen_Normal.png");
        }

        private static void GenerateLeather()
        {
            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cell = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                    float micro = Mathf.PerlinNoise(x * 0.5f, y * 0.5f);
                    float pebble = Mathf.Clamp01(cell * 0.7f + micro * 0.3f);

                    float baseDark = 0.14f + pebble * 0.06f;
                    albedo.SetPixel(x, y, new Color(baseDark, baseDark, baseDark * 1.05f, 1f));

                    height.SetPixel(x, y, new Color(pebble, pebble, pebble, 1f));
                }
            }
            albedo.Apply();
            height.Apply();

            SaveTexture(albedo, $"{TextureDir}/Leather_Albedo.png");
            Texture2D normal = GenerateNormalMapFromHeight(height, 2.2f);
            SaveTexture(normal, $"{TextureDir}/Leather_Normal.png");
        }

        private static void GenerateMonitorScreens()
        {
            // Center Monitor: OBS Broadcast Dashboard
            Texture2D center = new Texture2D(1024, 576, TextureFormat.RGBA32, true);
            for (int y = 0; y < 576; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    float u = (float)x / 1024f;
                    float v = (float)y / 576f;
                    Color c = new Color(0.06f, 0.07f, 0.11f, 1f);

                    // Top toolbar
                    if (v > 0.92f) c = new Color(0.12f, 0.14f, 0.18f);

                    // Central Video Feed (Live webcam broadcast frame)
                    if (u > 0.18f && u < 0.74f && v > 0.16f && v < 0.88f)
                    {
                        float roomLight = Mathf.Sin(u * 10f) * 0.1f;
                        c = new Color(0.15f + roomLight, 0.12f, 0.22f + roomLight);

                        // Red LIVE REC badge
                        if (u > 0.20f && u < 0.28f && v > 0.81f && v < 0.86f) c = Color.red;
                    }

                    // Bottom audio meters
                    if (v > 0.04f && v < 0.12f && u > 0.18f && u < 0.74f)
                    {
                        float meter = (u - 0.18f) / 0.56f;
                        c = meter < 0.7f ? Color.green : (meter < 0.88f ? Color.yellow : Color.red);
                    }

                    center.SetPixel(x, y, c);
                }
            }
            center.Apply();
            SaveTexture(center, $"{TextureDir}/Monitor_CenterScreen.png");

            // Left Monitor: Live Chat & Analytics
            Texture2D left = new Texture2D(512, 1024, TextureFormat.RGBA32, true);
            for (int y = 0; y < 1024; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    float u = (float)x / 512f;
                    float v = (float)y / 1024f;
                    Color c = new Color(0.05f, 0.06f, 0.09f, 1f);

                    int line = Mathf.FloorToInt(v * 28f);
                    if (line % 2 == 0 && u > 0.06f && u < 0.92f)
                    {
                        float lineHash = Mathf.Sin(line * 45.12f);
                        c = lineHash > 0f ? new Color(0.14f, 0.18f, 0.26f) : new Color(0.22f, 0.14f, 0.26f);
                    }

                    left.SetPixel(x, y, c);
                }
            }
            left.Apply();
            SaveTexture(left, $"{TextureDir}/Monitor_LeftChat.png");

            // Right Monitor: Audio Mixer & Music Player
            Texture2D right = new Texture2D(1024, 576, TextureFormat.RGBA32, true);
            for (int y = 0; y < 576; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    float u = (float)x / 1024f;
                    float v = (float)y / 576f;
                    Color c = new Color(0.07f, 0.08f, 0.11f, 1f);

                    int band = Mathf.FloorToInt(u * 20f);
                    float bandHeight = Mathf.Abs(Mathf.Sin(band * 3.14f * 0.25f)) * 0.65f + 0.15f;
                    if (v < bandHeight && v > 0.1f && (x % 51 < 38))
                    {
                        c = Color.Lerp(Color.cyan, Color.magenta, v / 0.85f);
                    }

                    right.SetPixel(x, y, c);
                }
            }
            right.Apply();
            SaveTexture(right, $"{TextureDir}/Monitor_RightMixer.png");
        }

        private static void GeneratePosters()
        {
            // Poster 1: Idol Concert
            int pw = 512, ph = 768;
            Texture2D concert = new Texture2D(pw, ph, TextureFormat.RGBA32, true);
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++)
                {
                    float u = (float)x / pw;
                    float v = (float)y / ph;
                    Color bg = Color.Lerp(new Color(0.08f, 0.04f, 0.18f), new Color(0.38f, 0.12f, 0.50f), v);
                    float beam = Mathf.Sin((u - 0.5f) * 14f + v * 5f);
                    if (beam > 0.55f && v > 0.35f) bg += new Color(0.25f, 0.15f, 0.35f);
                    if (x < 16 || x > pw - 17 || y < 16 || y > ph - 17) bg = new Color(0.85f, 0.72f, 0.3f);
                    concert.SetPixel(x, y, bg);
                }
            }
            concert.Apply();
            SaveTexture(concert, $"{TextureDir}/Poster_IdolConcert.png");

            // Poster 2: Gold Play Creator Award
            Texture2D award = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    float u = (float)x / 512f;
                    float v = (float)y / 512f;
                    Color bg = new Color(0.12f, 0.12f, 0.15f);
                    if (u > 0.15f && u < 0.85f && v > 0.2f && v < 0.8f)
                    {
                        bg = new Color(0.85f, 0.74f, 0.32f);
                        float cx = (u - 0.5f) * 3f;
                        float cy = (v - 0.5f) * 3f;
                        if (cx > -0.2f && cx < 0.3f && Mathf.Abs(cy) < (0.3f - cx * 0.5f))
                        {
                            bg = new Color(0.98f, 0.95f, 0.75f);
                        }
                    }
                    if (x < 12 || x > 500 || y < 12 || y > 500) bg = new Color(0.28f, 0.28f, 0.32f);
                    award.SetPixel(x, y, bg);
                }
            }
            award.Apply();
            SaveTexture(award, $"{TextureDir}/Poster_GoldPlayAward.png");
        }

        private static void GenerateUISprites()
        {
            int w = 128, h = 128, r = 24;
            Texture2D box = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int cx = x < r ? r : (x > w - r - 1 ? w - r - 1 : x);
                    int cy = y < r ? r : (y > h - r - 1 ? h - r - 1 : y);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = 1f - Mathf.Clamp01(d - (r - 1.5f));
                    box.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            box.Apply();
            SaveTexture(box, $"{TextureDir}/UIRoundedBox.png");

            Texture2D circle = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(w / 2f, h / 2f);
            float radius = (w / 2f) - 1.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float alpha = 1f - Mathf.Clamp01(d - radius);
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            circle.Apply();
            SaveTexture(circle, $"{TextureDir}/UICircle.png");

            Texture2D ring = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float outerR = (w / 2f) - 1.5f;
            float innerR = outerR - 12f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float outerAlpha = 1f - Mathf.Clamp01(d - outerR);
                    float innerAlpha = Mathf.Clamp01(d - innerR);
                    ring.SetPixel(x, y, new Color(1f, 1f, 1f, outerAlpha * innerAlpha));
                }
            }
            ring.Apply();
            SaveTexture(ring, $"{TextureDir}/UIRing.png");
        }

        private static void GeneratePhoneAssets()
        {
            // Phone Wallpaper (Modern twilight idol abstract wallpaper)
            int pw = 512, ph = 1024;
            Texture2D wall = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++)
                {
                    float u = (float)x / pw;
                    float v = (float)y / ph;
                    Color top = new Color(0.08f, 0.05f, 0.16f);
                    Color mid = new Color(0.24f, 0.10f, 0.38f);
                    Color bot = new Color(0.04f, 0.18f, 0.28f);

                    Color grad = v > 0.5f ? Color.Lerp(mid, top, (v - 0.5f) * 2f) : Color.Lerp(bot, mid, v * 2f);
                    float orb = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.45f)) * 2.2f);
                    grad += new Color(0.35f, 0.15f, 0.45f) * orb;

                    wall.SetPixel(x, y, grad);
                }
            }
            wall.Apply();
            SaveTexture(wall, $"{TextureDir}/Phone_Wallpaper.png");

            // App Icons: Squircle with glyph
            CreateAppIcon("AppIcon_Messages", new Color(0.22f, 0.82f, 0.42f), IconGlyph.ChatBubble);
            CreateAppIcon("AppIcon_Social", new Color(0.92f, 0.25f, 0.55f), IconGlyph.Heart);
            CreateAppIcon("AppIcon_Camera", new Color(0.28f, 0.30f, 0.35f), IconGlyph.Camera);
            CreateAppIcon("AppIcon_Schedule", new Color(0.95f, 0.95f, 0.96f), IconGlyph.Calendar);
            CreateAppIcon("AppIcon_Photos", new Color(0.98f, 0.92f, 0.82f), IconGlyph.PhotoIris);
            CreateAppIcon("AppIcon_Notes", new Color(0.95f, 0.82f, 0.25f), IconGlyph.Notes);
        }

        private enum IconGlyph { ChatBubble, Heart, Camera, Calendar, PhotoIris, Notes }

        private static void CreateAppIcon(string name, Color baseCol, IconGlyph glyph)
        {
            int size = 128;
            int r = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = x < r ? r : (x > size - r - 1 ? size - r - 1 : x);
                    int cy = y < r ? r : (y > size - r - 1 ? size - r - 1 : y);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = 1f - Mathf.Clamp01(d - (r - 1.5f));

                    Color c = baseCol;
                    float distCenter = Vector2.Distance(new Vector2(x, y), center);

                    // Draw glyphs procedurally
                    if (glyph == IconGlyph.ChatBubble)
                    {
                        if (distCenter < 24f || (x > 50 && x < 72 && y > 35 && y < 58))
                        {
                            c = Color.white;
                        }
                    }
                    else if (glyph == IconGlyph.Heart)
                    {
                        float hx = (x - 64f) / 22f;
                        float hy = (y - 60f) / 22f;
                        if ((hx * hx + hy * hy - 1f) * (hx * hx + hy * hy - 1f) * (hx * hx + hy * hy - 1f) - hx * hx * hy * hy * hy <= 0f)
                        {
                            c = Color.white;
                        }
                    }
                    else if (glyph == IconGlyph.Camera)
                    {
                        if ((distCenter > 12f && distCenter < 22f) || (distCenter < 7f) || (Mathf.Abs(x - 64) < 28 && Mathf.Abs(y - 64) < 20))
                        {
                            c = new Color(0.9f, 0.92f, 0.95f);
                        }
                    }
                    else if (glyph == IconGlyph.Calendar)
                    {
                        if (y > 78 && Mathf.Abs(x - 64) < 30) c = Color.red;
                        else if (Mathf.Abs(x - 64) < 26 && Mathf.Abs(y - 58) < 18) c = Color.black;
                    }
                    else if (glyph == IconGlyph.PhotoIris)
                    {
                        if (distCenter < 24f && distCenter > 4f)
                        {
                            float angle = Mathf.Atan2(y - 64, x - 64);
                            c = Color.HSVToRGB(Mathf.Repeat(angle / (2f * Mathf.PI), 1f), 0.75f, 0.95f);
                        }
                    }
                    else if (glyph == IconGlyph.Notes)
                    {
                        if (Mathf.Abs(x - 64) < 24 && Mathf.Abs(y - 64) < 30)
                        {
                            bool line = (y % 10 < 3);
                            c = line ? new Color(0.3f, 0.25f, 0.1f) : Color.white;
                        }
                    }

                    tex.SetPixel(x, y, new Color(c.r, c.g, c.b, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{TextureDir}/{name}.png");
        }

        private static void GenerateParticleTexture()
        {
            // Soft Gaussian circular glow for atmospheric dust motes
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size / 2f, size / 2f);
            float radius = (size / 2f) - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float normD = Mathf.Clamp01(d / radius);
                    float alpha = Mathf.Exp(-normD * normD * 4.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, $"{TextureDir}/Particle_SoftDot.png");
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

                    float hL = heightMap.GetPixel(xL, y).grayscale;
                    float hR = heightMap.GetPixel(xR, y).grayscale;
                    float hD = heightMap.GetPixel(x, yD).grayscale;
                    float hU = heightMap.GetPixel(x, yU).grayscale;

                    float dX = (hR - hL) * strength;
                    float dY = (hU - hD) * strength;

                    Vector3 n = new Vector3(-dX, -dY, 1.0f).normalized;
                    Color col = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    normal.SetPixel(x, y, col);
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
                $"{TextureDir}/FabricLinen_Normal.png",
                $"{TextureDir}/Leather_Normal.png"
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
                $"{TextureDir}/FabricLinen_Albedo.png",
                $"{TextureDir}/Leather_Albedo.png"
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

            string[] spriteFiles = new string[]
            {
                $"{TextureDir}/UIRoundedBox.png",
                $"{TextureDir}/UICircle.png",
                $"{TextureDir}/UIRing.png",
                $"{TextureDir}/Phone_Wallpaper.png",
                $"{TextureDir}/AppIcon_Messages.png",
                $"{TextureDir}/AppIcon_Social.png",
                $"{TextureDir}/AppIcon_Camera.png",
                $"{TextureDir}/AppIcon_Schedule.png",
                $"{TextureDir}/AppIcon_Photos.png",
                $"{TextureDir}/AppIcon_Notes.png",
                $"{TextureDir}/Particle_SoftDot.png"
            };

            foreach (string file in spriteFiles)
            {
                if (File.Exists(file))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        if (file.Contains("RoundedBox"))
                        {
                            importer.spriteBorder = new Vector4(24, 24, 24, 24);
                        }
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
