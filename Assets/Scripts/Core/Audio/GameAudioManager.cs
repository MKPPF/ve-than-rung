using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TopDownGame.Core;

namespace TopDownGame.Audio
{
    /// <summary>Bounds audio voices and owns loop transitions so scene changes and live volume controls share one gain path.</summary>
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour
    {
        private const int PrewarmSources = 18;
        private const int MaximumSources = 32;
        private const string MasterKey = "TopDownGame.Audio.Master";
        private const string MusicKey = "TopDownGame.Audio.Music";
        private const string AmbientKey = "TopDownGame.Audio.Ambient";
        private const string SfxKey = "TopDownGame.Audio.SFX";

        [Header("Optional AudioMixer routing")]
        [SerializeField] private AudioMixer mixerAsset;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        private sealed class Voice
        {
            public AudioSource Source;
            public AudioCue Cue;
            public float EndTime;
            public AudioBus Bus;
            public float FadeGain = 1f;
            public bool Counted;
        }

        private sealed class LoopChannel
        {
            public Voice Current;
            public Voice Outgoing;
            public Coroutine Transition;
        }

        private static GameAudioManager instance;
        private readonly List<Voice> voices = new List<Voice>(MaximumSources);
        private readonly Dictionary<ulong, int> cueVoiceCounts =
            new Dictionary<ulong, int>();
        private readonly LoopChannel musicChannel = new LoopChannel();
        private readonly LoopChannel ambientChannel = new LoopChannel();
        private readonly HashSet<ulong> missingClipWarnings = new HashSet<ulong>();

        public static GameAudioManager Instance => instance;
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.75f;
        public float AmbientVolume { get; private set; } = 0.7f;
        public float SfxVolume { get; private set; } = 0.85f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        public static GameAudioManager EnsureExists()
        {
            if (instance != null) return instance;
            GameObject root = new GameObject(nameof(GameAudioManager));
            instance = root.AddComponent<GameAudioManager>();
            DontDestroyOnLoad(root);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GamePreferences.Key(MasterKey), 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GamePreferences.Key(MusicKey), 0.75f));
            AmbientVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GamePreferences.Key(AmbientKey), 0.7f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GamePreferences.Key(SfxKey), 0.85f));
            LoadMixerRouting();
            for (int i = 0; i < PrewarmSources; i++) CreateVoice();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        private void Update()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                Voice voice = voices[i];
                if (!voice.Source.gameObject.activeSelf ||
                    float.IsPositiveInfinity(voice.EndTime) ||
                    Time.unscaledTime < voice.EndTime) continue;
                Release(voice);
            }
        }

        public AudioSource Play(AudioCue cue, Vector3 position)
        {
            if (cue == null) return null;
            AudioClip clip = cue.RandomClip;
            if (clip == null) { WarnMissingClip(cue); return null; }
            ulong cueId = EntityId.ToULong(cue.GetEntityId());
            if (cueVoiceCounts.TryGetValue(cueId, out int count) &&
                count >= cue.MaxConcurrent) return null;
            Voice voice = AcquireVoice();
            if (voice == null) return null;
            voice.Cue = cue;
            voice.Bus = cue.Bus;
            voice.FadeGain = 1f;
            voice.Counted = true;
            voice.Source.transform.position = position;
            voice.Source.clip = clip;
            voice.Source.pitch = cue.RandomPitch;
            voice.Source.loop = false;
            voice.Source.spatialBlend = cue.Spatial ? 1f : 0f;
            voice.Source.rolloffMode = AudioRolloffMode.Linear;
            voice.Source.minDistance = cue.MinimumDistance;
            voice.Source.maxDistance = cue.MaximumDistance;
            voice.Source.outputAudioMixerGroup = GroupFor(cue.Bus);
            ApplyVolume(voice);
            voice.EndTime = Time.unscaledTime + clip.length /
                Mathf.Max(0.1f, Mathf.Abs(voice.Source.pitch));
            cueVoiceCounts[cueId] = count + 1;
            voice.Source.Play();
            return voice.Source;
        }

        public void CrossfadeMusic(AudioCue cue, float duration = 1.5f)
        {
            BeginLoop(musicChannel, AudioBus.Music, cue, duration);
        }

        public void CrossfadeAmbient(AudioCue cue, float duration = 1.5f)
        {
            BeginLoop(ambientChannel, AudioBus.Ambient, cue, duration);
        }

        public void SetMasterVolume(float value) => SetVolume(MasterKey,
            value, v => MasterVolume = v);
        public void SetMusicVolume(float value) => SetVolume(MusicKey,
            value, v => MusicVolume = v);
        public void SetAmbientVolume(float value) => SetVolume(AmbientKey,
            value, v => AmbientVolume = v);
        public void SetSfxVolume(float value) => SetVolume(SfxKey,
            value, v => SfxVolume = v);

        private void BeginLoop(LoopChannel channel, AudioBus bus, AudioCue cue, float duration)
        {
            AudioClip clip = cue != null ? cue.RandomClip : null;
            if (clip == null) { if (cue != null) WarnMissingClip(cue); return; }
            // Requests for the active theme retain playback position, including
            // requests received before its fade has finished.
            if (channel.Current != null && channel.Current.Cue == cue &&
                channel.Current.Source.gameObject.activeSelf) return;
            if (channel.Transition != null) StopCoroutine(channel.Transition);
            channel.Transition = null;
            Release(channel.Outgoing);
            channel.Outgoing = null;
            Voice voice = AcquireVoice();
            if (voice == null)
            {
                if (channel.Current != null)
                {
                    channel.Current.FadeGain = 1f;
                    ApplyVolume(channel.Current);
                }
                return;
            }
            AudioSource next = voice.Source;
            next.clip = clip;
            next.pitch = 1f;
            next.loop = true;
            next.spatialBlend = 0f;
            next.outputAudioMixerGroup = GroupFor(bus);
            next.volume = 0f;
            voice.Cue = cue;
            voice.Bus = bus;
            voice.FadeGain = 0f;
            voice.Counted = false;
            voice.EndTime = float.PositiveInfinity;
            next.Play();
            channel.Outgoing = channel.Current;
            channel.Current = voice;
            channel.Transition = StartCoroutine(CrossfadeLoop(channel, duration));
        }

        private IEnumerator CrossfadeLoop(LoopChannel channel, float duration)
        {
            Voice next = channel.Current;
            Voice previous = channel.Outgoing;
            float start = previous != null ? previous.FadeGain : 0f;
            float elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                next.FadeGain = t;
                ApplyVolume(next);
                if (previous != null)
                {
                    previous.FadeGain = Mathf.Lerp(start, 0f, t);
                    ApplyVolume(previous);
                }
                yield return null;
            }
            next.FadeGain = 1f;
            ApplyVolume(next);
            Release(previous);
            channel.Outgoing = null;
            channel.Transition = null;
        }

        private Voice AcquireVoice()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                if (!voices[i].Source.gameObject.activeSelf)
                {
                    voices[i].Source.gameObject.SetActive(true);
                    return voices[i];
                }
            }
            if (voices.Count >= MaximumSources) return null;
            Voice created = CreateVoice();
            created.Source.gameObject.SetActive(true);
            return created;
        }

        private Voice CreateVoice()
        {
            GameObject child = new GameObject("PooledAudioSource");
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            Voice voice = new Voice { Source = source };
            voices.Add(voice);
            child.SetActive(false);
            return voice;
        }

        private void Release(Voice voice)
        {
            if (voice == null || voice.Source == null) return;
            if (voice.Counted && voice.Cue != null)
            {
                ulong id = EntityId.ToULong(voice.Cue.GetEntityId());
                if (cueVoiceCounts.TryGetValue(id, out int count))
                    cueVoiceCounts[id] = Mathf.Max(0, count - 1);
            }
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.loop = false;
            voice.Source.gameObject.SetActive(false);
            voice.Cue = null;
            voice.EndTime = 0f;
            voice.Counted = false;
            voice.FadeGain = 1f;
        }

        private AudioMixerGroup GroupFor(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Music => musicGroup,
                AudioBus.Ambient => ambientGroup,
                AudioBus.Ui => uiGroup,
                _ => sfxGroup
            };
        }

        private void LoadMixerRouting()
        {
            if (mixerAsset == null)
                mixerAsset = Resources.Load<AudioMixer>("Audio/GameAudioMixer");
            if (mixerAsset == null) return;
            AudioMixerGroup[] groups = mixerAsset.FindMatchingGroups(string.Empty);
            for (int i = 0; i < groups.Length; i++)
            {
                AudioMixerGroup group = groups[i];
                if (group == null) continue;
                switch (group.name)
                {
                    case "Music": musicGroup = group; break;
                    case "Ambient": ambientGroup = group; break;
                    case "SFX": sfxGroup = group; break;
                    case "UI": uiGroup = group; break;
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioCue music = Resources.Load<AudioCue>(
                "Audio/Cues/Music_Exploration");
            AudioCue ambient = Resources.Load<AudioCue>(scene.name.StartsWith(
                "Cave", System.StringComparison.OrdinalIgnoreCase)
                    ? "Audio/Cues/Ambient_Cave"
                    : "Audio/Cues/Ambient_Forest");
            if (music != null)
                CrossfadeMusic(music, 1.6f);
            if (ambient != null)
                CrossfadeAmbient(ambient, 1.6f);
        }

        private float VolumeFor(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Music => MusicVolume,
                AudioBus.Ambient => AmbientVolume,
                AudioBus.Ui => SfxVolume,
                _ => SfxVolume
            };
        }

        private void SetVolume(string key, float value,
            System.Action<float> setter)
        {
            float clamped = Mathf.Clamp01(value);
            setter(clamped);
            PlayerPrefs.SetFloat(GamePreferences.Key(key), clamped);
            PlayerPrefs.Save();
            RefreshPlayingVolumes();
        }

        private void RefreshPlayingVolumes()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                Voice voice = voices[i];
                if (voice.Source.gameObject.activeSelf && voice.Cue != null)
                    ApplyVolume(voice);
            }
        }

        private void ApplyVolume(Voice voice)
        {
            voice.Source.volume = voice.Cue.Volume * VolumeFor(voice.Bus) *
                MasterVolume * voice.FadeGain;
        }

        private void WarnMissingClip(AudioCue cue)
        {
            if (missingClipWarnings.Add(EntityId.ToULong(cue.GetEntityId())))
                Debug.LogWarning("Audio cue '" + cue.name + "' has no playable clip; see Assets/Audio/README_MISSING_AUDIO.md.", cue);
        }
    }
}
