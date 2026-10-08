using UnityEngine;

namespace Rpg.Gameplay
{
    // Small original synthesized cues, generated once; no external audio or service.
    // Audible mix and device output still require a native gameplay audition.
    internal sealed class ClearingAudio
    {
        private readonly AudioSource source;
        private readonly AudioClip light;
        private readonly AudioClip burst;
        private readonly AudioClip hurt;
        private readonly AudioClip unlock;
        private int variation;
        internal bool Muted { get { return source.mute; } }

        internal ClearingAudio(GameObject owner)
        {
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = .18f;
            light = Tone("Clearing light", .075f, 660f, 290f);
            burst = Tone("Clearing burst", .17f, 330f, 880f);
            hurt = Tone("Clearing hurt", .12f, 160f, 85f);
            unlock = Tone("Clearing reward", .32f, 440f, 880f);
        }

        private static AudioClip Tone(string name, float seconds, float start, float end)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.RoundToInt(rate * seconds)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += Mathf.Lerp(start, end, t) * 2f * Mathf.PI / rate;
                float envelope = Mathf.Min(t * 25f, 1f) * (1f - t) * (1f - t);
                samples[i] = Mathf.Sin(phase) * envelope * .6f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        internal void ToggleMute() { source.mute = !source.mute; }
        internal void Attack(bool isBurst) { Play(isBurst ? burst : light); }
        internal void Hurt() { Play(hurt); }
        internal void Reward() { Play(unlock); }
        private void Play(AudioClip clip)
        {
            source.pitch = 1f + ((variation++ % 3) - 1) * .025f;
            source.PlayOneShot(clip);
        }

        internal void Dispose()
        {
            Object.Destroy(light);
            Object.Destroy(burst);
            Object.Destroy(hurt);
            Object.Destroy(unlock);
        }
    }
}
