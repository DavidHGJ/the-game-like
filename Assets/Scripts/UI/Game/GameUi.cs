using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Core;
using GameLike.Systems.Market;
using GameLike.Systems.Projects;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>
    /// UI viva do jogo. Nasce sozinha em runtime quando a cena carregada tem um
    /// <see cref="GameController"/> (não precisa estar na cena nem de wiring no Inspector).
    /// Alimenta o HUD aprovado (<see cref="AnimatedHud"/>) com o estado real, abre as telas
    /// de decisão na hora certa (alocação de fase, lançamento, eventos, expansão, fim de run),
    /// trava o tempo enquanto uma tela está aberta e mostra avisos/sons dos fatos do jogo.
    /// Não contém regra de gameplay: só lê estado e dispara intenções nos managers.
    /// </summary>
    public sealed class GameUi : MonoBehaviour
    {
        private const string PanelResource = "HudPanelSettings";
        private const string MenuScene = "MainMenu";

        private sealed class Toast { public VisualElement ve; public float age, life; }

        private AnimatedHud _hud;
        private readonly HudLive _live = new HudLive();
        private UIDocument _screenDoc, _toastDoc;
        private VisualElement _screenRoot, _toastRoot, _toastColumn;
        private GalleryScreen _screen;
        private bool _locked, _ready;
        private readonly List<Toast> _toasts = new List<Toast>();
        private ProjectState _pendingReview;
        private string _launchPromptedFor, _allocationPromptedFor;
        private bool _expansionPrompted, _endShown, _founderAsked;
        private InputAction _pauseAction, _menuAction;

        /// <summary>O composition root do jogo — as telas leem estado e disparam intenções por aqui.</summary>
        public GameController Gc { get; private set; }

        // ------------------------------------------------------------------ nascimento

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded += (_, __) => TryAttach();
            TryAttach();
        }

        private static void TryAttach()
        {
            var gc = FindFirstObjectByType<GameController>();
            if (gc == null || FindFirstObjectByType<GameUi>() != null) return;
            var go = new GameObject("[GameUi]");
            go.AddComponent<GameUi>().Gc = gc;
        }

        private void Awake()
        {
            var panel = Resources.Load<PanelSettings>(PanelResource);
            if (panel == null)
            {
                Debug.LogError("[GameUi] PanelSettings '" + PanelResource + "' não encontrado em Resources.");
                enabled = false;
                return;
            }

            var hudGo = MakeDoc("HUD", panel, 2, out _);
            _hud = hudGo.AddComponent<AnimatedHud>();
            _hud.Live = _live;
            MakeDoc("Telas", panel, 10, out _screenDoc);
            MakeDoc("Avisos", panel, 20, out _toastDoc);

            Sfx.Ensure();
            Kit.UiSound = kind => Sfx.Play(kind == 0 ? SfxId.Hover : SfxId.Click);
            WireHud();

            _pauseAction = new InputAction("Pausar", binding: "<Keyboard>/space");
            _menuAction = new InputAction("Menu", binding: "<Keyboard>/escape");
            _pauseAction.performed += _ => { if (_screen == null && Gc != null && Gc.Time != null) Gc.Time.TogglePause(); };
            _menuAction.performed += _ => OnEscape();
        }

        private GameObject MakeDoc(string name, PanelSettings panel, int sorting, out UIDocument doc)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = sorting;
            return go;
        }

        private void OnEnable()
        {
            _pauseAction?.Enable();
            _menuAction?.Enable();
            EventBus.Subscribe<MoneyChangedEvent>(OnMoney);
            EventBus.Subscribe<WeekTickEvent>(OnWeek);
            EventBus.Subscribe<GameLaunchedEvent>(OnLaunched);
            EventBus.Subscribe<ResearchCompletedEvent>(OnResearchDone);
            EventBus.Subscribe<ResearchPointsChangedEvent>(OnResearchPoints);
            EventBus.Subscribe<EmployeeHiredEvent>(OnHired);
            EventBus.Subscribe<EmployeeLeftEvent>(OnLeft);
            EventBus.Subscribe<EmployeePromotedEvent>(OnPromoted);
            EventBus.Subscribe<RivalGameReleasedEvent>(OnRivalGame);
            EventBus.Subscribe<ConsoleReleasedEvent>(OnConsole);
            EventBus.Subscribe<GlobalMarketEvent>(OnGlobal);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievement);
            EventBus.Subscribe<PhaseExpansionAvailableEvent>(OnExpansionAvailable);
            EventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<ProjectReadyToLaunchEvent>(OnReady);
            EventBus.Subscribe<ProductionPhaseStartedEvent>(OnPhaseStarted);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Subscribe<NarrativeEventTriggeredEvent>(OnNarrative);
            EventBus.Subscribe<AcquisitionOfferReceivedEvent>(OnOffer);
        }

        private void OnDisable()
        {
            _pauseAction?.Disable();
            _menuAction?.Disable();
            EventBus.Unsubscribe<MoneyChangedEvent>(OnMoney);
            EventBus.Unsubscribe<WeekTickEvent>(OnWeek);
            EventBus.Unsubscribe<GameLaunchedEvent>(OnLaunched);
            EventBus.Unsubscribe<ResearchCompletedEvent>(OnResearchDone);
            EventBus.Unsubscribe<ResearchPointsChangedEvent>(OnResearchPoints);
            EventBus.Unsubscribe<EmployeeHiredEvent>(OnHired);
            EventBus.Unsubscribe<EmployeeLeftEvent>(OnLeft);
            EventBus.Unsubscribe<EmployeePromotedEvent>(OnPromoted);
            EventBus.Unsubscribe<RivalGameReleasedEvent>(OnRivalGame);
            EventBus.Unsubscribe<ConsoleReleasedEvent>(OnConsole);
            EventBus.Unsubscribe<GlobalMarketEvent>(OnGlobal);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievement);
            EventBus.Unsubscribe<PhaseExpansionAvailableEvent>(OnExpansionAvailable);
            EventBus.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<ProjectReadyToLaunchEvent>(OnReady);
            EventBus.Unsubscribe<ProductionPhaseStartedEvent>(OnPhaseStarted);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Unsubscribe<NarrativeEventTriggeredEvent>(OnNarrative);
            EventBus.Unsubscribe<AcquisitionOfferReceivedEvent>(OnOffer);
        }

        private void OnDestroy()
        {
            if (_locked && Gc != null && Gc.Time != null) Gc.Time.RemoveLock();
            Kit.UiSound = null;
        }

        private void WireHud()
        {
            var a = _hud.Actions;
            a.Pause = () => Gc.Time.Pause();
            a.Play = () => { Gc.Time.SetSpeedIndex(0); Gc.Time.Resume(); };
            a.Fast = () => { Gc.Time.SetSpeedIndex(Gc.Time.SpeedCount - 1); Gc.Time.Resume(); };
            a.NewProject = OpenNewProject;
            a.Project = OpenProjectContext;
            a.Research = () => OpenPanel(4);
            a.Money = () => OpenPanel(0);
            a.Panel = OpenPanel;
            a.Menu = () => Open(new PauseLive());
        }

        // ------------------------------------------------------------------ ciclo

        private void Update()
        {
            if (Gc == null || Gc.Run == null) return;
            if (!_ready)
            {
                if (_screenDoc.rootVisualElement == null || _toastDoc.rootVisualElement == null) return;
                _screenRoot = _screenDoc.rootVisualElement;
                _screenRoot.pickingMode = PickingMode.Ignore;
                _screenRoot.style.flexGrow = 1;
                _toastRoot = _toastDoc.rootVisualElement;
                _toastRoot.pickingMode = PickingMode.Ignore;
                _toastRoot.style.flexGrow = 1;
                _toastColumn = new VisualElement();
                _toastColumn.style.position = Position.Absolute;
                _toastColumn.style.right = 16; _toastColumn.style.top = 236; _toastColumn.style.width = 340;
                _toastColumn.pickingMode = PickingMode.Ignore;
                _toastRoot.Add(_toastColumn);
                _ready = true;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            FillLive();
            if (_screen != null) _screen.Tick(dt);
            else CheckPrompts();
            TickToasts(dt);
        }

        private void FillLive()
        {
            var run = Gc.Run;
            var c = run.company;
            var time = Gc.Time;
            _live.companyLine = c.name + " · " + Labels.Phase(c.phase);
            _live.cash = c.cash;
            _live.valuation = c.valuation;
            int h = c.valuationHistory.Count;
            _live.valuationRising = h < 5 || c.valuation >= c.valuationHistory[h - 5];
            _live.week = time.CurrentWeek;
            _live.year = time.CurrentYear;
            _live.weekProgress01 = time.WeekProgress01;
            _live.paused = time.IsPaused;
            _live.speedIndex = time.SpeedIndex;
            _live.secondsPerWeek = time.SecondsPerWeek;
            _live.team = c.EffectiveTeamSize;
            _live.research = run.research != null ? run.research.points : 0;

            var p = run.currentProject;
            _live.hasProject = run.HasActiveProject;
            _live.alert = "";
            _live.needsAttention = false;
            if (!_live.hasProject)
            {
                _live.title = "Sem projeto";
                _live.subtitle = run.ended ? "Run encerrada" : "Clique em + para criar um jogo";
                _live.bugs = 0;
                _live.hype100 = 0f;
            }
            else
            {
                _live.title = p.name;
                _live.subtitle = p.genreDisplay + (string.IsNullOrEmpty(p.themeDisplay) ? "" : " · " + p.themeDisplay)
                                 + (string.IsNullOrEmpty(p.consoleDisplay) ? "" : " · " + p.consoleDisplay);
                _live.bugs = p.bugCount;
                _live.hype100 = p.hype01 * 100f;
                _live.phaseIndex = Mathf.Clamp(p.phaseIndex, 0, ParameterCatalog.PhaseCount - 1);
                bool developing = run.IsDeveloping;
                float frac = developing && !time.IsPaused ? time.WeekProgress01 : 0f;
                var phaseParams = ParameterCatalog.ParametersOf((ProductionPhase)_live.phaseIndex);
                for (int i = 0; i < HudLive.ParameterCount; i++)
                {
                    _live.paramVisible[i] = ParameterCatalog.IsAvailable((GameParameter)i, p.size);
                    float pts = p.paramPoints[i];
                    if (developing && phaseParams.Contains((GameParameter)i))
                        pts = Mathf.Min(p.paramAllocated[i], pts + frac * p.paramAllocated[i] / Mathf.Max(1, p.phaseWeeks));
                    _live.paramPoints[i] = pts;
                }

                if (p.status == ProjectStatus.ReadyToLaunch)
                {
                    _live.phaseProgress01 = 1f;
                    _live.progressCaption = p.fixingBugs ? "Corrigindo bugs" : "Pronto para lançar";
                    _live.progressLabel = p.weeksWaiting + " sem. parado";
                    _live.alert = p.fixingBugs ? "Corrigindo bugs — clique para lançar" : "Jogo pronto! Clique para lançar";
                    _live.needsAttention = true;
                }
                else if (p.awaitingAllocation)
                {
                    _live.phaseProgress01 = 0f;
                    _live.progressCaption = "Aguardando você";
                    _live.progressLabel = ParameterCatalog.PhaseShortLabel((ProductionPhase)_live.phaseIndex);
                    _live.alert = "Distribua os pontos de " + ParameterCatalog.PhaseLabel((ProductionPhase)_live.phaseIndex);
                    _live.needsAttention = true;
                }
                else
                {
                    _live.phaseProgress01 = Mathf.Clamp01((p.phaseWeek + frac) / Mathf.Max(1, p.phaseWeeks));
                    _live.progressCaption = p.crunch ? "Fase em crunch" : "Progresso da fase";
                    _live.progressLabel = "Semana " + Mathf.Min(p.phaseWeek + 1, p.phaseWeeks) + " / " + p.phaseWeeks;
                }
            }

            FillSales(run);

            if (c.debtWeeks > 0 && !run.ended)
                _live.alert = "Caixa negativo! Falência em " + Mathf.Max(0, Gc.BankruptcyGraceWeeks - c.debtWeeks + 1) + " semanas";
            else if (_live.alert.Length == 0 && c.expansionAvailable)
                _live.alert = "Expansão disponível — clique para ver";
        }

        /// <summary>
        /// Gráfico de vendas do HUD: o jogo lançado mais recente que ainda vende. Antes das
        /// críticas (e antes da primeira semana de vendas) o card aparece vazio, avisando.
        /// </summary>
        private void FillSales(RunState run)
        {
            _live.salesBars.Clear();
            _live.salesAwaitingReview = false;
            _live.salesTitle = "";
            _live.salesCaption = "";
            ProjectState shown = null;
            int others = 0;
            for (int i = run.releasedProjects.Count - 1; i >= 0; i--)
            {
                var g = run.releasedProjects[i];
                if (!g.awaitingReview && !g.IsSelling) continue;
                if (shown == null) shown = g; else others++;
            }
            if (shown == null || run.ended) return;

            _live.salesTitle = "Vendas · " + shown.name;
            if (shown.awaitingReview)
            {
                _live.salesAwaitingReview = true;
                _live.salesCaption = "Críticas saem na próxima semana";
                if (_live.alert.Length == 0) _live.alert = "\"" + shown.name + "\" foi lançado — as críticas saem na próxima semana";
                return;
            }
            for (int i = 0; i < shown.weeklyUnits.Count; i++) _live.salesBars.Add(shown.weeklyUnits[i]);
            if (shown.weeklyUnits.Count == 0)
            {
                _live.salesAwaitingReview = true;
                _live.salesCaption = "Vendas começam na próxima semana";
                return;
            }
            long last = shown.weeklyUnits[shown.weeklyUnits.Count - 1];
            _live.salesCaption = "Sem. " + shown.salesWeek + " · " + Kit.Short(last) + " un. · " + Kit.Money(last * Mathf.Max(1, shown.price))
                                 + (others > 0 ? "  (+" + others + ")" : "");
        }

        private void OnWeek(WeekTickEvent e)
        {
            var released = Gc.Run.releasedProjects;
            for (int i = 0; i < released.Count; i++)
                if (released[i].IsSelling) { Sfx.Play(SfxId.Coin); return; }
        }

        /// <summary>Abre automaticamente a tela que o jogo está pedindo (em ordem de prioridade).</summary>
        private void CheckPrompts()
        {
            var run = Gc.Run;
            if (run.ended)
            {
                if (!_endShown) { _endShown = true; Open(new RunResultLive()); }
                return;
            }
            _endShown = false;

            if (!_founderAsked && !Gc.Meta.founder.created)
            {
                _founderAsked = true;
                Open(new FounderCreateLive());
                return;
            }
            if (_pendingReview != null)
            {
                var reviewed = _pendingReview;
                _pendingReview = null;
                Open(new ReviewsLive(reviewed));
                return;
            }
            if (Gc.Events != null && Gc.Events.Current != null) { Open(new EventLive()); return; }
            if (run.pendingOffer != null && run.pendingOffer.active) { Open(new SellLive(true)); return; }

            var p = run.currentProject;
            if (p != null && p.status == ProjectStatus.InDevelopment && p.awaitingAllocation && !p.autoAllocate)
            {
                string key = p.id + ":" + p.phaseIndex;
                if (_allocationPromptedFor == key) return;
                _allocationPromptedFor = key;
                Open(new PhaseAllocationLive());
                return;
            }
            if (p != null && p.status == ProjectStatus.ReadyToLaunch && _launchPromptedFor != p.id)
            {
                _launchPromptedFor = p.id;
                Open(new LaunchLive());
                return;
            }
            if (run.company.expansionAvailable && !_expansionPrompted)
            {
                _expansionPrompted = true;
                Open(new ExpansionLive());
            }
        }

        // ------------------------------------------------------------------ telas

        /// <summary>Abre uma tela por cima do jogo (trava o tempo até fechar).</summary>
        internal void Open(GalleryScreen screen)
        {
            if (!_ready) return;
            _screenRoot.Clear();
            _screen = screen;
            screen.Ui = this;
            screen.OnClose = CloseScreen;
            if (screen is ManagementScreen panel) panel.OnNavigate = OpenPanel;
            if (!_locked) { Gc.Time.AddLock(); _locked = true; }
            Sfx.Play(SfxId.Open);
            ResetScreenRoot();
            screen.Init(_screenRoot);
        }

        private void ResetScreenRoot()
        {
            _screenRoot.style.backgroundColor = StyleKeyword.Null;
            _screenRoot.pickingMode = PickingMode.Ignore;
        }

        /// <summary>Verdadeiro se há uma tela aberta agora.</summary>
        internal bool HasScreen => _screen != null;

        /// <summary>Fecha a tela atual e devolve o tempo ao estado anterior.</summary>
        internal void CloseScreen()
        {
            if (_screen == null) return;
            _screen = null;
            _screenRoot.Clear();
            ResetScreenRoot();
            if (_locked) { Gc.Time.RemoveLock(); _locked = false; }
            Sfx.Play(SfxId.Close);
        }

        /// <summary>Abre um painel de gestão: 0 Financeiro, 1 Projetos, 2 RH, 3 Mercado, 4 P&amp;D, 5 Relações, 6 Fundador.</summary>
        internal void OpenPanel(int index)
        {
            if (Gc.Run.ended) return;
            switch (index)
            {
                case 0: Open(new FinanceLive()); break;
                case 1: Open(new ProjectsLive()); break;
                case 2: Open(new StaffLive()); break;
                case 3: Open(new MarketLive()); break;
                case 4: Open(new ResearchLive()); break;
                case 5: Open(new RelationsLive()); break;
                case 6: Open(new FounderLive()); break;
                default: Open(new FounderLive()); break;
            }
        }

        internal void OpenNewProject()
        {
            if (Gc.Run.ended) return;
            if (Gc.Projects.CanStartProject) Open(new NewProjectLive());
            else { Notify(Kit.IcGamepad, Kit.Blue, "Já existe um jogo em produção", "Termine ou lance o atual primeiro."); Sfx.Play(SfxId.Fail); }
        }

        /// <summary>Clique no card do projeto / selo de alerta: abre o que o projeto pede agora.</summary>
        internal void OpenProjectContext()
        {
            var run = Gc.Run;
            if (run.ended) return;
            var p = run.currentProject;
            if (p == null || !run.HasActiveProject)
            {
                if (run.company.expansionAvailable) Open(new ExpansionLive()); else OpenNewProject();
                return;
            }
            if (p.status == ProjectStatus.ReadyToLaunch) Open(new LaunchLive());
            else if (p.awaitingAllocation) Open(new PhaseAllocationLive());
            else Open(new MarketingLive());
        }

        internal void OpenMarketing() => Open(new MarketingLive());
        internal void OpenSellOptions() => Open(new SellLive(false));
        internal void OpenAcquisition(RivalState rival) => Open(new AcquisitionLive(rival));
        internal void OpenSkillTree() => Open(new SkillTreeLive());
        internal void OpenPauseMenu() => Open(new PauseLive());

        /// <summary>O HUD remonta os círculos da fase (após confirmar uma alocação).</summary>
        internal void RefreshHudAttributes() => _hud.RefreshAttributes();

        /// <summary>Volta ao menu principal salvando a run.</summary>
        internal void GoToMainMenu()
        {
            Gc.AutoSave();
            CloseScreen();
            SceneManager.LoadScene(MenuScene, LoadSceneMode.Single);
        }

        internal void QuitGame()
        {
            Gc.AutoSave();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnEscape()
        {
            if (_screen == null) { if (!Gc.Run.ended) Open(new PauseLive()); return; }
            if (_screen is PauseLive || _screen is ManagementScreen || _screen is MarketingLive || _screen is NewProjectLive)
                CloseScreen();
        }

        // ------------------------------------------------------------------ avisos

        /// <summary>Mostra um aviso rápido na coluna da direita.</summary>
        internal void Notify(string icon, Color color, string title, string subtitle, float life = 5f)
        {
            if (!_ready) return;
            var card = Kit.Card(0, 14);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center; card.style.marginBottom = 8;
            Kit.Pad(card, 9, 12);
            card.pickingMode = PickingMode.Ignore;
            card.Add(Kit.Badge(icon, color, 32));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexShrink = 1;
            t.Add(Kit.Wrap(title, 13, Kit.Ink, true));
            if (!string.IsNullOrEmpty(subtitle)) t.Add(Kit.Wrap(subtitle, 11, Kit.Soft));
            card.Add(t);
            card.style.opacity = 0f;
            _toastColumn.Add(card);
            _toasts.Add(new Toast { ve = card, life = life });
            while (_toasts.Count > 5) { _toasts[0].ve.RemoveFromHierarchy(); _toasts.RemoveAt(0); }
        }

        private void TickToasts(float dt)
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var t = _toasts[i];
                t.age += dt;
                float enter = Mathf.Clamp01(t.age / 0.3f);
                float exit = Mathf.Clamp01((t.life - t.age) / 0.4f);
                t.ve.style.opacity = Mathf.Min(enter, exit);
                t.ve.style.translate = new Translate((1f - enter) * 320f, 0, 0);
                if (t.age >= t.life) { t.ve.RemoveFromHierarchy(); _toasts.RemoveAt(i); }
            }
        }

        // ------------------------------------------------------------------ fatos do jogo

        private void OnMoney(MoneyChangedEvent e)
        {
            if (_hud != null && System.Math.Abs(e.Delta) >= 100) _hud.ShowMoneyTick(e.Delta);
        }

        private void OnLaunched(GameLaunchedEvent e)
        {
            var released = Gc.Run.releasedProjects;
            for (int i = released.Count - 1; i >= 0; i--)
                if (released[i].name == e.GameName) { _pendingReview = released[i]; break; }
        }

        private void OnResearchPoints(ResearchPointsChangedEvent e) => Sfx.Play(SfxId.Point, 1.2f);

        private void OnResearchDone(ResearchCompletedEvent e)
        {
            Notify(Kit.IcFlask, Kit.Teal, "Pesquisa concluída", e.Label);
            Sfx.Play(SfxId.Unlock);
        }

        private void OnHired(EmployeeHiredEvent e) => Notify(Kit.IcUserPlus, Kit.Orange, e.EmployeeName + " entrou na equipe", null);

        private void OnLeft(EmployeeLeftEvent e)
        {
            if (e.Reason == LeaveReason.Fired) return;
            Notify(Kit.IcWarn, Kit.Red, e.EmployeeName + " pediu demissão", e.Reason == LeaveReason.Burnout ? "Burnout no limite." : null, 7f);
            Sfx.Play(SfxId.Fail);
        }

        private void OnPromoted(EmployeePromotedEvent e) =>
            Notify(Kit.IcArrowUp, Kit.Orange, e.EmployeeName + " foi promovido(a)", "O salário subiu junto.");

        private void OnRivalGame(RivalGameReleasedEvent e) =>
            Notify(Kit.IcRocket, Kit.Red, e.RivalName + " lançou " + e.GameName, "Nota " + Labels.Score(e.Score));

        private void OnConsole(ConsoleReleasedEvent e)
        {
            Notify(Kit.IcGamepad, Kit.Blue, "Novo console: " + e.ConsoleName, "Licença " + Kit.Money(e.LicenseCost), 8f);
            Sfx.Play(SfxId.Unlock);
        }

        private void OnGlobal(GlobalMarketEvent e)
        {
            Notify(Kit.IcNews, e.Positive ? Kit.Green : Kit.Red, e.Title, e.Body, 12f);
            Sfx.Play(e.Positive ? SfxId.Success : SfxId.Alert);
        }

        private void OnAchievement(AchievementUnlockedEvent e)
        {
            Notify(Kit.IcTrophy, Kit.Gold, "Conquista: " + e.Title, e.Description, 7f);
            Sfx.Play(SfxId.Unlock);
        }

        private void OnExpansionAvailable(PhaseExpansionAvailableEvent e)
        {
            _expansionPrompted = false;
            Sfx.Play(SfxId.Alert);
        }

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            _expansionPrompted = false;
            Notify(Labels.PhaseIcon(e.To), Kit.Teal, "Bem-vindo à fase " + Labels.Phase(e.To), Labels.PhaseBlurb(e.To), 7f);
            Sfx.Play(SfxId.Fanfare);
        }

        private void OnReady(ProjectReadyToLaunchEvent e) => Sfx.Play(SfxId.Success);
        private void OnPhaseStarted(ProductionPhaseStartedEvent e) => Sfx.Play(SfxId.Success, 1.1f);
        private void OnNarrative(NarrativeEventTriggeredEvent e) => Sfx.Play(SfxId.Alert);
        private void OnOffer(AcquisitionOfferReceivedEvent e) => Sfx.Play(SfxId.Alert);

        private void OnRunEnded(RunEndedEvent e)
        {
            if (_screen != null) CloseScreen();
            Sfx.Play(e.Reason == RunEndReason.Bankruptcy ? SfxId.GameOver : SfxId.Fanfare);
        }

        /// <summary>Chamado pela árvore de habilidades ao iniciar uma nova run.</summary>
        internal void OnNewRunStarted()
        {
            _pendingReview = null;
            _launchPromptedFor = null;
            _expansionPrompted = false;
            _endShown = false;
            _locked = false;
            _hud.RefreshAttributes();
        }
    }
}
