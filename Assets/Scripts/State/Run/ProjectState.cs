using System;
using GameLike.Data.Definitions;

namespace GameLike.State
{
    /// <summary>
    /// Áreas de desenvolvimento de alto nível nas quais o jogador distribui o
    /// foco da equipe ao criar um projeto. É uma agregação simplificada (slice
    /// jogável) dos 19 parâmetros de qualidade do GDD (seção 18.5): cada área
    /// reúne um subconjunto desses parâmetros — ver <c>QualityCalculator</c>.
    /// </summary>
    public enum DevArea
    {
        Design = 0,
        Art = 1,
        Audio = 2,
        Tech = 3,
        Content = 4
    }

    /// <summary>Ciclo de vida de um projeto no loop core.</summary>
    public enum ProjectStatus
    {
        Concept,
        InDevelopment,
        Released,
        /// <summary>Produção concluída: aguardando o jogador lançar ou corrigir bugs (adendo 23.3).</summary>
        ReadyToLaunch
    }

    /// <summary>
    /// Estado vivo de um projeto de jogo (camada de Estado, serializável). Referencia
    /// o gênero pelo <see cref="GameLike.Data.Base.DefinitionAsset.Id"/> estável e guarda
    /// os valores mutáveis da produção (alocação de foco, progresso, nota, receita).
    /// </summary>
    [Serializable]
    public sealed class ProjectState
    {
        public const int AreaCount = 5;

        /// <summary>Id único deste projeto (GUID) — referenciado por sequências (GDD 13).</summary>
        public string id = System.Guid.NewGuid().ToString("N");
        public string name;
        public string genreId;
        public string genreDisplay;
        /// <summary>Slug do gênero (nome do asset, ex: action_rpg) — para lookups de compatibilidade.</summary>
        public string genreKey;
        public string themeId;
        public string themeDisplay;
        /// <summary>Slug do tema (nome do asset) — para lookups de tendência/compatibilidade.</summary>
        public string themeKey;
        /// <summary>Multiplicador de hype derivado da compatibilidade Tema×Gênero (GDD 10.1).</summary>
        public float themeHypeMultiplier = 1f;

        /// <summary>Id estável do console/plataforma de lançamento (GDD 15). Vazio = não escolhido (saves v1).</summary>
        public string consoleId;
        public string consoleDisplay;
        /// <summary>Slug do console (nome do asset) — para lookups.</summary>
        public string consoleKey;
        /// <summary>Popularidade (0..1) do console no momento do início do projeto; escala as vendas no lançamento (GDD 15.2).</summary>
        public float consolePopularity01 = 1f;

        public ProjectSize size;

        /// <summary>Distribuição de foco por <see cref="DevArea"/>, normalizada (soma ≈ 1).</summary>
        public float[] areaAllocation = new float[AreaCount];

        /// <summary>Verdadeiro se este projeto é uma sequência de um jogo anterior (GDD 13).</summary>
        public bool isSequel;
        /// <summary>Id do projeto antecessor direto (quando sequência).</summary>
        public string parentProjectId;
        /// <summary>Id da franquia (o Id do primeiro jogo da linhagem). Vazio = IP novo.</summary>
        public string franchiseId;

        public ProjectStatus status = ProjectStatus.Concept;
        public int weeksInDevelopment;
        public int targetWeeks;
        public float progress01;
        public float finalScore;
        public long unitsSold;
        public long revenue;
        public long devCostSpent;

        /// <summary>Tamanho da equipe (staff) que trabalhou no projeto — definido pela fase (GDD 7.6).</summary>
        public int teamSize = 1;
        /// <summary>Cobertura de escopo alcançada (0..1): capacidade da equipe vs. escopo (GDD 5.3).</summary>
        public float scopeCoverage01 = 1f;
        /// <summary>Hype atual (0..1), acumulado por campanhas e progresso (GDD 10.1), decai com o tempo.</summary>
        public float hype01;
        /// <summary>Wishlists acumuladas até o lançamento (GDD 10.1).</summary>
        public long wishlists;
        /// <summary>Bugs acumulados durante a produção (GDD 5.5) — penalizam a nota no lançamento.</summary>
        public int bugCount;
        /// <summary>Total gasto em campanhas de marketing neste projeto (telemetria).</summary>
        public long marketingSpent;

