using UnityEngine;

namespace TopDownGame.Player
{
    public enum SonTinhPose
    {
        Locomotion = 0,
        RockPunch = 1,
        BoulderLift = 2,
        PalmsRaiseSkyward = 3,
        GroundSlam = 4
    }

    /// <summary>
    /// Generates and caches runtime sprites for the playable Son Tinh (Tan Vien Son Thanh) hero:
    /// muscular bare-chested warrior with Lac Viet feathered crown, crimson-magenta cape,
    /// emerald-green kilt, overhead blue-grey boulder, skyward upturned-palm mountain-raising pose,
    /// ground-slam pose, craggy mountain peaks, and 4 skill icons.
    /// </summary>
    public static class SonTinhVisualFactory
    {
        private static Sprite[][] poseFrames;
        private static Sprite giantBoulderSprite;
        private static readonly Sprite[] mountainPeakSprites = new Sprite[3];
        private static Sprite earthFissureSprite;
        private static Sprite rockShardSprite;
        private static Sprite shockwaveRingSprite;
        private static Sprite portraitSprite;
        private static readonly Sprite[] skillIcons = new Sprite[4];

        private static readonly Color OutlineDark = new Color(0.13f, 0.09f, 0.06f, 1f);
        private static readonly Color SkinLight = new Color(1.00f, 0.83f, 0.44f, 1f);
        private static readonly Color SkinMid = new Color(0.95f, 0.69f, 0.31f, 1f);
        private static readonly Color SkinShadow = new Color(0.76f, 0.49f, 0.19f, 1f);

        private static readonly Color CapeLight = new Color(0.93f, 0.43f, 0.67f, 1f);
        private static readonly Color CapeMid = new Color(0.82f, 0.26f, 0.52f, 1f);
        private static readonly Color CapeShadow = new Color(0.56f, 0.14f, 0.34f, 1f);

        private static readonly Color KiltLight = new Color(0.16f, 0.67f, 0.42f, 1f);
        private static readonly Color KiltMid = new Color(0.08f, 0.50f, 0.30f, 1f);
        private static readonly Color KiltShadow = new Color(0.04f, 0.32f, 0.18f, 1f);

        private static readonly Color CrownGold = new Color(0.96f, 0.76f, 0.22f, 1f);
        private static readonly Color FeatherRed = new Color(0.74f, 0.25f, 0.16f, 1f);
        private static readonly Color FeatherBright = new Color(0.92f, 0.42f, 0.22f, 1f);

        private static readonly Color BoulderIceLight = new Color(0.76f, 0.85f, 0.98f, 1f);
        private static readonly Color BoulderBlueMid = new Color(0.45f, 0.58f, 0.84f, 1f);
        private static readonly Color BoulderSlateShadow = new Color(0.27f, 0.36f, 0.58f, 1f);

        private static readonly Color MountainLight = new Color(0.66f, 0.73f, 0.86f, 1f);
        private static readonly Color MountainMid = new Color(0.42f, 0.49f, 0.65f, 1f);
        private static readonly Color MountainDark = new Color(0.25f, 0.29f, 0.40f, 1f);
        private static readonly Color EarthBrown = new Color(0.48f, 0.34f, 0.20f, 1f);
        private static readonly Color MossGreen = new Color(0.24f, 0.58f, 0.28f, 1f);
        private static readonly Color DivineGold = new Color(1.00f, 0.86f, 0.28f, 1f);
        private static readonly Color SeismicAmber = new Color(1.00f, 0.62f, 0.16f, 1f);

        private static bool IsValidSprite(Sprite s)
        {
            return s != null && s.texture != null;
        }

