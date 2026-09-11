using UnityEngine;

namespace TopDownGame.Core
{
    public static class RuntimeSpriteFactory
    {
        private static Sprite whiteSprite;
        private static Sprite circleSprite;
        private static Sprite softGlowSprite;
        private static Sprite ringSprite;

        private const int CircleResolution = 64;
        private const int RingResolution = 64;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "RuntimeWhiteTexture";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                Object.DontDestroyOnLoad(texture);

                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f), 1f);
                whiteSprite.name = "RuntimeWhiteSprite";
                Object.DontDestroyOnLoad(whiteSprite);
                return whiteSprite;
            }
        }

        /// <summary>
        /// Hình tròn trắng với viền mềm (filled circle, hard edge).
        /// Dùng cho indicator, ground mark.
        /// </summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null)
                {
                    return circleSprite;
                }

                int size = CircleResolution;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "RuntimeCircleTexture";
                texture.filterMode = FilterMode.Bilinear;
                float center = (size - 1) * 0.5f;
                float radiusSq = center * center;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distSq = dx * dx + dy * dy;
                        // Anti-alias viền: smooth 1-2 pixel
                        float dist = Mathf.Sqrt(distSq);
                        float alpha = Mathf.Clamp01((center - dist) / 1.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                Object.DontDestroyOnLoad(texture);
                circleSprite = Sprite.Create(texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f), size);
                circleSprite.name = "RuntimeCircleSprite";
                Object.DontDestroyOnLoad(circleSprite);
                return circleSprite;
            }
        }

        /// <summary>
        /// Hình tròn phát sáng mềm (soft glow), alpha giảm dần từ tâm ra ngoài.
        /// Dùng cho ground glow, ánh sáng mặt đất.
        /// </summary>
        public static Sprite SoftGlowSprite
        {
            get
            {
                if (softGlowSprite != null)
                {
                    return softGlowSprite;
                }

                int size = CircleResolution;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "RuntimeSoftGlowTexture";
                texture.filterMode = FilterMode.Bilinear;
                float center = (size - 1) * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float t = dist / center;
                        // Gaussian-like falloff
                        float alpha = Mathf.Exp(-t * t * 3f);
                        if (t > 1f) alpha = 0f;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                Object.DontDestroyOnLoad(texture);
                softGlowSprite = Sprite.Create(texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f), size);
                softGlowSprite.name = "RuntimeSoftGlowSprite";
                Object.DontDestroyOnLoad(softGlowSprite);
                return softGlowSprite;
            }
        }

        /// <summary>
        /// Vòng tròn rỗng (ring/donut), dùng cho shockwave/sóng xung kích.
        /// </summary>
        public static Sprite RingSprite
        {
            get
            {
                if (ringSprite != null)
                {
                    return ringSprite;
                }

                int size = RingResolution;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "RuntimeRingTexture";
                texture.filterMode = FilterMode.Bilinear;
                float center = (size - 1) * 0.5f;
                float outerRadius = center;
                float innerRadius = center * 0.75f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        // Ring: opaque between inner and outer, smooth edges
                        float outerAlpha = Mathf.Clamp01((outerRadius - dist) / 1.5f);
                        float innerAlpha = Mathf.Clamp01((dist - innerRadius) / 1.5f);
                        float alpha = outerAlpha * innerAlpha;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                Object.DontDestroyOnLoad(texture);
                ringSprite = Sprite.Create(texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f), size);
                ringSprite.name = "RuntimeRingSprite";
                Object.DontDestroyOnLoad(ringSprite);
                return ringSprite;
            }
        }
    }
}
