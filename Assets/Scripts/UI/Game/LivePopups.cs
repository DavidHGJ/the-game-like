using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Meta;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>Evento narrativo vivo (GDD 11): mostra o evento sorteado e resolve a opção escolhida.</summary>
    internal sealed class EventLive : GalleryScreen
    {
        private string _body;
        private Label _text, _bigIcon;
        private VisualElement _pauseChip;

        protected override void Build()
        {
            Scrim(0.55f);
            var events = Ui.Gc.Events;
            var ev = events.Current;
            if (ev == null) { RequestClose(); return; }
            _body = ev.Body ?? "";
            var style = Labels.CategoryStyle(ev.Category);

            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(820, 22); Kit.Pad(card, 0, 0); card.style.borderBottomWidth = 6; card.style.overflow = Overflow.Hidden;
            center.Add(Enter(card, 0.05f, 34f));

            var band = new VisualElement(); band.style.height = 140; band.style.backgroundColor = style.color;
            band.style.alignItems = Align.Center; band.style.justifyContent = Justify.Center;
            for (int i = 0; i < 5; i++)
            {
                var ring = new VisualElement(); ring.style.position = Position.Absolute;
                float sz = 120 + i * 90; ring.style.width = sz; ring.style.height = sz; Kit.Radius(ring, sz / 2f);
                Kit.Border(ring, Kit.A(Color.white, 0.10f), 2);
                band.Add(ring);
            }
            var disc = Kit.Badge(style.icon, Color.white, 88, style.color);
            _bigIcon = (Label)disc[0];
            band.Add(disc);
            _pauseChip = Kit.Chip("Tempo pausado", Kit.Ink, true, Kit.IcPause);
            _pauseChip.style.position = Position.Absolute; _pauseChip.style.top = 14; _pauseChip.style.right = 16;
            band.Add(_pauseChip);
            card.Add(band);

            var body = Kit.Col(); Kit.Pad(body, 22, 30);
            card.Add(body);
            var cat = Kit.Chip(Labels.Category(ev.Category), style.color); cat.style.alignSelf = Align.FlexStart;
            body.Add(cat);
            var title = Kit.Wrap(ev.Title, 28, Kit.Ink, true); title.style.marginTop = 8;
            body.Add(title);
            _text = Kit.Wrap("", 15, Kit.Soft); _text.style.marginTop = 8; _text.style.minHeight = 66;
            body.Add(_text);

            var options = ev.Options;
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var option = options[i];
                bool needsSkill = option.requiredSkill != Data.Definitions.FounderSkill.None;
                bool available = events.IsOptionAvailable(option);
                Color accent = !available ? Kit.Faint : needsSkill ? Kit.Teal : style.color;

                var o = Kit.Outline(14); o.style.flexDirection = FlexDirection.Row; o.style.alignItems = Align.Center;
                o.style.marginTop = 9; Kit.Pad(o, 12, 16);
                Kit.Smooth(o);
                if (needsSkill && available) { Kit.BorderColor(o, Kit.Teal); o.style.backgroundColor = Kit.A(Kit.Teal, 0.07f); }
                if (!available) o.style.opacity = 0.6f;
                var letter = Kit.Text(((char)('A' + i)).ToString(), 14, Color.white, true);
                letter.style.width = 30; letter.style.height = 30; Kit.Radius(letter, 15); letter.style.backgroundColor = accent;
                letter.style.unityTextAlign = TextAnchor.MiddleCenter; letter.style.flexShrink = 0;
                o.Add(letter);
                var t = Kit.Wrap(option.text, 15, available ? Kit.Ink : Kit.Soft, true); t.style.marginLeft = 14; t.style.flexGrow = 1; t.style.flexShrink = 1;
                o.Add(t);
                if (needsSkill)
                    o.Add(Kit.Chip(Labels.Skill(option.requiredSkill), available ? Kit.Teal : Kit.Soft, available, available ? Kit.IcCheck : Kit.IcLock));
                if (available)
                {
                    o.RegisterCallback<PointerEnterEvent>(_ => { o.style.translate = new Translate(8, 0, 0); Kit.BorderColor(o, accent); Kit.UiSound?.Invoke(0); });
                    o.RegisterCallback<PointerLeaveEvent>(_ => { o.style.translate = new Translate(0, 0, 0); Kit.BorderColor(o, needsSkill ? Kit.Teal : Kit.Track); });
                    o.RegisterCallback<ClickEvent>(_ =>
                    {
                        Sfx.Play(SfxId.Click);
                        events.ResolveOption(index);
                        RequestClose();
                    });
                }
                body.Add(o);
                Animate(1.2f + i * 0.12f, 0.4f, k =>
                {
                    o.style.opacity = Mathf.Clamp01(k * 1.5f) * (available ? 1f : 0.6f);
                    o.style.translate = new Translate(-30f * (1f - k), 0, 0);
                });
            }

            var foot = Kit.Row(); foot.style.marginTop = 14; foot.style.justifyContent = Justify.Center;
            foot.Add(Kit.Icon(Kit.IcQuestion, 11, Kit.Faint));
            var ft = Kit.Text("Você nunca sabe exatamente o que vai acontecer.", 11, Kit.Faint); ft.style.marginLeft = 6;
            foot.Add(ft);
            body.Add(foot);
        }

        protected override void OnTick(float dt)
        {
            if (_text == null) return;
            int n = Mathf.Clamp(Mathf.FloorToInt((Clock - 0.4f) * 130f), 0, _body.Length);
            if (_text.text.Length != n) _text.text = _body.Substring(0, n);
            _bigIcon.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 3f) * 8f));
            _pauseChip.style.opacity = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Clock * 2.5f));
        }
    }

    /// <summary>Oferta de expansão de fase (GDD 2.1 / adendo 23.8).</summary>
    internal sealed class ExpansionLive : GalleryScreen
    {
        private VisualElement _arrow, _rays;

        private static (string icon, string title, string text)[] UnlocksFor(CompanyPhase next) => next switch
        {
            CompanyPhase.Indie => new[]
            {
                (Kit.IcUsers, "Equipe", "2 funcionários + fundador"), (Kit.IcExpand, "Projetos", "Escopo Médio pesquisável"),
                (Kit.IcFlask, "P&D", "Módulos de engine médios"), (Kit.IcBullhorn, "Marketing", "Beta fechado")
            },
            CompanyPhase.Office => new[]
            {
                (Kit.IcUsers, "Equipe", "5 funcionários + fundador"), (Kit.IcExpand, "Projetos", "Escopo Grande pesquisável"),
                (Kit.IcCrown, "Negócios", "Franquias e M&A"), (Kit.IcBullhorn, "Marketing", "Influencers e game show")
            },
            _ => new[]
            {
                (Kit.IcUsers, "Equipe", "8 funcionários + fundador"), (Kit.IcExpand, "Projetos", "Escopo AAA pesquisável"),
                (Kit.IcChartLine, "Negócios", "IPO e rivais grandes"), (Kit.IcChip, "Engine", "Módulos de ponta")
            }
        };

        protected override void Build()
        {
            Scrim(0.5f);
            var gc = Ui.Gc;
            var c = gc.Run.company;
            if (!c.expansionAvailable) { RequestClose(); return; }
            var next = gc.NextPhase;
            long cost = gc.ExpansionCost;

            _rays = Kit.Fill(); _rays.pickingMode = PickingMode.Ignore;
            _rays.generateVisualContent += DrawRays;
            Root.Add(_rays);
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(900, 22); Kit.Pad(card, 28, 34); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 34f));

            card.Add(Kit.Caps("Valuation acima de " + Kit.Money(gc.NextPhaseThreshold), 12, Kit.Teal, 2.5f));
            var title = Kit.Text("Hora de crescer!", 38, Kit.Ink, true); title.style.marginTop = 4;
            card.Add(title);
            card.Add(Kit.Text("A fase " + Labels.Phase(c.phase) + " ficou pequena para a " + c.name + ".", 15, Kit.Soft));

            var row = Kit.Row(); row.style.marginTop = 22;
            row.Add(Pop(Office(Labels.PhaseIcon(c.phase), Labels.Phase(c.phase), Labels.PhaseBlurb(c.phase), Kit.Soft, false), 0.4f));
            _arrow = Kit.Icon(Kit.IcArrowRight, 34, Kit.Teal); _arrow.style.marginLeft = 26; _arrow.style.marginRight = 26;
            row.Add(_arrow);
            row.Add(Pop(Office(Labels.PhaseIcon(next), Labels.Phase(next), Labels.PhaseBlurb(next), Kit.Teal, true), 0.6f));
            card.Add(row);

            var unlocks = Kit.Row(Align.Stretch); unlocks.style.marginTop = 24; unlocks.style.alignSelf = Align.Stretch;
            float at = 0.9f;
            foreach (var u in UnlocksFor(next))
            {
                var tile = Kit.Outline(14); tile.style.flexGrow = 1; tile.style.flexBasis = 0; tile.style.marginLeft = 5; tile.style.marginRight = 5;
                tile.style.alignItems = Align.Center; Kit.Pad(tile, 12, 8);
                tile.Add(Kit.Badge(u.icon, Kit.A(Kit.Teal, 0.16f), 36, Kit.Teal));
                var t = Kit.Text(u.title, 13, Kit.Ink, true); t.style.marginTop = 6;
                tile.Add(t);
                var d = Kit.Wrap(u.text, 11, Kit.Soft); d.style.unityTextAlign = TextAnchor.MiddleCenter;
                tile.Add(d);
                unlocks.Add(Enter(tile, at));
                at += 0.1f;
            }
            card.Add(unlocks);

            var warn = Kit.Row(); warn.style.marginTop = 18; warn.style.backgroundColor = Kit.A(Kit.Gold, 0.18f);
            Kit.Radius(warn, 12); Kit.Pad(warn, 10, 16); warn.style.alignSelf = Align.Stretch;
            warn.Add(Kit.Icon(Kit.IcWarn, 14, Kit.Gold));
            string stag = c.stagnationWeeks > 0 ? " Já são " + c.stagnationWeeks + " semana(s) estagnado." : "";
            var wt = Kit.Wrap("Você escolhe quando. Mas ficar parado tem custo: o valuation perde um pouco a cada semana." + stag + " Os custos fixos também sobem na nova fase.", 12, Kit.Ink);
            wt.style.marginLeft = 10; wt.style.flexShrink = 1;
            warn.Add(wt);
            card.Add(Enter(warn, 1.3f));

            bool canPay = c.cash >= cost;
            var foot = Kit.Row(); foot.style.marginTop = 20; foot.style.alignSelf = Align.Stretch;
            var costCol = Kit.Col();
            costCol.Add(Kit.Text("Custo da mudança", 11, Kit.Soft, true));
            costCol.Add(Kit.Text(Kit.Money(cost), 26, canPay ? Kit.Ink : Kit.Red, true));
            if (!canPay) costCol.Add(Kit.Text("Falta caixa — você tem " + Kit.Money(c.cash), 11, Kit.Red, true));
            foot.Add(costCol);
            foot.Add(Kit.Spacer());
            foot.Add(Kit.Ghost("Agora não", null, RequestClose));
            foot.Add(Kit.Gap(12));
            var go = Kit.Confirm("Expandir empresa", Kit.IcRocket, () =>
            {
                if (gc.ExpandPhase()) RequestClose(); else Sfx.Play(SfxId.Fail);
            });
            LiveBits.SetEnabled(go, canPay);
            foot.Add(go);
            card.Add(foot);
        }

        private static VisualElement Office(string icon, string name, string sub, Color c, bool next)
        {
            var o = Kit.Tile(18); o.style.width = 230; o.style.alignItems = Align.Center; Kit.Pad(o, 18, 14);
            if (next) { o.style.backgroundColor = Kit.A(c, 0.14f); Kit.Border(o, c, 3); }
            o.Add(Kit.SquareBadge(icon, next ? c : Kit.Faint, 76, 20));
            var n = Kit.Text(name, 20, Kit.Ink, true); n.style.marginTop = 10;
            o.Add(n);
            o.Add(Kit.Text(sub, 12, Kit.Soft));
            return o;
        }

        protected override void OnTick(float dt)
        {
            if (_arrow == null) return;
            _arrow.style.translate = new Translate(Mathf.PingPong(Clock * 22f, 12f) - 6f, 0, 0);
            _rays.MarkDirtyRepaint();
        }

        private void DrawRays(MeshGenerationContext mgc)
        {
            var r = _rays.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center; float rad = Mathf.Max(r.width, r.height);
            float a = Mathf.Clamp01((Clock - 0.1f) / 0.6f) * 0.10f;
            for (int k = 0; k < 14; k++)
            {
                float a0 = Clock * 8f + k * (360f / 14f);
                p.fillColor = new Color(1f, 0.93f, 0.7f, a);
                p.BeginPath(); p.MoveTo(c); p.LineTo(Kit.OnCircle(c, rad, a0)); p.LineTo(Kit.OnCircle(c, rad, a0 + 12f)); p.ClosePath(); p.Fill();
            }
        }
    }

    /// <summary>Encerrar a run (GDD 2.4 / 12.1): oferta de um rival, ou as saídas voluntárias (venda / IPO).</summary>
    internal sealed class SellLive : GalleryScreen
    {
        private readonly bool _offer;
        private VisualElement _spin;

        public SellLive(bool offer) { _offer = offer; }

        protected override void Build()
        {
            Scrim(0.55f);
            if (_offer) BuildOffer(); else BuildExits();
        }

        private void BuildOffer()
        {
            var gc = Ui.Gc;
            var offer = gc.Run.pendingOffer;
            if (offer == null || !offer.active) { RequestClose(); return; }
            var c = gc.Run.company;

            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(820, 22); Kit.Pad(card, 28, 34); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 34f));
            card.Add(Kit.Chip("Tempo pausado", Kit.Ink, true, Kit.IcPause));
            var logos = Kit.Row(); logos.style.marginTop = 16;
            logos.Add(Pop(Kit.SquareBadge(Kit.IcBriefcase, Kit.Red, 72, 20), 0.3f));
            var hs = Kit.Icon(Kit.IcHandshake, 30, Kit.Soft); hs.style.marginLeft = 22; hs.style.marginRight = 22;
            logos.Add(hs);
            logos.Add(Pop(Kit.SquareBadge(Kit.IcRocket, Kit.Teal, 72, 20), 0.45f));
            card.Add(logos);

            var title = Kit.Wrap(offer.rivalName + " quer comprar a " + c.name, 26, Kit.Ink, true); title.style.marginTop = 16;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(title);
            card.Add(Kit.Text("Proposta fechada — é aceitar ou recusar.", 14, Kit.Soft));
            var price = Kit.Text("R$ 0", 60, Kit.Cash, true); price.style.marginTop = 10;
            card.Add(price);
            CountUp(price, 0, offer.price, 0.6f, 1.3f, Kit.Money);
            card.Add(Kit.Text(offer.multiple.ToString("0.0").Replace('.', ',') + "× o seu valuation atual (" + Kit.Money(c.valuation) + ")", 13, Kit.Soft, true));

            int legacy = LegacyCalculator.Compute(gc.Run, false);
            var row = Kit.Row(Align.Stretch); row.style.marginTop = 22; row.style.alignSelf = Align.Stretch;
            row.Add(Outcome(Kit.IcCheck, Kit.Green, "Se aceitar", new[]
            {
                "A run termina aqui", Kit.Money(offer.price) + " vão para o seu bolso", "≈ " + legacy + " Pontos de Legado", "Você volta no tempo e recomeça"
            }, 1.4f));
            row.Add(Kit.Gap(14));
            row.Add(Outcome(Kit.IcClose, Kit.Red, "Se recusar", new[]
            {
                "A run continua", offer.rivalName + " fica mais hostil", "Pode não haver outra oferta assim"
            }, 1.5f));
            card.Add(row);

            var foot = Kit.Row(); foot.style.marginTop = 22;
            foot.Add(Kit.Ghost("Recusar", Kit.IcClose, () => { gc.DeclineOffer(); RequestClose(); }));
            foot.Add(Kit.Gap(14));
            foot.Add(Kit.Button("Vender e encerrar a run", Kit.IcHourglass, Kit.Cash, Color.white, () => gc.AcceptOffer()));
            card.Add(foot);
        }

        private VisualElement Outcome(string icon, Color c, string title, string[] lines, float delay)
        {
            var o = Kit.Tile(16); o.style.flexGrow = 1; o.style.flexBasis = 0; Kit.Pad(o, 14, 16);
            o.style.backgroundColor = Kit.A(c, 0.09f);
            var h = Kit.Row();
            h.Add(Kit.Badge(icon, c, 26));
            var t = Kit.Text(title, 15, Kit.Ink, true); t.style.marginLeft = 10;
            h.Add(t);
            o.Add(h);
            foreach (string l in lines)
            {
                var r = Kit.Row(); r.style.marginTop = 7;
                var dot = new VisualElement(); dot.style.width = 6; dot.style.height = 6; Kit.Radius(dot, 3); dot.style.backgroundColor = c;
                r.Add(dot);
                var lt = Kit.Text(l, 12, Kit.Ink); lt.style.marginLeft = 8;
                r.Add(lt);
                o.Add(r);
            }
            Enter(o, delay);
            return o;
        }

        private void BuildExits()
        {
            var gc = Ui.Gc;
            var run = gc.Run;
            long sale = gc.SaleValue;
            var body = Window("Encerrar a run", Kit.IcHourglass, Kit.Gold, 1040, 640, out _, "Vender é o único jeito de levar dinheiro para a próxima vida");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            row.Add(Enter(Exit(Kit.IcStore, Kit.Cash, "Vender a empresa", Kit.Money(sale), "pelo valuation de hoje",
                "Disponível a qualquer momento.", "Vender agora", false, () => gc.SellCompany())));
            row.Add(Kit.Gap(14));
            bool pending = run.pendingOffer != null && run.pendingOffer.active;
            row.Add(Enter(Exit(Kit.IcBriefcase, Kit.Purple, "Proposta de rival", pending ? Kit.Money(run.pendingOffer.price) : "Nenhuma",
                "rivais procuram você", "Chegam de surpresa e costumam pagar acima do valuation.", pending ? "Ver proposta" : null, false,
                () => Ui.Open(new SellLive(true)))));
            row.Add(Kit.Gap(14));
            bool ipo = gc.CanIpo;
            row.Add(Enter(Exit(Kit.IcChartLine, Kit.Blue, "IPO", ipo ? Kit.Money(gc.IpoValue) : "Bloqueado", "abrir capital na bolsa",
                ipo ? "Paga um prêmio sobre o valuation." : "Requer fase Estúdio e a pesquisa de IPO.", ipo ? "Abrir capital" : null, !ipo,
                () => gc.DoIpo())));

            int legacy = LegacyCalculator.Compute(run, false);
            if (gc.SaleIsLoss)
            {
                var warn = Kit.Row(Align.FlexStart); warn.style.marginTop = 16; warn.style.backgroundColor = Kit.A(Kit.Red, 0.12f);
                Kit.Radius(warn, 14); Kit.Pad(warn, 14, 18);
                var face = Kit.Badge(Kit.IcFrown, Kit.Red, 44);
                _spin = face;
                warn.Add(face);
                var wt = Kit.Col(); wt.style.marginLeft = 14; wt.style.flexShrink = 1;
                wt.Add(Kit.Text("Calma aí, isso é prejuízo!", 17, Kit.Ink, true));
                wt.Add(Kit.Wrap("Você colocou " + Kit.Money(run.capitalInvested) + " nessa empresa e sairia com " + Kit.Money(sale) + ". Até a máquina do tempo ficou com pena. Tem certeza?", 13, Kit.Soft));
                warn.Add(wt);
                body.Add(Enter(warn, 0.7f));
            }
            else
            {
                var info = LiveBits.Note(Kit.IcHourglass, Kit.Gold, "Vendendo agora você também leva cerca de " + legacy + " Pontos de Legado para gastar na árvore de habilidades.");
                info.style.marginTop = 16;
                body.Add(Enter(info, 0.7f));
            }
        }

        private static VisualElement Exit(string icon, Color c, string title, string value, string caption, string note, string action, bool locked, System.Action onClick)
        {
            var e = Kit.Outline(18); e.style.flexGrow = 1; e.style.flexBasis = 0; Kit.Pad(e, 20, 18); e.style.alignItems = Align.Center;
            if (locked) e.style.opacity = 0.7f;
            e.Add(Kit.SquareBadge(locked ? Kit.IcLock : icon, locked ? Kit.Faint : c, 64, 18));
            var t = Kit.Text(title, 18, Kit.Ink, true); t.style.marginTop = 12;
            e.Add(t);
            e.Add(Kit.Text(caption, 11, Kit.Soft));
            var v = Kit.Text(value, 28, locked ? Kit.Faint : c, true); v.style.marginTop = 10;
            e.Add(v);
            var n = Kit.Wrap(note, 12, Kit.Soft); n.style.unityTextAlign = TextAnchor.MiddleCenter; n.style.marginTop = 8;
            e.Add(n);
            e.Add(Kit.Spacer());
            if (action != null)
            {
                var b = Kit.Button(action, null, c, Color.white, onClick, 38); b.style.alignSelf = Align.Stretch; b.style.marginTop = 12;
                e.Add(b);
            }
            return e;
        }

        protected override void OnTick(float dt)
        {
            if (_spin != null) _spin.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 5f) * 10f));
        }
    }

    /// <summary>M&amp;A vivo (GDD 12): proposta com chance de aceite e, se der certo, a absorção da equipe.</summary>
    internal sealed class AcquisitionLive : GalleryScreen
    {
        private readonly RivalState _rival;
        private readonly List<EmployeeInstance> _keep = new List<EmployeeInstance>();
        private VisualElement _gauge, _send;
        private Label _offerLabel, _multLabel, _chanceLabel;
        private float _mult = 2f, _shownChance;
        private bool _absorbing, _refused;

        public AcquisitionLive(RivalState rival) { _rival = rival; }

        protected override void Build()
        {
            Scrim();
            _gauge = null;
            if (_absorbing) BuildAbsorb();
            else if (_refused) BuildRefused();
            else BuildOffer();
        }

        private void BuildOffer()
        {
            var gc = Ui.Gc;
            var body = Window("Comprar rival", Kit.IcBriefcase, Kit.Purple, 1080, 640, out _, "Fusões e aquisições");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            var rival = Kit.Tile(18); rival.style.width = 300; rival.style.flexShrink = 0; Kit.Pad(rival, 18, 18);
            var rt = Kit.Row();
            rt.Add(Kit.SquareBadge(Kit.IcStar, Kit.Purple, 56));
            var rn = Kit.Col(); rn.style.marginLeft = 12; rn.style.flexShrink = 1;
            rn.Add(Kit.Wrap(_rival.name, 17, Kit.Ink, true));
            rn.Add(Kit.Text("Fase " + Labels.Phase(_rival.phase), 11, Kit.Soft));
            rt.Add(rn);
            rival.Add(rt);
            rival.Add(Kit.Divider(12));
            rival.Add(Kit.Text("Valuation estimado", 11, Kit.Soft, true));
            rival.Add(Kit.Text(Kit.Money(_rival.valuation), 24, Kit.Ink, true));
            rival.Add(Kit.Divider(12));
            rival.Add(Kit.Caps("O que você leva", 10, Kit.Soft));
            rival.Add(Take(_rival.gamesReleased + " jogo(s) no catálogo — entram no seu valuation", true));
            rival.Add(Take(_rival.employeeCount + " funcionário(s) — você escolhe quem fica", true));
            rival.Add(Take("Engines e tecnologias ficam para trás", false));
            row.Add(Enter(rival));

            row.Add(Kit.Gap(22));
            var mid = Kit.Col(); mid.style.flexGrow = 1;
            row.Add(mid);
            mid.Add(Kit.Caps("Sua proposta", 11, Kit.Soft));
            var or = Kit.Row(Align.FlexEnd); or.style.marginTop = 4;
            _offerLabel = Kit.Text("", 40, Kit.Cash, true);
            or.Add(_offerLabel);
            _multLabel = Kit.Text("", 16, Kit.Soft, true); _multLabel.style.marginLeft = 12; _multLabel.style.marginBottom = 7;
            or.Add(_multLabel);
            mid.Add(Enter(or));
            var sl = Widgets.Slider((_mult - 1f) / 2f, Kit.Purple, v => { _mult = 1f + v * 2f; Refresh(); });
            sl.style.flexGrow = 0; sl.style.marginTop = 8;
            mid.Add(Enter(sl));
            var ticks = Kit.Row(); ticks.style.justifyContent = Justify.SpaceBetween; ticks.style.marginTop = 2;
            foreach (string s in new[] { "1×", "1,5×", "2×", "2,5×", "3×" }) ticks.Add(Kit.Text(s, 10, Kit.Faint, true));
            mid.Add(ticks);

            var mods = Kit.Tile(16); mods.style.marginTop = 18; Kit.Pad(mods, 14, 16);
            mods.Add(Kit.Text("O que pesa na decisão deles", 13, Kit.Ink, true));
            mods.Add(Mod("Rival em crise financeira", "aceita por ~20% menos", _rival.inCrisis));
            mods.Add(Mod("Seu Networking", "cada nível facilita 5%", FounderPerks.AcquisitionEase > 0f));
            mods.Add(Mod("Fase do rival: " + Labels.Phase(_rival.phase), "menores aceitam múltiplos menores", (int)_rival.phase <= (int)CompanyPhase.Indie));
            mods.Add(Mod("Relação hostil", "reduz a chance de aceite", _rival.relation01 < 0.5f));
            mid.Add(Enter(mods));
            mid.Add(Kit.Spacer());
            var note = Kit.Text("Se recusarem, você só pode tentar de novo depois de algumas semanas.", 11, Kit.Soft);
            mid.Add(Enter(note));

            row.Add(Kit.Gap(22));
            var right = Kit.Col(); right.style.width = 230; right.style.flexShrink = 0; right.style.alignItems = Align.Center;
            row.Add(right);
            right.Add(Kit.Caps("Chance de aceite", 11, Kit.Soft));
            var gw = new VisualElement(); gw.style.width = 210; gw.style.height = 210; gw.style.marginTop = 10;
            gw.style.alignItems = Align.Center; gw.style.justifyContent = Justify.Center;
            _gauge = Kit.Fill(); _gauge.generateVisualContent += DrawGauge;
            gw.Add(_gauge);
            _chanceLabel = Kit.Text("0%", 46, Kit.Ink, true);
            gw.Add(_chanceLabel);
            right.Add(Enter(gw));
            right.Add(Kit.Spacer());
            _send = Kit.Button("Enviar proposta", Kit.IcPaperPlane, Kit.Purple, Color.white, Send); _send.style.alignSelf = Align.Stretch;
            right.Add(Enter(_send));
            var cancel = Kit.Ghost("Cancelar", null, RequestClose); cancel.style.alignSelf = Align.Stretch; cancel.style.marginTop = 8;
            right.Add(Enter(cancel));
            Refresh();
        }

        private void Refresh()
        {
            var gc = Ui.Gc;
            long price = gc.Rivals.PriceFor(_rival, _mult);
            bool canPay = gc.Run.company.cash >= price;
            _offerLabel.text = Kit.Money(price);
            _offerLabel.style.color = canPay ? Kit.Cash : Kit.Red;
            _multLabel.text = _mult.ToString("0.0").Replace('.', ',') + "×" + (canPay ? "" : "  — falta caixa");
            LiveBits.SetEnabled(_send, canPay);
        }

        private void Send()
        {
            var rivals = Ui.Gc.Rivals;
            if (rivals.TryAcquire(_rival, _mult))
            {
                _absorbing = true;
                _keep.Clear();
                Sfx.Play(SfxId.Fanfare);
            }
            else
            {
                _refused = true;
                Sfx.Play(SfxId.Fail);
            }
            Rebuild();
        }

        private void BuildRefused()
        {
            var body = Window("Proposta recusada", Kit.IcClose, Kit.Red, 620, 320, out _, _rival.name);
            body.Add(Kit.Wrap(_rival.name + " recusou a oferta. Eles só voltam a conversar daqui a algumas semanas — e ficaram um pouco mais desconfiados.", 15, Kit.Soft));
            body.Add(Kit.Spacer());
            var foot = Footer(body);
            foot.Add(Kit.Confirm("Entendi", null, RequestClose));
        }

        private void BuildAbsorb()
        {
            var gc = Ui.Gc;
            var pool = gc.Rivals.AbsorbPool;
            int free = gc.Staff.MaxTeam - gc.Run.company.EffectiveTeamSize;
            var body = Window("Aquisição concluída", Kit.IcHandshake, Kit.Green, 980, 620, out _, _rival.name + " agora é sua. Quem fica?");
            var lh = Kit.Row();
            lh.Add(Kit.Caps("Funcionários de " + _rival.name, 11, Kit.Soft)); lh.Add(Kit.Spacer());
            lh.Add(Kit.Chip("Vagas livres: " + Mathf.Max(0, free - _keep.Count) + " de " + Mathf.Max(0, free), free - _keep.Count > 0 ? Kit.Green : Kit.Red, false, Kit.IcUsers));
            body.Add(lh);
            long fees = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var e = pool[i];
                bool keep = _keep.Contains(e);
                if (!keep) fees += gc.Rivals.DismissalFee(e);
                var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 8; Kit.Pad(r, 10, 14);
                r.Add(Kit.Avatar(i + 1, 46));
                var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1;
                t.Add(Kit.Text(e.name, 14, Kit.Ink, true));
                t.Add(Kit.Text(Labels.SpecialtyName(e.specialty) + " · " + Labels.SeniorityName(e.seniority), 11, Kit.Soft));
                r.Add(t);
                var s = Kit.Col(); s.style.alignItems = Align.FlexEnd; s.style.marginRight = 14;
                s.Add(Kit.Text(Kit.Money(e.weeklySalary) + " / sem", 12, Kit.Ink, true));
                s.Add(Kit.Text("Demitir custa " + Kit.Money(gc.Rivals.DismissalFee(e)), 10, Kit.Red));
                r.Add(s);
                var kl = Kit.Text(keep ? "Fica" : "Sai", 12, keep ? Kit.Teal : Kit.Soft, true); kl.style.width = 34;
                r.Add(kl);
                bool canKeep = keep || _keep.Count < free;
                var toggle = Widgets.Toggle(keep, v =>
                {
                    if (v) { if (!_keep.Contains(e)) _keep.Add(e); } else _keep.Remove(e);
                    Rebuild();
                });
                LiveBits.SetEnabled(toggle, canKeep);
                r.Add(toggle);
                body.Add(r);
            }
            body.Add(Kit.Spacer());
            var foot = Footer(body);
            foot.Add(Kit.Text("Taxas de demissão: " + Kit.Money(fees), 13, fees > 0 ? Kit.Red : Kit.Soft, true));
            foot.Add(Kit.Gap(18));
            foot.Add(Kit.Confirm("Concluir integração", Kit.IcCheck, () =>
            {
                gc.Rivals.FinishAbsorption(_keep);
                RequestClose();
            }));
        }

        private static VisualElement Take(string text, bool good)
        {
            var r = Kit.Row(Align.FlexStart); r.style.marginTop = 9;
            r.Add(Kit.Badge(good ? Kit.IcCheck : Kit.IcClose, Kit.A(good ? Kit.Green : Kit.Red, 0.18f), 22, good ? Kit.Green : Kit.Red));
            var t = Kit.Wrap(text, 12, Kit.Ink); t.style.marginLeft = 9; t.style.flexShrink = 1;
            r.Add(t);
            return r;
        }

        private static VisualElement Mod(string name, string detail, bool active)
        {
            var r = Kit.Row(); r.style.marginTop = 9; r.style.opacity = active ? 1f : 0.55f;
            var t = Kit.Col(); t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 12, Kit.Ink, true));
            t.Add(Kit.Text(detail, 10, Kit.Soft));
            r.Add(t);
            r.Add(Kit.Chip(active ? "ativo" : "inativo", active ? Kit.Green : Kit.Soft));
            return r;
        }

        protected override void OnTick(float dt)
        {
            if (_gauge == null) return;
            float target = Clock < 0.4f ? 0f : Ui.Gc.Rivals.AcceptChance(_rival, _mult);
            _shownChance = Mathf.Lerp(_shownChance, target, 1f - Mathf.Exp(-7f * dt));
            _chanceLabel.text = Mathf.RoundToInt(_shownChance * 100f) + "%";
            _gauge.MarkDirtyRepaint();
        }

        private void DrawGauge(MeshGenerationContext mgc)
        {
            var r = _gauge.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center; float rad = r.width * 0.5f - 12f;
            Color col = _shownChance < 0.3f ? Kit.Red : _shownChance < 0.6f ? Kit.Gold : Kit.Green;
            p.lineCap = LineCap.Round; p.lineWidth = 16f; p.strokeColor = Kit.Track;
            p.BeginPath(); p.Arc(c, rad, new Angle(0f), new Angle(360f)); p.Stroke();
            if (_shownChance <= 0.005f) return;
            p.strokeColor = col;
            p.BeginPath(); p.Arc(c, rad, new Angle(-90f), new Angle(-90f + 360f * _shownChance)); p.Stroke();
        }
    }

    /// <summary>Menu de pausa (GDD 20.6): salvar, volume, voltar ao menu e sair.</summary>
    internal sealed class PauseLive : GalleryScreen
    {
        private VisualElement _icon;

        protected override void Build()
        {
            Scrim(0.6f);
            var gc = Ui.Gc;
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(440, 22); Kit.Pad(card, 26, 28); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Stretch;
            center.Add(Enter(card, 0.05f, 30f));

            var head = Kit.Col(); head.style.alignItems = Align.Center; head.style.marginBottom = 16;
            _icon = Kit.Badge(Kit.IcPause, Kit.Teal, 60);
            head.Add(_icon);
            var t = Kit.Text("Jogo pausado", 26, Kit.Ink, true); t.style.marginTop = 10;
            head.Add(t);
            head.Add(Kit.Text(gc.Run.company.name + " · Semana " + gc.Time.CurrentWeek + ", Ano " + gc.Time.CurrentYear, 12, Kit.Soft));
            card.Add(head);

            ResetCascade(0.2f);
            card.Add(Item(Kit.Confirm("Continuar", Kit.IcPlay, RequestClose)));
            card.Add(Item(Kit.Ghost("Salvar jogo", Kit.IcSave, () =>
            {
                gc.AutoSave();
                Ui.Notify(Kit.IcSave, Kit.Teal, "Jogo salvo", null, 3f);
                Sfx.Play(SfxId.Success);
            })));

            var vol = Kit.Tile(12); vol.style.marginBottom = 8; Kit.Pad(vol, 8, 14);
            var vr = Kit.Row();
            vr.Add(Kit.Icon(Kit.IcVolume, 14, Kit.Soft));
            var vl = Kit.Text("Volume dos efeitos", 12, Kit.Ink, true); vl.style.marginLeft = 8; vl.style.width = 130;
            vr.Add(vl);
            vr.Add(Widgets.Slider(Sfx.Volume, Kit.Teal, v => Sfx.Volume = v));
            vol.Add(vr);
            card.Add(Enter(vol));

            if (Tuning.TuningScreen.IsAvailable)
                card.Add(Item(Kit.Ghost("Parâmetros do jogo (dev)", Kit.IcSliders, Tuning.TuningScreen.Open)));
            card.Add(Item(Kit.Ghost("Encerrar a run…", Kit.IcHourglass, Ui.OpenSellOptions)));
            card.Add(Item(Kit.Ghost("Menu principal", Kit.IcHouse, Ui.GoToMainMenu)));
            card.Add(Item(Kit.Button("Sair do jogo", Kit.IcPower, Kit.A(Kit.Red, 0.14f), Kit.Red, Ui.QuitGame)));

            var foot = Kit.Row(); foot.style.justifyContent = Justify.Center; foot.style.marginTop = 12;
            foot.Add(Kit.Icon(Kit.IcSave, 10, Kit.Faint));
            var ft = Kit.Text("O jogo salva sozinho a cada lançamento e a cada 13 semanas.", 11, Kit.Faint); ft.style.marginLeft = 6;
            foot.Add(ft);
            card.Add(foot);
        }

        private VisualElement Item(VisualElement button)
        {
            button.style.marginBottom = 8;
            return Enter(button);
        }

        protected override void OnTick(float dt)
        {
            float s = 1f + 0.06f * Mathf.Sin(Clock * 4f);
            _icon.style.scale = new Scale(new Vector2(s, s));
        }
    }
}
