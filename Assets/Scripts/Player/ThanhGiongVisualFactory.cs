using System;
using System.IO;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Generates and caches runtime sprites for the Thanh Giong hero mode:
    /// golden-armored hero riding the fire-breathing Iron Horse, Iron Sword,
    /// single Ivory Bamboo, multi-stalk Ivory Bamboo bundle, and skill icons.
    /// </summary>
    public static class ThanhGiongVisualFactory
    {
        private static Sprite[] mountedIdleAndGallopSprites;
        private static Sprite[] mountedSwordAttackSprites;
        private static Sprite[] mountedBambooAttackSprites;
        private static Sprite ironSwordSprite;
        private static Sprite singleBambooSprite;
        private static Sprite bambooBundleSprite;
        private static Sprite slashArcSprite;
        private static Sprite flameSprite;
        private static Sprite bambooLeafSprite;
        private static Sprite shockwaveRingSprite;
        private static Sprite shrineStatueSprite;
        private static Sprite portraitSprite;
        private static readonly Sprite[] skillIcons = new Sprite[4];

        private static readonly Color OutlineDark = new Color(0.14f, 0.09f, 0.05f, 1f);
        private static readonly Color HorseSilverLight = new Color(0.91f, 0.93f, 0.97f, 1f);
        private static readonly Color HorseSilverMid = new Color(0.74f, 0.77f, 0.84f, 1f);
        private static readonly Color HorseSilverShadow = new Color(0.52f, 0.55f, 0.64f, 1f);
        private static readonly Color HorseManeDark = new Color(0.36f, 0.33f, 0.34f, 1f);
        private static readonly Color GoldBright = new Color(1.00f, 0.84f, 0.22f, 1f);
        private static readonly Color GoldMid = new Color(0.92f, 0.64f, 0.14f, 1f);
        private static readonly Color GoldShadow = new Color(0.68f, 0.41f, 0.09f, 1f);
        private static readonly Color ArmorCrimson = new Color(0.86f, 0.22f, 0.12f, 1f);
        private static readonly Color SkinTone = new Color(0.96f, 0.79f, 0.62f, 1f);
        private static readonly Color BambooGreen = new Color(0.56f, 0.76f, 0.20f, 1f);
        private static readonly Color BambooGold = new Color(0.82f, 0.86f, 0.28f, 1f);
        private static readonly Color BambooNode = new Color(0.34f, 0.48f, 0.12f, 1f);
        private static readonly Color SteelLight = new Color(0.88f, 0.94f, 1.00f, 1f);
        private static readonly Color SteelMid = new Color(0.58f, 0.68f, 0.78f, 1f);
        private static readonly Color FlameCore = new Color(1.00f, 0.96f, 0.55f, 1f);
        private static readonly Color FlameMid = new Color(1.00f, 0.58f, 0.12f, 1f);
        private static readonly Color FlameOuter = new Color(0.92f, 0.22f, 0.06f, 1f);

        private static bool IsValidSprite(Sprite s)
        {
            return s != null && s.texture != null;
        }

        private static bool IsValidArray(Sprite[] arr)
        {
            if (arr == null || arr.Length != 4) return false;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null || arr[i].texture == null) return false;
            }
            return true;
        }

        public static Sprite GetMountedHeroSprite(int strideFrame, bool swordPose, bool bambooChargePose)
        {
            EnsureCharacterSprites();
            int index = Mathf.Abs(strideFrame) % 4;
            if (swordPose)
            {
                return mountedSwordAttackSprites != null && mountedSwordAttackSprites.Length > index
                    ? mountedSwordAttackSprites[index]
                    : null;
            }

            if (bambooChargePose)
            {
                return mountedBambooAttackSprites != null && mountedBambooAttackSprites.Length > index
                    ? mountedBambooAttackSprites[index]
                    : null;
            }

            return mountedIdleAndGallopSprites != null && mountedIdleAndGallopSprites.Length > index
                ? mountedIdleAndGallopSprites[index]
                : null;
        }

        public static Sprite GetIronSwordSprite()
        {
            if (!IsValidSprite(ironSwordSprite))
            {
                ironSwordSprite = BuildIronSwordSprite();
            }

            return ironSwordSprite;
        }

        public static Sprite GetSingleBambooSprite()
        {
            if (!IsValidSprite(singleBambooSprite))
            {
                singleBambooSprite = BuildSingleBambooSprite();
            }

            return singleBambooSprite;
        }

        public static Sprite GetBambooBundleSprite()
        {
            if (!IsValidSprite(bambooBundleSprite))
            {
                bambooBundleSprite = BuildBambooBundleSprite();
            }

            return bambooBundleSprite;
        }

        public static Sprite GetSlashArcSprite()
        {
            if (!IsValidSprite(slashArcSprite))
            {
                slashArcSprite = BuildSlashArcSprite();
            }

            return slashArcSprite;
        }

        public static Sprite GetFlameSprite()
        {
            if (!IsValidSprite(flameSprite))
            {
                flameSprite = BuildFlameSprite();
            }

            return flameSprite;
        }

        public static Sprite GetBambooLeafSprite()
        {
            if (!IsValidSprite(bambooLeafSprite))
            {
                bambooLeafSprite = BuildBambooLeafSprite();
            }

            return bambooLeafSprite;
        }

        public static Sprite GetShockwaveRingSprite()
        {
            if (!IsValidSprite(shockwaveRingSprite))
            {
                shockwaveRingSprite = BuildShockwaveRingSprite();
            }

            return shockwaveRingSprite;
        }

        public static Sprite GetShrineStatueSprite()
        {
            if (!IsValidSprite(shrineStatueSprite))
            {
                shrineStatueSprite = BuildShrineStatueSprite();
            }

            return shrineStatueSprite;
        }

        public static Sprite GetHeroPortraitSprite()
        {
            if (IsValidSprite(portraitSprite))
            {
                return portraitSprite;
            }

#if UNITY_EDITOR
            // 1. In Unity Editor, load the officially imported Sprite
            Sprite editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Characters/ThanhGiong_Illustration.jpg");
            if (editorAsset == null)
            {
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/Characters/ThanhGiong_Illustration.jpg");
                foreach (var obj in allAssets)
                {
                    if (obj is Sprite s)
                    {
                        editorAsset = s;
                        break;
                    }
                }
            }

            if (editorAsset != null)
            {
                portraitSprite = editorAsset;
                return portraitSprite;
            }
#endif

            // 2. High-fidelity Procedural Portrait with golden border and celestial background
            portraitSprite = BuildDedicatedPortraitSprite();
            return portraitSprite;
        }

        private static Sprite BuildDedicatedPortraitSprite()
        {
            const int size = 160;
            Texture2D tex = CreateBlankTexture(size, size, "ThanhGiong_Portrait_Fallback");
            Color[] pixels = new Color[size * size];

            // Rich celestial background with golden border
            Color darkBlue = new Color(0.12f, 0.16f, 0.28f, 1f);
            DrawRoundedBox(pixels, size, size, 2, 2, size - 4, size - 4, darkBlue, GoldBright);

            // Ground flame aura
            DrawEllipse(pixels, size, size, 80, 24, 50, 10, FlameMid);
            DrawEllipse(pixels, size, size, 80, 24, 34, 6, FlameCore);

            // Silver Iron horse head & neck
            DrawOutlinedEllipse(pixels, size, size, 65, 68, 30, 20, HorseSilverLight, HorseSilverMid, OutlineDark);
            DrawThickLine(pixels, size, size, new Vector2(76, 76), new Vector2(106, 96), 11f, HorseSilverLight);
            DrawThickLine(pixels, size, size, new Vector2(106, 96), new Vector2(128, 86), 8f, HorseSilverLight);
            // Horse eye & fiery breath
            DrawCircle(pixels, size, size, 114, 94, 2f, OutlineDark);
            DrawCircle(pixels, size, size, 114, 94, 1.2f, FlameCore);
            DrawThickLine(pixels, size, size, new Vector2(128, 85), new Vector2(148, 75), 4.5f, FlameMid);

            // Golden harness
            DrawThickLine(pixels, size, size, new Vector2(104, 98), new Vector2(126, 88), 2.5f, GoldBright);

            // Hero rider in golden armor
            DrawOutlinedEllipse(pixels, size, size, 76, 88, 14, 18, GoldBright, GoldMid, OutlineDark);
            DrawRoundedBox(pixels, size, size, 70, 82, 13, 13, GoldBright, OutlineDark);
            DrawCircle(pixels, size, size, 76, 88, 3.2f, ArmorCrimson);

            // Pauldrons
            DrawOutlinedEllipse(pixels, size, size, 63, 98, 10, 6, GoldBright, ArmorCrimson, OutlineDark);
            DrawOutlinedEllipse(pixels, size, size, 89, 98, 10, 6, GoldBright, ArmorCrimson, OutlineDark);

            // Face
            DrawOutlinedEllipse(pixels, size, size, 77, 114, 9, 10, SkinTone, SkinTone, OutlineDark);
            DrawThickLine(pixels, size, size, new Vector2(74, 116), new Vector2(77, 115), 1.5f, OutlineDark);
            DrawThickLine(pixels, size, size, new Vector2(80, 115), new Vector2(83, 116), 1.5f, OutlineDark);

            // Winged golden crown
            DrawTriangle(pixels, size, size, new Vector2(67, 119), new Vector2(87, 119), new Vector2(77, 138), GoldBright);
            DrawCircle(pixels, size, size, 77, 126, 3.2f, ArmorCrimson);
            DrawOutlinedEllipse(pixels, size, size, 65, 120, 5, 8, GoldBright, GoldMid, OutlineDark);
            DrawOutlinedEllipse(pixels, size, size, 89, 120, 5, 8, GoldBright, GoldMid, OutlineDark);

            // Ivory bamboo bundle held proudly
            DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(28, 92), new Vector2(140, 50), 3.2f);
            DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(28, 88), new Vector2(142, 58), 2.8f);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, SpriteMeshType.FullRect);
        }

        public static Sprite GetSkillIcon(int slotIndex)
        {
            int clamped = Mathf.Clamp(slotIndex, 0, 3);
            if (!IsValidSprite(skillIcons[clamped]))
            {
                skillIcons[clamped] = BuildSkillIcon(clamped);
            }

            return skillIcons[clamped];
        }

        private static void EnsureCharacterSprites()
        {
            if (IsValidArray(mountedIdleAndGallopSprites) &&
                IsValidArray(mountedSwordAttackSprites) &&
                IsValidArray(mountedBambooAttackSprites))
            {
                return;
            }

            mountedIdleAndGallopSprites = new Sprite[4];
            mountedSwordAttackSprites = new Sprite[4];
            mountedBambooAttackSprites = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                mountedIdleAndGallopSprites[i] = BuildMountedHeroFrame(i, 0);
                mountedSwordAttackSprites[i] = BuildMountedHeroFrame(i, 1);
                mountedBambooAttackSprites[i] = BuildMountedHeroFrame(i, 2);
            }
        }

        private static Sprite BuildMountedHeroFrame(int stride, int weaponMode)
        {
            const int width = 160;
            const int height = 160;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_Hero_" + weaponMode + "_" + stride);
            Color[] pixels = new Color[width * height];

            float phase = stride * Mathf.PI * 0.5f;
            int bodyBob = Mathf.RoundToInt(Mathf.Sin(phase) * 2f);
            int legSwing = Mathf.RoundToInt(Mathf.Cos(phase) * 7f);

            // 1. Ground fire aura under the Iron Horse hooves
            DrawEllipse(pixels, width, height, 78, 18, 42, 7, new Color(1f, 0.45f, 0.08f, 0.42f));
            DrawEllipse(pixels, width, height, 78, 18, 28, 4, new Color(1f, 0.85f, 0.25f, 0.55f));
            for (int f = 0; f < 5; f++)
            {
                int fx = 42 + f * 18 + ((stride + f) % 2) * 3;
                int fy = 20 + ((stride + f) % 3) * 2;
                DrawTriangle(pixels, width, height,
                    new Vector2(fx - 5, fy - 2),
                    new Vector2(fx + 5, fy - 2),
                    new Vector2(fx + 1, fy + 11),
                    FlameMid);
                DrawTriangle(pixels, width, height,
                    new Vector2(fx - 3, fy - 1),
                    new Vector2(fx + 3, fy - 1),
                    new Vector2(fx, fy + 7),
                    FlameCore);
            }

            // 2. Iron Horse tail & back legs
            DrawThickLine(pixels, width, height, new Vector2(44, 62 + bodyBob), new Vector2(22, 54 + bodyBob - legSwing * 0.4f), 6.5f, HorseManeDark);
            DrawThickLine(pixels, width, height, new Vector2(22, 54 + bodyBob - legSwing * 0.4f), new Vector2(12, 44 + bodyBob), 4.5f, HorseManeDark);

            // Back legs (silver shadow)
            DrawHorseLeg(pixels, width, height, new Vector2(54, 54 + bodyBob), new Vector2(44 - legSwing, 34), new Vector2(40 - legSwing, 19), HorseSilverShadow);
            DrawHorseLeg(pixels, width, height, new Vector2(96, 56 + bodyBob), new Vector2(106 + legSwing, 38), new Vector2(102 + legSwing, 21), HorseSilverShadow);

            // Front legs (silver bright, raised galloping pose like the illustration)
            DrawHorseLeg(pixels, width, height, new Vector2(62, 54 + bodyBob), new Vector2(54 + legSwing, 34), new Vector2(48 + legSwing, 19), HorseSilverMid);
            DrawHorseLeg(pixels, width, height, new Vector2(104, 58 + bodyBob), new Vector2(122 - legSwing * 0.5f, 46 + Mathf.Abs(legSwing)), new Vector2(116 - legSwing * 0.5f, 29 + Mathf.Abs(legSwing)), HorseSilverLight);

            // 3. Iron Horse torso, chest, neck, and head (facing right by default; flipX mirrors left)
            DrawOutlinedEllipse(pixels, width, height, 78, 62 + bodyBob, 31, 16, HorseSilverLight, HorseSilverMid, OutlineDark);
            DrawOutlinedEllipse(pixels, width, height, 98, 68 + bodyBob, 16, 15, HorseSilverLight, HorseSilverMid, OutlineDark);

            // Horse muscular neck & head
            DrawThickLine(pixels, width, height, new Vector2(102, 72 + bodyBob), new Vector2(118, 98 + bodyBob), 12f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(102, 72 + bodyBob), new Vector2(118, 98 + bodyBob), 9.5f, HorseSilverLight);

            // Flowing dark mane along neck
            for (int m = 0; m < 4; m++)
            {
                float my = 76 + m * 6 + bodyBob;
                float mx = 96 + m * 3;
                DrawThickLine(pixels, width, height, new Vector2(mx, my), new Vector2(mx - 12, my - 3 + (stride % 2) * 2), 4.2f, HorseManeDark);
            }

            // Horse head & snout
            DrawThickLine(pixels, width, height, new Vector2(118, 100 + bodyBob), new Vector2(134, 91 + bodyBob), 8.5f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(118, 100 + bodyBob), new Vector2(134, 91 + bodyBob), 6.5f, HorseSilverLight);
            // Horse ears
            DrawTriangle(pixels, width, height, new Vector2(113, 104 + bodyBob), new Vector2(119, 104 + bodyBob), new Vector2(116, 115 + bodyBob), HorseSilverLight);
            DrawTriangle(pixels, width, height, new Vector2(118, 103 + bodyBob), new Vector2(124, 103 + bodyBob), new Vector2(122, 114 + bodyBob), ArmorCrimson);
            // Fierce eye
            DrawCircle(pixels, width, height, 123, 98 + bodyBob, 2.2f, OutlineDark);
            DrawCircle(pixels, width, height, 123, 98 + bodyBob, 1.2f, FlameCore);

            // Golden bridle & harness straps on horse head and chest/rump (matching picture!)
            DrawThickLine(pixels, width, height, new Vector2(116, 101 + bodyBob), new Vector2(131, 91 + bodyBob), 2.2f, GoldMid);
            DrawThickLine(pixels, width, height, new Vector2(128, 95 + bodyBob), new Vector2(132, 88 + bodyBob), 2.2f, GoldMid);
            // Chest & rear golden-red studded barding straps
            DrawThickLine(pixels, width, height, new Vector2(92, 60 + bodyBob), new Vector2(112, 63 + bodyBob), 5.5f, ArmorCrimson);
            DrawThickLine(pixels, width, height, new Vector2(92, 60 + bodyBob), new Vector2(112, 63 + bodyBob), 3.5f, GoldBright);
            DrawThickLine(pixels, width, height, new Vector2(49, 63 + bodyBob), new Vector2(68, 60 + bodyBob), 5.5f, ArmorCrimson);
            DrawThickLine(pixels, width, height, new Vector2(49, 63 + bodyBob), new Vector2(68, 60 + bodyBob), 3.5f, GoldBright);

            // Fire breathing from Iron Horse mouth!
            DrawThickLine(pixels, width, height, new Vector2(134, 89 + bodyBob), new Vector2(152, 73 + bodyBob - (stride % 2) * 3), 5.2f, FlameOuter);
            DrawThickLine(pixels, width, height, new Vector2(134, 89 + bodyBob), new Vector2(150, 76 + bodyBob - (stride % 2) * 2), 3.2f, FlameMid);
            DrawThickLine(pixels, width, height, new Vector2(134, 89 + bodyBob), new Vector2(146, 80 + bodyBob), 1.8f, FlameCore);

            // Saddle blanket (crimson with gold border)
            DrawRoundedBox(pixels, width, height, 65, 51 + bodyBob, 24, 20, ArmorCrimson, GoldBright);

            // 4. Thanh Giong Rider (Golden Armor, Red-Gold Chest Emblem, Winged Golden Crown)
            // Rider leg & sandal on stirrup
            DrawThickLine(pixels, width, height, new Vector2(76, 68 + bodyBob), new Vector2(82, 52 + bodyBob), 6f, GoldMid);
            DrawCircle(pixels, width, height, 83, 49 + bodyBob, 4f, SkinTone);

            // Flowing orange-gold waist sash
            DrawThickLine(pixels, width, height, new Vector2(68, 76 + bodyBob), new Vector2(46, 82 + bodyBob + (stride % 2) * 3), 5f, GoldMid);
            DrawThickLine(pixels, width, height, new Vector2(68, 76 + bodyBob), new Vector2(50, 72 + bodyBob), 4f, GoldBright);

            // Torso in golden armor
            DrawOutlinedEllipse(pixels, width, height, 77, 89 + bodyBob, 13, 17, GoldBright, GoldMid, OutlineDark);
            // Square red-gold chest plate with red sun emblem in center (just like the picture!)
            DrawRoundedBox(pixels, width, height, 71, 82 + bodyBob, 13, 13, GoldBright, OutlineDark);
            DrawRoundedBox(pixels, width, height, 73, 84 + bodyBob, 9, 9, new Color(0.95f, 0.45f, 0.12f, 1f), GoldBright);
            DrawCircle(pixels, width, height, 77, 88 + bodyBob, 3.0f, ArmorCrimson);

            // Golden layered shoulder pauldrons (Giáp vai vàng viền đỏ)
            DrawOutlinedEllipse(pixels, width, height, 64, 98 + bodyBob, 11, 6, GoldBright, ArmorCrimson, OutlineDark);
            DrawOutlinedEllipse(pixels, width, height, 89, 98 + bodyBob, 11, 6, GoldBright, ArmorCrimson, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(63, 102 + bodyBob), new Vector2(91, 102 + bodyBob), 3.5f, GoldBright);

            // Head & determined face
            DrawOutlinedEllipse(pixels, width, height, 78, 114 + bodyBob, 9, 10, SkinTone, SkinTone, OutlineDark);
            // Eyebrows & eyes
            DrawThickLine(pixels, width, height, new Vector2(75, 117 + bodyBob), new Vector2(78, 116 + bodyBob), 1.4f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(81, 116 + bodyBob), new Vector2(84, 117 + bodyBob), 1.4f, OutlineDark);

            // Golden Winged Helmet / Crown (Mũ trụ vàng có cánh hai bên như trong ảnh)
            DrawTriangle(pixels, width, height,
                new Vector2(68, 120 + bodyBob),
                new Vector2(88, 120 + bodyBob),
                new Vector2(78, 139 + bodyBob),
                GoldBright);
            DrawCircle(pixels, width, height, 78, 126 + bodyBob, 3.2f, ArmorCrimson);
            // Left & right golden crown wings
            DrawOutlinedEllipse(pixels, width, height, 66, 119 + bodyBob, 5, 8, GoldBright, GoldMid, OutlineDark);
            DrawOutlinedEllipse(pixels, width, height, 90, 119 + bodyBob, 5, 8, GoldBright, GoldMid, OutlineDark);

            // 5. Held Weapon in Hands (Bamboo Bundle / Iron Sword)
            if (weaponMode == 1)
            {
                // Swinging Iron Sword pose
                DrawThickLine(pixels, width, height, new Vector2(84, 95 + bodyBob), new Vector2(98, 92 + bodyBob), 4.8f, SkinTone);
                DrawCircle(pixels, width, height, 96, 92 + bodyBob, 3.5f, ArmorCrimson);
                DrawThickLine(pixels, width, height, new Vector2(96, 90 + bodyBob), new Vector2(146, 124 + bodyBob), 5.5f, OutlineDark);
                DrawThickLine(pixels, width, height, new Vector2(96, 90 + bodyBob), new Vector2(146, 124 + bodyBob), 3.6f, SteelLight);
                DrawThickLine(pixels, width, height, new Vector2(93, 96 + bodyBob), new Vector2(103, 84 + bodyBob), 3.2f, GoldBright);
            }
            else
            {
                // Holding long Ivory Bamboo bundle across body (iconic pose from the reference image!)
                DrawThickLine(pixels, width, height, new Vector2(64, 96 + bodyBob), new Vector2(54, 88 + bodyBob), 4.5f, SkinTone);
                DrawThickLine(pixels, width, height, new Vector2(86, 94 + bodyBob), new Vector2(94, 82 + bodyBob), 4.5f, SkinTone);

                float angleOffset = weaponMode == 2 ? -4f : 0f;
                Vector2 bambooTail = new Vector2(26, 98 + bodyBob - angleOffset);
                Vector2 bambooTip = new Vector2(148, 52 + bodyBob + angleOffset);
                // Draw 3-4 stalks of Ivory Bamboo fanning out at the front tip
                for (int b = -1; b <= 2; b++)
                {
                    Vector2 start = bambooTail + new Vector2(0, b * 1.6f);
                    Vector2 end = bambooTip + new Vector2(b * 2.5f, b * 5.2f);
                    DrawBambooStalkOnCanvas(pixels, width, height, start, end, 2.8f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.34f), 96f, SpriteMeshType.FullRect);
        }

        private static void DrawHorseLeg(Color[] pixels, int width, int height, Vector2 hip, Vector2 knee, Vector2 hoof, Color legColor)
        {
            DrawThickLine(pixels, width, height, hip, knee, 6.2f, OutlineDark);
            DrawThickLine(pixels, width, height, knee, hoof, 5.0f, OutlineDark);
            DrawThickLine(pixels, width, height, hip, knee, 4.2f, legColor);
            DrawThickLine(pixels, width, height, knee, hoof, 3.2f, legColor);
            DrawCircle(pixels, width, height, Mathf.RoundToInt(hoof.x), Mathf.RoundToInt(hoof.y), 3.4f, OutlineDark);
        }

        private static void DrawBambooStalkOnCanvas(Color[] pixels, int width, int height, Vector2 a, Vector2 b, float thickness)
        {
            DrawThickLine(pixels, width, height, a, b, thickness + 1.4f, OutlineDark);
            DrawThickLine(pixels, width, height, a, b, thickness, BambooGreen);
            DrawThickLine(pixels, width, height, a, b, thickness * 0.55f, BambooGold);
            for (int n = 1; n <= 4; n++)
            {
                Vector2 nodePos = Vector2.Lerp(a, b, n / 5f);
                DrawCircle(pixels, width, height, Mathf.RoundToInt(nodePos.x), Mathf.RoundToInt(nodePos.y), thickness * 0.85f, BambooNode);
            }
        }

        private static Sprite BuildIronSwordSprite()
        {
            const int width = 128;
            const int height = 48;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_IronSword");
            Color[] pixels = new Color[width * height];

            // Pommel & Hilt
            DrawCircle(pixels, width, height, 12, 24, 5.5f, GoldBright);
            DrawThickLine(pixels, width, height, new Vector2(14, 24), new Vector2(32, 24), 4.5f, ArmorCrimson);
            // Golden Crossguard
            DrawThickLine(pixels, width, height, new Vector2(33, 10), new Vector2(33, 38), 5.5f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(33, 11), new Vector2(33, 37), 3.8f, GoldBright);
            // Steel Blade
            DrawThickLine(pixels, width, height, new Vector2(36, 24), new Vector2(118, 24), 7.5f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(36, 24), new Vector2(116, 24), 5.5f, SteelMid);
            DrawThickLine(pixels, width, height, new Vector2(36, 25), new Vector2(118, 24), 2.8f, SteelLight);
            DrawTriangle(pixels, width, height, new Vector2(112, 18), new Vector2(112, 30), new Vector2(125, 24), SteelLight);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.15f, 0.5f), 64f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildSingleBambooSprite()
        {
            const int width = 160;
            const int height = 48;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_SingleBamboo");
            Color[] pixels = new Color[width * height];

            Vector2 start = new Vector2(10, 24);
            Vector2 end = new Vector2(150, 24);
            DrawBambooStalkOnCanvas(pixels, width, height, start, end, 5.2f);

            // Bamboo leaves along the upper half
            for (int i = 0; i < 6; i++)
            {
                float lx = 68 + i * 14;
                float dir = i % 2 == 0 ? 1f : -1f;
                DrawThickLine(pixels, width, height, new Vector2(lx, 24), new Vector2(lx + 11, 24 + dir * 13), 2.6f, BambooGreen);
                DrawThickLine(pixels, width, height, new Vector2(lx + 3, 24), new Vector2(lx + 14, 24 + dir * 9), 2.0f, BambooGold);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.12f, 0.5f), 48f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildBambooBundleSprite()
        {
            const int width = 160;
            const int height = 84;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_BambooBundle");
            Color[] pixels = new Color[width * height];

            Vector2 handle = new Vector2(14, 42);
            float[] endY = { 18f, 30f, 42f, 54f, 66f };
            for (int i = 0; i < endY.Length; i++)
            {
                Vector2 start = handle + new Vector2(0, (i - 2) * 2.5f);
                Vector2 end = new Vector2(148 - Mathf.Abs(i - 2) * 4, endY[i]);
                DrawBambooStalkOnCanvas(pixels, width, height, start, end, 3.8f);
                // Leaves near tips
                DrawThickLine(pixels, width, height, end - new Vector2(16, 0), end + new Vector2(6, (i % 2 == 0 ? 9 : -9)), 2.4f, BambooGreen);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.12f, 0.5f), 48f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildSlashArcSprite()
        {
            const int size = 128;
            Texture2D tex = CreateBlankTexture(size, size, "ThanhGiong_SlashArc");
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(36f, 64f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 d = new Vector2(x, y) - center;
                    float r = d.magnitude;
                    if (r < 34f || r > 58f || d.x < -4f)
                    {
                        continue;
                    }

                    float angle = Mathf.Abs(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    if (angle > 68f)
                    {
                        continue;
                    }

                    float radialT = 1f - Mathf.Abs((r - 46f) / 12f);
                    float angularT = 1f - angle / 68f;
                    float alpha = Mathf.Clamp01(radialT * angularT * 1.6f);
                    Color c = Color.Lerp(FlameMid, Color.white, radialT * 0.75f);
                    c.a = alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.28f, 0.5f), 48f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildFlameSprite()
        {
            const int size = 48;
            Texture2D tex = CreateBlankTexture(size, size, "ThanhGiong_Flame");
            Color[] pixels = new Color[size * size];
            DrawCircle(pixels, size, size, 24, 20, 14f, new Color(FlameOuter.r, FlameOuter.g, FlameOuter.b, 0.75f));
            DrawTriangle(pixels, size, size, new Vector2(10, 18), new Vector2(38, 18), new Vector2(24, 44), FlameMid);
            DrawCircle(pixels, size, size, 24, 18, 9f, FlameCore);
            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.4f), 64f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildBambooLeafSprite()
        {
            const int width = 32;
            const int height = 16;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_BambooLeaf");
            Color[] pixels = new Color[width * height];
            DrawThickLine(pixels, width, height, new Vector2(4, 8), new Vector2(28, 8), 3.6f, BambooGreen);
            DrawThickLine(pixels, width, height, new Vector2(8, 8), new Vector2(24, 8), 1.8f, BambooGold);
            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildShockwaveRingSprite()
        {
            const int size = 128;
            Texture2D tex = CreateBlankTexture(size, size, "ThanhGiong_ShockwaveRing");
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(64f, 64f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), center);
                    if (r < 44f || r > 60f)
                    {
                        continue;
                    }

                    float t = 1f - Mathf.Abs(r - 52f) / 8f;
                    pixels[y * size + x] = new Color(1f, 0.88f, 0.32f, Mathf.Clamp01(t));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildShrineStatueSprite()
        {
            const int width = 128;
            const int height = 144;
            Texture2D tex = CreateBlankTexture(width, height, "ThanhGiong_ShrineStatue");
            Color[] pixels = new Color[width * height];

            // Stone & gold pedestal base
            DrawRoundedBox(pixels, width, height, 20, 10, 88, 22, new Color(0.32f, 0.28f, 0.24f, 1f), GoldBright);
            DrawRoundedBox(pixels, width, height, 30, 30, 68, 16, ArmorCrimson, GoldBright);

            // Bamboo grove backdrop on pedestal
            for (int i = -2; i <= 2; i++)
            {
                Vector2 bStart = new Vector2(64 + i * 8, 46);
                Vector2 bEnd = new Vector2(64 + i * 13, 130 - Mathf.Abs(i) * 6);
                DrawBambooStalkOnCanvas(pixels, width, height, bStart, bEnd, 3.0f);
            }

            // Golden emblem of Iron Horse & Crown in center
            DrawOutlinedEllipse(pixels, width, height, 64, 78, 26, 26, GoldBright, GoldMid, OutlineDark);
            DrawCircle(pixels, width, height, 64, 78, 18f, ArmorCrimson);
            // Horse silhouette in silver
            DrawThickLine(pixels, width, height, new Vector2(54, 68), new Vector2(68, 88), 6.5f, HorseSilverLight);
            DrawThickLine(pixels, width, height, new Vector2(68, 88), new Vector2(78, 82), 5.0f, HorseSilverLight);
            DrawThickLine(pixels, width, height, new Vector2(78, 80), new Vector2(86, 74), 3.0f, FlameCore);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.2f), 96f, SpriteMeshType.FullRect);
        }

        private static Sprite BuildSkillIcon(int slotIndex)
        {
            const int size = 96;
            Texture2D tex = CreateBlankTexture(size, size, "SkillIconV4_ThanhGiong_" + slotIndex);
            Color[] pixels = new Color[size * size];

            // Rich dark-gold bordered background
            Color bg = slotIndex switch
            {
                0 => new Color(0.28f, 0.11f, 0.06f, 1f),
                1 => new Color(0.12f, 0.24f, 0.08f, 1f),
                2 => new Color(0.24f, 0.18f, 0.06f, 1f),
                _ => new Color(0.30f, 0.14f, 0.05f, 1f)
            };
            DrawRoundedBox(pixels, size, size, 4, 4, 88, 88, bg, GoldBright);

            if (slotIndex == 0)
            {
                // Skill 1 (Q): Iron Sword + Iron Horse Fire
                DrawThickLine(pixels, size, size, new Vector2(18, 22), new Vector2(52, 66), 8f, HorseSilverLight);
                DrawThickLine(pixels, size, size, new Vector2(52, 66), new Vector2(68, 56), 6f, HorseSilverLight);
                DrawThickLine(pixels, size, size, new Vector2(68, 54), new Vector2(84, 42), 4.5f, FlameMid);
                DrawThickLine(pixels, size, size, new Vector2(20, 74), new Vector2(78, 20), 6.5f, OutlineDark);
                DrawThickLine(pixels, size, size, new Vector2(20, 74), new Vector2(78, 20), 4.2f, SteelLight);
                DrawThickLine(pixels, size, size, new Vector2(26, 58), new Vector2(40, 72), 3.5f, GoldBright);
            }
            else if (slotIndex == 1)
            {
                // Skill 2 (E): 1 Ivory Bamboo Smash
                DrawEllipse(pixels, size, size, 64, 24, 20, 8, FlameMid);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(18, 78), new Vector2(74, 22), 5.8f);
                DrawThickLine(pixels, size, size, new Vector2(46, 52), new Vector2(64, 66), 3.0f, BambooGreen);
                DrawThickLine(pixels, size, size, new Vector2(58, 38), new Vector2(78, 48), 3.0f, BambooGold);
            }
            else if (slotIndex == 2)
            {
                // Skill 3 (R): 360° Mounted Bamboo Spin
                for (int r = 0; r < 32; r++)
                {
                    float a0 = r * Mathf.PI * 2f / 32f;
                    float a1 = (r + 0.7f) * Mathf.PI * 2f / 32f;
                    Vector2 p0 = new Vector2(48 + Mathf.Cos(a0) * 32f, 48 + Mathf.Sin(a0) * 32f);
                    Vector2 p1 = new Vector2(48 + Mathf.Cos(a1) * 32f, 48 + Mathf.Sin(a1) * 32f);
                    DrawThickLine(pixels, size, size, p0, p1, 3.2f, r % 2 == 0 ? BambooGold : FlameMid);
                }
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(20, 28), new Vector2(76, 68), 3.6f);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(20, 68), new Vector2(76, 28), 3.6f);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(48, 16), new Vector2(48, 80), 3.6f);
            }
            else
            {
                // Skill 4 (F): Iron Horse + Bamboo Bundle Charge
                DrawThickLine(pixels, size, size, new Vector2(12, 24), new Vector2(82, 24), 5f, FlameMid);
                DrawThickLine(pixels, size, size, new Vector2(16, 36), new Vector2(76, 36), 3.5f, FlameCore);
                DrawOutlinedEllipse(pixels, size, size, 42, 48, 20, 11, HorseSilverLight, HorseSilverMid, OutlineDark);
                DrawThickLine(pixels, size, size, new Vector2(54, 54), new Vector2(70, 72), 7f, HorseSilverLight);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(16, 66), new Vector2(84, 42), 3.4f);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(16, 60), new Vector2(84, 52), 3.4f);
                DrawBambooStalkOnCanvas(pixels, size, size, new Vector2(16, 54), new Vector2(84, 62), 3.4f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f, SpriteMeshType.FullRect);
        }

        private static Texture2D CreateBlankTexture(int width, int height, string name)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            return tex;
        }

        private static Sprite CreateSafeSprite(Texture2D tex, Rect rect, Vector2 pivot, float pixelsPerUnit, SpriteMeshType meshType = SpriteMeshType.FullRect)
        {
            Sprite sp = Sprite.Create(tex, rect, pivot, pixelsPerUnit, 0, meshType);
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }

        private static void DrawCircle(Color[] pixels, int width, int height, int cx, int cy, float radius, Color color)
        {
            DrawEllipse(pixels, width, height, cx, cy, radius, radius, color);
        }

        private static void DrawEllipse(Color[] pixels, int width, int height, int cx, int cy, float rx, float ry, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry + 1));
            float invRx = 1f / Mathf.Max(0.5f, rx);
            float invRy = 1f / Mathf.Max(0.5f, ry);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) * invRx;
                    float dy = (y - cy) * invRy;
                    float d = dx * dx + dy * dy;
                    if (d <= 1f)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawOutlinedEllipse(Color[] pixels, int width, int height, int cx, int cy, float rx, float ry, Color light, Color mid, Color outline)
        {
            DrawEllipse(pixels, width, height, cx, cy, rx + 1.8f, ry + 1.8f, outline);
            DrawEllipse(pixels, width, height, cx, cy, rx, ry, mid);
            DrawEllipse(pixels, width, height, cx - 1, cy + 2, rx * 0.72f, ry * 0.68f, light);
        }

        private static void DrawRoundedBox(Color[] pixels, int width, int height, int x0, int y0, int w, int h, Color fill, Color border)
        {
            int x1 = Mathf.Min(width - 1, x0 + w);
            int y1 = Mathf.Min(height - 1, y0 + h);
            for (int y = Mathf.Max(0, y0); y <= y1; y++)
            {
                for (int x = Mathf.Max(0, x0); x <= x1; x++)
                {
                    bool edge = x <= x0 + 2 || x >= x1 - 2 || y <= y0 + 2 || y >= y1 - 2;
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
                    Vector2 closest = a + ab * t;
                    if ((p - closest).sqrMagnitude <= rSq)
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
                    Vector2 p = new Vector2(x, y);
                    if (PointInTriangle(p, a, b, c))
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void BlendPixel(Color[] pixels, int width, int x, int y, Color src)
        {
            int idx = y * width + x;
            Color dst = pixels[idx];
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f)
            {
                pixels[idx] = Color.clear;
                return;
            }

            Color rgb = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            rgb.a = outA;
            pixels[idx] = rgb;
        }
    }
}
