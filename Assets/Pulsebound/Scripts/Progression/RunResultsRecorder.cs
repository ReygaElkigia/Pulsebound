using Pulsebound.Core.Events;
using Pulsebound.Core.Save;
using Pulsebound.Core.Services;
using Pulsebound.Gameplay;
using Pulsebound.Steam;
using UnityEngine;

namespace Pulsebound.Progression
{
    /// <summary>
    /// Listens for <see cref="GameEvents.RunFinished"/> and persists the outcome: best score,
    /// Resonance XP (scaled by accuracy + difficulty, not grind time), lifetime stats, and
    /// pushes the score to the leaderboard via <see cref="ISteamService"/>. Then re-evaluates
    /// cosmetic unlocks. This keeps the runner free of persistence concerns.
    /// </summary>
    public sealed class RunResultsRecorder : MonoBehaviour
    {
        [SerializeField] private long baseResonancePerRun = 100;

        private SaveSystem _save;
        private CosmeticManager _cosmetics;
        private BeatmapRunner _runner;

        private void Start()
        {
            _save = Services.Get<SaveSystem>();
            Services.TryGet<CosmeticManager>(out _cosmetics);
            Services.TryGet<BeatmapRunner>(out _runner);
            EventBus.Subscribe<GameEvents.RunFinished>(OnRunFinished);
        }

        private void OnDestroy() => EventBus.Unsubscribe<GameEvents.RunFinished>(OnRunFinished);

        private void OnRunFinished(GameEvents.RunFinished evt)
        {
            var beatmap = _runner != null ? _runner.ActiveBeatmap : null;
            if (beatmap == null) return;

            string grade = GradeCalculator.Compute(evt.Accuracy, evt.FullPerfect, evt.Failed);
            _save.Data.SubmitScore(beatmap.beatmapId, evt.Score, evt.Accuracy, evt.MaxCombo, evt.FullPerfect, grade);

            // Resonance scales with accuracy and star rating, never with time spent.
            if (!evt.Failed)
            {
                long gained = (long)(baseResonancePerRun * (0.5f + evt.Accuracy) * (1f + beatmap.starRating * 0.15f));
                AddResonance(gained);
            }

            _save.Data.lifetimePlays++;
            _save.Save();

            _cosmetics?.EvaluateUnlocks(_save.Data.resonanceLevel);

            if (Services.TryGet<ISteamService>(out var steam) && steam.IsAvailable)
            {
                string board = $"{beatmap.beatmapId}:{beatmap.difficulty}";
                steam.UploadLeaderboardScore(board, evt.Score);
                steam.SetRichPresence("status", $"Scored {evt.Score:N0} — {grade}");
            }
        }

        private void AddResonance(long amount)
        {
            var d = _save.Data;
            d.resonanceXp += amount;
            // Simple escalating curve: level N needs N*1000 xp.
            while (d.resonanceXp >= d.resonanceLevel * 1000L)
            {
                d.resonanceXp -= d.resonanceLevel * 1000L;
                d.resonanceLevel++;
            }
        }
    }
}