        private static bool IsValidPoseMatrix(Sprite[][] matrix)
        {
            if (matrix == null || matrix.Length != 5)
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

        public static Sprite GetHeroSprite(int strideFrame, SonTinhPose pose = SonTinhPose.Locomotion)
        {
            EnsureCharacterSprites();
            int poseIdx = Mathf.Clamp((int)pose, 0, 4);
            int frameIdx = Mathf.Abs(strideFrame) % 4;
            return poseFrames[poseIdx][frameIdx];
        }

        public static Sprite GetGiantBoulderSprite()
        {
            if (!IsValidSprite(giantBoulderSprite))
            {
                giantBoulderSprite = BuildGiantBoulderSprite();
            }

            return giantBoulderSprite;
        }

        public static Sprite GetMountainPeakSprite(int variant)
        {
            int idx = Mathf.Abs(variant) % mountainPeakSprites.Length;
            if (!IsValidSprite(mountainPeakSprites[idx]))
            {
                mountainPeakSprites[idx] = BuildMountainPeakSprite(idx);
            }

            return mountainPeakSprites[idx];
        }

        public static Sprite GetEarthFissureSprite()
        {
            if (!IsValidSprite(earthFissureSprite))
            {
                earthFissureSprite = BuildEarthFissureSprite();
            }

            return earthFissureSprite;
        }

        public static Sprite GetRockShardSprite()
        {
            if (!IsValidSprite(rockShardSprite))
            {
                rockShardSprite = BuildRockShardSprite();
            }

            return rockShardSprite;
        }

        public static Sprite GetShockwaveRingSprite()
        {
            if (!IsValidSprite(shockwaveRingSprite))
            {
                shockwaveRingSprite = BuildShockwaveRingSprite();
            }

            return shockwaveRingSprite;
        }

        public static Sprite GetHeroPortraitSprite()
        {
            if (IsValidSprite(portraitSprite))
            {
                return portraitSprite;
            }

#if UNITY_EDITOR
            const string PortraitAssetPath = "Assets/Art/Generated/Characters/SonTinh_Illustration.jpg";
            Texture2D editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitAssetPath);
            if (editorTex != null)
            {
                if (editorTex.width > editorTex.height * 1.25f)
                {
                    int side = editorTex.height;
                    int startX = Mathf.Clamp(Mathf.RoundToInt(editorTex.width * 0.42f), 0, editorTex.width - side);
                    portraitSprite = CreateSafeSprite(
                        editorTex,
                        new Rect(startX, 0f, side, side),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    return portraitSprite;
                }

                Sprite directSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PortraitAssetPath);
                if (directSprite != null)
                {
                    portraitSprite = directSprite;
                    return portraitSprite;
                }
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

            poseFrames = new Sprite[5][];
            for (int p = 0; p < 5; p++)
            {
                poseFrames[p] = new Sprite[4];
                for (int f = 0; f < 4; f++)
                {
                    poseFrames[p][f] = BuildHeroFrame(f, (SonTinhPose)p);
                }
            }
        }

        private static Sprite BuildHeroFrame(int stride, SonTinhPose pose)
        {
            const int width = 160;
            const int height = 160;
            Texture2D tex = CreateBlankTexture(width, height, "SonTinh_Hero_" + pose + "_" + stride);
            Color[] pixels = new Color[width * height];

            float phase = stride * Mathf.PI * 0.5f;
            int bodyBob = pose == SonTinhPose.GroundSlam
                ? -10
                : Mathf.RoundToInt(Mathf.Sin(phase) * 2f);
            int legSwing = pose == SonTinhPose.Locomotion
                ? Mathf.RoundToInt(Mathf.Cos(phase) * 6f)
                : 0;

            // 1. Earthen Mountain Aura under feet
            DrawEllipse(pixels, width, height, 80, 18, 34, 7, new Color(0.88f, 0.64f, 0.20f, 0.36f));
            DrawEllipse(pixels, width, height, 80, 18, 22, 4, new Color(1.00f, 0.88f, 0.36f, 0.48f));

            if (pose == SonTinhPose.PalmsRaiseSkyward)
            {
                // Golden divine earth ring under Son Tinh while raising mountains
                DrawEllipse(pixels, width, height, 80, 20, 44, 9, new Color(1f, 0.78f, 0.22f, 0.45f));
                for (int r = 0; r < 4; r++)
                {
                    int rx = 46 + r * 22;
                    int ry = 24 + ((stride + r) % 3) * 4;
                    DrawPolygonRock(pixels, width, height, new Vector2(rx, ry), 4.5f, BoulderBlueMid, BoulderIceLight);
                }
            }

            // 2. Flowing Crimson-Magenta Cape behind body (matching illustration)
            int capeWave = (stride % 2) * 3 + (pose == SonTinhPose.PalmsRaiseSkyward ? 4 : 0);
            DrawTriangle(pixels, width, height,
                new Vector2(72, 92 + bodyBob),
                new Vector2(24, 58 + bodyBob + capeWave),
                new Vector2(56, 38 + bodyBob - capeWave * 0.5f),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(73, 90 + bodyBob),
                new Vector2(28, 59 + bodyBob + capeWave),
                new Vector2(58, 41 + bodyBob - capeWave * 0.5f),
                CapeMid);
            DrawTriangle(pixels, width, height,
                new Vector2(74, 88 + bodyBob),
                new Vector2(36, 62 + bodyBob + capeWave),
                new Vector2(62, 48 + bodyBob),
                CapeLight);
            DrawThickLine(pixels, width, height,
                new Vector2(30, 58 + bodyBob + capeWave),
                new Vector2(58, 40 + bodyBob - capeWave * 0.5f),
                3.2f, CapeShadow);

            // 3. Muscular Bare Legs in Heroic Stance
            Vector2 leftHip = new Vector2(71, 56 + bodyBob);
            Vector2 leftKnee = new Vector2(61 - legSwing * 0.6f, 37 + bodyBob * 0.4f);
            Vector2 leftFoot = new Vector2(54 - legSwing, 19);

            Vector2 rightHip = new Vector2(87, 56 + bodyBob);
            Vector2 rightKnee = new Vector2(97 + legSwing * 0.6f, 37 + bodyBob * 0.4f);
            Vector2 rightFoot = new Vector2(104 + legSwing, 19);

            DrawMuscularLimb(pixels, width, height, leftHip, leftKnee, leftFoot, 6.2f, 5.0f, SkinMid, SkinShadow);
            DrawMuscularLimb(pixels, width, height, rightHip, rightKnee, rightFoot, 6.4f, 5.2f, SkinLight, SkinMid);

            // 4. Emerald-Green Warrior Loincloth / Kilt (matching illustration)
            DrawTriangle(pixels, width, height,
                new Vector2(63, 62 + bodyBob),
                new Vector2(96, 62 + bodyBob),
                new Vector2(106, 35 + bodyBob),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(64, 61 + bodyBob),
                new Vector2(69, 36 + bodyBob),
                new Vector2(103, 37 + bodyBob),
                OutlineDark);
            DrawTriangle(pixels, width, height,
                new Vector2(65, 60 + bodyBob),
                new Vector2(94, 60 + bodyBob),
                new Vector2(103, 37 + bodyBob),
                KiltMid);
            DrawTriangle(pixels, width, height,
                new Vector2(65, 60 + bodyBob),
                new Vector2(70, 38 + bodyBob),
                new Vector2(101, 38 + bodyBob),
                KiltMid);
            DrawThickLine(pixels, width, height,
                new Vector2(68, 56 + bodyBob),
                new Vector2(96, 42 + bodyBob),
                3.6f, KiltLight);
            DrawThickLine(pixels, width, height,
                new Vector2(65, 61 + bodyBob),
                new Vector2(94, 61 + bodyBob),
                3.2f, KiltShadow);

            // 5. Muscular Bare-Chested Golden-Tan Torso
            DrawOutlinedEllipse(pixels, width, height, 79, 77 + bodyBob, 15, 18, SkinLight, SkinMid, OutlineDark);
            // Broad V-taper chest
            DrawOutlinedEllipse(pixels, width, height, 79, 84 + bodyBob, 17, 11, SkinLight, SkinMid, OutlineDark);
            // Pectoral & abdominal definition
            DrawThickLine(pixels, width, height, new Vector2(71, 82 + bodyBob), new Vector2(78, 81 + bodyBob), 1.3f, SkinShadow);
            DrawThickLine(pixels, width, height, new Vector2(80, 81 + bodyBob), new Vector2(87, 82 + bodyBob), 1.3f, SkinShadow);
            DrawThickLine(pixels, width, height, new Vector2(79, 82 + bodyBob), new Vector2(79, 68 + bodyBob), 1.2f, SkinShadow);

            // Crimson-magenta cape collar & chest knot (iconic detail from illustration)
            DrawThickLine(pixels, width, height, new Vector2(66, 92 + bodyBob), new Vector2(79, 86 + bodyBob), 3.4f, CapeMid);
            DrawThickLine(pixels, width, height, new Vector2(92, 92 + bodyBob), new Vector2(79, 86 + bodyBob), 3.4f, CapeMid);
            DrawCircle(pixels, width, height, 79, 86 + bodyBob, 3.6f, CapeShadow);
            DrawCircle(pixels, width, height, 79, 86 + bodyBob, 2.4f, CapeLight);
            DrawThickLine(pixels, width, height, new Vector2(78, 85 + bodyBob), new Vector2(74, 77 + bodyBob), 2.2f, CapeMid);
            DrawThickLine(pixels, width, height, new Vector2(80, 85 + bodyBob), new Vector2(84, 78 + bodyBob), 2.2f, CapeLight);

            // 6. Head & Lac Viet Feathered Crown
            int headY = 104 + bodyBob;
            DrawOutlinedEllipse(pixels, width, height, 80, headY, 9.5f, 10.5f, SkinLight, SkinMid, OutlineDark);
            // Fierce eyebrows & eyes
            DrawThickLine(pixels, width, height, new Vector2(76, headY + 3), new Vector2(79, headY + 2), 1.5f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(82, headY + 2), new Vector2(85, headY + 3), 1.5f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(78, headY - 4), new Vector2(83, headY - 4), 1.3f, OutlineDark);

            // Crown headband + 5 russet feathers fanning up
            DrawThickLine(pixels, width, height, new Vector2(71, headY + 8), new Vector2(89, headY + 8), 3.6f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(72, headY + 8), new Vector2(88, headY + 8), 2.2f, CrownGold);
            for (int f = -2; f <= 2; f++)
            {
                Vector2 basePt = new Vector2(80 + f * 3.2f, headY + 9);
                Vector2 tipPt = new Vector2(80 + f * 5.2f, headY + 23 - Mathf.Abs(f) * 2.2f);
                DrawThickLine(pixels, width, height, basePt, tipPt, 3.0f, OutlineDark);
                DrawThickLine(pixels, width, height, basePt, tipPt, 1.9f, f % 2 == 0 ? FeatherBright : FeatherRed);
            }

            // 7. Pose-Specific Arms & Signature Visuals
            switch (pose)
            {
                case SonTinhPose.BoulderLift:
                {
                    // Exact pose from the uploaded reference image: two muscular arms lifting a huge blue-grey boulder overhead!
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(64, 90 + bodyBob),
                        new Vector2(53, 108 + bodyBob),
                        new Vector2(50, 125 + bodyBob),
                        5.8f, 4.8f, SkinMid, SkinShadow);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(94, 90 + bodyBob),
                        new Vector2(105, 108 + bodyBob),
                        new Vector2(108, 125 + bodyBob),
                        5.8f, 4.8f, SkinLight, SkinMid);

                    // Massive blue-grey jagged boulder held above crown
                    DrawBoulderMass(pixels, width, height, new Vector2(80, 138 + bodyBob), 36f, 16f);
                    break;
                }

                case SonTinhPose.PalmsRaiseSkyward:
                {
                    // "Dùng 2 tay ngửa ra dâng lên cao": arms spread wide and raised skyward with flat upturned palms + golden aura
                    Vector2 leftHand = new Vector2(43, 125 + bodyBob);
                    Vector2 rightHand = new Vector2(117, 125 + bodyBob);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(64, 90 + bodyBob),
                        new Vector2(49, 106 + bodyBob),
                        leftHand,
                        5.8f, 4.8f, SkinLight, SkinMid);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(94, 90 + bodyBob),
                        new Vector2(110, 106 + bodyBob),
                        rightHand,
                        5.8f, 4.8f, SkinLight, SkinMid);

