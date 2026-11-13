using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using GameLike.UI.Gallery;
using GameLike.UI.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.MapBuilder
{
    /// <summary>Ferramenta ativa do Map Builder.</summary>
    public enum MapTool { Furniture, Seats, Lights }

    /// <summary>
    /// Map Builder in-game: o dev monta o cenário de cada fase em 3D, vendo o resultado como
    /// a partida mostra. Escolhe um móvel na paleta e clica no chão para colocar, arrasta para
    /// mover, gira com R (a peça gira de verdade), define assentos e luzes, e salva — o jogo
    /// passa a usar o cenário salvo na fase marcada. Este componente liga ponteiro/teclado às
    /// operações da <see cref="MapBuilderSession"/>; não contém regra de gameplay.
    /// </summary>
    public sealed class MapBuilderController : MonoBehaviour
    {
        private const string PanelResource = "HudPanelSettings";

        private MapBuilderSession _session;
        private MapBuilderStage _stage;
        private MapBuilderCamera _camera;
        private MapBuilderInput _input;
        private MapBuilderHud _hud;
        private FurnitureThumbnails _thumbnails;

        private int _rotation;
        private Vector2Int _align;
        private bool _panning;
        private Vector2 _lastPointer;
        private int _dragIndex = -1;
        private Vector2Int _dragOrigin, _dragCell;
        private Vector2 _grabOffset;
        private bool _dragMoved;

        internal MapBuilderSession Session => _session;
        internal MapBuilderStage Stage => _stage;
        internal MapBuilderCamera View => _camera;
        internal MapBuilderHud Hud => _hud;

        /// <summary>Ponteiro e clique sintéticos — só para o roteiro de captura de validação.</summary>
        internal Vector2? PointerOverride { get; set; }
        internal bool ClickOverride { get; set; }

        /// <summary>Ferramenta ativa.</summary>
        public MapTool Tool { get; private set; } = MapTool.Furniture;

        /// <summary>Móvel armado para colocar (null = modo selecionar).</summary>
        public FurnitureDef3DSO Armed { get; private set; }

        /// <summary>Índice da peça selecionada (-1 = nenhuma).</summary>
        public int Selected { get; private set; } = -1;

        /// <summary>Rotação (passos de 90°) com que a peça armada será colocada.</summary>
        public int ArmedRotation => _rotation;

        /// <summary>Posição dentro da célula (centro, borda ou canto) com que a peça armada será colocada.</summary>
        public Vector2Int ArmedAlign => _align;

        // ------------------------------------------------------------------ ciclo de vida

        private void Awake()
        {
            Time.timeScale = 1f;
            var catalog = OfficeMapStore.Catalog;
            var panel = Resources.Load<PanelSettings>(PanelResource);
            if (panel == null)
            {
                Debug.LogError("[MapBuilder] PanelSettings '" + PanelResource + "' não encontrado em Resources.");
                enabled = false;
                return;
            }
            if (catalog == null)
                Debug.LogError("[MapBuilder] Catálogo de mobília ausente (Resources/" + FurnitureCatalogSO.ResourcePath
                               + "). No editor: Tools → GameLike → Office 3D → 8 - Sincronizar catálogo de mobília.");

            Sfx.Ensure();
            Kit.UiSound = kind => Sfx.Play(kind == 0 ? SfxId.Hover : SfxId.Click);

            _session = new MapBuilderSession(catalog);
            int missing = _session.Load(InitialMap());
            _camera = new MapBuilderCamera();
            _stage = new MapBuilderStage(_session, _camera.Camera);
            _input = new MapBuilderInput();
            _thumbnails = new FurnitureThumbnails(Kit.Sand);
            _hud = new MapBuilderHud(this, gameObject, panel, catalog, _thumbnails);
            if (missing > 0) _hud.Toast(missing + " peça(s) do cenário não existem mais e foram descartadas.", true);

            string autoshot = MapBuilderBoot.AutoshotFolder();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (autoshot != null) gameObject.AddComponent<MapBuilderAutoshot>().Begin(this, autoshot);
#endif
        }

        private void OnDestroy()
        {
            _input?.Dispose();
            _thumbnails?.Dispose();
        }

        /// <summary>Abre o cenário em uso na Garagem; senão o primeiro salvo; senão o asset do projeto; senão vazio.</summary>
        private static OfficeMapData InitialMap()
        {
            var saved = OfficeMapStore.ListAll();
            foreach (var map in saved)
                if (map.usedInGame) return map;
            if (saved.Count > 0) return saved[0];
            var assets = Resources.LoadAll<OfficeLayoutSO>("Office");
            if (assets.Length > 0) return assets[0].ToMapData(assets[0].name, true);
            return MapBuilderSession.Blank(CompanyPhase.Garage, "Garagem");
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _camera.GameSize = OfficeGrid.GameCameraSize(_session.Layout.Cols, _session.Layout.Rows);
            _thumbnails.Tick();
            _hud.Tick(dt);

            Vector2 pointer = PointerOverride ?? _input.PointerPosition;
            bool modal = _hud.ModalOpen;
            bool typing = _hud.IsTyping;
            bool overUi = modal || _hud.IsPointerOverUi(pointer);

            if (!modal) DriveCamera(pointer, overUi, typing, dt);
            _lastPointer = pointer;
            _camera.Tick(dt);
            _stage.FaceCamera(_camera.Camera, _camera.TargetForward);

            if (modal)
            {
                _stage.HideCursor();
                return;
            }
            if (!typing) HandleKeys();
            HandlePointer(pointer, overUi);
        }

        private void DriveCamera(Vector2 pointer, bool overUi, bool typing, float dt)
        {
            if (_input.PanDrag.WasPressedThisFrame() && !overUi) _panning = true;
            if (!_input.PanDrag.IsPressed()) _panning = false;
            if (_panning) _camera.DragPixels(pointer - _lastPointer);

            if (!overUi)
            {
                float scroll = _input.Zoom.ReadValue<float>();
                if (Mathf.Abs(scroll) > 0.01f) _camera.ZoomBy(Mathf.Sign(scroll));
            }
            if (!typing && !_input.Control.IsPressed())
                _camera.PanAxes(_input.Pan.ReadValue<Vector2>(), dt);
        }

        // ------------------------------------------------------------------ teclado

        private void HandleKeys()
        {
            if (_input.Undo.WasPressedThisFrame()) { Undo(); return; }
            if (_input.Redo.WasPressedThisFrame()) { Redo(); return; }
            if (_input.Save.WasPressedThisFrame()) { Save(); return; }
            if (_input.Duplicate.WasPressedThisFrame()) { DuplicateSelected(); return; }
            if (_input.Control.IsPressed()) return;

            if (_input.Rotate.WasPressedThisFrame()) RotateCurrent(1);
            if (_input.Remove.WasPressedThisFrame()) RemoveSelected();
            if (_input.Cancel.WasPressedThisFrame()) Cancel();
            if (_input.Guides.WasPressedThisFrame()) SetGuides(!_stage.Guides);
            if (_input.ViewLeft.WasPressedThisFrame()) _camera.Turn(-1);
            if (_input.ViewRight.WasPressedThisFrame()) _camera.Turn(1);
            if (_input.ViewHome.WasPressedThisFrame()) _camera.GameView();
        }

        private void Cancel()
        {
            if (_dragIndex >= 0) { EndDrag(false); return; }
            if (Armed != null) { Disarm(); return; }
            if (Selected >= 0) { Select(-1); return; }
            _hud.AskExit();
        }

        // ------------------------------------------------------------------ ponteiro

        private void HandlePointer(Vector2 pointer, bool overUi)
        {
            Ray ray = _camera.Camera.ScreenPointToRay(pointer);
            bool onGround = _stage.GroundPoint(ray, out Vector2 point);
            var hoverCell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            bool click = (_input.Primary.WasPressedThisFrame() || ClickOverride) && !overUi;
            ClickOverride = false;
            if (click) _hud.BlurText();

            switch (Tool)
            {
                case MapTool.Furniture when Armed != null:
                    PointerPlacing(point, onGround && !overUi, click);
                    break;
                case MapTool.Furniture:
                    PointerSelecting(ray, point, onGround, overUi, click);
                    break;
                case MapTool.Seats:
                    PointerSeats(ray, overUi, click);
                    break;
                case MapTool.Lights:
                    PointerLights(hoverCell, onGround && !overUi, click);
                    break;
            }

            if (_input.Secondary.WasPressedThisFrame() && !overUi)
            {
                if (Armed != null) Disarm();
                else Select(-1);
            }
        }

        private void PointerPlacing(Vector2 point, bool active, bool click)
        {
            if (!active)
            {
                _stage.DropGhost();
                _stage.HideCursor();
                return;
            }
            var footprint = OfficeGrid.RotatedFootprint(Armed.FootprintInCells, _rotation);
            var cell = ClampToRoom(OfficeLayoutRules.AnchorCell(point, footprint), footprint);
            bool valid = _session.Resolve(Armed, cell, _rotation, _align, -1, out bool onTop);
            _stage.ShowGhost(Armed, cell, _rotation, _align, onTop, valid);
            if (!click) return;

            int index = _session.Place(Armed, cell, _rotation, _align);
            if (index < 0)
            {
                Sfx.Play(SfxId.Fail);
                _hud.Toast(onTop ? "Já tem uma peça nesse ponto do móvel — escolha outro canto na \"Posição na célula\"." : "Não cabe aqui — já tem uma peça ocupando esse espaço.", true);
                return;
            }
            _stage.AddPiece(index);
            Sfx.Play(SfxId.Point);
            _hud.Refresh();
        }

        private void PointerSelecting(Ray ray, Vector2 point, bool onGround, bool overUi, bool click)
        {
            if (click)
            {
                int index = _stage.PickPiece(ray);
                Select(index);
                if (index >= 0 && onGround)
                {
                    _dragIndex = index;
                    _dragOrigin = _dragCell = _session.Layout.EditorPlacements[index].cell;
                    _grabOffset = point - new Vector2(_dragOrigin.x, _dragOrigin.y);
                    _dragMoved = false;
                }
            }

            if (_dragIndex >= 0)
            {
                if (_input.Primary.IsPressed()) ContinueDrag(point, onGround);
                else EndDrag(true);
                return;
            }

            int hover = overUi ? -1 : _stage.PickPiece(ray);
            if (hover >= 0 && hover != Selected)
            {
                var p = _session.Layout.EditorPlacements[hover];
                _stage.ShowCursor(p.cell, OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps), MapBuilderStage.HoverColor);
            }
            else _stage.HideCursor();
        }

        private void ContinueDrag(Vector2 point, bool onGround)
        {
            if (!onGround) return;
            var p = _session.Layout.EditorPlacements[_dragIndex];
            var footprint = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
            var cell = ClampToRoom(new Vector2Int(Mathf.RoundToInt(point.x - _grabOffset.x), Mathf.RoundToInt(point.y - _grabOffset.y)), footprint);
            bool onTop = p.onTop;
            bool valid = cell == _dragOrigin || _session.Resolve(p.furniture, cell, p.rotationSteps, p.align, _dragIndex, out onTop);
            if (cell != _dragCell)
            {
                _dragCell = cell;
                _dragMoved = true;
                _stage.PreviewPieceAt(_dragIndex, cell, onTop);
            }
            if (!_dragMoved) return;
            _stage.HideSelection();
            _stage.ShowCursor(cell, footprint, valid ? MapBuilderStage.ValidColor : MapBuilderStage.InvalidColor);
        }

        private void EndDrag(bool commit)
        {
            int index = _dragIndex;
            _dragIndex = -1;
            if (_dragMoved)
            {
                if (commit && _dragCell != _dragOrigin)
                {
                    int before = _session.Layout.Placements.Count;
                    if (_session.Move(index, _dragCell))
                    {
                        Sfx.Play(SfxId.Point);
                        if (_session.Layout.Placements.Count != before) Selected = -1;
                        _stage.SyncPieces();
                        _stage.HideCursor();
                        _stage.ShowSelection(Selected);
                        _hud.Refresh();
                        return;
                    }
                    else
                    {
                        Sfx.Play(SfxId.Fail);
                        _hud.Toast("Não cabe aí — a peça voltou para o lugar.", true);
                    }
                }
                _stage.SettlePiece(index);
            }
            _stage.HideCursor();
            _stage.ShowSelection(Selected);
            _hud.Refresh();
        }

        private void PointerSeats(Ray ray, bool overUi, bool click)
        {
            int hover = overUi ? -1 : _stage.PickPiece(ray);
            if (hover >= 0 && _session.IsChair(hover))
            {
                var p = _session.Layout.EditorPlacements[hover];
                _stage.ShowCursor(p.cell, OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps), MapBuilderStage.HoverColor);
            }
            else _stage.HideCursor();
            if (!click) return;

            if (_session.ToggleSeat(hover))
            {
                Sfx.Play(SfxId.Point);
                _stage.RefreshDerived();
                _hud.Refresh();
            }
            else _hud.Toast("Assento precisa de uma cadeira: clique numa cadeira.", true);
        }

        private void PointerLights(Vector2Int cell, bool active, bool click)
        {
            if (!active || !_stage.InsideRoom(cell))
            {
                _stage.HideCursor();
                return;
            }
            _stage.ShowCursor(cell, Vector2Int.one, MapBuilderStage.HoverColor);
            if (!click || !_session.ToggleLight(cell)) return;
            Sfx.Play(SfxId.Point);
            _stage.RefreshDerived();
            _hud.Refresh();
        }

        private Vector2Int ClampToRoom(Vector2Int cell, Vector2Int footprint) =>
            new Vector2Int(Mathf.Clamp(cell.x, 0, Mathf.Max(0, _session.Layout.Cols - footprint.x)),
                           Mathf.Clamp(cell.y, 0, Mathf.Max(0, _session.Layout.Rows - footprint.y)));

        // ------------------------------------------------------------------ intenções (HUD e atalhos)

        /// <summary>Troca a ferramenta ativa.</summary>
        public void SetTool(MapTool tool)
        {
            Tool = tool;
            Disarm();
            Select(-1);
            if (tool != MapTool.Furniture && !_stage.Guides) SetGuides(true);
            _hud.Refresh();
        }

        /// <summary>Arma um móvel da paleta: ele passa a seguir o ponteiro até ser colocado.</summary>
        public void Arm(FurnitureDef3DSO def)
        {
            if (def == null) return;
            Tool = MapTool.Furniture;
            Select(-1);
            Armed = Armed == def ? null : def;
            _align = Vector2Int.zero;
            if (Armed == null) { _stage.DropGhost(); _stage.HideCursor(); }
            else if (!Armed.Rotatable) _rotation = 0;
            _hud.Refresh();
        }

        public void Disarm()
        {
            if (Armed == null) return;
            Armed = null;
            _stage.DropGhost();
            _stage.HideCursor();
            _hud.Refresh();
        }

        public void Select(int index)
        {
            Selected = _session.Valid(index) ? index : -1;
            _stage.ShowSelection(Selected);
            _hud.Refresh();
        }

        /// <summary>Gira a peça armada ou, se não houver, a selecionada.</summary>
        public void RotateCurrent(int direction)
        {
            if (Armed != null)
            {
                if (Armed.Rotatable) _rotation = ((_rotation + direction) % 4 + 4) % 4;
                _hud.Refresh();
                return;
            }
            if (Selected < 0) return;
            if (!_session.Rotate(Selected, direction))
            {
                Sfx.Play(SfxId.Fail);
                _hud.Toast("Não dá para girar aqui — a peça bateria em outra ou sairia da sala.", true);
                return;
            }
            _stage.SyncPieces();
            _stage.ShowSelection(Selected);
            _hud.Refresh();
        }

        /// <summary>
        /// Posição dentro da célula — (0,0) centro, ±1 encosta na borda/canto — da peça armada
        /// ou, se não houver, da selecionada.
        /// </summary>
        public void SetAlign(Vector2Int align)
        {
            if (Armed != null) _align = align;
            else if (Selected >= 0)
            {
                var current = _session.Layout.EditorPlacements[Selected];
                if (current.align != align && !_session.SetAlign(Selected, align))
                {
                    Sfx.Play(SfxId.Fail);
                    _hud.Toast("Já tem outra peça nesse ponto do móvel.", true);
                    return;
                }
                _stage.SyncPieces();
                _stage.ShowSelection(Selected);
            }
            _hud.Refresh();
        }

        /// <summary>Troca o acabamento (liso, tijolinho, tábuas…) de chão e parede.</summary>
        public void SetStyles(SurfaceStyle floor, SurfaceStyle wall)
        {
            _session.SetStyles(floor, wall);
            _stage.RefreshShell();
        }

        /// <summary>Parede lateral própria (cor + acabamento) ou igual à do fundo.</summary>
        public void SetSideWall(bool custom, Color color, SurfaceStyle style, bool record)
        {
            _session.SetSideWall(custom, color, style, record);
            _stage.RefreshShell();
        }

        public void RemoveSelected()
        {
            if (Selected < 0) return;
            int index = Selected;
            Selected = -1;
            _session.Remove(index);
            _stage.RemovePiece(index);
            _stage.SyncPieces();
            _stage.HideSelection();
            Sfx.Play(SfxId.Close);
            _hud.Refresh();
        }

        /// <summary>Arma outra peça igual à selecionada, já na mesma rotação.</summary>
        public void DuplicateSelected()
        {
            if (Selected < 0) return;
            var p = _session.Layout.EditorPlacements[Selected];
            _rotation = p.rotationSteps;
            Armed = null;
            Arm(p.furniture);
            _align = p.align;
            _hud.Refresh();
        }

        public void ToggleSeatOnSelected()
        {
            if (!_session.ToggleSeat(Selected)) return;
            _stage.RefreshDerived();
            _hud.Refresh();
        }

        public void ShiftSeat(int seat, int direction) { _session.ShiftSeat(seat, direction); AfterSeatEdit(); }
        public void TurnSeat(int seat) { _session.TurnSeat(seat); AfterSeatEdit(); }
        public void RemoveSeat(int seat) { _session.RemoveSeat(seat); AfterSeatEdit(); }
        public void CycleSeatActivity(int seat) { _session.CycleSeatActivity(seat); AfterSeatEdit(); }

        private void AfterSeatEdit()
        {
            _stage.RefreshDerived();
            _hud.Refresh();
        }

        public void Undo() => AfterHistory(_session.Undo());
        public void Redo() => AfterHistory(_session.Redo());

        private void AfterHistory(bool changed)
        {
            if (!changed) return;
            ResetInteraction();
            _stage.RebuildAll();
            _hud.Refresh();
        }

        private void ResetInteraction()
        {
            Armed = null;
            Selected = -1;
            _dragIndex = -1;
        }

        public void ResizeBy(int deltaCols, int deltaRows)
        {
            int before = _session.Layout.Placements.Count;
            if (!_session.Resize(_session.Layout.Cols + deltaCols, _session.Layout.Rows + deltaRows)) return;
            int lost = before - _session.Layout.Placements.Count;
            ResetInteraction();
            _stage.RebuildAll();
            if (lost > 0) _hud.Toast(lost + " peça(s) ficaram fora da sala e foram removidas (Ctrl+Z desfaz).", true);
            _hud.Refresh();
        }

        public void SetPhase(CompanyPhase phase)
        {
            _session.SetPhase(phase);
            _hud.Refresh();
        }

        public void SetColors(Color floor, Color wall, bool record)
        {
            _session.SetColors(floor, wall, record);
            _stage.RefreshShell();
        }

        public void SetGuides(bool on)
        {
            _stage.SetGuides(on);
            _hud.Refresh();
        }

        public void SetPeople(bool on)
        {
            _stage.SetPeople(on);
            _hud.Refresh();
        }

        public void Rename(string name)
        {
            if (name == _session.Name) return;
            _session.Name = name;
            _session.Touch();
        }

        public void SetUsedInGame(bool used)
        {
            _session.UsedInGame = used;
            _session.Touch();
            _hud.Refresh();
        }

        /// <summary>Abre um cenário (descarta o que estava aberto — o HUD confirma antes).</summary>
        public void OpenMap(OfficeMapData data)
        {
            int missing = _session.Load(data);
            ResetInteraction();
            _stage.RebuildAll();
            _camera.GameView();
            _hud.Refresh();
            if (missing > 0) _hud.Toast(missing + " peça(s) do cenário não existem mais e foram descartadas.", true);
        }

        /// <summary>Grava o cenário aberto. Devolve false se não foi possível.</summary>
        public bool Save()
        {
            _session.Name = OfficeMapStore.SanitizeName(_session.Name);
            string path;
            try { path = OfficeMapStore.Save(_session.Snapshot()); }
            catch (System.IO.IOException e)
            {
                Sfx.Play(SfxId.Fail);
                _hud.Toast("Não consegui salvar: " + e.Message, true);
                return false;
            }
            _session.MarkSaved();
            Sfx.Play(SfxId.Success);
            string where = Application.isEditor ? "Assets/Resources/OfficeMaps/" + _session.Name + ".json" : path;
            string use = _session.UsedInGame
                ? " O jogo usa este cenário na fase " + Labels.Phase(_session.Layout.Phase) + "."
                : " (não marcado para uso no jogo)";
            _hud.Toast("Salvo em " + where + "." + use);
            if (_session.Layout.Seats.Count == 0)
                _hud.Toast("Atenção: sem assentos, nenhum boneco aparece. Use a aba Assentos e clique numa cadeira.", true);
            _hud.Refresh();
            return true;
        }

        public void Exit() => MapBuilderBoot.Close();
    }
}
