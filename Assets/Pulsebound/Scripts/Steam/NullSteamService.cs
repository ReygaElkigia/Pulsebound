using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Pulsebound.Steam
{
    /// <summary>
    /// Steam-free implementation. Cloud files fall back to local disk; leaderboards/workshop
    /// return empty results. Lets the entire game run without the Steam client attached.
    /// </summary>
    public sealed class NullSteamService : ISteamService
    {
        public bool IsAvailable => false;

        public void UnlockAchievement(string apiName) => Debug.Log($"[NullSteam] achievement {apiName}");
        public void SetStat(string apiName, int value) { }
        public void StoreStats() { }

        public void UploadLeaderboardScore(string leaderboardId, long score)
            => Debug.Log($"[NullSteam] leaderboard {leaderboardId} <- {score}");

        public void DownloadLeaderboard(string id, LeaderboardScope scope, Action<IReadOnlyList<LeaderboardEntry>> onDone)
            => onDone?.Invoke(Array.Empty<LeaderboardEntry>());

        public bool WriteCloudFile(string name, byte[] data)
        {
            try { File.WriteAllBytes(LocalPath(name), data); return true; }
            catch (Exception e) { Debug.LogWarning($"[NullSteam] cloud write failed: {e.Message}"); return false; }
        }

        public byte[] ReadCloudFile(string name)
        {
            var p = LocalPath(name);
            return File.Exists(p) ? File.ReadAllBytes(p) : null;
        }

        public void PublishWorkshopItem(WorkshopItem item, Action<ulong> onPublished) => onPublished?.Invoke(0);
        public void SubscribeWorkshopItem(ulong publishedFileId) { }
        public void QueryWorkshop(WorkshopQuery query, Action<IReadOnlyList<WorkshopItem>> onDone)
            => onDone?.Invoke(Array.Empty<WorkshopItem>());

        public void SetRichPresence(string key, string value) { }
        public void RunCallbacks() { }

        private static string LocalPath(string name) => Path.Combine(Application.persistentDataPath, "cloud", name);
    }
}
