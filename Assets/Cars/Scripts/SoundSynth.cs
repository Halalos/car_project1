using System;
using UnityEngine;

namespace Cars
{
    public enum SoundCue { Click, Start, Impact, Checkpoint, Pass, Upgrade, Denied, Victory, Defeat }
    public enum SynthSound { Engine, Wind, Boost, Click, Start, Impact, Checkpoint, Pass, Upgrade, Denied, Victory, Defeat }

    // Original, deterministic synthesized effects; no external recordings or game assets.
    public static class SoundSynth
    {
        public const int SampleRate = 44100;
        public static float Duration(SynthSound kind)
        {
            switch (kind)
            {
                case SynthSound.Engine: case SynthSound.Wind: case SynthSound.Boost: return 1;
                case SynthSound.Click: return .09f;
                case SynthSound.Impact: return .48f;
                case SynthSound.Pass: return .24f;
                case SynthSound.Denied: return .24f;
                case SynthSound.Victory: return 1.25f;
                case SynthSound.Defeat: return .95f;
                default: return .65f;
            }
        }
        public static float[] Samples(SynthSound kind)
        {
            int count = Mathf.RoundToInt(Duration(kind) * SampleRate);
            var samples = new float[count]; var random = new System.Random(7301 + (int)kind);
            float filtered = 0; double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate, u = i / (float)(count - 1);
                float noise = (float)(random.NextDouble() * 2 - 1);
                filtered = Mathf.Lerp(filtered, noise, .12f);
                float fade = Mathf.Min(1, t * 120) * Mathf.Min(1, (Duration(kind) - t) * 60);
                float value;
                switch (kind)
                {
                    case SynthSound.Engine:
                        // Integer harmonics are phase-continuous across the one-second loop.
                        value = .34f * Tone(60,t) + .23f * Tone(120,t) + .11f * Tone(180,t) + .06f * Tone(300,t);
                        value *= .82f + .18f * Tone(30,t); fade = 1; break;
                    case SynthSound.Wind: value = filtered * .7f; break;
                    case SynthSound.Boost:
                        value = filtered * .72f + .1f * Tone(360,t) + .06f * Tone(540,t); break;
                    case SynthSound.Impact:
                        value = (noise * .55f + Tone(65 - 40*u,t) * .45f) * Mathf.Exp(-t*10); break;
                    case SynthSound.Pass: value = filtered * Mathf.Sin(u * Mathf.PI) * 1.5f; break;
                    default:
                        float frequency = Frequency(kind, u);
                        phase += frequency / SampleRate;
                        value = ((float)Math.Sin(phase * Math.PI * 2) * .48f + (float)Math.Sin(phase * Math.PI * 4) * .1f);
                        value *= Mathf.Exp(-((u * (kind == SynthSound.Victory ? 4 : 3)) % 1) * 2.5f);
                        if (kind == SynthSound.Click) value *= 1-u;
                        break;
                }
                samples[i] = Mathf.Clamp(value * fade * .75f, -.95f, .95f);
            }
            return samples;
        }
        static float Tone(float hz, float t) { return Mathf.Sin(2*Mathf.PI*hz*t); }
        static float Frequency(SynthSound sound, float u)
        {
            switch (sound)
            {
                case SynthSound.Click: return 880 + u*400;
                case SynthSound.Start: return u < .33f ? 220 : u < .66f ? 330 : 660;
                case SynthSound.Checkpoint: return u < .5f ? 659.25f : 987.77f;
                case SynthSound.Upgrade: return u < .33f ? 523.25f : u < .66f ? 659.25f : 1046.5f;
                case SynthSound.Denied: return u < .5f ? 185 : 146.83f;
                case SynthSound.Victory: return u < .25f ? 523.25f : u < .5f ? 659.25f : u < .75f ? 783.99f : 1046.5f;
                case SynthSound.Defeat: return u < .33f ? 392 : u < .66f ? 311.13f : 196;
                default: return 440;
            }
        }
    }
}
