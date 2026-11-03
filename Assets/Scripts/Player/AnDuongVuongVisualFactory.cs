using System;
using UnityEngine;

namespace TopDownGame.Player
{
    public enum AnDuongVuongPose
    {
        Locomotion = 0,
        ShootSingle = 1,
        ShootTriple = 2,
        CallHeavens = 3
    }

    /// <summary>
    /// Generates and caches runtime sprites for the playable An Duong Vuong hero:
    /// King of Au Lac wearing the tall Dong Son feathered crown, regal silver beard,
    /// ivory/gold royal pleated kilt with concentric bronze drum spirals, accompanied by
    /// the celestial Chim Lac spirit aura, wielding the legendary No Than Lien Chau
    /// (magic multi-arrow crossbow) with bronze arrows, skyward arrow volleys, and 4 skill icons.
    /// </summary>
    public static class AnDuongVuongVisualFactory
    {
        private static Sprite[][] poseFrames;
        private static Sprite bronzeArrowSprite;
        private static Sprite crossbowSprite;
        private static readonly Sprite[] arrowRainSprites = new Sprite[3];
        private static Sprite dongSonAuraRingSprite;
        private static Sprite portraitSprite;
        private static readonly Sprite[] skillIcons = new Sprite[4];

        // Color palette based on historical Dong Son bronze culture and user reference art
        private static readonly Color OutlineDark = new Color(0.12f, 0.08f, 0.05f, 1f);
        private static readonly Color SkinLight = new Color(0.98f, 0.82f, 0.58f, 1f);
        private static readonly Color SkinMid = new Color(0.88f, 0.68f, 0.44f, 1f);
        private static readonly Color SkinShadow = new Color(0.70f, 0.48f, 0.28f, 1f);

        // Regal silver/white beard
        private static readonly Color BeardWhite = new Color(0.96f, 0.96f, 0.98f, 1f);
        private static readonly Color BeardMid = new Color(0.82f, 0.84f, 0.88f, 1f);
        private static readonly Color BeardShadow = new Color(0.60f, 0.62f, 0.68f, 1f);

        // Tall Dong Son feathered crown
        private static readonly Color CrownGold = new Color(1.00f, 0.82f, 0.24f, 1f);
        private static readonly Color CrownBronze = new Color(0.78f, 0.52f, 0.16f, 1f);
        private static readonly Color FeatherCyan = new Color(0.28f, 0.88f, 0.92f, 1f);
        private static readonly Color FeatherGold = new Color(1.00f, 0.92f, 0.40f, 1f);

        // Royal pleated kilt & Dong Son spiral sash
        private static readonly Color KiltIvory = new Color(0.95f, 0.92f, 0.82f, 1f);
        private static readonly Color KiltPleatShadow = new Color(0.74f, 0.70f, 0.60f, 1f);
        private static readonly Color SashGold = new Color(0.94f, 0.74f, 0.22f, 1f);
        private static readonly Color SashBronzeDark = new Color(0.48f, 0.32f, 0.12f, 1f);
        private static readonly Color DrumRingSpiral = new Color(0.22f, 0.16f, 0.10f, 1f);

        // Weapon: No Than Lien Chau & Co Loa bronze arrows
        private static readonly Color WoodStockRed = new Color(0.62f, 0.22f, 0.14f, 1f);
        private static readonly Color WoodStockDark = new Color(0.38f, 0.12f, 0.08f, 1f);
        private static readonly Color BowWingLight = new Color(0.88f, 0.78f, 0.52f, 1f);
        private static readonly Color BowWingDark = new Color(0.52f, 0.42f, 0.24f, 1f);
        private static readonly Color BronzeHeadGold = new Color(1.00f, 0.86f, 0.30f, 1f);
        private static readonly Color BronzeHeadGreen = new Color(0.35f, 0.68f, 0.55f, 1f);
        private static readonly Color BowString = new Color(1.00f, 0.98f, 0.85f, 0.92f);

        // Celestial Chim Lac aura
        private static readonly Color ChimLacGlow = new Color(0.30f, 0.92f, 1.00f, 0.90f);
        private static readonly Color ChimLacCore = new Color(0.85f, 0.98f, 1.00f, 1f);

        private static bool IsValidSprite(Sprite s)
        {
            return s != null && s.texture != null;
        }

