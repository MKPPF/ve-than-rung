using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>Stores motion feedback separately from progress so New Game cannot reset accessibility choices.</summary>
    public static class FeedbackPreferences
    {
        private const string ShakeKey = "TopDownGame.Feedback.CameraShake";
        private const string HitstopKey = "TopDownGame.Feedback.Hitstop";
        public static bool CameraShakeEnabled { get; private set; } = true;
        public static bool HitstopEnabled { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Reload()
        {
            CameraShakeEnabled = PlayerPrefs.GetInt(GamePreferences.Key(ShakeKey),1) != 0;
            HitstopEnabled = PlayerPrefs.GetInt(GamePreferences.Key(HitstopKey),1) != 0;
            if (!CameraShakeEnabled) CameraFollow2D.Active?.ClearShake();
            if (!HitstopEnabled) GameplayState.CancelHitstop();
        }

        public static void SetCameraShakeEnabled(bool value)
        {
            CameraShakeEnabled = value;
            PlayerPrefs.SetInt(GamePreferences.Key(ShakeKey),value ? 1 : 0);
            PlayerPrefs.Save();
            if (!value) CameraFollow2D.Active?.ClearShake();
        }

        public static void SetHitstopEnabled(bool value)
        {
            HitstopEnabled = value;
            PlayerPrefs.SetInt(GamePreferences.Key(HitstopKey),value ? 1 : 0);
            PlayerPrefs.Save();
            if (!value) GameplayState.CancelHitstop();
        }
    }
}