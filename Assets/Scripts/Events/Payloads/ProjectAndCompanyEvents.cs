namespace GameLike.Core.Events
{
    /// <summary>
    /// Raised when a game is released. One of the highest fan-out events:
    /// economy projects sales, valuation recalculates, reputation adjusts,
    /// market saturates the genre/theme, franchise updates its average,
    /// achievements check for unlocks.
    /// </summary>
    public readonly struct GameLaunchedEvent : IGameEvent
    {
        public readonly string GameName;
        public readonly float Score;
        public readonly bool IsSequel;

        public GameLaunchedEvent(string gameName, float score, bool isSequel)
        {
            GameName = gameName;
            Score = score;
            IsSequel = isSequel;
        }

        public EventCategory Category => EventCategory.Project;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe()
        {
            var sequel = IsSequel ? " (sequel)" : "";
            return $"\"{GameName}\"{sequel} launched — score {Score:0.0}/10";
        }
    }

    /// <summary>
    /// Raised when the company transitions between phases
    /// (Garage → Indie → Office → Studio).
    /// </summary>
    public readonly struct PhaseChangedEvent : IGameEvent
    {
        public readonly CompanyPhase From;
        public readonly CompanyPhase To;

        public PhaseChangedEvent(CompanyPhase from, CompanyPhase to)
        {
            From = from;
            To = to;
        }

        public EventCategory Category => EventCategory.Meta;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Phase {From} → {To}";
    }

    /// <summary>
    /// Raised when the company is sold (IPO or acquisition), ending the run.
    /// Critical severity — always visible in logs.
    /// </summary>
    public readonly struct CompanySoldEvent : IGameEvent
    {
        public readonly long SalePrice;
        public readonly SaleType SaleType;

        public CompanySoldEvent(long salePrice, SaleType saleType)
        {
            SalePrice = salePrice;
            SaleType = saleType;
        }

        public EventCategory Category => EventCategory.Acquisition;
        public EventSeverity Severity => EventSeverity.Critical;
        public string Describe() => $"Company sold via {SaleType} for {SalePrice:N0}";
    }

    /// <summary>
    /// The four company phases. Mirrors the GDD section 3.
    /// </summary>
    public enum CompanyPhase
    {
        Garage,
        Indie,
        Office,
        Studio
    }

    /// <summary>
    /// How a run ended on the positive side.
    /// </summary>
    public enum SaleType
    {
        IPO,
        RivalAcquisition,
        MarketSale
    }
}
