#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cars.Editor
{
    public static class NeonSceneStyle
    {
        public static void Apply(Camera camera, string folder)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.32f,.29f,.48f);
            RenderSettings.fogColor = new Color(.045f,.018f,.105f);
            RenderSettings.fogStartDistance = 130; RenderSettings.fogEndDistance = 450;
            string skyPath = folder + "/Materials/NeonSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null) { sky = new Material(Shader.Find("Cars/SynthwaveSky")); AssetDatabase.CreateAsset(sky, skyPath); }
            RenderSettings.skybox = sky;
            camera.clearFlags = CameraClearFlags.Skybox; camera.backgroundColor = RenderSettings.fogColor;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            string path = folder + "/NeonVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,path); }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1.05f); bloom.intensity.Override(.38f); bloom.scatter.Override(.65f);
            bloom.highQualityFiltering.Override(true);
            var tone = Get<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
            var color = Get<ColorAdjustments>(profile); color.postExposure.Override(.65f); color.contrast.Override(12); color.saturation.Override(-8);
            VisualPolish.Atmosphere(camera, profile);
            EditorUtility.SetDirty(bloom); EditorUtility.SetDirty(tone); EditorUtility.SetDirty(color); EditorUtility.SetDirty(profile);
            var volume = new GameObject("Neon Atmosphere", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true; volume.priority = 1; volume.sharedProfile = profile;
        }
        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var effect)) return effect;
            effect = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(effect, profile); return effect;
        }
    }
}
#endif

