using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using GameLike.UI.Gallery;
using GameLike.UI.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Interface do Map Builder (UI Toolkit, no visual do kit do jogo): barra do cenário em
    /// cima, paleta de móveis à esquerda, seleção/assentos/vista à direita, dicas embaixo,
    /// avisos e as janelas de abrir, novo, cores e confirmação. Só lê o estado e dispara
    /// intenções no <see cref="MapBuilderController"/>.
    /// </summary>
    internal sealed class MapBuilderHud
    {
        private static readonly string[] PhaseNames = { "Garagem", "Indie", "Escritório", "Estúdio" };
        private static readonly string[] ActivityNames = { "Trabalhando", "Pensando", "Relaxando", "Dormindo" };
        private static readonly string[] ToolNames = { "Móveis", "Assentos", "Luzes" };
        private static readonly string[] CategoryNames = { "Todas", "Mesas", "Cadeiras", "Armários", "Decoração", "Parede", "Chão" };

        private static readonly (SurfaceStyle style, string name)[] FloorStyles =
        {
            (SurfaceStyle.Plain, "Liso"), (SurfaceStyle.WoodPlanks, "Tábuas"), (SurfaceStyle.Tiles, "Piso"),
            (SurfaceStyle.Checker, "Xadrez"), (SurfaceStyle.Carpet, "Carpete"), (SurfaceStyle.Concrete, "Concreto"),
            (SurfaceStyle.Brick, "Tijolo")
        };

        private static readonly (SurfaceStyle style, string name)[] WallStyles =
        {
            (SurfaceStyle.Plain, "Lisa"), (SurfaceStyle.Brick, "Tijolinho"), (SurfaceStyle.WoodPanel, "Ripado"),
            (SurfaceStyle.Stripes, "Listras"), (SurfaceStyle.Tiles, "Azulejo"), (SurfaceStyle.Concrete, "Concreto")
        };

        private static readonly Color[] FloorSwatches =
        {
            Kit.RGB(158, 153, 143), Kit.RGB(168, 158, 143), Kit.RGB(196, 164, 124), Kit.RGB(150, 111, 78),
            Kit.RGB(104, 84, 68), Kit.RGB(120, 128, 138), Kit.RGB(84, 92, 104), Kit.RGB(206, 200, 188),
            Kit.RGB(110, 142, 132), Kit.RGB(92, 110, 150), Kit.RGB(170, 120, 110), Kit.RGB(60, 62, 70)
        };

        private static readonly Color[] WallSwatches =
        {
            Kit.RGB(140, 178, 128), Kit.RGB(143, 168, 189), Kit.RGB(236, 226, 205), Kit.RGB(214, 190, 150),
            Kit.RGB(196, 132, 110), Kit.RGB(126, 150, 172), Kit.RGB(168, 150, 190), Kit.RGB(232, 200, 110),
            Kit.RGB(110, 160, 160), Kit.RGB(204, 208, 214), Kit.RGB(96, 104, 120), Kit.RGB(220, 150, 160)
        };

        private sealed class PaletteRow { public FurnitureDef3DSO def; public VisualElement element; }
        private sealed class ToastEntry { public VisualElement element; public float life; }

        private readonly MapBuilderController _controller;
        private readonly FurnitureCatalogSO _catalog;
        private readonly FurnitureThumbnails _thumbnails;
        private readonly VisualElement _root, _modalLayer, _toastColumn;
        private readonly VisualElement _topDynamic, _toolTabs, _leftBody, _rightPanel, _palette;
        private readonly Label _hint;
        private readonly List<PaletteRow> _rows = new List<PaletteRow>();
        private readonly List<ToastEntry> _toasts = new List<ToastEntry>();
        private TextField _nameField;
        private VisualElement _saveButton;
        private string _search = "";
        private int _category;
        private bool _shownDirty;

        private MapBuilderSession Session => _controller.Session;

        /// <summary>true enquanto uma janela (abrir, novo, cores, confirmação) está aberta.</summary>
        public bool ModalOpen => _modalLayer.childCount > 0;

        /// <summary>true enquanto o dev digita num campo de texto (atalhos ficam suspensos).</summary>
        public bool IsTyping
        {
            get
            {
                var focused = _root.panel?.focusController?.focusedElement as VisualElement;
                return focused != null && (focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null);
            }
        }

        public MapBuilderHud(MapBuilderController controller, GameObject host, PanelSettings panel,
            FurnitureCatalogSO catalog, FurnitureThumbnails thumbnails)
        {
            _controller = controller;
            _catalog = catalog;
            _thumbnails = thumbnails;

            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.sortingOrder = 5;
            _root = document.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.pickingMode = PickingMode.Ignore;

            // barra superior
            var top = Kit.Card(0, 16);
            top.style.position = Position.Absolute;
            top.style.left = 14; top.style.right = 14; top.style.top = 12;
            top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
            top.Add(Kit.Button("Menu", Kit.IcArrowLeft, Kit.Sand, Kit.Ink, AskExit, 38));
            top.Add(Kit.Gap(14));
            _nameField = Widgets.Input(Session.Name, 200, 15, 40);
            _nameField.RegisterValueChangedCallback(e => _controller.Rename(e.newValue));
            top.Add(_nameField);
            top.Add(Kit.Gap(14));
            _topDynamic = Kit.Row();
            top.Add(_topDynamic);
            top.Add(Kit.Spacer());
            top.Add(Kit.Button("Novo", Kit.IcPlus, Kit.Sand, Kit.Ink, () => ConfirmDiscard(ShowNew), 38));
            top.Add(Kit.Gap(8));
            top.Add(Kit.Button("Abrir", Kit.IcFolder, Kit.Sand, Kit.Ink, () => ConfirmDiscard(ShowOpen), 38));
            top.Add(Kit.Gap(8));
            _saveButton = Kit.Button("Salvar", Kit.IcSave, Kit.Teal, Color.white, () => _controller.Save(), 38);
            top.Add(_saveButton);
            _root.Add(top);

            // painel esquerdo: ferramentas + paleta
            var left = Kit.Card(310, 16);
            left.style.position = Position.Absolute;
            left.style.left = 14; left.style.top = 82; left.style.bottom = 62;
            _toolTabs = new VisualElement();
            left.Add(_toolTabs);
            _leftBody = new VisualElement();
            _leftBody.style.flexGrow = 1; _leftBody.style.marginTop = 10;
            left.Add(_leftBody);
            _root.Add(left);
            _palette = BuildPalette();

            // painel direito
            _rightPanel = new VisualElement();
            _rightPanel.style.position = Position.Absolute;
            _rightPanel.style.right = 14; _rightPanel.style.top = 82; _rightPanel.style.width = 300;
            _rightPanel.pickingMode = PickingMode.Ignore;
            _root.Add(_rightPanel);

            // dicas
            var hintCard = Kit.Card(0, 12);
            hintCard.style.position = Position.Absolute;
            hintCard.style.left = 14; hintCard.style.bottom = 12;
            Kit.Pad(hintCard, 8, 14);
            _hint = Kit.Text("", 12, Kit.Soft, true);
            hintCard.Add(_hint);
            _root.Add(hintCard);

            // avisos
            _toastColumn = new VisualElement();
            _toastColumn.style.position = Position.Absolute;
            _toastColumn.style.left = 0; _toastColumn.style.right = 0; _toastColumn.style.top = 84;
            _toastColumn.style.alignItems = Align.Center;
            _toastColumn.pickingMode = PickingMode.Ignore;
            _root.Add(_toastColumn);

            _modalLayer = Kit.Fill();
            _modalLayer.pickingMode = PickingMode.Ignore;
            _root.Add(_modalLayer);

            Refresh();
        }

        // ------------------------------------------------------------------ consultas do controlador

        /// <summary>true se o ponteiro (pixels de tela, origem embaixo) está sobre algum painel.</summary>
        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            var panel = _root.panel;
            if (panel == null) return false;
            Vector2 point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return panel.Pick(point) != null;
        }

        /// <summary>Tira o foco do campo de texto (clicou na cena).</summary>
        public void BlurText()
        {
            var focused = _root.panel?.focusController?.focusedElement as VisualElement;
            focused?.Blur();
        }

        public void Tick(float deltaTime)
        {
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var toast = _toasts[i];
                toast.life -= deltaTime;
                if (toast.life < 0.4f) toast.element.style.opacity = Mathf.Clamp01(toast.life / 0.4f);
                if (toast.life > 0f) continue;
                toast.element.RemoveFromHierarchy();
                _toasts.RemoveAt(i);
            }
            if (Session.Dirty != _shownDirty) RefreshSaveButton();
        }

        public void Toast(string message, bool warning = false)
        {
            var card = Kit.Card(0, 12);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            card.style.marginBottom = 6; card.style.maxWidth = 760;
            card.pickingMode = PickingMode.Ignore;
            Kit.Pad(card, 8, 14);
            card.Add(Kit.Badge(warning ? Kit.IcWarn : Kit.IcCheck, warning ? Kit.Orange : Kit.Teal, 24));
            card.Add(Kit.Gap(10));
            var text = Kit.Wrap(message, 13, Kit.Ink, true);
            text.style.flexShrink = 1;
            card.Add(text);
            _toastColumn.Add(card);
            _toasts.Add(new ToastEntry { element = card, life = warning ? 5.5f : 4f });
            while (_toasts.Count > 4)
            {
                _toasts[0].element.RemoveFromHierarchy();
                _toasts.RemoveAt(0);
            }
        }

        // ------------------------------------------------------------------ atualização

        /// <summary>Redesenha tudo que depende do estado (seleção, ferramenta, cenário).</summary>
        public void Refresh()
        {
            if (_root == null || _topDynamic == null) return;
            if (!IsTyping && _nameField.value != Session.Name) _nameField.SetValueWithoutNotify(Session.Name);
            RefreshTop();
            RefreshLeft();
            RefreshRight();
            RefreshSaveButton();
            _hint.text = HintText();
        }

        private void RefreshSaveButton()
        {
            _shownDirty = Session.Dirty;
            _saveButton.style.backgroundColor = _shownDirty ? Kit.Blue : Kit.Teal;
            var label = LastLabel(_saveButton);
            if (label != null) label.text = _shownDirty ? "Salvar *" : "Salvo";
        }

        private static Label LastLabel(VisualElement element)
        {
            Label last = null;
            foreach (var child in element.Children())
                if (child is Label label) last = label;
            return last;
        }

        private string HintText()
        {
            const string view = "   ·   Vista: botão do meio arrasta, scroll aproxima, Q/E gira, Home = vista do jogo";
            if (_controller.Tool == MapTool.Seats) return "Clique numa cadeira para criar ou tirar o assento. F = fundador, 1, 2… = ordem de contratação." + view;
            if (_controller.Tool == MapTool.Lights) return "Clique numa célula para pôr ou tirar uma luz de teto." + view;
            if (_controller.Armed != null) return "Clique no chão para colocar · R gira · botão direito ou Esc solta a peça" + view;
            return "Clique seleciona · arraste move · R gira · Del remove · Ctrl+D duplica · Ctrl+Z desfaz · Ctrl+S salva" + view;
        }

        // ------------------------------------------------------------------ barra superior

        private void RefreshTop()
        {
            _topDynamic.Clear();
            var layout = Session.Layout;

            _topDynamic.Add(Widgets.Segmented(PhaseNames, (int)layout.Phase, i => _controller.SetPhase((CompanyPhase)i)));
            _topDynamic.Add(Kit.Gap(14));
            _topDynamic.Add(Stepper("Largura", layout.Cols, d => _controller.ResizeBy(d, 0)));
            _topDynamic.Add(Kit.Gap(10));
            _topDynamic.Add(Stepper("Fundo", layout.Rows, d => _controller.ResizeBy(0, d)));
            _topDynamic.Add(Kit.Gap(14));
            _topDynamic.Add(Swatch("Chão", layout.FloorColor, () => ShowSurface(Surface.Floor)));
            _topDynamic.Add(Kit.Gap(8));
            _topDynamic.Add(Swatch("Fundo", layout.WallColor, () => ShowSurface(Surface.BackWall)));
            _topDynamic.Add(Kit.Gap(8));
            _topDynamic.Add(Swatch("Lateral", layout.SideWallColor, () => ShowSurface(Surface.SideWall)));
            _topDynamic.Add(Kit.Gap(14));
            _topDynamic.Add(Widgets.Toggle(Session.UsedInGame, _controller.SetUsedInGame));
            _topDynamic.Add(Kit.Gap(8));
            _topDynamic.Add(Kit.Text("Usar no jogo", 13, Kit.Ink, true));
        }

        private static VisualElement Stepper(string label, int value, Action<int> onStep)
        {
            var row = Kit.Row();
            row.Add(Kit.Text(label, 12, Kit.Soft, true));
            row.Add(Kit.Gap(6));
            row.Add(Kit.RoundButton(Kit.IcMinus, Kit.Sand, Kit.Ink, 26, () => onStep(-1)));
            var number = Kit.Text(value.ToString(), 15, Kit.Ink, true);
            number.style.width = 30; number.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(number);
            row.Add(Kit.RoundButton(Kit.IcPlus, Kit.Sand, Kit.Ink, 26, () => onStep(1)));
            return row;
        }

        private static VisualElement Swatch(string label, Color color, Action onClick)
        {
            var row = Kit.Row();
            var chip = new VisualElement();
            chip.style.width = 30; chip.style.height = 30; chip.style.backgroundColor = color;
            Kit.Radius(chip, 9); Kit.Border(chip, Kit.Track, 2);
            Kit.Hover(chip, 1.1f, 0.92f);
            row.Add(chip);
            row.Add(Kit.Gap(6));
            row.Add(Kit.Text(label, 12, Kit.Soft, true));
            row.RegisterCallback<ClickEvent>(_ => onClick());
            return row;
        }

        // ------------------------------------------------------------------ painel esquerdo

        private void RefreshLeft()
        {
            _toolTabs.Clear();
            _toolTabs.Add(Widgets.Segmented(ToolNames, (int)_controller.Tool, i => _controller.SetTool((MapTool)i)));

            _leftBody.Clear();
            switch (_controller.Tool)
            {
                case MapTool.Furniture:
                    _leftBody.Add(_palette);
                    foreach (var row in _rows) PaintRow(row);
                    break;
                case MapTool.Seats:
                    _leftBody.Add(Kit.Wrap("Clique numa cadeira do cenário para ela virar um assento — é onde um boneco aparece sentado. " +
                                           "Clique de novo para tirar.", 13, Kit.Soft));
                    _leftBody.Add(Kit.Divider());
                    _leftBody.Add(SeatList());
                    break;
                default:
                    _leftBody.Add(Kit.Wrap("Clique numa célula do chão para pendurar uma luz de teto ali. Clique de novo para tirar.", 13, Kit.Soft));
                    _leftBody.Add(Kit.Divider());
                    _leftBody.Add(Kit.Text(Session.Layout.CeilingLights.Count + " luz(es) de teto no cenário", 13, Kit.Ink, true));
                    break;
            }
        }

        private VisualElement BuildPalette()
        {
            var wrap = new VisualElement();
            wrap.style.flexGrow = 1;

            var search = Widgets.Input("", 0, 14, 30);
            search.style.height = 36;
            search.textEdition.placeholder = "Buscar móvel…";
            search.RegisterValueChangedCallback(e => { _search = e.newValue ?? ""; FilterPalette(); });
            wrap.Add(search);

            var chips = Kit.Row();
            chips.style.flexWrap = Wrap.Wrap; chips.style.marginTop = 8; chips.style.marginBottom = 4;
            var chipElements = new List<VisualElement>();
            for (int i = 0; i < CategoryNames.Length; i++)
            {
                int index = i;
                var chip = Kit.Row();
                Kit.Pad(chip, 4, 10); Kit.Radius(chip, 10); Kit.Smooth(chip);
                chip.style.marginRight = 5; chip.style.marginBottom = 5;
                var label = Kit.Text(CategoryNames[i], 12, Kit.Soft, true);
                chip.Add(label);
                chip.RegisterCallback<ClickEvent>(_ =>
                {
                    _category = index;
                    Kit.Press();
                    PaintCategoryChips(chipElements);
                    FilterPalette();
                });
                chipElements.Add(chip);
                chips.Add(chip);
            }
            PaintCategoryChips(chipElements);
            wrap.Add(chips);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            wrap.Add(scroll);

            if (_catalog == null || _catalog.Furniture.Count == 0)
            {
                scroll.Add(Kit.Wrap("Nenhum móvel no catálogo. No editor: Tools → GameLike → Office 3D → 8 - Sincronizar catálogo de mobília.", 13, Kit.Red, true));
                return wrap;
            }

            foreach (var def in _catalog.Furniture)
            {
                if (def == null) continue;
                var row = new PaletteRow { def = def, element = PaletteItem(def) };
                _rows.Add(row);
                scroll.Add(row.element);
            }
            return wrap;
        }

        private void PaintCategoryChips(List<VisualElement> chips)
        {
            for (int i = 0; i < chips.Count; i++)
            {
                bool on = i == _category;
                chips[i].style.backgroundColor = on ? Kit.Teal : Kit.Sand;
                chips[i].Q<Label>().style.color = on ? Color.white : Kit.Soft;
            }
        }

        private VisualElement PaletteItem(FurnitureDef3DSO def)
        {
            var item = Kit.Row();
            item.style.marginBottom = 6; item.style.marginRight = 6;
            Kit.Radius(item, 12); Kit.Pad(item, 6, 8); Kit.Border(item, Kit.Track, 2);
            Kit.Smooth(item);

            var thumb = new Image { scaleMode = ScaleMode.ScaleToFit };
            thumb.style.width = 56; thumb.style.height = 56; thumb.style.flexShrink = 0;
            thumb.style.backgroundColor = Kit.Sand; Kit.Radius(thumb, 10);
            thumb.style.overflow = Overflow.Hidden;
            _thumbnails.Request(def, texture => thumb.image = texture);
            item.Add(thumb);
            item.Add(Kit.Gap(10));

            var info = Kit.Col();
            info.style.flexShrink = 1;
            info.Add(Kit.Text(def.EditorName, 14, Kit.Ink, true));
            string detail = def.FootprintInCells.x + "×" + def.FootprintInCells.y + " · " + CategoryNames[(int)def.Category + 1];
            if (def.IsWallPiece) detail += " · na parede";
            if (def.PlaceableOnTop) detail += " · vai em cima";
            if (def.Lights != null && def.Lights.Length > 0) detail += " · luz";
            var sub = Kit.Text(detail, 11, Kit.Soft);
            sub.style.marginTop = 3;
            info.Add(sub);
            item.Add(info);

            item.RegisterCallback<PointerEnterEvent>(_ => { if (_controller.Armed != def) item.style.backgroundColor = Kit.Sand; });
            item.RegisterCallback<PointerLeaveEvent>(_ => { if (_controller.Armed != def) item.style.backgroundColor = Color.white; });
            item.RegisterCallback<ClickEvent>(_ => { Kit.Press(); _controller.Arm(def); });
            return item;
        }

        private void PaintRow(PaletteRow row)
        {
            bool armed = _controller.Armed == row.def;
            row.element.style.backgroundColor = armed ? Kit.A(Kit.Teal, 0.16f) : Color.white;
            Kit.BorderColor(row.element, armed ? Kit.Teal : Kit.Track);
        }

        private void FilterPalette()
        {
            foreach (var row in _rows)
            {
                bool categoryOk = _category == 0 || (int)row.def.Category == _category - 1;
                bool searchOk = _search.Length == 0
                                || row.def.EditorName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                                || row.def.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                row.element.style.display = categoryOk && searchOk ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private VisualElement SeatList()
        {
            var column = Kit.Col();
            var layout = Session.Layout;
            if (layout.Seats.Count == 0)
            {
                column.Add(Kit.Wrap("Sem assentos: nenhum boneco aparece no cenário. O primeiro assento é o do fundador.", 13, Kit.Orange, true));
                return column;
            }
            column.Add(Kit.Caps("Quem senta onde", 11, Kit.Faint));
            for (int i = 0; i < layout.Seats.Count; i++)
            {
                int seat = i;
                bool onChair = OfficeBuilder.TryFindChairAt(layout, layout.Seats[i].cell, out var chair);
                var row = Kit.Row();
                row.style.marginTop = 6;
                row.Add(Kit.Badge(i == 0 ? Kit.IcCrown : Kit.IcUser, i == 0 ? Kit.Gold : Kit.Sky, 26));
                row.Add(Kit.Gap(8));
                var text = Kit.Col();
                text.style.flexGrow = 1; text.style.flexShrink = 1; text.style.overflow = Overflow.Hidden;
                text.Add(Kit.Text(i == 0 ? "Fundador" : "Funcionário " + i, 12, Kit.Ink, true));
                text.Add(Kit.Text(onChair ? chair.furniture.EditorName : "sem cadeira", 11, onChair ? Kit.Soft : Kit.Red));
                var activity = Kit.Chip(ActivityNames[(int)layout.Seats[i].activity % ActivityNames.Length], Kit.Purple, false, Kit.IcPlay);
                activity.style.alignSelf = Align.FlexStart; activity.style.marginTop = 3;
                Kit.Hover(activity, 1.06f, 0.95f);
                activity.RegisterCallback<ClickEvent>(_ => _controller.CycleSeatActivity(seat));
                text.Add(activity);
                row.Add(text);
                row.Add(Kit.RoundButton(Kit.IcArrowUp, Kit.Sand, Kit.Ink, 24, () => _controller.ShiftSeat(seat, -1)));
                row.Add(Kit.Gap(3));
                row.Add(Kit.RoundButton(Kit.IcArrowDown, Kit.Sand, Kit.Ink, 24, () => _controller.ShiftSeat(seat, 1)));
                row.Add(Kit.Gap(3));
                row.Add(Kit.RoundButton(Kit.IcUndo, Kit.Sand, Kit.Ink, 24, () => _controller.TurnSeat(seat)));
                row.Add(Kit.Gap(3));
                row.Add(Kit.RoundButton(Kit.IcTrash, Kit.A(Kit.Red, 0.15f), Kit.Red, 24, () => _controller.RemoveSeat(seat)));
                column.Add(row);
            }
            var tip = Kit.Wrap("Setas mudam a ordem · a seta curva gira o boneco 90° · clique na etiqueta roxa para trocar o que ele fica fazendo.", 11, Kit.Faint);
            tip.style.marginTop = 8;
            column.Add(tip);
            return column;
        }

        // ------------------------------------------------------------------ painel direito

        private void RefreshRight()
        {
            _rightPanel.Clear();
            _rightPanel.Add(SelectionCard());
            _rightPanel.Add(Kit.Gap(0, 10));
            _rightPanel.Add(ViewCard());
        }

        private VisualElement SelectionCard()
        {
            var card = Kit.Card(0, 16);
            var layout = Session.Layout;

            if (_controller.Armed != null)
            {
                var def = _controller.Armed;
                card.Add(Kit.Caps("Colocando", 11, Kit.Faint));
                card.Add(Title(def.EditorName));
                card.Add(Kit.Text("Girada " + _controller.ArmedRotation * 90 + "° · ocupa " + def.FootprintInCells.x + "×" + def.FootprintInCells.y, 12, Kit.Soft));
                if (def.PlaceableOnTop) card.Add(Kit.Wrap("Passe por cima de uma mesa ou armário para apoiar a peça nele.", 11, Kit.Teal, true));
                if (!def.IsWallPiece) card.Add(AlignPicker(_controller.ArmedAlign));
                card.Add(Kit.Gap(0, 10));
                var row = Kit.Row();
                if (def.Rotatable) row.Add(Grow(Kit.Button("Girar (R)", Kit.IcUndo, Kit.Sand, Kit.Ink, () => _controller.RotateCurrent(1), 36)));
                row.Add(Kit.Gap(8));
                row.Add(Grow(Kit.Button("Soltar", Kit.IcClose, Kit.Sand, Kit.Ink, _controller.Disarm, 36)));
                card.Add(row);
                return card;
            }

            if (Session.Valid(_controller.Selected))
            {
                var p = layout.EditorPlacements[_controller.Selected];
                card.Add(Kit.Caps("Selecionada", 11, Kit.Faint));
                card.Add(Title(p.furniture.EditorName));
                card.Add(Kit.Text("Célula " + p.cell.x + ", " + p.cell.y + " · " + p.rotationSteps * 90 + "°" + (p.onTop ? " · em cima de " + SupportName(p) : ""), 12, Kit.Soft));
                if (!p.furniture.IsWallPiece) card.Add(AlignPicker(p.align));
                card.Add(Kit.Gap(0, 10));

                if (p.furniture.Rotatable)
                {
                    var turn = Kit.Row();
                    turn.Add(Grow(Kit.Button("Girar (R)", Kit.IcUndo, Kit.Blue, Color.white, () => _controller.RotateCurrent(1), 36)));
                    turn.Add(Kit.Gap(8));
                    turn.Add(Grow(Kit.Button("Voltar 90°", null, Kit.Sand, Kit.Ink, () => _controller.RotateCurrent(-1), 36)));
                    card.Add(turn);
                    card.Add(Kit.Gap(0, 8));
                }

                var actions = Kit.Row();
                actions.Add(Grow(Kit.Button("Duplicar", Kit.IcLayers, Kit.Sand, Kit.Ink, _controller.DuplicateSelected, 36)));
                actions.Add(Kit.Gap(8));
                actions.Add(Grow(Kit.Button("Remover", Kit.IcTrash, Kit.Red, Color.white, _controller.RemoveSelected, 36)));
                card.Add(actions);

                if (Session.IsChair(_controller.Selected))
                {
                    int seat = Session.SeatOfChair(_controller.Selected);
                    card.Add(Kit.Gap(0, 8));
                    card.Add(Kit.Button(seat < 0 ? "Tornar assento" : "Tirar assento (" + (seat == 0 ? "fundador" : "funcionário " + seat) + ")",
                        Kit.IcUser, seat < 0 ? Kit.Teal : Kit.Sand, seat < 0 ? Color.white : Kit.Ink, _controller.ToggleSeatOnSelected, 36));
                }
                return card;
            }

            card.Add(Kit.Caps("Cenário", 11, Kit.Faint));
            card.Add(Title(Labels.Phase(layout.Phase) + " · " + layout.Cols + "×" + layout.Rows));
            card.Add(Kit.Text(layout.Placements.Count + " peças · " + layout.Seats.Count + " assentos · " + layout.CeilingLights.Count + " luzes", 12, Kit.Soft));
            var tip = Kit.Wrap("Escolha um móvel na lista e clique no chão para colocar. Clique numa peça para selecionar.", 12, Kit.Faint);
            tip.style.marginTop = 8;
            card.Add(tip);
            return card;
        }

        private string SupportName(FurniturePlacement rider)
        {
            var placements = Session.Layout.Placements;
            var footprint = OfficeGrid.RotatedFootprint(rider.furniture.FootprintInCells, rider.rotationSteps);
            int support = OfficeLayoutRules.SupportAt(placements, rider.cell, footprint);
            return support >= 0 ? placements[support].furniture.EditorName : "outro móvel";
        }

        /// <summary>
        /// Seletor 3×3 da posição da peça dentro da célula, desenhado em losango para bater com
        /// a vista do jogo: a ponta de cima é o canto onde as duas paredes se encontram.
        /// </summary>
        private VisualElement AlignPicker(Vector2Int current)
        {
            var wrap = Kit.Row();
            wrap.style.marginTop = 10;

            var holder = new VisualElement();
            holder.style.width = 112; holder.style.height = 112; holder.style.flexShrink = 0;
            holder.style.alignItems = Align.Center; holder.style.justifyContent = Justify.Center;
            var grid = new VisualElement();
            grid.style.width = 78; grid.style.height = 78;
            grid.style.rotate = new Rotate(new Angle(45f));
            for (int row = 0; row < 3; row++)
            {
                var line = Kit.Row();
                for (int col = 0; col < 3; col++)
                {
                    var align = new Vector2Int(row - 1, col - 1);
                    bool on = align == current;
                    var cell = new VisualElement();
                    cell.style.width = 22; cell.style.height = 22;
                    cell.style.marginRight = 4; cell.style.marginBottom = 4;
                    Kit.Radius(cell, 6);
                    cell.style.backgroundColor = on ? Kit.Teal : Kit.Sand;
                    Kit.Border(cell, on ? Kit.Teal : Kit.Track, 2);
                    Kit.Hover(cell, 1.15f, 0.9f);
                    cell.RegisterCallback<ClickEvent>(_ => _controller.SetAlign(align));
                    line.Add(cell);
                }
                grid.Add(line);
            }
            holder.Add(grid);
            wrap.Add(holder);
            wrap.Add(Kit.Gap(10));

            var text = Kit.Col();
            text.style.flexShrink = 1;
            text.Add(Kit.Caps("Posição na célula", 11, Kit.Faint));
            string where = current == Vector2Int.zero ? "Centro" : (current.x != 0 && current.y != 0 ? "Canto" : "Encostada na borda");
            text.Add(Kit.Text(where, 14, Kit.Ink, true));
            var tip = Kit.Wrap("A ponta de cima é o canto das duas paredes (na vista do jogo).", 11, Kit.Faint);
            tip.style.marginTop = 4;
            text.Add(tip);
            wrap.Add(text);
            return wrap;
        }

        private VisualElement ViewCard()
        {
            var card = Kit.Card(0, 16);
            card.Add(Kit.Caps("Vista", 11, Kit.Faint));
            card.Add(Kit.Gap(0, 8));

            var turn = Kit.Row();
            turn.Add(Kit.Button("Q", Kit.IcChevLeft, Kit.Sand, Kit.Ink, () => _controller.View.Turn(-1), 34));
            turn.Add(Kit.Gap(6));
            turn.Add(Grow(Kit.Button("Vista do jogo", Kit.IcGamepad, Kit.Sand, Kit.Ink, _controller.View.GameView, 34)));
            turn.Add(Kit.Gap(6));
            turn.Add(Kit.Button("E", Kit.IcChevRight, Kit.Sand, Kit.Ink, () => _controller.View.Turn(1), 34));
            card.Add(turn);
            card.Add(Kit.Gap(0, 10));

            card.Add(ToggleRow("Guias (grade, luzes, etiquetas) — G", _controller.Stage.Guides, _controller.SetGuides));
            card.Add(Kit.Gap(0, 8));
            card.Add(ToggleRow("Bonecos sentados", _controller.Stage.People, _controller.SetPeople));
            card.Add(Kit.Gap(0, 10));

            var history = Kit.Row();
            history.Add(Grow(Kit.Button("Desfazer", Kit.IcUndo, Kit.Sand, Session.CanUndo ? Kit.Ink : Kit.Faint, _controller.Undo, 34)));
            history.Add(Kit.Gap(6));
            history.Add(Grow(Kit.Button("Refazer", Kit.IcHistory, Kit.Sand, Session.CanRedo ? Kit.Ink : Kit.Faint, _controller.Redo, 34)));
            card.Add(history);
            return card;
        }

        private static VisualElement ToggleRow(string label, bool value, Action<bool> onChange)
        {
            var row = Kit.Row();
            var text = Kit.Text(label, 12, Kit.Ink, true);
            text.style.flexGrow = 1; text.style.flexShrink = 1; text.style.whiteSpace = WhiteSpace.Normal;
            row.Add(text);
            row.Add(Kit.Gap(8));
            row.Add(Widgets.Toggle(value, onChange));
            return row;
        }

        private static Label Title(string text)
        {
            var label = Kit.Text(text, 18, Kit.Ink, true);
            label.style.marginTop = 2; label.style.marginBottom = 2;
            return label;
        }

        private static VisualElement Grow(VisualElement element)
        {
            element.style.flexGrow = 1; element.style.flexBasis = 0;
            element.style.paddingLeft = 8; element.style.paddingRight = 8;
            return element;
        }

        // ------------------------------------------------------------------ janelas

        private VisualElement OpenModal(float width, bool dim, bool centered = true)
        {
            _modalLayer.Clear();
            _modalLayer.pickingMode = PickingMode.Position;
            _modalLayer.style.backgroundColor = dim ? new Color(0.10f, 0.09f, 0.07f, 0.55f) : new Color(0, 0, 0, 0);
            _modalLayer.style.alignItems = centered ? Align.Center : Align.FlexStart;
            _modalLayer.style.justifyContent = centered ? Justify.Center : Justify.FlexStart;
            var card = Kit.Card(width, 18);
            Kit.Pad(card, 20, 22);
            if (!centered) { card.style.marginLeft = 620; card.style.marginTop = 84; }
            _modalLayer.Add(card);
            Sfx.Play(SfxId.Open);
            return card;
        }

        private void CloseModal()
        {
            _modalLayer.Clear();
            _modalLayer.pickingMode = PickingMode.Ignore;
            _modalLayer.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        /// <summary>Se há alterações não salvas, pergunta antes de seguir.</summary>
        private void ConfirmDiscard(Action proceed)
        {
            if (!Session.Dirty) { proceed(); return; }
            var card = OpenModal(460, true);
            card.Add(Title("Alterações não salvas"));
            card.Add(Kit.Wrap("O cenário \"" + Session.Name + "\" tem mudanças que ainda não foram salvas.", 13, Kit.Soft));
            card.Add(Kit.Gap(0, 16));
            var row = Kit.Row();
            row.Add(Grow(Kit.Button("Cancelar", null, Kit.Sand, Kit.Ink, CloseModal, 40)));
            row.Add(Kit.Gap(8));
            row.Add(Grow(Kit.Button("Descartar", null, Kit.Red, Color.white, () => { CloseModal(); proceed(); }, 40)));
            row.Add(Kit.Gap(8));
            row.Add(Grow(Kit.Button("Salvar", Kit.IcSave, Kit.Teal, Color.white, () =>
            {
                CloseModal();
                if (_controller.Save()) proceed();
            }, 40)));
            card.Add(row);
        }

        /// <summary>Sair para o menu (confirma se houver alterações).</summary>
        public void AskExit()
        {
            if (ModalOpen) { CloseModal(); return; }
            ConfirmDiscard(_controller.Exit);
        }

        private void ShowNew()
        {
            var card = OpenModal(520, true);
            card.Add(Title("Novo cenário"));
            card.Add(Kit.Wrap("Para qual fase da empresa? A sala começa vazia no tamanho padrão da fase — dá para mudar depois.", 13, Kit.Soft));
            card.Add(Kit.Gap(0, 14));
            var row = Kit.Row();
            for (int i = 0; i < PhaseNames.Length; i++)
            {
                var phase = (CompanyPhase)i;
                if (i > 0) row.Add(Kit.Gap(8));
                row.Add(Grow(Kit.Button(PhaseNames[i], Labels.PhaseIcon(phase), Kit.PhaseColors[i], Color.white, () =>
                {
                    CloseModal();
                    var map = MapBuilderSession.Blank(phase, UniqueName(OfficeMapStore.SanitizeName(RemoveAccents(PhaseNames[(int)phase]))));
                    map.usedInGame = OfficeMapStore.FindForPhase(phase) == null;
                    _controller.OpenMap(map);
                    _controller.Session.Touch();
                    Refresh();
                }, 44)));
            }
            card.Add(row);
            card.Add(Kit.Gap(0, 14));
            card.Add(Kit.Button("Cancelar", null, Kit.Sand, Kit.Ink, CloseModal, 38));
        }

        private static string RemoveAccents(string text) =>
            text.Replace('ó', 'o').Replace('ú', 'u').Replace('é', 'e').Replace('á', 'a').Replace('í', 'i');

        private static string UniqueName(string baseName)
        {
            if (!OfficeMapStore.Exists(baseName)) return baseName;
            for (int i = 2; i < 100; i++)
                if (!OfficeMapStore.Exists(baseName + i)) return baseName + i;
            return baseName + DateTime.Now.Ticks;
        }

        private void ShowOpen()
        {
            var card = OpenModal(640, true);
            card.Add(Title("Abrir cenário"));
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.maxHeight = 560; scroll.style.marginTop = 8;
            card.Add(scroll);

            var saved = OfficeMapStore.ListAll();
            scroll.Add(Kit.Caps("Salvos pelo Map Builder", 11, Kit.Faint));
            if (saved.Count == 0)
                scroll.Add(Kit.Wrap("Nenhum cenário salvo ainda. Monte um e clique em Salvar.", 13, Kit.Soft));
            foreach (var map in saved)
            {
                var captured = map;
                scroll.Add(MapRow(map.name, map.phase, map.usedInGame, map.pieces.Count, false,
                    () => { CloseModal(); _controller.OpenMap(captured); },
                    () => ConfirmDelete(captured.name)));
            }

            var assets = Resources.LoadAll<OfficeLayoutSO>("Office");
            if (assets.Length > 0)
            {
                scroll.Add(Kit.Gap(0, 14));
                scroll.Add(Kit.Caps("Cenários antigos do projeto (assets)", 11, Kit.Faint));
                foreach (var asset in assets)
                {
                    var captured = asset;
                    bool overridden = OfficeMapStore.FindForPhase(asset.Phase) != null;
                    scroll.Add(MapRow(asset.name, asset.Phase, !overridden, asset.Placements.Count, true,
                        () =>
                        {
                            CloseModal();
                            _controller.OpenMap(captured.ToMapData(captured.name, true));
                            Toast("Aberto a partir do asset. Ao salvar, vira um cenário do Map Builder e passa a valer no jogo.");
                        }, null));
                }
            }

            card.Add(Kit.Gap(0, 14));
            card.Add(Kit.Button("Fechar", null, Kit.Sand, Kit.Ink, CloseModal, 38));
        }

        private static VisualElement MapRow(string name, CompanyPhase phase, bool inUse, int pieces, bool isAsset, Action open, Action delete)
        {
            var row = Kit.Row();
            row.style.marginTop = 8; row.style.backgroundColor = Color.white;
            Kit.Radius(row, 12); Kit.Border(row, Kit.Track, 2); Kit.Pad(row, 8, 12);
            row.Add(Kit.SquareBadge(Labels.PhaseIcon(phase), Kit.PhaseColors[(int)phase], 36, 10));
            row.Add(Kit.Gap(10));
            var info = Kit.Col();
            info.style.flexGrow = 1; info.style.flexShrink = 1;
            info.Add(Kit.Text(name, 15, Kit.Ink, true));
            var chips = Kit.Row();
            chips.style.marginTop = 4;
            chips.Add(Kit.Chip(Labels.Phase(phase), Kit.PhaseColors[(int)phase]));
            chips.Add(Kit.Gap(5));
            chips.Add(Kit.Chip(pieces + " peças", Kit.Soft));
            if (inUse) { chips.Add(Kit.Gap(5)); chips.Add(Kit.Chip("em uso no jogo", Kit.Teal, true, Kit.IcCheck)); }
            if (isAsset) { chips.Add(Kit.Gap(5)); chips.Add(Kit.Chip("asset", Kit.Orange)); }
            info.Add(chips);
            row.Add(info);
            if (delete != null)
            {
                row.Add(Kit.RoundButton(Kit.IcTrash, Kit.A(Kit.Red, 0.15f), Kit.Red, 32, delete));
                row.Add(Kit.Gap(8));
            }
            row.Add(Kit.Button("Abrir", null, Kit.Blue, Color.white, open, 36));
            return row;
        }

        private void ConfirmDelete(string mapName)
        {
            var card = OpenModal(440, true);
            card.Add(Title("Apagar \"" + mapName + "\"?"));
            card.Add(Kit.Wrap("O arquivo do cenário é apagado do disco. Não dá para desfazer.", 13, Kit.Soft));
            card.Add(Kit.Gap(0, 16));
            var row = Kit.Row();
            row.Add(Grow(Kit.Button("Cancelar", null, Kit.Sand, Kit.Ink, ShowOpen, 40)));
            row.Add(Kit.Gap(8));
            row.Add(Grow(Kit.Button("Apagar", Kit.IcTrash, Kit.Red, Color.white, () =>
            {
                OfficeMapStore.Delete(mapName);
                ShowOpen();
            }, 40)));
            card.Add(row);
        }

        private enum Surface { Floor, BackWall, SideWall }

        /// <summary>Janela de cor + acabamento do chão, da parede do fundo ou da parede lateral.</summary>
        private void ShowSurface(Surface surface)
        {
            var layout = Session.Layout;
            bool floor = surface == Surface.Floor;
            Color current = floor ? layout.FloorColor : surface == Surface.BackWall ? layout.WallColor : layout.SideWallColor;
            SurfaceStyle currentStyle = floor ? layout.FloorStyle : surface == Surface.BackWall ? layout.WallStyle : layout.SideWallStyle;
            bool recorded = false;

            void Apply(Color color, SurfaceStyle style)
            {
                current = color;
                currentStyle = style;
                var now = Session.Layout;
                switch (surface)
                {
                    case Surface.Floor:
                        _controller.SetColors(color, now.WallColor, !recorded);
                        _controller.SetStyles(style, now.WallStyle);
                        break;
                    case Surface.BackWall:
                        _controller.SetColors(now.FloorColor, color, !recorded);
                        _controller.SetStyles(now.FloorStyle, style);
                        break;
                    default:
                        _controller.SetSideWall(true, color, style, !recorded);
                        break;
                }
                recorded = true;
            }

            var card = OpenModal(360, false, false);
            card.Add(Title(floor ? "Chão" : surface == Surface.BackWall ? "Parede do fundo" : "Parede lateral"));

            if (surface == Surface.SideWall)
            {
                var same = Kit.Row();
                same.style.marginTop = 4;
                same.Add(Widgets.Toggle(!layout.SideWallCustom, on =>
                {
                    var now = Session.Layout;
                    _controller.SetSideWall(!on, now.SideWallColor, now.SideWallStyle, true);
                    ShowSurface(surface);
                }));
                same.Add(Kit.Gap(8));
                same.Add(Kit.Text("Igual à parede do fundo", 13, Kit.Ink, true));
                card.Add(same);
            }

            var grid = Kit.Row();
            grid.style.flexWrap = Wrap.Wrap; grid.style.marginTop = 8;
            foreach (var swatch in floor ? FloorSwatches : WallSwatches)
            {
                var color = swatch;
                var chip = new VisualElement();
                chip.style.width = 44; chip.style.height = 44; chip.style.marginRight = 8; chip.style.marginBottom = 8;
                chip.style.backgroundColor = color;
                Kit.Radius(chip, 11); Kit.Border(chip, Kit.Track, 2); Kit.Hover(chip, 1.08f, 0.94f);
                chip.RegisterCallback<ClickEvent>(_ => { Apply(color, currentStyle); ShowSurface(surface); });
                grid.Add(chip);
            }
            card.Add(grid);

            card.Add(Kit.Caps("Acabamento", 11, Kit.Faint));
            var styles = Kit.Row();
            styles.style.flexWrap = Wrap.Wrap; styles.style.marginTop = 6; styles.style.marginBottom = 8;
            foreach (var (style, name) in floor ? FloorStyles : WallStyles)
            {
                var captured = style;
                bool on = style == currentStyle;
                var chip = Kit.Row();
                Kit.Pad(chip, 5, 11); Kit.Radius(chip, 10); Kit.Hover(chip, 1.06f, 0.95f);
                chip.style.marginRight = 6; chip.style.marginBottom = 6;
                chip.style.backgroundColor = on ? Kit.Teal : Kit.Sand;
                chip.Add(Kit.Text(name, 12, on ? Color.white : Kit.Ink, true));
                chip.RegisterCallback<ClickEvent>(_ => { Apply(current, captured); ShowSurface(surface); });
                styles.Add(chip);
            }
            card.Add(styles);

            card.Add(Kit.Caps("Ajuste fino da cor", 11, Kit.Faint));
            card.Add(ChannelSlider("R", current.r, Kit.Red, v => Apply(new Color(v, current.g, current.b), currentStyle)));
            card.Add(ChannelSlider("G", current.g, Kit.Green, v => Apply(new Color(current.r, v, current.b), currentStyle)));
            card.Add(ChannelSlider("B", current.b, Kit.Blue, v => Apply(new Color(current.r, current.g, v), currentStyle)));
            card.Add(Kit.Gap(0, 12));
            card.Add(Kit.Button("Pronto", Kit.IcCheck, Kit.Teal, Color.white, () => { CloseModal(); Refresh(); }, 38));
        }

        private static VisualElement ChannelSlider(string label, float value, Color color, Action<float> onChange)
        {
            var row = Kit.Row();
            row.style.marginTop = 6;
            var text = Kit.Text(label, 13, Kit.Soft, true);
            text.style.width = 18;
            row.Add(text);
            row.Add(Widgets.Slider(value, color, onChange));
            return row;
        }
    }
}
