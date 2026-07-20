using System.Collections.Generic;
using Pulsebound.Core.Events;
using Pulsebound.Core.Save;
using Pulsebound.Core.Services;
using Pulsebound.Core.Timing;
using Pulsebound.Data;
using Pulsebound.Steam;
using UnityEngine;

namespace Pulsebound.Progression
{
    /// <summary>
    /// Tracks achievement progress locally (authoritative) and mirrors unlocks to Steam.
    /// Reacts to gameplay events to advance progress-based achievements (lifetime Perfects,
    /// combos, full-Perfect clears) without the rest of the game knowing achievements exist.
    /// </summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        [SerializeField] private List<AchievementDefinition> catalog = new();

        private SaveSystem _save;
        private CosmeticManager _cosmetics;
        private readonly Dictionary<string, AchievementDefinition> _byId = new();

        private void Awake()
        {
            Services.Register<AchievementManager>(this);
            foreach (var a in catalog)
                if (a != null) _byId[a.achievementId] = a;
        }

        private void OnDestroy()
        {
            if (Services.IsRegistered<AchievementManager>() && Services.Get<AchievementManager>() == this)
                Services.Unregister<AchievementManager>();
            EventBus.Unsubscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Unsubscribe<GameEvents.ScoreChanged>(OnScoreChanged);
            EventBus.Unsubscribe<GameEvents.RunFinished>(OnRunFinished);
        }

        private void Start()
        {
            _save = Services.Get<SaveSystem>();
            Services.TryGet<CosmeticManager>(out _cosmetics);
            EventBus.Subscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Subscribe<GameEvents.ScoreChanged>(OnScoreChanged);
            EventBus.Subscribe<GameEvents.RunFinished>(OnRunFinished);
        }

        private void OnNodeJudged(GameEvents.NodeJudged evt)
        {
            if (evt.Judgment == Judgment.Perfect)
            {
                _save.Data.lifetimePerfects++;
                Progress("perfect-10k", _save.Data.lifetimePerfects / 10000f);
            }
        }

        private void OnScoreChanged(GameEvents.ScoreChanged evt)
        {
            if (evt.Combo >= 500) Unlock("combo-500");
        }

        private void OnRunFinished(GameEvents.RunFinished evt)
        {
            if (evt.FullPerfect) Unlock("first-pulse-perfect");
            if (!evt.Failed && evt.Accuracy >= 0.98f) Unlock("first-s-grade");
            _save.Save();
        }

        public void Progress(string achievementId, float progress01)
        {
            if (!_byId.ContainsKey(achievementId)) return;
            var entry = FindProgress(achievementId);
            entry.progress = Mathf.Clamp01(Mathf.Max(entry.progress, progress01));
            if (entry.progress >= 1f) Unlock(achievementId);
        }

        public void Unlock(string achievementId)
        {
            if (!_byId.TryGetValue(achievementId, out var def)) return;
            if (_save.Data.unlockedAchievements.Contains(achievementId)) return;

            _save.Data.unlockedAchievements.Add(achievementId);
            if (def.rewardCosmetic != null) _cosmetics?.Unlock(def.rewardCosmetic.cosmeticId);

            if (Services.TryGet<ISteamService>(out var steam) && steam.IsAvailable)
                steam.UnlockAchievement(def.steamApiName);

            _save.Save();
        }

        private AchievementProgress FindProgress(string id)
        {
            foreach (var p in _save.Data.achievementProgress)
                if (p.achievementId == id) return p;
            var np = new AchievementProgress { achievementId = id, progress = 0f };
            _save.Data.achievementProgress.Add(np);
            return np;
        }
    }
}
