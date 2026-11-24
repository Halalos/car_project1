using System.Collections.Generic;
using UnityEngine;

namespace Cars
{
    public sealed class RivalDriver : MonoBehaviour
    {
        public bool aggressive;
        public float Distance { get; private set; }
        public float Offset { get; private set; }
        public float Speed { get; private set; }
        public float FinishTime { get; private set; } = -1;
        public int lane;
        float decision;
        float targetSpeed;
        TrackSystem track;
        public void Begin(TrackSystem road, float startDistance, int startLane, float pace, bool attack)
        {
            track = road; Distance = startDistance; lane = startLane; Offset = LaneX(lane);
            aggressive = attack; targetSpeed = pace * (attack ? 1.04f : .98f);
            decision = .3f + startLane * .15f; Speed = 0; FinishTime = -1;
            RenderPosition();
        }
        public static float LaneX(int lane) { return (lane - 1) * 3.3f; }
        public void Tick(float dt, float elapsed, float finish, IList<RivalDriver> rivals,
            IList<TrafficActor> traffic, ArcadeCarController player)
        {
            if (FinishTime >= 0) return;
            decision -= dt;
            bool blocked = Blocked(lane, rivals, traffic, player, 23);
            if (decision <= 0)
            {
                decision = aggressive ? .35f : .8f;
                if (blocked || aggressive)
                {
                    int preferred = aggressive ? (lane + 1) % 3 : lane - 1;
                    for (int i = 0; i < 3; i++)
                    {
                        int candidate = (preferred + i + 3) % 3;
                        if (!Blocked(candidate, rivals, traffic, player, aggressive ? 17 : 30)) { lane = candidate; break; }
                    }
                }
                blocked = Blocked(lane, rivals, traffic, player, 23);
            }
            Speed = Mathf.MoveTowards(Speed, targetSpeed * (blocked ? .58f : 1), (aggressive ? 13 : 10) * dt);
            Offset = Mathf.MoveTowards(Offset, LaneX(lane), (aggressive ? 5 : 3.5f) * dt);
            float before = Distance;
            Distance += Speed * dt;
            if (Distance >= finish)
            {
                float crossing = Speed > 0 ? (finish - before) / Speed : 0;
                FinishTime = elapsed - dt + Mathf.Clamp(crossing, 0, dt);
                Distance = finish;
            }
        }
        bool Blocked(int candidate, IList<RivalDriver> rivals, IList<TrafficActor> traffic, ArcadeCarController player, float ahead)
        {
            float x = LaneX(candidate);
            foreach (var rival in rivals)
                if (rival != this && Mathf.Abs(rival.Offset - x) < 2.3f && rival.Distance > Distance - 6 && rival.Distance < Distance + ahead) return true;
            foreach (var actor in traffic)
                if (Mathf.Abs(actor.Offset - x) < 2.3f && actor.Distance > Distance - 6 && actor.Distance < Distance + ahead) return true;
            return Mathf.Abs(player.Offset - x) < 2.3f && player.Distance > Distance - 6 && player.Distance < Distance + ahead;
        }
        public void RenderPosition()
        { if (track != null) transform.SetPositionAndRotation(track.Point(Distance, Offset, .02f), track.Direction(Distance)); }
    }
}
