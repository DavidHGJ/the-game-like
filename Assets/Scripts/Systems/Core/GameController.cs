using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Meta;
using GameLike.Systems.Economy;
using GameLike.Systems.Events;
using GameLike.Systems.Market;
using GameLike.Systems.Marketing;
using GameLike.Systems.Meta;
using GameLike.Systems.Projects;
using GameLike.Systems.Research;
using GameLike.Systems.Save;
using GameLike.Systems.Staff;
using GameLike.Systems.Time;
using UnityEngine;

namespace GameLike.Systems.Core
{
    /// <summary>
    /// Composition root do jogo: mantém o <see cref="RunState"/> e a meta-progressão, liga os
    /// managers ao estado e expõe as intenções que a UI dispara (nova run, novo projeto,
    /// expandir de fase, vender, aceitar oferta...). Reage a fatos de alto fan-out para
    /// recalcular valuation, oferecer a expansão de fase e detectar falência.
    /// Managers ausentes na cena são criados aqui — a cena não precisa ser remontada quando
    /// um sistema novo nasce.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        private const int ValuationHistoryCap = 520;
        private const int AutosaveEveryWeeks = 13;

        [Header("Managers (resolvidos/criados no mesmo GameObject se vazios)")]
        [SerializeField] private EconomyManager economy;
        [SerializeField] private ProjectManager projects;
        [SerializeField] private TimeManager time;
        [SerializeField] private StaffManager staff;
        [SerializeField] private MarketingManager marketing;
        [SerializeField] private MarketManager market;
        [SerializeField] private EventManager events;
        [SerializeField] private ResearchManager research;

        [Header("Conteúdo")]
        [Tooltip("Gêneros disponíveis. Se vazio, carrega de Resources/Genres.")]
        [SerializeField] private List<GenreSO> genres = new List<GenreSO>();
        [Tooltip("Temas disponíveis. Se vazio, carrega de Resources/Themes.")]
        [SerializeField] private List<ThemeSO> themes = new List<ThemeSO>();
        [Tooltip("Consoles/plataformas. Se vazio, carrega de Resources/Consoles.")]
        [SerializeField] private List<ConsoleSO> consoles = new List<ConsoleSO>();

        [Header("Identidade inicial")]
        [SerializeField] private string defaultCompanyName = "Minha Dev";

        [Header("Save")]
        [SerializeField] private int autosaveSlot = 0;

        [Header("Regras provisórias (validar)")]
        [Tooltip("Semanas seguidas com caixa negativo antes da falência (GDD 20.4: 'sem capacidade de recuperação').")]
        [SerializeField] private int bankruptcyGraceWeeks = 4;
        [Tooltip("Prêmio do IPO sobre o valuation (GDD não define; provisório).")]
        [SerializeField] private float ipoPremium = 1.5f;

        private RunState _run;
        private int _nextRunNumber = 1;
        private MetaProgressionState _meta = new MetaProgressionState();
        private RivalManager _rivals;
        private TimelineManager _timeline;
        private AchievementManager _achievements;
        private long _weekIncome, _weekExpense;
        private int _weeksSinceAutosave;
        private readonly List<string> _genreKeys = new List<string>();

        public RunState Run => _run;
        public MetaProgressionState Meta => _meta;
        public EconomyManager Economy => economy;
        public TimeManager Time => time;
        public ProjectManager Projects => projects;
        public StaffManager Staff => staff;
        public MarketingManager Marketing => marketing;
        public MarketManager Market => market;
        public EventManager Events => events;
        public ResearchManager Research => research;
        public RivalManager Rivals => _rivals;
        public IReadOnlyList<GenreSO> AvailableGenres => genres;
        public IReadOnlyList<ThemeSO> AvailableThemes => themes;
        public IReadOnlyList<ConsoleSO> AllConsoles => consoles;
        public int BankruptcyGraceWeeks
        {
            get
            {
                var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
                return tuning != null ? tuning.BankruptcyGraceWeeks : bankruptcyGraceWeeks;
            }
        }

        private float IpoPremium
        {
            get
            {
                var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
                return tuning != null ? tuning.IpoPremium : ipoPremium;
            }
        }

