namespace GameLike.Core.Events
{
    /// <summary>
    /// Raised when a narrative event fires and pauses the game (GDD 11). The HUD opens
    /// the event popup; it reads the full event (text, options) from the EventManager.
    /// Carries primitive data only (Events layer is below Data/State).
    /// </summary>
    public readonly struct NarrativeEventTriggeredEvent : IGameEvent
    {
        public readonly string EventId;
        public readonly string Title;

        public NarrativeEventTriggeredEvent(string eventId, string title)
        {
            EventId = eventId;
            Title = title;
        }

        public EventCategory Category => EventCategory.NarrativeEvent;
        public EventSeverity Severity => EventSeverity.Warning;
        public string Describe() => $"Event: {Title}";
    }

    /// <summary>Raised after the player resolves a narrative event by choosing an option.</summary>
    public readonly struct NarrativeEventResolvedEvent : IGameEvent
    {
        public readonly string EventId;
        public readonly int OptionIndex;

        public NarrativeEventResolvedEvent(string eventId, int optionIndex)
        {
            EventId = eventId;
            OptionIndex = optionIndex;
        }

        public EventCategory Category => EventCategory.NarrativeEvent;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Event {EventId} resolved with option {OptionIndex}";
    }
}
