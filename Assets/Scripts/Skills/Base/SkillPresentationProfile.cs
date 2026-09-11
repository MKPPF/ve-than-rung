using UnityEngine;

namespace TopDownGame.Skills
{
    public enum SpellElement { Fire, Ice, Blade, Lightning, Nature, Ward, Void }
    /// <summary>Builder-authored presentation only; balance and damage remain in SkillData and skill behaviours.</summary>
    public sealed class SkillPresentationProfile : ScriptableObject
    {
        [SerializeField] private SpellElement element;
        [SerializeField] private Shader shader;
        [SerializeField] private Color primary = Color.cyan;
        [SerializeField] private Color core = Color.white;
        [SerializeField, Range(0.04f,0.5f)] private float windup = 0.18f;
        [SerializeField, Range(0.04f,0.5f)] private float recovery = 0.22f;
        [Header("Painted animation")]
        [SerializeField] private Sprite[] paintedFrames;
        [SerializeField] private Material paintedMaterial;
        [SerializeField] private Sprite[] actorFrames;
        [SerializeField] private Material actorMaterial;
        [SerializeField] private int releasePose = 2;
        public Sprite[] PaintedFrames => paintedFrames;
        public Material PaintedMaterial => paintedMaterial;
        public Sprite[] ActorFrames => actorFrames;
        public Material ActorMaterial => actorMaterial;
        public int ReleasePose => releasePose;
        public bool HasPaintedArt => paintedFrames != null && paintedFrames.Length == 6 && paintedMaterial != null;
        public SpellElement Element => element;
        public Shader Shader => shader;
        public Color Primary => primary;
        public Color Core => core;
        public float Windup => windup;
        public float Recovery => recovery;
    }
}
