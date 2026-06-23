namespace GameLike.Core.Events
{
    /// <summary>
    /// Raised by TimeManager every in-game week tick. High fan-out: economy,
    /// projects, research, market, marketing and events all react to it.
    /// Verbose by design — silence via EventBusLogger when debugging other domains.
    /// </summary>
    public readonly struct WeekTickEvent : IGameEvent
    {
        public readonly int Week;
        public readonly int Year;

        public WeekTickEvent(int week, int year)
        {
            Week = week;
            Year = year;
        }

        public EventCategory Category => EventCategory.Time;
        public EventSeverity Severity => EventSeverity.Verbose;
        public string Describe() => $"Week {Week} (Year {Year}) tick";
    }

    /// <summary>
    /// Raised whenever the company cash balance changes, after the change is applied.
    /// </summary>
    public readonly struct MoneyChangedEvent : IGameEvent
    {
        public readonly long NewBalance;
        public readonly long Delta;
        public readonly string Reason;

        public MoneyChangedEvent(long newBalance, long delta, string reason)
        {
            NewBalance = newBalance;
            Delta = delta;
            Reason = reason;
        }

        public EventCategory Category => EventCategory.Economy;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe()
        {
            var sign = Delta >= 0 ? "+" : "";
            return $"{Reason}: {sign}{Delta:N0} → balance {NewBalance:N0}";
        }
    }
}
