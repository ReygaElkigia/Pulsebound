using Pulsebound.Core.Audio;
using Pulsebound.Core.Services;
using Pulsebound.Data;
using UnityEngine;
using UnityEngine.Splines;

namespace Pulsebound.Gameplay
{
    /// <summary>
    /// Moves the Pulse along an authored spline path at a music-driven speed. Position is
    /// derived from the Conductor's beat (not Time.deltaTime), so the Pulse is always exactly
    /// where the music says it should be — retries and frame hitches never desync it.
    ///
    /// Speed Modifier and Gravity Shift zones warp travel speed and orientation for a section.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public sealed class PulseController : MonoBehaviour
    {
        [Header("Motion")]
        [Tooltip("Base path distance (0..1 of spline) travelled per beat.")]
        [SerializeField] private float progressPerBeat = 0.02f;
        [SerializeField] private Transform pulseTransform;

        [Header("Reduced motion")]
        [SerializeField] private bool allowReorientation = true;

        private SplineContainer _spline;
        private Conductor _conductor;
        private Beatmap _beatmap;
        private float _speedMultiplier = 1f;
        private float _gravityDegrees;

        private void Awake() => _spline = GetComponent<SplineContainer>();

        private void Start() => _conductor = Services.Get<Conductor>();

        public void Bind(Beatmap beatmap) => _beatmap = beatmap;

        private void Update()
        {
            if (_conductor == null || !_conductor.IsRunning || _spline == null) return;

            float beat = (float)_conductor.SongBeat;
            ApplyActiveZones(beat);

            float t = Mathf.Repeat(beat * progressPerBeat * _speedMultiplier, 1f);
            Vector3 pos = _spline.EvaluatePosition(t);
            Vector3 fwd = _spline.EvaluateTangent(t);

            var tr = pulseTransform != null ? pulseTransform : transform;
            tr.position = pos;
            if (fwd.sqrMagnitude > 1e-5f)
            {
                Quaternion look = Quaternion.LookRotation(Vector3.forward, fwd.normalized);
                if (allowReorientation && !Mathf.Approximately(_gravityDegrees, 0f))
                    look *= Quaternion.Euler(0f, 0f, _gravityDegrees);
                tr.rotation = look;
            }
        }

        private void ApplyActiveZones(float beat)
        {
            _speedMultiplier = 1f;
            _gravityDegrees = 0f;
            if (_beatmap == null) return;

            foreach (var z in _beatmap.zones)
            {
                if (!z.Contains(beat)) continue;
                switch (z.type)
                {
                    case ZoneType.SpeedModifier: _speedMultiplier *= Mathf.Max(0.1f, z.value); break;
                    case ZoneType.GravityShift: _gravityDegrees = z.value; break;
                    // Invisible / Mirror are presentation-layer; TempoRamp lives in the BpmMap.
                }
            }
        }

        public void SetReducedMotion(bool reduced) => allowReorientation = !reduced;
    }
}
