#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Core;
using GameLike.Systems.Marketing;
using UnityEngine;

namespace GameLike.UI.Game
{
    /// <summary>
    /// Ferramenta de dev: com <c>-glplay &lt;pasta&gt;</c>, joga o loop completo sozinho (fundador →
    /// projeto → 5 fases → lançamento → notas → painéis → expansão → venda → árvore → nova run),
    /// salvando um PNG de cada tela e encerrando. Serve de teste de fumaça visual do jogo real.
    /// Usa o slot de save 9 para não tocar no save do jogador. Custo zero no release.
    /// </summary>
    public sealed class LoopAutoplay : MonoBehaviour
    {
        private const int TestSlot = 9;
        private string _dir;
        private int _shot;
        private GameController _gc;
        private GameUi _ui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string dir = ArgValue("-glplay");
            if (string.IsNullOrEmpty(dir)) return;
            var go = new GameObject("[LoopAutoplay]");
            DontDestroyOnLoad(go);
            go.AddComponent<LoopAutoplay>()._dir = dir;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            Application.runInBackground = true;
            float waited = 0f;
            while ((_gc == null || _ui == null || _gc.Run == null) && waited < 20f)
            {
                _gc = FindFirstObjectByType<GameController>();
                _ui = FindFirstObjectByType<GameUi>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_gc == null || _ui == null) { Debug.LogError("[LoopAutoplay] GameController/GameUi não encontrados."); Quit(1); yield break; }

            _gc.StartFreshForTesting(TestSlot);
            _ui.OnNewRunStarted();
            yield return Wait(2.2f);

            // 1. fundador
            yield return Shot("fundador");
            _gc.SetFounder("David", 0, 0, 1, 0, 1);
            _gc.RenameCompany("Pixel Forja");
            _ui.CloseScreen();
            yield return Wait(0.6f);
            yield return Shot("hud_sem_projeto");

            // 2. conceito
            _ui.OpenNewProject();
            yield return Wait(2.0f);
            yield return Shot("novo_projeto");
            _ui.CloseScreen();
            yield return RunOneGame("Lendas de Aethel", true);

            // 3. painéis
            if (_gc.Run.research != null) _gc.Run.research.points += 30;
            for (int panel = 0; panel <= 6; panel++)
            {
                _ui.OpenPanel(panel);
                yield return Wait(2.0f);
                yield return Shot("painel_" + panel);
                _ui.CloseScreen();
                yield return Wait(0.2f);
            }

            // 4. segundo jogo (com pesquisa rodando) e expansão de fase
            foreach (var node in _gc.Research.Available())
                if (_gc.Research.StartResearch(node.Id)) break;
            _gc.Economy.ApplyDelta(400_000, "Piloto automático: caixa de teste");
            yield return RunOneGame("Turbo Kart", false);
            yield return WaitFor(() => _gc.Run.company.expansionAvailable, 20f);
            yield return WaitFor(() => _ui.HasScreen, 6f);
            yield return Wait(2.2f);
            yield return Shot("expansao");
            Debug.Log("[LoopAutoplay] Expansão: " + _gc.ExpandPhase() + " → fase " + _gc.Run.company.phase);
            _ui.CloseScreen();
            for (int h = 0; h < 2 && _gc.Staff.Candidates.Count > 0; h++) _gc.Staff.Hire(_gc.Staff.Candidates[0]);
            yield return Wait(2.5f);
            yield return Shot("cenario_indie_com_equipe");
            _ui.CloseScreen();
            yield return Wait(1.0f);

            _ui.OpenPanel(2);
            yield return Wait(1.6f);
            yield return Shot("rh_fase_indie");
            _ui.CloseScreen();

            // 5. venda, resultado, árvore, nova run
            _ui.OpenSellOptions();
            yield return Wait(2.0f);
            yield return Shot("encerrar_run");
            _ui.CloseScreen();
            _gc.SellCompany();
            yield return WaitFor(() => _ui.HasScreen, 6f);
            yield return Wait(5.0f);
            yield return Shot("resultado");
            _ui.OpenSkillTree();
            yield return Wait(2.2f);
            yield return Shot("arvore");
            Systems.Meta.SkillTreeCatalog.TryBuyRoot(_gc.Meta, FounderSkill.Technical);
            Systems.Meta.SkillTreeCatalog.TryBuyPerk(_gc.Meta, Systems.Meta.SkillTreeCatalog.CodeReview);
            _gc.StartNewRun("Pixel Forja II", _gc.Meta.personalMoney / 4);
            _ui.OnNewRunStarted();
            _ui.CloseScreen();
            yield return Wait(1.5f);
            yield return Shot("nova_run");

