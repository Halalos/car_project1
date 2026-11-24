using System;
using System.Collections;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Cars.Tests
{
    public sealed class GameRulesTests
    {
        GameConfig config;
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        T Own<T>(T item) where T : UnityEngine.Object { owned.Add(item); return item; }
        TrackSystem Road()
        {
            var road = Own(new GameObject("Test road")).AddComponent<TrackSystem>(); road.config = config; return road;
        }
        ArcadeCarController Car(TrackSystem track)
        {
            var car = Own(new GameObject("Test car")).AddComponent<ArcadeCarController>();
            car.Begin(track, config.vehicle); return car;
        }
        [SetUp] public void Setup() { config = Own(ScriptableObject.CreateInstance<GameConfig>()); }
        [TearDown] public void Cleanup()
        { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]); owned.Clear(); }
        [TestCase(0, 0)] [TestCase(10, 36)] [TestCase(38, 136.8f)]
        public void SpeedDisplayUsesMetresPerSecond(float metres, float kmh)
        { Assert.That(RaceRules.Kmh(metres), Is.EqualTo(kmh).Within(.001f)); }
        [Test]
        public void SteeringRespondsWithinOneFrameAndRemainsOnRoad()
        {
            var car = Car(Road()); car.Tick(1f/60, 1, false);
            Assert.That(car.Offset, Is.GreaterThan(0));
            for (int i = 0; i < 3600; i++) car.Tick(1f/60, 1, false);
            Assert.That(Mathf.Abs(car.Offset), Is.LessThanOrEqualTo(config.roadHalfWidth - 1));
            Assert.That(car.Distance, Is.GreaterThan(2000));
        }
        [Test]
        public void MovementIsConsistentAcrossFrameRates()
        {
            var track = Road(); var slow = Car(track); var fast = Car(track);
            for (int i = 0; i < 900; i++) slow.Tick(1f/30, 0, false);
            for (int i = 0; i < 3600; i++) fast.Tick(1f/120, 0, false);
            Assert.That(slow.Distance, Is.EqualTo(fast.Distance).Within(1));
        }
        [Test]
        public void CollisionPenalizesSpeedAndHasBriefImmunity()
        {
            var car = Car(Road()); for (int i = 0; i < 600; i++) car.Tick(1f/60, 0, false);
            float speed = car.Speed;
            Assert.IsTrue(car.Hit(25)); Assert.IsFalse(car.Hit(25));
            Assert.That(car.Speed, Is.LessThan(speed * .5f)); Assert.That(car.Integrity, Is.EqualTo(75));
            for (int i = 0; i < 70; i++) car.Tick(1f/60, 0, false);
            Assert.IsTrue(car.Hit(25));
        }
        [Test]
        public void BoostIsTemporaryAndRecharges()
        {
            var car = Car(Road()); for (int i = 0; i < 300; i++) car.Tick(1f/60, 0, false);
            for (int i = 0; i < 180; i++) car.Tick(1f/60, 0, true);
            Assert.That(car.Speed, Is.GreaterThan(config.vehicle.topSpeed));
            Assert.That(car.Boost, Is.LessThan(.1f));
            for (int i = 0; i < 600; i++) car.Tick(1f/60, 0, false);
            Assert.That(car.Boost, Is.EqualTo(1).Within(.01f));
        }
        [Test]
        public void AllUpgradesChangeDrivingPropertiesAndRespectBudgetAndCap()
        {
            var profile = new PlayerProfile { coins = 10000 };
            foreach (UpgradeKind kind in Enum.GetValues(typeof(UpgradeKind))) Assert.IsTrue(profile.Purchase(kind, config));
            var improved = config.vehicle.WithUpgrades(profile);
            Assert.That(improved.topSpeed, Is.GreaterThan(config.vehicle.topSpeed));
            Assert.That(improved.acceleration, Is.GreaterThan(config.vehicle.acceleration));
            Assert.That(improved.handling, Is.GreaterThan(config.vehicle.handling));
            Assert.That(profile.coins, Is.EqualTo(9550));
            profile.coins = 0; Assert.IsFalse(profile.Purchase(UpgradeKind.Speed, config));
            profile.coins = 10000; profile.speedLevel = 5; Assert.IsFalse(profile.Purchase(UpgradeKind.Speed, config));
        }
        [Test]
        public void RunRewardIsIdempotentAndBestRecordsRemainIndependent()
        {
            var profile = new PlayerProfile();
            var result = new RunResult { runId = "one", mode = GameMode.Race, score = 1000, won = true, elapsed = 50, earnedCoins = 140 };
            Assert.IsTrue(profile.Apply(result)); Assert.IsFalse(profile.Apply(result));
            Assert.That(profile.coins, Is.EqualTo(140)); Assert.That(profile.pending.Count, Is.EqualTo(1));
            profile.Apply(new RunResult { runId = "two", mode = GameMode.Race, score = 500, won = true, elapsed = 45 });
            Assert.That(profile.best[0].score, Is.EqualTo(1000)); Assert.That(profile.best[0].time, Is.EqualTo(45));
            profile.Apply(new RunResult { runId = "three", mode = GameMode.TimeAttack, score = 100 });
            Assert.That(profile.best.Count, Is.EqualTo(2));
        }
        [Test]
        public void RaceAndTimeAttackCanBothBeWonAndLost()
        {
            Assert.IsTrue(RaceRules.IsWin(GameMode.Race, true, 1, 0));
            Assert.IsFalse(RaceRules.IsWin(GameMode.Race, true, 2, 0));
            Assert.IsFalse(RaceRules.IsWin(GameMode.Race, false, 1, 0));
            Assert.IsTrue(RaceRules.IsWin(GameMode.TimeAttack, true, 1, .01f));
            Assert.IsFalse(RaceRules.IsWin(GameMode.TimeAttack, true, 1, -.01f));
            Assert.That(RaceRules.Position(100, new [] {90f, 110f, 120f, 80f, 99f}), Is.EqualTo(3));
        }
        [Test]
        public void RivalsAvoidBlockedLaneAndBehavioursHaveDifferentPace()
        {
            var road = Road(); var player = Car(road);
            var cautious = Own(new GameObject("Cautious")).AddComponent<RivalDriver>();
            var aggressive = Own(new GameObject("Aggressive")).AddComponent<RivalDriver>();
            cautious.Begin(road, 10, 1, 32, false); aggressive.Begin(road, 10, 2, 32, true);
            var barrier = Own(new GameObject("Barrier")).AddComponent<TrafficActor>(); barrier.obstacle = true; barrier.Place(road, 25, 1);
            var rivals = new List<RivalDriver> { cautious, aggressive }; var traffic = new List<TrafficActor> { barrier };
            for (int i = 0; i < 60; i++)
            { cautious.Tick(1f/60, i/60f, 2000, rivals, traffic, player); aggressive.Tick(1f/60, i/60f, 2000, rivals, traffic, player); }
            Assert.That(cautious.lane, Is.Not.EqualTo(1));
            Assert.That(aggressive.Speed, Is.GreaterThan(cautious.Speed));
        }
        [Test]
        public void RivalFinishTimeIsInterpolatedAndFrozen()
        {
            var road = Road(); var player = Car(road);
            var rival = Own(new GameObject("Rival")).AddComponent<RivalDriver>(); rival.Begin(road, 99, 2, 32, true);
            var rivals = new List<RivalDriver> { rival }; var traffic = new List<TrafficActor>();
            for (int i = 1; i <= 60; i++) rival.Tick(1f/60, i/60f, 100, rivals, traffic, player);
            Assert.That(rival.FinishTime, Is.GreaterThan(0).And.LessThan(1)); Assert.That(rival.Distance, Is.EqualTo(100));
            float time = rival.FinishTime; rival.Tick(1, 10, 100, rivals, traffic, player); Assert.That(rival.FinishTime, Is.EqualTo(time));
        }
        [Test]
        public void RoadPoolReusesSectionsAndRebasesWithoutGaps()
        {
            var road = Road(); var prefab = Own(new GameObject("Segment", typeof(TrackSection)));
            road.valleySegment = prefab; road.desertSegment = prefab;
            road.Begin(EnvironmentKind.GreenValley, 1600, true, false);
            foreach (float distance in new [] { 0f, 520f, 2400f, 100000f })
            {
                road.Tick(distance);
                var sections = road.GetComponentsInChildren<TrackSection>();
                Assert.That(sections.Length, Is.EqualTo(config.pooledSegments));
                var starts = sections.Select(x => x.StartDistance).OrderBy(x => x).ToArray();
                for (int i = 1; i < starts.Length; i++) Assert.That(starts[i]-starts[i-1], Is.EqualTo(config.segmentLength).Within(.01f));
                Assert.That(Mathf.Abs(road.Point(distance, 0).z), Is.LessThanOrEqualTo(500));
            }
        }
        [Test]
        public void UpgradesChangeMeasuredAccelerationSteeringAndTopSpeed()
        {
            var road = Road(); var baseline = Car(road); var upgraded = Car(road);
            var profile = new PlayerProfile { speedLevel = 5, accelerationLevel = 5, handlingLevel = 5 };
            upgraded.Begin(road, config.vehicle.WithUpgrades(profile));
            for (int i = 0; i < 30; i++) { baseline.Tick(1f/60, .2f, false); upgraded.Tick(1f/60, .2f, false); }
            Assert.That(upgraded.Speed, Is.GreaterThan(baseline.Speed));
            Assert.That(upgraded.Offset, Is.GreaterThan(baseline.Offset));
            for (int i = 0; i < 600; i++) { baseline.Tick(1f/60, 0, false); upgraded.Tick(1f/60, 0, false); }
            Assert.That(upgraded.Speed, Is.GreaterThan(baseline.Speed));
            Assert.That(upgraded.Distance, Is.GreaterThan(baseline.Distance));
        }
        [UnityTest]
        public IEnumerator LocalSaveRoundTripAndCorruptionRecovery()
        {
            var operation = CheckLocalSaveAsync();
            while (!operation.IsCompleted) yield return null;
            operation.GetAwaiter().GetResult();
        }
        async Task CheckLocalSaveAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CarsTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new LocalSaveService(folder); var first = await service.LoadProfileAsync(); first.coins = 123;
                await service.SaveProfileAsync(first); first.coins = 456; await service.SaveProfileAsync(first);
                Assert.That((await service.LoadProfileAsync()).coins, Is.EqualTo(456));
                File.WriteAllText(Path.Combine(folder, "cars-profile.json"), "broken json");
                Assert.That((await service.LoadProfileAsync()).coins, Is.EqualTo(123));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [UnityTest]
        public IEnumerator CloudStartupDoesNotBlockLocalProgress()
        {
            var operation = CheckCloudStartupAsync();
            while (!operation.IsCompleted) yield return null;
            operation.GetAwaiter().GetResult();
        }

        static async Task CheckCloudStartupAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CarsTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                var connection = new TaskCompletionSource<bool>();
                var cloud = new TestCloud { FirstConnection = connection.Task };
                var saves = new SaveCoordinator(folder, true, cloud);

                await saves.InitializeAsync();
                Assert.That(cloud.ConnectionAttempts, Is.EqualTo(1));
                Assert.That(connection.Task.IsCompleted, Is.False);
                Assert.That(saves.Ready, Is.True);
                Assert.That(saves.CloudBusy, Is.False);

                connection.SetException(new IOException("Offline"));
                saves.Profile.coins = 75;
                await saves.SaveProgressAsync();
                Assert.That(cloud.ConnectionAttempts, Is.EqualTo(2));
                Assert.That(cloud.StoredProfile.coins, Is.EqualTo(75));
                Assert.That((await new LocalSaveService(folder).LoadProfileAsync()).coins, Is.EqualTo(75));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        [UnityTest]
        public IEnumerator CloudSaveRetriesQueuedRunAndRestoresProfile()
        {
            var operation = CheckCloudRoundTripAsync();
            while (!operation.IsCompleted) yield return null;
            operation.GetAwaiter().GetResult();
        }

        static async Task CheckCloudRoundTripAsync()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CarsTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                var cloud = new TestCloud { RejectProfileWrite = true };
                var saves = new SaveCoordinator(folder, true, cloud);
                await saves.InitializeAsync();
                var run = new RunResult {
                    runId = "test-run", score = 500, level = 2, earnedCoins = 120,
                    mode = GameMode.Race, environment = EnvironmentKind.SunsetDesert,
                    finishedUtc = "2026-11-23T12:00:00.0000000Z", position = 1, won = true
                };
                saves.Profile.speedLevel = 1;
                saves.Profile.accelerationLevel = 2;
                saves.Profile.handlingLevel = 3;
                saves.Profile.Apply(run);
                await saves.SaveProgressAsync();

                var local = new LocalSaveService(folder);
                Assert.That((await local.LoadProfileAsync()).pending.Single().runId, Is.EqualTo(run.runId));
                Assert.That(saves.Status, Does.Contain("Cloud unavailable"));

                cloud.RejectProfileWrite = false;
                await saves.SyncAsync();
                Assert.That((await local.LoadProfileAsync()).pending, Is.Empty);
                var storedRun = cloud.Results.Values.Single();
                Assert.That(storedRun.score, Is.EqualTo(run.score));
                Assert.That(storedRun.level, Is.EqualTo(run.level));
                Assert.That(storedRun.environment, Is.EqualTo(run.environment));
                Assert.That(storedRun.finishedUtc, Is.EqualTo(run.finishedUtc));

                await local.SaveProfileAsync(new PlayerProfile());
                var restored = new SaveCoordinator(folder, true, cloud);
                await restored.InitializeAsync();
                await restored.SyncAsync(restoreCloud: true);
                Assert.That(restored.Status, Is.EqualTo("Cloud profile restored"));
                var profile = await local.LoadProfileAsync();
                Assert.That(profile.best.Single().score, Is.EqualTo(run.score));
                Assert.That(profile.best.Single().key, Is.EqualTo(run.Key));
                Assert.That(profile.coins, Is.EqualTo(120));
                Assert.That(profile.speedLevel, Is.EqualTo(1));
                Assert.That(profile.accelerationLevel, Is.EqualTo(2));
                Assert.That(profile.handlingLevel, Is.EqualTo(3));
                Assert.That(profile.rewardedRuns, Is.EqualTo(new[] { run.runId }));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }

        sealed class TestCloud : ISaveService
        {
            public Task FirstConnection = Task.CompletedTask;
            public int ConnectionAttempts;
            public bool RejectProfileWrite;
            public PlayerProfile StoredProfile;
            public readonly Dictionary<string, RunResult> Results = new Dictionary<string, RunResult>();
            public string Status => "Test cloud saved";

            public Task InitializeAsync() => ++ConnectionAttempts == 1 ? FirstConnection : Task.CompletedTask;
            public Task<PlayerProfile> LoadProfileAsync() =>
                Task.FromResult(JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(StoredProfile)));
            public Task SaveProfileAsync(PlayerProfile profile)
            {
                if (RejectProfileWrite) return Task.FromException(new IOException("Offline during profile upload"));
                StoredProfile = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
                StoredProfile.pending.Clear();
                return Task.CompletedTask;
            }
            public Task SaveResultAsync(RunResult result)
            {
                Results[result.runId] = JsonUtility.FromJson<RunResult>(JsonUtility.ToJson(result));
                return Task.CompletedTask;
            }
        }
    }
}
