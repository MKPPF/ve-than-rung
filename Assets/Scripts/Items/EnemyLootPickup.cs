using TopDownGame.Core;
using TopDownGame.Enemies.Progression;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Items
{
    /// <summary>
    /// Represents the complete reward from one defeated enemy as a visible
    /// world pickup. Rewards are granted only after the player walks over the
    /// drop, making hunting and collection part of the leveling loop.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyLootPickup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float attractionRadius = 2.2f;
        [SerializeField, Min(0f)] private float attractionSpeed = 5.5f;
        [SerializeField, Min(0.05f)] private float collectDistance = 0.45f;
        [SerializeField, Min(0f)] private float bobHeight = 0.1f;
        [SerializeField, Min(0f)] private float bobSpeed = 3.5f;

        private int experience;
        private int gold;
        private EquipmentData equipment;
        private Transform player;
        private Vector3 groundPosition;
        private bool collected;

        public int Experience => experience;
        public int Gold => gold;
        public EquipmentData Equipment => equipment;
        public bool IsCollected => collected;

        public static EnemyLootPickup Spawn(Vector3 position, int experience,
            int gold, EquipmentData equipment)
        {
            GameObject drop = new GameObject("EnemyLootPickup");
            drop.transform.position = position;
            SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
            CircleCollider2D trigger = drop.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;
            EnemyLootPickup pickup = drop.AddComponent<EnemyLootPickup>();
            pickup.Initialize(experience, gold, equipment);
            renderer.sortingOrder = 18;
            return pickup;
        }

        public void Initialize(int experienceReward, int goldReward,
            EquipmentData equipmentReward)
        {
            experience = Mathf.Max(0, experienceReward);
            gold = Mathf.Max(0, goldReward);
            equipment = equipmentReward;
            collected = false;
            groundPosition = transform.position;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = equipment != null && equipment.Icon != null
                ? equipment.Icon
                : RuntimeSpriteFactory.CircleSprite;
            renderer.color = equipment != null
                ? equipment.RarityColor
                : new Color(1f, 0.78f, 0.18f, 1f);
            float authoredSize = renderer.sprite != null
                ? Mathf.Max(renderer.sprite.bounds.size.x,
                    renderer.sprite.bounds.size.y)
                : 1f;
            float targetWorldSize = equipment != null ? 0.9f : 0.42f;
            transform.localScale = Vector3.one *
                (targetWorldSize / Mathf.Max(0.01f, authoredSize));
        }

        private void Update()
        {
            if (collected) return;
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 displayPosition = groundPosition + Vector3.up * bob;
            if (player != null)
            {
                float distance = Vector2.Distance(groundPosition, player.position);
                if (distance <= collectDistance)
                {
                    Collect(player.gameObject);
                    return;
                }

                if (distance <= attractionRadius)
                {
                    groundPosition = Vector3.MoveTowards(groundPosition,
                        player.position, attractionSpeed * Time.deltaTime);
                    displayPosition = groundPosition + Vector3.up * bob;
                }
            }

            transform.position = displayPosition;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        public void Collect(GameObject collector)
        {
            if (collected || collector == null || !collector.CompareTag("Player"))
            {
                return;
            }

            PlayerStats stats = collector.GetComponent<PlayerStats>();
            if (stats == null) return;
            collected = true;
            stats.AddGold(gold);
            stats.AddExperience(experience);

            EquipmentData collectedEquipment = null;
            EquipmentData salvagedEquipment = null;
            int hunterEssence = 0;
            GameProgressManager progress = GameProgressManager.Instance;
            if (equipment != null)
            {
                if (progress != null && progress.HasItem(equipment.ItemId))
                {
                    salvagedEquipment = equipment;
                    hunterEssence = equipment.DuplicateEssenceValue;
                    progress.AddHunterEssence(hunterEssence);
                }
                else
                {
                    collectedEquipment = equipment;
                    progress?.AddUniqueItem(equipment.ItemId);
                }
            }

            HuntRewardEvents.PublishReward(new HuntRewardNotice(experience, gold,
                collectedEquipment, transform.position, hunterEssence,
                salvagedEquipment));
            progress?.CaptureActivePlayer();
            Destroy(gameObject);
        }
    }
}
