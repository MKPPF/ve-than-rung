using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TopDownGame.EditorTools
{
    public static partial class TopDownGameBuilder
    {
        public const string GameFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        public const string VietnameseCharacters =
            "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ" +
            "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ";

        /// <summary>Bakes localized glyphs into the referenced primary atlas so builds never need a transient fallback texture.</summary>
        [MenuItem("Top Down Game/Prepare Vietnamese Font")]
        public static void PrepareVietnameseFont()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before baking the game font.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameFontPath);
            if (font == null) throw new BuildFailedException("Missing game font: " + GameFontPath);
            // Preserve the existing asset GUID and material/texture subassets used by all scenes.
            var required = new HashSet<uint>(font.characterTable.Select(c => c.unicode));
            for (uint c = 32; c <= 126; c++) required.Add(c);
            foreach (char c in VietnameseCharacters + "•…–—×÷→←↑↓★☆✓□−") required.Add(c);
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            var sizeSettings = new SerializedObject(font);
            sizeSettings.FindProperty("m_AtlasWidth").intValue = 4096;
            sizeSettings.FindProperty("m_AtlasHeight").intValue = 4096;
            sizeSettings.ApplyModifiedPropertiesWithoutUndo();
            font.isMultiAtlasTexturesEnabled = false;
            font.ClearFontAssetData();
            // A few optional symbol glyphs may not exist in Liberation Sans; Vietnamese must all exist.
            font.TryAddCharacters(required.OrderBy(c => c).ToArray(), out uint[] missing, true);
            foreach (char c in VietnameseCharacters)
                if (!font.HasCharacter((int)c)) throw new BuildFailedException("Vietnamese glyph missing: " + c);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.material.SetTexture("_MainTex", font.atlasTexture);
            font.material.SetFloat("_TextureWidth", font.atlasWidth);
            font.material.SetFloat("_TextureHeight", font.atlasHeight);
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.material);
            foreach (Texture2D atlas in font.atlasTextures) EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Debug.Log("Vietnamese font baked: " + font.characterTable.Count + " characters; static atlas, no runtime Vietnamese fallback required.");
        }
    }

    /// <summary>Rejects shipping a font that would depend on Editor-only dynamic glyph data.</summary>
    public sealed class VietnameseFontBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TopDownGameBuilder.GameFontPath);
            if (font == null || font.atlasPopulationMode != AtlasPopulationMode.Static ||
                TopDownGameBuilder.VietnameseCharacters.Any(c => !font.HasCharacter((int)c)))
                throw new BuildFailedException("Run Top Down Game > Prepare Vietnamese Font before building.");
        }
    }
}