        /// <summary>
        /// Consoles já lançados no ano atual da run (GDD 15.2), ordenados por ano de
        /// lançamento. É a lista que a tela de novo projeto oferece como plataforma.
        /// </summary>
        public List<ConsoleSO> ConsolesAvailableNow()
        {
            int year = time != null ? time.CurrentYear : 1;
            var result = new List<ConsoleSO>();
            for (int i = 0; i < consoles.Count; i++)
            {
                var c = consoles[i];
                if (c != null && c.IsAvailable(year)) result.Add(c);
            }
            result.Sort((a, b) => a.ReleaseYear.CompareTo(b.ReleaseYear));
            return result;
        }

        public GenreSO GenreById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < genres.Count; i++) if (genres[i] != null && genres[i].Id == id) return genres[i];
            return null;
        }

        public ThemeSO ThemeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < themes.Count; i++) if (themes[i] != null && themes[i].Id == id) return themes[i];
            return null;
        }

        private T Ensure<T>(T current) where T : Component
        {
            if (current != null) return current;
            var found = GetComponent<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        private void Awake()
        {
            economy = Ensure(economy);
            projects = Ensure(projects);
            time = Ensure(time);
            staff = Ensure(staff);
            marketing = Ensure(marketing);
            market = Ensure(market);
            events = Ensure(events);
            research = Ensure(research);
            _rivals = Ensure(_rivals);
            _timeline = Ensure(_timeline);
            _achievements = Ensure(_achievements);

            if (genres == null || genres.Count == 0)
                genres = new List<GenreSO>(Resources.LoadAll<GenreSO>("Genres"));
            if (themes == null || themes.Count == 0)
                themes = new List<ThemeSO>(Resources.LoadAll<ThemeSO>("Themes"));
            if (consoles == null || consoles.Count == 0)
                consoles = new List<ConsoleSO>(Resources.LoadAll<ConsoleSO>("Consoles"));

            _genreKeys.Clear();
            for (int i = 0; i < genres.Count; i++) if (genres[i] != null) _genreKeys.Add(genres[i].name);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameLaunchedEvent>(OnGameLaunched);
            EventBus.Subscribe<WeekTickEvent>(OnWeek);
            EventBus.Subscribe<MoneyChangedEvent>(OnMoney);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLaunchedEvent>(OnGameLaunched);
            EventBus.Unsubscribe<WeekTickEvent>(OnWeek);
            EventBus.Unsubscribe<MoneyChangedEvent>(OnMoney);
        }

        private void Start()
        {
            if (_run != null) return;

            // O jogo abre no menu (GDD 20.1). A cena de gameplay lê a intenção escolhida ali.
            switch (GameBoot.Mode)
            {
                case GameBoot.BootMode.Continue:
                    if (LoadGame(GameBoot.Slot)) { GameBoot.Clear(); return; }
                    goto default; // save ausente: cai no fallback de nova run
                case GameBoot.BootMode.NewRun:
                    // "Novo jogo" no menu recomeça do zero, incluindo a meta-progressão (GDD 20.1).
                    _meta = new MetaProgressionState();
                    _nextRunNumber = 1;
                    StartNewRun(GameBoot.CompanyName);
                    GameBoot.Clear();
                    return;
                default:
                    // Sem intent (dev abriu a cena direto no editor): continua o save se existir,
                    // senão inicia uma run padrão para a cena não ficar vazia.
                    if (!LoadMetaOnly(autosaveSlot)) _meta = new MetaProgressionState();
                    StartNewRun(defaultCompanyName);
                    return;
            }
        }

        private bool LoadMetaOnly(int slot)
        {
            var data = SaveManager.Load(slot);
            if (data?.meta == null) return false;
            _meta = data.meta;
            _meta.EnsureCollections();
            if (data.run != null) _nextRunNumber = Mathf.Max(_nextRunNumber, data.run.runNumber + (data.run.ended ? 1 : 0));
            return true;
        }

        // ------------------------------------------------------------------ run

        /// <summary>
        /// Ferramenta de dev/teste: recomeça do zero (meta incluída) gravando em outro slot,
        /// para pilotos automáticos e testes não tocarem no save do jogador.
        /// </summary>
        public void StartFreshForTesting(int slot)
        {
            autosaveSlot = slot;
            _meta = new MetaProgressionState();
            _nextRunNumber = 1;
            StartNewRun(defaultCompanyName);
        }

        /// <summary>
        /// Inicia uma nova run. <paramref name="capitalFromPersonal"/> é quanto do dinheiro pessoal
        /// do fundador vira capital inicial (GDD 2.2); negativo = leva tudo.
        /// </summary>
        public void StartNewRun(string companyName, long capitalFromPersonal = -1)
        {
            _meta.EnsureCollections();
            FounderPerks.Bind(_meta);

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            long baseCash = loop != null ? loop.StartingCash : 50_000;
            long capital = capitalFromPersonal < 0
                ? _meta.personalMoney
                : System.Math.Min(capitalFromPersonal, _meta.personalMoney);
            capital = System.Math.Max(0, capital);
            _meta.personalMoney -= capital;
            long startingCash = baseCash + capital;

            var company = new CompanyState(
                string.IsNullOrWhiteSpace(companyName) ? defaultCompanyName : companyName,
                startingCash);
            company.reputation01 = Mathf.Clamp01(company.reputation01 + FounderPerks.StartingReputationBonus);
            var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
            float spread = tuning != null ? tuning.RunCostVariationPercent / 100f : 0f;
            company.costVariation = 1f + UnityEngine.Random.Range(-spread, spread); // GDD 23.12

            _run = new RunState(company) { runNumber = _nextRunNumber++, capitalInvested = startingCash };
            _weekIncome = _weekExpense = 0;
            _weeksSinceAutosave = 0;
            if (time != null)
            {
                time.ClearLocks();
                time.SetDate(1, 1);
                time.Resume();
            }
            BindManagers();
            RecomputeValuation();
        }

        /// <summary>Define nome e aparência do fundador (GDD 20.2).</summary>
        public void SetFounder(string founderName, int skin, int hair, int hairStyle, int shirt, int accessory)
        {
            _meta.EnsureCollections();
            var f = _meta.founder;
            f.created = true;
            f.name = string.IsNullOrWhiteSpace(founderName) ? "Fundador" : founderName.Trim();
            f.skin = skin; f.hair = hair; f.hairStyle = hairStyle; f.shirt = shirt; f.accessory = accessory;
            var crew = FindFirstObjectByType<GameLike.Systems.Office.OfficeCrew>();
            if (crew != null) crew.Refresh(); // o boneco 3D passa a ter a aparência escolhida
        }

        /// <summary>Renomeia a empresa da run atual (usado na criação do fundador).</summary>
        public void RenameCompany(string companyName)
        {
            if (_run != null && !string.IsNullOrWhiteSpace(companyName)) _run.company.name = companyName.Trim();
        }

        /// <summary>Liga todos os managers ao estado da run atual.</summary>
        private void BindManagers()
        {
            FounderPerks.Bind(_meta);
            economy?.Bind(_run.company);
            market?.Bind(_run);
            research?.Bind(_run, economy, themes);
            projects?.Bind(_run, economy, market, research, GenreById, ThemeById);
            staff?.Bind(_run, economy);
            marketing?.Bind(_run, economy, research);
            events?.Bind(_run, economy, time, market);
            _rivals?.Bind(_run, economy, _meta, research, staff, _genreKeys);
            _timeline?.Bind(_run, consoles);
            _achievements?.Bind(_run, _meta);
        }

        /// <summary>Salva a run + meta atuais no slot de autosave (GDD 20.6).</summary>
        public void AutoSave()
        {
            if (_run == null) return;
            _weeksSinceAutosave = 0;
            SaveManager.Save(autosaveSlot, _run, _meta);
        }

        /// <summary>Carrega a run + meta de um slot e religa os managers. Retorna false se não há save.</summary>
        public bool LoadGame(int slot)
        {
            var data = SaveManager.Load(slot);
            if (data == null || data.run == null) return false;
            _run = data.run;
            _run.EnsureCollections();
            if (data.meta != null) _meta = data.meta;
            _meta.EnsureCollections();
            _nextRunNumber = Mathf.Max(_nextRunNumber, _run.runNumber + 1);
            if (time != null)
            {
                time.ClearLocks();
                time.SetDate(_run.week, _run.year);
            }
            BindManagers();
            RecomputeValuation();
            return true;
        }

        // ------------------------------------------------------------------ saídas (GDD 2.4)

        /// <summary>Verdadeiro se o IPO está liberado: fase Estúdio + pesquisa (GDD 3.2).</summary>
        public bool CanIpo =>
            _run != null && !_run.ended && _run.company.phase == CompanyPhase.Studio
            && research != null && research.IsUnlocked(ResearchCatalog.FeatureIpo);

        /// <summary>Quanto a venda voluntária paga agora (o valuation do dia).</summary>
        public long SaleValue => _run != null ? RecomputeValuation() : 0;

        /// <summary>Quanto o IPO pagaria agora.</summary>
        public long IpoValue => _run != null ? (long)(RecomputeValuation() * IpoPremium) : 0;

        /// <summary>Verdadeiro se vender agora devolve menos do que foi investido (aviso de prejuízo, GDD 2.4).</summary>
        public bool SaleIsLoss => _run != null && SaleValue < _run.capitalInvested;

        /// <summary>Venda voluntária da empresa pelo valuation atual — encerra a run.</summary>
        public void SellCompany()
        {
            if (_run == null || _run.ended) return;
            EndRun(RunEndReason.MarketSale, RecomputeValuation());
        }

        /// <summary>Abre o capital (IPO) — encerra a run com prêmio sobre o valuation.</summary>
        public bool DoIpo()
        {
            if (!CanIpo) return false;
            EndRun(RunEndReason.IPO, IpoValue);
            return true;
        }

        /// <summary>Aceita a proposta de aquisição pendente de um rival — encerra a run (GDD 12.1).</summary>
        public bool AcceptOffer()
        {
            if (_run == null || _run.ended || !_run.pendingOffer.active) return false;
            long price = _run.pendingOffer.price;
            _run.pendingOffer.active = false;
            EndRun(RunEndReason.Acquisition, price);
            return true;
        }

        /// <summary>Recusa a proposta de aquisição pendente (o rival fica mais hostil).</summary>
        public void DeclineOffer() => _rivals?.DeclineOffer();

        /// <summary>
        /// Encerra a run por qualquer motivo: calcula o dinheiro pessoal recebido e os Pontos de
        /// Legado (GDD 19.5), acumula na meta-progressão, registra a empresa como futura rival
        /// e anuncia o fato.
        /// </summary>
        private void EndRun(RunEndReason reason, long cashOut)
        {
            if (_run == null || _run.ended) return;

            bool bankrupt = reason == RunEndReason.Bankruptcy;
            long finalValue = bankrupt ? 0 : System.Math.Max(0, cashOut);

            _run.ended = true;
            _run.salePrice = finalValue;
            _run.pendingOffer.active = false;

            int legacy = LegacyCalculator.Compute(_run, bankrupt);
            _meta.EnsureCollections();
            _meta.personalMoney += finalValue;
            _meta.legacyPoints += legacy;
            _meta.runsCompleted++;
            if (finalValue > _meta.bestSale) _meta.bestSale = finalValue;
            _meta.pastCompanies.Add(new PastCompany
            {
                name = _run.company.name, runNumber = _run.runNumber, phase = _run.company.phase,
                finalValuation = System.Math.Max(_run.company.valuation, finalValue), bankrupt = bankrupt
            });

            time?.Pause();
            if (!bankrupt)
            {
                var saleType = reason == RunEndReason.IPO ? SaleType.IPO
                    : reason == RunEndReason.Acquisition ? SaleType.RivalAcquisition : SaleType.MarketSale;
                EventBus.Publish(new CompanySoldEvent(finalValue, saleType));
            }
            EventBus.Publish(new RunEndedEvent(reason, finalValue, legacy, _meta.personalMoney));
            AutoSave(); // persiste a meta-progressão ao fim da run (GDD 20.6)
        }

        // ------------------------------------------------------------------ projetos

        /// <summary>Sequências exigem a pesquisa de Gestão de Franquias (GDD 13.2).</summary>
        public bool CanMakeSequels => research == null || research.IsUnlocked(ResearchCatalog.FeatureFranchise);

        /// <summary>Custo da licença de um console já com o desconto do fundador.</summary>
        public long LicenseCost(ConsoleSO console) =>
            console == null ? 0 : (long)(console.LicenseCost * FounderPerks.LicenseCostMultiplier);

        /// <summary>
        /// Cria e inicia um projeto a partir do conceito escolhido na UI (GDD 5.2 fase 1). A
        /// primeira fase de produção fica aguardando a distribuição de pontos.
        /// </summary>
        public bool StartProject(string projectName, GenreSO genre, ProjectSize size, ThemeSO theme,
            string parentProjectId, ConsoleSO console)
        {
            return CreateProject(projectName, genre, size, theme, parentProjectId, console, false);
        }

        /// <summary>
        /// Atalho com distribuição automática (segue o perfil ideal do gênero em todas as
        /// fases e lança sozinho ao terminar). Usado por testes e ferramentas de dev.
        /// </summary>
        public void StartProject(string projectName, GenreSO genre, ProjectSize size, float[] allocation,
            ThemeSO theme = null, string parentProjectId = null, ConsoleSO console = null)
        {
            CreateProject(projectName, genre, size, theme, parentProjectId, console, true);
        }

        private bool CreateProject(string projectName, GenreSO genre, ProjectSize size, ThemeSO theme,
            string parentProjectId, ConsoleSO console, bool autoAllocate)
        {
            if (_run == null || _run.ended || genre == null || projects == null) return false;
            if (!projects.CanStartProject) return false;

            var project = new ProjectState(
                string.IsNullOrWhiteSpace(projectName) ? "Sem título" : projectName.Trim(),
                genre.Id, genre.DisplayLabel, size, null)
            {
                genreKey = genre.name,
                autoAllocate = autoAllocate
            };

            if (theme != null)
            {
                project.themeId = theme.Id;
                project.themeDisplay = theme.DisplayLabel;
                project.themeKey = theme.name;
            }

            // Plataforma (GDD 15): cobra a licença e captura a popularidade do console no ano atual.
            int year = time != null ? time.CurrentYear : 1;
            if (console != null && console.IsAvailable(year))
            {
                project.consoleId = console.Id;
                project.consoleDisplay = console.DisplayLabel;
                project.consoleKey = console.name;
                project.consolePopularity01 = console.PopularityAtYear(year);
                long license = LicenseCost(console);
                if (license > 0) economy?.ApplyDelta(-license, $"Licença de {console.DisplayLabel}");
            }

            // Sequência (GDD 13): vincula a um jogo anterior do catálogo e entra na franquia.
            var parent = CanMakeSequels || autoAllocate ? FindReleased(parentProjectId) : null;
            if (parent != null)
            {
                project.isSequel = true;
                project.parentProjectId = parent.id;
                project.franchiseId = string.IsNullOrEmpty(parent.franchiseId) ? parent.id : parent.franchiseId;
            }

            projects.StartProject(genre, theme, project);
            return true;
        }

        private ProjectState FindReleased(string projectId)
        {
            if (_run == null || string.IsNullOrEmpty(projectId)) return null;
            for (int i = 0; i < _run.releasedProjects.Count; i++)
                if (_run.releasedProjects[i].id == projectId) return _run.releasedProjects[i];
            return null;
        }

        // ------------------------------------------------------------------ reações

        private void OnGameLaunched(GameLaunchedEvent e)
        {
            if (_run == null) return;
            // Reputação acompanha a recepção do jogo (GDD 4.1).
            var c = _run.company;
            c.reputation01 = Mathf.Clamp01(c.reputation01 + (e.Score - 6f) * 0.03f);
            RecomputeValuation();
            CheckExpansion();
            AutoSave(); // autosave em momento importante (GDD 20.6)
        }

        private void OnMoney(MoneyChangedEvent e)
        {
            if (e.Delta >= 0) _weekIncome += e.Delta; else _weekExpense -= e.Delta;
        }

        private void OnWeek(WeekTickEvent e)
        {
            if (_run == null || _run.ended) return;
            var c = _run.company;
            _run.week = e.Week;
            _run.year = e.Year;
            c.lastWeekIncome = _weekIncome;
            c.lastWeekExpense = _weekExpense;
            _weekIncome = _weekExpense = 0;

            if (c.expansionAvailable) c.stagnationWeeks++;
            long valuation = RecomputeValuation();
            c.valuationHistory.Add(valuation);
            if (c.valuationHistory.Count > ValuationHistoryCap) c.valuationHistory.RemoveAt(0);
            CheckExpansion();

            // Falência: caixa negativo por semanas seguidas encerra a run (GDD 2.4 / 20.4).
            c.debtWeeks = c.cash < 0 ? c.debtWeeks + 1 : 0;
            if (c.debtWeeks > BankruptcyGraceWeeks)
            {
                EndRun(RunEndReason.Bankruptcy, 0);
                return;
            }

            if (++_weeksSinceAutosave >= AutosaveEveryWeeks) AutoSave();
        }

        private long RecomputeValuation()
        {
            if (_run == null) return 0;
            var c = _run.company;
            var economyBalance = BalanceProvider.Get<EconomyBalanceSO>();
            double valuation = ValuationCalculator.Compute(c, _run.releasedProjects) * FounderPerks.ValuationMultiplier;

            // Estagnação: expansão disponível e não feita corrói o valuation (GDD 2.1 / adendo 23.8).
            if (c.expansionAvailable && economyBalance != null)
            {
                float loss = Mathf.Min(economyBalance.StagnationLossMax,
                    c.stagnationWeeks * economyBalance.StagnationLossPerWeek * FounderPerks.StagnationMultiplier);
                valuation *= 1f - loss;
            }

            c.valuation = (long)System.Math.Max(0, valuation);
            if (c.valuation > c.peakValuation) c.peakValuation = c.valuation;
            return c.valuation;
        }

        // ------------------------------------------------------------------ expansão de fase (GDD 2.1)

        /// <summary>Fase seguinte à atual, ou a própria fase se já é a última.</summary>
        public CompanyPhase NextPhase =>
            _run == null || _run.company.phase == CompanyPhase.Studio ? CompanyPhase.Studio : _run.company.phase + 1;

        /// <summary>Custo para expandir para a próxima fase.</summary>
        public long ExpansionCost
        {
            get
            {
                var economyBalance = BalanceProvider.Get<EconomyBalanceSO>();
                return economyBalance != null && _run != null ? economyBalance.ExpansionCostTo(NextPhase) : 0;
            }
        }

        /// <summary>Valuation necessário para liberar a próxima expansão (0 se já está no Estúdio).</summary>
        public long NextPhaseThreshold
        {
            get
            {
                var economyBalance = BalanceProvider.Get<EconomyBalanceSO>();
                return economyBalance != null && _run != null ? economyBalance.ThresholdAfter(_run.company.phase) : 0;
            }
        }

        private void CheckExpansion()
        {
            if (_run == null) return;
            var c = _run.company;
            if (c.expansionAvailable || c.phase == CompanyPhase.Studio) return;
            long threshold = NextPhaseThreshold;
            if (threshold <= 0 || c.valuation < threshold) return;
            c.expansionAvailable = true;
            c.stagnationWeeks = 0;
            EventBus.Publish(new PhaseExpansionAvailableEvent(NextPhase, ExpansionCost));
        }

        /// <summary>Paga a expansão e muda de fase. Falso se não está disponível ou falta caixa.</summary>
        public bool ExpandPhase()
        {
            if (_run == null || _run.ended) return false;
            var c = _run.company;
            if (!c.expansionAvailable || c.phase == CompanyPhase.Studio) return false;
            long cost = ExpansionCost;
            if (c.cash < cost) return false;

            CompanyPhase from = c.phase;
            if (cost > 0) economy?.ApplyDelta(-cost, "Expansão da empresa");
            c.phase = NextPhase;
            c.expansionAvailable = false;
            c.stagnationWeeks = 0;
            EventBus.Publish(new PhaseChangedEvent(from, c.phase));
            RecomputeValuation();
            CheckExpansion();
            AutoSave();
            return true;
        }

        /// <summary>Estimativa do custo fixo semanal atual (folha + operação + pesquisa + engine).</summary>
        public long WeeklyBurn
        {
            get
            {
                if (_run == null) return 0;
                var economyBalance = BalanceProvider.Get<EconomyBalanceSO>();
                long burn = economyBalance != null ? economyBalance.WeeklyOperatingFor(_run.company.phase) : 0;
                if (staff != null) burn += staff.WeeklyPayroll;
                if (_run.research != null) burn += _run.research.activeWeeklyCost;
                if (_run.IsDeveloping && research != null) burn += research.EngineWeeklyCost;
                return burn;
            }
        }
    }
}
