using TopDownGame.Combat;
using TopDownGame.Enemies.Bosses;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>Routes confirmed impacts into bounded time and camera feedback, with an unscaled driver that survives scene transitions.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatFeedback : MonoBehaviour
    {
        private static CombatFeedback instance;
        public static bool HasActiveDriver => instance != null && instance.isActiveAndEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => EnsureExists();

        public static CombatFeedback EnsureExists()
        {
            if (instance != null) return instance;
            return new GameObject(nameof(CombatFeedback)).AddComponent<CombatFeedback>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update() => GameplayState.TickHitstop();
        private void OnDisable()
        {
            if (instance == this) GameplayState.CancelHitstop();
        }
        private void OnDestroy()
        {
            if (instance != this) return;
            GameplayState.CancelHitstop();
            instance = null;
        }

        private static bool CanPlay => Application.isPlaying && !GameplayState.IsInputBlocked &&
            !GameplayState.IsPaused && (Time.timeScale > 0f || GameplayState.IsHitstopActive);

        public static void EnemyHit(DamageInfo hit, Vector2 position)
        {
            if (!CanPlay || hit.IsPeriodic || hit.Amount <= 0f || hit.Source == null ||
                hit.Source.GetComponentInParent<PlayerCombat>() == null) return;
            CameraFollow2D camera = CameraFollow2D.Active;
            if (camera == null || !camera.IsPointVisible(position)) return;
            EnsureExists();
            GameplayState.RequestHitstop(hit.IsCritical ? 0.055f : 0.035f);
            if (hit.IsCritical || hit.Knockback >= 2.5f)
                camera.RequestShake(hit.IsCritical ? 0.08f : 0.045f,0.14f);
        }

        public static void PlayerHit(DamageInfo hit)
        {
            if (!CanPlay || hit.IsPeriodic || hit.Amount <= 0f) return;
            bool boss = hit.Source != null && hit.Source.GetComponentInParent<ForestGuardianBoss>() != null;
            CameraFollow2D.Active?.RequestShake(boss ? 0.14f : 0.07f,boss ? 0.22f : 0.14f);
        }

        public static void WorldImpact(Vector2 position, float strength, float radius = 12f)
        {
            if (!CanPlay || CameraFollow2D.Active == null || radius <= 0f) return;
            CameraFollow2D camera = CameraFollow2D.Active;
            float distance = Vector2.Distance(camera.UnshakenPosition,position);
            float attenuation = Mathf.Clamp01(1f - distance / radius);
            if (attenuation <= 0f) return;
            camera.RequestShake(strength * attenuation,0.22f);
        }
    }
}
