using System.Collections.Generic;
using Pulsebound.Core.Audio;
using Pulsebound.Core.Events;
using Pulsebound.Core.Scoring;
using Pulsebound.Core.Services;
using Pulsebound.Core.Timing;
using Pulsebound.Data;
using UnityEngine;

namespace Pulsebound.Gameplay
{
    /// <summary>
    /// Orchestrates a single run: starts the Conductor, streams nodes into the
    /// JudgmentSystem as they enter the reachable window, tracks Sync chains, detects the
    /// end of the chart, and publishes <see cref="GameEvents.RunFinished"/> with a grade.
    ///
    /// It owns no timing math itself — it reads beats from the Conductor and reacts to bus
    /// events, keeping the run loop thin and testable.
    /// </summary>
    public sealed class BeatmapRunner : MonoBehaviour
    {
        [Header("Spawn timing")]
        [Tooltip("How many beats ahead a node is handed to the judgment system / spawned.")]
        [SerializeField] private float lookaheadBeats = 4f;

        private Conductor _conductor;
        private JudgmentSystem _judgment;
        private ScoreManager _score;

        private Beatmap _beatmap;
        private int _nextNodeIndex;
        private float _endBeat;
        private bool _running;

        // Sync chain tracking.
        private int _activeChainCount;
        private bool _chainBroken;

        public Beatmap ActiveBeatmap => _beatmap;
        public bool IsRunning => _running;

        private void Start()
        {
            _conductor = Services.Get<Conductor>();
            _judgment = Services.Get<JudgmentSystem>();
            _score = Services.Get<ScoreManager>();

            EventBus.Subscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Subscribe<GameEvents.SongEnded>(OnSongEnded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Unsubscribe<GameEvents.SongEnded>(OnSongEnded);
        }

        public void Begin(Beatmap beatmap, AudioClip clip, bool hardcore)
        {
            _beatmap = beatmap;
            _nextNodeIndex = 0;
            _activeChainCount = 0;
            _chainBroken = false;
            _endBeat = beatmap.LastBeat();

            _judgment.ResetForRun(beatmap.difficulty);
            _score.Configure(hardcore);

            _conductor.Play(clip, beatmap.BuildBpmMap(), beatmap.songId, beatmap.leadInSeconds);
            _running = true;
        }

        public void Abort()
        {
            if (!_running) return;
            _running = false;
            _conductor.Stop(completed: false);
        }

        private void Update()
        {
            if (!_running || _beatmap == null) return;

            double beat = _conductor.SongBeat;

            // Stream upcoming nodes into the judgment system as they come into reach.
            while (_nextNodeIndex < _beatmap.nodes.Count &&
                   _beatmap.nodes[_nextNodeIndex].beat - beat <= lookaheadBeats)
            {
                _judgment.RegisterActive(_beatmap.nodes[_nextNodeIndex]);
                _nextNodeIndex++;
            }

            // End the run a bit after the last node so its window can resolve.
            if (_nextNodeIndex >= _beatmap.nodes.Count && beat > _endBeat + 2f)
                Finish(failed: false);
        }

        private void OnNodeJudged(GameEvents.NodeJudged evt)
        {
            // Advance / break the current Sync chain when chain-link nodes resolve.
            // (In content, SyncChainLink nodes are contiguous runs; a non-Perfect breaks them.)
            var type = FindNodeType(evt.NodeId);
            if (type == NodeType.SyncChainLink)
            {
                if (evt.Judgment == Judgment.Perfect && !_chainBroken)
                {
                    _activeChainCount++;
                }
                else
                {
                    _chainBroken = true;
                }
            }
            else if (_activeChainCount > 0)
            {
                // Chain ended on a non-link node: award if unbroken.
                if (!_chainBroken)
                    EventBus.Publish(new GameEvents.SyncChainCompleted(_activeChainCount));
                _activeChainCount = 0;
                _chainBroken = false;
            }
        }

        private NodeType FindNodeType(int nodeId)
        {
            // Linear scan is fine: chains are short and this only fires on link nodes.
            for (int i = 0; i < _beatmap.nodes.Count; i++)
                if (_beatmap.nodes[i].id == nodeId) return _beatmap.nodes[i].type;
            return NodeType.Standard;
        }

        private void OnSongEnded(GameEvents.SongEnded evt)
        {
            if (_running && evt.Completed) Finish(failed: false);
        }

        private void Finish(bool failed)
        {
            if (!_running) return;
            _running = false;
            if (_conductor.IsRunning) _conductor.Stop(completed: !failed);

            var grade = GradeCalculator.Compute(_score.Accuracy, _score.FullPerfect, failed);
            EventBus.Publish(new GameEvents.RunFinished(
                _score.Score, _score.Accuracy, _score.MaxCombo, _score.FullPerfect, failed));

            // Persisted score submission is handled by a RunResults listener (progression layer).
            Debug.Log($"[Run] {_beatmap.title} — {grade} | {_score.Score} | acc {_score.Accuracy:P2} | combo {_score.MaxCombo}");
        }
    }

    /// <summary>Maps accuracy + perfection into the letter grade shown on results.</summary>
    public static class GradeCalculator
    {
        public static string Compute(float accuracy, bool fullPerfect, bool failed)
        {
            if (failed) return "F";
            if (fullPerfect) return "S+";
            if (accuracy >= 0.98f) return "S";
            if (accuracy >= 0.93f) return "A";
            if (accuracy >= 0.85f) return "B";
            if (accuracy >= 0.72f) return "C";
            return "D";
        }
    }
}
