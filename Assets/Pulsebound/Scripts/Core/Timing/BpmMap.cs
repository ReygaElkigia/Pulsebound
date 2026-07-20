using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pulsebound.Core.Timing
{
    /// <summary>
    /// A tempo anchor: at <see cref="beat"/> the tempo is <see cref="bpm"/>. When
    /// <see cref="ramp"/> is true the tempo interpolates linearly from this anchor's
    /// bpm to the next anchor's bpm (accelerando / ritardando — the "Tempo Ramp" mechanic).
    /// </summary>
    [Serializable]
    public struct TempoAnchor
    {
        public float beat;
        public float bpm;
        public bool ramp;

        public TempoAnchor(float beat, float bpm, bool ramp = false)
        {
            this.beat = beat; this.bpm = bpm; this.ramp = ramp;
        }
    }

    /// <summary>
    /// Maps between beats and seconds across an arbitrary tempo map. This is what lets
    /// Pulsebound support constant BPM, mid-song tempo changes, and smooth tempo ramps
    /// while keeping all node timing authored in beats.
    ///
    /// The conversion integrates piecewise across anchors; ramp segments integrate the
    /// linear-tempo case in closed form so there is no per-frame numeric drift.
    /// </summary>
    [Serializable]
    public sealed class BpmMap
    {
        [SerializeField] private List<TempoAnchor> anchors = new();

        // Cached cumulative seconds at the start of each anchor segment.
        private double[] _segmentStartSeconds;
        private bool _dirty = true;

        public BpmMap() { }

        public BpmMap(float constantBpm)
        {
            anchors.Add(new TempoAnchor(0f, Mathf.Max(1f, constantBpm)));
            _dirty = true;
        }

        public BpmMap(IEnumerable<TempoAnchor> anchorList)
        {
            anchors = new List<TempoAnchor>(anchorList);
            anchors.Sort((a, b) => a.beat.CompareTo(b.beat));
            _dirty = true;
        }

        public IReadOnlyList<TempoAnchor> Anchors => anchors;

        public void SetConstant(float bpm)
        {
            anchors.Clear();
            anchors.Add(new TempoAnchor(0f, Mathf.Max(1f, bpm)));
            _dirty = true;
        }

        public void AddAnchor(TempoAnchor anchor)
        {
            anchors.Add(anchor);
            anchors.Sort((a, b) => a.beat.CompareTo(b.beat));
            _dirty = true;
        }

        public float BpmAtBeat(float beat)
        {
            EnsureValid();
            int i = SegmentIndexForBeat(beat);
            var a = anchors[i];
            if (!a.ramp || i + 1 >= anchors.Count) return a.bpm;

            var next = anchors[i + 1];
            float span = Mathf.Max(1e-4f, next.beat - a.beat);
            float t = Mathf.Clamp01((beat - a.beat) / span);
            return Mathf.Lerp(a.bpm, next.bpm, t);
        }

        /// <summary>Converts an absolute beat position to seconds from song start.</summary>
        public double BeatToSeconds(double beat)
        {
            EnsureValid();
            int i = SegmentIndexForBeat((float)beat);
            double seconds = _segmentStartSeconds[i];
            var a = anchors[i];

            double localBeats = beat - a.beat;
            if (localBeats <= 0) return seconds;

            if (a.ramp && i + 1 < anchors.Count)
            {
                var next = anchors[i + 1];
                seconds += RampSecondsForBeats(a.bpm, next.bpm, next.beat - a.beat, localBeats);
            }
            else
            {
                seconds += localBeats * 60.0 / a.bpm;
            }
            return seconds;
        }

        /// <summary>Converts seconds from song start into an absolute beat position.</summary>
        public double SecondsToBeats(double seconds)
        {
            EnsureValid();
            // Find the segment whose cumulative start second is <= seconds.
            int i = 0;
            for (int s = 1; s < anchors.Count; s++)
            {
                if (_segmentStartSeconds[s] <= seconds) i = s; else break;
            }

            var a = anchors[i];
            double localSeconds = seconds - _segmentStartSeconds[i];
            if (localSeconds <= 0) return a.beat;

            if (a.ramp && i + 1 < anchors.Count)
            {
                var next = anchors[i + 1];
                double localBeats = RampBeatsForSeconds(a.bpm, next.bpm, next.beat - a.beat, localSeconds);
                return a.beat + localBeats;
            }

            return a.beat + localSeconds * a.bpm / 60.0;
        }

        // --- internals ---

        private void EnsureValid()
        {
            if (!_dirty && _segmentStartSeconds != null) return;
            if (anchors.Count == 0) anchors.Add(new TempoAnchor(0f, 120f));
            if (anchors[0].beat > 0f) anchors.Insert(0, new TempoAnchor(0f, anchors[0].bpm));

            _segmentStartSeconds = new double[anchors.Count];
            _segmentStartSeconds[0] = 0.0;
            for (int i = 1; i < anchors.Count; i++)
            {
                var prev = anchors[i - 1];
                var cur = anchors[i];
                double segBeats = cur.beat - prev.beat;
                double segSeconds = prev.ramp
                    ? RampSecondsForBeats(prev.bpm, cur.bpm, (float)segBeats, segBeats)
                    : segBeats * 60.0 / prev.bpm;
                _segmentStartSeconds[i] = _segmentStartSeconds[i - 1] + segSeconds;
            }
            _dirty = false;
        }

        private int SegmentIndexForBeat(float beat)
        {
            int idx = 0;
            for (int i = 1; i < anchors.Count; i++)
            {
                if (anchors[i].beat <= beat) idx = i; else break;
            }
            return idx;
        }

        // Closed-form integral of seconds over a linear-tempo (in beats) segment.
        // Tempo b(x) = bpm0 + (bpm1-bpm0)*(x/spanBeats); dt = 60/b(x) dx.
        private static double RampSecondsForBeats(float bpm0, float bpm1, float spanBeats, double beatsIntoSegment)
        {
            if (Mathf.Approximately(bpm0, bpm1) || spanBeats <= 0f)
                return beatsIntoSegment * 60.0 / bpm0;

            double k = (bpm1 - bpm0) / spanBeats;              // bpm per beat
            double bAt = bpm0 + k * beatsIntoSegment;
            // ∫ 60 / (bpm0 + k x) dx = 60/k * ln((bpm0 + k x)/bpm0)
            return 60.0 / k * Math.Log(bAt / bpm0);
        }

        // Inverse of the above: how many beats elapse in a given number of seconds.
        private static double RampBeatsForSeconds(float bpm0, float bpm1, float spanBeats, double secondsIntoSegment)
        {
            if (Mathf.Approximately(bpm0, bpm1) || spanBeats <= 0f)
                return secondsIntoSegment * bpm0 / 60.0;

            double k = (bpm1 - bpm0) / spanBeats;
            // seconds = 60/k * ln((bpm0 + k x)/bpm0) -> solve for x
            double bAt = bpm0 * Math.Exp(k * secondsIntoSegment / 60.0);
            return (bAt - bpm0) / k;
        }
    }
}
