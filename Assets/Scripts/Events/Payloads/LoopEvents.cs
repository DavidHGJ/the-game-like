namespace GameLike.Core.Events
{
    /// <summary>Um projeto foi criado e entrou em produção.</summary>
    public readonly struct ProjectStartedEvent : IGameEvent
    {
        public readonly string GameName;
        public ProjectStartedEvent(string gameName) { GameName = gameName; }
        public EventCategory Category => EventCategory.Project;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Projeto iniciado: \"{GameName}\"";
    }

    /// <summary>Uma fase de produção terminou e a próxima aguarda a distribuição de pontos (adendo 23.2).</summary>
    public readonly struct ProductionPhaseAwaitingEvent : IGameEvent
    {
        public readonly int PhaseIndex;
        public ProductionPhaseAwaitingEvent(int phaseIndex) { PhaseIndex = phaseIndex; }
        public EventCategory Category => EventCategory.Project;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Fase {PhaseIndex + 1} aguardando alocação";
    }

    /// <summary>A alocação de uma fase foi confirmada e a fase começou a rodar.</summary>
    public readonly struct ProductionPhaseStartedEvent : IGameEvent
    {
        public readonly int PhaseIndex;
        public readonly int Weeks;
        public ProductionPhaseStartedEvent(int phaseIndex, int weeks) { PhaseIndex = phaseIndex; Weeks = weeks; }
        public EventCategory Category => EventCategory.Project;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Fase {PhaseIndex + 1} iniciada ({Weeks} semanas)";
    }

    /// <summary>A produção terminou: o jogo está pronto para lançar (adendo 23.3).</summary>
    public readonly struct ProjectReadyToLaunchEvent : IGameEvent
    {
        public readonly string GameName;
        public readonly int Bugs;
        public ProjectReadyToLaunchEvent(string gameName, int bugs) { GameName = gameName; Bugs = bugs; }
        public EventCategory Category => EventCategory.Project;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"\"{GameName}\" pronto para lançar ({Bugs} bugs)";
    }

    /// <summary>O valuation atingiu o limite da próxima fase: expansão disponível (GDD 2.1).</summary>
    public readonly struct PhaseExpansionAvailableEvent : IGameEvent
    {
        public readonly CompanyPhase Next;
        public readonly long Cost;
        public PhaseExpansionAvailableEvent(CompanyPhase next, long cost) { Next = next; Cost = cost; }
        public EventCategory Category => EventCategory.Meta;
        public EventSeverity Severity => EventSeverity.Warning;
        public string Describe() => $"Expansão disponível → {Next} (custo {Cost:N0})";
    }

    /// <summary>Um rival fez uma proposta para comprar a empresa do jogador (GDD 12.1).</summary>
    public readonly struct AcquisitionOfferReceivedEvent : IGameEvent
    {
        public readonly string RivalName;
        public readonly long Price;
        public AcquisitionOfferReceivedEvent(string rivalName, long price) { RivalName = rivalName; Price = price; }
        public EventCategory Category => EventCategory.Acquisition;
        public EventSeverity Severity => EventSeverity.Critical;
        public string Describe() => $"{RivalName} ofereceu {Price:N0} pela empresa";
    }

    /// <summary>Um rival lançou um jogo (GDD 8.3).</summary>
    public readonly struct RivalGameReleasedEvent : IGameEvent
    {
        public readonly string RivalName;
        public readonly string GameName;
        public readonly float Score;
        public RivalGameReleasedEvent(string rivalName, string gameName, float score)
        {
            RivalName = rivalName; GameName = gameName; Score = score;
        }
        public EventCategory Category => EventCategory.Market;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"{RivalName} lançou \"{GameName}\" ({Score:0.0})";
    }

    /// <summary>O jogador comprou um rival (GDD 12).</summary>
    public readonly struct RivalAcquiredEvent : IGameEvent
    {
        public readonly string RivalName;
        public readonly long Price;
        public RivalAcquiredEvent(string rivalName, long price) { RivalName = rivalName; Price = price; }
        public EventCategory Category => EventCategory.Acquisition;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Rival adquirido: {RivalName} por {Price:N0}";
    }

    /// <summary>Evento global da linha do tempo (crash, boom — GDD 15.2).</summary>
    public readonly struct GlobalMarketEvent : IGameEvent
    {
        public readonly string Title;
        public readonly string Body;
        public readonly bool Positive;
        public GlobalMarketEvent(string title, string body, bool positive) { Title = title; Body = body; Positive = positive; }
        public EventCategory Category => EventCategory.Market;
        public EventSeverity Severity => EventSeverity.Warning;
        public string Describe() => $"Evento global: {Title}";
    }

    /// <summary>Um novo console chegou ao mercado (GDD 15.2).</summary>
    public readonly struct ConsoleReleasedEvent : IGameEvent
    {
        public readonly string ConsoleName;
        public readonly long LicenseCost;
        public readonly int Year;
        public ConsoleReleasedEvent(string consoleName, long licenseCost, int year)
        {
            ConsoleName = consoleName; LicenseCost = licenseCost; Year = year;
        }
        public EventCategory Category => EventCategory.Platform;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Novo console: {ConsoleName} (ano {Year})";
    }

    /// <summary>Uma conquista foi desbloqueada (GDD 20.5).</summary>
    public readonly struct AchievementUnlockedEvent : IGameEvent
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public AchievementUnlockedEvent(string id, string title, string description)
        {
            Id = id; Title = title; Description = description;
        }
        public EventCategory Category => EventCategory.Achievement;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Conquista: {Title}";
    }
}
