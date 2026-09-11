using NUnit.Framework;
using TopDownGame.Skills;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class SkillArchitectureTests
    {
        [Test]
        public void AllSkillsHaveIndependentPresentationProfiles()
        {
            foreach(SkillId id in System.Enum.GetValues(typeof(SkillId)))
            {
                var data=AssetDatabase.LoadAssetAtPath<SkillData>("Assets/ScriptableObjects/Skills/"+id+".asset");
                Assert.That(data,Is.Not.Null);
                Assert.That(data.Presentation,Is.Not.Null,id.ToString());
                Assert.That(data.Presentation.Shader,Is.Not.Null);
                Assert.That(data.Presentation.Shader.isSupported,Is.True);
                Assert.That(data.Presentation.Windup,Is.InRange(0.04f,0.5f));
                Assert.That(data.Presentation.Recovery,Is.GreaterThan(0));
                Assert.That(AssetDatabase.GetAssetPath(data.Icon),Does.Contain("SkillIconV4_"));
                if(id!=SkillId.SwordSpin)
                {
                    Assert.That(data.Presentation.HasPaintedArt,Is.True,id.ToString());
                    Assert.That(data.Presentation.ActorFrames.Length,Is.EqualTo(6));
                    Assert.That(data.Presentation.PaintedFrames[0].rect.width,Is.GreaterThan(400));
                    Assert.That(data.Presentation.PaintedMaterial.shader.isSupported,Is.True);
                }
                Assert.That(AssetDatabase.GetAssetPath(data.Presentation),Does.Contain(id+"Presentation"));
            }
        }
    }
}
