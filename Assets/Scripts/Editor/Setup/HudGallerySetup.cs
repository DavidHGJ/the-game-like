using GameLike.UI.Gallery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Monta a cena <c>Assets/Scenes/UIGallery.unity</c> com o HUD principal (gameplay,
    /// <see cref="AnimatedHud"/>), as 3 versões animadas do menu inicial (<see cref="AnimatedMenu"/>)
    /// e todas as demais telas do GDD (<see cref="GalleryScreenHost"/>), agrupadas por contexto,
    /// para o dev ligar/desligar na Hierarchy e validar. Ferramenta de produção.
    /// </summary>
    public static class HudGallerySetup
    {
        private const string PanelPath = "Assets/UI/Resources/HudPanelSettings.asset";
        private const string ScenePath = "Assets/Scenes/UIGallery.unity";
        private const int ScreenSorting = 10;

        [MenuItem("Tools/GameLike/UI Gallery/Build HUD Gallery Scene")]
        public static void Build()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                Debug.LogError("[Gallery] HudPanelSettings nao encontrado em " + PanelPath + ". Rode o setup do HUD antes.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene descarrega assets não referenciados → recarrega o panel p/ não gravar null.
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);

            var camGo = new GameObject("Backdrop Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.933f, 0.906f, 0.839f, 1f);
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.tag = "MainCamera";

            // HUD PRINCIPAL (gameplay) — direção aprovada
            var hudParent = new GameObject("HUD PRINCIPAL (gameplay)");
            var hud = MakeDoc("HUD Animado", panel, 2, true, hudParent.transform);
            hud.AddComponent<AnimatedHud>();
            EditorUtility.SetDirty(hud);

            // MENU INICIAL — 3 versões p/ escolher (ligue uma por vez, desligue o HUD)
            var menuParent = new GameObject("MENU INICIAL (ligue 1 p/ ver)");
            CreateMenu(menuParent.transform, "Menu v1 — Console creme", panel, 3, 0);
            CreateMenu(menuParent.transform, "Menu v2 — Estúdio", panel, 3, 1);
            CreateMenu(menuParent.transform, "Menu v3 — Arcade", panel, 3, 2);

            // DEMAIS TELAS DO GDD — ligue uma por vez (as "sobre o jogo" ficam por cima do HUD)
            var outRun = new GameObject("1 - FORA DA RUN (desligue o HUD)").transform;
            CreateScreen(outRun, "Slots de save", panel, GalleryScreenId.SaveSlots, false, 0);
            CreateScreen(outRun, "Opções — Áudio", panel, GalleryScreenId.Options, false, 0);
            CreateScreen(outRun, "Opções — Acessibilidade", panel, GalleryScreenId.Options, false, 3);
            CreateScreen(outRun, "Novo fundador", panel, GalleryScreenId.FounderCreate, false, 0);

            var project = new GameObject("2 - FLUXO DO PROJETO").transform;
            CreateScreen(project, "Novo projeto (conceito)", panel, GalleryScreenId.NewProject, true, 0);
            CreateScreen(project, "Alocação da fase (sliders)", panel, GalleryScreenId.PhaseAllocation, true, 0);
            CreateScreen(project, "Marketing", panel, GalleryScreenId.Marketing, true, 0);
            CreateScreen(project, "Lançamento", panel, GalleryScreenId.Launch, true, 0);
            CreateScreen(project, "Notas e vendas", panel, GalleryScreenId.Reviews, true, 0);

            var panels = new GameObject("3 - PAINÉIS DE GESTÃO").transform;
            CreateScreen(panels, "Financeiro", panel, GalleryScreenId.Finance, true, 0);
            CreateScreen(panels, "Projetos", panel, GalleryScreenId.Projects, true, 0);
            CreateScreen(panels, "RH — Equipe", panel, GalleryScreenId.Staff, true, 0);
            CreateScreen(panels, "RH — Mercado de talentos", panel, GalleryScreenId.Staff, true, 1);
            CreateScreen(panels, "Mercado — Tendências", panel, GalleryScreenId.Market, true, 0);
            CreateScreen(panels, "Mercado — Rivais", panel, GalleryScreenId.Market, true, 1);
            CreateScreen(panels, "P&D — Pesquisa", panel, GalleryScreenId.Research, true, 0);
            CreateScreen(panels, "P&D — Engines", panel, GalleryScreenId.Research, true, 1);
            CreateScreen(panels, "Relações — Contratos", panel, GalleryScreenId.Relations, true, 0);
            CreateScreen(panels, "Relações — Declarações do CEO", panel, GalleryScreenId.Relations, true, 1);
            CreateScreen(panels, "Fundador", panel, GalleryScreenId.Founder, true, 0);

            var popups = new GameObject("4 - EVENTOS E POPUPS").transform;
            CreateScreen(popups, "Evento narrativo", panel, GalleryScreenId.NarrativeEvent, true, 0);
            CreateScreen(popups, "Expansão de fase", panel, GalleryScreenId.PhaseExpansion, true, 0);
            CreateScreen(popups, "M&A — Proposta de compra", panel, GalleryScreenId.Acquisition, true, 0);
            CreateScreen(popups, "M&A — Absorção", panel, GalleryScreenId.Acquisition, true, 1);
            CreateScreen(popups, "Venda — Oferta de rival", panel, GalleryScreenId.SellCompany, true, 0);
            CreateScreen(popups, "Venda — Encerrar a run", panel, GalleryScreenId.SellCompany, true, 1);
            CreateScreen(popups, "Menu de pausa", panel, GalleryScreenId.PauseMenu, true, 0);
            CreateScreen(popups, "Avisos (conquista, evento global, dica)", panel, GalleryScreenId.Toasts, true, 0);

            var meta = new GameObject("5 - FIM DE RUN E META (desligue o HUD)").transform;
            CreateScreen(meta, "Resultado — Empresa vendida", panel, GalleryScreenId.RunResult, false, 0);
            CreateScreen(meta, "Resultado — Falência", panel, GalleryScreenId.RunResult, false, 1);
            CreateScreen(meta, "Árvore de habilidades", panel, GalleryScreenId.SkillTree, false, 0);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Gallery] Cena montada: " + ScenePath + " — HUD principal + 3 menus + telas do GDD. " +
                      "Dê Play e ligue uma tela por vez na Hierarchy.");
        }

        private static GameObject MakeDoc(string name, PanelSettings panel, int sorting, bool active, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = sorting;
            var so = new SerializedObject(doc);
            SetRef(so, "m_PanelSettings", panel);
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(active);
            EditorUtility.SetDirty(doc);
            EditorUtility.SetDirty(go);
            return go;
        }

        private static void CreateMenu(Transform parent, string name, PanelSettings panel, int sorting, int variant)
        {
            var go = MakeDoc(name, panel, sorting, false, parent);
            var menu = go.AddComponent<AnimatedMenu>();
            var mso = new SerializedObject(menu);
            var vp = mso.FindProperty("variant");
            if (vp != null) vp.intValue = variant;
            mso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void CreateScreen(Transform parent, string name, PanelSettings panel, GalleryScreenId id, bool overGameplay, int state)
        {
            var go = MakeDoc(name, panel, ScreenSorting, false, parent);
            var host = go.AddComponent<GalleryScreenHost>();
            var so = new SerializedObject(host);
            so.FindProperty("screen").enumValueIndex = (int)id;
            so.FindProperty("overGameplay").boolValue = overGameplay;
            so.FindProperty("initialState").intValue = state;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(host);
        }

        private static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}
