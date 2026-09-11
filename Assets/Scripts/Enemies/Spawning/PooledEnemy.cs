using TopDownGame.Enemies.Progression;
using UnityEngine;

namespace TopDownGame.Enemies.Spawning
{
    [DisallowMultipleComponent]
    public sealed class PooledEnemy : MonoBehaviour
    {
        private EnemySpawnManager owner;
        private EnemyBase enemy;

        public bool IsManaged => owner != null;
        public bool PopulationMember { get; private set; } = true;
        public float ReadyAt { get; private set; }
        public Vector2 HomePosition { get; private set; }
        public EnemyHealth Health { get; private set; }

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            Health = GetComponent<EnemyHealth>();
        }

        public void Configure(EnemySpawnManager manager, Vector2 homePosition,
            bool populationMember = true)
        {
            owner = manager;
            HomePosition = homePosition;
            PopulationMember = populationMember;
            enemy ??= GetComponent<EnemyBase>();
            Health ??= GetComponent<EnemyHealth>();
        }

        public void Schedule(float readyAt)
        {
            ReadyAt = readyAt;
        }

        public void Spawn(Vector2 position, int level, EnemyVariant variant)
        {
            HomePosition = position;
            enemy ??= GetComponent<EnemyBase>();
            enemy?.ResetForSpawn(position, level, variant);
            gameObject.SetActive(true);
        }

        public void ReturnToPool()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            owner.NotifyReturned(this);
            gameObject.SetActive(false);
        }
    }
}
