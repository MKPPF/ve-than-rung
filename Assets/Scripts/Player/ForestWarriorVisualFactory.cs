using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Generates and provides runtime sprites for the default Forest Warrior hero:
    /// blue-hooded and cloaked adventurer with golden trim and leather boots,
    /// and the four core elemental starting skill icons.
    /// </summary>
    public static class ForestWarriorVisualFactory
    {
        private static Sprite portraitSprite;
        private static readonly Sprite[] skillIcons = new Sprite[4];

        private static readonly Color OutlineDark = new Color(0.12f, 0.08f, 0.05f, 1f);
        private static readonly Color HoodBlue = new Color(0.02f, 0.27f, 0.54f, 1f);
        private static readonly Color HoodBlueLight = new Color(0.08f, 0.42f, 0.76f, 1f);
        private static readonly Color TrimGold = new Color(1.00f, 0.87f, 0.45f, 1f);
        private static readonly Color HairBrown = new Color(0.27f, 0.13f, 0.05f, 1f);
        private static readonly Color SkinTone = new Color(0.98f, 0.82f, 0.64f, 1f);
        private static readonly Color BeltBrown = new Color(0.42f, 0.24f, 0.12f, 1f);
        private static readonly Color TrousersWhite = new Color(0.85f, 0.88f, 0.92f, 1f);
        private static readonly Color BootsBrown = new Color(0.32f, 0.18f, 0.08f, 1f);

        private static bool IsValidSprite(Sprite s)
        {
            return s != null && s.texture != null;
        }

        public static Sprite GetHeroPortraitSprite()
        {
            if (IsValidSprite(portraitSprite))
            {
                return portraitSprite;
            }

            // 1. Try to fetch the authored idle sprite from the PlayerCharacterAnimator in scene
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerCharacterAnimator animator = player.GetComponent<PlayerCharacterAnimator>();
                if (animator != null && IsValidSprite(animator.IdleSprite) &&
                    !ThanhGiongHeroController.IsCustomHeroSprite(animator.IdleSprite))
                {
                    portraitSprite = animator.IdleSprite;
                    return portraitSprite;
                }
            }

#if UNITY_EDITOR
            // 2. In Unity Editor, load the authored Idle_00 frame or Player.png
            Sprite idleAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Generated/Characters/PlayerV2/Idle_00.png");
            if (idleAsset != null)
            {
                portraitSprite = idleAsset;
                return portraitSprite;
            }

            Sprite playerAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Generated/Characters/Player.png");
            if (playerAsset != null)
            {
                portraitSprite = playerAsset;
                return portraitSprite;
            }
#endif

            // 3. Fallback procedural portrait matching the blue hood & adventurer appearance
            portraitSprite = BuildDedicatedPortraitSprite();
            return portraitSprite;
        }

        public static Sprite GetSkillIcon(int slotIndex)
        {
            int clamped = Mathf.Clamp(slotIndex, 0, 3);
            if (IsValidSprite(skillIcons[clamped]))
            {
                return skillIcons[clamped];
            }

#if UNITY_EDITOR
            string iconName = clamped switch
            {
                0 => "Fireball",
                1 => "IceDash",
                2 => "SwordSpin",
                _ => "LightningStrike"
            };
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Generated/SkillIcons/" + iconName + ".png");
            if (loaded != null)
            {
                skillIcons[clamped] = loaded;
                return loaded;
            }
#endif

            skillIcons[clamped] = BuildSkillIcon(clamped);
            return skillIcons[clamped];
        }

        private static Sprite BuildDedicatedPortraitSprite()
        {
            const int size = 160;
            Texture2D tex = CreateBlankTexture(size, size, "ForestWarrior_Portrait_Fallback");
            Color[] pixels = new Color[size * size];

            // Background with rich fantasy forest hue & golden frame
            Color bgDark = new Color(0.08f, 0.14f, 0.12f, 1f);
            DrawRoundedBox(pixels, size, size, 2, 2, size - 4, size - 4, bgDark, TrimGold);

            // Torso & Blue Cloak
            DrawOutlinedEllipse(pixels, size, size, 80, 52, 26, 32, HoodBlueLight, HoodBlue, OutlineDark);
            // White trousers
            DrawRoundedBox(pixels, size, size, 66, 16, 28, 20, TrousersWhite, OutlineDark);
            // Boots
            DrawRoundedBox(pixels, size, size, 62, 4, 14, 16, BootsBrown, OutlineDark);
            DrawRoundedBox(pixels, size, size, 84, 4, 14, 16, BootsBrown, OutlineDark);
            // Belt with golden buckle
            DrawRoundedBox(pixels, size, size, 64, 46, 32, 8, BeltBrown, OutlineDark);
            DrawCircle(pixels, size, size, 80, 50, 3f, TrimGold);

            // Blue Hood (surrounding head)
            DrawOutlinedEllipse(pixels, size, size, 80, 106, 28, 30, HoodBlueLight, HoodBlue, OutlineDark);
            // Golden hood trim rim
            DrawEllipse(pixels, size, size, 80, 102, 24, 25, TrimGold);

            // Face (inside hood)
            DrawCircle(pixels, size, size, 80, 100, 19f, SkinTone);
            // Brown hair bangs
            DrawThickLine(pixels, size, size, new Vector2(66, 112), new Vector2(94, 112), 4f, HairBrown);
            DrawTriangle(pixels, size, size, new Vector2(70, 114), new Vector2(78, 104), new Vector2(86, 114), HairBrown);

            // Adventurer Eyes
            DrawCircle(pixels, size, size, 73, 98, 2.5f, OutlineDark);
            DrawCircle(pixels, size, size, 87, 98, 2.5f, OutlineDark);
            DrawCircle(pixels, size, size, 74, 99, 1f, Color.white);
            DrawCircle(pixels, size, size, 88, 99, 1f, Color.white);

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildSkillIcon(int slotIndex)
        {
            const int size = 64;
            Texture2D tex = CreateBlankTexture(size, size, "ForestSkillIcon_" + slotIndex);
            Color[] pixels = new Color[size * size];

            Color bg = new Color(0.10f, 0.12f, 0.18f, 1f);
            Color border = TrimGold;
            DrawRoundedBox(pixels, size, size, 1, 1, size - 2, size - 2, bg, border);

            switch (slotIndex)
            {
                case 0: // Fireball (Cầu Lửa)
                    DrawCircle(pixels, size, size, 32, 32, 16f, new Color(1f, 0.45f, 0.1f, 1f));
                    DrawCircle(pixels, size, size, 32, 32, 10f, new Color(1f, 0.85f, 0.2f, 1f));
                    DrawCircle(pixels, size, size, 32, 32, 4f, Color.white);
                    break;
                case 1: // IceDash (Lướt Băng)
                    DrawThickLine(pixels, size, size, new Vector2(16, 20), new Vector2(48, 44), 6f, new Color(0.3f, 0.8f, 1f, 1f));
                    DrawThickLine(pixels, size, size, new Vector2(22, 22), new Vector2(42, 42), 3f, Color.white);
                    break;
                case 2: // SwordSpin (Kiếm Luân)
                    DrawCircle(pixels, size, size, 32, 32, 15f, new Color(0.85f, 0.75f, 0.3f, 1f));
                    DrawCircle(pixels, size, size, 32, 32, 9f, bg);
                    DrawThickLine(pixels, size, size, new Vector2(18, 18), new Vector2(46, 46), 4f, Color.white);
                    break;
                default: // LightningStrike (Thiên Lôi)
                    DrawThickLine(pixels, size, size, new Vector2(36, 52), new Vector2(26, 32), 4f, new Color(0.95f, 0.95f, 0.35f, 1f));
                    DrawThickLine(pixels, size, size, new Vector2(26, 32), new Vector2(38, 32), 4f, new Color(0.95f, 0.95f, 0.35f, 1f));
                    DrawThickLine(pixels, size, size, new Vector2(38, 32), new Vector2(28, 12), 4f, Color.white);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D CreateBlankTexture(int width, int height, string name)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] clear = new Color[width * height];
            tex.SetPixels(clear);
            tex.Apply(false, false);
            return tex;
        }

        private static void BlendPixel(Color[] pixels, int width, int x, int y, Color color)
        {
            if (x < 0 || x >= width || y < 0 || y >= pixels.Length / width) return;
            int idx = y * width + x;
            if (color.a >= 0.999f)
            {
                pixels[idx] = color;
            }
            else if (color.a > 0.001f)
            {
                Color prev = pixels[idx];
                float outA = color.a + prev.a * (1f - color.a);
                if (outA > 0f)
                {
                    Color blended = (color * color.a + prev * prev.a * (1f - color.a)) / outA;
                    blended.a = outA;
                    pixels[idx] = blended;
                }
            }
        }

        private static void DrawCircle(Color[] pixels, int width, int height, int cx, int cy, float radius, Color color)
        {
            int r = Mathf.CeilToInt(radius);
            float rSq = radius * radius;
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(height - 1, cy + r); y++)
            {
                for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(width - 1, cx + r); x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipse(Color[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color color)
        {
            float rxSq = Mathf.Max(1, rx * rx);
            float rySq = Mathf.Max(1, ry * ry);
            for (int y = Mathf.Max(0, cy - ry); y <= Mathf.Min(height - 1, cy + ry); y++)
            {
                for (int x = Mathf.Max(0, cx - rx); x <= Mathf.Min(width - 1, cx + rx); x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if ((dx * dx) / rxSq + (dy * dy) / rySq <= 1f)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawOutlinedEllipse(Color[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color fill, Color shadow, Color outline)
        {
            DrawEllipse(pixels, width, height, cx, cy, rx + 2, ry + 2, outline);
            DrawEllipse(pixels, width, height, cx, cy, rx, ry, shadow);
            DrawEllipse(pixels, width, height, cx, cy + 2, rx - 1, ry - 2, fill);
        }

        private static void DrawRoundedBox(Color[] pixels, int width, int height, int x0, int y0, int w, int h, Color fill, Color border)
        {
            int x1 = Mathf.Min(width - 1, x0 + w);
            int y1 = Mathf.Min(height - 1, y0 + h);
            for (int y = Mathf.Max(0, y0); y <= y1; y++)
            {
                for (int x = Mathf.Max(0, x0); x <= x1; x++)
                {
                    bool edge = x <= x0 + 1 || x >= x1 - 1 || y <= y0 + 1 || y >= y1 - 1;
                    BlendPixel(pixels, width, x, y, edge ? border : fill);
                }
            }
        }

        private static void DrawThickLine(Color[] pixels, int width, int height, Vector2 a, Vector2 b, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1));
            Vector2 ab = b - a;
            float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);
            float rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                    Vector2 proj = a + ab * t;
                    if ((p - proj).sqrMagnitude <= rSq)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawTriangle(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Sign(p, a, b);
                    float d2 = Sign(p, b, c);
                    float d3 = Sign(p, c, a);
                    bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
                    bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
                    if (!(hasNeg && hasPos))
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
