using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class CastProbeSkill : SkillBase, ITargetedSkill
    {
        public int Casts;
        public Vector2 Point;
        public void SetNextTarget(Vector2 point) {Point=point;}
        public override void Cast() {Casts++;}
    }

    public sealed class SpellPresentationTests
    {
        private GameObject owner;
        private SkillData data;
        private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(owner!=null) GameplayState.ExitModal(owner);
            Time.timeScale=1;
            Object.Destroy(owner);Object.Destroy(data);
            foreach(var vfx in Object.FindObjectsByType<SpellPresentation>())
                Object.Destroy(vfx.gameObject);
            foreach(var vfx in Object.FindObjectsByType<SpellVfx>()) Object.Destroy(vfx.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WindupLocksTargetBlocksOverlapAndFreezesWithModal()
        {
            owner=new GameObject("CastProbe");
            var stats=owner.AddComponent<PlayerStats>();
            owner.AddComponent<PlayerHealth>();
            var combat=owner.AddComponent<PlayerCombat>();combat.SetAutoAttackEnabled(false);
            var inventory=owner.AddComponent<SkillInventory>();
            data=ScriptableObject.CreateInstance<SkillData>();
            var probe=owner.AddComponent<CastProbeSkill>();probe.Initialize(data,1);
            inventory.AddOrUpgrade("Fireball");inventory.TryEquip("Fireball",0);
            var manager=owner.AddComponent<SkillManager>();
            typeof(SkillManager).GetField("skillDatabase",Private).SetValue(manager,new[]{data});
            yield return null;
            Assert.That(manager.TryAutoCast(new Vector2(3,2),3),Is.True);
            Assert.That(probe.Casts,Is.Zero);
            Assert.That(manager.IsCasting,Is.True);
            Assert.That(manager.TryAutoCast(Vector2.left,1),Is.False);
            GameplayState.EnterModal(owner);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(probe.Casts,Is.Zero);
            GameplayState.ExitModal(owner);
            yield return new WaitForSeconds(0.17f);
            Assert.That(probe.Casts,Is.EqualTo(1));
            Assert.That(probe.Point,Is.EqualTo(new Vector2(3,2)));
            yield return new WaitForSeconds(0.35f);
            Assert.That(manager.IsCasting,Is.False);
            Assert.That(manager.GetCooldownRemaining(0),Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DisableCancelsPendingCastAndEffectsCleanUp()
        {
            owner=new GameObject("CancelCastProbe");
            owner.AddComponent<PlayerStats>();
            var inventory=owner.AddComponent<SkillInventory>();
            data=ScriptableObject.CreateInstance<SkillData>();
            var probe=owner.AddComponent<CastProbeSkill>();probe.Initialize(data,1);
            inventory.AddOrUpgrade("Fireball");inventory.TryEquip("Fireball",0);
            var manager=owner.AddComponent<SkillManager>();
            typeof(SkillManager).GetField("skillDatabase",Private).SetValue(manager,new[]{data});
            yield return null;
            Assert.That(manager.TryAutoCast(Vector2.right,1),Is.True);
            manager.enabled=false;
            yield return new WaitForSeconds(0.6f);
            Assert.That(probe.Casts,Is.Zero);
            Assert.That(manager.IsCasting,Is.False);
            Assert.That(Object.FindObjectsByType<SpellPresentation>(),Is.Empty);
            Assert.That(Object.FindObjectsByType<SpellVfx>(),Is.Empty);
        }
    }
}