            Debug.Log("GAMELIKE_AUTOPLAY_OK run=" + _gc.Run.runNumber + " rivais=" + _gc.Run.rivals.Count
                      + " pessoal=" + _gc.Meta.personalMoney + " PL=" + _gc.Meta.legacyPoints);
            Quit(0);
        }

        /// <summary>Cria um jogo e o conduz até o lançamento e as notas.</summary>
        private IEnumerator RunOneGame(string gameName, bool screenshots)
        {
            var genre = _gc.AvailableGenres[_gc.Run.company.gamesReleased % _gc.AvailableGenres.Count];
            ThemeSO theme = null;
            foreach (var t in _gc.AvailableThemes)
                if (_gc.Research.IsThemeUnlocked(t.name)) { theme = t; break; }
            var consoles = _gc.ConsolesAvailableNow();
            bool started = _gc.StartProject(gameName, genre, ProjectSize.Small, theme, null, consoles.Count > 0 ? consoles[0] : null);
            Debug.Log("[LoopAutoplay] Projeto '" + gameName + "' iniciado: " + started);
            _gc.Time.SetSpeedIndex(_gc.Time.SpeedCount - 1);
            _gc.Time.Resume();

            bool shotHud = false, shotMarketing = false;
            float guard = 0f;
            while (_gc.Run.currentProject != null && _gc.Run.currentProject.status == ProjectStatus.InDevelopment && guard < 180f)
            {
                var p = _gc.Run.currentProject;
                if (p.awaitingAllocation)
                {
                    yield return WaitFor(() => _ui.HasScreen, 4f);
                    yield return Wait(screenshots ? 1.8f : 0.3f);
                    if (screenshots) yield return Shot("alocacao_fase_" + (p.phaseIndex + 1));
                    _gc.Projects.ConfirmAllocation(_gc.Projects.IdealAllocation(p), false);
                    _ui.RefreshHudAttributes();
                    _ui.CloseScreen();
                }
                else if (screenshots && !shotHud && p.phaseIndex == 0 && p.phaseWeek >= 1)
                {
                    shotHud = true;
                    yield return Wait(0.4f);
                    yield return Shot("hud_producao");
                }
                else if (screenshots && !shotMarketing && p.phaseIndex == 1)
                {
                    shotMarketing = true;
                    _gc.Marketing.RunCampaign(MarketingCampaign.Announcement);
                    _ui.OpenMarketing();
                    yield return Wait(1.8f);
                    yield return Shot("marketing");
                    _ui.CloseScreen();
                }
                else if (_ui.HasScreen)
                {
                    // tela inesperada (evento, expansão, oferta): fecha para o tempo voltar a andar
                    if (_gc.Events.Current != null) _gc.Events.ResolveOption(0);
                    if (_gc.Run.pendingOffer.active) _gc.DeclineOffer();
                    _ui.CloseScreen();
                }
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            // pronto para lançar → tela de lançamento
            yield return WaitFor(() => _ui.HasScreen, 6f);
            yield return Wait(screenshots ? 1.8f : 0.3f);
            if (screenshots) yield return Shot("lancamento");
            var ready = _gc.Run.currentProject;
            bool launched = ready != null && _gc.Projects.Launch(_gc.Projects.ReferencePrice(ready), 2_000);
            Debug.Log("[LoopAutoplay] Lançado: " + launched);
            _ui.CloseScreen();
            if (screenshots) { yield return Wait(0.5f); yield return Shot("hud_aguardando_criticas"); }

            // semana seguinte: críticas
            var last = _gc.Run.releasedProjects[_gc.Run.releasedProjects.Count - 1];
            yield return WaitFor(() => !last.awaitingReview && _ui.HasScreen, 12f);
            yield return Wait(screenshots ? 6.5f : 0.5f);
            if (screenshots) yield return Shot("notas");
            _ui.CloseScreen();

            // depois das críticas: vendas semana a semana (gráfico do HUD)
            float selling = 0f;
            while (last.salesWeek < 5 && selling < 25f)
            {
                if (_ui.HasScreen)
                {
                    if (_gc.Events.Current != null) _gc.Events.ResolveOption(0);
                    if (_gc.Run.pendingOffer.active) _gc.DeclineOffer();
                    _ui.CloseScreen();
                }
                selling += Time.unscaledDeltaTime;
                yield return null;
            }
            if (screenshots) yield return Shot("hud_vendas");
            Debug.Log("[LoopAutoplay] '" + last.name + "' nota " + last.finalScore + " previsão " + last.forecastUnits
                      + " un. semanas vendidas " + last.salesWeek + " caixa " + _gc.Run.company.cash + " valuation " + _gc.Run.company.valuation);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            _shot++;
            string file = Path.Combine(_dir, _shot.ToString("00") + "_" + name + ".png");
            ScreenCapture.CaptureScreenshot(file, 2);
            yield return Wait(0.35f);
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }

        private static string ArgValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }
    }
}
#endif
