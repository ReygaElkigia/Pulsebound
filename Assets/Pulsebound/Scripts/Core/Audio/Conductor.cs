using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using Pulsebound.Core.Timing;
using UnityEngine;

namespace Pulsebound.Core.Audio
{
    /// <summary>
    /// The single source of musical truth. Every timed system (nodes, camera, VFX,
    /// background) reads position from the Conductor and NEVER from Time.time.
    ///
    /// Timing is anchored to <see cref="AudioSettings.dspTime"/> so judgments are
    /// frame-rate independent and deterministic across machines — the requirement for
    /// fair leaderboards.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class Conductor : MonoBehaviour
    {
        [Header("Calibration (seconds)")]
        [Tooltip("Compensates audio output latency. Positive = song is treated as further along.")]
        [SerializeField] private float audioOffset;
        [Tooltip("Compensates the player's systematic input bias. Applied to judgment, not playback.")]
        [SerializeField] private float inputOffset;

        private AudioSource _source;
        private BpmMap _bpmMap = new(120f);
        private string _songId = "";

        private double _dspSongStart;   // dspTime when playback began (scheduled)
        private bool _running;
        private int _lastWholeBeat = -1;

        public bool IsRunning => _running;
        public string SongId => _songId;
        public BpmMap BpmMap => _bpmMap;

        /// <summary>Player calibration for input timing, in seconds.</summary>
        public float InputOffset { get => inputOffset; set => inputOffset = value; }
        public float AudioOffset { get => audioOffset; set => audioOffset = value; }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            Services.Register<Conductor>(this);
        }

        private void OnDestroy()
        {
            if (Services.IsRegistered<Conductor>() && Services.Get<Conductor>() == this)
                Services.Unregister<Conductor>();
        }

        /// <summary>
        /// Schedules the clip to start a short lead-in from now so the first beat is
        /// perfectly aligned rather than "started this frame".
        /// </summary>
        public void Play(AudioClip clip, BpmMap bpmMap, string songId, double leadInSeconds = 0.25)
        {
            _bpmMap = bpmMap ?? new BpmMap(120f);
            _songId = !string.IsNullOrEmpty(songId) ? songId : (clip != null ? clip.name : "unknown");
            _source.clip = clip;

            double startAt = AudioSettings.dspTime + leadInSeconds;
            _dspSongStart = startAt;
            _lastWholeBeat = -1;
            _running = true;

            _source.PlayScheduled(startAt);
            EventBus.Publish(new GameEvents.SongStarted(_songId, _bpmMap.BpmAtBeat(0f)));
        }

        public void Stop(bool completed)
        {
            if (!_running) return;
            _running = false;
            _source.Stop();
            EventBus.Publish(new GameEvents.SongEnded(_songId, completed));
        }

        /// <summary>Seconds since the (offset-corrected) song start. Negative during lead-in.</summary>
        public double SongTimeSeconds =>
            AudioSettings.dspTime - _dspSongStart + audioOffset;

        /// <summary>Current absolute beat position (float). Sub-beats are the fractional part.</summary>
        public double SongBeat => _bpmMap.SecondsToBeats(SongTimeSeconds);

        /// <summary>
        /// Converts an input's DSP timestamp into a beat position, applying the player's
        /// input-offset calibration. Judgment code compares this against a node's target beat.
        /// </summary>
        public double DspToBeat(double dspTimestamp)
        {
            double seconds = dspTimestamp - _dspSongStart + audioOffset - inputOffset;
            return _bpmMap.SecondsToBeats(seconds);
        }

        /// <summary>Seconds a given beat lands at (for scheduling/spawn lead time).</summary>
        public double BeatToSongSeconds(double beat) => _bpmMap.BeatToSeconds(beat);

        /// <summary>Milliseconds-per-beat at a beat position (for converting beat delta → ms).</summary>
        public double MsPerBeatAt(double beat) => 60000.0 / Mathf.Max(1f, _bpmMap.BpmAtBeat((float)beat));

        private void Update()
        {
            if (!_running) return;

            double beat = SongBeat;
            int whole = Mathf.FloorToInt((float)beat);
            if (whole > _lastWholeBeat && whole >= 0)
            {
                _lastWholeBeat = whole;
                EventBus.Publish(new GameEvents.BeatTick(whole, (float)SongTimeSeconds));
            }

            if (_source.clip != null && !_source.isPlaying && SongTimeSeconds > 0.1)
            {
                // Clip finished playing naturally.
                Stop(completed: true);
            }
        }
    }
}
