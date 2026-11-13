namespace GameLike.Core.Events
{
    /// <summary>Como uma run terminou (GDD 2.4/20.4).</summary>
    public enum RunEndReason
    {
        IPO,
        Acquisition,
        Bankruptcy,
        /// <summary>Venda voluntária da empresa pelo valuation do dia (GDD 2.4).</summary>
        MarketSale
    }

    /// <summary>
    /// Raised when a run ends by any means (sale or bankruptcy). Carries the final
    /// value cashed out, legacy points earned and the running personal total — the
    /// meta screen and UI react. Critical severity.
    /// </summary>
    public readonly struct RunEndedEvent : IGameEvent
    {
        public readonly RunEndReason Reason;
        public readonly long FinalValue;
        public readonly int LegacyGained;
        public readonly long PersonalTotal;

        public RunEndedEvent(RunEndReason reason, long finalValue, int legacyGained, long personalTotal)
        {
            Reason = reason;
            FinalValue = finalValue;
            LegacyGained = legacyGained;
            PersonalTotal = personalTotal;
        }

        public EventCategory Category => EventCategory.Meta;
        public EventSeverity Severity => EventSeverity.Critical;
        public string Describe() => $"Run ended ({Reason}): ${FinalValue:N0}, +{LegacyGained} PL";
    }
}
