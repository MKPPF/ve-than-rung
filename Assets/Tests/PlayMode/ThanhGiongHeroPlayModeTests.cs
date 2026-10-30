using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class ThanhGiongHeroPlayModeTests
    {
        private GameObject player;
        private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        private readonly List<EnemyData> runtimeData = new List<EnemyData>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayerPrefs.DeleteKey(GamePreferences.Key("TopDownGame.SelectedHero"));
            if (player != null)
            {
                Object.Destroy(player);
            }

            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Object.Destroy(enemy);
                }
            }
            spawnedEnemies.Clear();

            foreach (EnemyData data in runtimeData)
            {
                if (data != null)
                {
                    Object.Destroy(data);
                }
            }
            runtimeData.Clear();

            foreach (ThanhGiongVfx vfx in Object.FindObjectsByType<ThanhGiongVfx>())
            {
                Object.Destroy(vfx.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ThanhGiongExecutesAllFiveIronSwordIronHorseAndBambooMoves()
        {
            player = new GameObject("ThanhGiongTestPlayer");
            player.tag = "Player";
            player.transform.position = new Vector3(400f, 400f, 0f);
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            PlayerStats stats = player.AddComponent<PlayerStats>();
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            PlayerCharacterAnimator animator = player.AddComponent<PlayerCharacterAnimator>();
            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            combat.SetAutoAttackEnabled(false);
            SkillInventory inventory = player.AddComponent<SkillInventory>();
            SkillManager skillManager = player.AddComponent<SkillManager>();
            ThanhGiongHeroController hero = player.AddComponent<ThanhGiongHeroController>();

            yield return null;

            hero.SelectHero(HeroCharacterId.ThanhGiong, false);
            Assert.That(hero.IsThanhGiongActive, Is.True);
            Assert.That(skillManager.GetSkillData(0).DisplayName, Is.EqualTo("Kiếm Sắt Chiến Mã"));
            Assert.That(skillManager.GetSkillData(1).DisplayName, Is.EqualTo("Tre Ngà Quật Địch"));
            Assert.That(skillManager.GetSkillData(2).DisplayName, Is.EqualTo("Thần Mã Quật Tre 360°"));
            Assert.That(skillManager.GetSkillData(3).DisplayName, Is.EqualTo("Thiết Mã Càn Lướt"));

            yield return null;
            Assert.That(sr.sprite, Is.Not.Null);
            Assert.That(sr.sprite.texture.name, Does.Contain("ThanhGiong_Hero"));

            // 1. Move 0 (Normal Attack): Iron Sword
            EnemyHealth frontEnemy = SpawnDummyEnemy(new Vector2(401.4f, 400f), 2000f);
            float hpBefore = frontEnemy.CurrentHealth;
            MethodInfo attackRoutine = typeof(PlayerCombat).GetMethod("AttackRoutine",
                BindingFlags.Instance | BindingFlags.NonPublic);
            yield return player.GetComponent<MonoBehaviour>().StartCoroutine(
                (IEnumerator)attackRoutine.Invoke(combat, new object[] { Vector2.right, false }));
            Assert.That(frontEnemy.CurrentHealth, Is.LessThan(hpBefore),
                "Normal attack with Iron Sword must damage enemies in front.");

            // 2. Move 1 (Q - Slot 0): Iron Sword + Iron Horse
            rb.position = new Vector2(400f, 400f);
            player.transform.position = new Vector3(400f, 400f, 0f);
            stats.RestoreFullMana();
            EnemyHealth horseSwordTarget = SpawnDummyEnemy(new Vector2(402.8f, 400f), 2000f);
            hpBefore = horseSwordTarget.CurrentHealth;
            Assert.That(hero.TryCastThanhGiongSkill(0, new Vector2(403f, 400f), ~0), Is.True);
            yield return new WaitForSeconds(0.45f);
            Assert.That(horseSwordTarget.CurrentHealth, Is.LessThan(hpBefore),
                "Skill 1 (Iron Sword + Iron Horse) must lunge and cleave enemies ahead.");

            // 3. Move 2 (E - Slot 1): Single Bamboo Smash
            rb.position = new Vector2(400f, 400f);
            player.transform.position = new Vector3(400f, 400f, 0f);
            stats.RestoreFullMana();
            EnemyHealth bambooSmashTarget = SpawnDummyEnemy(new Vector2(403.5f, 400f), 2000f);
            hpBefore = bambooSmashTarget.CurrentHealth;
            Assert.That(hero.TryCastThanhGiongSkill(1, new Vector2(404f, 400f), ~0), Is.True);
            yield return new WaitForSeconds(0.45f);
            Assert.That(bambooSmashTarget.CurrentHealth, Is.LessThan(hpBefore),
                "Skill 2 (Single Bamboo Smash) must hit enemies along the forward line.");

            // 4. Move 3 (R - Slot 2): 360-degree Mounted Bamboo Bundle Spin
            rb.position = new Vector2(400f, 400f);
            player.transform.position = new Vector3(400f, 400f, 0f);
            stats.RestoreFullMana();
            EnemyHealth northEnemy = SpawnDummyEnemy(new Vector2(400f, 402.2f), 2000f);
            EnemyHealth westEnemy = SpawnDummyEnemy(new Vector2(397.8f, 400f), 2000f);
            float northBefore = northEnemy.CurrentHealth;
            float westBefore = westEnemy.CurrentHealth;
            Assert.That(hero.TryCastThanhGiongSkill(2, null, ~0), Is.True);
            yield return new WaitForSeconds(1.5f);
            Assert.That(northEnemy.CurrentHealth, Is.LessThan(northBefore),
                "Skill 3 (360° Mounted Bamboo Spin) must hit enemies to the north.");
            Assert.That(westEnemy.CurrentHealth, Is.LessThan(westBefore),
                "Skill 3 (360° Mounted Bamboo Spin) must hit enemies behind/west (360 degrees).");

            // 5. Move 4 (F - Slot 3): Iron Horse + Bamboo Bundle Charge
            rb.position = new Vector2(400f, 400f);
            player.transform.position = new Vector3(400f, 400f, 0f);
            stats.RestoreFullMana();
            EnemyHealth chargeEnemy = SpawnDummyEnemy(new Vector2(403.2f, 400f), 2000f);
            hpBefore = chargeEnemy.CurrentHealth;
            float startX = rb.position.x;
            Assert.That(hero.TryCastThanhGiongSkill(3, new Vector2(406f, 400f), ~0), Is.True);
            yield return new WaitForSeconds(0.55f);
            Assert.That(rb.position.x, Is.GreaterThan(startX + 3.5f),
                "Skill 4 (Iron Horse + Bamboo Bundle Charge) must charge a long distance forward.");
            Assert.That(chargeEnemy.CurrentHealth, Is.LessThan(hpBefore),
                "Skill 4 must sweep and damage enemies along the charge path.");
        }

        private EnemyHealth SpawnDummyEnemy(Vector2 position, float maxHp)
        {
            GameObject enemy = new GameObject("DummyEnemy");
            enemy.transform.position = position;
            CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            runtimeData.Add(data);
            typeof(EnemyData).GetField("maxHealth", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(data, maxHp);

            EnemyRuntimeProfile profile = enemy.AddComponent<EnemyRuntimeProfile>();
            profile.Configure(data, 1, EnemyVariant.Normal);
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.ResetForSpawn(data, profile);

            spawnedEnemies.Add(enemy);
            return health;
        }
    }
}
