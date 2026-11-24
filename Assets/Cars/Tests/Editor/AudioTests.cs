using System;
using NUnit.Framework;
using UnityEngine;

namespace Cars.Tests
{
    public sealed class AudioTests
    {
        [Test]
        public void OriginalEffectsHaveFiniteNonSilentUnclippedSamples()
        {
            foreach (SynthSound sound in Enum.GetValues(typeof(SynthSound)))
            {
                var samples = SoundSynth.Samples(sound);
                Assert.That(samples.Length, Is.GreaterThan(2000), sound.ToString());
                double energy = 0; bool finite = true; float peak = 0;
                foreach (float sample in samples)
                {
                    finite &= !float.IsNaN(sample) && !float.IsInfinity(sample);
                    peak = Mathf.Max(peak,Mathf.Abs(sample)); energy += sample*sample;
                }
                Assert.IsTrue(finite); Assert.That(peak, Is.LessThan(1));
                Assert.That(energy / samples.Length, Is.GreaterThan(.00001), sound.ToString());
            }
        }
        [Test]
        public void EngineLoopHasNoBoundaryDiscontinuity()
        {
            var samples = SoundSynth.Samples(SynthSound.Engine);
            Assert.That(Mathf.Abs(samples[0] - samples[samples.Length-1]), Is.LessThan(.03f));
        }
    }
}
