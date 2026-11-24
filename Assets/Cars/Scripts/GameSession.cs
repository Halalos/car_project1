using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cars
{
    public sealed class GameSession : MonoBehaviour
    {
        public static string SaveDirectoryOverride;
        public static Action ExitOverride;
        public bool IsQuitting { get; private set; }
        bool quitPrepared;
        public GameAudio audioEffects;
        public GameConfig config;
        public TrackSystem track;
        public ArcadeCarController playerPrefab;
        public RivalDriver rivalPrefab;
        public TrafficActor trafficPrefab;
        public TrafficActor obstaclePrefab;
        public ChaseCamera chase;
        public CarsUI ui;
        public bool gameplayScene;
        public GameMode mode = GameMode.EndlessSurvival;
        public EnvironmentKind environment;
        public int level;
        public SessionState State { get; private set; } = SessionState.Ready;
        public SaveCoordinator Saves { get; private set; }
        public ArcadeCarController Player { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining { get; private set; }
        public int Score { get; private set; }
        public int Position { get; private set; } = 1;
        public RunResult Result { get; private set; }
        readonly List<RivalDriver> rivals = new List<RivalDriver>();
        readonly List<TrafficActor> traffic = new List<TrafficActor>();
        float checkpointAt;
        float farthestTraffic;
        int sequence;
        int passedScore;
        string runId;
        bool finishing;
        bool savingPurchase;
        static GameMode chosenMode;
        static EnvironmentKind chosenEnvironment;
        static int chosenLevel;
        static bool launchRequested;
        LevelDefinition Level { get { return config.levels[Mathf.Clamp(level, 0, config.levels.Length - 1)]; } }

        void Awake() { Application.wantsToQuit += WantsToQuit; }
        bool WantsToQuit() { if (quitPrepared) return true; Quit(); return false; }
        async void Start()
        {
            Time.timeScale = 1;
            Application.targetFrameRate = 60;
            if (config == null || track == null || playerPrefab == null || chase == null || ui == null)
            { Debug.LogError("Cars scene is incomplete. Run Tools > Cars > Build Complete Game."); enabled = false; return; }
            Saves = new SaveCoordinator(SaveDirectoryOverride, config.enableFirebase && SaveDirectoryOverride == null);
            ui.session = this;
            ui.Show("Loading");
            await Saves.InitializeAsync();
            if (this == null) return;
            if (gameplayScene && launchRequested)
            {
                mode = chosenMode; environment = chosenEnvironment; level = chosenLevel;
                launchRequested = false; Begin();
            }
            else
            {
                track.Begin(environment, Level.length, true, false);
                Player = Instantiate(playerPrefab);
                Player.Begin(track, config.vehicle.WithUpgrades(Saves.Profile));
                chase.target = Player; chase.menu = true; chase.Snap(); ui.Show("Main");
            }
        }
        public void Launch()
        {
            if (Saves == null || !Saves.Ready || Saves.CloudBusy || savingPurchase || IsQuitting) return;
            chosenMode = mode; chosenEnvironment = environment; chosenLevel = level; launchRequested = true;
            SceneManager.LoadScene("Gameplay");
        }
        public void Begin()
        {
            if (Saves == null || Saves.CloudBusy || finishing || savingPurchase || IsQuitting) return;
            Time.timeScale = 1;
            if (Player != null) Destroy(Player.gameObject);
            foreach (var rival in rivals) Destroy(rival.gameObject);
            foreach (var actor in traffic) Destroy(actor.gameObject);
            rivals.Clear(); traffic.Clear();
            track.Begin(environment, Level.length, mode == GameMode.EndlessSurvival, mode == GameMode.TimeAttack);
            Player = Instantiate(playerPrefab);
            Player.Begin(track, config.vehicle.WithUpgrades(Saves.Profile));
            chase.target = Player; chase.menu = false; chase.Snap();
            Elapsed = 0; Remaining = Level.timeLimit; Score = 0; passedScore = 0; Position = 1;
            checkpointAt = 500; sequence = 0; farthestTraffic = 100; runId = Guid.NewGuid().ToString("N"); Result = null;
            if (mode == GameMode.Race)
            {
                for (int i = 0; i < config.rivalCount; i++)
                {
                    var rival = Instantiate(rivalPrefab);
                    rival.Begin(track, 8 + i * 7, i % 3, Level.aiSpeed + (i % 3) * .6f, i % 2 == 1);
                    rivals.Add(rival);
                }
            }
            for (int i = 0; i < 12; i++)
            {
                var actor = Instantiate(i % 4 == 3 ? obstaclePrefab : trafficPrefab);
                farthestTraffic += Level.trafficSpacing;
                actor.Place(track, farthestTraffic, sequence++); traffic.Add(actor);
            }
            State = SessionState.Running; ui.Show("HUD");
            if (audioEffects != null) { audioEffects.Observe(Player); audioEffects.Play(SoundCue.Start); }
        }
        void Update()
        {
            if (Saves == null || !Saves.Ready || Player == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == SessionState.Running) Pause();
                else if (State == SessionState.Paused) Resume();
            }
            if (State != SessionState.Running || IsQuitting) return;
            float dt = Time.deltaTime;
            Elapsed += dt;
            if (mode == GameMode.TimeAttack) Remaining -= dt;
            float before = Player.Distance;
            float input = Input.GetAxisRaw("Horizontal");
            Player.Tick(dt, input, Input.GetKey(KeyCode.Space));
            foreach (var actor in traffic)
            {
                actor.Tick(dt);
                if (!actor.Scored && actor.Distance < Player.Distance - 5)
                { actor.Scored = true; passedScore += actor.obstacle ? 15 : 50; if (audioEffects != null) audioEffects.Play(SoundCue.Pass); }
                if (actor.Distance < Player.Distance - 65)
                {
                    farthestTraffic = Mathf.Max(farthestTraffic, Player.Distance + 300) + Level.trafficSpacing;
                    actor.Place(track, farthestTraffic, sequence++);
                }
                if (Mathf.Abs(actor.Distance - Player.Distance) < 3.8f && Mathf.Abs(actor.Offset - Player.Offset) < 1.7f)
                    Player.Hit(actor.obstacle ? 34 : 25);
            }
            foreach (var rival in rivals)
            {
                rival.Tick(dt, Elapsed, Level.length, rivals, traffic, Player);
                if (Mathf.Abs(rival.Distance - Player.Distance) < 3.8f && Mathf.Abs(rival.Offset - Player.Offset) < 1.7f)
                    Player.Hit(12);
            }
            Position = 1;
            foreach (var rival in rivals) if (rival.Distance > Player.Distance || rival.FinishTime >= 0) Position++;
            Score = Mathf.FloorToInt(Player.Distance) + passedScore;
            if (mode == GameMode.TimeAttack && Player.Distance >= checkpointAt && checkpointAt < Level.length)
            { if (audioEffects != null) audioEffects.Play(SoundCue.Checkpoint); Remaining += 5; checkpointAt += 500; track.SetCheckpoint(checkpointAt); ui.Notify("CHECKPOINT  +5 SECONDS"); }
            float oldOrigin = track.Origin;
            track.Tick(Player.Distance);
            chase.transform.position -= Vector3.forward * (track.Origin - oldOrigin);
            Player.RenderPosition();
            foreach (var rival in rivals) rival.RenderPosition();
            foreach (var actor in traffic) actor.RenderPosition();
            if (mode != GameMode.EndlessSurvival && Player.Distance >= Level.length)
            {
                float crossing = Player.Speed > 0 ? (Level.length - before) / Player.Speed : 0;
                float finishTime = Elapsed - dt + Mathf.Clamp(crossing, 0, dt);
                Position = 1;
                foreach (var rival in rivals) if (rival.FinishTime >= 0 && rival.FinishTime <= finishTime) Position++;
                float finishRemaining = Remaining + (Elapsed - finishTime);
                Elapsed = finishTime;
                Complete(RaceRules.IsWin(mode, true, Position, finishRemaining), "Finish line reached");
            }
            else if (Player.Integrity <= 0) Complete(false, "Car disabled after collisions");
            else if (mode == GameMode.TimeAttack && Remaining <= 0) Complete(false, "Time expired");
        }
        async void Complete(bool won, string reason)
        {
            if (State == SessionState.Finished || finishing) return;
            State = SessionState.Finished; finishing = true;
            Result = new RunResult {
                runId = runId, playerId = Saves.Profile.playerId, finishedUtc = DateTime.UtcNow.ToString("O"),
                mode = mode, environment = environment, level = level, score = Score,
                distance = Player.Distance, elapsed = Elapsed, position = Position, won = won,
                earnedCoins = RaceRules.Reward(Score, won)
            };
            Saves.Profile.Apply(Result);
            ui.ResultReason = reason;
            ui.Show("Result");
            if (audioEffects != null) audioEffects.Play(won ? SoundCue.Victory : SoundCue.Defeat);
            try { await Saves.SaveProgressAsync(); }
            finally { finishing = false; }
        }
        public void Pause() { State = SessionState.Paused; Time.timeScale = 0; ui.Show("Pause"); }
        public void Resume() { Time.timeScale = 1; State = SessionState.Running; ui.Show("HUD"); }
        public void MainMenu()
        {
            if (finishing || savingPurchase || Saves.CloudBusy || IsQuitting)
            { ui.Notify("Please wait for saving to finish"); return; }
            Time.timeScale = 1; SceneManager.LoadScene("MainMenu");
        }
        public async void Purchase(UpgradeKind kind)
        {
            if (finishing || savingPurchase || Saves.CloudBusy || IsQuitting) return;
            if (!Saves.Profile.Purchase(kind, config)) { if (audioEffects != null) audioEffects.Play(SoundCue.Denied); return; }
            if (audioEffects != null) audioEffects.Play(SoundCue.Upgrade);
            savingPurchase = true;
            try { await Saves.SaveProgressAsync(); }
            finally { savingPurchase = false; }
        }
        public async void CloudSync(bool restore)
        {
            if (State == SessionState.Running || State == SessionState.Paused ||
                finishing || savingPurchase || IsQuitting) return;
            await Saves.SyncAsync(restore);
        }
        public async void Quit()
        {
            if (IsQuitting) return;
            if (finishing || savingPurchase || (Saves != null && Saves.CloudBusy))
            {
                if (ui != null) ui.Notify("Please wait for saving to finish");
                return;
            }
            IsQuitting = true;
            if (State == SessionState.Running) Pause();
            if (ui != null) ui.Notify("Saving before exit...");
            if (Saves != null && Saves.Ready)
            {
                await Saves.SaveLocalAsync();
                if (!Saves.LastLocalSaveSucceeded)
                {
                    IsQuitting = false;
                    if (ui != null) ui.Notify("Could not save progress. Please try quitting again.");
                    return;
                }
            }
            Time.timeScale = 1;
            PlayerPrefs.Save();
            quitPrepared = true;
            Debug.Log("CARS_QUIT_READY: local save completed; exiting.");
            if (ExitOverride != null) { ExitOverride(); return; }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnApplicationFocus(bool focus) { if (!focus && State == SessionState.Running) Pause(); }
        void OnDestroy() { Application.wantsToQuit -= WantsToQuit; Time.timeScale = 1; }
    }
}
