using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cars.Tests
{
    public sealed class NeonAssetTests
    {
        const string Root = "Assets/Cars/Generated";
        [Test]
        public void EmissiveRoadMaterialsKeepEmissionAfterAssetImport()
        {
            foreach (string name in new [] { "Ivory", "RoadPaint", "Headlamps", "NeonWindows", "TailGlow" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");
                Assert.IsNotNull(material, name);
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"), name);
                Assert.That(material.GetColor("_EmissionColor").maxColorComponent, Is.GreaterThan(1));
            }
        }
        [Test]
        public void RendererHasRealPostProcessingResources()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Root + "/CarsRenderer.asset");
            Assert.IsNotNull(renderer);
            var serialized = new SerializedObject(renderer);
            Assert.IsNotNull(serialized.FindProperty("postProcessData").objectReferenceValue);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<ScriptableObject>(Root + "/NeonVolume.asset"));
        }
        [Test]
        public void GeneratedWavFilesImportAsPlayableMonoClips()
        {
            foreach (SynthSound sound in Enum.GetValues(typeof(SynthSound)))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + sound + ".wav");
                Assert.IsNotNull(clip,sound.ToString()); Assert.That(clip.channels,Is.EqualTo(1));
                Assert.That(clip.frequency,Is.EqualTo(SoundSynth.SampleRate));
                Assert.That(clip.length,Is.EqualTo(SoundSynth.Duration(sound)).Within(.002f));
            }
        }
    }
}
