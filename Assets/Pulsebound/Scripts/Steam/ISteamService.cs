using System;
using System.Collections.Generic;

namespace Pulsebound.Steam
{
    /// <summary>
    /// Platform abstraction so the game never references Steamworks types directly. A real
    /// build registers <c>SteamService</c>; dev/itch builds register <c>NullSteamService</c>
    /// and behave identically minus the platform features.
    /// </summary>
    public interface ISteamService
    {
        bool IsAvailable { get; }

        // Achievements & stats
        void UnlockAchievement(string apiName);
        void SetStat(string apiName, int value);
        void StoreStats();

        // Leaderboards
        void UploadLeaderboardScore(string leaderboardId, long score);
        void DownloadLeaderboard(string leaderboardId, LeaderboardScope scope, Action<IReadOnlyList<LeaderboardEntry>> onDone);

        // Cloud saves
        bool WriteCloudFile(string name, byte[] data);
        byte[] ReadCloudFile(string name);

        // Workshop
        void PublishWorkshopItem(WorkshopItem item, Action<ulong> onPublished);
        void SubscribeWorkshopItem(ulong publishedFileId);
        void QueryWorkshop(WorkshopQuery query, Action<IReadOnlyList<WorkshopItem>> onDone);

        // Rich presence & input
        void SetRichPresence(string key, string value);
        void RunCallbacks();
    }

    public enum LeaderboardScope { Global, FriendsOnly, AroundUser }

    public readonly struct LeaderboardEntry
    {
        public readonly string DisplayName;
        public readonly long Score;
        public readonly int Rank;
        public LeaderboardEntry(string name, long score, int rank) { DisplayName = name; Score = score; Rank = rank; }
    }

    public sealed class WorkshopItem
    {
        public ulong PublishedFileId;
        public string Title;
        public string Description;
        public string ContentPath;   // folder containing the .pbmap + audio + cover
        public List<string> Tags = new();
        public float Rating;
        public int Subscribers;
    }

    public sealed class WorkshopQuery
    {
        public string SearchText;
        public List<string> RequiredTags = new();
        public string SortBy = "rating"; // rating | subscribers | recent | trending
        public int Page = 1;
    }
}
