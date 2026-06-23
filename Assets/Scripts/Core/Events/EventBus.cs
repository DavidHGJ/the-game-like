using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameLike.Core.Events
{
    /// <summary>
    /// Central, type-based publish/subscribe hub for architecture-level game events.
    /// Subscribe and publish by payload type: a new event type only requires a new
    /// payload struct, never a change to this class.
    /// All traffic flows through <see cref="Publish{T}"/>, which is also the single
    /// instrumentation point for the logging layer (see EventBusLogger).
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new();

        /// <summary>
        /// Registers a handler for events of type <typeparamref name="T"/>.
        /// Always pair with <see cref="Unsubscribe{T}"/> (OnEnable/OnDisable).
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (Handlers.TryGetValue(type, out var existing))
            {
                Handlers[type] = (Action<T>)existing + handler;
            }
            else
            {
                Handlers[type] = handler;
            }
        }

        /// <summary>
        /// Removes a previously registered handler for events of type <typeparamref name="T"/>.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;

            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var existing)) return;

            var updated = (Action<T>)existing - handler;
            if (updated == null)
            {
                Handlers.Remove(type);
            }
            else
            {
                Handlers[type] = updated;
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers of type <typeparamref name="T"/>.
        /// The caller member/file is captured automatically for diagnostics.
        /// </summary>
        public static void Publish<T>(
            T payload,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFile = "") where T : struct, IGameEvent
        {
            var type = typeof(T);
            Handlers.TryGetValue(type, out var existing);
            var listenerCount = existing?.GetInvocationList().Length ?? 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBusLogger.LogPublish(payload, callerMember, callerFile, listenerCount);
#endif

            if (existing is Action<T> action)
            {
                action.Invoke(payload);
            }
        }

        /// <summary>
        /// Clears every registered handler. Call on full game teardown or when
        /// returning to the main menu to avoid stale subscriptions across runs.
        /// </summary>
        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
