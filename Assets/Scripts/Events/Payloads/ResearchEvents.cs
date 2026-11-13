namespace GameLike.Core.Events
{
    /// <summary>Disparado quando os Pontos de Pesquisa mudam (ganho semanal, gasto, lançamento).</summary>
    public readonly struct ResearchPointsChangedEvent : IGameEvent
    {
        public readonly int Points;
        public ResearchPointsChangedEvent(int points) { Points = points; }

        public EventCategory Category => EventCategory.Research;
        public EventSeverity Severity => EventSeverity.Verbose;
        public string Describe() => $"Pontos de Pesquisa: {Points}";
    }

    /// <summary>Disparado quando uma pesquisa começa (GDD 6).</summary>
    public readonly struct ResearchStartedEvent : IGameEvent
    {
        public readonly string Id;
        public readonly string Label;
        public ResearchStartedEvent(string id, string label) { Id = id; Label = label; }

        public EventCategory Category => EventCategory.Research;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Pesquisa iniciada: {Label}";
    }

    /// <summary>Disparado quando uma pesquisa termina e o desbloqueio é concedido (GDD 6).</summary>
    public readonly struct ResearchCompletedEvent : IGameEvent
    {
        public readonly string Id;
        public readonly string Label;
        public ResearchCompletedEvent(string id, string label) { Id = id; Label = label; }

        public EventCategory Category => EventCategory.Research;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Desbloqueado: {Label}";
    }
}
