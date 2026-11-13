using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Economy;
using GameLike.Systems.Market;
using GameLike.Systems.Meta;
using GameLike.Systems.Research;
using GameLike.Systems.Staff;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>Previsão mostrada na tela de lançamento (GDD 5.7) — só leitura, sem efeito no estado.</summary>
    public readonly struct LaunchPreview
    {
        public readonly float Hype01;
        public readonly long Wishlists;
        public readonly long WishlistSales;
        public readonly int ReferencePrice;
        public readonly float PriceFactor;

        public LaunchPreview(float hype01, long wishlists, long wishlistSales, int referencePrice, float priceFactor)
        {
            Hype01 = hype01;
            Wishlists = wishlists;
            WishlistSales = wishlistSales;
            ReferencePrice = referencePrice;
            PriceFactor = priceFactor;
        }
    }

    /// <summary>
    /// Conduz o ciclo de vida do projeto (GDD 5 / adendo 23.2–23.4): conceito → 5 fases de
    /// produção com distribuição de pontos por fase → pronto para lançar (com correção de bugs
    /// opcional) → lançamento (nota por casas de crítica) → vendas semanais em cauda.
    /// Anuncia os fatos via EventBus; não conhece quem reage. Lê o balanceamento a cada uso.
    /// </summary>
    public sealed class ProjectManager : MonoBehaviour
    {
        public const int ReviewerCount = 4;

        private RunState _run;
        private EconomyManager _economy;
        private MarketManager _market;
        private ResearchManager _research;
        private Func<string, GenreSO> _genreById;
        private Func<string, ThemeSO> _themeById;

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        /// <summary>Liga o manager ao estado da run e aos sistemas que consulta (feito pelo GameController).</summary>
        public void Bind(RunState run, EconomyManager economy, MarketManager market = null, ResearchManager research = null,
            Func<string, GenreSO> genreById = null, Func<string, ThemeSO> themeById = null)
        {
            _run = run;
            _economy = economy;
            _market = market;
            _research = research;
            _genreById = genreById;
            _themeById = themeById;
        }

        /// <summary>Verdadeiro se é possível iniciar um novo projeto agora.</summary>
        public bool CanStartProject => _run != null && !_run.ended && !_run.HasActiveProject;

        private GenreSO GenreOf(ProjectState p) => p != null && _genreById != null ? _genreById(p.genreId) : null;
        private ThemeSO ThemeOf(ProjectState p) => p != null && _themeById != null ? _themeById(p.themeId) : null;

        // ------------------------------------------------------------------ criação

        /// <summary>
        /// Inicia a produção de um projeto já preenchido pela UI (nome, gênero, tema, escopo,
        /// plataforma). A primeira fase fica aguardando a distribuição de pontos.
        /// </summary>
        public void StartProject(GenreSO genre, ThemeSO theme, ProjectState project)
        {
            if (_run == null || genre == null || project == null) return;
            if (_run.HasActiveProject) return;

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            int weeks = loop != null ? loop.WeeksFor(project.size) : 14;
            if (project.size == ProjectSize.Small)
                weeks = Mathf.Max(ParameterCatalog.PhaseCount, Mathf.RoundToInt(weeks * FounderPerks.SmallProjectDurationMultiplier));
            project.EnsureCollections();
            project.targetWeeks = weeks;
            project.status = ProjectStatus.InDevelopment;
            project.weeksInDevelopment = 0;
            project.progress01 = 0f;
            project.phaseIndex = 0;
            project.phaseWeek = 0;

            // Equipe efetiva = fundador + contratados, limitada pela fase da empresa (GDD 7.6).
            int maxTeam = loop != null ? loop.MaxTeamForPhase(_run.company.phase) : 1;
            int staff = Mathf.Clamp(_run.company.EffectiveTeamSize, 1, maxTeam);
            _run.company.teamSize = staff;
            project.teamSize = staff;
            project.scopeCoverage01 = ProductionCalculator.ScopeCoverage01(staff, project.size, loop);
            project.engineBudget = _research != null ? _research.EngineBudget : 0;

            // Tema (GDD 18.7): multiplicador de hype pela compatibilidade com o gênero.
            if (theme != null)
            {
                var weight = theme.CompatibilityFor(project.genreKey);
                project.themeHypeMultiplier = Mathf.Clamp(1f + (int)weight * 0.15f, 0.55f, 1.45f);
            }
            else
            {
                project.themeHypeMultiplier = 1f;
            }

            // Sequência (GDD 13.3): hype inicial herdado da média da franquia.
            if (project.isSequel && !string.IsNullOrEmpty(project.franchiseId))
            {
                float avg = FranchiseCalculator.AverageScore(_run.releasedProjects, project.franchiseId);
                project.hype01 = Mathf.Max(project.hype01, FranchiseCalculator.InitialHype01(avg));
            }

            _run.currentProject = project;
            BeginPhase(project);
            EventBus.Publish(new ProjectStartedEvent(project.name));
            if (!project.autoAllocate) EventBus.Publish(new ProductionPhaseAwaitingEvent(project.phaseIndex));
        }

        // ------------------------------------------------------------------ fases

        /// <summary>Pontos que a equipe entrega em uma fase (GDD 5.3): 100 por pessoa + nível/produtividade.</summary>
        public int PhaseBudget(ProjectState project)
        {
            if (_run == null || project == null) return 0;
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            int perPerson = loop != null ? loop.PointsPerPersonPerPhase : 100;
            float people = 1f; // fundador
            var staff = _run.company.employees;
            int counted = 1;
            for (int i = 0; i < staff.Count && counted < project.teamSize; i++, counted++)
                people += (1f + 0.2f * (int)staff[i].seniority) * StaffCalculator.ProductivityFactor(staff[i]);
            float points = perPerson * people * FounderPerks.SmallTeamPointMultiplier(project.teamSize);
            return Mathf.Max(perPerson, Mathf.RoundToInt(points / 10f) * 10);
        }

        /// <summary>Teto de pontos por parâmetro para o escopo do projeto (GDD 5.3).</summary>
        public int ParameterCap(ProjectState project)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            return loop != null && project != null ? loop.ParamCapFor(project.size) : 300;
        }

        /// <summary>Semanas que a fase dada leva neste projeto (adendo 23.2), com ou sem crunch.</summary>
        public int WeeksForPhase(ProjectState project, int phaseIndex, bool crunch)
        {
            if (project == null) return 1;
            float share = ParameterCatalog.DurationShare((ProductionPhase)Mathf.Clamp(phaseIndex, 0, ParameterCatalog.PhaseCount - 1));
            int weeks = Mathf.Max(1, Mathf.RoundToInt(project.targetWeeks * share));
            if (crunch)
            {
                var loop = BalanceProvider.Get<GameLoopBalanceSO>();
                weeks = Mathf.Max(1, Mathf.CeilToInt(weeks * (loop != null ? loop.CrunchDurationMultiplier : 0.75f)));
            }
            return weeks;
        }

        private void BeginPhase(ProjectState project)
        {
            project.phaseWeek = 0;
            project.crunch = false;
            project.phasePointBudget = PhaseBudget(project);
            project.phaseWeeks = WeeksForPhase(project, project.phaseIndex, false);
            project.awaitingAllocation = true;
        }

        /// <summary>Distribuição que segue o perfil ideal do gênero (usada no modo automático e em testes).</summary>
        public float[] IdealAllocation(ProjectState project)
        {
            var points = new float[ParameterCatalog.ParameterCount];
            if (project == null) return points;
            var phase = (ProductionPhase)project.phaseIndex;
            var parameters = ParameterCatalog.AvailableFor(phase, project.size);
            var shares = QualityCalculator.IdealPhaseShares(GenreOf(project), project.size, phase);
            int cap = ParameterCap(project);
            for (int i = 0; i < parameters.Count; i++)
                points[(int)parameters[i]] = Mathf.Min(cap, shares[i] * project.phasePointBudget);
            return points;
        }

        /// <summary>
        /// Confirma a distribuição de pontos da fase atual (um valor por parâmetro; só os da
        /// fase contam) e põe a fase para rodar. Excesso sobre o orçamento ou o teto é cortado.
        /// </summary>
        public bool ConfirmAllocation(float[] pointsByParameter, bool crunch)
        {
            var p = _run?.currentProject;
            if (p == null || p.status != ProjectStatus.InDevelopment || !p.awaitingAllocation) return false;
            if (pointsByParameter == null || pointsByParameter.Length < ParameterCatalog.ParameterCount) return false;

            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)p.phaseIndex, p.size);
            int cap = ParameterCap(p);
            float total = 0f;
            for (int i = 0; i < parameters.Count; i++)
                total += Mathf.Clamp(pointsByParameter[(int)parameters[i]], 0f, cap);
            float scale = total > p.phasePointBudget && total > 0f ? p.phasePointBudget / total : 1f;
            for (int i = 0; i < parameters.Count; i++)
            {
                int idx = (int)parameters[i];
                p.paramAllocated[idx] = Mathf.Clamp(pointsByParameter[idx], 0f, cap) * scale;
            }

            p.crunch = crunch;
            p.phaseWeeks = WeeksForPhase(p, p.phaseIndex, crunch);
            p.phaseWeek = 0;
            p.awaitingAllocation = false;
            p.areaAllocation = QualityCalculator.AreaSharesFromParameters(p.paramAllocated);
            EventBus.Publish(new ProductionPhaseStartedEvent(p.phaseIndex, p.phaseWeeks));
            return true;
        }

        // ------------------------------------------------------------------ semana

        private void OnWeekTick(WeekTickEvent e) => AdvanceWeek();

        /// <summary>Avança uma semana de vendas e produção (público para testes, que não recebem OnEnable).</summary>
        public void AdvanceWeek()
        {
            if (_run == null || _run.ended) return;

            // Lançados: na semana seguinte ao lançamento saem as críticas; só depois começam
            // as vendas, uma semana por vez (adendo 23.3 / 23.4).
            for (int i = 0; i < _run.releasedProjects.Count; i++)
            {
                var released = _run.releasedProjects[i];
                if (released.awaitingReview)
                {
                    released.awaitingReview = false;
                    EventBus.Publish(new GameLaunchedEvent(released.name, released.finalScore, released.isSequel));
                }
                else if (released.IsSelling) SellWeek(released);
            }

            var project = _run.currentProject;
            if (project == null) return;
            if (project.status == ProjectStatus.InDevelopment) TickDevelopment(project);
            else if (project.status == ProjectStatus.ReadyToLaunch) TickWaiting(project);
        }

        private void TickDevelopment(ProjectState p)
        {
            if (p.awaitingAllocation)
            {
                if (!p.autoAllocate) return;
                ConfirmAllocation(IdealAllocation(p), false);
            }

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)p.phaseIndex, p.size);
            for (int i = 0; i < parameters.Count; i++)
            {
                int idx = (int)parameters[i];
                p.paramPoints[idx] = Mathf.Min(p.paramAllocated[idx], p.paramPoints[idx] + p.paramAllocated[idx] / Mathf.Max(1, p.phaseWeeks));
            }

            p.bugCount += BugsThisWeek(p, loop);
            if (p.crunch) ApplyCrunchBurnout(loop);
            if (_research != null)
            {
                long engineCost = _research.EngineWeeklyCost;
                if (engineCost > 0) _economy?.ApplyDelta(-engineCost, "Engine");
            }

            p.phaseWeek++;
            p.weeksInDevelopment++;
            p.progress01 = OverallProgress01(p);
            if (p.phaseWeek < p.phaseWeeks) return;

            for (int i = 0; i < parameters.Count; i++)
                p.paramPoints[(int)parameters[i]] = p.paramAllocated[(int)parameters[i]];

            if (p.phaseIndex < ParameterCatalog.PhaseCount - 1)
            {
                p.phaseIndex++;
                BeginPhase(p);
                if (!p.autoAllocate) EventBus.Publish(new ProductionPhaseAwaitingEvent(p.phaseIndex));
                return;
            }

            p.status = ProjectStatus.ReadyToLaunch;
            p.progress01 = 1f;
            p.weeksWaiting = 0;
            p.fixingBugs = false;
            EventBus.Publish(new ProjectReadyToLaunchEvent(p.name, p.bugCount));
            if (p.autoAllocate) Launch(ReferencePrice(p), 0);
        }

        /// <summary>Progresso total do projeto (0..1) somando fases concluídas e a fração da fase atual.</summary>
        public static float OverallProgress01(ProjectState p)
        {
            if (p == null) return 0f;
            if (p.status != ProjectStatus.InDevelopment) return p.status == ProjectStatus.Concept ? 0f : 1f;
            float done = 0f;
            for (int i = 0; i < p.phaseIndex && i < ParameterCatalog.PhaseCount; i++)
                done += ParameterCatalog.DurationShare((ProductionPhase)i);
            float current = ParameterCatalog.DurationShare((ProductionPhase)Mathf.Min(p.phaseIndex, ParameterCatalog.PhaseCount - 1));
            float frac = p.phaseWeeks > 0 ? Mathf.Clamp01(p.phaseWeek / (float)p.phaseWeeks) : 0f;
            return Mathf.Clamp01(done + current * frac);
        }

        private int BugsThisWeek(ProjectState p, GameLoopBalanceSO loop)
        {
            float basePerWeek = loop != null ? loop.BugBasePerWeek : 2.5f;
            float teamQuality = StaffCalculator.TeamQuality01(_run.company.employees, p.areaAllocation);
            float teamFactor = Mathf.Clamp01(1.3f - teamQuality);
            float techFactor = 1f - BugCalculator.TechFocus01(p.areaAllocation);
            // Pontos em Estabilidade e Testes seguram os bugs (GDD 5.5).
            float stability = p.paramAllocated[(int)GameParameter.StabilityTesting];
            float stabilityFactor = 1f - 0.5f * Mathf.Clamp01(stability / Mathf.Max(1f, 0.5f * p.phasePointBudget));
            float crunch = p.crunch ? (loop != null ? loop.CrunchBugMultiplier : 1.6f) : 1f;
            float bugs = basePerWeek * teamFactor * techFactor * stabilityFactor * crunch * FounderPerks.BugMultiplier;
            int whole = Mathf.FloorToInt(bugs);
            if (UnityEngine.Random.value < bugs - whole) whole++;
            return Mathf.Max(0, whole);
        }

        private void ApplyCrunchBurnout(GameLoopBalanceSO loop)
        {
            float amount = (loop != null ? loop.CrunchBurnoutPerWeek : 0.05f) * FounderPerks.BurnoutMultiplier;
            var staff = _run.company.employees;
            for (int i = 0; i < staff.Count; i++)
                staff[i].burnout01 = Mathf.Clamp01(staff[i].burnout01 + amount);
        }

        // ------------------------------------------------------------------ pronto para lançar

        /// <summary>Liga/desliga a correção de bugs enquanto o jogo espera o lançamento (adendo 23.3).</summary>
        public void SetFixingBugs(bool fixing)
        {
            var p = _run?.currentProject;
            if (p != null && p.status == ProjectStatus.ReadyToLaunch) p.fixingBugs = fixing;
        }

        /// <summary>Bugs que a equipe corrige por semana com o jogo pronto.</summary>
        public int BugFixRate(ProjectState project)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            int basePerWeek = loop != null ? loop.BugFixPerWeekBase : 3;
            int perPerson = loop != null ? loop.BugFixPerWeekPerPerson : 2;
            return basePerWeek + perPerson * Mathf.Max(1, project != null ? project.teamSize : 1);
        }

        private void TickWaiting(ProjectState p)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            p.weeksWaiting++;
            p.hype01 = Mathf.Max(0f, p.hype01 - (loop != null ? loop.WaitingHypeLossPerWeek : 0.03f));
            if (!p.fixingBugs) return;
            p.bugCount = Mathf.Max(0, p.bugCount - BugFixRate(p));
            if (p.bugCount == 0) p.fixingBugs = false;
        }

        // ------------------------------------------------------------------ lançamento

        /// <summary>Preço de referência do escopo do projeto.</summary>
        public int ReferencePrice(ProjectState project)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            return loop != null && project != null ? loop.PriceFor(project.size) : 20;
        }

        /// <summary>Previsão para a tela de lançamento com o preço e o budget escolhidos.</summary>
        public LaunchPreview PreviewLaunch(int price, long marketingBudget)
        {
            var p = _run?.currentProject;
            if (p == null) return default;
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var qf = BalanceProvider.Get<QualityFormulaBalanceSO>();
            float hype = Mathf.Clamp01(p.hype01 + SalesCalculator.LaunchBudgetHype(marketingBudget, loop) * FounderPerks.HypeMultiplier);
            int reference = ReferencePrice(p);
            float priceFactor = SalesCalculator.PriceFactor(price, reference, loop != null ? loop.PriceElasticity : 1.3f);
            long wishSales = (long)(p.wishlists * (qf != null ? qf.SalesPer100Wishlists : 80) / 100 * priceFactor);
            return new LaunchPreview(hype, p.wishlists, wishSales, reference, priceFactor);
        }

        /// <summary>
        /// Lança o jogo pronto (GDD 5.7): calcula a nota (19.7), publica as casas de crítica,
        /// projeta as vendas e começa a cauda semanal. Falso se não há jogo pronto.
        /// </summary>
        public bool Launch(int price, long marketingBudget)
        {
            var p = _run?.currentProject;
            if (p == null || p.status != ProjectStatus.ReadyToLaunch) return false;

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var qf = BalanceProvider.Get<QualityFormulaBalanceSO>();
            var genre = GenreOf(p);
            var theme = ThemeOf(p);

            marketingBudget = Math.Max(0, Math.Min(marketingBudget, Math.Max(0, _run.company.cash)));
            if (marketingBudget > 0)
            {
                _economy?.ApplyDelta(-marketingBudget, $"Marketing de lançamento de \"{p.name}\"");
                p.marketingSpent += marketingBudget;
                p.hype01 = Mathf.Clamp01(p.hype01 + SalesCalculator.LaunchBudgetHype(marketingBudget, loop) * FounderPerks.HypeMultiplier);
            }
            p.launchMarketingBudget = marketingBudget;
            p.price = Mathf.Max(1, price);

            float coverage = ProductionCalculator.ScopeCoverage01(p.teamSize, p.size, loop);
            p.areaAllocation = QualityCalculator.AreaSharesFromParameters(p.paramPoints);
            float teamQuality = StaffCalculator.TeamQuality01(_run.company.employees, p.areaAllocation);
            float engineScore = EngineCalculator.BudgetScore01(p.engineBudget, p.size, BalanceProvider.Get<EngineBalanceSO>());
            float marketTrend = _market != null ? _market.ThemeTrend01(p.themeKey) : 0.5f;

            float score = genre != null
                ? QualityCalculator.ComputeDetailed(genre, p.size, p.paramPoints, teamQuality, coverage, engineScore,
                    theme, p.genreKey, marketTrend, p.devEventsBonus)
                : 5f;
            score -= BugCalculator.ScorePenalty(p.bugCount, p.size, loop);
            score = Mathf.Clamp(score + FounderPerks.ReviewBonus, 0f, 10f);

            // Casas de crítica (adendo 23.3): a nota final é a média das notas publicadas.
            float noise = loop != null ? loop.ReviewerNoise : 1f;
            p.reviewScores = new float[ReviewerCount];
            float sum = 0f;
            for (int i = 0; i < ReviewerCount; i++)
            {
                p.reviewScores[i] = Mathf.Clamp(Mathf.Round(score + UnityEngine.Random.Range(-noise, noise)), 1f, 10f);
                sum += p.reviewScores[i];
            }
            float finalScore = sum / ReviewerCount;

            float platformReach = string.IsNullOrEmpty(p.consoleId) ? 1f : SalesCalculator.PlatformReach(p.consolePopularity01);
            var sales = SalesCalculator.Compute(finalScore, p.size, p.hype01, p.wishlists, loop, qf, platformReach);
            float priceFactor = SalesCalculator.PriceFactor(p.price, ReferencePrice(p), loop != null ? loop.PriceElasticity : 1.3f);

            p.scopeCoverage01 = coverage;
            p.finalScore = finalScore;
            p.forecastUnits = (long)(sales.units * priceFactor);
            p.salesRetention = SalesCalculator.TailRetention(finalScore, loop);
            p.salesWeeksTotal = loop != null ? loop.SalesTailWeeks : 26;
            p.salesWeek = 0;
            p.unitsSold = 0;
            p.revenue = 0;
            p.weeklyUnits.Clear();
            p.releaseWeek = _run.week;
            p.releaseYear = _run.year;
            p.status = ProjectStatus.Released;
            p.fixingBugs = false;

            _run.company.gamesReleased++;
            if (!string.IsNullOrEmpty(p.genreId) && !_run.company.genresReleased.Contains(p.genreId))
                _run.company.genresReleased.Add(p.genreId);
            _run.releasedProjects.Add(p);
            _run.currentProject = null;

            // As críticas saem na próxima semana; as vendas começam depois delas.
            p.awaitingReview = true;
            return true;
        }

        private void SellWeek(ProjectState p)
        {
            long units = SalesCalculator.TailUnitsForWeek(p.forecastUnits, p.salesWeek, p.salesWeeksTotal, p.salesRetention);
            float global = _run.market != null ? _run.market.globalSalesMultiplier : 1f;
            units = (long)(units * Mathf.Max(0f, global));
            long revenue = units * Mathf.Max(1, p.price);
            p.salesWeek++;
            p.unitsSold += units;
            p.revenue += revenue;
            p.weeklyUnits.Add(units);
            _run.company.lifetimeRevenue += revenue;
            if (revenue > 0) _economy?.ApplyDelta(revenue, $"Vendas de \"{p.name}\"");
        }

        /// <summary>Receita semanal atual somando todos os jogos ainda em venda (painel financeiro).</summary>
        public long CurrentWeeklySalesRevenue()
        {
            if (_run == null) return 0;
            long sum = 0;
            for (int i = 0; i < _run.releasedProjects.Count; i++)
            {
                var p = _run.releasedProjects[i];
                if (p.IsSelling && p.weeklyUnits.Count > 0)
                    sum += p.weeklyUnits[p.weeklyUnits.Count - 1] * Mathf.Max(1, p.price);
            }
            return sum;
        }
    }
}
