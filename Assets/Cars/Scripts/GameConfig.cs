using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cars
{
    public enum GameMode { EndlessSurvival, Race, TimeAttack }
    public enum EnvironmentKind { GreenValley, SunsetDesert }
    public enum UpgradeKind { Speed, Acceleration, Handling }
    public enum SessionState { Ready, Running, Paused, Finished }

    [Serializable]
    public class VehicleStats
    {
        public float topSpeed = 38f;
        public float acceleration = 11f;
        public float handling = 8f;
        public VehicleStats WithUpgrades(PlayerProfile profile)
        {
            return new VehicleStats {
                topSpeed = topSpeed * (1 + profile.speedLevel * .06f),
                acceleration = acceleration * (1 + profile.accelerationLevel * .10f),
                handling = handling * (1 + profile.handlingLevel * .08f)
            };
        }
    }

    [Serializable]
    public class LevelDefinition
    {
        public string title;
        public float length;
        public float timeLimit;
        public float aiSpeed;
        public float trafficSpacing;
        public LevelDefinition(string name, float distance, float seconds, float speed, float spacing)
        { title = name; length = distance; timeLimit = seconds; aiSpeed = speed; trafficSpacing = spacing; }
    }

    [CreateAssetMenu(menuName = "Cars/Game Configuration")]
    public class GameConfig : ScriptableObject
    {
        public bool enableFirebase = false;
        public VehicleStats vehicle = new VehicleStats();
        public float roadHalfWidth = 6f;
        public float segmentLength = 40f;
        public int pooledSegments = 16;
        public int rivalCount = 5;
        public int maxUpgradeLevel = 5;
        public int upgradeBaseCost = 150;
        public LevelDefinition[] levels = {
            new LevelDefinition("Rookie", 1600, 48, 31f, 85),
            new LevelDefinition("Challenger", 2400, 65, 34f, 65),
            new LevelDefinition("Master", 3200, 78, 37f, 50)
        };
        public int UpgradeCost(int currentLevel) { return upgradeBaseCost * (currentLevel + 1); }
    }

    [Serializable]
    public class RunResult
    {
        public string runId;
        public string playerId;
        public string finishedUtc;
        public GameMode mode;
        public EnvironmentKind environment;
        public int level;
        public int score;
        public float distance;
        public float elapsed;
        public int position;
        public bool won;
        public int earnedCoins;
        public string Key { get { return mode + "_" + environment + "_" + level; } }
    }

    [Serializable]
    public class BestRecord
    {
        public string key;
        public int score;
        public float time;
        public bool completed;
    }

    [Serializable]
    public class PlayerProfile
    {
        public int schemaVersion = 1;
        public string playerId = "local-" + Guid.NewGuid().ToString("N");
        public int coins;
        public int speedLevel;
        public int accelerationLevel;
        public int handlingLevel;
        public List<BestRecord> best = new List<BestRecord>();
        public List<string> rewardedRuns = new List<string>();
        public List<RunResult> pending = new List<RunResult>();
        public int GetLevel(UpgradeKind kind)
        { return kind == UpgradeKind.Speed ? speedLevel : kind == UpgradeKind.Acceleration ? accelerationLevel : handlingLevel; }
        public bool Purchase(UpgradeKind kind, GameConfig config)
        {
            int current = GetLevel(kind);
            if (current >= config.maxUpgradeLevel || coins < config.UpgradeCost(current)) return false;
            coins -= config.UpgradeCost(current);
            if (kind == UpgradeKind.Speed) speedLevel++;
            else if (kind == UpgradeKind.Acceleration) accelerationLevel++;
            else handlingLevel++;
            return true;
        }
        public bool Apply(RunResult result)
        {
            if (rewardedRuns.Contains(result.runId)) return false;
            rewardedRuns.Add(result.runId);
            coins += result.earnedCoins;
            pending.Add(result);
            var record = best.Find(x => x.key == result.Key);
            if (record == null) { record = new BestRecord { key = result.Key }; best.Add(record); }
            record.score = Mathf.Max(record.score, result.score);
            if (result.won && (!record.completed || result.elapsed < record.time)) record.time = result.elapsed;
            record.completed |= result.won;
            return true;
        }
    }

    public static class RaceRules
    {
        public static float Kmh(float metresPerSecond) { return Mathf.Max(0, metresPerSecond) * 3.6f; }
        public static int Reward(int score, bool won) { return Mathf.Max(10, score / 25) + (won ? 100 : 0); }
        public static int Position(float playerDistance, IEnumerable<float> rivals)
        { int place = 1; foreach (float distance in rivals) if (distance > playerDistance) place++; return place; }
        public static bool IsWin(GameMode mode, bool reachedFinish, int position, float remaining)
        { return mode == GameMode.Race ? reachedFinish && position == 1 : mode == GameMode.TimeAttack && reachedFinish && remaining >= 0; }
    }
}