        // ---------- Produção em fases (adendo 23.2) ----------
        /// <summary>Fase de produção atual (0..4, índice de <c>ProductionPhase</c>).</summary>
        public int phaseIndex;
        /// <summary>Semanas já trabalhadas na fase atual.</summary>
        public int phaseWeek;
        /// <summary>Duração planejada da fase atual, em semanas.</summary>
        public int phaseWeeks;
        /// <summary>Pontos que a equipe tem para distribuir na fase atual.</summary>
        public int phasePointBudget;
        /// <summary>Verdadeiro enquanto o jogo espera o jogador distribuir os pontos da fase.</summary>
        public bool awaitingAllocation;
        /// <summary>Se verdadeiro, o sistema distribui os pontos pelo perfil do gênero (testes/atalho).</summary>
        public bool autoAllocate;
        /// <summary>Crunch ligado na fase atual (GDD 5.6).</summary>
        public bool crunch;
        /// <summary>Pontos planejados por parâmetro (19) — definidos na alocação de cada fase.</summary>
        public float[] paramAllocated = new float[ParameterCount];
        /// <summary>Pontos já entregues por parâmetro (19) — crescem semana a semana.</summary>
        public float[] paramPoints = new float[ParameterCount];
        /// <summary>Soma do bônus de nota vindo de decisões em eventos durante o desenvolvimento (-1..+1).</summary>
        public float devEventsBonus;
        /// <summary>Engine Budget da empresa no início do projeto (adendo 23.7).</summary>
        public int engineBudget;
        /// <summary>Campanhas de marketing já feitas neste projeto (índices de <c>MarketingCampaign</c>).</summary>
        public System.Collections.Generic.List<int> campaignsUsed = new System.Collections.Generic.List<int>();

        // ---------- Pronto para lançar (adendo 23.3) ----------
        /// <summary>Semanas com o jogo pronto e ainda não lançado.</summary>
        public int weeksWaiting;
        /// <summary>Verdadeiro enquanto a equipe corrige bugs antes de lançar.</summary>
        public bool fixingBugs;

        // ---------- Lançamento e cauda de vendas (adendo 23.3 / 23.4) ----------
        public int price;
        public long launchMarketingBudget;
        /// <summary>Notas das casas de crítica (a nota final é a média).</summary>
        public float[] reviewScores = new float[0];
        public int releaseWeek;
        public int releaseYear;
        /// <summary>Unidades totais previstas ao longo de toda a cauda.</summary>
        public long forecastUnits;
        /// <summary>Semanas de venda já contabilizadas.</summary>
        public int salesWeek;
        public int salesWeeksTotal;
        /// <summary>Retenção semanal das vendas (0..1) — maior com nota melhor.</summary>
        public float salesRetention;
        /// <summary>Unidades vendidas em cada semana desde o lançamento.</summary>
        public System.Collections.Generic.List<long> weeklyUnits = new System.Collections.Generic.List<long>();

        public const int ParameterCount = 19;

        /// <summary>Verdadeiro enquanto o jogo ainda está vendendo.</summary>
        /// <summary>Lançado e esperando as críticas saírem (na semana seguinte ao lançamento).</summary>
        public bool awaitingReview;

        public bool IsSelling => status == ProjectStatus.Released && !awaitingReview && salesWeeksTotal > 0 && salesWeek < salesWeeksTotal;

        /// <summary>Garante arrays/listas válidos após carregar um save antigo.</summary>
        public void EnsureCollections()
        {
            if (areaAllocation == null || areaAllocation.Length != AreaCount) areaAllocation = new float[AreaCount];
            if (paramAllocated == null || paramAllocated.Length != ParameterCount) paramAllocated = new float[ParameterCount];
            if (paramPoints == null || paramPoints.Length != ParameterCount) paramPoints = new float[ParameterCount];
            if (reviewScores == null) reviewScores = new float[0];
            if (campaignsUsed == null) campaignsUsed = new System.Collections.Generic.List<int>();
            if (weeklyUnits == null) weeklyUnits = new System.Collections.Generic.List<long>();
        }

        public ProjectState(string name, string genreId, string genreDisplay, ProjectSize size, float[] allocation)
        {
            this.name = name;
            this.genreId = genreId;
            this.genreDisplay = genreDisplay;
            this.size = size;
            if (allocation != null && allocation.Length == AreaCount)
            {
                areaAllocation = allocation;
            }
        }
    }
}