        private static bool IsValidPoseMatrix(Sprite[][] matrix)
        {
            if (matrix == null || matrix.Length != 4)
            {
                return false;
            }

            for (int p = 0; p < matrix.Length; p++)
            {
                if (matrix[p] == null || matrix[p].Length != 4)
                {
                    return false;
                }

                for (int f = 0; f < 4; f++)
                {
                    if (!IsValidSprite(matrix[p][f]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static Sprite GetHeroSprite(int strideFrame, AnDuongVuongPose pose = AnDuongVuongPose.Locomotion)
        {
            EnsureCharacterSprites();
            int poseIdx = Mathf.Clamp((int)pose, 0, 3);
            int frameIdx = Mathf.Abs(strideFrame) % 4;
            return poseFrames[poseIdx][frameIdx];
        }

        public static Sprite GetBronzeArrowSprite()
        {
            if (!IsValidSprite(bronzeArrowSprite))
            {
                bronzeArrowSprite = BuildBronzeArrowSprite();
            }

            return bronzeArrowSprite;
        }

        public static Sprite GetCrossbowSprite()
        {
            if (!IsValidSprite(crossbowSprite))
            {
                crossbowSprite = BuildCrossbowSprite();
            }

            return crossbowSprite;
        }

        public static Sprite GetArrowRainSprite(int variant)
        {
            int idx = Mathf.Abs(variant) % arrowRainSprites.Length;
            if (!IsValidSprite(arrowRainSprites[idx]))
            {
                arrowRainSprites[idx] = BuildArrowRainSprite(idx);
            }

            return arrowRainSprites[idx];
        }

        public static Sprite GetDongSonAuraRingSprite()
        {
            if (!IsValidSprite(dongSonAuraRingSprite))
            {
                dongSonAuraRingSprite = BuildDongSonAuraRingSprite();
            }

            return dongSonAuraRingSprite;
        }

        public static Sprite GetHeroPortraitSprite()
        {
            if (IsValidSprite(portraitSprite))
            {
                return portraitSprite;
            }

#if UNITY_EDITOR
            const string PortraitAssetPath = "Assets/Art/Generated/Characters/AnDuongVuong_Illustration.jpg";
            Texture2D editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitAssetPath);
            if (editorTex != null)
            {
                Sprite directSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PortraitAssetPath);
                if (directSprite != null)
                {
                    portraitSprite = directSprite;
                    return portraitSprite;
                }

                portraitSprite = CreateSafeSprite(
                    editorTex,
                    new Rect(0f, 0f, editorTex.width, editorTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return portraitSprite;
            }
#endif

            portraitSprite = BuildDedicatedPortraitSprite();
            return portraitSprite;
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
            if (IsValidPoseMatrix(poseFrames))
            {
                return;
            }

            poseFrames = new Sprite[4][];
            for (int p = 0; p < 4; p++)
            {
                poseFrames[p] = new Sprite[4];
                for (int f = 0; f < 4; f++)
                {
                    poseFrames[p][f] = BuildHeroFrame(f, (AnDuongVuongPose)p);
                }
            }
        }

        private static Sprite BuildHeroFrame(int stride, AnDuongVuongPose pose)
        {
            const int width = 160;
            const int height = 160;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_Hero_" + pose + "_" + stride);
            Color[] pixels = new Color[width * height];

            float phase = stride * Mathf.PI * 0.5f;
            int bodyBob = pose == AnDuongVuongPose.CallHeavens ? 2 : Mathf.RoundToInt(Mathf.Sin(phase) * 2f);
            int legSwing = pose == AnDuongVuongPose.Locomotion ? Mathf.RoundToInt(Mathf.Cos(phase) * 5f) : 0;

            // 1. Dong Son Golden Sun aura under feet
            DrawEllipse(pixels, width, height, 80, 18, 36, 8, new Color(1.00f, 0.82f, 0.25f, 0.38f));
            DrawEllipse(pixels, width, height, 80, 18, 22, 5, new Color(0.30f, 0.90f, 1.00f, 0.45f));

            // 2. Royal Flowing Cape / Robe drapery behind body
            int capeWave = (stride % 2) * 3 + (pose == AnDuongVuongPose.CallHeavens ? 5 : 0);
            DrawTriangle(pixels, width, height,
                new Vector2(74, 94 + bodyBob),
                new Vector2(26, 52 + bodyBob + capeWave),
                new Vector2(58, 36 + bodyBob - capeWave * 0.5f),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(75, 92 + bodyBob),
                new Vector2(30, 54 + bodyBob + capeWave),
                new Vector2(60, 39 + bodyBob - capeWave * 0.5f),
                new Color(0.25f, 0.18f, 0.12f, 1f));
            DrawTriangle(pixels, width, height,
                new Vector2(76, 90 + bodyBob),
                new Vector2(36, 58 + bodyBob + capeWave),
                new Vector2(64, 44 + bodyBob),
                KiltIvory);

            // 3. Regal Legs with Bronze Shin-guards
            Vector2 leftHip = new Vector2(72, 56 + bodyBob);
            Vector2 leftKnee = new Vector2(63 - legSwing * 0.5f, 38 + bodyBob * 0.4f);
            Vector2 leftFoot = new Vector2(56 - legSwing, 19);

            Vector2 rightHip = new Vector2(88, 56 + bodyBob);
            Vector2 rightKnee = new Vector2(97 + legSwing * 0.5f, 38 + bodyBob * 0.4f);
            Vector2 rightFoot = new Vector2(104 + legSwing, 19);

            DrawMuscularLimb(pixels, width, height, leftHip, leftKnee, leftFoot, 6.0f, 4.8f, SkinMid, SkinShadow);
            DrawMuscularLimb(pixels, width, height, rightHip, rightKnee, rightFoot, 6.2f, 5.0f, SkinLight, SkinMid);

            // Bronze leg wraps / greaves
            DrawThickLine(pixels, width, height, leftKnee, leftFoot, 4.2f, CrownBronze);
            DrawThickLine(pixels, width, height, rightKnee, rightFoot, 4.2f, CrownGold);

            // 4. Ivory Royal Pleated Kilt
            DrawTriangle(pixels, width, height,
                new Vector2(62, 63 + bodyBob),
                new Vector2(98, 63 + bodyBob),
                new Vector2(108, 34 + bodyBob),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(64, 62 + bodyBob),
                new Vector2(70, 35 + bodyBob),
                new Vector2(106, 35 + bodyBob),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(64, 61 + bodyBob),
                new Vector2(96, 61 + bodyBob),
                new Vector2(104, 36 + bodyBob),
                KiltIvory);
            DrawTriangle(pixels, width, height,
                new Vector2(65, 61 + bodyBob),
                new Vector2(71, 37 + bodyBob),
                new Vector2(103, 37 + bodyBob),
                KiltPleatShadow);

            // Vertical pleat lines on kilt
            for (int k = 0; k < 4; k++)
            {
                float px = 70 + k * 8;
                DrawThickLine(pixels, width, height,
                    new Vector2(px, 60 + bodyBob),
                    new Vector2(px + (k - 1.5f) * 2f, 37 + bodyBob),
                    1.6f, new Color(0.64f, 0.60f, 0.50f, 1f));
            }

            // 5. Central Royal Bronze Sash with Concentric Spirals (Image 2 style)
            DrawBox(pixels, width, height, 72, 33 + bodyBob, 16, 29, OutlineDark);
            DrawBox(pixels, width, height, 74, 35 + bodyBob, 12, 26, SashGold);
            // Dong Son circular motifs down sash
            for (int s = 0; s < 3; s++)
            {
                int cy = 40 + s * 8 + bodyBob;
                DrawCircle(pixels, width, height, 80, cy, 3, OutlineDark);
                DrawCircle(pixels, width, height, 80, cy, 2, DrumRingSpiral);
                DrawCircle(pixels, width, height, 80, cy, 1, CrownGold);
            }

            // Broad Golden Waist Belt with Bronze Buckle
            DrawBox(pixels, width, height, 63, 62 + bodyBob, 34, 9, OutlineDark);
            DrawBox(pixels, width, height, 65, 63 + bodyBob, 30, 7, CrownGold);
            DrawBox(pixels, width, height, 76, 62 + bodyBob, 8, 9, CrownBronze);

            // 6. Muscular Torso with Royal Bronze Neck Collar & Cross-Strap
            DrawBox(pixels, width, height, 67, 70 + bodyBob, 26, 22, OutlineDark);
            DrawBox(pixels, width, height, 68, 71 + bodyBob, 24, 20, SkinLight);
            // Pectoral / abdominal shading
            DrawThickLine(pixels, width, height, new Vector2(80, 71 + bodyBob), new Vector2(80, 86 + bodyBob), 1.8f, SkinShadow);
            DrawThickLine(pixels, width, height, new Vector2(72, 79 + bodyBob), new Vector2(88, 79 + bodyBob), 1.5f, SkinMid);
            // Bronze crescent chest collar (Phiến giáp hộ tâm đồng)
            DrawEllipse(pixels, width, height, 80, 88 + bodyBob, 14, 5, CrownBronze);
            DrawEllipse(pixels, width, height, 80, 88 + bodyBob, 11, 3, CrownGold);

            // 7. Head, Majestic Silver Beard, & Tall Dong Son Feather Crown
            int headY = 96 + bodyBob;
            // Majestic silver beard flowing down
            DrawTriangle(pixels, width, height,
                new Vector2(72, headY + 2),
                new Vector2(88, headY + 2),
                new Vector2(80, headY - 14),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(74, headY + 1),
                new Vector2(86, headY + 1),
                new Vector2(80, headY - 12),
                BeardWhite);
            DrawThickLine(pixels, width, height,
                new Vector2(80, headY - 2), new Vector2(80, headY - 11), 1.8f, BeardShadow);

            // Head and face
            DrawCircle(pixels, width, height, 80, headY + 4, 9, OutlineDark);
            DrawCircle(pixels, width, height, 80, headY + 4, 8, SkinLight);
            // Eyes & brows (stern, majestic king)
            DrawBox(pixels, width, height, 76, headY + 5, 3, 2, OutlineDark);
            DrawBox(pixels, width, height, 82, headY + 5, 3, 2, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(75, headY + 8), new Vector2(79, headY + 7), 1.4f, BeardShadow);
            DrawThickLine(pixels, width, height, new Vector2(81, headY + 7), new Vector2(85, headY + 8), 1.4f, BeardShadow);

            // Tall Dong Son feathered crown (Image 2 - Vua An Duong Vuong)
            int crownBaseY = headY + 11;
            DrawBox(pixels, width, height, 70, crownBaseY, 20, 10, OutlineDark);
            DrawBox(pixels, width, height, 72, crownBaseY + 1, 16, 8, CrownGold);
            // Geometric engraving on crown base
            for (int g = 0; g < 4; g++)
            {
                DrawCircle(pixels, width, height, 74 + g * 4, crownBaseY + 5, 1, CrownBronze);
            }
            // Rising feathered plumes / crown spikes (tall stylized feathers)
            DrawTriangle(pixels, width, height,
                new Vector2(71, crownBaseY + 9),
                new Vector2(89, crownBaseY + 9),
                new Vector2(80, crownBaseY + 34),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(73, crownBaseY + 10),
                new Vector2(87, crownBaseY + 10),
                new Vector2(80, crownBaseY + 32),
                CrownGold);
            // Center feather spine & cyan trim
            DrawThickLine(pixels, width, height,
                new Vector2(80, crownBaseY + 10), new Vector2(80, crownBaseY + 32), 2.2f, FeatherCyan);
            DrawThickLine(pixels, width, height,
                new Vector2(75, crownBaseY + 10), new Vector2(77, crownBaseY + 24), 1.8f, FeatherGold);
            DrawThickLine(pixels, width, height,
                new Vector2(85, crownBaseY + 10), new Vector2(83, crownBaseY + 24), 1.8f, FeatherGold);

            // 8. Arms and the Legendary No Than Lien Chau according to pose
            if (pose == AnDuongVuongPose.Locomotion)
            {
                // Left arm resting regally forward with glowing Chim Lac bird
                DrawMuscularLimb(pixels, width, height,
                    new Vector2(68, 88 + bodyBob),
                    new Vector2(56, 75 + bodyBob),
                    new Vector2(50, 68 + bodyBob),
                    4.8f, 3.8f, SkinLight, SkinMid);
                DrawBox(pixels, width, height, 48, 66 + bodyBob, 6, 6, CrownGold);

                // Right arm holding No Than Lien Chau horizontally / angled
                DrawMuscularLimb(pixels, width, height,
                    new Vector2(92, 88 + bodyBob),
                    new Vector2(104, 76 + bodyBob),
                    new Vector2(112, 68 + bodyBob),
                    4.8f, 3.8f, SkinLight, SkinMid);

                // Draw No Than held in hand
                DrawCrossbowAt(pixels, width, height,
                    new Vector2(112, 68 + bodyBob),
                    new Vector2(1f, 0.15f), 0.75f, 1);

                // Celestial Chim Lac bird spirit hovering near left hand
                DrawChimLacSpirit(pixels, width, height, new Vector2(46, 78 + bodyBob + (stride % 2) * 3));
            }
            else if (pose == AnDuongVuongPose.ShootSingle)
            {
                // Athletic archery / crossbow aiming pose forward
                DrawMuscularLimb(pixels, width, height,
                    new Vector2(68, 88 + bodyBob),
                    new Vector2(82, 82 + bodyBob),
                    new Vector2(98, 80 + bodyBob),
                    4.8f, 3.8f, SkinLight, SkinMid);

                DrawMuscularLimb(pixels, width, height,
                    new Vector2(92, 88 + bodyBob),
                    new Vector2(106, 84 + bodyBob),
                    new Vector2(118, 80 + bodyBob),
                    4.8f, 3.8f, SkinLight, SkinMid);

                // No Than aiming straight ahead with 1 arrow glowing on rail
                DrawCrossbowAt(pixels, width, height,
                    new Vector2(112, 80 + bodyBob),
                    new Vector2(1f, 0f), 0.95f, 1);

                DrawChimLacSpirit(pixels, width, height, new Vector2(44, 86 + bodyBob));
            }
            else if (pose == AnDuongVuongPose.ShootTriple)
            {
                // Wide braced shooting stance with 3 glowing arrows loaded
                DrawMuscularLimb(pixels, width, height,
                    new Vector2(68, 88 + bodyBob),
                    new Vector2(86, 84 + bodyBob),
                    new Vector2(104, 82 + bodyBob),
                    5.0f, 4.0f, SkinLight, SkinMid);

                DrawMuscularLimb(pixels, width, height,
                    new Vector2(92, 88 + bodyBob),
                    new Vector2(110, 86 + bodyBob),
                    new Vector2(122, 82 + bodyBob),
                    5.0f, 4.0f, SkinLight, SkinMid);

                // No Than with 3 arrows loaded fanned out
                DrawCrossbowAt(pixels, width, height,
                    new Vector2(114, 82 + bodyBob),
                    new Vector2(1f, 0f), 1.05f, 3);

                DrawChimLacSpirit(pixels, width, height, new Vector2(42, 90 + bodyBob));
            }
            else if (pose == AnDuongVuongPose.CallHeavens)
            {
                // Raising No Than and left hand to the sky ("Ho troi chia mui ten xuong")
                DrawMuscularLimb(pixels, width, height,
                    new Vector2(68, 88 + bodyBob),
                    new Vector2(54, 102 + bodyBob),
                    new Vector2(48, 122 + bodyBob),
                    5.0f, 4.0f, SkinLight, SkinMid);

                DrawMuscularLimb(pixels, width, height,
                    new Vector2(92, 88 + bodyBob),
                    new Vector2(106, 104 + bodyBob),
                    new Vector2(118, 124 + bodyBob),
                    5.0f, 4.0f, SkinLight, SkinMid);

                // Crossbow raised pointing up into heavens
                DrawCrossbowAt(pixels, width, height,
                    new Vector2(118, 124 + bodyBob),
                    new Vector2(0.5f, 1f).normalized, 0.95f, 3);

                // Radiant Chim Lac spirit with expanded wings above head
                DrawChimLacSpiritExpanded(pixels, width, height, new Vector2(48, 134 + bodyBob));

                // Skyward golden light beam
                DrawThickLine(pixels, width, height,
                    new Vector2(80, headY + 30), new Vector2(80, 158), 6.0f, new Color(1.00f, 0.88f, 0.35f, 0.45f));
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.16f), 100f);
        }

        private static void DrawCrossbowAt(Color[] pixels, int width, int height, Vector2 pos, Vector2 aimDir, float scale, int arrowCount)
        {
            Vector2 forward = aimDir.sqrMagnitude > 0.001f ? aimDir.normalized : Vector2.right;
            Vector2 right = new Vector2(forward.y, -forward.x);

            float stockLen = 32f * scale;
            float wingSpan = 28f * scale;

            Vector2 stockBack = pos - forward * (stockLen * 0.4f);
            Vector2 stockFront = pos + forward * (stockLen * 0.6f);

            // 1. Crossbow Main Wooden Stock (Than no go)
            DrawThickLine(pixels, width, height, stockBack, stockFront, 4.5f * scale, WoodStockDark);
            DrawThickLine(pixels, width, height, stockBack, stockFront, 3.0f * scale, WoodStockRed);

            // Bronze trigger & arrow rail (Mang no va lay no)
            DrawThickLine(pixels, width, height, pos, stockFront, 2.0f * scale, CrownBronze);
            DrawCircle(pixels, width, height, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(3f * scale), CrownGold);

            // 2. Crossbow Curved Limbs / Wings (Canh no)
            Vector2 wingLeft = stockFront - forward * (4f * scale) - right * wingSpan;
            Vector2 wingRight = stockFront - forward * (4f * scale) + right * wingSpan;

            DrawThickLine(pixels, width, height, wingLeft, stockFront, 3.8f * scale, BowWingDark);
            DrawThickLine(pixels, width, height, wingLeft, stockFront, 2.4f * scale, BowWingLight);

            DrawThickLine(pixels, width, height, stockFront, wingRight, 3.8f * scale, BowWingDark);
            DrawThickLine(pixels, width, height, stockFront, wingRight, 2.4f * scale, BowWingLight);

            // Golden bands on bow limbs
            for (int b = 1; b <= 3; b++)
            {
                float t = b * 0.25f;
                DrawCircle(pixels, width, height,
                    Mathf.RoundToInt(Mathf.Lerp(stockFront.x, wingLeft.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(stockFront.y, wingLeft.y, t)),
                    Mathf.RoundToInt(2f * scale), CrownGold);
                DrawCircle(pixels, width, height,
                    Mathf.RoundToInt(Mathf.Lerp(stockFront.x, wingRight.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(stockFront.y, wingRight.y, t)),
                    Mathf.RoundToInt(2f * scale), CrownGold);
            }

            // 3. Taut Bowstring (Day no)
            DrawThickLine(pixels, width, height, wingLeft, pos, 1.4f * scale, BowString);
            DrawThickLine(pixels, width, height, pos, wingRight, 1.4f * scale, BowString);

            // 4. Bronze Arrows on the rail
            if (arrowCount == 1)
            {
                Vector2 arrowHead = stockFront + forward * (10f * scale);
                DrawThickLine(pixels, width, height, pos, arrowHead, 2.2f * scale, BronzeHeadGold);
                DrawTriangle(pixels, width, height,
                    arrowHead + forward * (6f * scale),
                    arrowHead - right * (3.5f * scale),
                    arrowHead + right * (3.5f * scale),
                    BronzeHeadGreen);
            }
            else if (arrowCount >= 3)
            {
                float[] angles = { -22f, 0f, 22f };
                for (int i = 0; i < 3; i++)
                {
                    Quaternion rot = Quaternion.Euler(0f, 0f, angles[i]);
                    Vector2 aDir = rot * forward;
                    Vector2 aHead = pos + aDir * (28f * scale);
                    DrawThickLine(pixels, width, height, pos, aHead, 2.0f * scale, BronzeHeadGold);
                    DrawCircle(pixels, width, height, Mathf.RoundToInt(aHead.x), Mathf.RoundToInt(aHead.y), Mathf.RoundToInt(3f * scale), FeatherCyan);
                }
            }
        }

        private static void DrawChimLacSpirit(Color[] pixels, int width, int height, Vector2 pos)
        {
            int cx = Mathf.RoundToInt(pos.x);
            int cy = Mathf.RoundToInt(pos.y);

            // Glowing cyan aura ring
            DrawEllipse(pixels, width, height, cx, cy, 14, 10, ChimLacGlow);
            DrawCircle(pixels, width, height, cx, cy, 6, ChimLacCore);

            // Stylized bird beak & body
            DrawThickLine(pixels, width, height, new Vector2(cx - 8, cy - 2), new Vector2(cx + 8, cy + 2), 2.2f, FeatherCyan);
            DrawTriangle(pixels, width, height,
                new Vector2(cx + 12, cy + 3),
                new Vector2(cx + 6, cy + 5),
                new Vector2(cx + 6, cy + 1),
                FeatherGold);

            // Wings
            DrawTriangle(pixels, width, height,
                new Vector2(cx, cy + 1),
                new Vector2(cx - 6, cy + 9),
                new Vector2(cx + 4, cy + 7),
                ChimLacCore);
        }

        private static void DrawChimLacSpiritExpanded(Color[] pixels, int width, int height, Vector2 pos)
        {
            int cx = Mathf.RoundToInt(pos.x);
            int cy = Mathf.RoundToInt(pos.y);

            DrawEllipse(pixels, width, height, cx, cy, 22, 16, ChimLacGlow);
            DrawCircle(pixels, width, height, cx, cy, 8, ChimLacCore);

            // Wide bird wings in flight (Chim Lac bay tren trong dong)
            DrawTriangle(pixels, width, height,
                new Vector2(cx - 2, cy + 2),
                new Vector2(cx - 16, cy + 14),
                new Vector2(cx - 4, cy + 10),
                FeatherCyan);
            DrawTriangle(pixels, width, height,
                new Vector2(cx + 2, cy + 2),
                new Vector2(cx + 16, cy + 14),
                new Vector2(cx + 4, cy + 10),
                FeatherCyan);

            // Long tail feathers
            DrawThickLine(pixels, width, height, new Vector2(cx, cy - 2), new Vector2(cx - 10, cy - 12), 2.4f, FeatherGold);
            DrawThickLine(pixels, width, height, new Vector2(cx, cy - 2), new Vector2(cx - 4, cy - 14), 2.0f, FeatherCyan);
        }

        private static Sprite BuildBronzeArrowSprite()
        {
            const int width = 48;
            const int height = 18;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_BronzeArrow");
            Color[] pixels = new Color[width * height];

            // Bamboo arrow shaft
            DrawThickLine(pixels, width, height, new Vector2(4, 9), new Vector2(36, 9), 2.6f, WoodStockDark);
            DrawThickLine(pixels, width, height, new Vector2(4, 9), new Vector2(36, 9), 1.6f, BowWingLight);

            // Rear fletching feathers (Lac bird feathers)
            DrawTriangle(pixels, width, height, new Vector2(2, 9), new Vector2(10, 15), new Vector2(10, 9), FeatherCyan);
            DrawTriangle(pixels, width, height, new Vector2(2, 9), new Vector2(10, 3), new Vector2(10, 9), FeatherGold);

            // Co Loa 3-winged bronze arrowhead (Mui ten dong ba canh Co Loa)
            DrawTriangle(pixels, width, height,
                new Vector2(46, 9),
                new Vector2(34, 16),
                new Vector2(34, 2),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(44, 9),
                new Vector2(35, 14),
                new Vector2(35, 4),
                BronzeHeadGreen);
            // Center ridge & glowing gold tip
            DrawThickLine(pixels, width, height, new Vector2(35, 9), new Vector2(44, 9), 2.0f, BronzeHeadGold);
            DrawCircle(pixels, width, height, 43, 9, 2, FeatherCyan);

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.85f, 0.5f), 100f);
        }

        private static Sprite BuildCrossbowSprite()
        {
            const int width = 64;
            const int height = 64;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_Crossbow");
            Color[] pixels = new Color[width * height];

            DrawCrossbowAt(pixels, width, height, new Vector2(24, 32), Vector2.right, 1.25f, 1);

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildArrowRainSprite(int variant)
        {
            const int width = 36;
            const int height = 72;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_ArrowRain_" + variant);
            Color[] pixels = new Color[width * height];

            int offset = (variant % 3) * 4;
            // Vertical falling bronze arrows with streak
            for (int a = 0; a < 3; a++)
            {
                int x = 8 + a * 10 + (variant == 1 ? -2 : 2);
                int topY = 66 - (a * 10 + offset) % 24;
                int bottomY = topY - 32;

                // Motion speed streak
                DrawThickLine(pixels, width, height, new Vector2(x, topY), new Vector2(x, bottomY), 1.6f,
                    new Color(1f, 0.85f, 0.35f, 0.40f));
                DrawThickLine(pixels, width, height, new Vector2(x, bottomY + 12), new Vector2(x, bottomY), 2.2f,
                    BronzeHeadGold);

                // Downward arrowhead
                DrawTriangle(pixels, width, height,
                    new Vector2(x, bottomY - 6),
                    new Vector2(x - 3, bottomY),
                    new Vector2(x + 3, bottomY),
                    BronzeHeadGreen);
                DrawCircle(pixels, width, height, x, bottomY - 6, 1, FeatherCyan);
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildDongSonAuraRingSprite()
        {
            const int width = 128;
            const int height = 128;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_DongSonRing");
            Color[] pixels = new Color[width * height];

            int cx = 64;
            int cy = 64;

            // Concentric golden Dong Son bronze drum circles
            DrawCircleRing(pixels, width, height, cx, cy, 60, 2.2f, new Color(1.00f, 0.82f, 0.22f, 0.85f));
            DrawCircleRing(pixels, width, height, cx, cy, 52, 1.8f, new Color(0.35f, 0.72f, 0.60f, 0.75f));
            DrawCircleRing(pixels, width, height, cx, cy, 40, 2.0f, new Color(1.00f, 0.86f, 0.32f, 0.90f));
            DrawCircleRing(pixels, width, height, cx, cy, 26, 1.6f, new Color(0.30f, 0.88f, 0.96f, 0.80f));

            // Central 14-point Dong Son solar star (Sao 14 canh)
            const int points = 14;
            for (int i = 0; i < points; i++)
            {
                float ang = i * Mathf.PI * 2f / points;
                float nextAng = (i + 1) * Mathf.PI * 2f / points;
                float midAng = (i + 0.5f) * Mathf.PI * 2f / points;

                Vector2 tip = new Vector2(cx + Mathf.Cos(ang) * 22f, cy + Mathf.Sin(ang) * 22f);
                Vector2 inner1 = new Vector2(cx + Mathf.Cos(ang - 0.15f) * 8f, cy + Mathf.Sin(ang - 0.15f) * 8f);
                Vector2 inner2 = new Vector2(cx + Mathf.Cos(ang + 0.15f) * 8f, cy + Mathf.Sin(ang + 0.15f) * 8f);

                DrawTriangle(pixels, width, height, tip, inner1, inner2, CrownGold);
            }
            DrawCircle(pixels, width, height, cx, cy, 8, CrownGold);
            DrawCircle(pixels, width, height, cx, cy, 5, FeatherCyan);

            // Flying Chim Lac birds in the outer ring
            const int birds = 6;
            for (int b = 0; b < birds; b++)
            {
                float bAng = b * Mathf.PI * 2f / birds;
                Vector2 bPos = new Vector2(cx + Mathf.Cos(bAng) * 46f, cy + Mathf.Sin(bAng) * 46f);
                Vector2 bFwd = new Vector2(-Mathf.Sin(bAng), Mathf.Cos(bAng)); // Flying counter-clockwise
                DrawThickLine(pixels, width, height, bPos - bFwd * 4f, bPos + bFwd * 5f, 2.0f, FeatherCyan);
                DrawTriangle(pixels, width, height,
                    bPos + bFwd * 6f,
                    bPos + (new Vector2(bFwd.y, -bFwd.x)) * 3f,
                    bPos - (new Vector2(bFwd.y, -bFwd.x)) * 3f,
                    CrownGold);
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildDedicatedPortraitSprite()
        {
            const int width = 180;
            const int height = 180;
            Texture2D tex = CreateBlankTexture(width, height, "AnDuongVuong_DedicatedPortrait");
            Color[] pixels = new Color[width * height];

            // Background royal temple amber gradient
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color bg = Color.Lerp(new Color(0.16f, 0.10f, 0.06f, 1f), new Color(0.36f, 0.24f, 0.10f, 1f), t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = bg;
                }
            }

            // Dong Son sun halo in background
            DrawCircleRing(pixels, width, height, 90, 95, 68, 3.2f, new Color(0.96f, 0.78f, 0.22f, 0.45f));
            DrawCircleRing(pixels, width, height, 90, 95, 52, 2.4f, new Color(0.30f, 0.85f, 0.90f, 0.35f));

            // Torso & collar
            DrawBox(pixels, width, height, 64, 10, 52, 50, OutlineDark);
            DrawBox(pixels, width, height, 66, 12, 48, 46, SkinLight);
            DrawEllipse(pixels, width, height, 90, 48, 28, 10, CrownBronze);
            DrawEllipse(pixels, width, height, 90, 48, 22, 6, CrownGold);

            // Flowing silver beard
            DrawTriangle(pixels, width, height, new Vector2(70, 78), new Vector2(110, 78), new Vector2(90, 28), OutlineDark);
            DrawTriangle(pixels, width, height, new Vector2(73, 76), new Vector2(107, 76), new Vector2(90, 32), BeardWhite);
            DrawThickLine(pixels, width, height, new Vector2(90, 68), new Vector2(90, 34), 2.8f, BeardShadow);

            // Head and features
            DrawCircle(pixels, width, height, 90, 84, 22, OutlineDark);
            DrawCircle(pixels, width, height, 90, 84, 20, SkinLight);
            // Stern eyes and dark brows
            DrawBox(pixels, width, height, 80, 86, 6, 3, OutlineDark);
            DrawBox(pixels, width, height, 94, 86, 6, 3, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(78, 92), new Vector2(87, 90), 2.4f, BeardShadow);
            DrawThickLine(pixels, width, height, new Vector2(93, 90), new Vector2(102, 92), 2.4f, BeardShadow);

            // Tall Dong Son feathered crown
            int cbY = 104;
            DrawBox(pixels, width, height, 68, cbY, 44, 18, OutlineDark);
            DrawBox(pixels, width, height, 71, cbY + 2, 38, 14, CrownGold);
            for (int g = 0; g < 4; g++)
            {
                DrawCircle(pixels, width, height, 76 + g * 9, cbY + 9, 3, CrownBronze);
                DrawCircle(pixels, width, height, 76 + g * 9, cbY + 9, 1, FeatherCyan);
            }
            // Tall central feathered crest
            DrawTriangle(pixels, width, height, new Vector2(72, cbY + 17), new Vector2(108, cbY + 17), new Vector2(90, 168), OutlineDark);
            DrawTriangle(pixels, width, height, new Vector2(75, cbY + 18), new Vector2(105, cbY + 18), new Vector2(90, 164), CrownGold);
            DrawThickLine(pixels, width, height, new Vector2(90, cbY + 18), new Vector2(90, 164), 3.4f, FeatherCyan);

            // Glowing Chim Lac Spirit near shoulder
            DrawChimLacSpirit(pixels, width, height, new Vector2(40, 118));

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildSkillIcon(int slot)
        {
            const int size = 80;
            Texture2D tex = CreateBlankTexture(size, size, "AnDuongVuong_SkillIcon_" + slot);
            Color[] pixels = new Color[size * size];

            // Deep bronze & obsidian border frame
            Color bgDark = new Color(0.12f, 0.08f, 0.04f, 1f);
            Color bgGlow = slot switch
            {
                0 => new Color(0.85f, 0.45f, 0.15f, 1f), // Golden fire bronze
                1 => new Color(0.95f, 0.75f, 0.20f, 1f), // Radiant triple bronze
                2 => new Color(0.25f, 0.65f, 0.85f, 1f), // Celestial storm blue
                _ => new Color(0.92f, 0.35f, 0.15f, 1f)  // Divine wrath crimson-gold
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(40, 40));
                    pixels[y * size + x] = Color.Lerp(bgGlow * 0.45f, bgDark, Mathf.Clamp01(dist / 44f));
                }
            }

            // Brass rim
            DrawCircleRing(pixels, size, size, 40, 40, 37, 2.2f, CrownGold);
            DrawCircleRing(pixels, size, size, 40, 40, 35, 1.2f, CrownBronze);

            if (slot == 0)
            {
                // Q: Bắn 1 mũi tên vào kẻ địch — Single high-speed glowing bronze arrow piercing forward
                DrawThickLine(pixels, size, size, new Vector2(14, 40), new Vector2(62, 40), 4.2f, WoodStockDark);
                DrawThickLine(pixels, size, size, new Vector2(16, 40), new Vector2(62, 40), 2.8f, BronzeHeadGold);
                // Piercing arrow head
                DrawTriangle(pixels, size, size, new Vector2(68, 40), new Vector2(52, 50), new Vector2(52, 30), OutlineDark);
                DrawTriangle(pixels, size, size, new Vector2(66, 40), new Vector2(54, 48), new Vector2(54, 32), BronzeHeadGreen);
                DrawCircle(pixels, size, size, 64, 40, 3, FeatherCyan);
                // Speed shockwave rings
                DrawCircleRing(pixels, size, size, 34, 40, 10, 1.6f, new Color(1f, 0.85f, 0.3f, 0.7f));
                DrawCircleRing(pixels, size, size, 48, 40, 8, 1.6f, new Color(0.3f, 0.9f, 1f, 0.8f));
            }
            else if (slot == 1)
            {
                // E: Bắn 3 mũi tên chỉa ra 3 hướng của kẻ địch — 3 arrows fanning out in 3 directions
                Vector2 origin = new Vector2(16, 40);
                float[] angles = { -26f, 0f, 26f };
                for (int i = 0; i < 3; i++)
                {
                    Quaternion rot = Quaternion.Euler(0f, 0f, angles[i]);
                    Vector2 fwd = rot * Vector2.right;
                    Vector2 tip = origin + fwd * 46f;
                    Vector2 right = new Vector2(fwd.y, -fwd.x);

                    DrawThickLine(pixels, size, size, origin, tip, 3.0f, BronzeHeadGold);
                    DrawTriangle(pixels, size, size, tip + fwd * 6f, tip - right * 5f, tip + right * 5f, BronzeHeadGreen);
                    DrawCircle(pixels, size, size, Mathf.RoundToInt(tip.x), Mathf.RoundToInt(tip.y), 2, FeatherCyan);
                }
                // Fan arc glow
                DrawCircleRing(pixels, size, size, 16, 40, 30, 1.8f, CrownGold);
            }
            else if (slot == 2)
            {
                // R: Hô trời chỉa mũi tên xuống kẻ địch trước mặt — Frontal skyward arrow storm
                // Dark celestial cloud above
                DrawEllipse(pixels, size, size, 40, 66, 26, 8, new Color(0.24f, 0.28f, 0.38f, 0.95f));
                DrawEllipse(pixels, size, size, 40, 68, 20, 6, new Color(0.40f, 0.50f, 0.65f, 0.85f));

                // 4 arrows raining vertically down
                for (int a = 0; a < 4; a++)
                {
                    int x = 20 + a * 13;
                    int yTop = 62 - (a % 2) * 6;
                    int yBot = 22 + (a % 2) * 4;
                    DrawThickLine(pixels, size, size, new Vector2(x, yTop), new Vector2(x, yBot), 2.2f, BronzeHeadGold);
                    DrawTriangle(pixels, size, size, new Vector2(x, yBot - 5), new Vector2(x - 3, yBot), new Vector2(x + 3, yBot), BronzeHeadGreen);
                    DrawCircle(pixels, size, size, x, yBot - 4, 1, FeatherCyan);
                }
                // Impact ground waves
                DrawEllipse(pixels, size, size, 40, 16, 24, 6, new Color(1.00f, 0.78f, 0.22f, 0.75f));
            }
            else
            {
                // F: Chiêu chỉa mũi tên xuống kẻ địch từ xa nhưng hồi chiêu lâu hơn — Massive long-range orbital arrow strike
                // Central Dong Son star beacon
                DrawCircleRing(pixels, size, size, 40, 40, 28, 2.0f, CrownGold);
                DrawCircleRing(pixels, size, size, 40, 40, 18, 1.8f, FeatherCyan);
                for (int p = 0; p < 8; p++)
                {
                    float a = p * Mathf.PI * 0.25f;
                    Vector2 p1 = new Vector2(40 + Mathf.Cos(a) * 26f, 40 + Mathf.Sin(a) * 26f);
                    DrawCircle(pixels, size, size, Mathf.RoundToInt(p1.x), Mathf.RoundToInt(p1.y), 2, CrownGold);
                }
                // Colossal golden arrow lance plunging through center
                DrawThickLine(pixels, size, size, new Vector2(40, 72), new Vector2(40, 22), 4.5f, CrownGold);
                DrawThickLine(pixels, size, size, new Vector2(40, 72), new Vector2(40, 22), 2.5f, Color.white);
                DrawTriangle(pixels, size, size, new Vector2(40, 12), new Vector2(32, 24), new Vector2(48, 24), BronzeHeadGreen);
                DrawCircle(pixels, size, size, 40, 14, 4, FeatherCyan);
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // =====================================================================
        // PRIMITIVE DRAWING UTILITIES
        // =====================================================================

        private static Texture2D CreateBlankTexture(int width, int height, string name)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }
            tex.SetPixels(pixels);
            return tex;
        }

        private static Sprite CreateSafeSprite(Texture2D tex, Rect rect, Vector2 pivot, float ppu)
        {
            return Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        private static void DrawMuscularLimb(Color[] pixels, int width, int height,
            Vector2 a, Vector2 b, Vector2 c, float th1, float th2, Color inner, Color shade)
        {
            DrawThickLine(pixels, width, height, a, b, th1 + 2.0f, OutlineDark);
            DrawThickLine(pixels, width, height, b, c, th2 + 2.0f, OutlineDark);
            DrawThickLine(pixels, width, height, a, b, th1, shade);
            DrawThickLine(pixels, width, height, b, c, th2, shade);
            DrawThickLine(pixels, width, height, a + new Vector2(0.8f, 0.8f), b + new Vector2(0.8f, 0.8f), th1 * 0.6f, inner);
            DrawThickLine(pixels, width, height, b + new Vector2(0.8f, 0.8f), c + new Vector2(0.8f, 0.8f), th2 * 0.6f, inner);
        }

        private static void DrawBox(Color[] pixels, int width, int height, int x, int y, int w, int h, Color color)
        {
            int minX = Mathf.Max(0, x);
            int maxX = Mathf.Min(width - 1, x + w - 1);
            int minY = Mathf.Max(0, y);
            int maxY = Mathf.Min(height - 1, y + h - 1);

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    BlendPixel(pixels, width, px, py, color);
                }
            }
        }

        private static void DrawCircle(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
        {
            int rSq = radius * radius;
            int minX = Mathf.Max(0, cx - radius);
            int maxX = Mathf.Min(width - 1, cx + radius);
            int minY = Mathf.Max(0, cy - radius);
            int maxY = Mathf.Min(height - 1, cy + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawCircleRing(Color[] pixels, int width, int height, int cx, int cy, float radius, float thickness, Color color)
        {
            float halfTh = thickness * 0.5f;
            float rInnerSq = (radius - halfTh) * (radius - halfTh);
            float rOuterSq = (radius + halfTh) * (radius + halfTh);

            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - halfTh));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius + halfTh));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - halfTh));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius + halfTh));

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dSq = dx * dx + dy * dy;
                    if (dSq >= rInnerSq && dSq <= rOuterSq)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipse(Color[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color color)
        {
            if (rx <= 0 || ry <= 0) return;
            int minX = Mathf.Max(0, cx - rx);
            int maxX = Mathf.Min(width - 1, cx + rx);
            int minY = Mathf.Max(0, cy - ry);
            int maxY = Mathf.Min(height - 1, cy + ry);

            float invRxSq = 1f / (rx * rx);
            float invRySq = 1f / (ry * ry);

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    if (dx * dx * invRxSq + dy * dy * invRySq <= 1f)
                    {
                        BlendPixel(pixels, width, x, y, color);
                    }
                }
            }
        }

        private static void DrawThickLine(Color[] pixels, int width, int height, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
            {
                DrawCircle(pixels, width, height, Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(thickness * 0.5f), color);
                return;
            }

            float radius = thickness * 0.5f;
            float rSq = radius * radius;

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius));

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

        private static void BlendPixel(Color[] pixels, int width, int x, int y, Color src)
        {
            int idx = y * width + x;
            Color dst = pixels[idx];
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f)
            {
                return;
            }

            Color rgb = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            rgb.a = outA;
            pixels[idx] = rgb;
        }
    }
}
