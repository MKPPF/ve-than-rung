using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Audio;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Uses synthetic clips to verify transition arbitration and live settings independently of placeholder content.</summary>
    public sealed class AudioFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> generated = new List<Object>();
        private readonly string[] names = { "Master", "Music", "Ambient", "SFX" };
        private readonly Dictionary<string, (bool existed, float value)> preferences =
            new Dictionary<string, (bool, float)>();
        private GameAudioManager audio;
        private GameObject pauseOwner;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            foreach (string name in names)
            {
                string key = GamePreferences.Key("TopDownGame.Audio." + name);
                Assert.That(key, Does.StartWith("TopDownGame.Tests."));
                preferences[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetFloat(key));
            }
            if (GameAudioManager.Instance != null) Object.Destroy(GameAudioManager.Instance.gameObject);
            yield return null;
            audio = GameAudioManager.EnsureExists();
            audio.SetMasterVolume(1);
            audio.SetMusicVolume(1);
            audio.SetAmbientVolume(1);
            audio.SetSfxVolume(1);
            pauseOwner = new GameObject("AudioFlowPauseProbe");
        }

        private AudioCue Cue(string name, AudioBus bus = AudioBus.Music, int maximum = 100)
        {
            var cue = ScriptableObject.CreateInstance<AudioCue>();
            cue.name = name;
            generated.Add(cue);
            AudioClip clip = AudioClip.Create(name + "Clip", 441000, 1, 44100, false);
            generated.Add(clip);
            Set(cue, "clips", new[] { clip });
            Set(cue, "bus", bus);
            Set(cue, "maxConcurrent", maximum);
            Set(cue, "spatial", false);
            return cue;
        }

        private static void Set(AudioCue cue, string field, object value) =>
            typeof(AudioCue).GetField(field, Private).SetValue(cue, value);
        private AudioSource[] ActiveLoops() => audio.GetComponentsInChildren<AudioSource>(true)
            .Where(source => source.gameObject.activeSelf && source.loop).ToArray();

        [UnityTest]
        public IEnumerator RepeatingThemeRequestKeepsSourceAndPlaybackPosition()
        {
            AudioCue cue = Cue("AudioSameTheme");
            audio.CrossfadeMusic(cue, 0.05f);
            yield return new WaitForSecondsRealtime(0.1f);
            AudioSource first = ActiveLoops().Single();
            first.timeSamples = 22050;
            int before = first.timeSamples;
            for (int i = 0; i < 20; i++) audio.CrossfadeMusic(cue, 1);
            Assert.That(ActiveLoops().Single(), Is.SameAs(first));
            Assert.That(first.timeSamples, Is.EqualTo(before));
            Assert.That(first.volume, Is.EqualTo(1).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator LiveVolumeChangesRemainAppliedThroughoutFadeWhilePaused()
        {
            AudioCue music = Cue("AudioMusicFade");
            AudioCue ambient = Cue("AudioAmbientFade", AudioBus.Ambient);
            GameplayState.EnterModal(pauseOwner);
            audio.CrossfadeMusic(music, 0.4f);
            audio.CrossfadeAmbient(ambient, 0.4f);
            yield return new WaitForSecondsRealtime(0.08f);
            audio.SetMasterVolume(0);
            yield return new WaitForSecondsRealtime(0.08f);
            foreach (AudioSource source in ActiveLoops()) Assert.That(source.volume, Is.Zero);
            audio.SetMusicVolume(0.2f);
            audio.SetAmbientVolume(0.4f);
            audio.SetMasterVolume(0.8f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(ActiveLoops().Length, Is.EqualTo(2));
            Assert.That(ActiveLoops().Single(s => s.clip.name == "AudioMusicFadeClip").volume, Is.EqualTo(0.16f).Within(0.001f));
            Assert.That(ActiveLoops().Single(s => s.clip.name == "AudioAmbientFadeClip").volume, Is.EqualTo(0.32f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator RapidTransitionsReleaseSupersededLoopsAndKeepLatestTheme()
        {
            AudioCue[] cues = { Cue("AudioFirst"), Cue("AudioSecond"), Cue("AudioLatest") };
            for (int i = 0; i < 50; i++)
            {
                audio.CrossfadeMusic(cues[i % 3], 0.05f);
                Assert.That(ActiveLoops().Length, Is.LessThanOrEqualTo(2));
            }
            audio.CrossfadeMusic(cues[2], 0.05f);
            yield return new WaitForSecondsRealtime(0.15f);
            AudioSource source = ActiveLoops().Single();
            Assert.That(source.clip.name, Is.EqualTo("AudioLatestClip"));
            Assert.That(source.pitch, Is.EqualTo(1));
            Assert.That(source.volume, Is.EqualTo(1).Within(0.001f));
            Assert.That(audio.GetComponentsInChildren<AudioSource>(true).Length, Is.LessThanOrEqualTo(32));
        }

        [UnityTest]
        public IEnumerator VoicePoolExpandsWithActiveSourcesAndStopsAtCap()
        {
            AudioCue cue = Cue("AudioPool", AudioBus.Sfx);
            var acquired = new HashSet<AudioSource>();
            for (int i = 0; i < 40; i++)
            {
                AudioSource source = audio.Play(cue, Vector3.zero);
                if (i >= 32) { Assert.That(source, Is.Null); continue; }
                Assert.That(source, Is.Not.Null, "voice " + i);
                Assert.That(source.gameObject.activeInHierarchy, Is.True, "expanded voice " + i);
                Assert.That(acquired.Add(source), Is.True, "active voice must not be reused");
            }
            Assert.That(acquired.Count, Is.EqualTo(32));
            Assert.That(audio.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(32));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoopReleaseDoesNotDecrementConcurrentOneShotCount()
        {
            AudioCue shared = Cue("AudioSharedLimit", maximum: 1);
            Assert.That(audio.Play(shared, Vector3.zero), Is.Not.Null);
            audio.CrossfadeMusic(shared, 0.05f);
            yield return new WaitForSecondsRealtime(0.1f);
            audio.CrossfadeMusic(Cue("AudioReplacement"), 0.05f);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(audio.Play(shared, Vector3.zero), Is.Null, "The original ten-second one-shot is still active.");
        }

        [UnityTest]
        public IEnumerator EmptyCueWarnsOnceAndMixedClipArrayAlwaysReturnsPlayableClip()
        {
            AudioCue cue = ScriptableObject.CreateInstance<AudioCue>();
            cue.name = "AudioMissingClipProbe";
            generated.Add(cue);
            LogAssert.Expect(LogType.Warning, "Audio cue 'AudioMissingClipProbe' has no playable clip; see Assets/Audio/README_MISSING_AUDIO.md.");
            Assert.That(audio.Play(cue, Vector3.zero), Is.Null);
            Assert.That(audio.Play(cue, Vector3.zero), Is.Null);
            audio.CrossfadeMusic(cue);
            audio.CrossfadeAmbient(cue);
            Assert.That(ActiveLoops(), Is.Empty);
            AudioCue valid = Cue("AudioMixedClips");
            AudioClip clip = valid.RandomClip;
            Set(valid, "clips", new AudioClip[] { null, clip, null });
            for (int i = 0; i < 100; i++) Assert.That(valid.RandomClip, Is.SameAs(clip));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            GameplayState.ExitModal(pauseOwner);
            if (pauseOwner != null) Object.Destroy(pauseOwner);
            if (audio != null) Object.Destroy(audio.gameObject);
            yield return null;
            foreach (Object item in generated) if (item != null) Object.Destroy(item);
            generated.Clear();
            foreach (var preference in preferences)
                if (preference.Value.existed) PlayerPrefs.SetFloat(preference.Key, preference.Value.value);
                else PlayerPrefs.DeleteKey(preference.Key);
            PlayerPrefs.Save();
            GameAudioManager.EnsureExists();
        }
    }
}
