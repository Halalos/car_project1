using UnityEngine;

namespace Cars
{
    public sealed class GameAudio : MonoBehaviour
    {
        public AudioClip engineClip, windClip, boostClip;
        public AudioClip clickClip, startClip, impactClip, checkpointClip, passClip, upgradeClip, deniedClip, victoryClip, defeatClip;
        public GameSession session;
        public float MasterVolume { get; private set; } = .7f;
        public AudioSource EngineSource { get; private set; }
        public AudioSource BoostSource { get; private set; }
        public AudioSource WindSource { get; private set; }
        public AudioSource EffectsSource { get; private set; }
        public SoundCue LastCue { get; private set; }
        ArcadeCarController observedCar;
        void Awake()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Cars.SoundVolume", .7f));
            EngineSource = Source(true, engineClip, 180);
            BoostSource = Source(true, boostClip, 180);
            WindSource = Source(true, windClip, 200);
            EffectsSource = Source(false, null, 90);
        }
        AudioSource Source(bool loop, AudioClip clip, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.loop = loop; source.clip = clip; source.spatialBlend = 0; source.volume = 0;
            source.priority = priority; source.ignoreListenerPause = !loop;
            return source;
        }
        public void SetVolume(float volume, bool persist = true)
        {
            MasterVolume = Mathf.Clamp01(volume);
            if (EffectsSource != null) EffectsSource.volume = MasterVolume * .6f;
            if (persist) { PlayerPrefs.SetFloat("Cars.SoundVolume", MasterVolume); PlayerPrefs.Save(); }
        }
        public void ToggleSound() { SetVolume(MasterVolume > 0 ? 0 : .7f); }
        public void Observe(ArcadeCarController car)
        {
            if (observedCar == car) return;
            if (observedCar != null) observedCar.Collided -= OnImpact;
            observedCar = car;
            if (observedCar != null) observedCar.Collided += OnImpact;
        }
        void OnImpact() { Play(SoundCue.Impact); }
        public void Play(SoundCue cue)
        {
            LastCue = cue;
            if (EffectsSource == null || MasterVolume <= 0) return;
            AudioClip clip = cue == SoundCue.Click ? clickClip : cue == SoundCue.Start ? startClip :
                cue == SoundCue.Impact ? impactClip : cue == SoundCue.Checkpoint ? checkpointClip :
                cue == SoundCue.Pass ? passClip : cue == SoundCue.Upgrade ? upgradeClip :
                cue == SoundCue.Denied ? deniedClip : cue == SoundCue.Victory ? victoryClip : defeatClip;
            EffectsSource.volume = MasterVolume * .6f;
            if (clip != null) EffectsSource.PlayOneShot(clip);
        }
        void Update() { RefreshMix(); }
        public void RefreshMix()
        {
            bool running = session != null && session.State == SessionState.Running && !session.IsQuitting;
            var car = session == null ? null : session.Player;
            Observe(car);
            float speed = car == null ? 0 : Mathf.Clamp01(car.Speed / 48);
            UpdateLoop(EngineSource, running, MasterVolume * (.12f + speed*.16f), .65f + speed*1.5f);
            UpdateLoop(WindSource, running, MasterVolume * speed * .12f, .85f + speed*.35f);
            UpdateLoop(BoostSource, running && car != null && car.Boosting, MasterVolume*.22f, 1.05f);
        }
        static void UpdateLoop(AudioSource source, bool active, float volume, float pitch)
        {
            if (source == null) return;
            source.pitch = pitch;
            if (!active || volume <= 0) { if (source.isPlaying) source.Pause(); source.volume = 0; return; }
            source.volume = volume;
            if (!source.isPlaying && source.clip != null) source.Play();
        }
        void OnDisable()
        {
            Observe(null);
            if (EngineSource != null) EngineSource.Stop();
            if (WindSource != null) WindSource.Stop();
            if (BoostSource != null) BoostSource.Stop();
        }
    }
}
