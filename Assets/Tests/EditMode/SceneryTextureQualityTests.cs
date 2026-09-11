using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class SceneryTextureQualityTests
    {
        [TestCase("Assets/Art/Generated/Lobby/WoodlandWaystationV7_GardenPond.png")]
        [TestCase("Assets/Art/Generated/Lobby/WoodlandWaystation_RestoredV1.png")]
        [TestCase("Assets/Art/Generated/Environment/Map02_AncientWoodland_RestoredV1.png")]
        [TestCase("Assets/Art/Generated/Environment/Map01_EmeraldSanctuaryV7.png")]
        public void FullMapPaintingsRetainSourcePixelsAndUseSmoothSampling(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.maxTextureSize,Is.GreaterThanOrEqualTo(4096));
            Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            Assert.That(texture.width,Is.EqualTo(width));
            Assert.That(texture.height,Is.EqualTo(height));
            Assert.That(importer.isReadable,Is.False,"Avoid keeping a CPU-readable copy for static backgrounds.");
        }

        [Test]
        public void RestoredPaintingsKeepApprovedDimensions()
        {
            var lobby=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/Lobby/WoodlandWaystation_RestoredV1.png");
            var forest=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/Environment/Map02_AncientWoodland_RestoredV1.png");
            Assert.That(lobby.width,Is.EqualTo(6476));
            Assert.That(lobby.height,Is.EqualTo(3884));
            Assert.That(forest.width,Is.EqualTo(6144));
            Assert.That(forest.height,Is.EqualTo(4096));
        }

        [Test]
        public void TiledGroundRetainsRepeatWrapping()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Generated/Environment/ForestFloor.png");
            Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Bilinear));
        }
    }
}
