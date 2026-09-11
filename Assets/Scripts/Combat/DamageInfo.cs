using UnityEngine;

namespace TopDownGame.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source, Vector2 hitPoint, Vector2 direction,
            float knockback = 0f, bool isCritical = false, bool ignoreArmor = false)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            HitPoint = hitPoint;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
            Knockback = Mathf.Max(0f, knockback);
            IsCritical = isCritical;
            IgnoreArmor = ignoreArmor;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector2 HitPoint { get; }
        public Vector2 Direction { get; }
        public float Knockback { get; }
        public bool IsCritical { get; }
        public bool IgnoreArmor { get; }
    }
}