                    // Open flat palms facing skyward ("ngửa bàn tay lên trời")
                    DrawThickLine(pixels, width, height, leftHand + new Vector2(-8, 1), leftHand + new Vector2(7, 2), 3.6f, OutlineDark);
                    DrawThickLine(pixels, width, height, leftHand + new Vector2(-7, 1), leftHand + new Vector2(6, 2), 2.3f, SkinLight);
                    DrawThickLine(pixels, width, height, rightHand + new Vector2(-7, 2), rightHand + new Vector2(8, 1), 3.6f, OutlineDark);
                    DrawThickLine(pixels, width, height, rightHand + new Vector2(-6, 2), rightHand + new Vector2(7, 1), 2.3f, SkinLight);

                    // Rising golden-amber mountain power above upturned palms
                    DrawEllipse(pixels, width, height, (int)leftHand.x, (int)leftHand.y + 8, 12, 4, new Color(1f, 0.86f, 0.25f, 0.78f));
                    DrawEllipse(pixels, width, height, (int)rightHand.x, (int)rightHand.y + 8, 12, 4, new Color(1f, 0.86f, 0.25f, 0.78f));
                    DrawPolygonRock(pixels, width, height, leftHand + new Vector2(0, 14 + (stride % 2) * 3), 5.5f, BoulderBlueMid, BoulderIceLight);
                    DrawPolygonRock(pixels, width, height, rightHand + new Vector2(0, 14 + ((stride + 1) % 2) * 3), 5.5f, BoulderBlueMid, BoulderIceLight);
                    break;
                }

