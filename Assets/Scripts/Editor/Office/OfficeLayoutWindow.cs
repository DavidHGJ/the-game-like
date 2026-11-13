using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.LevelBuilder;
using GameLike.Editor.Setup;
using GameLike.Systems.Office;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Office
{
    /// <summary>
    /// Map Builder — a ferramenta de produção para montar os cenários (escritórios) do jogo.
    /// Edita um <see cref="OfficeLayoutSO"/>: tamanho da área, lista completa de móveis com
    /// busca/filtro, arrastar da lista para a planta, mover, girar e remover peças, luzes de
    /// teto e os ASSENTOS (em que cadeira cada boneco senta — assento 0 = fundador, os demais
    /// na ordem de contratação). "Preview na cena" monta o cenário 3D pronto, com os bonecos.
    ///
    /// A planta é vista de cima: o topo é a parede do fundo e a esquerda é a parede lateral
    /// (as duas paredes que o jogo desenha). x cresce para a direita; a linha cresce para
    /// baixo, na direção da câmera.
    /// </summary>
    public sealed class OfficeLayoutWindow : EditorWindow
    {
        private enum Tool { Furniture, Seats, CeilingLight }

        private const string DefFolder = "Assets/Data/Furniture3D";
        private const string PreviewName = "OfficePreview";
        private const string DragKey = "GameLike.Furniture";
        private static readonly string[] ToolNames = { "Móveis", "Assentos", "Luz de teto" };
        private static readonly string[] CategoryNames = { "Todas", "Mesas", "Cadeiras", "Armazenamento", "Decoração", "Parede", "Chão" };

        private OfficeLayoutSO _layout;
        private FurnitureDef3DSO[] _palette = new FurnitureDef3DSO[0];
        private string _search = "";
        private int _categoryFilter;
        private Tool _tool = Tool.Furniture;
        private FurnitureDef3DSO _armed;
        private int _rotation;
        private int _selectedPlacement = -1, _selectedSeat = -1;
        private bool _moving;
        private Vector2Int _hoverCell = new Vector2Int(-1, -1);
        private Vector2 _paletteScroll, _inspectorScroll;
        private bool _autoPreview = true;
        private int _previewPeople = 1;
        private GUIStyle _centered, _tiny;

        [MenuItem("Tools/GameLike/Map Builder antigo (janela do editor)", priority = 20)]
        [MenuItem("Tools/GameLike/Office 3D/★ Level Builder 3D (criador de fases)")]
        public static void Open()
        {
            var w = GetWindow<OfficeLayoutWindow>("Map Builder");
            w.minSize = new Vector2(980, 620);
            w.ReloadPalette();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            ReloadPalette();
            if (_layout == null)
            {
                var all = Resources.LoadAll<OfficeLayoutSO>("Office");
                if (all.Length > 0) _layout = all[0];
            }
        }

        private void ReloadPalette()
        {
            var list = new List<FurnitureDef3DSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:FurnitureDef3DSO", new[] { DefFolder }))
            {
                var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null) list.Add(def);
            }
            list.Sort((a, b) => a.Category != b.Category
                ? a.Category.CompareTo(b.Category)
                : string.Compare(a.EditorName, b.EditorName, System.StringComparison.OrdinalIgnoreCase));
            _palette = list.ToArray();
        }

        // ------------------------------------------------------------------ GUI

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();
            if (_layout == null)
            {
                DrawEmptyState();
                return;
            }

            DrawRoomBar();
            HandleKeys();
            EditorGUILayout.BeginHorizontal();
            DrawPalette();
            DrawCanvas();
            DrawInspector();
            EditorGUILayout.EndHorizontal();
            DrawFooter();
        }

        private void EnsureStyles()
        {
            if (_centered != null) return;
            _centered = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _centered.normal.textColor = Color.white;
            _tiny = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Cenário:", GUILayout.Width(56));
            var picked = (OfficeLayoutSO)EditorGUILayout.ObjectField(_layout, typeof(OfficeLayoutSO), false, GUILayout.Width(240));
            if (picked != _layout) { _layout = picked; Deselect(); }
            if (GUILayout.Button("Novo cenário…", EditorStyles.toolbarDropDown, GUILayout.Width(110))) ShowNewMenu();
            GUILayout.FlexibleSpace();
            _tool = (Tool)GUILayout.Toolbar((int)_tool, ToolNames, EditorStyles.toolbarButton, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Recarregar móveis", EditorStyles.toolbarButton, GUILayout.Width(120))) ReloadPalette();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("Escolha um cenário acima ou crie um novo. Cada fase da empresa (Garagem, Indie, Escritório, Estúdio) " +
                                    "usa o cenário salvo em Assets/Resources/Office com aquela fase.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            foreach (CompanyPhase phase in System.Enum.GetValues(typeof(CompanyPhase)))
                if (GUILayout.Button("Novo cenário: " + PhaseLabel(phase), GUILayout.Height(32))) CreateForPhase(phase);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Gerar cenário de teste (Indie, com 3 assentos)", GUILayout.Height(28)))
            {
                _layout = MapBuilderSetup.CreateTestScenario();
                Deselect();
            }
        }

        private void ShowNewMenu()
        {
            var menu = new GenericMenu();
            foreach (CompanyPhase phase in System.Enum.GetValues(typeof(CompanyPhase)))
            {
                var captured = phase;
                menu.AddItem(new GUIContent("Cenário vazio para a fase " + PhaseLabel(phase)), false, () => CreateForPhase(captured));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Cenário de teste (Indie, com 3 assentos)"), false, () => { _layout = MapBuilderSetup.CreateTestScenario(); Deselect(); });
            menu.AddItem(new GUIContent("Escolher onde salvar…"), false, CreateLayoutAsset);
            menu.ShowAsContext();
        }

        private void DrawRoomBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Fase:", GUILayout.Width(34));
            var phase = (CompanyPhase)EditorGUILayout.Popup((int)_layout.Phase,
                new[] { "Garagem", "Indie", "Escritório", "Estúdio" }, GUILayout.Width(100));
            if (phase != _layout.Phase) { Record("Fase do cenário"); _layout.EditorSetPhase(phase); Changed(); }

            GUILayout.Space(12);
            GUILayout.Label("Área (células):", GUILayout.Width(88));
            int cols = EditorGUILayout.DelayedIntField(_layout.Cols, GUILayout.Width(40));
            GUILayout.Label("×", GUILayout.Width(12));
            int rows = EditorGUILayout.DelayedIntField(_layout.Rows, GUILayout.Width(40));
            if (GUILayout.Button("−", EditorStyles.miniButtonLeft, GUILayout.Width(22))) { cols--; rows--; }
            if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(22))) { cols++; rows++; }
            if ((cols != _layout.Cols || rows != _layout.Rows) && cols >= 2 && rows >= 2 && cols <= 40 && rows <= 40)
                Resize(cols, rows);

            GUILayout.Space(12);
            var floor = EditorGUILayout.ColorField(GUIContent.none, _layout.FloorColor, false, false, false, GUILayout.Width(40));
            GUILayout.Label("chão", GUILayout.Width(32));
            var wall = EditorGUILayout.ColorField(GUIContent.none, _layout.WallColor, false, false, false, GUILayout.Width(40));
            GUILayout.Label("parede", GUILayout.Width(44));
            if (floor != _layout.FloorColor || wall != _layout.WallColor)
            {
                Record("Cores do cenário");
                _layout.EditorSetColors(floor, wall);
                Changed();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(_layout.EditorPlacements.Count + " peças · " + _layout.EditorSeats.Count + " assentos · "
                            + _layout.EditorCeilingLights.Count + " luzes", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ lista de móveis

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Móveis (" + _palette.Length + ")", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            _categoryFilter = EditorGUILayout.Popup(_categoryFilter, CategoryNames);
            GUILayout.Label("Clique para armar e clique na planta — ou arraste direto para a planta. R gira.", _tiny);

            if (_palette.Length == 0)
                EditorGUILayout.HelpBox("Nenhum móvel em " + DefFolder + ". Rode Tools → GameLike → Office 3D → 1 - Create Furniture Defs.", MessageType.Warning);

            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);
            var e = Event.current;
            for (int i = 0; i < _palette.Length; i++)
            {
                var def = _palette[i];
                if (_categoryFilter > 0 && (int)def.Category != _categoryFilter - 1) continue;
                if (_search.Length > 0 && def.EditorName.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0
                                       && def.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                Rect row = GUILayoutUtility.GetRect(230, 54, GUILayout.ExpandWidth(true));
                bool armed = _armed == def;
                EditorGUI.DrawRect(row, armed ? new Color(0.17f, 0.62f, 0.56f, 0.55f) : new Color(0f, 0f, 0f, i % 2 == 0 ? 0.10f : 0.04f));
                EditorGUI.DrawRect(new Rect(row.x, row.y, 4, row.height), CategoryColor(def.Category));

                var thumbRect = new Rect(row.x + 8, row.y + 3, 48, 48);
                Texture thumb = def.Prefab != null ? AssetPreview.GetAssetPreview(def.Prefab) : null;
                if (thumb != null) GUI.DrawTexture(thumbRect, thumb, ScaleMode.ScaleToFit);
                else
                {
                    EditorGUI.DrawRect(thumbRect, new Color(0f, 0f, 0f, 0.2f));
                    if (def.Prefab != null && AssetPreview.IsLoadingAssetPreview(def.Prefab.GetInstanceID())) Repaint();
                }
                GUI.Label(new Rect(row.x + 62, row.y + 6, row.width - 66, 18), def.EditorName, EditorStyles.boldLabel);
                GUI.Label(new Rect(row.x + 62, row.y + 26, row.width - 66, 18),
                    def.FootprintInCells.x + "×" + def.FootprintInCells.y + " · " + CategoryNames[(int)def.Category + 1]
                    + (def.IsWallPiece ? " · na parede" : ""), EditorStyles.miniLabel);

                if (e.type == EventType.MouseDown && e.button == 0 && row.Contains(e.mousePosition))
                {
                    _armed = armed ? null : def;
                    _tool = Tool.Furniture;
                    _selectedPlacement = -1; _selectedSeat = -1;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && row.Contains(e.mousePosition))
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(DragKey, def);
                    DragAndDrop.objectReferences = new Object[] { def };
                    DragAndDrop.StartDrag(def.EditorName);
                    _armed = def;
                    _tool = Tool.Furniture;
                    e.Use();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------ planta

        private void DrawCanvas()
        {
            Rect area = GUILayoutUtility.GetRect(200, 4000, 200, 4000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(area, new Color(0.16f, 0.16f, 0.17f));

            int cols = _layout.Cols, rows = _layout.Rows;
            float cell = Mathf.Floor(Mathf.Min((area.width - 48f) / cols, (area.height - 48f) / rows));
            cell = Mathf.Clamp(cell, 10f, 72f);
            var origin = new Vector2(Mathf.Round(area.x + (area.width - cell * cols) * 0.5f), Mathf.Round(area.y + (area.height - cell * rows) * 0.5f) + 6f);
            var grid = new Rect(origin.x, origin.y, cell * cols, cell * rows);

            // chão + paredes (topo = fundo, esquerda = lateral)
            EditorGUI.DrawRect(grid, _layout.FloorColor * 0.9f + new Color(0, 0, 0, 1f) * 0.1f);
            EditorGUI.DrawRect(new Rect(grid.x - 8, grid.y - 8, grid.width + 8, 8), _layout.WallColor);
            EditorGUI.DrawRect(new Rect(grid.x - 8, grid.y - 8, 8, grid.height + 8), _layout.WallColor);
            GUI.Label(new Rect(grid.x, grid.y - 26, grid.width, 16), "parede do fundo", _centered);
            GUI.Label(new Rect(grid.x, grid.yMax + 4, grid.width, 16), "▼ câmera", _centered);
            for (int x = 1; x < cols; x++) EditorGUI.DrawRect(new Rect(grid.x + x * cell, grid.y, 1, grid.height), new Color(0, 0, 0, 0.14f));
            for (int y = 1; y < rows; y++) EditorGUI.DrawRect(new Rect(grid.x, grid.y + y * cell, grid.width, 1), new Color(0, 0, 0, 0.14f));

            var e = Event.current;
            bool inside = grid.Contains(e.mousePosition);
            var hover = inside
                ? new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - grid.x) / cell), Mathf.FloorToInt((e.mousePosition.y - grid.y) / cell))
                : new Vector2Int(-1, -1);
            if (hover != _hoverCell) { _hoverCell = hover; Repaint(); }

            var placements = _layout.EditorPlacements;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < placements.Count; i++)
                {
                    var p = placements[i];
                    if (p.furniture == null) continue;
                    bool floorPiece = p.furniture.Category == FurnitureCategory.FloorFixture;
                    if ((pass == 0) != floorPiece) continue;
                    if (_moving && i == _selectedPlacement) continue;
                    DrawPiece(grid, cell, p.furniture, p.cell, p.rotationSteps, i == _selectedPlacement ? 1f : 0.82f, i == _selectedPlacement);
                }
            }

            foreach (var light in _layout.EditorCeilingLights)
            {
                var r = CellRect(grid, cell, light.cell, Vector2Int.one);
                GUI.Label(r, "☀", _centered);
                EditorGUI.DrawRect(new Rect(r.x + 2, r.y + 2, r.width - 4, 3), new Color(1f, 0.85f, 0.35f, 0.9f));
            }

            var seats = _layout.EditorSeats;
            for (int i = 0; i < seats.Count; i++)
            {
                var r = CellRect(grid, cell, seats[i].cell, Vector2Int.one);
                float size = Mathf.Min(r.width, r.height) * 0.62f;
                var badge = new Rect(r.center.x - size * 0.5f, r.center.y - size * 0.5f, size, size);
                EditorGUI.DrawRect(badge, i == _selectedSeat ? Color.white : (i == 0 ? new Color(0.95f, 0.7f, 0.15f) : new Color(0.2f, 0.75f, 0.9f)));
                var inner = new Rect(badge.x + 2, badge.y + 2, badge.width - 4, badge.height - 4);
                EditorGUI.DrawRect(inner, i == 0 ? new Color(0.75f, 0.5f, 0.05f) : new Color(0.1f, 0.45f, 0.6f));
                GUI.Label(inner, i == 0 ? "F" : i.ToString(), _centered);
            }

            // fantasma: peça armada / sendo movida / arrastada da lista
            FurnitureDef3DSO ghost = null; int ghostRot = _rotation;
            if (_tool == Tool.Furniture)
            {
                if (_moving && _selectedPlacement >= 0 && _selectedPlacement < placements.Count)
                {
                    ghost = placements[_selectedPlacement].furniture; ghostRot = placements[_selectedPlacement].rotationSteps;
                }
                else if (_armed != null) ghost = _armed;
            }
            if (ghost != null && inside)
            {
                bool ok = CanPlace(ghost, hover, ghostRot, _moving ? _selectedPlacement : -1);
                DrawPiece(grid, cell, ghost, hover, ghostRot, 0.55f, false, ok ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f));
            }
            else if (inside && _tool != Tool.Furniture)
            {
                EditorGUI.DrawRect(CellRect(grid, cell, hover, Vector2Int.one), new Color(1f, 1f, 1f, 0.18f));
            }

            HandleCanvasInput(e, area, inside, hover);
        }

        private void DrawPiece(Rect grid, float cell, FurnitureDef3DSO def, Vector2Int at, int rot, float alpha, bool selected, Color? tint = null)
        {
            var fp = OfficeGrid.RotatedFootprint(def.FootprintInCells, rot);
            var r = CellRect(grid, cell, at, fp);
            var body = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
            Color c = tint ?? CategoryColor(def.Category);
            c.a = def.Category == FurnitureCategory.FloorFixture ? alpha * 0.45f : alpha;
            EditorGUI.DrawRect(body, c);
            if (selected)
            {
                EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2), Color.white); EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), Color.white);
                EditorGUI.DrawRect(new Rect(r.x, r.y, 2, r.height), Color.white); EditorGUI.DrawRect(new Rect(r.xMax - 2, r.y, 2, r.height), Color.white);
            }
            // marca a "frente" da peça (para onde ela aponta): 0 = câmera, 1 = direita, 2 = fundo, 3 = esquerda
            const float t = 4f;
            Rect front = (rot % 4) switch
            {
                0 => new Rect(body.x + body.width * 0.25f, body.yMax - t, body.width * 0.5f, t),
                1 => new Rect(body.xMax - t, body.y + body.height * 0.25f, t, body.height * 0.5f),
                2 => new Rect(body.x + body.width * 0.25f, body.y, body.width * 0.5f, t),
                _ => new Rect(body.x, body.y + body.height * 0.25f, t, body.height * 0.5f)
            };
            EditorGUI.DrawRect(front, new Color(1f, 1f, 1f, alpha));
            if (cell >= 22f) GUI.Label(body, def.EditorName, _centered);
        }

        private static Rect CellRect(Rect grid, float cell, Vector2Int at, Vector2Int footprint) =>
            new Rect(grid.x + at.x * cell, grid.y + at.y * cell, footprint.x * cell, footprint.y * cell);

        private void HandleCanvasInput(Event e, Rect area, bool inside, Vector2Int hover)
        {
            // arrastar da lista para a planta
            if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && area.Contains(e.mousePosition))
            {
                var def = DragAndDrop.GetGenericData(DragKey) as FurnitureDef3DSO;
                if (def != null)
                {
                    DragAndDrop.visualMode = inside && CanPlace(def, hover, _rotation, -1) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    if (e.type == EventType.DragPerform && inside)
                    {
                        DragAndDrop.AcceptDrag();
                        Place(def, hover, _rotation);
                        _armed = null;
                    }
                    e.Use();
                    Repaint();
                }
                return;
            }

            if (!inside)
            {
                if (e.type == EventType.MouseUp) _moving = false;
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                RemoveAt(hover);
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                GUI.FocusControl(null);
                switch (_tool)
                {
                    case Tool.Furniture:
                        if (_armed != null) Place(_armed, hover, _rotation);
                        else
                        {
                            _selectedPlacement = PlacementAt(hover);
                            _selectedSeat = -1;
                            _moving = _selectedPlacement >= 0;
                        }
                        break;
                    case Tool.Seats: ClickSeat(hover); break;
                    case Tool.CeilingLight: ToggleCeilingLight(hover); break;
                }
                e.Use();
                Repaint();
                return;
            }
            if (e.type == EventType.MouseDrag && _moving) { e.Use(); Repaint(); return; }
            if (e.type == EventType.MouseUp && _moving)
            {
                _moving = false;
                MoveSelected(hover);
                e.Use();
                Repaint();
            }
        }

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;
            switch (e.keyCode)
            {
                case KeyCode.R:
                    if (_armed != null || _selectedPlacement < 0) _rotation = (_rotation + 1) % 4;
                    else RotateSelected(1);
                    e.Use(); Repaint();
                    break;
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (_selectedSeat >= 0) RemoveSeat(_selectedSeat);
                    else if (_selectedPlacement >= 0) RemovePlacement(_selectedPlacement);
                    e.Use(); Repaint();
                    break;
                case KeyCode.Escape:
                    _armed = null; Deselect();
                    e.Use(); Repaint();
                    break;
            }
        }

        // ------------------------------------------------------------------ painel da direita

        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(270));
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            var placements = _layout.EditorPlacements;
            var seats = _layout.EditorSeats;

            GUILayout.Label("Seleção", EditorStyles.boldLabel);
            if (_armed != null)
            {
                EditorGUILayout.HelpBox("Colocando: " + _armed.EditorName + " (" + _rotation * 90 + "°).\nClique na planta. R gira, Esc cancela.", MessageType.None);
                if (GUILayout.Button("Girar 90° (R)")) _rotation = (_rotation + 1) % 4;
            }
            else if (_selectedPlacement >= 0 && _selectedPlacement < placements.Count)
            {
                var p = placements[_selectedPlacement];
                GUILayout.Label(p.furniture != null ? p.furniture.EditorName : "(peça sem definição)", EditorStyles.largeLabel);
                GUILayout.Label("Célula " + p.cell.x + ", " + p.cell.y + " · " + p.rotationSteps * 90 + "°", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("⟲ Girar")) RotateSelected(-1);
                if (GUILayout.Button("Girar ⟳ (R)")) RotateSelected(1);
                EditorGUILayout.EndHorizontal();
                if (p.furniture != null && p.furniture.Category == FurnitureCategory.Chair)
                {
                    int seat = SeatAt(p.cell);
                    if (seat < 0) { if (GUILayout.Button("Tornar assento (boneco senta aqui)")) AddSeat(p.cell); }
                    else GUILayout.Label("Esta cadeira é o assento " + SeatLabel(seat) + ".", _tiny);
                }
                if (GUILayout.Button("Remover peça (Del)")) RemovePlacement(_selectedPlacement);
                GUILayout.Label("Arraste a peça na planta para mover.", _tiny);
            }
            else if (_selectedSeat >= 0 && _selectedSeat < seats.Count)
            {
                DrawSeatInspector(_selectedSeat);
            }
            else
            {
                GUILayout.Label(_tool == Tool.Furniture ? "Clique numa peça da planta para selecionar, ou escolha um móvel na lista."
                    : _tool == Tool.Seats ? "Clique numa cadeira para criar um assento ali. Clique num assento para ajustar."
                    : "Clique numa célula para pôr ou tirar uma luz de teto.", _tiny);
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("Assentos (quem senta onde)", EditorStyles.boldLabel);
            var prefab = (GameObject)EditorGUILayout.ObjectField("Boneco", _layout.CharacterPrefab, typeof(GameObject), false);
            if (prefab != _layout.CharacterPrefab) { Record("Boneco do cenário"); _layout.EditorSetCharacterPrefab(prefab); Changed(); }
            for (int i = 0; i < seats.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                bool chair = OfficeBuilder.TryFindChairAt(_layout, seats[i].cell, out var chairPlacement);
                string where = chair ? chairPlacement.furniture.EditorName : "sem cadeira!";
                if (GUILayout.Toggle(_selectedSeat == i, SeatLabel(i) + " — " + where + " (" + seats[i].cell.x + "," + seats[i].cell.y + ")", EditorStyles.miniButton))
                {
                    if (_selectedSeat != i) { _selectedSeat = i; _selectedPlacement = -1; _armed = null; }
                }
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Width(22))) SwapSeats(i, i - 1);
                GUI.enabled = i < seats.Count - 1;
                if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(22))) SwapSeats(i, i + 1);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Criar assentos em todas as cadeiras")) AutoSeats();

            int needed = MaxTeam(_layout.Phase);
            if (seats.Count == 0)
                EditorGUILayout.HelpBox("Sem assentos: nenhum boneco aparece no cenário. O assento 0 é o do fundador.", MessageType.Warning);
            else if (seats.Count < needed)
                EditorGUILayout.HelpBox("A fase " + PhaseLabel(_layout.Phase) + " comporta " + needed + " pessoas (com o fundador), mas só há "
                                        + seats.Count + " assento(s). Quem não tiver assento não aparece.", MessageType.Info);
            if (_layout.CharacterPrefab == null)
                EditorGUILayout.HelpBox("Escolha o prefab do boneco para ele aparecer nos assentos.", MessageType.Warning);

            EditorGUILayout.Space(10);
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            _autoPreview = EditorGUILayout.ToggleLeft("Atualizar a cena a cada mudança", _autoPreview);
            int people = EditorGUILayout.IntSlider("Bonecos", _previewPeople, 0, Mathf.Max(1, seats.Count));
            if (people != _previewPeople) { _previewPeople = people; RefreshPreviewIfOpen(); }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSeatInspector(int index)
        {
            var seats = _layout.EditorSeats;
            var seat = seats[index];
            GUILayout.Label("Assento " + SeatLabel(index), EditorStyles.largeLabel);
            GUILayout.Label(index == 0 ? "É aqui que o fundador senta." : "Ocupado pelo " + index + "º funcionário contratado.", _tiny);
            int rot = EditorGUILayout.IntPopup("Giro do boneco", seat.rotationSteps, new[] { "0°", "90°", "180°", "270°" }, new[] { 0, 1, 2, 3 });
            Vector3 offset = EditorGUILayout.Vector3Field("Ajuste fino (m)", seat.offset);
            if (rot != seat.rotationSteps || offset != seat.offset)
            {
                Record("Ajustar assento");
                seat.rotationSteps = rot; seat.offset = offset;
                seats[index] = seat;
                Changed();
            }
            GUILayout.Label("X = lado, Y = altura, Z = para a frente do boneco.", _tiny);
            if (GUILayout.Button("Remover assento (Del)")) RemoveSeat(index);
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview na cena (cenário pronto)", GUILayout.Height(28))) RebuildPreview(true);
            if (GUILayout.Button("Salvar", GUILayout.Height(28), GUILayout.Width(110)))
            {
                EditorUtility.SetDirty(_layout);
                AssetDatabase.SaveAssets();
                ShowNotification(new GUIContent("Cenário salvo"));
            }
            if (GUILayout.Button("Limpar tudo", GUILayout.Height(28), GUILayout.Width(110))
                && EditorUtility.DisplayDialog("Limpar cenário", "Remover todas as peças, assentos e luzes deste cenário?", "Limpar", "Cancelar"))
            {
                Record("Limpar cenário");
                _layout.EditorPlacements.Clear(); _layout.EditorSeats.Clear(); _layout.EditorCeilingLights.Clear();
                Deselect();
                Changed();
            }
            EditorGUILayout.EndHorizontal();

            string path = AssetDatabase.GetAssetPath(_layout);
            bool inResources = path.Replace('\\', '/').Contains("/Resources/Office/");
            string status = inResources
                ? "O jogo usa este cenário na fase " + PhaseLabel(_layout.Phase) + "."
                : "Atenção: para o jogo usar este cenário, o asset precisa estar em Assets/Resources/Office.";
            foreach (var other in Resources.LoadAll<OfficeLayoutSO>("Office"))
                if (other != _layout && other.Phase == _layout.Phase && inResources)
                    status = "Atenção: \"" + other.name + "\" também está marcado para a fase " + PhaseLabel(_layout.Phase) + " — o jogo usa o primeiro que achar.";
            EditorGUILayout.LabelField(status + "   Botão direito = remover · R = girar · Del = apagar seleção · Esc = cancelar", EditorStyles.miniLabel);
        }

        // ------------------------------------------------------------------ operações

        private void Record(string what) => Undo.RecordObject(_layout, what);

        private void Changed()
        {
            EditorUtility.SetDirty(_layout);
            Repaint();
            if (_autoPreview) RefreshPreviewIfOpen();
        }

        private void Deselect()
        {
            _selectedPlacement = -1; _selectedSeat = -1; _moving = false;
        }

        private bool Blocks(FurnitureDef3DSO def) => def.Category != FurnitureCategory.FloorFixture && !def.IsWallPiece;

        private bool CanPlace(FurnitureDef3DSO def, Vector2Int cell, int rot, int ignoreIndex)
        {
            var fp = OfficeGrid.RotatedFootprint(def.FootprintInCells, rot);
            if (cell.x < 0 || cell.y < 0 || cell.x + fp.x > _layout.Cols || cell.y + fp.y > _layout.Rows) return false;
            if (!Blocks(def)) return true;
            var placements = _layout.EditorPlacements;
            for (int i = 0; i < placements.Count; i++)
            {
                if (i == ignoreIndex) continue;
                var p = placements[i];
                if (p.furniture == null || !Blocks(p.furniture)) continue;
                var ofp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                bool apart = cell.x + fp.x <= p.cell.x || p.cell.x + ofp.x <= cell.x || cell.y + fp.y <= p.cell.y || p.cell.y + ofp.y <= cell.y;
                if (!apart) return false;
            }
            return true;
        }

        private void Place(FurnitureDef3DSO def, Vector2Int cell, int rot)
        {
            if (!CanPlace(def, cell, rot, -1)) return;
            Record("Posicionar móvel");
            _layout.EditorPlacements.Add(new FurniturePlacement { furniture = def, cell = cell, rotationSteps = def.Rotatable ? rot : 0 });
            Changed();
        }

        /// <summary>Peça sob a célula: primeiro as que ocupam espaço, depois as de chão/parede.</summary>
        private int PlacementAt(Vector2Int cell)
        {
            var placements = _layout.EditorPlacements;
            int fallback = -1;
            for (int i = placements.Count - 1; i >= 0; i--)
            {
                var p = placements[i];
                if (p.furniture == null) continue;
                var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                if (cell.x < p.cell.x || cell.y < p.cell.y || cell.x >= p.cell.x + fp.x || cell.y >= p.cell.y + fp.y) continue;
                if (Blocks(p.furniture)) return i;
                if (fallback < 0) fallback = i;
            }
            return fallback;
        }

        private void MoveSelected(Vector2Int to)
        {
            var placements = _layout.EditorPlacements;
            if (_selectedPlacement < 0 || _selectedPlacement >= placements.Count) return;
            var p = placements[_selectedPlacement];
            if (p.furniture == null || to == p.cell || !CanPlace(p.furniture, to, p.rotationSteps, _selectedPlacement)) return;
            Record("Mover móvel");
            MoveSeatsWith(p, to);
            p.cell = to;
            placements[_selectedPlacement] = p;
            Changed();
        }

        /// <summary>Assentos atrelados a uma cadeira acompanham a cadeira quando ela é movida.</summary>
        private void MoveSeatsWith(FurniturePlacement p, Vector2Int to)
        {
            if (p.furniture.Category != FurnitureCategory.Chair) return;
            var seats = _layout.EditorSeats;
            var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
            for (int i = 0; i < seats.Count; i++)
            {
                var s = seats[i];
                if (s.cell.x < p.cell.x || s.cell.y < p.cell.y || s.cell.x >= p.cell.x + fp.x || s.cell.y >= p.cell.y + fp.y) continue;
                s.cell += to - p.cell;
                seats[i] = s;
            }
        }

        private void RotateSelected(int dir)
        {
            var placements = _layout.EditorPlacements;
            if (_selectedPlacement < 0 || _selectedPlacement >= placements.Count) return;
            var p = placements[_selectedPlacement];
            if (p.furniture == null || !p.furniture.Rotatable) return;
            int rot = (p.rotationSteps + dir + 4) % 4;
            if (!CanPlace(p.furniture, p.cell, rot, _selectedPlacement)) { ShowNotification(new GUIContent("Não cabe girada aqui")); return; }
            Record("Girar móvel");
            p.rotationSteps = rot;
            placements[_selectedPlacement] = p;
            Changed();
        }

        private void RemoveAt(Vector2Int cell)
        {
            if (_tool == Tool.CeilingLight) { ToggleCeilingLight(cell, true); return; }
            int seat = SeatAt(cell);
            if (_tool == Tool.Seats) { if (seat >= 0) RemoveSeat(seat); return; }
            int index = PlacementAt(cell);
            if (index >= 0) RemovePlacement(index);
        }

        private void RemovePlacement(int index)
        {
            var placements = _layout.EditorPlacements;
            if (index < 0 || index >= placements.Count) return;
            Record("Remover móvel");
            var p = placements[index];
            placements.RemoveAt(index);
            if (p.furniture != null && p.furniture.Category == FurnitureCategory.Chair)
            {
                // sem a cadeira, o assento dela deixa de fazer sentido
                var seats = _layout.EditorSeats;
                var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                for (int i = seats.Count - 1; i >= 0; i--)
                {
                    var s = seats[i];
                    if (s.cell.x >= p.cell.x && s.cell.y >= p.cell.y && s.cell.x < p.cell.x + fp.x && s.cell.y < p.cell.y + fp.y)
                        seats.RemoveAt(i);
                }
            }
            Deselect();
            Changed();
        }

        private int SeatAt(Vector2Int cell)
        {
            var seats = _layout.EditorSeats;
            bool chair = OfficeBuilder.TryFindChairAt(_layout, cell, out var p);
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i].cell == cell) return i;
                if (chair && OfficeBuilder.TryFindChairAt(_layout, seats[i].cell, out var other) && other.cell == p.cell) return i;
            }
            return -1;
        }

        private void ClickSeat(Vector2Int cell)
        {
            int existing = SeatAt(cell);
            _selectedPlacement = -1; _armed = null;
            if (existing >= 0) { _selectedSeat = existing; return; }
            if (!OfficeBuilder.TryFindChairAt(_layout, cell, out var chair))
            {
                ShowNotification(new GUIContent("Assento precisa de uma cadeira: clique numa cadeira"));
                return;
            }
            AddSeat(chair.cell);
        }

        private void AddSeat(Vector2Int cell)
        {
            Record("Criar assento");
            _layout.EditorSeats.Add(new SeatPoint { cell = cell });
            _selectedSeat = _layout.EditorSeats.Count - 1;
            if (_layout.CharacterPrefab == null) _layout.EditorSetCharacterPrefab(MapBuilderSetup.DefaultCharacter());
            _previewPeople = Mathf.Max(_previewPeople, _layout.EditorSeats.Count);
            Changed();
        }

        private void RemoveSeat(int index)
        {
            var seats = _layout.EditorSeats;
            if (index < 0 || index >= seats.Count) return;
            Record("Remover assento");
            seats.RemoveAt(index);
            _selectedSeat = -1;
            Changed();
        }

        private void SwapSeats(int a, int b)
        {
            var seats = _layout.EditorSeats;
            Record("Reordenar assentos");
            (seats[a], seats[b]) = (seats[b], seats[a]);
            _selectedSeat = b;
            Changed();
        }

        private void AutoSeats()
        {
            Record("Assentos automáticos");
            int added = MapBuilderSetup.AddSeatsOnChairs(_layout);
            _previewPeople = Mathf.Max(_previewPeople, _layout.EditorSeats.Count);
            ShowNotification(new GUIContent(added + " assento(s) criado(s)"));
            Changed();
        }

        private void ToggleCeilingLight(Vector2Int cell, bool removeOnly = false)
        {
            var list = _layout.EditorCeilingLights;
            int idx = list.FindIndex(c => c.cell == cell);
            if (idx < 0 && removeOnly) return;
            Record("Luz de teto");
            if (idx >= 0) list.RemoveAt(idx);
            else list.Add(new CeilingLight { cell = cell, color = new Color(1f, 0.95f, 0.85f), intensity = 2.2f });
            Changed();
        }

        private void Resize(int cols, int rows)
        {
            Record("Redimensionar área");
            _layout.EditorSetSize(cols, rows);
            // tira o que ficou para fora da nova área
            var placements = _layout.EditorPlacements;
            for (int i = placements.Count - 1; i >= 0; i--)
            {
                var p = placements[i];
                if (p.furniture == null) continue;
                var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                if (p.cell.x + fp.x > cols || p.cell.y + fp.y > rows) placements.RemoveAt(i);
            }
            _layout.EditorSeats.RemoveAll(s => s.cell.x >= cols || s.cell.y >= rows);
            _layout.EditorCeilingLights.RemoveAll(l => l.cell.x >= cols || l.cell.y >= rows);
            Deselect();
            Changed();
        }

        // ------------------------------------------------------------------ preview na cena

        private void RefreshPreviewIfOpen()
        {
            if (Application.isPlaying || GameObject.Find(PreviewName) == null) return;
            RebuildPreview(false);
        }

        private void RebuildPreview(bool frame)
        {
            if (Application.isPlaying)
            {
                ShowNotification(new GUIContent("Saia do Play para ver o preview"));
                return;
            }
            var root = GameObject.Find(PreviewName);
            if (root == null) root = new GameObject(PreviewName);
            OfficeBuilder.Build(_layout, root.transform);
            OfficeCrew.Populate(_layout, root.transform, _previewPeople);
            OfficeLighting.Apply();
            if (!frame) return;
            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();
        }

        // ------------------------------------------------------------------ criação

        private void CreateForPhase(CompanyPhase phase)
        {
            _layout = MapBuilderSetup.CreateEmpty(phase);
            Deselect();
        }

        private void CreateLayoutAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Novo cenário", "NewOfficeLayout", "asset",
                "Escolha onde salvar (use Assets/Resources/Office para o jogo carregar por fase).", "Assets/Resources/Office");
            if (string.IsNullOrEmpty(path)) return;
            var layout = CreateInstance<OfficeLayoutSO>();
            layout.EditorSetCharacterPrefab(MapBuilderSetup.DefaultCharacter());
            AssetDatabase.CreateAsset(layout, path);
            AssetDatabase.SaveAssets();
            _layout = layout;
            Deselect();
        }

        // ------------------------------------------------------------------ apoio

        private static string SeatLabel(int index) => index == 0 ? "F (fundador)" : "#" + index;

        private static string PhaseLabel(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Garage => "Garagem",
            CompanyPhase.Indie => "Indie",
            CompanyPhase.Office => "Escritório",
            _ => "Estúdio"
        };

        private static int MaxTeam(CompanyPhase phase)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            if (loop != null) return loop.MaxTeamForPhase(phase);
            return phase switch { CompanyPhase.Garage => 1, CompanyPhase.Indie => 3, CompanyPhase.Office => 6, _ => 9 };
        }

        private static Color CategoryColor(FurnitureCategory c) => c switch
        {
            FurnitureCategory.Desk => new Color(0.72f, 0.45f, 0.25f),
            FurnitureCategory.Chair => new Color(0.30f, 0.50f, 0.62f),
            FurnitureCategory.Storage => new Color(0.66f, 0.50f, 0.32f),
            FurnitureCategory.Decoration => new Color(0.30f, 0.60f, 0.35f),
            FurnitureCategory.WallFixture => new Color(0.55f, 0.45f, 0.65f),
            FurnitureCategory.FloorFixture => new Color(0.35f, 0.55f, 0.60f),
            _ => Color.gray
        };
    }
}
