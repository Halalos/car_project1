#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cars.Editor
{
    public static class AudioAssetBuilder
    {
        public static void Generate(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (SynthSound sound in Enum.GetValues(typeof(SynthSound)))
            {
                string path = folder + "/" + sound + ".wav";
                var samples = SoundSynth.Samples(sound);
                using (var file = new MemoryStream())
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length*2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                    writer.Write((short)1); writer.Write((short)1); writer.Write(SoundSynth.SampleRate);
                    writer.Write(SoundSynth.SampleRate*2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length*2);
                    foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
                    File.WriteAllBytes(path, file.ToArray());
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
            }
        }
        public static void Assign(GameAudio audio, string folder)
        {
            audio.engineClip = Clip(folder, SynthSound.Engine); audio.windClip = Clip(folder, SynthSound.Wind);
            audio.boostClip = Clip(folder, SynthSound.Boost); audio.clickClip = Clip(folder, SynthSound.Click);
            audio.startClip = Clip(folder, SynthSound.Start); audio.impactClip = Clip(folder, SynthSound.Impact);
            audio.checkpointClip = Clip(folder, SynthSound.Checkpoint); audio.passClip = Clip(folder, SynthSound.Pass);
            audio.upgradeClip = Clip(folder, SynthSound.Upgrade); audio.deniedClip = Clip(folder, SynthSound.Denied);
            audio.victoryClip = Clip(folder, SynthSound.Victory); audio.defeatClip = Clip(folder, SynthSound.Defeat);
        }
        static AudioClip Clip(string folder, SynthSound kind) { return AssetDatabase.LoadAssetAtPath<AudioClip>(folder + "/" + kind + ".wav"); }
    }
}
#endif
