using NUnit.Framework;
using TMPro;
using TopDownGame.EditorTools;
using UnityEditor;

namespace TopDownGame.Tests.EditMode
{
    /// <summary>Checks the primary shipped atlas instead of allowing Editor dynamic fallback to hide missing glyphs.</summary>
    public sealed class VietnameseFontTests
    {
        [Test]
        public void ShippedPrimaryFontContainsAllVietnameseGlyphs()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TopDownGameBuilder.GameFontPath);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(font.atlasTexture.width, Is.GreaterThan(0));
            Assert.That(font.material.mainTexture == font.atlasTexture, Is.True);
            foreach (char c in TopDownGameBuilder.VietnameseCharacters)
            {
                Assert.That(font.HasCharacter((int)c), Is.True, "Missing primary glyph " + c);
                var glyph = font.characterLookupTable[c].glyph;
                Assert.That(glyph.glyphRect.width, Is.GreaterThan(0), "Empty glyph " + c);
                Assert.That(glyph.glyphRect.height, Is.GreaterThan(0), "Empty glyph " + c);
            }
            Assert.That(new SerializedObject(font).FindProperty("m_ClearDynamicDataOnBuild").boolValue, Is.False);
        }
    }
}
