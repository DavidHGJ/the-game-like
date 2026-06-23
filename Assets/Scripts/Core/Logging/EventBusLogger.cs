#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLike.Core.Events
{
    /// <summary>
    /// Console logging layer for the EventBus. Compiled only in the editor and
    /// development builds; it does not exist in release, so it has zero runtime
    /// cost for players. Output is colored by category and filtered by severity.
    /// </summary>
    public static class EventBusLogger
    {
        /// <summary>Master switch. Turn off to silence all event logging.</summary>
        public static bool Enabled = true;

        /// <summary>Events below this severity are not printed.</summary>
        public static EventSeverity MinSeverity = EventSeverity.Verbose;

        /// <summary>Per-category mute set. Add a category to hide its events.</summary>
        public static readonly HashSet<EventCategory> MutedCategories = new();

        /// <summary>When true, prints how many listeners reacted to each event.</summary>
        public static bool ShowListenerCount = true;

        private static readonly Dictionary<EventCategory, string> CategoryColors = new()
        {
            { EventCategory.Time,           "#7F8C8D" },
            { EventCategory.Economy,        "#27AE60" },
            { EventCategory.Project,        "#2980B9" },
            { EventCategory.Research,       "#8E44AD" },
            { EventCategory.Employee,       "#16A085" },
            { EventCategory.Market,         "#D35400" },
            { EventCategory.Marketing,      "#E67E22" },
            { EventCategory.Reputation,     "#F39C12" },
            { EventCategory.Contract,       "#2C3E50" },
            { EventCategory.NarrativeEvent, "#9B59B6" },
            { EventCategory.Acquisition,    "#C0392B" },
            { EventCategory.Franchise,      "#1ABC9C" },
            { EventCategory.Founder,        "#34495E" },
            { EventCategory.Valuation,      "#2ECC71" },
            { EventCategory.Platform,       "#3498DB" },
            { EventCategory.Achievement,    "#F1C40F" },
            { EventCategory.Meta,           "#95A5A6" },
            { EventCategory.System,         "#BDC3C7" }
        };

        public static void LogPublish<T>(
            T payload, string callerMember, string callerFile, int listenerCount)
            where T : struct, IGameEvent
        {
            if (!Enabled) return;
            if (payload.Severity < MinSeverity) return;
            if (MutedCategories.Contains(payload.Category)) return;

            var color = CategoryColors.TryGetValue(payload.Category, out var c) ? c : "#FFFFFF";
            var caller = ShortenCaller(callerFile, callerMember);
            var listeners = ShowListenerCount ? $"  <color=#7F8C8D>({listenerCount} listeners)</color>" : "";

            var line =
                $"<color={color}><b>[{payload.Category}]</b></color> " +
                $"<color=#BDC3C7>{caller}</color> → " +
                $"{payload.Describe()}{listeners}";

            switch (payload.Severity)
            {
                case EventSeverity.Warning:
                    Debug.LogWarning(line);
                    break;
                case EventSeverity.Critical:
                    Debug.LogError(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }
        }

        private static string ShortenCaller(string callerFile, string callerMember)
        {
            var typeName = string.IsNullOrEmpty(callerFile)
                ? "Unknown"
                : Path.GetFileNameWithoutExtension(callerFile);
            return $"{typeName}.{callerMember}";
        }
    }
}
#endif
