namespace GameLike.Core.Events
{
    /// <summary>
    /// Marker interface for every EventBus payload.
    /// Provides metadata the logging layer uses to categorize and color output.
    /// Implementing structs stay lightweight (value types, no allocation on publish).
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>Domain this event belongs to (Economy, Project, Crisis, ...).</summary>
        EventCategory Category { get; }

        /// <summary>Severity used to filter console verbosity.</summary>
        EventSeverity Severity { get; }

        /// <summary>Human-readable one-line summary shown in the log.</summary>
        string Describe();
    }

    /// <summary>
    /// Domain channels for filtering and coloring log output.
    /// Extend freely as new systems are added.
    /// </summary>
    public enum EventCategory
    {
        Time,
        Economy,
        Project,
        Research,
        Employee,
        Market,
        Marketing,
        Reputation,
        Contract,
        NarrativeEvent,
        Acquisition,
        Franchise,
        Founder,
        Valuation,
        Platform,
        Achievement,
        Meta,
        System
    }

    /// <summary>
    /// Severity levels. The logger applies a global minimum filter so noisy
    /// per-tick events can be silenced while keeping critical ones visible.
    /// </summary>
    public enum EventSeverity
    {
        Verbose,
        Info,
        Warning,
        Critical
    }
}
