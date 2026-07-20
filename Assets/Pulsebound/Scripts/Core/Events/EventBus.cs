using System;
using System.Collections.Generic;

namespace Pulsebound.Core.Events
{
    /// <summary>
    /// Type-safe, allocation-light publish/subscribe bus. Events are plain structs
    /// (see <see cref="GameEvents"/>) so publishing them does not allocate on the heap.
    ///
    /// Systems communicate through this bus rather than by referencing each other:
    /// the Judgment system publishes <see cref="GameEvents.NodeJudged"/>; Scoring, HUD,
    /// VFX and Audio each subscribe independently and never know about one another.
    /// </summary>
    public static class EventBus
    {
        private static class Channel<T> where T : struct
        {
            public static event Action<T> Handlers;
            public static void Raise(in T evt) => Handlers?.Invoke(evt);
            public static void Add(Action<T> h) => Handlers += h;
            public static void Remove(Action<T> h) => Handlers -= h;
            public static void ClearAll() => Handlers = null;
        }

        // Keep track of clear delegates so a global Clear() can wipe every channel.
        private static readonly List<Action> ChannelClears = new();
        private static readonly HashSet<Type> KnownChannels = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            RegisterChannel<T>();
            Channel<T>.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Channel<T>.Remove(handler);
        }

        public static void Publish<T>(in T evt) where T : struct
        {
            Channel<T>.Raise(in evt);
        }

        private static void RegisterChannel<T>() where T : struct
        {
            if (KnownChannels.Add(typeof(T)))
                ChannelClears.Add(Channel<T>.ClearAll);
        }

        /// <summary>Removes every subscriber on every channel. Use on scene teardown.</summary>
        public static void Clear()
        {
            foreach (var clear in ChannelClears) clear();
        }
    }
}
