using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TopDownGame.Enemies
{
    public enum EnemyArchetype
    {
        Slime,
        Goblin,
        Archer,
        Mage,
        Tank,
        CrystalScorpion,
        VoidReaver,
        Boss
    }

    [CreateAssetMenu(menuName = "Top Down Game/Enemy Data", fileName = "EnemyData")]
    [MovedFrom(true, "TopDownGame.Enemies", "Assembly-CSharp", "EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private EnemyArchetype archetype;
        [SerializeField, Min(1f)] private float maxHealth = 30f;
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Min(0f)] private float armor;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0.1f)] private float detectionRange = 8f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.2f;
        [SerializeField, Min(0)] private int goldReward = 3;
        [SerializeField, Min(0)] private int experienceReward;
        [SerializeField, Range(0f, 1f)] private float itemDropChance = 0.1f;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public EnemyArchetype Archetype => archetype;
        public float MaxHealth => maxHealth;
        public float Damage => damage;
        public float Armor => armor;
        public float MoveSpeed => moveSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int GoldReward => goldReward;
        public int ExperienceReward => experienceReward > 0
            ? experienceReward
            : Mathf.Max(5, goldReward * 4);
        public float ItemDropChance => itemDropChance;
    }
}
