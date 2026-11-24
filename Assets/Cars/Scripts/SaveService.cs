using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cars
{
    public interface ISaveService
    {
        string Status { get; }
        Task InitializeAsync();
        Task<PlayerProfile> LoadProfileAsync();
        Task SaveProfileAsync(PlayerProfile profile);
        Task SaveResultAsync(RunResult result);
    }

    // Local writes are atomic; the previous complete file is retained as a recovery copy.
    public sealed class LocalSaveService : ISaveService
    {
        readonly string path;
        readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        public string Status { get; private set; } = "Local save ready";
        public LocalSaveService(string folder) { path = Path.Combine(folder, "cars-profile.json"); }
        public Task InitializeAsync() { return Task.CompletedTask; }
        public async Task<PlayerProfile> LoadProfileAsync()
        {
            await gate.WaitAsync();
            try
            {
                foreach (string candidate in new [] { path, path + ".bak" })
                {
                    if (!File.Exists(candidate)) continue;
                    try
                    {
                        string json = await Task.Run(() => File.ReadAllText(candidate));
                        var value = JsonUtility.FromJson<PlayerProfile>(json);
                        if (value == null || value.schemaVersion != 1 || string.IsNullOrEmpty(value.playerId) ||
                            value.best == null || value.pending == null || value.rewardedRuns == null || value.coins < 0)
                            throw new InvalidDataException("Invalid profile");
                        value.speedLevel = Mathf.Clamp(value.speedLevel, 0, 5);
                        value.accelerationLevel = Mathf.Clamp(value.accelerationLevel, 0, 5);
                        value.handlingLevel = Mathf.Clamp(value.handlingLevel, 0, 5);
                        Status = candidate.EndsWith(".bak") ? "Recovered backup save" : "Local profile loaded";
                        return value;
                    }
                    catch (Exception e) { Status = "Save recovery: " + e.Message; }
                }
                return new PlayerProfile();
            }
            finally { gate.Release(); }
        }
        public async Task SaveProfileAsync(PlayerProfile profile)
        {
            string json = JsonUtility.ToJson(profile, true);
            await gate.WaitAsync();
            try
            {
                await Task.Run(() => {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path + ".tmp", json);
                    if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                    else File.Move(path + ".tmp", path);
                });
                Status = "Saved locally";
            }
            finally { gate.Release(); }
        }
        public Task SaveResultAsync(RunResult result) { return Task.CompletedTask; }
    }

    public sealed class FirebaseService : ISaveService
    {
        public string Status { get; private set; } = "Cloud not configured - local saves active";
#if CARS_FIREBASE
        static async Task AwaitNetworkAsync(Task operation)
        {
            if (await Task.WhenAny(operation, Task.Delay(20000)) != operation)
            {
                _ = operation.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("Firebase request exceeded 20 seconds. Retry sync when online.");
            }
            await operation;
        }
        static async Task<T> AwaitNetworkAsync<T>(Task<T> operation)
        { await AwaitNetworkAsync((Task)operation); return await operation; }
        Firebase.Auth.FirebaseAuth auth;
        Firebase.Firestore.FirebaseFirestore database;
        Task signIn;
        Task connection;
        public Task InitializeAsync()
        {
            if (connection == null || connection.IsFaulted || connection.IsCanceled)
                connection = ConnectAsync();
            return connection;
        }
        async Task ConnectAsync()
        {
            var dependency = await AwaitNetworkAsync(Firebase.FirebaseApp.CheckAndFixDependenciesAsync());
            if (dependency != Firebase.DependencyStatus.Available)
                throw new InvalidOperationException("Firebase dependencies: " + dependency);
            auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser == null)
            {
                if (signIn == null || signIn.IsCompleted) signIn = auth.SignInAnonymouslyAsync();
                await AwaitNetworkAsync(signIn);
            }
            database = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
            Status = "Cloud connected (save not yet verified)";
        }
        string Uid { get { return auth.CurrentUser.UserId; } }
        Firebase.Firestore.DocumentReference ProfileDocument { get { return database.Collection("players").Document(Uid); } }
        public async Task<PlayerProfile> LoadProfileAsync()
        {
            var snapshot = await AwaitNetworkAsync(ProfileDocument.GetSnapshotAsync(Firebase.Firestore.Source.Server));
            if (snapshot.Metadata.HasPendingWrites)
                throw new IOException("Cloud profile still has unconfirmed writes");
            if (!snapshot.Exists) return null;
            var profile = JsonUtility.FromJson<PlayerProfile>(snapshot.GetValue<string>("profileJson"));
            if (profile == null || profile.schemaVersion != 1 || profile.best == null ||
                profile.pending == null || profile.rewardedRuns == null || profile.coins < 0 ||
                profile.speedLevel < 0 || profile.speedLevel > 5 ||
                profile.accelerationLevel < 0 || profile.accelerationLevel > 5 ||
                profile.handlingLevel < 0 || profile.handlingLevel > 5)
                throw new InvalidDataException("Invalid cloud profile");
            if (snapshot.GetValue<string>("playerId") != Uid ||
                snapshot.GetValue<long>("coins") != profile.coins ||
                (snapshot.ContainsField("upgrades") && !UpgradesMatch(snapshot, profile)))
                throw new InvalidDataException("Cloud profile fields disagree");
            profile.playerId = Uid;
            return profile;
        }
        static bool UpgradesMatch(Firebase.Firestore.DocumentSnapshot document, PlayerProfile profile)
        {
            return document.GetValue<long>("upgrades.speed") == profile.speedLevel &&
                document.GetValue<long>("upgrades.acceleration") == profile.accelerationLevel &&
                document.GetValue<long>("upgrades.handling") == profile.handlingLevel;
        }
        public async Task SaveProfileAsync(PlayerProfile profile)
        {
            var saved = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            saved.playerId = Uid;
            saved.pending.Clear();
            string json = JsonUtility.ToJson(saved);
            await AwaitNetworkAsync(ProfileDocument.SetAsync(new Dictionary<string, object> {
                { "playerId", Uid }, { "coins", saved.coins },
                { "upgrades", new Dictionary<string, object> {
                    { "speed", saved.speedLevel }, { "acceleration", saved.accelerationLevel },
                    { "handling", saved.handlingLevel }
                } },
                { "profileJson", json },
                { "updatedAt", Firebase.Firestore.FieldValue.ServerTimestamp }
            }));
            // Read back from the server; a cached write alone is not verification.
            var verified = await AwaitNetworkAsync(ProfileDocument.GetSnapshotAsync(Firebase.Firestore.Source.Server));
            if (!verified.Exists || verified.Metadata.HasPendingWrites ||
                verified.GetValue<string>("playerId") != Uid || !verified.ContainsField("updatedAt") ||
                verified.GetValue<string>("profileJson") != json ||
                verified.GetValue<long>("coins") != saved.coins ||
                !UpgradesMatch(verified, saved))
                throw new IOException("Cloud read-back verification failed");
            Status = "Cloud profile saved and read back";
        }
        public async Task SaveResultAsync(RunResult result)
        {
            var reference = ProfileDocument.Collection("results").Document(result.runId);
            await AwaitNetworkAsync(reference.SetAsync(new Dictionary<string, object> {
                { "playerId", Uid }, { "score", result.score }, { "finishedUtc", result.finishedUtc },
                { "createdAt", Firebase.Firestore.FieldValue.ServerTimestamp },
                { "mode", result.mode.ToString() }, { "track", result.environment.ToString() },
                { "level", result.level }, { "distance", result.distance }, { "elapsed", result.elapsed },
                { "position", result.position }, { "won", result.won }, { "earnedCoins", result.earnedCoins }
            }));
            var check = await AwaitNetworkAsync(reference.GetSnapshotAsync(Firebase.Firestore.Source.Server));
            if (!check.Exists || check.Metadata.HasPendingWrites ||
                check.GetValue<string>("playerId") != Uid ||
                check.GetValue<long>("score") != result.score ||
                check.GetValue<string>("finishedUtc") != result.finishedUtc ||
                check.GetValue<string>("mode") != result.mode.ToString() ||
                check.GetValue<string>("track") != result.environment.ToString() ||
                check.GetValue<long>("level") != result.level ||
                check.GetValue<double>("distance") != result.distance ||
                check.GetValue<double>("elapsed") != result.elapsed ||
                check.GetValue<long>("position") != result.position ||
                check.GetValue<bool>("won") != result.won ||
                check.GetValue<long>("earnedCoins") != result.earnedCoins ||
                !check.ContainsField("createdAt"))
                throw new IOException("Result read-back verification failed");
            Status = "Cloud result saved and read back";
        }
