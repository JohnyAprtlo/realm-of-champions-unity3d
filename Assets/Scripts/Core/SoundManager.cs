using UnityEngine;

namespace MobaGame.Core
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource bgmSource;

        private AudioClip slashClip;
        private AudioClip hitClip;
        private AudioClip dashClip;
        private AudioClip shieldClip;
        private AudioClip ultClip;
        private AudioClip victoryClip;
        private AudioClip defeatClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupAudioSources();
                GenerateProceduralClips();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupAudioSources()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.45f;
        }

        private void GenerateProceduralClips()
        {
            slashClip = CreateWhooshClip("SlashWhoosh", 0.18f, 600f, 150f);
            hitClip = CreateHitImpactClip("HitImpact", 0.22f);
            dashClip = CreateWhooshClip("DashWhoosh", 0.25f, 300f, 800f);
            shieldClip = CreateToneClip("ShieldChime", 0.45f, 440f, 880f);
            ultClip = CreateExplosionClip("CelestialExplosion", 0.75f);
            victoryClip = CreateFanfareClip("VictoryFanfare", true);
            defeatClip = CreateFanfareClip("DefeatFanfare", false);
        }

        public void PlaySlash() { PlaySFX(slashClip, 0.7f); }
        public void PlayHit() { PlaySFX(hitClip, 0.85f); }
        public void PlayDash() { PlaySFX(dashClip, 0.75f); }
        public void PlayShield() { PlaySFX(shieldClip, 0.8f); }
        public void PlayUltimate() { PlaySFX(ultClip, 1.0f); }
        public void PlayVictory() { PlaySFX(victoryClip, 0.9f); }
        public void PlayDefeat() { PlaySFX(defeatClip, 0.9f); }

        private void PlaySFX(AudioClip clip, float vol = 1f)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, vol);
            }
        }

        // Procedural Audio Generators
        private AudioClip CreateWhooshClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float sin = Mathf.Sin(2f * Mathf.PI * freq * (float)i / sampleRate);
                float envelope = Mathf.Sin(t * Mathf.PI);
                data[i] = (sin * 0.6f + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateHitImpactClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 12f);
                float hit = Mathf.Sin(2f * Mathf.PI * 110f * (float)i / sampleRate) * 0.6f;
                float noise = (Random.value * 2f - 1f) * 0.5f;
                data[i] = (hit + noise) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateToneClip(string name, float duration, float f1, float f2)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI);
                float s1 = Mathf.Sin(2f * Mathf.PI * f1 * (float)i / sampleRate);
                float s2 = Mathf.Sin(2f * Mathf.PI * f2 * (float)i / sampleRate);
                data[i] = (s1 * 0.5f + s2 * 0.5f) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateExplosionClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 5f);
                float bass = Mathf.Sin(2f * Mathf.PI * 60f * (float)i / sampleRate) * 0.8f;
                float noise = (Random.value * 2f - 1f) * 0.5f;
                data[i] = (bass + noise) * env;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFanfareClip(string name, bool victory)
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[] notes = victory ? new float[] { 392f, 523.25f, 659.25f, 783.99f } : new float[] { 440f, 392f, 349.23f, 261.63f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                int noteIndex = Mathf.Clamp((int)(t * notes.Length), 0, notes.Length - 1);
                float freq = notes[noteIndex];
                float tone = Mathf.Sin(2f * Mathf.PI * freq * (float)i / sampleRate);
                float env = Mathf.Sin(t * Mathf.PI);
                data[i] = tone * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
