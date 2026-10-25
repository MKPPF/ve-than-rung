using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>Persists only confirmed display choices; preview rollback is owned by the settings panel.</summary>
    public static class DisplayPreferences
    {
        private const string WidthKey = "TopDownGame.Display.Width";
        private const string HeightKey = "TopDownGame.Display.Height";
        private const string FullscreenKey = "TopDownGame.Display.Fullscreen";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            int width = PlayerPrefs.GetInt(GamePreferences.Key(WidthKey), 0);
            int height = PlayerPrefs.GetInt(GamePreferences.Key(HeightKey), 0);
            if (width <= 0 || height <= 0) return;
            Vector2Int requested = new Vector2Int(width, height);
            // A monitor may have changed since the previous launch.
            if (!AvailableResolutions().Contains(requested)) return;
            Apply(requested, PlayerPrefs.GetInt(GamePreferences.Key(FullscreenKey), 1) != 0
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static List<Vector2Int> AvailableResolutions()
        {
            var result = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                Vector2Int size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 800 && size.y >= 600 && !result.Contains(size)) result.Add(size);
            }
            Vector2Int current = new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            if (!result.Contains(current)) result.Add(current);
            result.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return result;
        }

        public static void Apply(Vector2Int size, FullScreenMode mode)
        {
#if !UNITY_EDITOR
            Screen.SetResolution(size.x, size.y, mode);
#endif
        }

        public static void Save(Vector2Int size, FullScreenMode mode)
        {
            PlayerPrefs.SetInt(GamePreferences.Key(WidthKey), size.x);
            PlayerPrefs.SetInt(GamePreferences.Key(HeightKey), size.y);
            PlayerPrefs.SetInt(GamePreferences.Key(FullscreenKey), mode == FullScreenMode.Windowed ? 0 : 1);
            PlayerPrefs.Save();
        }
    }
}
