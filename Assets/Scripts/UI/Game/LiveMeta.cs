using System.Collections.Generic;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Meta;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>Resultado da run vivo (GDD 20.4 / 19.5): venda ou falência, dinheiro pessoal, PL e estatísticas.</summary>
    internal sealed class RunResultLive : GalleryScreen
    {
        private sealed class Coin { public float x, y, vy, size, phase; }

        private SunburstBackdrop _bg;
        private FounderArt _art;
        private VisualElement _coinLayer;
        private readonly List<Coin> _coins = new List<Coin>();
        private bool _sold;

        protected override void Build()
        {
            var gc = Ui.Gc;
            var run = gc.Run;
            var meta = gc.Meta;
            var c = run.company;
            _sold = run.salePrice > 0;

            _bg = _sold ? SunburstBackdrop.Gold() : SunburstBackdrop.Rose();
            _bg.pickingMode = PickingMode.Position;
            Root.Add(_bg);
            _coinLayer = Kit.Fill(); _coinLayer.pickingMode = PickingMode.Ignore;
            _coinLayer.generateVisualContent += DrawCoins;
            Root.Add(_coinLayer);
            var center = Kit.Centered();
            Root.Add(center);

            var halo = new VisualElement(); halo.style.width = 150; halo.style.height = 150; Kit.Radius(halo, 75);
            halo.style.backgroundColor = Kit.A(Color.white, 0.6f); halo.style.alignItems = Align.Center; halo.style.justifyContent = Justify.FlexEnd;
            halo.style.overflow = Overflow.Hidden;
            _art = new FounderArt(140) { Skin = meta.founder.skin, Hair = meta.founder.hair, HairStyle = meta.founder.hairStyle, Shirt = meta.founder.shirt, Accessory = meta.founder.accessory };
            halo.Add(_art);
            center.Add(Pop(halo, 0.1f));

            var over = Kit.Caps("Run " + run.runNumber + " encerrada · " + run.year + (run.year == 1 ? " ano" : " anos") + " de empresa", 13, Kit.Soft, 3f);
            over.style.marginTop = 14;
            center.Add(Enter(over, 0.3f));
            var head = Kit.Text(_sold ? "EMPRESA VENDIDA!" : "FALÊNCIA", 64, _sold ? Kit.Ink : Kit.Red, true);
            head.style.letterSpacing = 2;
            center.Add(Pop(head, 0.45f));
            center.Add(Enter(Kit.Text(_sold ? "A " + c.name + " agora tem novos donos" : "O caixa zerou e a " + c.name + " fechou as portas", 16, Kit.Soft), 0.6f));

            var money = Kit.Text("R$ 0", 54, _sold ? Kit.Cash : Kit.Soft, true); money.style.marginTop = 8;
            center.Add(Enter(money, 0.7f));
            if (_sold) CountUp(money, 0, run.salePrice, 0.9f, 1.6f, Kit.Money);
            center.Add(Enter(Kit.Text(_sold ? "direto para o seu bolso" : "nada para levar desta vez", 13, Kit.Soft, true), 0.8f));

            var row = Kit.Row(Align.Stretch); row.style.marginTop = 24;
            center.Add(row);

            var pl = Kit.Card(430, 18); Kit.Pad(pl, 18, 20); pl.style.marginRight = 16;
            var ph = Kit.Row();
            ph.Add(Kit.Badge(Kit.IcHourglass, Kit.Gold, 34));
            var pt = Kit.Text("Pontos de Legado", 16, Kit.Ink, true); pt.style.marginLeft = 10;
            ph.Add(pt); ph.Add(Kit.Spacer());
            var total = Kit.Text("0", 30, Kit.Gold, true);
            ph.Add(total);
            pl.Add(ph);
            pl.Add(Kit.Divider(10));
            var lines = LegacyCalculator.Breakdown(c, run.releasedProjects, !_sold, run.hadMajorCrisis);
            int sum = 0; float at = 1.6f;
            foreach (var l in lines)
            {
                sum += l.Points;
                var r = Kit.Row(); r.style.marginBottom = 7;
                r.Add(Kit.Icon(Kit.IcCheck, 11, Kit.Green));
                var lt = Kit.Text(l.Label, 13, Kit.Ink); lt.style.marginLeft = 8;
                r.Add(lt); r.Add(Kit.Spacer());
                r.Add(Kit.Text("+" + l.Points, 14, Kit.Gold, true));
                pl.Add(r);
                SlideIn(r, -24f, 0f, at, 0.35f);
                at += 0.22f;
            }
            if (lines.Count == 0) pl.Add(Kit.Text("Nenhum marco atingido desta vez.", 12, Kit.Soft));
            CountUp(total, 0, sum, 1.6f, Mathf.Max(0.3f, at - 1.4f), v => Mathf.RoundToInt(v).ToString());
            row.Add(Enter(pl, 1.0f));

            float best = 0f; int franchises = 0;
            var seen = new HashSet<string>();
            foreach (var g in run.releasedProjects)
            {
                if (g.finalScore > best) best = g.finalScore;
                if (!string.IsNullOrEmpty(g.franchiseId) && seen.Add(g.franchiseId)) franchises++;
            }
            var st = Kit.Card(430, 18); Kit.Pad(st, 18, 20);
            st.Add(Kit.Text("A run em números", 16, Kit.Ink, true));
            st.Add(Kit.Divider(10));
            var grid = Kit.Row(); grid.style.flexWrap = Wrap.Wrap;
            grid.Add(Stat(Kit.IcGamepad, "Jogos lançados", c.gamesReleased.ToString()));
            grid.Add(Stat(Kit.IcStar, "Melhor nota", c.gamesReleased > 0 ? Labels.Score(best) : "—"));
            grid.Add(Stat(Kit.IcChartLine, "Valuation máximo", "R$ " + Kit.Short(c.peakValuation)));
            grid.Add(Stat(Kit.IcBuilding, "Fase alcançada", Labels.Phase(c.phase)));
            grid.Add(Stat(Kit.IcCrown, "Franquias", franchises.ToString()));
            grid.Add(Stat(Kit.IcUsers, "Equipe final", c.EffectiveTeamSize.ToString()));
            st.Add(grid);
            row.Add(Enter(st, 1.1f));

            var note = Kit.Chip("A " + c.name + " vira rival na sua próxima timeline", Kit.Soft, false, Kit.IcHourglass);
            note.style.marginTop = 16;
            center.Add(Enter(note, 2.4f));
            var go = Kit.Button("Gastar em habilidades", Kit.IcArrowRight, Kit.Teal, Color.white, Ui.OpenSkillTree, 52); go.style.marginTop = 16;
            center.Add(Enter(go, 2.8f));
        }

        private static VisualElement Stat(string icon, string cap, string val)
        {
            var c = Kit.Row(); c.style.width = Length.Percent(50); c.style.marginBottom = 12;
            c.Add(Kit.Badge(icon, Kit.Sand, 32, Kit.Soft));
            var t = Kit.Col(); t.style.marginLeft = 10;
            t.Add(Kit.Text(val, 16, Kit.Ink, true));
            t.Add(Kit.Text(cap, 10, Kit.Soft, true));
            c.Add(t);
            return c;
        }

        protected override void OnTick(float dt)
        {
            _bg.Tick(dt);
            _art.Tick(dt);
            if (!_sold) return;
            var rect = Root.contentRect;
            if (Clock > 0.9f && Clock < 3.2f && _coins.Count < 70 && Random.value < 0.6f)
                _coins.Add(new Coin { x = Random.Range(0.1f, 0.9f) * rect.width, y = -20f, vy = Random.Range(260f, 520f), size = Random.Range(7f, 13f), phase = Random.Range(0f, 6f) });
            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                var c = _coins[i]; c.y += c.vy * dt; c.vy += 300f * dt;
                if (c.y > rect.height + 30f) _coins.RemoveAt(i);
            }
            _coinLayer.MarkDirtyRepaint();
        }

        private void DrawCoins(MeshGenerationContext mgc)
        {
            if (_coins.Count == 0) return;
            var p = mgc.painter2D;
            foreach (var c in _coins)
            {
                float squash = Mathf.Max(0.15f, Mathf.Abs(Mathf.Cos(Clock * 6f + c.phase)));
                p.fillColor = Kit.A(Kit.Gold, 0.9f);
                p.BeginPath();
                for (int k = 0; k <= 12; k++)
                {
                    float a = k / 12f * Mathf.PI * 2f;
                    var pt = new Vector2(c.x + Mathf.Cos(a) * c.size * squash, c.y + Mathf.Sin(a) * c.size);
                    if (k == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.ClosePath(); p.Fill();
            }
        }
    }

    /// <summary>
    /// Árvore de habilidades viva (GDD 14 / 19.5): compra de níveis ($) e perks (PL), reset de PL,
    /// escolha do capital que vai para a próxima empresa e o botão Nova Run.
    /// </summary>
    internal sealed class SkillTreeLive : GalleryScreen
    {
        private sealed class Node
        {
            public SkillTreeDef tree;
            public PerkDef perk; // nulo = nó raiz
            public Color color;
            public string icon;
            public VisualElement disc;
            public Label iconLabel, costLabel;
            public Vector2 pos;
            public bool IsRoot => perk == null;
        }

        private const float CellW = 452f, CellH = 318f, RootSize = 74f, ChildSize = 44f;

        private readonly List<Node> _nodes = new List<Node>();
        private readonly List<(VisualElement layer, SkillTreeDef tree)> _links = new List<(VisualElement, SkillTreeDef)>();
        private Node _selected;
        private string _selectedKey;
        private float _capital01 = 1f;
        private string _companyName;
        private Label _moneyLabel, _legacyLabel, _dName, _dType, _dDesc, _dCost, _dIcon, _dBuyLabel, _capitalLabel;
        private VisualElement _dBadge, _dBuy, _runIcon, _bgDots;
        private TextField _nameField;

        private State.Meta.MetaProgressionState Meta => Ui.Gc.Meta;

        protected override void Build()
        {
            _nodes.Clear(); _links.Clear(); _selected = null;
            Root.style.backgroundColor = Kit.RGB(238, 231, 214);
            Root.pickingMode = PickingMode.Position;
            _bgDots = Kit.Fill(); _bgDots.pickingMode = PickingMode.Ignore; _bgDots.generateVisualContent += DrawDots;
            Root.Add(_bgDots);
            if (_companyName == null) _companyName = Ui.Gc.Run.company.name + " II";

            var page = Kit.Fill(); Kit.Pad(page, 22, 28);
            Root.Add(page);

            var top = Kit.Row(); top.style.marginBottom = 14;
            var tt = Kit.Col();
            tt.Add(Kit.Text("Habilidades do fundador", 30, Kit.Ink, true));
            tt.Add(Kit.Text("O que você aprende aqui volta no tempo com você — para todas as próximas empresas.", 13, Kit.Soft));
            top.Add(tt);
            top.Add(Kit.Spacer());
            top.Add(Wallet(Kit.IcCoins, Kit.Cash, "Dinheiro pessoal", out _moneyLabel));
            top.Add(Kit.Gap(12));
            top.Add(Wallet(Kit.IcHourglass, Kit.Gold, "Pontos de Legado", out _legacyLabel));
            page.Add(Enter(top, 0.05f));

            var mid = Kit.Row(Align.Stretch); mid.style.flexGrow = 1;
            page.Add(mid);
            var grid = Kit.Row(Align.FlexStart); grid.style.flexWrap = Wrap.Wrap; grid.style.flexGrow = 1; grid.style.alignContent = Align.FlexStart;
            mid.Add(grid);
            var trees = SkillTreeCatalog.All;
            for (int i = 0; i < trees.Count; i++) grid.Add(Enter(BuildTree(trees[i]), 0.15f + i * 0.08f));
            mid.Add(Enter(BuildDetail(), 0.5f));

            var bottom = Kit.Card(0, 18); bottom.style.flexDirection = FlexDirection.Row; bottom.style.alignItems = Align.Center;
            bottom.style.marginTop = 10; Kit.Pad(bottom, 14, 20);
            var reset = Kit.Ghost("Resetar Legado · " + Kit.Money(SkillTreeCatalog.ResetCost()), Kit.IcUndo, () =>
            {
                if (SkillTreeCatalog.TryResetPerks(Meta)) { Sfx.Play(SfxId.Spend); Rebuild(); } else Sfx.Play(SfxId.Fail);
            });
            LiveBits.SetEnabled(reset, Meta.perks.Count > 0 && Meta.personalMoney >= SkillTreeCatalog.ResetCost());
            bottom.Add(reset);
            bottom.Add(Kit.Gap(22));
            var nameCol = Kit.Col();
            nameCol.Add(Kit.Text("Nome da próxima empresa", 11, Kit.Soft, true));
            _nameField = Widgets.Input(_companyName, 260);
            _nameField.RegisterValueChangedCallback(e => _companyName = e.newValue);
            nameCol.Add(_nameField);
            bottom.Add(nameCol);
            bottom.Add(Kit.Gap(22));
            var cap = Kit.Col(); cap.style.flexGrow = 1;
            var cr = Kit.Row();
            cr.Add(Kit.Text("Capital para a próxima run (o resto fica guardado)", 12, Kit.Soft, true)); cr.Add(Kit.Spacer());
            _capitalLabel = Kit.Text("", 16, Kit.Cash, true);
            cr.Add(_capitalLabel);
            cap.Add(cr);
            cap.Add(Widgets.Slider(_capital01, Kit.Cash, v => { _capital01 = v; RefreshWallets(); }));
            bottom.Add(cap);
            bottom.Add(Kit.Gap(22));
            var run = Kit.Button("Nova Run", Kit.IcHourglass, Kit.Teal, Color.white, StartRun, 56);
            _runIcon = run[0];
            bottom.Add(run);
            page.Add(Enter(bottom, 0.7f));

            RefreshWallets();
            Node initial = _nodes[0];
            foreach (var n in _nodes) if (Key(n) == _selectedKey) initial = n;
            Select(initial);
        }

        private long Capital() => (long)(Mathf.Round(Meta.personalMoney * _capital01 / 1000f) * 1000f);

        private void StartRun()
        {
            var gc = Ui.Gc;
            long capital = System.Math.Min(Capital(), Meta.personalMoney);
            gc.StartNewRun(_companyName, capital);
            gc.AutoSave();
            Ui.OnNewRunStarted();
            Sfx.Play(SfxId.Fanfare);
            RequestClose();
        }

        private static string Key(Node n) => n.IsRoot ? "root:" + n.tree.Skill : n.perk.Id;

        private VisualElement BuildTree(SkillTreeDef tree)
        {
            var style = Labels.SkillStyle(tree.Skill);
            var cell = Kit.Card(CellW, 18); cell.style.height = CellH; cell.style.marginRight = 12; cell.style.marginBottom = 12; Kit.Pad(cell, 0, 0);
            var head = Kit.Row(); head.style.position = Position.Absolute; head.style.left = 16; head.style.top = 12;
            head.Add(Kit.Icon(style.icon, 13, style.color));
            var hl = Kit.Text(tree.Name, 15, Kit.Ink, true); hl.style.marginLeft = 8;
            head.Add(hl);
            cell.Add(head);

            var layer = Kit.Fill(); layer.pickingMode = PickingMode.Ignore;
            layer.generateVisualContent += m => DrawLinks(m, tree);
            cell.Add(layer);
            _links.Add((layer, tree));

            var root = new Node { tree = tree, color = style.color, icon = style.icon, pos = new Vector2(34f, 120f) };
            PlaceNode(cell, root, RootSize);
            var rl = Kit.Col(); rl.style.position = Position.Absolute; rl.style.left = 8; rl.style.top = root.pos.y + RootSize + 6; rl.style.width = RootSize + 52;
            rl.style.alignItems = Align.Center;
            var rn = Kit.Wrap(tree.RootName, 11, Kit.Ink, true); rn.style.unityTextAlign = TextAnchor.MiddleCenter;
            rl.Add(rn);
            root.costLabel = Kit.Text("", 10, Kit.Soft, true);
            rl.Add(root.costLabel);
            cell.Add(rl);

            for (int i = 0; i < tree.Perks.Length; i++)
            {
                var node = new Node { tree = tree, perk = tree.Perks[i], color = style.color, icon = Kit.IcGem, pos = new Vector2(214f, 52f + i * 62f) };
                PlaceNode(cell, node, ChildSize);
                var lbl = Kit.Col(); lbl.style.position = Position.Absolute; lbl.style.left = node.pos.x + ChildSize + 10; lbl.style.top = node.pos.y + 4;
                lbl.style.width = 170;
                lbl.Add(Kit.Text(node.perk.Name, 12, Kit.Ink, true));
                node.costLabel = Kit.Text("", 10, Kit.Soft, true);
                lbl.Add(node.costLabel);
                cell.Add(lbl);
            }
            return cell;
        }

        private void PlaceNode(VisualElement cell, Node n, float size)
        {
            var disc = new VisualElement();
            disc.style.position = Position.Absolute; disc.style.left = n.pos.x; disc.style.top = n.pos.y;
            disc.style.width = size; disc.style.height = size; Kit.Radius(disc, size / 2f);
            disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;
            n.iconLabel = Kit.Icon(n.icon, Mathf.RoundToInt(size * 0.42f), Color.white);
            disc.Add(n.iconLabel);
            Kit.Smooth(disc);
            disc.RegisterCallback<ClickEvent>(_ => { Sfx.Play(SfxId.Click); Select(n); });
            disc.RegisterCallback<PointerEnterEvent>(_ => disc.style.scale = new Scale(new Vector2(1.12f, 1.12f)));
            disc.RegisterCallback<PointerLeaveEvent>(_ => disc.style.scale = new Scale(n == _selected ? new Vector2(1.08f, 1.08f) : Vector2.one));
            n.disc = disc;
            cell.Add(disc);
            _nodes.Add(n);
        }

        private int Level(Node n) => Meta.skillLevels[(int)n.tree.Skill - 1];
        private bool Owned(Node n) => n.IsRoot ? Level(n) > 0 : Meta.perks.Contains(n.perk.Id);
        private long Cost(Node n) => n.IsRoot ? SkillTreeCatalog.RootCost(Level(n)) : n.perk.CostPL;
        private bool CanBuy(Node n) => n.IsRoot
            ? Meta.personalMoney >= Cost(n)
            : n.perk.Implemented && !Owned(n) && Meta.legacyPoints >= n.perk.CostPL;

        private void Paint(Node n)
        {
            bool filled = Owned(n);
            bool afford = CanBuy(n);
            n.disc.style.backgroundColor = filled ? n.color : Color.white;
            Kit.Border(n.disc, n == _selected ? Kit.Ink : filled ? Kit.A(Color.white, 0.7f) : afford ? n.color : Kit.Track, n == _selected ? 5 : 3);
            n.iconLabel.style.color = filled ? Color.white : afford ? n.color : Kit.Faint;
            if (n.IsRoot) n.costLabel.text = "Nível " + Level(n) + " · R$ " + Kit.Short(Cost(n));
            else n.costLabel.text = filled ? "Comprada" : !n.perk.Implemented ? "Em breve" : n.perk.CostPL + " PL";
            n.costLabel.style.color = !n.IsRoot && filled ? n.color : Kit.Soft;
        }

        private VisualElement Wallet(string icon, Color c, string cap, out Label value)
        {
            var w = Kit.Card(0, 14); w.style.flexDirection = FlexDirection.Row; w.style.alignItems = Align.Center; Kit.Pad(w, 9, 16);
            w.Add(Kit.Badge(icon, c, 34));
            var t = Kit.Col(); t.style.marginLeft = 10;
            t.Add(Kit.Text(cap, 10, Kit.Soft, true));
            value = Kit.Text("", 20, c, true);
            t.Add(value);
            w.Add(t);
            return w;
        }

        private VisualElement BuildDetail()
        {
            var d = Kit.Card(330, 18); d.style.flexShrink = 0; d.style.marginBottom = 12; Kit.Pad(d, 20, 20); d.style.alignItems = Align.Center;
            _dBadge = Kit.Badge(Kit.IcStar, Kit.Teal, 84);
            _dIcon = (Label)_dBadge[0];
            d.Add(_dBadge);
            _dName = Kit.Wrap("", 20, Kit.Ink, true); _dName.style.marginTop = 14; _dName.style.unityTextAlign = TextAnchor.MiddleCenter;
            d.Add(_dName);
            _dType = Kit.Wrap("", 11, Kit.Soft, true); _dType.style.marginTop = 4; _dType.style.unityTextAlign = TextAnchor.MiddleCenter;
            d.Add(_dType);
            _dDesc = Kit.Wrap("", 13, Kit.Soft); _dDesc.style.marginTop = 12; _dDesc.style.unityTextAlign = TextAnchor.MiddleCenter;
            d.Add(_dDesc);
            d.Add(Kit.Spacer());
            d.Add(Kit.Text("Custo", 10, Kit.Soft, true));
            _dCost = Kit.Text("", 26, Kit.Ink, true);
            d.Add(_dCost);
            _dBuy = Kit.Button("Comprar", Kit.IcCheck, Kit.Teal, Color.white, Buy);
            _dBuyLabel = (Label)_dBuy[1];
            _dBuy.style.alignSelf = Align.Stretch; _dBuy.style.marginTop = 12;
            d.Add(_dBuy);
            var legend = Kit.Col(); legend.style.marginTop = 14; legend.style.alignSelf = Align.Stretch;
            legend.Add(LiveBits.Line(Kit.IcCoins, "Raiz", "várias vezes, com dinheiro", Kit.Soft));
            legend.Add(LiveBits.Line(Kit.IcHourglass, "Ramos", "únicos, com Legado", Kit.Soft));
            d.Add(legend);
            return d;
        }

        private void Select(Node n)
        {
            var prev = _selected;
            _selected = n;
            _selectedKey = Key(n);
            foreach (var x in _nodes) Paint(x);
            if (prev != null) prev.disc.style.scale = new Scale(Vector2.one);
            n.disc.style.scale = new Scale(new Vector2(1.08f, 1.08f));

            _dBadge.style.backgroundColor = n.color;
            _dIcon.text = n.icon;
            _dName.text = n.IsRoot ? n.tree.RootName : n.perk.Name;
            _dType.text = (n.IsRoot ? "INCREMENTAL · NÍVEL " + Level(n) : "HABILIDADE ÚNICA") + " · " + n.tree.Name.ToUpperInvariant();
            _dDesc.text = n.IsRoot ? n.tree.RootDescription : n.perk.Description;
            bool done = !n.IsRoot && Owned(n);
            bool soon = !n.IsRoot && !n.perk.Implemented;
            _dCost.text = done || soon ? "—" : n.IsRoot ? Kit.Money(Cost(n)) : n.perk.CostPL + " PL";
            _dCost.style.color = n.IsRoot ? Kit.Cash : Kit.Gold;
            bool can = CanBuy(n);
            _dBuy.style.backgroundColor = can ? n.color : Kit.Faint;
            _dBuyLabel.text = done ? "Já é sua" : soon ? "Em breve" : !can ? "Falta " + (n.IsRoot ? "dinheiro" : "Legado") : n.IsRoot ? "Subir de nível" : "Comprar";
            LiveBits.SetEnabled(_dBuy, can);
        }

        private void Buy()
        {
            var n = _selected;
            if (n == null || !CanBuy(n)) { Sfx.Play(SfxId.Fail); return; }
            bool ok = n.IsRoot ? SkillTreeCatalog.TryBuyRoot(Meta, n.tree.Skill) : SkillTreeCatalog.TryBuyPerk(Meta, n.perk.Id);
            if (!ok) { Sfx.Play(SfxId.Fail); return; }
            Sfx.Play(SfxId.Unlock);
            RefreshWallets();
            Select(n);
            var disc = n.disc;
            Animate(0f, 0.45f, k =>
            {
                float s = 1.08f + 0.35f * Mathf.Sin(k * Mathf.PI);
                disc.style.scale = new Scale(new Vector2(s, s));
            }, Linear);
        }

        private void RefreshWallets()
        {
            _moneyLabel.text = Kit.Money(Meta.personalMoney);
            _legacyLabel.text = Meta.legacyPoints + " PL";
            _capitalLabel.text = Kit.Money(System.Math.Min(Capital(), Meta.personalMoney));
        }

        protected override void OnTick(float dt)
        {
            foreach (var l in _links) l.layer.MarkDirtyRepaint();
            _bgDots.MarkDirtyRepaint();
            if (_runIcon != null)
                _runIcon.style.rotate = new Rotate(new Angle(Mathf.Floor(Clock / 2f) * 180f + Mathf.Clamp01((Clock % 2f) / 0.5f) * 180f));
        }

        private void DrawLinks(MeshGenerationContext mgc, SkillTreeDef tree)
        {
            var p = mgc.painter2D;
            p.lineCap = LineCap.Round;
            Color color = Labels.SkillStyle(tree.Skill).color;
            Vector2 from = new Vector2(34f + RootSize, 120f + RootSize * 0.5f);
            for (int i = 0; i < tree.Perks.Length; i++)
            {
                Vector2 to = new Vector2(214f, 52f + i * 62f + ChildSize * 0.5f);
                Vector2 c1 = new Vector2(from.x + 50f, from.y), c2 = new Vector2(to.x - 50f, to.y);
                bool on = Meta.perks.Contains(tree.Perks[i].Id);
                p.strokeColor = on ? color : Kit.Track; p.lineWidth = on ? 4f : 3f;
                p.BeginPath(); p.MoveTo(from); p.BezierCurveTo(c1, c2, to); p.Stroke();
                if (!on) continue;
                float k = (Clock * 0.6f + i * 0.23f) % 1f;
                float u = 1f - k;
                Vector2 pt = u * u * u * from + 3f * u * u * k * c1 + 3f * u * k * k * c2 + k * k * k * to;
                Kit.Disc(p, pt, 6f, Kit.A(color, 0.3f));
                Kit.Disc(p, pt, 3.5f, Color.white);
            }
        }

        private void DrawDots(MeshGenerationContext mgc)
        {
            var r = _bgDots.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            for (int k = 0; k < 26; k++)
            {
                float x = ((k * 0.137f + Mathf.Sin(Clock * 0.2f + k) * 0.02f) % 1f) * r.width;
                float y = (1f - ((Clock * 0.03f + k * 0.11f) % 1f)) * r.height;
                Kit.Disc(p, new Vector2(x, y), 3f + (k % 3), new Color(1f, 1f, 1f, 0.35f));
            }
        }
    }
}
