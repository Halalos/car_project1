#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cars.Editor
{
    // Generated details stay reproducible through Build Complete Game.
    public static class VisualPolish
    {
        public static void Materials(string root)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, true);
            var random = new System.Random(731);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float grain = .68f + (float)random.NextDouble() * .52f;
                    if (random.NextDouble() > .985) grain *= 1.4f;
                    pixels[y * size + x] = new Color(.24f, .27f, .36f) * grain;
                }
            texture.SetPixels(pixels); texture.Apply();
            string path = root + "/Materials/AsphaltGrain.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 8;
            importer.mipmapEnabled = true; importer.SaveAndReimport();
            var asphalt = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/Asphalt.mat");
            asphalt.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            asphalt.SetTextureScale("_BaseMap", new Vector2(7, 3));
            asphalt.color = new Color(.48f,.52f,.65f); asphalt.SetFloat("_Smoothness", .48f);
            EditorUtility.SetDirty(asphalt);
            foreach (string name in new [] { "PlayerMint", "RivalCoral", "TrafficGold", "Metal", "Windows" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/" + name + ".mat");
                material.SetFloat("_Metallic", name == "Windows" ? .25f : .58f);
                material.SetFloat("_Smoothness", .78f); EditorUtility.SetDirty(material);
            }
        }
        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name; piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position; piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
        }
        public static void Road(Transform root, Material metal, Material glow)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var pole = new GameObject("Boulevard light").transform; pole.SetParent(root, false);
                pole.localPosition = new Vector3(side * 7.7f, 0, side < 0 ? 8 : 28);
                Part(pole, "Column", new Vector3(0,3.7f,0), new Vector3(.16f,7.4f,.16f), metal);
                Part(pole, "Arm", new Vector3(-side*1.2f,7.3f,0), new Vector3(2.5f,.13f,.18f), metal);
                Part(pole, "LED panel", new Vector3(-side*2.25f,7.2f,0), new Vector3(.8f,.08f,.45f), glow);
                var light = new GameObject("Road light", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(pole, false); light.transform.localPosition = new Vector3(-side*2.25f,7,0);
                light.transform.localRotation = Quaternion.Euler(90,0,0);
                light.type = LightType.Spot; light.range = 17; light.spotAngle = 110;
                light.innerSpotAngle = 55; light.intensity = 5; light.color = new Color(.42f,.7f,1);
                light.shadows = LightShadows.None;
            }
        }
        public static void Car(Transform body, Material metal, Material dark)
        {
            Part(body, "Rear diffuser", new Vector3(0,.27f,-1.96f), new Vector3(1.45f,.18f,.12f), dark);
            for (int i = -2; i <= 2; i++)
                Part(body, "Diffuser fin", new Vector3(i*.24f,.28f,-2.03f), new Vector3(.035f,.18f,.25f), metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(body, "Side skirt", new Vector3(side*.97f,.34f,0), new Vector3(.12f,.13f,2.5f), metal);
                Part(body, "Door handle", new Vector3(side*.925f,.8f,-.42f), new Vector3(.035f,.055f,.25f), metal);
            }
        }
        public static void Atmosphere(Camera camera, VolumeProfile profile)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.25f,.29f,.46f);
            RenderSettings.ambientEquatorColor = new Color(.19f,.14f,.29f);
            RenderSettings.ambientGroundColor = new Color(.07f,.085f,.14f);
            RenderSettings.fogColor = new Color(.075f,.055f,.14f);
            RenderSettings.fogStartDistance = 65; RenderSettings.fogEndDistance = 360;
            camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camera.GetUniversalAdditionalCameraData().antialiasingQuality = AntialiasingQuality.High;
            Vignette vignette;
            if (!profile.TryGet(out vignette)) { vignette = profile.Add<Vignette>(true); AssetDatabase.AddObjectToAsset(vignette, profile); }
            vignette.intensity.Override(.18f); vignette.smoothness.Override(.65f);
            EditorUtility.SetDirty(vignette); EditorUtility.SetDirty(profile);
        }
    }
}
#endif
