using System.Collections.Generic;
using UnityEngine;

namespace Cars
{
    public sealed class TrackSystem : MonoBehaviour
    {
        public GameConfig config;
        public GameObject valleySegment;
        public GameObject desertSegment;
        public GameObject finishPrefab;
        readonly List<TrackSection> sections = new List<TrackSection>();
        public float Origin { get; private set; }
        public EnvironmentKind Environment { get; private set; }
        GameObject finish;
        GameObject checkpoint;
        float finishDistance;
        float checkpointDistance;
        public static float Centre(float distance, EnvironmentKind environment)
        {
            float strength = environment == EnvironmentKind.GreenValley ? 12f : 19f;
            return Mathf.Sin(distance / 180f) * strength + Mathf.Sin(distance / 430f) * 8;
        }
        public Vector3 Point(float distance, float offset, float height = 0)
        { return new Vector3(Centre(distance, Environment) + offset, height, distance - Origin); }
        public Quaternion Direction(float distance)
        {
            float dx = Centre(distance + 1, Environment) - Centre(distance - 1, Environment);
            return Quaternion.LookRotation(new Vector3(dx, 0, 2));
        }
        public void Begin(EnvironmentKind environment, float length, bool endless, bool timed)
        {
            Clear(); Environment = environment; Origin = 0; finishDistance = length;
            for (int i = 0; i < config.pooledSegments; i++)
            {
                var instance = Instantiate(environment == EnvironmentKind.GreenValley ? valleySegment : desertSegment, transform);
                var section = instance.GetComponent<TrackSection>();
                section.Capture(); section.Place(this, (i - 2) * config.segmentLength);
                sections.Add(section);
            }
            if (!endless) { finish = Instantiate(finishPrefab, transform); PlaceGate(finish, finishDistance); }
            if (timed)
            {
                checkpointDistance = 500;
                checkpoint = Instantiate(finishPrefab, transform);
                checkpoint.name = "Checkpoint";
                foreach (var renderer in checkpoint.GetComponentsInChildren<Renderer>()) renderer.material.color = new Color(.1f, .9f, 1);
                PlaceGate(checkpoint, checkpointDistance);
            }
        }
        public void SetCheckpoint(float distance)
        { checkpointDistance = distance; if (checkpoint != null) { checkpoint.SetActive(distance < finishDistance); PlaceGate(checkpoint, distance); } }
        void PlaceGate(GameObject gate, float distance)
        { gate.transform.SetPositionAndRotation(Point(distance, 0), Direction(distance)); }
        public void Tick(float playerDistance)
        {
            bool rebased = Mathf.Abs(playerDistance - Origin) > 500;
            if (rebased) Origin = Mathf.Floor(playerDistance / 500) * 500;
            float earliest = Mathf.Floor(playerDistance / config.segmentLength) * config.segmentLength - 2 * config.segmentLength;
            float span = config.pooledSegments * config.segmentLength;
            foreach (var section in sections)
            {
                float start = section.StartDistance;
                while (start < earliest) start += span;
                if (rebased || !Mathf.Approximately(start, section.StartDistance)) section.Place(this, start);
            }
            if (finish != null) PlaceGate(finish, finishDistance);
            if (checkpoint != null) PlaceGate(checkpoint, checkpointDistance);
        }
        public void Clear()
        {
            foreach (var section in sections) if (section != null) Destroy(section.gameObject);
            sections.Clear();
            if (finish != null) Destroy(finish);
            if (checkpoint != null) Destroy(checkpoint);
        }
    }
}
