using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cars.Tests
{
    public sealed class RuntimeFlowTests
    {
        string testFolder;
        GameSession session;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            testFolder = Path.Combine(Application.temporaryCachePath, "CarsRuntimeTest-" + Guid.NewGuid().ToString("N"));
            GameSession.SaveDirectoryOverride = testFolder;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            float deadline = Time.realtimeSinceStartup + 15;
            do { session = UnityEngine.Object.FindObjectOfType<GameSession>(); yield return null; }
            while ((session == null || session.Saves == null || !session.Saves.Ready || session.Player == null) && Time.realtimeSinceStartup < deadline);
            Assert.IsNotNull(session); Assert.IsNotNull(session.Player); Assert.IsTrue(session.Saves.Ready);
            // Clone configuration so automated scenario tuning never changes a project asset.
            session.config = UnityEngine.Object.Instantiate(session.config); session.config.enableFirebase = false;
            session.track.config = session.config;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            GameSession.ExitOverride = null;
            yield return new WaitForSecondsRealtime(.25f);
            if (session != null)
            {
                var temporaryConfig = session.config;
                UnityEngine.Object.Destroy(session.gameObject);
                UnityEngine.Object.Destroy(temporaryConfig);
            }
            yield return null;
            GameSession.SaveDirectoryOverride = null;
            if (Directory.Exists(testFolder)) Directory.Delete(testFolder, true);
        }
        [UnityTest]
        public IEnumerator MenuSelectionAndAllModesShareWorkingRuntime()
        {
            var selectButton = session.ui.transform.Find("Screen_Main/Select").GetComponent<Button>();
            selectButton.onClick.Invoke(); Assert.IsTrue(session.ui.transform.Find("Screen_Select").gameObject.activeSelf);
            yield return Capture("mode-selection");
            foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
            {
                session.mode = mode; session.environment = mode == GameMode.Race ? EnvironmentKind.SunsetDesert : EnvironmentKind.GreenValley;
                session.Begin(); yield return new WaitForSeconds(1f);
                Assert.That(session.Player.Speed, Is.GreaterThan(0)); Assert.That(session.Player.Distance, Is.GreaterThan(0));
                yield return Capture(mode.ToString());
                Assert.That(session.track.GetComponentsInChildren<TrackSection>().Length, Is.EqualTo(session.config.pooledSegments));
                Assert.That(UnityEngine.Object.FindObjectsOfType<RivalDriver>().Length, Is.EqualTo(mode == GameMode.Race ? 5 : 0));
                session.Pause(); float distance = session.Player.Distance;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(session.Player.Distance, Is.EqualTo(distance));
                session.Resume(); yield return new WaitForSeconds(.1f);
                Assert.That(session.Player.Distance, Is.GreaterThan(distance));
            }
            session.Begin(); Assert.That(session.Player.Distance, Is.EqualTo(0));
            yield return null;
        }
        [UnityTest]
        public IEnumerator RealRaceFinishCanProduceVictoryAndDefeat()
        {
            session.mode = GameMode.Race; session.config.levels[0].length = 5;
            session.Begin();
            foreach (var rival in UnityEngine.Object.FindObjectsOfType<RivalDriver>()) rival.Begin(session.track, -100, 2, 31, false);
            session.Player.Tick(1, 0, false); yield return null;
            Assert.That(session.State, Is.EqualTo(SessionState.Finished)); Assert.IsTrue(session.Result.won);
            yield return WaitForLocalSave();
            session.Begin(); yield return null;
            session.Player.Tick(1, 0, false); yield return null;
            Assert.That(session.State, Is.EqualTo(SessionState.Finished)); Assert.IsFalse(session.Result.won);
            Assert.That(session.Result.position, Is.GreaterThan(1));
            yield return WaitForLocalSave();
        }
        [UnityTest]
        public IEnumerator TimeAttackAndSurvivalUseActualFailureConditions()
        {
            session.mode = GameMode.TimeAttack; session.config.levels[0].length = 5; session.config.levels[0].timeLimit = 30;
            session.Begin(); session.Player.Tick(1, 0, false); yield return null;
            Assert.IsTrue(session.Result.won); yield return WaitForLocalSave();
            session.config.levels[0].timeLimit = .001f; session.Begin(); yield return null;
            Assert.IsFalse(session.Result.won); Assert.That(session.ui.ResultReason, Is.EqualTo("Time expired"));
            yield return WaitForLocalSave();
            session.mode = GameMode.EndlessSurvival; session.Begin(); session.Player.Hit(100); yield return null;
            Assert.IsFalse(session.Result.won); Assert.That(session.Player.Integrity, Is.EqualTo(0));
            yield return WaitForLocalSave();
        }
        [UnityTest]
        public IEnumerator AudioTracksDrivingPausesAndCollisionEvents()
        {
            var audio = session.audioEffects;
            Assert.IsNotNull(audio); Assert.IsNotNull(audio.engineClip); Assert.IsNotNull(audio.victoryClip);
            float originalVolume = audio.MasterVolume;
            audio.SetVolume(.7f, false);
            session.Begin(); yield return new WaitForSeconds(.2f);
            float lowPitch = audio.EngineSource.pitch;
            session.Player.Tick(2,0,true); audio.RefreshMix();
            Assert.That(audio.EngineSource.pitch, Is.GreaterThan(lowPitch));
            Assert.That(audio.BoostSource.volume, Is.GreaterThan(0));
            Assert.IsTrue(session.Player.Hit(20)); Assert.That(audio.LastCue, Is.EqualTo(SoundCue.Impact));
            Assert.IsFalse(session.Player.Hit(20));
            session.Pause(); yield return null;
            Assert.That(audio.EngineSource.volume, Is.EqualTo(0)); Assert.That(audio.BoostSource.volume, Is.EqualTo(0));
            audio.SetVolume(0, false); audio.Play(SoundCue.Checkpoint);
            Assert.That(audio.EffectsSource.volume, Is.EqualTo(0));
            audio.SetVolume(originalVolume, false);
        }
        [UnityTest]
        public IEnumerator QuitButtonSavesBeforeRequestingPlatformExit()
        {
            bool exited = false;
            GameSession.ExitOverride = () => exited = true;
            session.Saves.Profile.coins = 417;
            session.ui.transform.Find("Screen_Main/Quit").GetComponent<Button>().onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 5;
            while (!exited && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(exited); Assert.IsTrue(session.IsQuitting); Assert.That(Time.timeScale, Is.EqualTo(1));
            string path = Path.Combine(testFolder,"cars-profile.json"); Assert.IsTrue(File.Exists(path));
            Assert.That(JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path)).coins, Is.EqualTo(417));
        }
        IEnumerator Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var camera = Camera.main;
            var canvas = session.ui.GetComponent<Canvas>();
            var previousMode = canvas.renderMode;
            var renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            renderTexture.Create();
            camera.targetTexture = renderTexture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            yield return null;
            yield return null;
            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Artifacts"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            canvas.renderMode = previousMode;
            UnityEngine.Object.Destroy(image);
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
        }
        IEnumerator WaitForLocalSave()
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (!File.Exists(Path.Combine(testFolder, "cars-profile.json")) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForSecondsRealtime(.15f);
        }
    }
}