                case SonTinhPose.GroundSlam:
                {
                    // "Đấm lòng đất": crouching low, driving both heavy rock-encased fists straight into the earth!
                    Vector2 leftFist = new Vector2(68, 25);
                    Vector2 rightFist = new Vector2(96, 25);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(65, 84 + bodyBob),
                        new Vector2(62, 54),
                        leftFist,
                        6.0f, 5.2f, SkinMid, SkinShadow);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(93, 84 + bodyBob),
                        new Vector2(100, 54),
                        rightFist,
                        6.0f, 5.2f, SkinLight, SkinMid);

                    DrawPolygonRock(pixels, width, height, leftFist, 7.5f, BoulderBlueMid, BoulderIceLight);
                    DrawPolygonRock(pixels, width, height, rightFist, 8.0f, BoulderBlueMid, BoulderIceLight);

                    // Bright golden-amber seismic cracks radiating from slammed fists
                    DrawThickLine(pixels, width, height, new Vector2(46, 18), new Vector2(124, 18), 3.2f, SeismicAmber);
                    DrawThickLine(pixels, width, height, new Vector2(56, 18), new Vector2(114, 22), 2.0f, DivineGold);
                    break;
                }

                case SonTinhPose.RockPunch:
                {
                    // Back arm braced, front arm thrusting stone fist forward
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(64, 88 + bodyBob),
                        new Vector2(52, 76 + bodyBob),
                        new Vector2(58, 68 + bodyBob),
                        5.4f, 4.6f, SkinMid, SkinShadow);

                    Vector2 punchFist = new Vector2(128, 86 + bodyBob);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(93, 89 + bodyBob),
                        new Vector2(110, 88 + bodyBob),
                        punchFist,
                        6.0f, 5.2f, SkinLight, SkinMid);
                    DrawPolygonRock(pixels, width, height, punchFist, 9.0f, BoulderBlueMid, BoulderIceLight);
                    DrawThickLine(pixels, width, height, punchFist + new Vector2(6, -10), punchFist + new Vector2(16, 0), 2.5f, DivineGold);
                    DrawThickLine(pixels, width, height, punchFist + new Vector2(6, 10), punchFist + new Vector2(16, 0), 2.5f, DivineGold);
                    break;
                }

                default:
                {
                    // Locomotion / Idle power stance
                    int armSwing = Mathf.RoundToInt(Mathf.Sin(phase) * 5f);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(64, 89 + bodyBob),
                        new Vector2(54, 75 + bodyBob - armSwing * 0.5f),
                        new Vector2(59 - armSwing, 63 + bodyBob),
                        5.5f, 4.6f, SkinMid, SkinShadow);
                    DrawMuscularLimb(pixels, width, height,
                        new Vector2(94, 89 + bodyBob),
                        new Vector2(104, 75 + bodyBob + armSwing * 0.5f),
                        new Vector2(101 + armSwing, 64 + bodyBob),
                        5.6f, 4.8f, SkinLight, SkinMid);

                    // Subtle stone bracers / earth power on fists
                    DrawCircle(pixels, width, height, 59 - armSwing, 63 + bodyBob, 4.4f, BoulderBlueMid);
                    DrawCircle(pixels, width, height, 101 + armSwing, 64 + bodyBob, 4.6f, BoulderBlueMid);
                    break;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.26f), 96f);
        }

        private static Sprite BuildGiantBoulderSprite()
        {
            const int width = 128;
            const int height = 112;
            Texture2D tex = CreateBlankTexture(width, height, "SonTinh_GiantBoulder");
            Color[] pixels = new Color[width * height];

            DrawBoulderMass(pixels, width, height, new Vector2(64f, 56f), 50f, 38f);
            // Extra craggy facets & cracks
            DrawThickLine(pixels, width, height, new Vector2(34, 62), new Vector2(62, 76), 2.4f, BoulderIceLight);
            DrawThickLine(pixels, width, height, new Vector2(62, 76), new Vector2(92, 66), 2.2f, BoulderIceLight);
            DrawThickLine(pixels, width, height, new Vector2(46, 38), new Vector2(68, 52), 2.0f, OutlineDark);
            DrawThickLine(pixels, width, height, new Vector2(68, 52), new Vector2(86, 40), 1.8f, OutlineDark);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite BuildMountainPeakSprite(int variant)
        {
            const int width = 128;
            const int height = 144;
            Texture2D tex = CreateBlankTexture(width, height, "SonTinh_MountainPeak_" + variant);
            Color[] pixels = new Color[width * height];

            float peakOffsetX = variant == 0 ? 0f : (variant == 1 ? -10f : 10f);
            Vector2 apex = new Vector2(64f + peakOffsetX, 134f - variant * 5f);
            Vector2 leftBase = new Vector2(10f, 16f);
            Vector2 rightBase = new Vector2(118f, 16f);

            // Secondary side spire for craggy silhouette
            Vector2 sideApex = variant % 2 == 0
                ? new Vector2(96f, 92f)
                : new Vector2(32f, 96f);
            DrawTriangle(pixels, width, height, new Vector2(6f, 14f), new Vector2(122f, 14f), sideApex, OutlineDark);
            DrawTriangle(pixels, width, height, new Vector2(10f, 16f), new Vector2(118f, 16f), sideApex, MountainDark);

            // Main towering peak with dark outline
            DrawTriangle(pixels, width, height, new Vector2(6f, 14f), new Vector2(122f, 14f), apex + new Vector2(0f, 4f), OutlineDark);
            // Left sunlit rocky facet & right shadowed rocky facet
            DrawTriangle(pixels, width, height, leftBase, new Vector2(64f, 16f), apex, MountainLight);
            DrawTriangle(pixels, width, height, new Vector2(64f, 16f), rightBase, apex, MountainDark);
            DrawTriangle(pixels, width, height, new Vector2(34f, 16f), new Vector2(82f, 16f), apex, MountainMid);

            // Ridge spine & mossy earth ledges
            DrawThickLine(pixels, width, height, apex, new Vector2(62f, 20f), 2.8f, BoulderIceLight);
            DrawThickLine(pixels, width, height, new Vector2(34f, 64f), new Vector2(58f, 76f), 3.6f, MossGreen);
            DrawThickLine(pixels, width, height, new Vector2(68f, 52f), new Vector2(92f, 44f), 3.4f, MossGreen);

            // Glowing golden seismic vein at base
            DrawEllipse(pixels, width, height, 64, 16, 54, 9, EarthBrown);
            DrawThickLine(pixels, width, height, new Vector2(20f, 18f), new Vector2(108f, 18f), 2.8f, SeismicAmber);
            DrawThickLine(pixels, width, height, new Vector2(44f, 20f), new Vector2(58f, 48f), 1.8f, DivineGold);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.12f), 64f);
        }

        private static Sprite BuildEarthFissureSprite()
        {
            const int width = 128;
            const int height = 64;
            Texture2D tex = CreateBlankTexture(width, height, "SonTinh_EarthFissure");
            Color[] pixels = new Color[width * height];

            DrawEllipse(pixels, width, height, 64, 32, 54, 18, new Color(0.26f, 0.17f, 0.09f, 0.85f));
            Vector2[] pts =
            {
                new Vector2(12, 30),
                new Vector2(34, 38),
                new Vector2(56, 26),
                new Vector2(78, 39),
                new Vector2(100, 29),
                new Vector2(116, 34)
            };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                DrawThickLine(pixels, width, height, pts[i], pts[i + 1], 4.8f, OutlineDark);
                DrawThickLine(pixels, width, height, pts[i], pts[i + 1], 2.8f, SeismicAmber);
                DrawThickLine(pixels, width, height, pts[i], pts[i + 1], 1.4f, DivineGold);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite BuildRockShardSprite()
        {
            const int size = 36;
            Texture2D tex = CreateBlankTexture(size, size, "SonTinh_RockShard");
            Color[] pixels = new Color[size * size];
            DrawPolygonRock(pixels, size, size, new Vector2(18f, 18f), 11f, BoulderBlueMid, BoulderIceLight);
            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite BuildShockwaveRingSprite()
        {
            const int size = 128;
            Texture2D tex = CreateBlankTexture(size, size, "SonTinh_ShockwaveRing");
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(64f, 64f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), center);
                    if (r < 42f || r > 60f)
                    {
                        continue;
                    }

                    float t = 1f - Mathf.Abs(r - 51f) / 9f;
                    Color c = Color.Lerp(SeismicAmber, DivineGold, t);
                    c.a = Mathf.Clamp01(t);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite BuildDedicatedPortraitSprite()
        {
            const int size = 160;
            Texture2D tex = CreateBlankTexture(size, size, "SonTinh_Portrait_Fallback");
            Color[] pixels = new Color[size * size];

            Color skyGold = new Color(0.96f, 0.86f, 0.44f, 1f);
            DrawRoundedBox(pixels, size, size, 2, 2, size - 4, size - 4, skyGold, CrownGold);

            // Flowing cape & kilt
            DrawTriangle(pixels, size, size, new Vector2(74, 82), new Vector2(138, 48), new Vector2(96, 32), CapeMid);
            DrawTriangle(pixels, size, size, new Vector2(62, 46), new Vector2(96, 46), new Vector2(106, 16), KiltMid);

            // Muscular torso & arms lifting boulder overhead
            DrawOutlinedEllipse(pixels, size, size, 78, 66, 16, 20, SkinLight, SkinMid, OutlineDark);
            DrawThickLine(pixels, size, size, new Vector2(66, 78), new Vector2(78, 72), 3.4f, CapeMid);
            DrawThickLine(pixels, size, size, new Vector2(90, 78), new Vector2(78, 72), 3.4f, CapeMid);

            DrawOutlinedEllipse(pixels, size, size, 78, 92, 10, 11, SkinLight, SkinMid, OutlineDark);
            for (int f = -2; f <= 2; f++)
            {
                DrawThickLine(pixels, size, size, new Vector2(78 + f * 3, 100), new Vector2(78 + f * 5, 114 - Mathf.Abs(f) * 2), 2.4f, FeatherRed);
            }

            DrawMuscularLimb(pixels, size, size, new Vector2(63, 76), new Vector2(52, 98), new Vector2(48, 118), 5.6f, 4.6f, SkinMid, SkinShadow);
            DrawMuscularLimb(pixels, size, size, new Vector2(93, 76), new Vector2(104, 98), new Vector2(108, 118), 5.6f, 4.6f, SkinLight, SkinMid);
            DrawBoulderMass(pixels, size, size, new Vector2(78, 134), 42f, 18f);

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite BuildSkillIcon(int slotIndex)
        {
            const int size = 96;
            Texture2D tex = CreateBlankTexture(size, size, "SkillIconV4_SonTinh_" + slotIndex);
            Color[] pixels = new Color[size * size];

            Color bg = slotIndex switch
            {
                0 => new Color(0.14f, 0.20f, 0.34f, 1f),
                1 => new Color(0.18f, 0.24f, 0.14f, 1f),
                2 => new Color(0.28f, 0.17f, 0.08f, 1f),
                _ => new Color(0.22f, 0.16f, 0.28f, 1f)
            };
            DrawRoundedBox(pixels, size, size, 4, 4, 88, 88, bg, CrownGold);

            if (slotIndex == 0)
            {
                // Q: Hurling Giant Blue-Grey Boulder at enemy
                DrawThickLine(pixels, size, size, new Vector2(18, 78), new Vector2(44, 56), 4.0f, DivineGold);
                DrawThickLine(pixels, size, size, new Vector2(14, 66), new Vector2(36, 48), 3.0f, SeismicAmber);
                DrawBoulderMass(pixels, size, size, new Vector2(58f, 40f), 24f, 19f);
                DrawEllipse(pixels, size, size, 64, 18, 22, 6, SeismicAmber);
            }
            else if (slotIndex == 1)
            {
                // E: Two upturned palms raising a mountain at enemy area
                DrawTriangle(pixels, size, size, new Vector2(22, 30), new Vector2(74, 30), new Vector2(48, 82), OutlineDark);
                DrawTriangle(pixels, size, size, new Vector2(25, 31), new Vector2(48, 31), new Vector2(48, 79), MountainLight);
                DrawTriangle(pixels, size, size, new Vector2(48, 31), new Vector2(71, 31), new Vector2(48, 79), MountainDark);
                // Upturned golden palms below mountain
                DrawThickLine(pixels, size, size, new Vector2(20, 18), new Vector2(38, 22), 4.0f, SkinLight);
                DrawThickLine(pixels, size, size, new Vector2(76, 18), new Vector2(58, 22), 4.0f, SkinLight);
                DrawThickLine(pixels, size, size, new Vector2(30, 26), new Vector2(66, 26), 2.5f, DivineGold);
            }
            else if (slotIndex == 2)
            {
                // R: Slamming earth -> cascading mountain ridges advancing forward
                DrawPolygonRock(pixels, size, size, new Vector2(20, 26), 8f, SkinLight, SeismicAmber);
                for (int m = 0; m < 3; m++)
                {
                    float bx = 36f + m * 20f;
                    float h = 34f + m * 14f;
                    DrawTriangle(pixels, size, size, new Vector2(bx - 13, 18), new Vector2(bx + 13, 18), new Vector2(bx, 18 + h), OutlineDark);
                    DrawTriangle(pixels, size, size, new Vector2(bx - 10, 19), new Vector2(bx + 10, 19), new Vector2(bx, 16 + h), m == 2 ? BoulderIceLight : MountainMid);
                }
                DrawThickLine(pixels, size, size, new Vector2(14, 16), new Vector2(84, 16), 3.0f, SeismicAmber);
            }
            else
            {
                // F: 360° surrounding mountain ring around Son Tinh
                DrawEllipse(pixels, size, size, 48, 46, 32, 26, new Color(1f, 0.75f, 0.2f, 0.35f));
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * Mathf.PI * 2f / 6f;
                    Vector2 pos = new Vector2(48f + Mathf.Cos(ang) * 26f, 44f + Mathf.Sin(ang) * 20f);
                    DrawTriangle(pixels, size, size, pos + new Vector2(-9, -8), pos + new Vector2(9, -8), pos + new Vector2(0, 13), OutlineDark);
                    DrawTriangle(pixels, size, size, pos + new Vector2(-7, -7), pos + new Vector2(7, -7), pos + new Vector2(0, 11), MountainLight);
                }
                // Hero silhouette raising both hands in center
                DrawCircle(pixels, size, size, 48, 46, 7f, SkinLight);
                DrawThickLine(pixels, size, size, new Vector2(42, 46), new Vector2(34, 58), 3.0f, DivineGold);
                DrawThickLine(pixels, size, size, new Vector2(54, 46), new Vector2(62, 58), 3.0f, DivineGold);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return CreateSafeSprite(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
        }

        private static void DrawBoulderMass(Color[] pixels, int width, int height, Vector2 center, float rx, float ry)
        {
            DrawEllipse(pixels, width, height, Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), rx + 2.2f, ry + 2.2f, OutlineDark);
            DrawEllipse(pixels, width, height, Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), rx, ry, BoulderSlateShadow);
            DrawEllipse(pixels, width, height, Mathf.RoundToInt(center.x - rx * 0.1f), Mathf.RoundToInt(center.y + ry * 0.12f), rx * 0.86f, ry * 0.78f, BoulderBlueMid);
            DrawEllipse(pixels, width, height, Mathf.RoundToInt(center.x - rx * 0.22f), Mathf.RoundToInt(center.y + ry * 0.26f), rx * 0.52f, ry * 0.45f, BoulderIceLight);
        }

        private static void DrawPolygonRock(Color[] pixels, int width, int height, Vector2 center, float radius, Color mid, Color light)
        {
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            DrawCircle(pixels, width, height, cx, cy, radius + 1.6f, OutlineDark);
            DrawCircle(pixels, width, height, cx, cy, radius, mid);
            DrawCircle(pixels, width, height, cx - 1, cy + 1, radius * 0.58f, light);
        }

        private static void DrawMuscularLimb(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Vector2 c, float r1, float r2, Color main, Color shadow)
        {
            DrawThickLine(pixels, width, height, a, b, r1 + 1.6f, OutlineDark);
            DrawThickLine(pixels, width, height, b, c, r2 + 1.6f, OutlineDark);
            DrawThickLine(pixels, width, height, a, b, r1, shadow);
            DrawThickLine(pixels, width, height, b, c, r2, shadow);
            DrawThickLine(pixels, width, height, a + new Vector2(-0.5f, 0.8f), b + new Vector2(-0.5f, 0.8f), r1 * 0.75f, main);
            DrawThickLine(pixels, width, height, b + new Vector2(-0.5f, 0.8f), c + new Vector2(-0.5f, 0.8f), r2 * 0.75f, main);
        }

        private static Texture2D CreateBlankTexture(int width, int height, string name)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
        }

        private static Sprite CreateSafeSprite(Texture2D tex, Rect rect, Vector2 pivot, float pixelsPerUnit)
        {
            Sprite sp = Sprite.Create(tex, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
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
                    if (dx * dx + dy * dy <= 1f)
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
