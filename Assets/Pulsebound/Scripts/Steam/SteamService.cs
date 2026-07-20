#if PULSEBOUND_STEAM
using System;
using System.Collections.Generic;
using UnityEngine;
// NOTE: This file only compiles when the PULSEBOUND_STEAM scripting define is set AND a
// Steamworks binding (Steamworks.NET or Facepunch.Steamworks) is present in the project.
// The bodies below are intentionally thin adapters; swap the // STEAMWORKS: lines for real
// calls against your chosen binding. Kept behind the define so a bare checkout still builds.

namespace Pulsebound.Steam
{
    /// <summary>
    /// Concrete Steam platform service. Translates the engine-agnostic <see cref="ISteamService"/>
    /// contract into Steamworks calls. The rest of the game never sees a Steam type.
    /// </summary>
    public sealed class SteamService : ISteamService
    {
        private bool _initialized;
        public bool IsAvailable => _initialized;

        public bool Initialize()
        {
            try
            {
                // STEAMWORKS: _initialized = SteamAPI.Init();
                _initialized = false; // flip on once a binding is wired up
                return _initialized;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamService] init failed: {e.Message}");
                return false;
            }
        }

        public void UnlockAchievement(string apiName)
        {
            // STEAMWORKS: SteamUserStats.SetAchievement(apiName); SteamUserStats.StoreStats();
        }

        public void SetStat(string apiName, int value)
        {
            // STEAMWORKS: SteamUserStats.SetStat(apiName, value);
        }

        public void StoreStats()
        {
            // STEAMWORKS: SteamUserStats.StoreStats();
        }

        public void UploadLeaderboardScore(string leaderboardId, long score)
        {
            // STEAMWORKS: find-or-create leaderboard, then UploadLeaderboardScore(KeepBest, score)
        }

        public void DownloadLeaderboard(string id, LeaderboardScope scope, Action<IReadOnlyList<LeaderboardEntry>> onDone)
        {
            // STEAMWORKS: DownloadLeaderboardEntries(...) -> map to LeaderboardEntry
            onDone?.Invoke(Array.Empty<LeaderboardEntry>());
        }

        public bool WriteCloudFile(string name, byte[] data)
        {
            // STEAMWORKS: return SteamRemoteStorage.FileWrite(name, data, data.Length);
            return false;
        }

        public byte[] ReadCloudFile(string name)
        {
            // STEAMWORKS: SteamRemoteStorage.FileRead(...)
            return null;
        }

        public void PublishWorkshopItem(WorkshopItem item, Action<ulong> onPublished)
        {
            // STEAMWORKS: CreateItem + SubmitItemUpdate against item.ContentPath
            onPublished?.Invoke(0);
        }

        public void SubscribeWorkshopItem(ulong publishedFileId)
        {
            // STEAMWORKS: SteamUGC.SubscribeItem(new PublishedFileId_t(publishedFileId));
        }

        public void QueryWorkshop(WorkshopQuery query, Action<IReadOnlyList<WorkshopItem>> onDone)
        {
            // STEAMWORKS: SteamUGC.CreateQueryAllUGCRequest(...) -> map results
            onDone?.Invoke(Array.Empty<WorkshopItem>());
        }

        public void SetRichPresence(string key, string value)
        {
            // STEAMWORKS: SteamFriends.SetRichPresence(key, value);
        }

        public void RunCallbacks()
        {
            // STEAMWORKS: SteamAPI.RunCallbacks();
        }
    }
}
#endif
