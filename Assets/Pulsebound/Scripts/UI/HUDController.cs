using Pulsebound.Core.Events;
using Pulsebound.Core.Timing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pulsebound.UI
{
    /// <summary>
    /// Minimal, readable in-run HUD. Subscribes to score/flow/judgment events — it holds no
    /// gameplay logic, it only renders. Judgment popups appear at a fixed anchor; a production
    /// build spawns them pooled at the hit location.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text accuracyText;

        [Header("Flow")]
        [SerializeField] private Image flowBar;

        [Header("Judgment popup")]
        [SerializeField] private TMP_Text judgmentPopup;
        [SerializeField] private float popupSeconds = 0.35f;

        private float _popupTimer;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.ScoreChanged>(OnScore);
            EventBus.Subscribe<GameEvents.FlowChanged>(OnFlow);
            EventBus.Subscribe<GameEvents.NodeJudged>(OnJudged);
            EventBus.Subscribe<GameEvents.ComboBroken>(OnComboBroken);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.ScoreChanged>(OnScore);
            EventBus.Unsubscribe<GameEvents.FlowChanged>(OnFlow);
            EventBus.Unsubscribe<GameEvents.NodeJudged>(OnJudged);
            EventBus.Unsubscribe<GameEvents.ComboBroken>(OnComboBroken);
        }

        private void OnScore(GameEvents.ScoreChanged e)
        {
            if (comboText) comboText.text = e.Combo > 0 ? $"{e.Combo}" : "";
            if (multiplierText) multiplierText.text = $"x{e.MultiplierValue:0.##}";
            if (scoreText) scoreText.text = e.Score.ToString("N0");
            if (accuracyText) accuracyText.text = e.Accuracy.ToString("P1");
        }

        private void OnFlow(GameEvents.FlowChanged e)
        {
            if (flowBar) flowBar.fillAmount = e.Flow01;
        }

        private void OnJudged(GameEvents.NodeJudged e)
        {
            if (!judgmentPopup) return;
            judgmentPopup.text = e.Judgment switch
            {
                Judgment.Perfect => "PERFECT",
                Judgment.Great => "GREAT",
                Judgment.Good => "GOOD",
                _ => "MISS"
            };
            judgmentPopup.color = JudgmentColor(e.Judgment);
            _popupTimer = popupSeconds;
        }

        private void OnComboBroken(GameEvents.ComboBroken e)
        {
            if (comboText) comboText.text = "";
        }

        private void Update()
        {
            if (_popupTimer > 0f)
            {
                _popupTimer -= Time.deltaTime;
                if (_popupTimer <= 0f && judgmentPopup) judgmentPopup.text = "";
            }
        }

        // Colorblind-safe palette (distinct in luminance, not just hue).
        private static Color JudgmentColor(Judgment j) => j switch
        {
            Judgment.Perfect => new Color(0.16f, 0.90f, 1f),
            Judgment.Great => new Color(0.55f, 1f, 0.55f),
            Judgment.Good => new Color(1f, 0.95f, 0.6f),
            _ => new Color(1f, 0.35f, 0.45f)
        };
    }
}
