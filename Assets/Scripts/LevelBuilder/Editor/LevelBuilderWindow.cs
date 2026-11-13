using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>
    /// Janela única e coesa do Level Builder (LEVEL_BUILDER.md §14): orquestra
    /// andar/camada/pincel ativos e a palette da camada, gravando tudo no
    /// <see cref="LevelBuilderState"/> compartilhado. A pintura em si acontece
    /// na SceneView via <see cref="LevelBuilderTool"/> (o preview é a cena real).
    ///
    /// Fase 1 entrega a camada <b>Floor</b> completa (Paint/Erase/Rectangle/Pick).
    /// Wall/Furniture/Zones aparecem desabilitadas até suas fases.
    /// </summary>
    public sealed class LevelBuilderWindow : EditorWindow
    {
        private readonly List<TileBase> _tilePalette = new();
        private readonly List<FurnitureDefinitionSO> _furniturePalette = new();
        private Vector2 _paletteScroll;
        private const float TileButtonSize = 56f;

        [MenuItem("Tools/GameLike/Level Builder/Window", false, 1)]
        public static void Open()
        {
            var w = GetWindow<LevelBuilderWindow>("Level Builder");
            w.minSize = new Vector2(320f, 420f);
        }

        private void OnEnable()
        {
            RefreshPalette();
            RefreshFurniture();
            ToolManager.activeToolChanged += Repaint; // mantém o status do botão sempre atualizado
        }

        private void OnDisable()
        {
            ToolManager.activeToolChanged -= Repaint;
        }

        private void OnFocus()
        {
            RefreshPalette();
            RefreshFurniture();
        }

        /// <summary>Ativa a ferramenta de pintura na SceneView se ainda não estiver ativa.</summary>
        private static void EnsureToolActive()
        {
            if (ToolManager.activeToolType != typeof(LevelBuilderTool))
                ToolManager.SetActiveTool<LevelBuilderTool>();
        }

        private void OnGUI()
        {
            var state = LevelBuilderState.instance;

            DrawSceneSection();
            EditorGUILayout.Space(4f);
            DrawControls(state);
            EditorGUILayout.Space(4f);
            if (state.layer == BuildLayer.Furniture)
                DrawFurniturePalette(state);
            else
                DrawPalette(state);
        }

        // ── Cena / ferramenta ────────────────────────────────────────────

        private void DrawSceneSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!LevelBuilderSceneRefs.SceneIsSetup())
                {
                    EditorGUILayout.HelpBox("A cena ainda não tem a fundação isométrica.", MessageType.Warning);
                    if (GUILayout.Button("Setup Scene (criar Grid iso + Tilemaps + câmera)"))
                        LevelBuilderSceneSetup.SetupScene();
                    return;
                }

                bool toolActive = ToolManager.activeToolType == typeof(LevelBuilderTool);
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = toolActive ? new Color(0.4f, 0.8f, 0.4f) : new Color(1f, 0.8f, 0.3f);
                if (GUILayout.Button(toolActive ? "✓ Pintando na cena (clique p/ sair)" : "Entrar no modo de pintar"))
                {
                    if (toolActive) ToolManager.RestorePreviousPersistentTool();
                    else            EnsureToolActive();
                }
                GUI.backgroundColor = prev;
                EditorGUILayout.LabelField("Selecionar um tile/pincel já ativa o modo. Pinte na SceneView. Ctrl+Z desfaz.", EditorStyles.miniLabel);
            }
        }

        // ── Controles (andar / camada / pincel) ──────────────────────────

        private void DrawControls(LevelBuilderState state)
        {
            EditorGUI.BeginChangeCheck();

            state.storey = EditorGUILayout.IntField("Andar ativo", Mathf.Max(0, state.storey));

            state.layer = (BuildLayer)EditorGUILayout.EnumPopup("Camada ativa", state.layer);
            EditorGUILayout.LabelField("Zones chega na próxima fase.", EditorStyles.miniLabel);

            if (state.layer == BuildLayer.Wall)
            {
                state.wallHeight    = EditorGUILayout.IntField("Altura (blocos)", Mathf.Max(1, state.wallHeight));
                state.wallBaseLevel = EditorGUILayout.IntField("Nível base (Z)", Mathf.Max(0, state.wallBaseLevel));
            }

            if (state.layer == BuildLayer.Furniture)
                state.furnitureBaseLevel = EditorGUILayout.IntField("Altura de apoio (Z)", Mathf.Max(0, state.furnitureBaseLevel));

            EditorGUILayout.LabelField("Pincel", EditorStyles.boldLabel);
            state.brush = (BuildBrush)GUILayout.Toolbar((int)state.brush,
                new[] { "Paint", "Erase", "Rectangle", "Pick" });

            state.brushSize = EditorGUILayout.IntSlider("Tamanho do pincel", Mathf.Max(1, state.brushSize), 1, 5);

            if (EditorGUI.EndChangeCheck())
            {
                state.Save();
                EnsureToolActive(); // mexeu no pincel/camada → já entra no modo de pintar
            }
        }

        // ── Palette de tiles ─────────────────────────────────────────────

        private void DrawPalette(LevelBuilderState state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Palette (Floor/Wall)", EditorStyles.boldLabel);
                if (GUILayout.Button("Atualizar", GUILayout.Width(80f))) RefreshPalette();
                if (GUILayout.Button("Gerar Tiles…", GUILayout.Width(90f))) TilePaletteGenerator.GenerateFromFolderDialog();
            }

            if (_tilePalette.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum Tile encontrado. Use 'Gerar Tiles…' a partir de uma pasta de sprites.", MessageType.Info);
                return;
            }

            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / (TileButtonSize + 6f)));
            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);

            int i = 0;
            while (i < _tilePalette.Count)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < columns && i < _tilePalette.Count; c++, i++)
                        DrawTileButton(state, _tilePalette[i]);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTileButton(LevelBuilderState state, TileBase tile)
        {
            var preview = AssetPreview.GetAssetPreview(tile) ?? AssetPreview.GetMiniThumbnail(tile);
            bool selected = state.selectedTile == tile;

            var prev = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);

            var content = new GUIContent(preview, tile.name);
            if (GUILayout.Button(content, GUILayout.Width(TileButtonSize), GUILayout.Height(TileButtonSize)))
            {
                state.selectedTile = tile;
                state.brush = BuildBrush.Paint;
                state.Save();
                EnsureToolActive(); // selecionou um tile → já entra no modo de pintar
            }

            GUI.backgroundColor = prev;
        }

        private void RefreshPalette()
        {
            _tilePalette.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:TileBase"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null) _tilePalette.Add(tile);
            }
            Repaint();
        }

        // ── Palette de mobília (FurnitureDefinitionSO) ───────────────────

        private void DrawFurniturePalette(LevelBuilderState state)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Mobília", EditorStyles.boldLabel);
                if (GUILayout.Button("Atualizar", GUILayout.Width(80f))) RefreshFurniture();
            }

            EditorGUI.BeginChangeCheck();
            state.furnitureDirection = (IsoDirection)EditorGUILayout.EnumPopup("Direção (R cicla — Fase 3b)", state.furnitureDirection);
            if (EditorGUI.EndChangeCheck()) state.Save();

            if (_furniturePalette.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhuma FurnitureDefinitionSO no projeto. Sem arte ainda? Crie um placeholder pra validar a colocação.", MessageType.Info);
                if (GUILayout.Button("Criar mobília placeholder"))
                    CreatePlaceholderFurniture();
                return;
            }

            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / (TileButtonSize + 6f)));
            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);

            int i = 0;
            while (i < _furniturePalette.Count)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < columns && i < _furniturePalette.Count; c++, i++)
                        DrawFurnitureButton(state, _furniturePalette[i]);
                }
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Criar mobília placeholder"))
                CreatePlaceholderFurniture();
        }

        private void DrawFurnitureButton(LevelBuilderState state, FurnitureDefinitionSO furniture)
        {
            var preview = AssetPreview.GetAssetPreview(furniture) ?? AssetPreview.GetMiniThumbnail(furniture);
            bool selected = state.selectedFurniture == furniture;

            var prev = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.85f, 0.4f, 1f);

            var content = new GUIContent(preview, furniture.name);
            if (GUILayout.Button(content, GUILayout.Width(TileButtonSize), GUILayout.Height(TileButtonSize)))
            {
                state.selectedFurniture = furniture;
                state.brush = BuildBrush.Paint;
                state.Save();
                EnsureToolActive();
            }

            GUI.backgroundColor = prev;
        }

        private void RefreshFurniture()
        {
            _furniturePalette.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:FurnitureDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<FurnitureDefinitionSO>(path);
                if (so != null) _furniturePalette.Add(so);
            }
            Repaint();
        }

        /// <summary>
        /// Cria uma <see cref="FurnitureDefinitionSO"/> mínima (sem arte) para validar a colocação
        /// na Fase 3a. O <c>Id</c> é atribuído pelo DefinitionAssetIdAssigner na importação; a peça
        /// renderiza um placeholder até receber sprites reais.
        /// </summary>
        private void CreatePlaceholderFurniture()
        {
            const string parent = "Assets/Data";
            const string folder = "Assets/Data/Furniture";
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, "Furniture");

            var so   = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/PlaceholderFurniture.asset");
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshFurniture();

            var state = LevelBuilderState.instance;
            state.selectedFurniture = so;
            state.brush = BuildBrush.Paint;
            state.Save();
            EnsureToolActive();
        }
    }
}
