using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Keeps the forest enemies' hits aligned with their locked attack direction
    /// and prevents aggro or damage through solid scenery. Triggers and actors
    /// are deliberately excluded from the terrain visibility test.
    /// </summary>
    public static class ForestCombatGeometry
    {
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[1];

        public static bool HasClearPath(Vector2 origin, Vector2 target)
        {
            int mask = Physics2D.AllLayers;
            if (GameplayCollisionRules.PlayerLayer >= 0)
                mask &= ~(1 << GameplayCollisionRules.PlayerLayer);
            if (GameplayCollisionRules.EnemyLayer >= 0)
                mask &= ~(1 << GameplayCollisionRules.EnemyLayer);
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(mask);
            return Physics2D.Linecast(origin, target, filter, Hits) == 0;
        }

        public static bool IsInsideStrike(Vector2 origin, Vector2 target,
            Vector2 direction, float range, float halfAngle)
        {
            Vector2 offset = target - origin;
            return offset.sqrMagnitude <= range * range &&
                (offset.sqrMagnitude < 0.001f ||
                 Vector2.Angle(direction, offset) <= halfAngle) &&
                HasClearPath(origin, target);
        }
    }
}