#else
        public Task InitializeAsync() { throw new InvalidOperationException(Status); }
        public Task<PlayerProfile> LoadProfileAsync() { return Task.FromResult<PlayerProfile>(null); }
        public Task SaveProfileAsync(PlayerProfile profile) { throw new InvalidOperationException(Status); }
        public Task SaveResultAsync(RunResult result) { throw new InvalidOperationException(Status); }
#endif
    }

    public sealed class SaveCoordinator
    {
        readonly LocalSaveService local;
        readonly bool enableCloud;
        static readonly FirebaseService defaultCloud = new FirebaseService();
        readonly ISaveService cloud;
        public SaveCoordinator(string directory = null, bool enableFirebase = false, ISaveService cloudService = null)
        {
            local = new LocalSaveService(directory ?? Application.persistentDataPath);
            enableCloud = enableFirebase;
            cloud = cloudService ?? defaultCloud;
        }
        readonly SemaphoreSlim cloudGate = new SemaphoreSlim(1, 1);
        public PlayerProfile Profile { get; private set; }
        public string Status { get; private set; } = "Loading local profile...";
        public bool Ready { get; private set; }
        public bool CloudBusy { get; private set; }
        public bool LastLocalSaveSucceeded { get; private set; } = true;
        Task cloudConnection;
        public async Task InitializeAsync()
        {
            try { Profile = await local.LoadProfileAsync(); Status = local.Status; }
            catch (Exception e) { Profile = new PlayerProfile(); Status = "Local load failed: " + e.Message; }
            Ready = true;
            if (enableCloud) _ = ConnectInBackgroundAsync();
        }
        async Task ConnectInBackgroundAsync()
        {
            Status = "Connecting to cloud...";
            try
            {
                await ConnectCloudAsync();
                if (!CloudBusy) Status = "Cloud sign-in ready";
            }
            catch (Exception e)
            {
                if (!CloudBusy) Status = "Cloud unavailable; local progress retained. " + e.Message;
            }
        }
        async Task ConnectCloudAsync()
        {
            if (cloudConnection == null || cloudConnection.IsFaulted || cloudConnection.IsCanceled)
                cloudConnection = cloud.InitializeAsync();
            await cloudConnection;
        }
        public async Task SaveProgressAsync()
        {
            await SaveLocalAsync();
            if (enableCloud && LastLocalSaveSucceeded) await SyncAsync();
        }
        public async Task SaveLocalAsync()
        {
            try { await local.SaveProfileAsync(Profile); LastLocalSaveSucceeded = true; Status = local.Status; }
            catch (Exception e) { LastLocalSaveSucceeded = false; Status = "Local save failed: " + e.Message; }
        }
        public async Task SyncAsync(bool restoreCloud = false)
        {
            if (!enableCloud) { Status = "Cloud saves unavailable. Progress stays on this device."; return; }
            if (!Ready || !await cloudGate.WaitAsync(0)) return;
            CloudBusy = true;
            try
            {
                Status = "Connecting to cloud...";
                await ConnectCloudAsync();
                var candidate = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(Profile));
                if (restoreCloud)
                {
                    var remote = await cloud.LoadProfileAsync();
                    if (remote == null)
                    {
                        Status = "No cloud profile found. Local progress retained.";
                        return;
                    }
                    remote.pending.Clear();
                    foreach (var result in candidate.pending)
                    {
                        if (!remote.Apply(result)) remote.pending.Add(result);
                    }
                    candidate = remote;
                }
                foreach (var result in candidate.pending) await cloud.SaveResultAsync(result);
                await cloud.SaveProfileAsync(candidate);
                candidate.pending.Clear();
                await local.SaveProfileAsync(candidate);
                Profile = candidate;
                Status = restoreCloud ? "Cloud profile restored" : cloud.Status;
            }
            catch (Exception e) { Status = "Cloud unavailable; local progress retained. " + e.Message; }
            finally { CloudBusy = false; cloudGate.Release(); }
        }
    }
}
