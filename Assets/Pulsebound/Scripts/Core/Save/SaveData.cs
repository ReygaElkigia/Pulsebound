using System;
using System.Collections.Generic;

namespace Pulsebound.Core.Save
{
    /// <summary>Root persisted profile. Versioned so future saves can migrate cleanly.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;

        public string profileName = "Pulse";
        public long resonanceXp;
        public int resonanceLevel = 1;
        public int masteryTokens;

        public SettingsData settings = new();

        public List<BestScore> bestScores = new();
        public List<string> unlockedCosmetics = new();
        public List<string> equippedCosmetics = new();
        public List<string> unlockedAchievements = new();
        public List<AchievementProgress> achievementProgress = new();
        public List<string> subscribedLevels = new();

        // Lifetime stats for achievements/stats mirroring.
        public long lifetimePerfects;
        public long lifetimePlays;
        public int dailyStreak;
        public string lastDailyIso = "";

        public BestScore GetBest(string beatmapId)
        {
            foreach (var b in bestScores)
                if (b.beatmapId == beatmapId) return b;
            return null;
        }

        public void SubmitScore(string beatmapId, long score, float accuracy, int maxCombo, bool fullPerfect, string grade)
        {
            var best = GetBest(beatmapId);
            if (best == null)
            {
                bestScores.Add(new BestScore
                {
                    beatmapId = beatmapId, score = score, accuracy = accuracy,
                    maxCombo = maxCombo, fullPerfect = fullPerfect, grade = grade
                });
                return;
            }
            if (score > best.score)
            {
                best.score = score;
                best.accuracy = accuracy;
                best.maxCombo = maxCombo;
                best.fullPerfect = best.fullPerfect || fullPerfect;
                best.grade = grade;
            }
        }
    }

    [Serializable]
    public sealed class BestScore
    {
        public string beatmapId;
        public long score;
        public float accuracy;
        public int maxCombo;
        public bool fullPerfect;
        public string grade;
    }

    [Serializable]
    public sealed class AchievementProgress
    {
        public string achievementId;
        public float progress; // 0..1
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.9f;
        public float sfxVolume = 0.8f;
        public float uiVolume = 0.7f;

        public float audioOffsetSeconds;
        public float inputOffsetSeconds;

        public bool reducedMotion;
        public bool screenShake = true;
        public bool flashEffects = true;
        public bool colorblindSafe;
        public string uiThemeId = "default";

        public int targetFrameRate = 144;
        public bool vsync = true;
    }
}
