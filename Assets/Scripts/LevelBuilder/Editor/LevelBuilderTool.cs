using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using GameLike.LevelBuilder.Components;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>
    /// Ferramenta de SceneView do Level Builder (LEVEL_BUILDER.md §14): pinta na
    /// célula sob o cursor com o pincel/camada/andar ativos do
    /// <see cref="LevelBuilderState"/>. A interação usa <c>Event.current</c>/IMGUI
    /// (não o Input System de runtime), com ghost translúcido, overlay do grid
    /// isométrico, leitura da coordenada <c>(x,y,storey)</c> e Undo por stroke.
    ///
    /// O preview é a própria cena (URP 2D real) — esta tool só escreve no
    /// Tilemap; o resultado renderizado É o resultado final.
    /// </summary>
    [EditorTool("Level Builder")]
    public sealed class LevelBuilderTool : EditorTool
    {
        private const int OverlayRadius = 8; // células ao redor do cursor no overlay

        private bool       _dragging;
        private Vector3Int _dragStartCell;

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;

            var state = LevelBuilderState.instance;

            if (state.layer == BuildLayer.Furniture)
            {
                OnFurnitureGUI(state);
                return;
            }

            var tilemap = LevelBuilderSceneRefs.GetTilemap(state.storey, state.layer);

            if (tilemap == null)
            {
                DrawHint(state.layer);
                return;
            }

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            var e = Event.current;
            if (!TryGetCellUnderMouse(tilemap, out var cell))
                return;

            DrawGridOverlay(tilemap, cell);
            DrawGhost(tilemap, cell, state);
            DrawReadout(cell, state);

            HandleBrush(e, tilemap, cell, state);

            if (e.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }

        // ── Conversão mouse → célula ─────────────────────────────────────

        private static bool TryGetCellUnderMouse(Tilemap tilemap, out Vector3Int cell)
        {
            cell = default;
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            // Sprites 2D vivem no plano z=0; o andar/offset desloca em X/Y, capturado pelo transform do tilemap.
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return false;

            var world = ray.GetPoint(enter);
            // WorldToCell do PRÓPRIO tilemap (usa o transform dele) → a célula calculada é exatamente
            // a mesma onde o tile será renderizado, mesmo que o tilemap esteja deslocado.
            cell = tilemap.WorldToCell(world);
            cell.z = 0; // Floor/Wall vivem em z=0 (LEVEL_BUILDER.md §7/§8)
            return true;
        }

        // ── Brush dispatch ───────────────────────────────────────────────

        private void HandleBrush(Event e, Tilemap tilemap, Vector3Int cell, LevelBuilderState state)
        {
            switch (state.brush)
            {
                case BuildBrush.Paint:  HandlePaintErase(e, tilemap, cell, state, erase: false); break;
                case BuildBrush.Erase:  HandlePaintErase(e, tilemap, cell, state, erase: true);  break;
                case BuildBrush.Rectangle: HandleRectangle(e, tilemap, cell, state); break;
                case BuildBrush.Pick:   HandlePick(e, tilemap, cell, state); break;
            }
        }

        private void HandlePaintErase(Event e, Tilemap tilemap, Vector3Int cell, LevelBuilderState state, bool erase)
        {
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (e.button != 0 || e.alt) return;
            if (!erase && state.selectedTile == null) return;

            if (e.type == EventType.MouseDown)
                Undo.RegisterCompleteObjectUndo(tilemap, erase ? "Level Builder Erase" : "Level Builder Paint");

            foreach (var baseCell in BrushCells(cell, state.brushSize))
                foreach (var c in LayerCells(baseCell, state))
                    tilemap.SetTile(c, erase ? null : state.selectedTile);

            MarkDirty(tilemap);
            e.Use();
        }

        private void HandleRectangle(Event e, Tilemap tilemap, Vector3Int cell, LevelBuilderState state)
        {
            if (e.button != 0 || e.alt) return;

            if (e.type == EventType.MouseDown)
            {
                _dragging = true;
                _dragStartCell = cell;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _dragging)
            {
                _dragging = false;
                Undo.RegisterCompleteObjectUndo(tilemap, "Level Builder Rectangle");
                foreach (var baseCell in RectCells(_dragStartCell, cell))
                    foreach (var c in LayerCells(baseCell, state))
                        tilemap.SetTile(c, state.selectedTile);
                MarkDirty(tilemap);
                e.Use();
            }
        }

        private void HandlePick(Event e, Tilemap tilemap, Vector3Int cell, LevelBuilderState state)
        {
            if (e.type != EventType.MouseDown || e.button != 0) return;
            var picked = tilemap.GetTile(cell);
            if (picked != null)
            {
                state.selectedTile = picked;
                state.brush = BuildBrush.Paint; // conta-gotas volta a pintar
                state.Save();
            }
            e.Use();
        }

        // ── Mobília (GameObject data-driven, não Tilemap) ────────────────

        /// <summary>
        /// Caminho de edição da camada Furniture (LEVEL_BUILDER.md §9): mobília é GameObject
        /// (<see cref="FurnitureInstance"/>), não tile. Usa o Tilemap_Floor só como referência de
        /// célula (mesmo snap do chão) e coloca/remove instâncias sob o root "Furniture" do andar.
        /// Fase 3a: footprint 1×1, direção fixa; multi-célula + rotação são a 3b.
        /// </summary>
        private void OnFurnitureGUI(LevelBuilderState state)
        {
            var grid  = LevelBuilderSceneRefs.GetGrid();
            var floor = LevelBuilderSceneRefs.GetTilemap(state.storey, BuildLayer.Floor);
            if (grid == null || floor == null)
            {
                DrawHint(state.layer);
                return;
            }

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            var e = Event.current;
            if (!TryGetCellUnderMouse(floor, out var cell))
                return;

            DrawGridOverlay(floor, cell);
            DrawFurnitureGhost(floor, cell, state);
            DrawReadout(cell, state);

            HandleFurnitureBrush(e, grid, cell, state);

            if (e.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }

        private void HandleFurnitureBrush(Event e, UnityEngine.Grid grid, Vector3Int cell, LevelBuilderState state)
        {
            if (e.type != EventType.MouseDown || e.button != 0 || e.alt) return;

            switch (state.brush)
            {
                case BuildBrush.Erase:
                    EraseFurnitureAt(cell, state);
                    break;
                case BuildBrush.Pick:
                    PickFurnitureAt(cell, state);
                    break;
                default: // Paint / Rectangle → coloca uma peça na célula
                    if (state.selectedFurniture == null) return;
                    PlaceFurniture(grid, cell, state);
                    break;
            }
            e.Use();
        }

        private void PlaceFurniture(UnityEngine.Grid grid, Vector3Int cell, LevelBuilderState state)
        {
            var root = LevelBuilderSceneRefs.GetFurnitureRoot(state.storey);
            if (root == null) return;

            int group = Undo.GetCurrentGroup();

            // Uma peça por célula-âncora: substitui a anterior em vez de empilhar duplicatas.
            EraseFurnitureAt(cell, state);

            var go = new GameObject(state.selectedFurniture.name);
            go.transform.SetParent(root, false);
            var instance = go.AddComponent<FurnitureInstance>();
            instance.Configure(state.selectedFurniture, cell, state.furnitureDirection, state.storey, state.furnitureBaseLevel, grid);

            // Registrar o objeto já montado: o Undo destrói o root e, com ele, os renderers das camadas.
            Undo.RegisterCreatedObjectUndo(go, "Place Furniture");
            // Colapsa apagar-antigo + criar-novo num único Ctrl+Z.
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        private void EraseFurnitureAt(Vector3Int cell, LevelBuilderState state)
        {
            var root = LevelBuilderSceneRefs.GetFurnitureRoot(state.storey);
            if (root == null) return;

            foreach (var inst in root.GetComponentsInChildren<FurnitureInstance>(includeInactive: true))
                if (inst.AnchorCell == cell && inst.Storey == state.storey)
                    Undo.DestroyObjectImmediate(inst.gameObject);
        }

        private void PickFurnitureAt(Vector3Int cell, LevelBuilderState state)
        {
            var root = LevelBuilderSceneRefs.GetFurnitureRoot(state.storey);
            if (root == null) return;

            foreach (var inst in root.GetComponentsInChildren<FurnitureInstance>(includeInactive: true))
                if (inst.AnchorCell == cell && inst.Definition != null)
                {
                    state.selectedFurniture = inst.Definition;
                    state.furnitureDirection = inst.Direction;
                    state.brush = BuildBrush.Paint; // conta-gotas volta a colocar
                    state.Save();
                    return;
                }
        }

        private void DrawFurnitureGhost(Tilemap floor, Vector3Int cell, LevelBuilderState state)
        {
            bool erasing = state.brush == BuildBrush.Erase;
            var fill = erasing
                ? new Color(1f, 0.30f, 0.30f, 0.25f)
                : (state.selectedFurniture != null ? new Color(0.85f, 0.40f, 1f, 0.30f)
                                                   : new Color(1f, 1f, 1f, 0.12f));

            DrawCellDiamond(floor, cell, fill, outline: true, fillAlpha: true);

            // Quando houver arte (Fase 3b traz sprite por direção), mostra o ghost translúcido real.
            var sprite = FirstSprite(state.selectedFurniture);
            if (!erasing && sprite != null)
                DrawTileSpriteGhost(floor, cell, sprite, alpha: 0.5f);
        }

        private static Sprite FirstSprite(FurnitureDefinitionSO def)
        {
            if (def == null || def.VisualLayers == null) return null;
            foreach (var layer in def.VisualLayers)
                if (layer.sprite != null) return layer.sprite;
            return null;
        }

        // ── Desenho (Handles) ────────────────────────────────────────────

        private void DrawGhost(Tilemap tilemap, Vector3Int cell, LevelBuilderState state)
        {
            // O pincel sempre destaca o DIAMANTE da célula (o footprint), no MESMO referencial do
            // tilemap — é onde o tile vai assentar. Por cima, quando há tile selecionado, mostramos
            // o ghost translúcido do sprite real (também no referencial do tilemap).
            bool erasing = state.brush == BuildBrush.Erase;
            var sprite   = erasing ? null : TryGetTileSprite(state.selectedTile);
            var fill      = erasing ? new Color(1f, 0.30f, 0.30f, 0.25f) : new Color(0.30f, 0.80f, 1f, 0.25f);

            var cells = (_dragging && state.brush == BuildBrush.Rectangle)
                ? RectCells(_dragStartCell, cell)
                : BrushCells(cell, state.brushSize);

            foreach (var baseCell in cells)
                foreach (var c in LayerCells(baseCell, state))
                {
                    DrawCellDiamond(tilemap, c, fill, outline: true, fillAlpha: true);
                    if (sprite != null)
                        DrawTileSpriteGhost(tilemap, c, sprite, alpha: 0.5f);
                }
        }

        /// <summary>
        /// Desenha o sprite do tile translúcido na posição em que o Tilemap o renderizaria
        /// (mesma âncora/pivô), convertido para coordenadas de tela. Resolve o "ghost
        /// desalinhado" em tiles com altura: o preview aparece exatamente sobre o bloco.
        /// </summary>
        private static void DrawTileSpriteGhost(Tilemap tilemap, Vector3Int cell, Sprite sprite, float alpha)
        {
            var tex = sprite.texture;
            if (tex == null) return;

            // Posição do pivô calculada pelo PRÓPRIO tilemap → casa exatamente com onde o tile é
            // renderizado (mesmo transform). O tileAnchor é coordenada FRACIONÁRIA de célula.
            Vector3 pivotLocal = tilemap.CellToLocalInterpolated((Vector3)cell + tilemap.tileAnchor);
            Vector3 pivotWorld = tilemap.LocalToWorld(pivotLocal);

            float ppu  = sprite.pixelsPerUnit;
            float wW   = sprite.rect.width  / ppu;
            float wH   = sprite.rect.height / ppu;
            var pivotN = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);

            Vector3 bottomLeft = pivotWorld - new Vector3(pivotN.x * wW, pivotN.y * wH, 0f);
            Vector3 topLeft    = bottomLeft + new Vector3(0f, wH, 0f);
            Vector3 bottomRight = bottomLeft + new Vector3(wW, 0f, 0f);

            Vector2 tlGui = HandleUtility.WorldToGUIPoint(topLeft);
            Vector2 brGui = HandleUtility.WorldToGUIPoint(bottomRight);
            var rect = new Rect(tlGui.x, tlGui.y, brGui.x - tlGui.x, brGui.y - tlGui.y);

            var uv = new Rect(
                sprite.rect.x / tex.width,
                sprite.rect.y / tex.height,
                sprite.rect.width / tex.width,
                sprite.rect.height / tex.height);

            Handles.BeginGUI();
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
            GUI.color = prev;
            Handles.EndGUI();
        }

        private static Sprite TryGetTileSprite(TileBase tileBase)
        {
            return tileBase is Tile t ? t.sprite : null;
        }

        private void DrawGridOverlay(Tilemap layout, Vector3Int center)
        {
            var line = new Color(1f, 1f, 1f, 0.10f);
            for (int dx = -OverlayRadius; dx <= OverlayRadius; dx++)
                for (int dy = -OverlayRadius; dy <= OverlayRadius; dy++)
                    DrawCellDiamond(layout, new Vector3Int(center.x + dx, center.y + dy, 0), line, outline: true, fillAlpha: false);
        }

        private static void DrawCellDiamond(Tilemap layout, Vector3Int cell, Color color, bool outline, bool fillAlpha = true)
        {
            // Os 4 cantos do diamante = os 4 pontos de lattice da célula, via tilemap.CellToWorld —
            // no MESMO referencial em que o tile é renderizado. Assim o destaque do pincel cai SEMPRE
            // colado na célula real onde o tile vai aparecer (não no grid se ele estiver deslocado).
            var corners = new[]
            {
                layout.CellToWorld(cell),
                layout.CellToWorld(cell + new Vector3Int(1, 0, 0)),
                layout.CellToWorld(cell + new Vector3Int(1, 1, 0)),
                layout.CellToWorld(cell + new Vector3Int(0, 1, 0)),
            };

            if (fillAlpha)
            {
                Handles.color = color;
                Handles.DrawAAConvexPolygon(corners);
            }
            if (outline)
            {
                Handles.color = new Color(color.r, color.g, color.b, Mathf.Max(color.a, 0.5f));
                Handles.DrawAAPolyLine(2f, corners[0], corners[1], corners[2], corners[3], corners[0]);
            }
        }

        private static void DrawReadout(Vector3Int cell, LevelBuilderState state)
        {
            Handles.BeginGUI();
            var rect = new Rect(8f, 8f, 260f, 54f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 16f),
                $"Célula ({cell.x}, {cell.y})  •  Andar {state.storey}");
            GUI.Label(new Rect(rect.x + 6f, rect.y + 22f, rect.width - 12f, 16f),
                $"Camada: {state.layer}   Pincel: {state.brush} ({state.brushSize}×{state.brushSize})");
            string thirdLine = state.layer == BuildLayer.Furniture
                ? (state.selectedFurniture != null
                    ? $"Mobília: {state.selectedFurniture.name}  •  Dir: {state.furnitureDirection}"
                    : "Mobília: (nenhuma)")
                : (state.selectedTile != null ? $"Tile: {state.selectedTile.name}" : "Tile: (nenhum)");
            GUI.Label(new Rect(rect.x + 6f, rect.y + 38f, rect.width - 12f, 16f), thirdLine);
            Handles.EndGUI();
        }

        private static void DrawHint(BuildLayer layer)
        {
            Handles.BeginGUI();
            var rect = new Rect(8f, 8f, 360f, 38f);
            GUI.Box(rect, GUIContent.none);
            string msg = LevelBuilderSceneRefs.SceneIsSetup()
                ? $"Camada '{layer}' ainda não implementada (Fase 1 = Floor)."
                : "Cena sem fundação. Rode Tools → GameLike → Level Builder → Setup Scene.";
            GUI.Label(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, 32f), msg);
            Handles.EndGUI();
        }

        // ── Helpers de células ───────────────────────────────────────────

        /// <summary>
        /// Expande uma célula-base (x,y) nas células a afetar conforme a camada: Floor coloca um
        /// tile em z=0; Wall empilha uma coluna no eixo Z (de wallBaseLevel até +wallHeight),
        /// formando paredes de vários blocos.
        /// </summary>
        private static IEnumerable<Vector3Int> LayerCells(Vector3Int baseCell, LevelBuilderState state)
        {
            if (state.layer == BuildLayer.Wall)
            {
                int z0 = Mathf.Max(0, state.wallBaseLevel);
                int h  = Mathf.Max(1, state.wallHeight);
                for (int z = z0; z < z0 + h; z++)
                    yield return new Vector3Int(baseCell.x, baseCell.y, z);
            }
            else
            {
                yield return new Vector3Int(baseCell.x, baseCell.y, 0);
            }
        }

        private static IEnumerable<Vector3Int> BrushCells(Vector3Int center, int size)
        {
            int half = (size - 1) / 2;
            for (int dx = -half; dx <= size - 1 - half; dx++)
                for (int dy = -half; dy <= size - 1 - half; dy++)
                    yield return new Vector3Int(center.x + dx, center.y + dy, 0);
        }

        private static IEnumerable<Vector3Int> RectCells(Vector3Int a, Vector3Int b)
        {
            int minX = Mathf.Min(a.x, b.x), maxX = Mathf.Max(a.x, b.x);
            int minY = Mathf.Min(a.y, b.y), maxY = Mathf.Max(a.y, b.y);
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    yield return new Vector3Int(x, y, 0);
        }

        private static void MarkDirty(Tilemap tilemap)
        {
            EditorUtility.SetDirty(tilemap);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
        }
    }
}
