using UnityEngine;

namespace Cars
{
    public sealed class ArcadeCarController : MonoBehaviour
    {
        public event System.Action Collided;
        public Transform body;
        public Transform[] wheels;
        public ParticleSystem collisionEffect;
        public ParticleSystem boostEffect;
        public float Distance { get; private set; }
        public float Offset { get; private set; }
        public float Speed { get; private set; }
        public float Integrity { get; private set; } = 100;
        public float HitFlash { get; private set; }
        public bool Boosting { get; private set; }
        public float Boost { get; private set; } = 1;
        VehicleStats stats;
        TrackSystem track;
        float steering;
        float immune;
        float previousSpeed;
        public void Begin(TrackSystem road, VehicleStats properties)
        {
            track = road; stats = properties;
            Distance = 0; Offset = 0; Speed = 0; Integrity = 100; Boost = 1;
            steering = 0; immune = 0; HitFlash = 0;
            RenderPosition();
        }
        public void Tick(float dt, float input, bool boost)
        {
            immune = Mathf.Max(0, immune - dt);
            HitFlash = Mathf.MoveTowards(HitFlash, 0, dt * 2);
            Boosting = boost && Boost > .02f;
            Boost = Mathf.Clamp01(Boost + (Boosting ? -.32f : .10f) * dt);
            float maxSpeed = stats.topSpeed * (Boosting ? 1.25f : 1);
            previousSpeed = Speed;
            Speed = Mathf.MoveTowards(Speed, maxSpeed, stats.acceleration * dt);
            steering = Mathf.Lerp(steering, Mathf.Clamp(input, -1, 1), 1 - Mathf.Exp(-18 * dt));
            float edge = track.config.roadHalfWidth - 1;
            Offset += steering * stats.handling * dt;
            if (Mathf.Abs(Offset) > edge - .5f)
                Offset = Mathf.MoveTowards(Offset, Mathf.Sign(Offset) * (edge - .5f), dt * 2);
            Offset = Mathf.Clamp(Offset, -edge, edge);
            Distance += Speed * dt;
            if (boostEffect != null)
            {
                if (Boosting && !boostEffect.isPlaying) boostEffect.Play();
                if (!Boosting && boostEffect.isPlaying) boostEffect.Stop();
            }
            if (wheels != null) foreach (var wheel in wheels) if (wheel != null) wheel.Rotate(Speed * dt * 150, 0, 0, Space.Self);
        }
        public bool Hit(float damage)
        {
            if (immune > 0) return false;
            immune = 1.1f; HitFlash = 1; Speed *= .48f;
            Integrity = Mathf.Max(0, Integrity - damage);
            if (collisionEffect != null) collisionEffect.Play();
            Collided?.Invoke();
            return true;
        }
        public void RenderPosition()
        {
            if (track == null) return;
            transform.SetPositionAndRotation(track.Point(Distance, Offset, .02f), track.Direction(Distance));
            if (body != null) body.localRotation = Quaternion.Euler(0, steering * 8, -steering * 6);
        }
    }
}
