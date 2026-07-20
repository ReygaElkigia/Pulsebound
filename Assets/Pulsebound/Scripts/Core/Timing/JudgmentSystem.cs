using System.Collections.Generic;
using Pulsebound.Core.Audio;
using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using Pulsebound.Data;
using UnityEngine;

namespace Pulsebound.Core.Timing
{
    /// <summary>
    /// Turns raw Bind inputs into judgments. It holds a forward-moving window of active
    /// nodes and, on each input, matches to the nearest unjudged node then classifies the
    /// timing delta against the active <see cref="TimingWindow"/>.
    ///
    /// It also auto-misses nodes whose window has fully passed with no input, so the
    /// scoring/combo systems get a deterministic miss even when the player does nothing.
    /// </summary>
    public sealed class JudgmentSystem : MonoBehaviour
    {
        private Conductor _conductor;
        private TimingWindow _window = TimingWindow.Normal;

        // Active nodes awaiting judgment, kept sorted by beat.
        private readonly List<NodeDefinition> _active = new();
        private readonly HashSet<int> _judged = new();

        public TimingWindow Window { get => _window; set => _window = value; }

        private void Awake() => Services.Register<JudgmentSystem>(this);

        private void OnDestroy()
        {
            if (Services.IsRegistered<JudgmentSystem>() && Services.Get<JudgmentSystem>() == this)
                Services.Unregister<JudgmentSystem>();
            EventBus.Unsubscribe<GameEvents.BindPressed>(OnBind);
        }

        private void Start()
        {
            _conductor = Services.Get<Conductor>();
            EventBus.Subscribe<GameEvents.BindPressed>(OnBind);
        }

        public void SetDifficulty(GameDifficulty difficulty) => _window = TimingWindow.ForDifficulty(difficulty);

        public void ResetForRun(GameDifficulty difficulty)
        {
            _window = TimingWindow.ForDifficulty(difficulty);
            _active.Clear();
            _judged.Clear();
        }

        /// <summary>Called by the runner as nodes enter the reachable window.</summary>
        public void RegisterActive(in NodeDefinition node)
        {
            _active.Add(node);
        }

        private void OnBind(GameEvents.BindPressed evt)
        {
            if (_conductor == null || _active.Count == 0) return;

            double inputBeat = _conductor.DspToBeat(evt.DspTime);

            // Find nearest unjudged node by beat distance.
            int bestIdx = -1;
            float bestAbsDelta = float.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                var n = _active[i];
                if (_judged.Contains(n.id)) continue;
                float d = Mathf.Abs((float)(inputBeat - n.beat));
                if (d < bestAbsDelta) { bestAbsDelta = d; bestIdx = i; }
            }
            if (bestIdx < 0) return;

            var node = _active[bestIdx];
            float deltaBeats = (float)(inputBeat - node.beat);
            float msPerBeat = (float)_conductor.MsPerBeatAt(node.beat);
            float deltaMs = deltaBeats * msPerBeat;

            // ReverseRhythm inverts the intended bias: a late press is the "correct" read.
            float judgeMs = node.type == NodeType.ReverseRhythm ? -deltaMs : deltaMs;

            // If well outside the widest window, ignore this press (don't steal a future node).
            if (Mathf.Abs(judgeMs) > _window.MissThresholdMs) return;

            Judgment j = _window.Classify(judgeMs);
            MarkJudged(node, j, deltaMs);
        }

        private void MarkJudged(in NodeDefinition node, Judgment j, float deltaMs)
        {
            _judged.Add(node.id);
            EventBus.Publish(new GameEvents.NodeJudged(node.id, j, deltaMs, node.beat));
        }

        private void Update()
        {
            if (_conductor == null || !_conductor.IsRunning) return;

            double beat = _conductor.SongBeat;
            float missAfterBeats = _window.MissThresholdMs / (float)_conductor.MsPerBeatAt(beat);

            // Auto-miss + retire nodes whose window has fully elapsed.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var n = _active[i];
                if (_judged.Contains(n.id))
                {
                    _active.RemoveAt(i);
                    continue;
                }
                if (beat > n.beat + missAfterBeats)
                {
                    MarkJudged(n, Judgment.Miss, missAfterBeats * (float)_conductor.MsPerBeatAt(beat));
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
