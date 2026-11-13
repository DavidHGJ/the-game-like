using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Resultado da run (GDD 20.4, 19.5): venda ou falência, dinheiro pessoal, Pontos de Legado e estatísticas.</summary>
    internal sealed class RunResultScreen : GalleryScreen
    {
        private sealed class Coin { public float x, y, vy, size, phase; }

        private SunburstBackdrop _bg;
        private FounderArt _art;
        private VisualElement _coinLayer;
        private readonly List<Coin> _coins = new List<Coin>();
        private int _state;


        public override float SettleSeconds => 4.4f;

        public override void SetState(int index)
        {
            _state = index;
            _coins.Clear();
            Root.Clear();
            Build();
        }

        protected override void Build()
        {
            bool sold = _state == 0;
            _bg = sold ? SunburstBackdrop.Gold() : SunburstBackdrop.Rose();
            Root.Add(_bg);
            _coinLayer = Kit.Fill(); _coinLayer.pickingMode = PickingMode.Ignore;
            _coinLayer.generateVisualContent += DrawCoins;
            Root.Add(_coinLayer);

            var center = Kit.Centered();
            Root.Add(center);

            var halo = new VisualElement(); halo.style.width = 150; halo.style.height = 150; Kit.Radius(halo, 75);
            halo.style.backgroundColor = Kit.A(Color.white, 0.6f); halo.style.alignItems = Align.Center; halo.style.justifyContent = Justify.FlexEnd;
            halo.style.overflow = Overflow.Hidden;
            _art = new FounderArt(140) { HairStyle = 1, Accessory = 1, Shirt = sold ? 0 : 4 };
            halo.Add(_art);
            center.Add(Pop(halo, 0.1f));

            var over = Kit.Caps("Run 4 encerrada · 9 anos de empresa", 13, Kit.Soft, 3f); over.style.marginTop = 14;
            center.Add(Enter(over, 0.3f));
            var head = Kit.Text(sold ? "EMPRESA VENDIDA!" : "FALÊNCIA", 64, sold ? Kit.Ink : Kit.Red, true);
            head.style.letterSpacing = 2;
            center.Add(Pop(head, 0.45f));
            center.Add(Enter(Kit.Text(sold ? "MegaByte Corp comprou a Pixel Forja" : "O caixa zerou e a Pixel Forja fechou as portas", 16, Kit.Soft), 0.6f));

            var money = Kit.Text("R$ 0", 54, sold ? Kit.Cash : Kit.Soft, true); money.style.marginTop = 8;
            center.Add(Enter(money, 0.7f));
            if (sold) CountUp(money, 0, 1240000, 0.9f, 1.6f, Kit.Money);
            center.Add(Enter(Kit.Text(sold ? "direto para o seu bolso" : "nada para levar desta vez", 13, Kit.Soft, true), 0.8f));

            var row = Kit.Row(Align.Stretch); row.style.marginTop = 24;
            center.Add(row);

            // pontos de legado
            var pl = Kit.Card(430, 18); Kit.Pad(pl, 18, 20); pl.style.marginRight = 16;
            var ph = Kit.Row();
            ph.Add(Kit.Badge(Kit.IcHourglass, Kit.Gold, 34));
            var pt = Kit.Text("Pontos de Legado", 16, Kit.Ink, true); pt.style.marginLeft = 10;
            ph.Add(pt); ph.Add(Kit.Spacer());
            var total = Kit.Text("0", 30, Kit.Gold, true);
            ph.Add(total);
            pl.Add(ph);
            pl.Add(Kit.Divider(10));
            (string label, int pts)[] lines = sold
                ? new[] { ("Chegou à fase Indie", 5), ("Chegou à fase Escritório", 15), ("3 jogos com nota acima de 7", 6), ("Valuation máximo acima de R$ 1M", 5), ("Run sem crise grave", 5) }
                : new[] { ("Chegou à fase Indie", 5), ("1 jogo com nota acima de 7", 2), ("Penalidade de falência", -3) };
            int sum = 0; float at = 1.6f;
            foreach (var l in lines)
            {
                sum += l.pts;
                var r = Kit.Row(); r.style.marginBottom = 7;
                r.Add(Kit.Icon(l.pts >= 0 ? Kit.IcCheck : Kit.IcClose, 11, l.pts >= 0 ? Kit.Green : Kit.Red));
                var lt = Kit.Text(l.label, 13, Kit.Ink); lt.style.marginLeft = 8;
                r.Add(lt); r.Add(Kit.Spacer());
                r.Add(Kit.Text((l.pts >= 0 ? "+" : "") + l.pts, 14, l.pts >= 0 ? Kit.Gold : Kit.Red, true));
                pl.Add(r);
                SlideIn(r, -24f, 0f, at, 0.35f);
                at += 0.22f;
            }
            CountUp(total, 0, sum, 1.6f, at - 1.4f, v => Mathf.RoundToInt(v).ToString());
            row.Add(Enter(pl, 1.0f));

            // estatísticas
            var st = Kit.Card(430, 18); Kit.Pad(st, 18, 20);
            st.Add(Kit.Text("A run em números", 16, Kit.Ink, true));
            st.Add(Kit.Divider(10));
            var grid = Kit.Row(); grid.style.flexWrap = Wrap.Wrap;
            grid.Add(Stat(Kit.IcGamepad, "Jogos lançados", sold ? "7" : "2"));
            grid.Add(Stat(Kit.IcStar, "Melhor nota", sold ? "8,7" : "7,2"));
            grid.Add(Stat(Kit.IcChartLine, "Valuation máximo", sold ? "R$ 1,3M" : "R$ 190K"));
            grid.Add(Stat(Kit.IcBuilding, "Fase alcançada", sold ? "Escritório" : "Indie"));
            grid.Add(Stat(Kit.IcCrown, "Franquias", sold ? "2" : "0"));
            grid.Add(Stat(Kit.IcUsers, "Maior equipe", sold ? "5" : "2"));
            st.Add(grid);
            row.Add(Enter(st, 1.1f));

            var ach = Kit.Row(); ach.style.marginTop = 16;
            if (sold)
            {
                ach.Add(Pop(Trophy("Primeira Venda"), 3.0f));
                ach.Add(Pop(Trophy("Negócio Lucrativo"), 3.2f));
            }
            else
            {
                var note = Kit.Chip("Sua empresa vira rival na próxima timeline", Kit.Soft, false, Kit.IcHourglass);
                ach.Add(Enter(note, 2.6f));
            }
            center.Add(ach);

            var go = Kit.Button("Gastar em habilidades", Kit.IcArrowRight, Kit.Teal, Color.white, null, 52); go.style.marginTop = 20;
            center.Add(Enter(go, sold ? 3.3f : 2.8f));
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

        private static VisualElement Trophy(string name)
        {
            var c = Kit.Card(0, 14); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center;
            c.style.marginLeft = 6; c.style.marginRight = 6; Kit.Pad(c, 8, 14); Kit.Border(c, Kit.Gold, 2); c.style.borderBottomWidth = 4;
            c.Add(Kit.Badge(Kit.IcTrophy, Kit.Gold, 30));
            var t = Kit.Col(); t.style.marginLeft = 10;
            t.Add(Kit.Caps("Conquista", 9, Kit.Gold));
            t.Add(Kit.Text(name, 14, Kit.Ink, true));
            c.Add(t);
            return c;
        }

        protected override void OnTick(float dt)
        {
            _bg.Tick(dt);
            _art.Tick(dt);
            if (_state != 0) return;
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
                float squash = Mathf.Abs(Mathf.Cos(Clock * 6f + c.phase));
                var pos = new Vector2(c.x, c.y);
                p.fillColor = Kit.A(Kit.Gold, 0.9f);
                p.BeginPath();
                for (int k = 0; k <= 12; k++)
                {
                    float a = k / 12f * Mathf.PI * 2f;
                    var pt = new Vector2(pos.x + Mathf.Cos(a) * c.size * Mathf.Max(0.15f, squash), pos.y + Mathf.Sin(a) * c.size);
                    if (k == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.ClosePath(); p.Fill();
            }
        }
    }

    /// <summary>
    /// Árvore de habilidades do fundador (GDD 14): 6 árvores, raiz incremental ($) + 4 filhos únicos (PL),
    /// reset de PL, capital para a próxima run e o botão Nova Run.
    /// </summary>
    internal sealed class SkillTreeScreen : GalleryScreen
    {
        private sealed class Node
        {
            public string name, desc, icon;
            public bool root, owned;
            public int level, cost;
            public Color color;
            public VisualElement disc;
            public Label iconLabel, costLabel;
            public Vector2 pos;
            public Tree tree;
        }

        private sealed class Tree
        {
            public string name, icon;
            public Color color;
            public Node root;
            public readonly List<Node> children = new List<Node>();
            public VisualElement links;
        }

        private const float CellW = 452f, CellH = 318f, RootSize = 74f, ChildSize = 44f;

        private readonly List<Tree> _trees = new List<Tree>();
        private Node _selected;
        private float _money = 1240000f;
        private int _legacy = 36;
        private Label _moneyLabel, _legacyLabel, _dName, _dType, _dDesc, _dCost, _dIcon, _capitalLabel;
        private VisualElement _dBadge, _dBuy, _detail, _runIcon, _bgDots;
        private Label _dBuyLabel;

        protected override void Build()
        {
            Root.style.backgroundColor = Kit.RGB(238, 231, 214);
            _bgDots = Kit.Fill(); _bgDots.pickingMode = PickingMode.Ignore; _bgDots.generateVisualContent += DrawDots;
            Root.Add(_bgDots);
            BuildData();

            var page = Kit.Fill(); Kit.Pad(page, 22, 28);
            Root.Add(page);

            // topo
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
            for (int i = 0; i < _trees.Count; i++) grid.Add(Enter(BuildTree(_trees[i]), 0.15f + i * 0.08f));

            _detail = BuildDetail();
            mid.Add(Enter(_detail, 0.5f));

            // rodapé
            var bottom = Kit.Card(0, 18); bottom.style.flexDirection = FlexDirection.Row; bottom.style.alignItems = Align.Center;
            bottom.style.marginTop = 10; Kit.Pad(bottom, 14, 20);
            bottom.Add(Kit.Ghost("Resetar Pontos de Legado · R$ 50.000", Kit.IcUndo));
            bottom.Add(Kit.Gap(26));
            var cap = Kit.Col(); cap.style.flexGrow = 1;
            var cr = Kit.Row();
            cr.Add(Kit.Text("Capital para a próxima run", 12, Kit.Soft, true)); cr.Add(Kit.Spacer());
            _capitalLabel = Kit.Text("", 16, Kit.Cash, true);
            cr.Add(_capitalLabel);
            cap.Add(cr);
            cap.Add(Widgets.Slider(0.25f, Kit.Cash, v => _capitalLabel.text = Kit.Money(Mathf.Round(_money * v / 1000f) * 1000f)));
            bottom.Add(cap);
            bottom.Add(Kit.Gap(26));
            var run = Kit.Button("Nova Run", Kit.IcHourglass, Kit.Teal, Color.white, null, 56);
            _runIcon = run[0];
            bottom.Add(run);
            page.Add(Enter(bottom, 0.7f));

            RefreshWallets();
            _capitalLabel.text = Kit.Money(Mathf.Round(_money * 0.25f / 1000f) * 1000f);
            Select(_trees[0].children[1]);
        }

        private void BuildData()
        {
            _trees.Clear();
            AddTree("Visão de Negócio", Kit.IcBriefcase, Kit.Cash, "Faro Empresarial", "+5% no valuation base ao iniciar cada run. Custo e bônus crescem a cada compra.", 2,
                ("Negociador Nato", Kit.IcHandshake, "Reduz o custo de licenças de plataforma e de IP externo.", 5, true),
                ("Olho no Mercado", Kit.IcEye, "Revela tendências de mercado com maior antecedência.", 10, false),
                ("Visão de Longo Prazo", Kit.IcBinoculars, "Reduz o decaimento de valuation por estagnação.", 15, false),
                ("Fusão Estratégica", Kit.IcLink, "Reduz o custo de P&D para desbloquear M&A.", 25, false));
            AddTree("Liderança", Kit.IcUsers, Kit.Orange, "Cultura Forte", "+5% de satisfação base dos funcionários ao contratar.", 1,
                ("Anti-Burnout", Kit.IcPulse, "Reduz a taxa de acúmulo de burnout da equipe.", 5, true),
                ("Retenção de Talentos", Kit.IcShield, "Reduz a chance de funcionários serem assediados por rivais.", 10, false),
                ("Mentor Nato", Kit.IcGradCap, "O fundador gera mais XP nos funcionários ao redor.", 15, false),
                ("Equipe Enxuta", Kit.IcUser, "Bônus de eficiência maior em equipes pequenas (Garagem e Indie).", 10, false));
            AddTree("Técnica", Kit.IcCode, Kit.Sky, "Curva de Aprendizado", "+5% de PP gerado passivamente durante o desenvolvimento.", 3,
                ("Arquiteto de Engine", Kit.IcChip, "Reduz o custo de P&D para módulos de engine.", 10, true),
                ("Code Review", Kit.IcBug, "Reduz a geração passiva de bugs durante o desenvolvimento.", 5, true),
                ("Prototipagem Rápida", Kit.IcBolt, "Reduz o tempo de desenvolvimento de projetos pequenos.", 10, false),
                ("Otimizador", Kit.IcWrench, "Aumenta o Engine Budget das engines criadas.", 20, false));
            AddTree("Marketing", Kit.IcBullhorn, Kit.Pink, "Presença Digital", "+5% de hype gerado por todas as campanhas.", 1,
                ("Timing Perfeito", Kit.IcCalendar, "Revela janelas de lançamento ideais com maior antecedência.", 5, false),
                ("Viral Natural", Kit.IcFire, "Chance de campanhas de wishlist gerarem um pico orgânico de hype.", 10, false),
                ("Relações com Imprensa", Kit.IcNews, "Aumenta a nota base recebida da crítica.", 20, false),
                ("Recuperação de Crise", Kit.IcUndo, "Reduz o impacto de declarações ruins no sentimento público.", 10, false));
            AddTree("Carisma", Kit.IcComments, Kit.Purple, "Presença Pública", "Reduz a chance de declarações saírem pela culatra.", 1,
                ("CEO Celebridade", Kit.IcStar, "Declarações positivas geram um pico maior de valuation.", 10, false),
                ("Discurso Afiado", Kit.IcMic, "Desbloqueia declarações exclusivas de alto impacto positivo.", 15, false),
                ("Gerenciamento de Crise", Kit.IcShield, "Declarações ruins viram eventos gerenciáveis em vez de crises.", 10, true),
                ("Embaixador da Marca", Kit.IcTrophy, "Aumenta o hype gerado em game shows e eventos.", 10, false));
            AddTree("Networking", Kit.IcNetwork, Kit.Teal, "Agenda Cheia", "+5% de chance de receber propostas de contratos exclusivos.", 0,
                ("Contatos no Mercado", Kit.IcUserPlus, "Funcionários raros no mercado de talentos desde o início da run.", 15, false),
                ("Parceiro Estratégico", Kit.IcHandshake, "Reduz o custo de co-desenvolvimento e publisher.", 10, false),
                ("Insider", Kit.IcSecret, "Revela projetos em desenvolvimento dos rivais mais cedo.", 10, false),
                ("Headhunter", Kit.IcSearch, "Aumenta a qualidade dos candidatos no mercado de talentos.", 20, false));
        }

        private void AddTree(string name, string icon, Color color, string rootName, string rootDesc, int level,
            params (string name, string icon, string desc, int cost, bool owned)[] children)
        {
            var t = new Tree { name = name, icon = icon, color = color };
            t.root = new Node
            {
                name = rootName, desc = rootDesc, icon = icon, root = true, level = level, owned = level > 0,
                cost = 10000 * (1 << level), color = color, tree = t, pos = new Vector2(34f, 120f)
            };
            for (int i = 0; i < children.Length; i++)
            {
                var c = children[i];
                t.children.Add(new Node
                {
                    name = c.name, desc = c.desc, icon = c.icon, cost = c.cost, owned = c.owned, color = color, tree = t,
                    pos = new Vector2(214f, 52f + i * 62f)
                });
            }
            _trees.Add(t);
        }

        private VisualElement BuildTree(Tree t)
        {
            var cell = Kit.Card(CellW, 18); cell.style.height = CellH; cell.style.marginRight = 12; cell.style.marginBottom = 12; Kit.Pad(cell, 0, 0);
            var head = Kit.Row(); head.style.position = Position.Absolute; head.style.left = 16; head.style.top = 12;
            head.Add(Kit.Icon(t.icon, 13, t.color));
            var hl = Kit.Text(t.name, 15, Kit.Ink, true); hl.style.marginLeft = 8;
            head.Add(hl);
            cell.Add(head);

            t.links = Kit.Fill(); t.links.pickingMode = PickingMode.Ignore;
            t.links.generateVisualContent += m => DrawLinks(m, t);
            cell.Add(t.links);

            PlaceNode(cell, t.root, RootSize);
            var rl = Kit.Col(); rl.style.position = Position.Absolute; rl.style.left = 8; rl.style.top = t.root.pos.y + RootSize + 6; rl.style.width = RootSize + 52;
            rl.style.alignItems = Align.Center;
            var rn = Kit.Wrap(t.root.name, 11, Kit.Ink, true); rn.style.unityTextAlign = TextAnchor.MiddleCenter;
            rl.Add(rn);
            t.root.costLabel = Kit.Text("", 10, Kit.Soft, true);
            rl.Add(t.root.costLabel);
            cell.Add(rl);

            foreach (var c in t.children)
            {
                PlaceNode(cell, c, ChildSize);
                var lbl = Kit.Col(); lbl.style.position = Position.Absolute; lbl.style.left = c.pos.x + ChildSize + 10; lbl.style.top = c.pos.y + 4;
                lbl.style.width = 170;
                lbl.Add(Kit.Text(c.name, 12, Kit.Ink, true));
                c.costLabel = Kit.Text("", 10, Kit.Soft, true);
                lbl.Add(c.costLabel);
                cell.Add(lbl);
            }
            foreach (var n in AllNodes(t)) Paint(n);
            return cell;
        }

        private static IEnumerable<Node> AllNodes(Tree t)
        {
            yield return t.root;
            foreach (var c in t.children) yield return c;
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
            disc.RegisterCallback<ClickEvent>(_ => Select(n));
            disc.RegisterCallback<PointerEnterEvent>(_ => disc.style.scale = new Scale(new Vector2(1.12f, 1.12f)));
            disc.RegisterCallback<PointerLeaveEvent>(_ => disc.style.scale = new Scale(n == _selected ? new Vector2(1.08f, 1.08f) : Vector2.one));
            n.disc = disc;
            cell.Add(disc);
        }

        private bool CanAfford(Node n) => n.root ? _money >= n.cost : _legacy >= n.cost;

        private void Paint(Node n)
        {
            bool filled = n.owned && !n.root || n.root && n.level > 0;
            bool afford = CanAfford(n);
            n.disc.style.backgroundColor = filled ? n.color : Color.white;
            Kit.Border(n.disc, filled ? Kit.A(Color.white, 0.7f) : afford ? n.color : Kit.Track, n == _selected ? 5 : 3);
            if (n == _selected) Kit.BorderColor(n.disc, Kit.Ink);
            n.iconLabel.style.color = filled ? Color.white : afford ? n.color : Kit.Faint;
            if (n.root) n.costLabel.text = "Nível " + n.level + " · " + Kit.Short(n.cost).Insert(0, "R$ ");
            else n.costLabel.text = n.owned ? "Comprada" : n.cost + " PL";
            n.costLabel.style.color = !n.root && n.owned ? n.color : Kit.Soft;
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
            _dType = Kit.Text("", 11, Kit.Soft, true); _dType.style.marginTop = 4;
            d.Add(_dType);
            _dDesc = Kit.Wrap("", 13, Kit.Soft); _dDesc.style.marginTop = 12; _dDesc.style.unityTextAlign = TextAnchor.MiddleCenter;
            d.Add(_dDesc);
            d.Add(Kit.Spacer());
            d.Add(Kit.Text("Custo", 10, Kit.Soft, true));
            _dCost = Kit.Text("", 26, Kit.Ink, true);
            d.Add(_dCost);
            _dBuy = Kit.Button("Comprar", Kit.IcCheck, Kit.Teal, Color.white);
            _dBuyLabel = (Label)_dBuy[1];
            _dBuy.style.alignSelf = Align.Stretch; _dBuy.style.marginTop = 12;
            _dBuy.RegisterCallback<ClickEvent>(_ => Buy());
            d.Add(_dBuy);
            var legend = Kit.Col(); legend.style.marginTop = 14; legend.style.alignSelf = Align.Stretch;
            legend.Add(LegendLine(Kit.IcCoins, Kit.Cash, "Raiz: compra várias vezes com dinheiro"));
            legend.Add(LegendLine(Kit.IcHourglass, Kit.Gold, "Ramos: únicos, pagos com Legado"));
            d.Add(legend);
            return d;
        }

        private static VisualElement LegendLine(string icon, Color c, string text)
        {
            var r = Kit.Row(); r.style.marginTop = 4;
            var i = Kit.Icon(icon, 10, c); i.style.width = 16;
            r.Add(i);
            var t = Kit.Text(text, 10, Kit.Soft); t.style.marginLeft = 6;
            r.Add(t);
            return r;
        }

        private void Select(Node n)
        {
            var prev = _selected;
            _selected = n;
            if (prev != null) { Paint(prev); prev.disc.style.scale = new Scale(Vector2.one); }
            Paint(n);
            n.disc.style.scale = new Scale(new Vector2(1.08f, 1.08f));

            _dBadge.style.backgroundColor = n.color;
            _dIcon.text = n.icon;
            _dName.text = n.name;
            _dType.text = (n.root ? "INCREMENTAL · NÍVEL " + n.level : "HABILIDADE ÚNICA") + " · " + n.tree.name.ToUpperInvariant();
            _dDesc.text = n.desc;
            bool done = !n.root && n.owned;
            _dCost.text = done ? "—" : n.root ? Kit.Money(n.cost) : n.cost + " PL";
            _dCost.style.color = n.root ? Kit.Cash : Kit.Gold;
            bool afford = CanAfford(n);
            _dBuy.style.backgroundColor = done ? Kit.Track : afford ? n.color : Kit.Faint;
            _dBuyLabel.text = done ? "Já é sua" : !afford ? "Falta " + (n.root ? "dinheiro" : "Legado") : n.root ? "Subir de nível" : "Comprar";
            Animate(0f, 0.3f, k =>
            {
                float s = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
                _dBadge.style.scale = new Scale(new Vector2(s, s));
            }, Linear);
        }

        private void Buy()
        {
            var n = _selected;
            if (n == null || (!n.root && n.owned) || !CanAfford(n)) return;
            if (n.root) { _money -= n.cost; n.level++; n.owned = true; n.cost *= 2; }
            else { _legacy -= n.cost; n.owned = true; }
            RefreshWallets();
            foreach (var t in _trees) foreach (var x in AllNodes(t)) Paint(x);
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
            _moneyLabel.text = Kit.Money(_money);
            _legacyLabel.text = _legacy + " PL";
        }

        protected override void OnTick(float dt)
        {
            foreach (var t in _trees) t.links.MarkDirtyRepaint();
            _bgDots.MarkDirtyRepaint();
            if (_runIcon != null) _runIcon.style.rotate = new Rotate(new Angle(Mathf.Floor(Clock / 2f) * 180f + Mathf.Clamp01((Clock % 2f) / 0.5f) * 180f));
        }

        private void DrawLinks(MeshGenerationContext mgc, Tree t)
        {
            var p = mgc.painter2D;
            p.lineCap = LineCap.Round;
            Vector2 from = t.root.pos + new Vector2(RootSize, RootSize * 0.5f);
            for (int i = 0; i < t.children.Count; i++)
            {
                var c = t.children[i];
                Vector2 to = c.pos + new Vector2(0f, ChildSize * 0.5f);
                Vector2 c1 = new Vector2(from.x + 50f, from.y), c2 = new Vector2(to.x - 50f, to.y);
                bool on = c.owned;
                p.strokeColor = on ? t.color : Kit.Track; p.lineWidth = on ? 4f : 3f;
                p.BeginPath(); p.MoveTo(from); p.BezierCurveTo(c1, c2, to); p.Stroke();
                if (!on) continue;
                float k = (Clock * 0.6f + i * 0.23f) % 1f;
                float u = 1f - k;
                Vector2 pt = u * u * u * from + 3f * u * u * k * c1 + 3f * u * k * k * c2 + k * k * k * to;
                Kit.Disc(p, pt, 6f, Kit.A(t.color, 0.3f));
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

    /// <summary>Fábrica das telas da galeria.</summary>
    internal static class GalleryScreens
    {
        public static GalleryScreen Create(GalleryScreenId id)
        {
            switch (id)
            {
                case GalleryScreenId.SaveSlots: return new SaveSlotsScreen();
                case GalleryScreenId.Options: return new OptionsScreen();
                case GalleryScreenId.FounderCreate: return new FounderCreateScreen();
                case GalleryScreenId.NewProject: return new NewProjectScreen();
                case GalleryScreenId.PhaseAllocation: return new PhaseAllocationScreen();
                case GalleryScreenId.Marketing: return new MarketingScreen();
                case GalleryScreenId.Launch: return new LaunchScreen();
                case GalleryScreenId.Reviews: return new ReviewsScreen();
                case GalleryScreenId.Finance: return new FinanceScreen();
                case GalleryScreenId.Projects: return new ProjectsScreen();
                case GalleryScreenId.Staff: return new StaffScreen();
                case GalleryScreenId.Market: return new MarketScreen();
                case GalleryScreenId.Research: return new ResearchScreen();
                case GalleryScreenId.Relations: return new RelationsScreen();
                case GalleryScreenId.Founder: return new FounderScreen();
                case GalleryScreenId.NarrativeEvent: return new NarrativeEventScreen();
                case GalleryScreenId.PhaseExpansion: return new PhaseExpansionScreen();
                case GalleryScreenId.Acquisition: return new AcquisitionScreen();
                case GalleryScreenId.SellCompany: return new SellCompanyScreen();
                case GalleryScreenId.PauseMenu: return new PauseMenuScreen();
                case GalleryScreenId.Toasts: return new ToastsScreen();
                case GalleryScreenId.RunResult: return new RunResultScreen();
                default: return new SkillTreeScreen();
            }
        }
    }
}
