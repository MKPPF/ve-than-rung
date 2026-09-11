using TopDownGame.Audio;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>Builder-authored art and sound for the sword sample; no damage values live in this profile.</summary>
    public sealed class SwordComboProfile : ScriptableObject
    {
        [SerializeField] private Sprite[] actorFrames;
        [SerializeField] private Sprite[] slashFrames;
        [SerializeField] private Material actorMaterial;
        [SerializeField] private Material slashMaterial;
        [SerializeField] private AudioCue swingCue;
        [SerializeField] private AudioCue hitCue;
        public Sprite[] ActorFrames => actorFrames;
        public Sprite[] SlashFrames => slashFrames;
        public Material ActorMaterial => actorMaterial;
        public Material SlashMaterial => slashMaterial;
        public AudioCue SwingCue => swingCue;
        public AudioCue HitCue => hitCue;
    }
}
