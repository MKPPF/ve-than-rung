using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class SwordSpinSkill : SkillBase
    {
        [Header("Visual")]
        [SerializeField] private Sprite effectSprite;
        [SerializeField] private SwordComboProfile comboProfile;
        public SwordComboProfile ComboProfile => comboProfile;
        public float Duration => duration;

        [Header("Damage")]
        [SerializeField, Min(0.5f)] private float attackRadius = 2f;
        [SerializeField, Min(0.1f)] private float duration = 1.2f;
        [SerializeField, Min(0.02f)] private float damageInterval = 0.2f;
        [SerializeField, Min(1)] private int maximumHitsPerEnemy = 6;
        [SerializeField] private LayerMask enemyLayers = ~0;

        private SpinningSwordEffect2D activeEffect;

        public override void Cast()
        {
            if (activeEffect != null)
            {
                Destroy(activeEffect.gameObject);
            }

            GameObject effectObject = new GameObject("SpinningSwordEffect");
            activeEffect = effectObject.AddComponent<SpinningSwordEffect2D>();
            activeEffect.Initialize(transform, gameObject, effectSprite,
                Data.EffectColor, ScaledDamage(), attackRadius, duration, damageInterval,
                maximumHitsPerEnemy, enemyLayers);
            if(comboProfile!=null) activeEffect.SetComboPresentation(comboProfile);
            else activeEffect.SetPresentation(Data.Presentation);
        }
        private void OnDisable(){if(activeEffect!=null) Destroy(activeEffect.gameObject);}
    }
}
