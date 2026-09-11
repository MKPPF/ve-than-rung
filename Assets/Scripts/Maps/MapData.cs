using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TopDownGame.Maps
{
    [CreateAssetMenu(menuName = "Top Down Game/Map Data", fileName = "MapData")]
    [MovedFrom(true, "TopDownGame.Maps", "Assembly-CSharp", "MapData")]
    public sealed class MapData : ScriptableObject
    {
        [SerializeField] private string mapId = "Map01";
        [SerializeField] private string displayName = "Forest Clearing";
        [SerializeField] private string nextScene = "Map02";
        [SerializeField] private bool isBossMap;
        [SerializeField] private bool opensSkillSelection;

        [Header("Persistent Hunting Zone")]
        [SerializeField, Min(0)] private int recommendedMinLevel;
        [SerializeField, Min(0)] private int recommendedMaxLevel;
        [SerializeField, Min(0)] private int targetActiveEnemies;

        public string MapId => mapId;
        public string DisplayName => displayName;
        public string NextScene => nextScene;
        public bool IsBossMap => isBossMap;
        public bool OpensSkillSelection => opensSkillSelection;
        public int RecommendedMinLevel => recommendedMinLevel > 0
            ? recommendedMinLevel
            : DefaultMinimumLevel(mapId);
        public int RecommendedMaxLevel => Mathf.Max(RecommendedMinLevel,
            recommendedMaxLevel > 0 ? recommendedMaxLevel : DefaultMaximumLevel(mapId));
        public int TargetActiveEnemies => targetActiveEnemies > 0
            ? targetActiveEnemies
            : DefaultTargetCount(mapId, isBossMap);
        public string RecommendedLevelLabel => "Lv. " + RecommendedMinLevel +
            "-" + RecommendedMaxLevel;

        public static int RecommendedMinimumForScene(string sceneName)
        {
            return DefaultMinimumLevel(sceneName);
        }

        private static int DefaultMinimumLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 1;
            if (id.StartsWith("Rift", System.StringComparison.OrdinalIgnoreCase)) return 50;
            if (id.StartsWith("Cave", System.StringComparison.OrdinalIgnoreCase)) return 30;
            if (id.StartsWith("Boss", System.StringComparison.OrdinalIgnoreCase)) return 45;
            return id switch
            {
                "Map02" => 8,
                "Map03" => 18,
                _ => 1
            };
        }

        private static int DefaultMaximumLevel(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 10;
            if (id.StartsWith("Rift", System.StringComparison.OrdinalIgnoreCase)) return 75;
            if (id.StartsWith("Cave", System.StringComparison.OrdinalIgnoreCase)) return 50;
            if (id.StartsWith("Boss", System.StringComparison.OrdinalIgnoreCase)) return 65;
            return id switch
            {
                "Map02" => 20,
                "Map03" => 35,
                _ => 10
            };
        }

        private static int DefaultTargetCount(string id, bool boss)
        {
            if (boss || (!string.IsNullOrWhiteSpace(id) &&
                id.StartsWith("Boss", System.StringComparison.OrdinalIgnoreCase))) return 1;
            if (!string.IsNullOrWhiteSpace(id) &&
                id.StartsWith("Rift", System.StringComparison.OrdinalIgnoreCase)) return 22;
            if (!string.IsNullOrWhiteSpace(id) &&
                id.StartsWith("Cave", System.StringComparison.OrdinalIgnoreCase)) return 20;
            return id switch
            {
                "Map02" => 16,
                "Map03" => 18,
                _ => 14
            };
        }
    }
}
