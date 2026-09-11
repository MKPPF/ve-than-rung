using System;
using System.Collections.Generic;
using TopDownGame.Enemies;
using TopDownGame.Maps;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.EditorTools
{
    /// <summary>Authored geometry traced against the V4 painting, not the retired tiled maze.</summary>
    public static partial class TopDownGameBuilder
    {
        public static Vector2 Map02ArtPoint(float x, float y)
            => new Vector2(x * 36f / 1536f - 18f, 12f - y * 24f / 1024f);

        private static MazeLayout PaintedForestLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(36, 24),
                PlayerSpawn = Map02ArtPoint(260, 140),
                ExitPosition = Map02ArtPoint(1320, 790),
                ChestPosition = Map02ArtPoint(1230, 188),
                ItemPosition = Map02ArtPoint(1030, 350),
                PrimaryEnemyPositions = new Vector3[]
                {
                    Map02ArtPoint(349, 440), Map02ArtPoint(382, 560),
                    Map02ArtPoint(410, 680), Map02ArtPoint(640, 535),
                    Map02ArtPoint(790, 410), Map02ArtPoint(972, 400),
                    Map02ArtPoint(1195, 586), Map02ArtPoint(1100, 790)
                },
                SecondaryEnemyPositions = new Vector3[]
                {
                    Map02ArtPoint(715, 268), Map02ArtPoint(1070, 280),
                    Map02ArtPoint(941, 490), Map02ArtPoint(700, 766)
                }
            };
        }

        private static Vector2[] ArtPolygon(params float[] coordinates)
        {
            var points = new Vector2[coordinates.Length / 2];
            for (int i = 0; i < points.Length; i++)
                points[i] = Map02ArtPoint(coordinates[i * 2], coordinates[i * 2 + 1]);
            return points;
        }

        private static Vector2[][] PaintedForestPaths() => new[]
        {
            // Widen the entrance elbow for an enemy's clearance radius, not
            // only the player's point route through the painted path.
            ArtPolygon(210,110, 315,110, 340,170, 380,240, 375,320,
                285,325, 270,250, 240,200, 210,180),
            ArtPolygon(168,110, 220,54, 300,92, 296,154, 395,175, 471,178, 540,237,
                487,284, 390,273, 347,245, 288,231, 257,192, 185,180),
            ArtPolygon(286,210, 380,220, 420,266, 389,326, 418,361, 451,381,
                448,474, 409,515, 345,557, 303,609, 241,578, 237,514,
                249,458, 270,400, 310,349, 278,315, 270,255),
            ArtPolygon(312,508, 410,496, 451,540, 520,551, 557,602, 548,682,
                499,724, 440,760, 413,816, 310,824, 301,753, 264,724,
                248,672, 261,606),
            ArtPolygon(385,480, 472,484, 527,509, 602,480, 670,471, 727,495,
                738,565, 668,600, 657,645, 583,674, 538,636, 568,562,
                484,562, 444,590),
            ArtPolygon(470,218, 565,249, 599,307, 655,320, 692,377, 707,491,
                626,528, 595,446, 616,382, 575,364, 512,344, 501,289, 445,279),
            ArtPolygon(600,217, 824,214, 853,276, 824,322, 758,340, 682,337,
                620,310, 579,280),
            ArtPolygon(720,324, 843,310, 914,260, 961,244, 1054,254,
                1149,300, 1135,387, 1150,462, 1115,520, 1036,553,
                958,537, 880,518, 795,537, 710,568, 652,523, 675,440, 692,379),
            ArtPolygon(967,228, 1067,218, 1128,186, 1155,146, 1233,128,
                1300,143, 1318,194, 1271,239, 1210,263, 1155,278, 1082,310, 993,316),
            ArtPolygon(1076,486, 1170,466, 1240,528, 1260,591, 1316,637,
                1340,728, 1390,775, 1387,849, 1270,870, 1200,820,
                1240,758, 1237,719, 1180,681, 1140,644, 1120,595, 1043,548),
            ArtPolygon(490,679, 588,671, 680,705, 760,726, 840,746, 898,775,
                1007,775, 1100,731, 1164,690, 1240,713, 1263,777,
                1180,821, 1104,829, 1065,858, 952,867, 866,868,
                772,844, 699,815, 639,805, 564,797, 498,762, 455,756),
            ArtPolygon(329,790, 410,796, 380,907, 329,1018, 239,1018, 293,920)
        };

        private static bool InsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static void CreatePaintedForestEnvironment()
        {
            var environment = new GameObject("Environment");
            Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(
                RestoredMap02Art);
            if (art == null) throw new InvalidOperationException("Map02 V4 painting is missing.");
            var background = CreateSpriteProp("Floor", environment.transform, art,
                Vector3.zero, 24f, -30, false);
            background.GetComponent<SpriteRenderer>().sharedMaterial = CreateForestColorMaterial();
            background.AddComponent<ForestNavigation>();
            var arrival = environment.AddComponent<PaintedForestArrival>();
            Set(arrival, "entrance", (Vector2)PaintedForestLayout().PlayerSpawn);
            var fire = environment.AddComponent<LobbyFireEffects>();
            SetVector2Array(fire, "firePositions", new[] { Map02ArtPoint(210, 640) });
            Set(fire, "intensity", 0.6f);
            var water = environment.AddComponent<ForestWaterAmbience>();
            Set(water, "pondCenter", Map02ArtPoint(1352, 460));
            Set(water, "waterfallBottom", Map02ArtPoint(1420, 407));
            Set(water, "pondRadius", new Vector2(1.8f, 1.1f));
            var wildlife = environment.AddComponent<ForestFishSchool>();
            // Inset water habitats traced in source-art pixels, avoiding rocks,
            // waterfall foam, banks and the wooden bridge. Sizes include the fish body.
            SetVector2Array(wildlife, "habitatCenters", new[] {
                Map02ArtPoint(1315, 440), Map02ArtPoint(1370, 460),
                Map02ArtPoint(177, 862), Map02ArtPoint(493, 878),
                Map02ArtPoint(678, 923), Map02ArtPoint(830, 930),
                Map02ArtPoint(1021, 948), Map02ArtPoint(1168, 1000) });
            SetVector2Array(wildlife, "habitatRadii", new[] {
                new Vector2(0.95f, 0.45f), new Vector2(1.05f, 0.42f),
                new Vector2(0.9f, 0.19f), new Vector2(0.9f, 0.23f),
                new Vector2(0.85f, 0.20f), new Vector2(0.5f, 0.17f),
                new Vector2(0.75f, 0.19f), new Vector2(0.7f, 0.17f) });
            Set(wildlife, "pondHabitatCount", 2);
            Set(wildlife, "fishPerPondHabitat", 4);
            Set(wildlife, "fishPerRiverHabitat", 2);
        }

        private static void CreatePaintedForestCollision()
        {
            var root = new GameObject("CombatDecor_Generated");
            var collisions = new GameObject("PaintedForestCollision");
            collisions.transform.SetParent(root.transform, false);
            MazeLayout layout = PaintedForestLayout();
            MazeMask mask = new MazeMask(layout.ArenaSize);
            Vector2[][] paths = PaintedForestPaths();
            for (int y = 0; y < mask.Height; y++)
            for (int x = 0; x < mask.Width; x++)
            {
                Vector2 p = mask.CellCenter(x, y);
                foreach (Vector2[] polygon in paths)
                    if (InsidePolygon(p, polygon)) { mask.Walkable[x, y] = true; break; }
            }
            ValidateMazeLayout("Map02", layout, mask);
            // Fill all blocked terrain, not just its rim: no respawn or ranged
            // attack can accidentally treat the interior of a grove as free space.
            for (int y = 0; y < mask.Height; y++)
            for (int x = 0; x < mask.Width; x++) mask.Walkable[x, y] = !mask.Walkable[x, y];
            List<MazeArea> blocked = MergeWalkableRectangles(mask);
            for (int i = 0; i < blocked.Count; i++)
                CreateSizedBlock("ForestTerrain_" + i, collisions.transform,
                    blocked[i].Center, blocked[i].Size, Color.clear, true, 0);
            CreateSizedBlock("ForestLimitNorth", collisions.transform, new Vector2(0, 12.3f), new Vector2(37, 0.6f), Color.clear, true, 0);
            CreateSizedBlock("ForestLimitSouth", collisions.transform, new Vector2(0, -12.3f), new Vector2(37, 0.6f), Color.clear, true, 0);
            CreateSizedBlock("ForestLimitWest", collisions.transform, new Vector2(-18.3f, 0), new Vector2(0.6f, 25), Color.clear, true, 0);
            CreateSizedBlock("ForestLimitEast", collisions.transform, new Vector2(18.3f, 0), new Vector2(0.6f, 25), Color.clear, true, 0);
            foreach (var entry in new[] { ("Trại Bóng Lá", 395f, 603f), ("Đền Nguyệt Thạch", 713f, 306f) })
                CreateWorldLabel("ForestAreaLabel", entry.Item1, Map02ArtPoint(entry.Item2, entry.Item3), new Color(1f, 0.91f, 0.66f), 0.025f);
        }

        private static Material CreateForestColorMaterial()
        {
            const string path = "Assets/Settings/Map02ColorV5.mat";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/PaintedForestColor.shader");
            if (shader == null) throw new InvalidOperationException("Map02 color shader is missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetFloat("_Strength", 1f);
            // The restored image already resolves edges; avoid double sharpening.
            material.SetFloat("_Detail", 0f);
            material.SetFloat("_Clarity", 0.65f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
