using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.State.Staff;

namespace GameLike.State
{
    /// <summary>
    /// Estado raiz de uma run (camada de Estado, serializável): a empresa, o projeto em
    /// andamento, o catálogo lançado, o mercado, a pesquisa e os rivais. É o objeto que o
    /// save persiste por slot.
    /// </summary>
    [Serializable]
    public sealed class RunState
    {
        public CompanyState company;
        public ProjectState currentProject;
        public List<ProjectState> releasedProjects = new List<ProjectState>();
        public MarketState market = new MarketState();
        public int runNumber = 1;
        public bool ended;
        public long salePrice;
        public ResearchState research = new ResearchState();

        /// <summary>Rivais desta timeline (GDD 8.3) — NPCs e empresas de runs anteriores.</summary>
        public List<RivalState> rivals = new List<RivalState>();
        /// <summary>Oferta de aquisição pendente feita por um rival (GDD 12.1).</summary>
        public AcquisitionOffer pendingOffer = new AcquisitionOffer();
        /// <summary>Semana/ano atuais, espelhados aqui para o save.</summary>
        public int week = 1;
        public int year = 1;
        /// <summary>Verdadeiro se a run passou por uma crise grave (GDD 19.5).</summary>
        public bool hadMajorCrisis;
        /// <summary>Dinheiro pessoal investido como capital inicial (aviso de prejuízo, GDD 2.4).</summary>
        public long capitalInvested;

        public RunState(CompanyState company)
        {
            this.company = company;
        }

        /// <summary>Há um projeto em produção ou pronto para lançar.</summary>
        public bool HasActiveProject =>
            currentProject != null &&
            (currentProject.status == ProjectStatus.InDevelopment || currentProject.status == ProjectStatus.ReadyToLaunch);

        /// <summary>O projeto atual está efetivamente em produção (fase rodando).</summary>
        public bool IsDeveloping =>
            currentProject != null && currentProject.status == ProjectStatus.InDevelopment && !currentProject.awaitingAllocation;

        /// <summary>Garante listas válidas após carregar um save antigo.</summary>
        public void EnsureCollections()
        {
            if (releasedProjects == null) releasedProjects = new List<ProjectState>();
            if (market == null) market = new MarketState();
            if (research == null) research = new ResearchState();
            if (rivals == null) rivals = new List<RivalState>();
            if (pendingOffer == null) pendingOffer = new AcquisitionOffer();
            if (week < 1) week = 1;
            if (year < 1) year = 1;
            currentProject?.EnsureCollections();
            for (int i = 0; i < releasedProjects.Count; i++) releasedProjects[i]?.EnsureCollections();
            if (company != null)
            {
                if (company.employees == null) company.employees = new List<EmployeeInstance>();
                if (company.valuationHistory == null) company.valuationHistory = new List<long>();
            }
        }
    }

    /// <summary>Um rival da timeline (GDD 8.3 / 12).</summary>
    [Serializable]
    public sealed class RivalState
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name;
        public CompanyPhase phase;
        public long valuation;
        /// <summary>0 = hostil, 0.5 = neutro, 1 = amigável.</summary>
        public float relation01 = 0.5f;
        public string lastGameName;
        public float lastGameScore;
        public int gamesReleased;
        public int employeeCount = 2;
        /// <summary>Verdadeiro se é uma empresa de uma run anterior do jogador (GDD 1.2).</summary>
        public bool fromPastRun;
        public int pastRunNumber;
        /// <summary>Semanas até aceitar ouvir nova proposta de compra do jogador (GDD 12.1).</summary>
        public int offerCooldownWeeks;
        public bool inCrisis;
        public bool acquired;
        public int iconIndex;
    }

    /// <summary>Proposta de um rival para comprar a empresa do jogador (GDD 12.1).</summary>
    [Serializable]
    public sealed class AcquisitionOffer
    {
        public bool active;
        public string rivalId;
        public string rivalName;
        public long price;
        public float multiple;
    }
}
