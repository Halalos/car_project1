using UnityEngine;

namespace Cars
{
    public sealed class TrafficActor : MonoBehaviour
    {
        public float Distance { get; private set; }
        public float Offset { get; private set; }
        public bool Scored { get; set; }
        public bool obstacle;
        float speed;
        float targetOffset;
        float nextChange;
        int serial;
        TrackSystem track;
        public void Place(TrackSystem road, float distance, int sequence)
        {
            track = road; Distance = distance; serial = sequence;
            Offset = RivalDriver.LaneX(sequence % 3); targetOffset = Offset;
            speed = obstacle ? 0 : 12 + sequence % 9;
            nextChange = 3 + sequence % 5; Scored = false;
            RenderPosition();
        }
        public void Tick(float dt)
        {
            Distance += speed * dt;
            if (!obstacle)
            {
                nextChange -= dt;
                if (nextChange <= 0) { nextChange = 5 + serial % 4; serial++; targetOffset = RivalDriver.LaneX(serial % 3); }
                Offset = Mathf.MoveTowards(Offset, targetOffset, dt * 1.5f);
            }
        }
        public void RenderPosition()
        { if (track != null) transform.SetPositionAndRotation(track.Point(Distance, Offset, .02f), track.Direction(Distance)); }
    }
}
