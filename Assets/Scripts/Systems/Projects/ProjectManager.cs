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

        /// <summary>Aptidão do fundador nesta run (adendo 23.16), criada da base do perfil se faltar.</summary>
        public float[] FounderAptitude
        {
            get
            {
                if (_run == null) return AptitudeCalculator.Ensure(null);
                if (_run.founderAptitude == null || _run.founderAptitude.Length != ProjectState.AreaCount)
                    _run.founderAptitude = AptitudeCalculator.Ensure(null);
                return _run.founderAptitude;
            }
        }

        /// <summary>Uma pessoa que trabalha no projeto: aptidão por área e produtividade (fundador = 1).</summary>
        public readonly struct Worker
        {
            public readonly string Id;
            public readonly float[] Aptitude;
            public readonly float Productivity;

            public Worker(string id, float[] aptitude, float productivity)
            {
                Id = id; Aptitude = aptitude; Productivity = productivity;
            }
        }

        /// <summary>Quem produz no projeto: o fundador e os contratados, até o tamanho da equipe do projeto.</summary>
        public List<Worker> Workers(ProjectState project)
        {
            var list = new List<Worker>();
            if (_run == null) return list;
            list.Add(new Worker(FounderId, FounderAptitude, 1f));
            var staff = _run.company.employees;
            int limit = project != null ? Mathf.Max(1, project.teamSize) : int.MaxValue;
            for (int i = 0; i < staff.Count && list.Count < limit; i++)
                list.Add(new Worker(staff[i].id, AptitudeCalculator.Of(staff[i]), StaffCalculator.ProductivityFactor(staff[i])));
            return list;
        }

        /// <summary>Id usado para o fundador nas listas de quem produz (as bolinhas saem de cima dele).</summary>
        public const string FounderId = "founder";

        /// <summary>
        /// Pontos que cada pessoa gera numa semana no parâmetro, com a fração dada (adendo 23.16), já com
        /// os bônus de equipe pequena e de crunch (mais pontos por semana, mesma fase mais curta).
        /// </summary>
        public float WeeklyPointsFor(ProjectState project, Worker worker, GameParameter parameter, float share01, bool crunch)
        {
            return DevelopmentRules.WeeklyPoints(worker.Aptitude, parameter, share01, worker.Productivity,
                project != null ? project.teamSize : 1, crunch, BalanceProvider.Get<GameLoopBalanceSO>());
        }

        /// <summary>Previsão dos pontos que a equipe gera na fase atual com as frações dadas (só para a UI).</summary>
        public float ForecastPhasePoints(ProjectState project, float[] sharesByParameter, bool crunch)
        {
            if (project == null || sharesByParameter == null) return 0f;
            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)project.phaseIndex, project.size);
            int weeks = WeeksForPhase(project, project.phaseIndex, crunch);
            float total = 0f;
            foreach (var worker in Workers(project))
                for (int i = 0; i < parameters.Count; i++)
                    total += WeeklyPointsFor(project, worker, parameters[i], sharesByParameter[(int)parameters[i]], crunch) * weeks;
            return total;
        }

        /// <summary>Previsão dos pontos do jogo inteiro com a equipe atual e o perfil ideal em cada fase (só UI).</summary>
        public float ForecastProjectPoints(ProjectState project)
        {
            if (project == null) return 0f;
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            int saved = project.phaseIndex, savedTarget = project.targetWeeks;
            if (project.targetWeeks <= 0) project.targetWeeks = loop != null ? loop.WeeksFor(project.size) : 8;
            float total = 0f;
            for (int i = 0; i < ParameterCatalog.PhaseCount; i++)
            {
                project.phaseIndex = i;
                total += ForecastPhasePoints(project, IdealAllocation(project), false);
            }
            project.phaseIndex = saved;
            project.targetWeeks = savedTarget;
            return total;
        }

        /// <summary>Previsão de pontos da fase atual com o perfil ideal do gênero (adendo 23.16).</summary>
        public int PhaseBudget(ProjectState project)
        {
            if (_run == null || project == null) return 0;
            return Mathf.RoundToInt(ForecastPhasePoints(project, IdealAllocation(project), false));
        }

        /// <summary>Pontos esperados na fase atual para o tamanho do jogo (referência do volume, adendo 23.16).</summary>
        public float ExpectedPhasePoints(ProjectState project)
        {
            if (project == null) return 0f;
            float required = ProductionCalculator.RequiredPoints(project.size, BalanceProvider.Get<GameLoopBalanceSO>());
            return required * ParameterCatalog.DurationShare((ProductionPhase)Mathf.Clamp(project.phaseIndex, 0, ParameterCatalog.PhaseCount - 1));
        }

        /// <summary>Semanas que a fase dada leva neste projeto (adendo 23.2), com ou sem crunch.</summary>
        public int WeeksForPhase(ProjectState project, int phaseIndex, bool crunch)
        {
            if (project == null) return 1;
            return DevelopmentRules.PhaseWeeks(project.targetWeeks, phaseIndex, crunch, BalanceProvider.Get<GameLoopBalanceSO>());
        }

        private void BeginPhase(ProjectState project)
        {
            project.phaseWeek = 0;
            project.crunch = false;
            project.phasePointBudget = PhaseBudget(project);
            project.phaseWeeks = WeeksForPhase(project, project.phaseIndex, false);
            project.awaitingAllocation = true;
            UpdateWeeklyRate(project);
        }

        /// <summary>Frações (soma 1) que seguem o perfil ideal do gênero (usada no modo automático e em testes).</summary>
        public float[] IdealAllocation(ProjectState project)
        {
            var shares = new float[ParameterCatalog.ParameterCount];
            if (project == null) return shares;
            var phase = (ProductionPhase)project.phaseIndex;
            var parameters = ParameterCatalog.AvailableFor(phase, project.size);
            var ideal = QualityCalculator.IdealPhaseShares(GenreOf(project), project.size, phase);
            for (int i = 0; i < parameters.Count; i++) shares[(int)parameters[i]] = ideal[i];
            return shares;
        }

        /// <summary>
        /// Confirma a distribuição da fase atual em frações dos pontos gerados (um valor por parâmetro;
        /// só os da fase contam; normalizado para somar 1 — adendo 23.16) e põe a fase para rodar.
        /// </summary>
        public bool ConfirmAllocation(float[] sharesByParameter, bool crunch)
        {
            var p = _run?.currentProject;
            if (p == null || p.status != ProjectStatus.InDevelopment || !p.awaitingAllocation) return false;
            if (sharesByParameter == null || sharesByParameter.Length < ParameterCatalog.ParameterCount) return false;

            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)p.phaseIndex, p.size);
            float total = 0f;
            for (int i = 0; i < parameters.Count; i++) total += Mathf.Max(0f, sharesByParameter[(int)parameters[i]]);
            if (total <= 0.0001f) return false;
            for (int i = 0; i < parameters.Count; i++)
            {
                int idx = (int)parameters[i];
                p.paramAllocated[idx] = Mathf.Max(0f, sharesByParameter[idx]) / total;
            }

            p.crunch = crunch;
            p.phaseWeeks = WeeksForPhase(p, p.phaseIndex, crunch);
            p.phaseWeek = 0;
            p.awaitingAllocation = false;
            p.areaAllocation = CurrentPhaseAreaShares(p);
            UpdateWeeklyRate(p);
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
                    EventBus.Publish(new GameLaunchedEvent(released.name, released.finalScore, released.isSequel, released.hypeBacklash01));
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
            float produced = ProduceWeek(p, loop);

            p.bugCount += BugsThisWeek(p, produced, loop);
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

        /// <summary>
        /// Uma semana de produção (adendo 23.16): cada pessoa gera pontos por parâmetro pela fração
        /// alocada e pela aptidão na área, e pratica (sobe a aptidão) no que produziu.
        /// </summary>
        private float ProduceWeek(ProjectState p, GameLoopBalanceSO loop)
        {
            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)p.phaseIndex, p.size);
            float produced = 0f;
            var byArea = new float[ProjectState.AreaCount];
            foreach (var worker in Workers(p))
            {
                Array.Clear(byArea, 0, byArea.Length);
                for (int i = 0; i < parameters.Count; i++)
                {
                    int idx = (int)parameters[i];
                    float pts = WeeklyPointsFor(p, worker, parameters[i], p.paramAllocated[idx], p.crunch);
                    p.paramPoints[idx] += pts;
                    produced += pts;
                    byArea[QualityCalculator.AreaIndexOf(parameters[i])] += pts;
                }
                for (int a = 0; a < byArea.Length; a++) AptitudeCalculator.Practice(worker.Aptitude, a, byArea[a], loop);
            }
            UpdateWeeklyRate(p);
            return produced;
        }

        /// <summary>Recalcula os pontos por semana de cada parâmetro com a equipe e a aptidão atuais (UI interpola).</summary>
        public void UpdateWeeklyRate(ProjectState p)
        {
            if (p == null) return;
            p.EnsureCollections();
            Array.Clear(p.paramWeeklyRate, 0, p.paramWeeklyRate.Length);
            if (p.awaitingAllocation || p.status != ProjectStatus.InDevelopment) return;
            var parameters = ParameterCatalog.AvailableFor((ProductionPhase)p.phaseIndex, p.size);
            foreach (var worker in Workers(p))
                for (int i = 0; i < parameters.Count; i++)
                {
                    int idx = (int)parameters[i];
                    p.paramWeeklyRate[idx] += WeeklyPointsFor(p, worker, parameters[i], p.paramAllocated[idx], p.crunch);
                }
        }

        /// <summary>
        /// Treinamento (adendo 23.16): paga e sobe a aptidão da pessoa (fundador ou funcionário pelo id)
        /// na área. Falso se não há caixa, a pessoa não existe ou a área já está no máximo.
        /// </summary>
        public bool Train(string workerId, int area, out long cost)
        {
            cost = 0;
            if (_run == null || _run.ended) return false;
            float[] aptitude = null;
            if (workerId == FounderId) aptitude = FounderAptitude;
            else
                foreach (var e in _run.company.employees)
                    if (e.id == workerId) { aptitude = AptitudeCalculator.Of(e); break; }
            if (aptitude == null || area < 0 || area >= aptitude.Length) return false;
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            cost = AptitudeCalculator.TrainingCost(aptitude[area], loop);
            if (aptitude[area] >= AptitudeCalculator.Max || _run.company.cash < cost) return false;
            AptitudeCalculator.Train(aptitude, area, loop);
            if (_economy != null) _economy.ApplyDelta(-cost, "Treinamento");
            else _run.company.cash -= cost;
            UpdateWeeklyRate(_run.currentProject);
            return true;
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

        /// <summary>Divisão da fase atual somada por área (soma 1) — só os itens da fase em andamento.</summary>
        public static float[] CurrentPhaseAreaShares(ProjectState p)
        {
            var current = new float[ParameterCatalog.ParameterCount];
            if (p == null) return QualityCalculator.AreaSharesFromParameters(current);
            foreach (var gp in ParameterCatalog.AvailableFor((ProductionPhase)Mathf.Clamp(p.phaseIndex, 0, ParameterCatalog.PhaseCount - 1), p.size))
                current[(int)gp] = p.paramAllocated[(int)gp];
            return QualityCalculator.AreaSharesFromParameters(current);
        }

        private int BugsThisWeek(ProjectState p, float producedPoints, GameLoopBalanceSO loop)
        {
            var tech = new List<float[]>();
            foreach (var w in Workers(p)) tech.Add(w.Aptitude);
            float teamQuality = DevelopmentRules.TechQuality01(tech);
            float bugs = DevelopmentRules.ExpectedBugs(producedPoints, teamQuality, p.areaAllocation,
                p.phaseIndex == (int)ProductionPhase.Technology ? p.paramAllocated[(int)GameParameter.StabilityTesting] : 0f, p.crunch, loop);
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
            float priceFactor = SalesCalculator.PriceFactor(price, reference, loop);
            long wishSales = SalesCalculator.WishlistUnits(p.wishlists, qf);
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

            // Nota "quão perto do perfeito" (adendo 23.18): tema + divisão das fases, pontos e engine amenizam, bugs descontam.
            float required = ProductionCalculator.RequiredPoints(p.size, loop);
            float delivered = 0f;
            foreach (float v in p.paramPoints) delivered += Mathf.Max(0f, v);
            float coverage = required > 0f ? Mathf.Clamp01(delivered / required) : 1f;
            p.areaAllocation = QualityCalculator.AreaSharesFromParameters(p.paramPoints);
            var engineLevels = _research != null ? _research.EngineLevelsByParameter() : null;
            var breakdown = ScoreCalculator.Compute(genre, p.size, ScoreCalculator.ThemeAccuracy(theme, p.genreKey, loop),
                p.paramAllocated, p.paramPoints, required, engineLevels, p.bugCount, loop);
            float score = Mathf.Clamp(breakdown.score + FounderPerks.ReviewBonus, 0f, 10f);

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

            // Vendas semana a semana (adendo 23.19): a 1ª semana é o ponto de partida; o hype (com o bônus do
            // tema em alta) segura a queda e os bugs a aceleram.
            float trend = _market != null ? _market.ThemeTrend01(p.themeKey) : 0.5f;
            p.hype01 = Mathf.Clamp01(p.hype01 + SalesCalculator.TrendingThemeHype(trend, loop));
            float platformReach = string.IsNullOrEmpty(p.consoleId) ? 1f : SalesCalculator.PlatformReach(p.consolePopularity01);
            float bugRatio = p.bugCount / Mathf.Max(1f, delivered);

            p.scopeCoverage01 = coverage;
            p.finalScore = finalScore;
            p.firstWeekUnits = SalesCalculator.FirstWeekUnits(finalScore, p.size, p.hype01, p.wishlists, p.price, ReferencePrice(p),
                platformReach, loop, qf, SalesCalculator.HitScore(p.reviewScores, finalScore));
            p.nextWeekUnits = p.firstWeekUnits;
            float delivery = SalesCalculator.Delivery(finalScore, loop);
            p.salesDecay = SalesCalculator.WeeklyDecay(p.hype01, delivery, bugRatio, loop);
            p.hypeBacklash01 = SalesCalculator.Backlash01(p.hype01, delivery);
            p.salesEnded = p.firstWeekUnits <= 0;
            p.forecastUnits = (long)SalesCalculator.ExpectedTotalUnits(p.firstWeekUnits, p.salesDecay, loop);
            p.salesWeek = 0;
            p.unitsSold = 0;
            p.revenue = 0;
            p.weeklyUnits.Clear();
            p.releaseWeek = _run.week;
            p.releaseYear = _run.year;
            p.status = ProjectStatus.Released;
            p.fixingBugs = false;

            _run.company.gamesReleased++;
            _run.company.gamesInPhase++;
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
            // Semana a semana (adendo 23.19): vende o previsto, e a próxima semana sai desta com queda e variação.
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            float global = _run.market != null ? _run.market.globalSalesMultiplier : 1f;
            long units = (long)Mathf.Round(p.nextWeekUnits * Mathf.Max(0f, global));
            p.nextWeekUnits = SalesCalculator.NextWeekUnits(p.nextWeekUnits, p.salesDecay, UnityEngine.Random.value, loop);
            if (SalesCalculator.SalesEnded(p.nextWeekUnits, p.firstWeekUnits, loop)) p.salesEnded = true;
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
