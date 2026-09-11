using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>
    /// Keeps character navigation independent from combat overlap. The player
    /// and enemies can cross each other while hitboxes and hurtboxes continue
    /// to detect attacks normally.
    /// </summary>
    public static class GameplayCollisionRules
    {
        public const string PlayerLayerName = "PlayerCharacter";
        public const string EnemyLayerName = "EnemyCharacter";

        public static int PlayerLayer => LayerMask.NameToLayer(PlayerLayerName);
        public static int EnemyLayer => LayerMask.NameToLayer(EnemyLayerName);
        public static int PlayerMovementMask => EnemyLayer >= 0
            ? ~(1 << EnemyLayer)
            : ~0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyBeforeSceneLoad()
        {
            Apply();
        }

        public static void Apply()
        {
            int playerLayer = PlayerLayer;
            int enemyLayer = EnemyLayer;
            if (playerLayer >= 0 && enemyLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
            }
        }

        public static void ConfigurePlayer(GameObject player)
        {
            if (player == null) return;
            int layer = PlayerLayer;
            if (layer >= 0) player.layer = layer;
            Apply();
        }

        public static void ConfigureEnemy(GameObject enemy)
        {
            if (enemy == null) return;
            int layer = EnemyLayer;
            if (layer >= 0) enemy.layer = layer;
            Apply();
        }
    }
}
