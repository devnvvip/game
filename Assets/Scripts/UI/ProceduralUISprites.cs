using UnityEngine;

namespace StreamerGame.UI
{
    public static class ProceduralUISprites
    {
        private static Sprite roundedBoxSprite;
        private static Sprite circleSprite;
        private static Sprite ringSprite;

        public static Sprite GetRoundedBox()
        {
            if (roundedBoxSprite == null)
            {
                roundedBoxSprite = CreateRoundedRectSprite(128, 128, 28, new Color(1f, 1f, 1f, 1f));
            }
            return roundedBoxSprite;
        }

        public static Sprite GetCircle()
        {
            if (circleSprite == null)
            {
                circleSprite = CreateCircleSprite(128, true);
            }
            return circleSprite;
        }

        public static Sprite GetRing()
        {
            if (ringSprite == null)
            {
                ringSprite = CreateRingSprite(128, 12);
            }
            return ringSprite;
        }

        private static Sprite CreateRoundedRectSprite(int w, int h, int r, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = 0f;
                    int cx = x < r ? r : (x > w - r - 1 ? w - r - 1 : x);
                    int cy = y < r ? r : (y > h - r - 1 ? h - r - 1 : y);
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                    float alpha = 1f - Mathf.Clamp01(dist - (r - 1.5f));
                    colors[y * w + x] = new Color(col.r, col.g, col.b, alpha * col.a);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        private static Sprite CreateCircleSprite(int size, bool filled)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float radius = (size / 2f) - 1.5f;
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01(d - radius);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateRingSprite(int size, int thickness)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float outerR = (size / 2f) - 1.5f;
            float innerR = outerR - thickness;
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float outerAlpha = 1f - Mathf.Clamp01(d - outerR);
                    float innerAlpha = Mathf.Clamp01(d - innerR);
                    float alpha = outerAlpha * innerAlpha;
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
