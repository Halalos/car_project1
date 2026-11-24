#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cars.Editor
{
    public static class CompleteGameBuilder
    {
        const string Root = "Assets/Cars/Generated";
        static Material asphalt, stripe, rubber, glass, metal, red, teal, yellow, white, grass, sand, leaf, bark, building, rock, lamp;
        static Mesh bodyMesh, cabinMesh, crystalMesh;
        static Material neonWindow, tailGlow;
        static GameConfig config;
        static GameObject player, rival, traffic, obstacle, valley, desert, gate;

        [MenuItem("Tools/Cars/Build Complete Game")]
        public static void BuildCompleteGame()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            ConfigurePipeline();
            MakeMaterials(); VisualPolish.Materials(Root);
            AudioAssetBuilder.Generate(Root + "/Audio");
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/GameConfig.asset");
            if (config == null) { config = ScriptableObject.CreateInstance<GameConfig>(); AssetDatabase.CreateAsset(config, Root + "/GameConfig.asset"); }
            bodyMesh = MakeWedge("BodyMesh", 1.98f, 1.82f, 4.15f, 3.88f, .42f);
            cabinMesh = MakeWedge("CabinMesh", 1.64f, 1.22f, 1.95f, 1.24f, .58f);
            crystalMesh = MakeWedge("CrystalMesh", 2.6f, .08f, 2.6f, .08f, 3.3f);
            player = MakeCar("PlayerCar", teal, 0);
            rival = MakeCar("RivalCar", red, 1);
            traffic = MakeCar("TrafficCar", yellow, 2);
            obstacle = MakeObstacle();
            valley = MakeSegment(false); desert = MakeSegment(true); gate = MakeGate();
            MakeScene("MainMenu", false);
            MakeScene("Gameplay", true);
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap", typeof(GameBootstrap));
            EditorSceneManager.SaveScene(boot, Root + "/Scenes/Bootstrap.unity");
            EditorBuildSettings.scenes = new [] { "Bootstrap", "MainMenu", "Gameplay" }
                .Select(x => new EditorBuildSettingsScene(Root + "/Scenes/" + x + ".unity", true)).ToArray();
            PlayerSettings.companyName = "Cars Student Team";
            PlayerSettings.productName = "Cars Velocity";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(Root + "/Scenes/MainMenu.unity");
            ValidateGeneratedGame();
            Debug.Log("CARS_GENERATION_OK: Scenes, prefabs, URP, UI and all three modes generated. Firebase is NOT verified.");
        }

        static void ConfigurePipeline()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "/CarsRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Root + "/CarsRenderer.asset");
                ResourceReloader.ReloadAllNullIn(renderer, "Packages/com.unity.render-pipelines.universal");
            }
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (renderer.postProcessData == null) throw new InvalidOperationException("URP post-process data is missing");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/CarsURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, Root + "/CarsURP.asset");
            }
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            var serializedPipeline = new SerializedObject(pipeline);
            serializedPipeline.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serializedPipeline.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowDistance = 140; pipeline.shadowCascadeCount = 2;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.vSyncCount = 0;
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(pipeline);
        }
        static Material Mat(string name, Color color, float smooth = .1f, bool emission = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Smoothness", smooth);
            if (emission) { material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 2.4f); }
            else { material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; material.DisableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(material); return material;
        }
        static void MakeMaterials()
        {
            asphalt = Mat("Asphalt", new Color(.025f,.028f,.075f), .45f);
            stripe = Mat("RoadPaint", new Color(1,.045f,.52f), .4f, true);
            rubber = Mat("Tyres", new Color(.012f,.015f,.025f));
            glass = Mat("Windows", new Color(.035f,.09f,.18f), .9f);
            metal = Mat("Metal", new Color(.19f,.12f,.34f), .75f);
            red = Mat("RivalCoral", new Color(.92f,.04f,.40f), .65f);
            teal = Mat("PlayerMint", new Color(.035f,.73f,.93f), .7f);
            yellow = Mat("TrafficGold", new Color(1,.42f,.065f), .65f);
            white = Mat("Ivory", new Color(.12f,.75f,.95f), .4f, true);
            grass = Mat("Grass", new Color(.10f,.035f,.19f));
            sand = Mat("Sand", new Color(.04f,.075f,.17f));
            leaf = Mat("Foliage", new Color(.26f,.06f,.40f), .25f);
            bark = Mat("Wood", new Color(.06f,.025f,.12f));
            building = Mat("Buildings", new Color(.12f,.075f,.23f), .4f);
            rock = Mat("Stone", new Color(.18f,.07f,.27f));
            lamp = Mat("Headlamps", new Color(.55f,.91f,1), .5f, true);
            neonWindow = Mat("NeonWindows", new Color(.82f,.07f,.43f), .5f, true);
            tailGlow = Mat("TailGlow", new Color(1,.025f,.18f), .5f, true);
        }
        static GameObject Piece(string name, Transform parent, Vector3 at, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static GameObject SavePrefab(GameObject source, string name)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(source, Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(source); return saved;
        }
        static Mesh MakeWedge(string name, float bottomWidth, float topWidth, float bottomLength, float topLength, float height)
        {
            string path = Root + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear();
            var corners = new [] {
                new Vector3(-bottomWidth/2,0,-bottomLength/2), new Vector3(bottomWidth/2,0,-bottomLength/2),
                new Vector3(bottomWidth/2,0,bottomLength/2), new Vector3(-bottomWidth/2,0,bottomLength/2),
                new Vector3(-topWidth/2,height,-topLength/2), new Vector3(topWidth/2,height,-topLength/2),
                new Vector3(topWidth/2,height,topLength/2), new Vector3(-topWidth/2,height,topLength/2)
            };
            int[] faces = {0,3,2,1, 4,5,6,7, 0,1,5,4, 3,7,6,2, 0,4,7,3, 1,2,6,5};
            var vertices = new Vector3[24]; var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                for (int j = 0; j < 4; j++) vertices[face*4+j] = corners[faces[face*4+j]];
                int v = face*4; int t = face*6;
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
                triangles[t+3] = v; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh); return mesh;
        }
        static GameObject MeshPart(string name, Transform parent, Vector3 at, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
        static GameObject MakeCar(string name, Material paint, int role)
        {
            var root = new GameObject(name);
            var body = new GameObject("Body").transform; body.SetParent(root.transform, false);
            MeshPart("Sculpted body", body, new Vector3(0,.35f,0), bodyMesh, paint);
            MeshPart("Glazed cabin", body, new Vector3(0,.77f,-.15f), cabinMesh, glass);
            Piece("Roof", body, new Vector3(0,1.36f,-.15f), new Vector3(1.28f,.08f,1.38f), paint);
            Piece("Bonnet stripe", body, new Vector3(0,.785f,1.26f), new Vector3(.28f,.025f,1), white);
            Piece("Rear stripe", body, new Vector3(0,.785f,-1.43f), new Vector3(.28f,.025f,.65f), white);
            Piece("Windscreen pillar L", body, new Vector3(-.73f,1.04f,-.15f), new Vector3(.07f,.49f,.10f), paint);
            Piece("Windscreen pillar R", body, new Vector3(.73f,1.04f,-.15f), new Vector3(.07f,.49f,.10f), paint);
            Piece("Front bumper", body, new Vector3(0,.42f,1.93f), new Vector3(1.65f,.17f,.12f), metal);
            Piece("Grille", body, new Vector3(0,.64f,1.92f), new Vector3(.7f,.18f,.06f), rubber);
            Piece("Back bumper", body, new Vector3(0,.42f,-1.93f), new Vector3(1.65f,.17f,.12f), metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Piece("Headlight", body, new Vector3(side*.65f,.70f,1.91f), new Vector3(.48f,.10f,.08f), lamp);
                Piece("Tail light", body, new Vector3(side*.65f,.70f,-1.91f), new Vector3(.48f,.12f,.08f), tailGlow);
                Piece("Wing mirror", body, new Vector3(side*.99f,1.14f,.3f), new Vector3(.25f,.14f,.28f), paint);
                Piece("Exhaust", body, new Vector3(side*.53f,.31f,-2), new Vector3(.17f,.17f,.25f), rubber);
            }
            var glow = Mat(name + "Underglow", paint.color, .3f, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Piece("Neon sill", body, new Vector3(side*.97f,.29f,0), new Vector3(.055f,.055f,3.1f), glow);
                Piece("Neon bumper", body, new Vector3(0,.33f,side*2.01f), new Vector3(1.7f,.045f,.05f), glow);
            }
            var wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Wheel" + i).transform; pivot.SetParent(body, false);
                pivot.localPosition = new Vector3(i % 2 == 0 ? -.94f : .94f, .40f, i < 2 ? 1.18f : -1.17f);
                var tyre = Piece("Tyre", pivot, Vector3.zero, new Vector3(.73f,.17f,.73f), rubber, PrimitiveType.Cylinder);
                tyre.transform.localRotation = Quaternion.Euler(0,0,90);
                var hub = Piece("Alloy", pivot, new Vector3(i % 2 == 0 ? -.18f : .18f,0,0), new Vector3(.43f,.025f,.43f), metal, PrimitiveType.Cylinder);
                hub.transform.localRotation = Quaternion.Euler(0,0,90); wheels[i] = pivot;
            }
            VisualPolish.Car(body, metal, rubber);
            if (role == 0)
            {
                Piece("Spoiler", body, new Vector3(0,1.18f,-1.65f), new Vector3(1.9f,.10f,.32f), paint);
                var headlight = new GameObject("Headlight beam", typeof(Light)).GetComponent<Light>();
                headlight.transform.SetParent(body, false); headlight.transform.localPosition = new Vector3(0,.65f,1.9f);
                headlight.transform.localRotation = Quaternion.Euler(7,0,0); headlight.type = LightType.Spot;
                headlight.range = 38; headlight.spotAngle = 75; headlight.intensity = 4; headlight.color = new Color(.5f,.8f,1);
                var controller = root.AddComponent<ArcadeCarController>(); controller.body = body; controller.wheels = wheels;
                controller.collisionEffect = Effect(root.transform, "Collision Sparks", new Color(1,.55f,.12f), false);
                controller.boostEffect = Effect(root.transform, "Boost Trail", new Color(.1f,.9f,1), true);
            }
            else if (role == 1) root.AddComponent<RivalDriver>();
            else root.AddComponent<TrafficActor>();
            return SavePrefab(root, name);
        }
        static ParticleSystem Effect(Transform parent, string name, Color color, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0,.45f,-2);
            go.transform.localRotation = Quaternion.Euler(0,180,0);
            var system = go.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.playOnAwake = false; main.loop = loop; main.duration = loop ? 1 : .25f;
            main.startLifetime = .35f; main.startSpeed = loop ? 7 : 4; main.startSize = loop ? .25f : .15f; main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 80;
            var emission = system.emission; emission.rateOverTime = loop ? 35 : 0;
            if (!loop) emission.SetBursts(new [] { new ParticleSystem.Burst(0, 25) });
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = loop ? 10 : 80; shape.radius = .45f;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Particles.mat");
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(mat, Root + "/Materials/Particles.mat"); }
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat; return system;
        }
        static GameObject MakeObstacle()
        {
            var go = new GameObject("RoadworksBarrier");
            Piece("Barrier", go.transform, new Vector3(0,.6f,0), new Vector3(1.8f,1.2f,1.4f), red);
            for (int i = 0; i < 3; i++) Piece("Reflector", go.transform, new Vector3(-.6f+i*.6f,.7f,-.72f), new Vector3(.28f,.5f,.03f), white);
            go.AddComponent<TrafficActor>().obstacle = true;
            return SavePrefab(go, "RoadworksBarrier");
        }
        static GameObject MakeSegment(bool arid)
        {
            var root = new GameObject(arid ? "DesertSegment" : "ValleySegment", typeof(TrackSection));
            for (int slice = 0; slice < 8; slice++)
            {
                float z = slice*5 + 2.5f;
                var road = new GameObject("Road slice " + slice).transform; road.SetParent(root.transform, false); road.localPosition = new Vector3(0,0,z);
                Piece("Asphalt", road, new Vector3(0,-.16f,0), new Vector3(12,.3f,5.2f), asphalt);
                for (int side = -1; side <= 1; side += 2)
                {
                    Piece("Edge paint", road, new Vector3(side*5.75f,.006f,0), new Vector3(.12f,.018f,5.2f), stripe);
                    Piece("Guard rail", road, new Vector3(side*6.6f,.75f,0), new Vector3(.10f,.13f,5.2f), white);
                    Piece("Rail post", road, new Vector3(side*6.6f,.35f,0), new Vector3(.13f,.8f,.15f), metal);
                    Piece("Lane dash", road, new Vector3(side*1.65f,.008f,0), new Vector3(.10f,.02f,2.4f), white);
                }
            }
            Piece("Terrain", root.transform, new Vector3(0,-.6f,20), new Vector3(190,.7f,42), arid ? sand : grass);
            for (int line = 0; line < 4; line++)
            {
                var grid = new GameObject("Neon terrain grid").transform; grid.SetParent(root.transform,false);
                grid.localPosition = new Vector3(0,-.23f,line*10);
                for (int side = -1; side <= 1; side += 2)
                {
                    Piece("Cross grid", grid, new Vector3(side*32,0,0), new Vector3(48,.02f,.045f), arid ? neonWindow : white);
                    Piece("Grid spine", grid, new Vector3(side*18,0,5), new Vector3(.045f,.02f,10), arid ? neonWindow : white);
                }
            }
            for (int i = 0; i < 12; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float x = side*(11+i%4*5); float z = 3+(i*11)%37;
                var item = new GameObject(arid ? "Cactus" : "Pine tree").transform; item.SetParent(root.transform, false); item.localPosition = new Vector3(x,0,z);
                Piece("Trunk", item, new Vector3(0,1.5f,0), new Vector3(.55f,3,.55f), arid ? leaf : bark);
                if (arid)
                {
                    Piece("Arm", item, new Vector3(.8f,1.6f,0), new Vector3(1.5f,.4f,.4f), leaf);
                    Piece("Arm tip", item, new Vector3(1.4f,2,0), new Vector3(.4f,1.1f,.4f), neonWindow);
                    Piece("Pylon beacon", item, new Vector3(0,3.2f,0), new Vector3(.65f,.3f,.65f), neonWindow);
                }
                else
                {
                    MeshPart("Crystal canopy", item, new Vector3(0,1.5f,0), crystalMesh, leaf);
                    Piece("Trunk light", item, new Vector3(0,1.3f,0), new Vector3(.57f,.08f,.57f), white);
                    var top = MeshPart("Crystal crown", item, new Vector3(0,3.2f,0), crystalMesh, leaf); top.transform.localScale = Vector3.one*.7f;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                var house = new GameObject("Roadside building").transform; house.SetParent(root.transform, false); house.localPosition = new Vector3(side*32,0,12+i*18);
                Piece("Walls", house, new Vector3(0,3,0), new Vector3(9,6,9), building);
                Piece("Roof", house, new Vector3(0,6.3f,0), new Vector3(10,.15f,10), arid ? neonWindow : white);
                for (int window = 0; window < 3; window++)
                    Piece("Window", house, new Vector3(-3+window*3,3,-4.55f), new Vector3(1.5f,2,.1f), neonWindow);
                if (arid)
                {
                    Piece("Tower", house, new Vector3(0,11,0), new Vector3(6,10,6), building);
                    for (int floor = 0; floor < 4; floor++)
                        Piece("Tower neon floor", house, new Vector3(0,7.5f+floor*2.4f,-3.05f), new Vector3(5,.12f,.08f), neonWindow);
                }
                Piece("Door", house, new Vector3(0,1.3f,-4.57f), new Vector3(1.4f,2.6f,.12f), bark);
                Piece("Hill", root.transform, new Vector3(side*65,-2,22), new Vector3(30,arid ? 30 : 18,35), arid ? rock : leaf, PrimitiveType.Sphere);
            }
            var sign = new GameObject("Road sign").transform; sign.SetParent(root.transform, false); sign.localPosition = new Vector3(8,0,26);
            Piece("Pole", sign, new Vector3(0,1.8f,0), new Vector3(.14f,3.6f,.14f), metal);
            Piece("Sign", sign, new Vector3(0,3.4f,0), new Vector3(2,1.2f,.15f), teal);
            Piece("Arrow", sign, new Vector3(0,3.4f,-.10f), new Vector3(1,.17f,.06f), white);
            VisualPolish.Road(root.transform, metal, lamp);
            return SavePrefab(root, root.name);
        }
        static GameObject MakeGate()
        {
            var root = new GameObject("FinishGate");
            for (int side = -1; side <= 1; side+=2)
                Piece("Pillar", root.transform, new Vector3(side*6.1f,3.5f,0), new Vector3(.45f,7,.45f), teal);
            Piece("Banner", root.transform, new Vector3(0,6.9f,0), new Vector3(12.6f,1.2f,.35f), rubber);
            for (int i = 0; i < 20; i++)
                Piece("Chequer", root.transform, new Vector3(-5.7f+(i%10)*1.25f,6.65f+(i/10)*.5f,-.19f), new Vector3(.61f,.5f,.04f), i%2 == i/10 ? white : rubber);
            Piece("Finish line", root.transform, new Vector3(0,.025f,0), new Vector3(12,.025f,.8f), stripe);
            return SavePrefab(root, "FinishGate");
        }
        static void MakeScene(string name, bool gameplay)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f,.70f,.78f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.43f,.69f,.77f); RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 520;
            var sun = new GameObject("Sun", typeof(Light)); sun.transform.rotation = Quaternion.Euler(48,-30,0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f; light.color = new Color(.8f,.88f,1); light.shadows = LightShadows.Soft;
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ChaseCamera)); camera.tag = "MainCamera";
            var view = camera.GetComponent<Camera>(); view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = RenderSettings.fogColor; view.farClipPlane = 560; view.nearClipPlane = .1f;
            camera.transform.position = new Vector3(0,7,-13); camera.transform.rotation = Quaternion.Euler(20,0,0);
            NeonSceneStyle.Apply(view, Root);
            var track = new GameObject("Track System", typeof(TrackSystem)).GetComponent<TrackSystem>();
            track.config = config; track.valleySegment = valley; track.desertSegment = desert; track.finishPrefab = gate;
            var canvas = new GameObject("Cars UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CarsUI));
            var ui = canvas.GetComponent<CarsUI>(); ui.BuildLayout();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var manager = new GameObject("Game Session", typeof(GameSession)).GetComponent<GameSession>();
            manager.config = config; manager.track = track; manager.playerPrefab = player.GetComponent<ArcadeCarController>();
            manager.rivalPrefab = rival.GetComponent<RivalDriver>(); manager.trafficPrefab = traffic.GetComponent<TrafficActor>();
            manager.obstaclePrefab = obstacle.GetComponent<TrafficActor>(); manager.chase = camera.GetComponent<ChaseCamera>(); manager.ui = ui;
            manager.gameplayScene = gameplay; ui.session = manager;
            manager.audioEffects = manager.gameObject.AddComponent<GameAudio>(); manager.audioEffects.session = manager;
            AudioAssetBuilder.Assign(manager.audioEffects, Root + "/Audio");
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/" + name + ".unity");
        }
        [MenuItem("Tools/Cars/Validate Generated Game")]
        public static void ValidateGeneratedGame()
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (!File.Exists(scene.path)) throw new InvalidOperationException("Missing scene " + scene.path);
            foreach (string name in new [] { "PlayerCar", "RivalCar", "TrafficCar", "RoadworksBarrier", "ValleySegment", "DesertSegment", "FinishGate" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab") == null)
                    throw new InvalidOperationException("Missing prefab " + name);
            if (GraphicsSettings.defaultRenderPipeline == null) throw new InvalidOperationException("URP is missing");
            Debug.Log("CARS_VALIDATION_OK");
        }
        [MenuItem("Tools/Cars/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            ValidateGeneratedGame();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
                "Builds/Windows/CarsVelocity.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Directory.CreateDirectory("Builds/Windows/Licenses");
            if (File.Exists("Docs/FIREBASE_SDK_LICENSE.txt"))
                File.Copy("Docs/FIREBASE_SDK_LICENSE.txt", "Builds/Windows/Licenses/Firebase.txt", true);
            Debug.Log("CARS_WINDOWS_BUILD_OK");
        }
    }
}
#endif

