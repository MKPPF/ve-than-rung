using UnityEngine;

namespace TopDownGame.Audio
{
    public enum AudioBus
    {
        Music,
        Ambient,
        Sfx,
        Ui
    }

    [CreateAssetMenu(menuName = "Top Down Game/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);
        [SerializeField, Min(1)] private int maxConcurrent = 3;
        [SerializeField] private bool spatial = true;
        [SerializeField, Min(0f)] private float minimumDistance = 1.5f;
        [SerializeField, Min(0.1f)] private float maximumDistance = 14f;

        public AudioBus Bus => bus;
        public float Volume => volume;
        public int MaxConcurrent => maxConcurrent;
        public bool Spatial => spatial && bus != AudioBus.Music && bus != AudioBus.Ui;
        public float MinimumDistance => minimumDistance;
        public float MaximumDistance => Mathf.Max(minimumDistance, maximumDistance);
        public float RandomPitch => Random.Range(Mathf.Min(pitchRange.x, pitchRange.y),
            Mathf.Max(pitchRange.x, pitchRange.y));
        public AudioClip RandomClip
        {
            get
            {
                if (clips == null) return null;
                int count = 0;
                foreach (AudioClip clip in clips) if (clip != null) count++;
                if (count == 0) return null;
                int selected = Random.Range(0, count);
                foreach (AudioClip clip in clips)
                    if (clip != null && selected-- == 0) return clip;
                return null;
            }
        }
    }
}
