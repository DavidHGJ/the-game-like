using System;
using System.Collections.Generic;
using GameLike.Systems.Core;
using GameLike.Systems.Save;
using GameLike.Systems.Updates;
using GameLike.UI.Gallery;
using GameLike.UI.MapBuilder;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>
    /// Menu inicial do jogo (GDD 20.1). Nasce sozinho em runtime quando a cena <c>MainMenu</c>
    /// carrega (nada na cena/Inspector), mostra o menu aprovado — <see cref="AnimatedMenu"/>,
    /// variante Console creme — e liga as opções ao fluxo de boot: Novo jogo, Continuar,
    /// Opções, Sair e, no editor/development build, o Map Builder. A escolha é registrada em
    /// <see cref="GameBoot"/>; o <see cref="GameController"/> lê a intenção ao iniciar.
    /// </summary>
    public sealed class MainMenuLive : MonoBehaviour
    {
        private const string MenuScene = "MainMenu";
        private const string GameScene = "GameOffice";
        private const string PanelResource = "HudPanelSettings";
        private const string DefaultCompanyName = "Minha Dev";
        private const int ContinueSlot = 0;
        private const int ConsoleVariant = 0;

        private readonly List<Action> _actions = new List<Action>();
        private VisualElement _overlay;
        private string _savedCompany;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded += (scene, _) => TryAttach(scene);
            TryAttach(SceneManager.GetActiveScene());
        }

        private static void TryAttach(Scene scene)
        {
            if (scene.name != MenuScene || FindFirstObjectByType<MainMenuLive>() != null) return;
            var go = new GameObject("[MainMenu]");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<MainMenuLive>();
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            var panel = Resources.Load<PanelSettings>(PanelResource);
            if (panel == null)
            {
                Debug.LogError("[MainMenu] PanelSettings '" + PanelResource + "' não encontrado em Resources.");
                enabled = false;
                return;
            }
            EnsureCamera();
            Sfx.Ensure();
            Kit.UiSound = kind => Sfx.Play(kind == 0 ? SfxId.Hover : SfxId.Click);
            _savedCompany = PeekSavedCompany();

            var items = new List<(string label, string icon)>();
            void Add(string label, string icon, Action action) { items.Add((label, icon)); _actions.Add(action); }
            Add("Novo jogo", Kit.IcPlay, AskNewGame);
            Add(_savedCompany != null ? "Continuar — " + _savedCompany : "Continuar (sem jogo salvo)", Kit.IcFolder, Continue);
            Add("Opções", Kit.IcGear, ShowOptions);
            if (MapBuilderBoot.IsAvailable) Add(MapBuilderBoot.MenuLabel, Kit.IcCubes, MapBuilderBoot.Open);
            if (Tuning.TuningScreen.IsAvailable) Add("Parâmetros", Kit.IcSliders, Tuning.TuningScreen.Open);
            Add("Sair", Kit.IcPower, Quit);

            var menuGo = MakeDoc("Menu", panel, 0, out _);
            menuGo.AddComponent<AnimatedMenu>().Configure(ConsoleVariant, items.ToArray(), Choose);

            MakeDoc("Avisos", panel, 10, out var overlayDoc);
            var overlayRoot = overlayDoc.rootVisualElement;
            overlayRoot.style.flexGrow = 1;
            overlayRoot.pickingMode = PickingMode.Ignore;

            var version = Kit.Text("v" + Application.version, 12, Kit.Soft, true);
            version.style.position = Position.Absolute;
            version.style.left = 16; version.style.bottom = 12;
            version.pickingMode = PickingMode.Ignore;
            overlayRoot.Add(version);

            _overlay = Kit.Fill();
            _overlay.pickingMode = PickingMode.Ignore;
            overlayRoot.Add(_overlay);

            if (GameUpdater.Supported) StartCoroutine(GameUpdater.Check(OnUpdateChecked));
        }

        // ------------------------------------------------------------------ atualização

        private void OnUpdateChecked(AvailableUpdate update)
        {
            if (update == null || _overlay.childCount > 0) return;
            string size = update.sizeBytes > 0 ? " (" + (update.sizeBytes / 1048576f).ToString("0") + " MB)" : "";
            string notes = string.IsNullOrWhiteSpace(update.notes) ? "" : "\n\n" + (update.notes.Length > 420 ? update.notes.Substring(0, 420) + "…" : update.notes);
            Dialog("Atualização disponível",
                "A versão " + update.version + size + " está pronta. O jogo baixa, fecha e reabre já atualizado — seu jogo salvo continua." + notes,
                "Atualizar agora", Kit.Teal, () => StartCoroutine(InstallUpdate(update)), "Depois");
        }

        private System.Collections.IEnumerator InstallUpdate(AvailableUpdate update)
        {
            _overlay.Clear();
            _overlay.pickingMode = PickingMode.Position;
            var dim = Kit.Centered();
            dim.style.backgroundColor = new Color(0.10f, 0.09f, 0.07f, 0.55f);
            var card = Kit.Card(440, 18);
            Kit.Pad(card, 20, 22);
            card.Add(Kit.Text("Baixando a versão " + update.version, 20, Kit.Ink, true));
            var status = Kit.Text("0%", 14, Kit.Soft, true);
            status.style.marginTop = 8; status.style.marginBottom = 10;
            card.Add(status);
            card.Add(Kit.Bar(12, Kit.Teal, out var fill));
            dim.Add(card);
            _overlay.Add(dim);

            string error = null;
            yield return GameUpdater.DownloadAndInstall(update, p =>
            {
                fill.style.width = Length.Percent(Mathf.Clamp01(p) * 100f);
                status.text = Mathf.RoundToInt(p * 100f) + "%" + (p >= 1f ? " — instalando e reabrindo…" : "");
            }, message => error = message);

            if (error != null) Dialog("Não deu para atualizar", error, "Ok", Kit.Teal, null, null);
        }

        private GameObject MakeDoc(string docName, PanelSettings panel, int sorting, out UIDocument doc)
        {
            var go = new GameObject(docName);
            go.transform.SetParent(transform, false);
            doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = sorting;
            return go;
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.84f, 0.85f, 0.94f);
            go.AddComponent<AudioListener>();
        }

        private static string PeekSavedCompany()
        {
            try
            {
                if (!SaveManager.HasSave(ContinueSlot)) return null;
                string company = SaveManager.PeekCompanyName(ContinueSlot);
                return string.IsNullOrEmpty(company) ? null : company;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MainMenu] Save ilegível, 'Continuar' desativado: " + e.Message);
                return null;
            }
        }

        private void Choose(int index)
        {
            if (_overlay.childCount > 0 || FindFirstObjectByType<Tuning.TuningScreen>() != null || index < 0 || index >= _actions.Count) return;
            Sfx.Play(SfxId.Click);
            _actions[index]();
        }

        // ------------------------------------------------------------------ ações

        private void AskNewGame()
        {
            if (_savedCompany == null) { StartNew(); return; }
            Dialog("Começar do zero?",
                "Um novo jogo substitui a empresa \"" + _savedCompany + "\" e zera a progressão do fundador.",
                "Começar", Kit.Red, StartNew, "Cancelar");
        }

        private void StartNew()
        {
            GameBoot.RequestNewRun(DefaultCompanyName);
            SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }

        private void Continue()
        {
            if (_savedCompany == null)
            {
                Sfx.Play(SfxId.Fail);
                Dialog("Nenhum jogo salvo", "Ainda não há uma empresa salva. Comece um novo jogo.", "Ok", Kit.Teal, null, null);
                return;
            }
            GameBoot.RequestContinue(ContinueSlot);
            SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }

        private void ShowOptions() =>
            Dialog("Opções", "As opções de vídeo, som e idioma ainda não estão ligadas ao jogo — chegam em breve.", "Ok", Kit.Teal, null, null);

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Dialog(string title, string message, string confirm, Color confirmColor, Action onConfirm, string cancel)
        {
            _overlay.Clear();
            _overlay.pickingMode = PickingMode.Position;
            var dim = Kit.Centered();
            dim.style.backgroundColor = new Color(0.10f, 0.09f, 0.07f, 0.55f);
            var card = Kit.Card(440, 18);
            Kit.Pad(card, 20, 22);
            card.Add(Kit.Text(title, 20, Kit.Ink, true));
            var body = Kit.Wrap(message, 14, Kit.Soft);
            body.style.marginTop = 8; body.style.marginBottom = 18;
            card.Add(body);

            var row = Kit.Row();
            if (cancel != null)
            {
                var no = Kit.Button(cancel, null, Kit.Sand, Kit.Ink, CloseDialog, 40);
                no.style.flexGrow = 1; no.style.flexBasis = 0;
                row.Add(no);
                row.Add(Kit.Gap(8));
            }
            var yes = Kit.Button(confirm, null, confirmColor, Color.white, () => { CloseDialog(); onConfirm?.Invoke(); }, 40);
            yes.style.flexGrow = 1; yes.style.flexBasis = 0;
            row.Add(yes);
            card.Add(row);
            dim.Add(card);
            _overlay.Add(dim);
            Sfx.Play(SfxId.Open);
        }

        private void CloseDialog()
        {
            _overlay.Clear();
            _overlay.pickingMode = PickingMode.Ignore;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Teste de fumaça (<c>-glplay</c>): o piloto automático precisa chegar ao jogo passando
        /// pelo menu de verdade, então o menu carrega a cena de jogo sozinho depois de aparecer —
        /// sem registrar "Novo jogo", para não tocar no save do jogador (o piloto usa o slot 9).
        /// </summary>
        private System.Collections.IEnumerator Start()
        {
            int tune = Array.IndexOf(Environment.GetCommandLineArgs(), "-gltuning");
            if (tune >= 0)
            {
                // print de validação da tela de Parâmetros, e encerra
                string shot = Environment.GetCommandLineArgs()[tune + 1];
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(shot));
                Tuning.TuningScreen.Open();
                yield return new WaitForSecondsRealtime(2f);

                // digita sem pontos no primeiro campo numérico e confere a máscara e o valor aplicado
                var screen = FindFirstObjectByType<Tuning.TuningScreen>();
                var fields = screen.GetComponent<UIDocument>().rootVisualElement.Query<TextField>().ToList();
                var economy = GameLike.Data.Balance.BalanceProvider.Get<GameLike.Data.Balance.EconomyBalanceSO>();
                long before = economy.GarageToIndieThreshold;
                if (fields.Count > 1) fields[1].value = "12500000";
                yield return new WaitForSecondsRealtime(0.5f);
                string shown = fields.Count > 1 ? fields[1].value : "?";
                Debug.Log("[TuningShot] digitado=12500000 mostrado=" + shown + " aplicado=" + economy.GarageToIndieThreshold);
                Debug.Log(shown == "12.500.000" && economy.GarageToIndieThreshold == 12_500_000 ? "GAMELIKE_TUNING_MASK_OK" : "GAMELIKE_TUNING_MASK_FAIL");
                ScreenCapture.CaptureScreenshot(shot);
                yield return new WaitForSecondsRealtime(1f);
                GameLike.Data.Balance.BalanceProvider.ResetToDefaults(economy);
                if (economy.GarageToIndieThreshold != before) Debug.LogWarning("[TuningShot] valor original não restaurado");
                yield return new WaitForSecondsRealtime(1f);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(0);
#else
                Application.Quit();
#endif
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-glplay") < 0) yield break;
            yield return new WaitForSecondsRealtime(2.5f);
            string dir = Environment.GetCommandLineArgs()[Array.IndexOf(Environment.GetCommandLineArgs(), "-glplay") + 1];
            System.IO.Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "00_menu.png"));
            yield return new WaitForSecondsRealtime(0.8f);
            SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }
#endif
    }
}
