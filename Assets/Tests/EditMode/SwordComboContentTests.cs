using NUnit.Framework;
using TopDownGame.Skills;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class SwordComboContentTests
    {
        [Test]
        public void SwordSampleHasSixPosesPaintedSlashesAndSound()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var p=prefab.GetComponent<SwordSpinSkill>().ComboProfile;
            Assert.That(p,Is.Not.Null);
            Assert.That(p.ActorFrames.Length,Is.EqualTo(6));Assert.That(p.SlashFrames.Length,Is.EqualTo(6));
            foreach(var s in p.ActorFrames){Assert.That(s,Is.Not.Null);Assert.That(s.rect.width,Is.GreaterThan(400));}
            foreach(var s in p.SlashFrames) Assert.That(s,Is.Not.Null);
            Assert.That(p.ActorMaterial.shader.isSupported,Is.True);
            Assert.That(p.SlashMaterial.shader.isSupported,Is.True);
            Assert.That(p.SwingCue.RandomClip,Is.Not.Null);Assert.That(p.HitCue.RandomClip,Is.Not.Null);
        }
    }
}
