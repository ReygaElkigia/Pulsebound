using Pulsebound.Core.Events;
using Pulsebound.Core.Services;
using Pulsebound.Core.Timing;
using UnityEngine;

namespace Pulsebound.Core.Scoring
{
    /// <summary>
    /// Owns combo, multiplier tiers, running score, accuracy, and the Flow meter.
    /// It reacts to <see cref="GameEvents.NodeJudged"/> and never talks to input or nodes
    /// directly — it just listens on the bus and publishes <see cref="GameEvents.ScoreChanged"/>.
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("Scoring")]
        [SerializeField] private long baseNodeValue = 1000;

        [Header("Multiplier tiers")]
        [Tooltip("Combo thresholds at which each multiplier tier unlocks.")]
        [SerializeField] private int[] tierThresholds = { 0, 10, 25, 50, 100 };
        [SerializeField] private float[] tierValues = { 1f, 1.5f, 2f, 3f, 4f };

        [Header("Flow meter")]
        [SerializeField] private float flowPerPerfect = 0.05f;
        [SerializeField] private float flowPerGreat = 0.02f;
        [SerializeField] private float flowDrainPerMiss = 0.12f;
        [SerializeField, Range(0f, 1f)] private float startFlow = 0.5f;
        [Tooltip("If true (Hardcore), empty Flow fails the run instead of dimming the world.")]
        [SerializeField] private bool failHard;

        // Live state.
        private long _score;
        private int _combo;
        private int _maxCombo;
        private int _tier;
        private float _flow;
        private int _hitCount;
        private int _judgedCount;
        private double _weightedAccuracy; // sum of judgment weights
        private bool _failed;

        public long Score => _score;
        public int Combo => _combo;
        public int MaxCombo => _maxCombo;
        public float MultiplierValue => tierValues[Mathf.Clamp(_tier, 0, tierValues.Length - 1)];
        public float Flow => _flow;
        public float Accuracy => _judgedCount == 0 ? 1f : (float)(_weightedAccuracy / _judgedCount);
        public bool FullPerfect => _judgedCount > 0 && _hitCount == _judgedCount && Mathf.Approximately(Accuracy, 1f);
        public bool Failed => _failed;

        private void Awake() => Services.Register<ScoreManager>(this);

        private void OnDestroy()
        {
            if (Services.IsRegistered<ScoreManager>() && Services.Get<ScoreManager>() == this)
                Services.Unregister<ScoreManager>();
            EventBus.Unsubscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Unsubscribe<GameEvents.SyncChainCompleted>(OnSyncChain);
        }

        private void Start()
        {
            EventBus.Subscribe<GameEvents.NodeJudged>(OnNodeJudged);
            EventBus.Subscribe<GameEvents.SyncChainCompleted>(OnSyncChain);
        }

        public void Configure(bool hardcore)
        {
            failHard = hardcore;
            ResetForRun();
        }

        public void ResetForRun()
        {
            _score = 0; _combo = 0; _maxCombo = 0; _tier = 0;
            _flow = startFlow; _hitCount = 0; _judgedCount = 0;
            _weightedAccuracy = 0; _failed = false;
            PublishScore();
            EventBus.Publish(new GameEvents.FlowChanged(_flow, false));
        }

        private void OnNodeJudged(GameEvents.NodeJudged evt)
        {
            if (_failed) return;

            _judgedCount++;
            _weightedAccuracy += evt.Judgment.ScoreWeight();

            if (evt.Judgment.IsHit())
            {
                _combo++;
                _maxCombo = Mathf.Max(_maxCombo, _combo);
                _hitCount++;
                UpdateTier();

                long value = (long)(baseNodeValue * evt.Judgment.ScoreWeight() * MultiplierValue);
                _score += value;

                AdjustFlow(evt.Judgment == Judgment.Perfect ? flowPerPerfect
                          : evt.Judgment == Judgment.Great ? flowPerGreat : 0f);
            }
            else
            {
                if (_combo > 0) EventBus.Publish(new GameEvents.ComboBroken(_combo));
                _combo = 0;
                _tier = Mathf.Max(0, _tier - 1); // drop one tier, not to zero
                AdjustFlow(-flowDrainPerMiss);
            }

            PublishScore();
        }

        private void OnSyncChain(GameEvents.SyncChainCompleted evt)
        {
            // Sync chains award a burst proportional to their length and current multiplier.
            _score += (long)(baseNodeValue * evt.ChainLength * MultiplierValue * 0.5f);
            AdjustFlow(0.15f);
            PublishScore();
        }

        private void UpdateTier()
        {
            int tier = 0;
            for (int i = 0; i < tierThresholds.Length; i++)
                if (_combo >= tierThresholds[i]) tier = i;
            _tier = tier;
        }

        private void AdjustFlow(float delta)
        {
            _flow = Mathf.Clamp01(_flow + delta);
            bool depleted = _flow <= 0f;
            EventBus.Publish(new GameEvents.FlowChanged(_flow, depleted));
            if (depleted && failHard && !_failed)
            {
                _failed = true;
                EventBus.Publish(new GameEvents.RunFinished(_score, Accuracy, _maxCombo, false, true));
            }
        }

        private void PublishScore()
        {
            EventBus.Publish(new GameEvents.ScoreChanged(_score, _combo, _tier, MultiplierValue, Accuracy));
        }
    }
}
