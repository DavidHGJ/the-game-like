using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Evento narrativo (GDD 11): pausa o tempo, texto caricato, 2–4 opções, algumas exigem habilidade.</summary>
    internal sealed class NarrativeEventScreen : GalleryScreen
    {
        private const string Body = "Um jornalista famoso te pergunta ao vivo, com a câmera ligada e um sorrisinho no rosto: " +
                                    "“O seu próximo jogo vai mesmo competir com os gigantes do mercado?”";
        private Label _text, _bigIcon;
        private VisualElement _pauseChip;

        public override float SettleSeconds => 3.4f;

        protected override void Build()
        {
            Scrim(0.55f);
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(820, 22); Kit.Pad(card, 0, 0); card.style.borderBottomWidth = 6; card.style.overflow = Overflow.Hidden;
            center.Add(Enter(card, 0.05f, 34f));

            var band = new VisualElement(); band.style.height = 150; band.style.backgroundColor = Kit.Purple;
            band.style.alignItems = Align.Center; band.style.justifyContent = Justify.Center;
            for (int i = 0; i < 5; i++)
            {
                var ring = new VisualElement(); ring.style.position = Position.Absolute;
                float sz = 120 + i * 90; ring.style.width = sz; ring.style.height = sz; Kit.Radius(ring, sz / 2f);
                Kit.Border(ring, Kit.A(Color.white, 0.10f), 2);
                band.Add(ring);
            }
            var disc = Kit.Badge(Kit.IcMic, Color.white, 92, Kit.Purple);
            _bigIcon = (Label)disc[0];
            band.Add(disc);
            _pauseChip = Kit.Chip("Tempo pausado", Kit.Ink, true, Kit.IcPause);
            _pauseChip.style.position = Position.Absolute; _pauseChip.style.top = 14; _pauseChip.style.right = 16;
            band.Add(_pauseChip);
            card.Add(band);

            var body = Kit.Col(); Kit.Pad(body, 22, 30);
            card.Add(body);
            var cat = Kit.Chip("Declarações públicas", Kit.Purple); cat.style.alignSelf = Align.FlexStart;
            body.Add(cat);
            var title = Kit.Text("A Grande Entrevista", 28, Kit.Ink, true); title.style.marginTop = 8;
            body.Add(title);
            _text = Kit.Wrap("", 15, Kit.Soft); _text.style.marginTop = 8; _text.style.minHeight = 66;
            body.Add(_text);

            body.Add(Option(0, "“Com certeza, vamos dominar o mercado!”", null, 1.6f));
            body.Add(Option(1, "“Estamos focados em fazer algo especial.”", null, 1.72f));
            body.Add(Option(2, "“Sem comentários.”", null, 1.84f));
            body.Add(Option(3, "“Deixa o jogo falar por si mesmo — mas vai surpreender.”", "Carisma", 1.96f));

            var foot = Kit.Row(); foot.style.marginTop = 14; foot.style.justifyContent = Justify.Center;
            foot.Add(Kit.Icon(Kit.IcQuestion, 11, Kit.Faint));
            var ft = Kit.Text("Você nunca sabe exatamente o que vai acontecer.", 11, Kit.Faint); ft.style.marginLeft = 6;
            foot.Add(ft);
            body.Add(foot);
        }

        private VisualElement Option(int index, string text, string skill, float delay)
        {
            var o = Kit.Outline(14); o.style.flexDirection = FlexDirection.Row; o.style.alignItems = Align.Center;
            o.style.marginTop = 9; Kit.Pad(o, 12, 16);
            Kit.Smooth(o);
            Color accent = skill != null ? Kit.Teal : Kit.Purple;
            if (skill != null) { Kit.BorderColor(o, Kit.Teal); o.style.backgroundColor = Kit.A(Kit.Teal, 0.07f); }
            var letter = Kit.Text(((char)('A' + index)).ToString(), 14, Color.white, true);
            letter.style.width = 30; letter.style.height = 30; Kit.Radius(letter, 15); letter.style.backgroundColor = accent;
            letter.style.unityTextAlign = TextAnchor.MiddleCenter; letter.style.flexShrink = 0;
            o.Add(letter);
            var t = Kit.Wrap(text, 15, Kit.Ink, true); t.style.marginLeft = 14; t.style.flexGrow = 1; t.style.flexShrink = 1;
            o.Add(t);
            if (skill != null) o.Add(Kit.Chip(skill, Kit.Teal, true, Kit.IcCheck));
            o.RegisterCallback<PointerEnterEvent>(_ => { o.style.translate = new Translate(8, 0, 0); Kit.BorderColor(o, accent); });
            o.RegisterCallback<PointerLeaveEvent>(_ => { o.style.translate = new Translate(0, 0, 0); Kit.BorderColor(o, skill != null ? Kit.Teal : Kit.Track); });
            Animate(delay, 0.4f, k =>
            {
                o.style.opacity = Mathf.Clamp01(k * 1.5f);
                o.style.translate = new Translate(-30f * (1f - k), 0, 0);
            });
            return o;
        }

        protected override void OnTick(float dt)
        {
            int n = Mathf.Clamp(Mathf.FloorToInt((Clock - 0.5f) * 110f), 0, Body.Length);
            if (_text.text.Length != n) _text.text = Body.Substring(0, n);
            _bigIcon.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 3f) * 8f));
            _pauseChip.style.opacity = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Clock * 2.5f));
        }
    }

    /// <summary>Oferta de expansão de fase (GDD 2.1): o valuation bateu o limite, jogador decide quando expandir.</summary>
    internal sealed class PhaseExpansionScreen : GalleryScreen
    {
        private VisualElement _arrow, _rays;

        protected override void Build()
        {
            Scrim(0.5f);
            _rays = Kit.Fill(); _rays.pickingMode = PickingMode.Ignore;
            _rays.generateVisualContent += DrawRays;
            Root.Add(_rays);

            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(900, 22); Kit.Pad(card, 28, 34); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 34f));

            card.Add(Kit.Caps("Valuation acima de R$ 250.000", 12, Kit.Teal, 2.5f));
            var title = Kit.Text("Hora de crescer!", 38, Kit.Ink, true); title.style.marginTop = 4;
            card.Add(title);
            card.Add(Kit.Text("A garagem ficou pequena para a Pixel Forja.", 15, Kit.Soft));

            var row = Kit.Row(); row.style.marginTop = 22;
            row.Add(Pop(Office(Kit.IcCar, "Garagem", "Só o fundador", Kit.Soft, false), 0.4f));
            _arrow = Kit.Icon(Kit.IcArrowRight, 34, Kit.Teal); _arrow.style.marginLeft = 26; _arrow.style.marginRight = 26;
            row.Add(_arrow);
            row.Add(Pop(Office(Kit.IcMug, "Indie", "Coworking pequeno", Kit.Teal, true), 0.6f));
            card.Add(row);

            var unlocks = Kit.Row(Align.Stretch); unlocks.style.marginTop = 24; unlocks.style.alignSelf = Align.Stretch;
            unlocks.Add(Unlock(Kit.IcUsers, "Equipe", "2 funcionários + fundador", 0.9f));
            unlocks.Add(Unlock(Kit.IcExpand, "Projetos", "Escopo Médio liberado", 1.0f));
            unlocks.Add(Unlock(Kit.IcFlask, "P&D", "Categorias expandidas", 1.1f));
            unlocks.Add(Unlock(Kit.IcGamepad, "Plataformas", "Consoles pesquisáveis", 1.2f));
            card.Add(unlocks);

            var warn = Kit.Row(); warn.style.marginTop = 18; warn.style.backgroundColor = Kit.A(Kit.Gold, 0.18f);
            Kit.Radius(warn, 12); Kit.Pad(warn, 10, 16); warn.style.alignSelf = Align.Stretch;
            warn.Add(Kit.Icon(Kit.IcWarn, 14, Kit.Gold));
            var wt = Kit.Wrap("Você escolhe quando. Mas ficar parado tem custo: o valuation começa a cair por estagnação.", 12, Kit.Ink);
            wt.style.marginLeft = 10; wt.style.flexShrink = 1;
            warn.Add(wt);
            card.Add(Enter(warn, 1.3f));

            var foot = Kit.Row(); foot.style.marginTop = 20; foot.style.alignSelf = Align.Stretch;
            var cost = Kit.Col();
            cost.Add(Kit.Text("Custo da mudança", 11, Kit.Soft, true));
            var cv = Kit.Text("R$ 0", 26, Kit.Ink, true);
            cost.Add(cv);
            CountUp(cv, 0, 40000, 1.0f, 0.8f, Kit.Money);
            foot.Add(cost);
            foot.Add(Kit.Spacer());
            foot.Add(Kit.Ghost("Agora não"));
            foot.Add(Kit.Gap(12));
            foot.Add(Kit.Confirm("Expandir empresa", Kit.IcRocket));
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

        private VisualElement Unlock(string icon, string title, string text, float delay)
        {
            var u = Kit.Outline(14); u.style.flexGrow = 1; u.style.flexBasis = 0; u.style.marginLeft = 5; u.style.marginRight = 5;
            u.style.alignItems = Align.Center; Kit.Pad(u, 12, 8);
            u.Add(Kit.Badge(icon, Kit.A(Kit.Teal, 0.16f), 36, Kit.Teal));
            var t = Kit.Text(title, 13, Kit.Ink, true); t.style.marginTop = 6;
            u.Add(t);
            var d = Kit.Wrap(text, 11, Kit.Soft); d.style.unityTextAlign = TextAnchor.MiddleCenter;
            u.Add(d);
            Enter(u, delay);
            return u;
        }

        protected override void OnTick(float dt)
        {
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

    /// <summary>M&A — comprar um rival (GDD 12): proposta com chance de aceite e absorção de IPs/funcionários.</summary>
    internal sealed class AcquisitionScreen : GalleryScreen
    {
        private const float RivalValuation = 205000f;
        private VisualElement _gauge;
        private Label _offerLabel, _multLabel, _chanceLabel;
        private float _mult = 2f, _shownChance;
        private int _state;


        public override void SetState(int index)
        {
            _state = index;
            Root.Clear();
            Build();
        }

        protected override void Build()
        {
            Scrim();
            if (_state == 0) BuildOffer(); else BuildAbsorb();
        }

        /// <summary>Curva do GDD 12.3: 1x≈5%, 1,5x≈25%, 2x≈55%, 3x≈85%.</summary>
        private static float Chance(float mult)
        {
            if (mult <= 1f) return 0.05f;
            if (mult <= 1.5f) return Mathf.Lerp(0.05f, 0.25f, (mult - 1f) / 0.5f);
            if (mult <= 2f) return Mathf.Lerp(0.25f, 0.55f, (mult - 1.5f) / 0.5f);
            return Mathf.Lerp(0.55f, 0.85f, Mathf.Clamp01(mult - 2f));
        }

        private void BuildOffer()
        {
            var body = Window("Comprar rival", Kit.IcBriefcase, Kit.Purple, 1080, 660, out _, "Fusões e aquisições");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            var rival = Kit.Tile(18); rival.style.width = 300; rival.style.flexShrink = 0; Kit.Pad(rival, 18, 18);
            var rt = Kit.Row();
            rt.Add(Kit.SquareBadge(Kit.IcStar, Kit.Purple, 56));
            var rn = Kit.Col(); rn.style.marginLeft = 12;
            rn.Add(Kit.Text("Lua Nova Games", 17, Kit.Ink, true));
            rn.Add(Kit.Text("Fase Indie · relação neutra", 11, Kit.Soft));
            rt.Add(rn);
            rival.Add(rt);
            rival.Add(Kit.Divider(12));
            rival.Add(Kit.Text("Valuation estimado", 11, Kit.Soft, true));
            rival.Add(Kit.Text(Kit.Money(RivalValuation), 24, Kit.Ink, true));
            rival.Add(Kit.Divider(12));
            rival.Add(Kit.Caps("O que você leva", 10, Kit.Soft));
            rival.Add(Take(Kit.IcGamepad, "3 jogos no catálogo", true));
            rival.Add(Take(Kit.IcCrown, "Franquia Peças do Céu (média 7,1)", true));
            rival.Add(Take(Kit.IcUsers, "2 funcionários — você escolhe quem fica", true));
            rival.Add(Take(Kit.IcChip, "Engines e tecnologias ficam para trás", false));
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
            var sl = Widgets.Slider(0.5f, Kit.Purple, v => { _mult = 1f + v * 2f; Refresh(); });
            sl.style.flexGrow = 0; sl.style.marginTop = 8;
            mid.Add(Enter(sl));
            var ticks = Kit.Row(); ticks.style.justifyContent = Justify.SpaceBetween; ticks.style.marginTop = 2;
            foreach (string s in new[] { "1×", "1,5×", "2×", "2,5×", "3×" }) ticks.Add(Kit.Text(s, 10, Kit.Faint, true));
            mid.Add(ticks);

            var mods = Kit.Tile(16); mods.style.marginTop = 18; Kit.Pad(mods, 14, 16);
            mods.Add(Kit.Text("O que pesa na decisão deles", 13, Kit.Ink, true));
            mods.Add(Mod(Kit.IcCoins, "Rival em crise financeira", "aceita por ~20% menos", false));
            mods.Add(Mod(Kit.IcNetwork, "Seu Networking", "nível 1 — sem bônus ainda", false));
            mods.Add(Mod(Kit.IcBuilding, "Fase do rival: Indie", "aceita múltiplos menores que um Estúdio", true));
            mods.Add(Mod(Kit.IcSkull, "Histórico hostil", "nenhuma sabotagem ou poaching", true));
            mid.Add(Enter(mods));
            mid.Add(Kit.Spacer());
            var note = Kit.Row();
            note.Add(Kit.Icon(Kit.IcHourglass, 11, Kit.Soft));
            var nt = Kit.Text("Se recusarem, você só pode tentar de novo depois de um tempo.", 11, Kit.Soft); nt.style.marginLeft = 6;
            note.Add(nt);
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
            var send = Kit.Button("Enviar proposta", Kit.IcPaperPlane, Kit.Purple, Color.white); send.style.alignSelf = Align.Stretch;
            right.Add(Enter(send));
            var cancel = Kit.Ghost("Cancelar"); cancel.style.alignSelf = Align.Stretch; cancel.style.marginTop = 8;
            right.Add(Enter(cancel));
            Refresh();
        }

        private void Refresh()
        {
            _offerLabel.text = Kit.Money(Mathf.Round(RivalValuation * _mult / 1000f) * 1000f);
            _multLabel.text = _mult.ToString("0.0").Replace('.', ',') + "× o valuation";
        }

        private static VisualElement Take(string icon, string text, bool good)
        {
            var r = Kit.Row(Align.FlexStart); r.style.marginTop = 9;
            r.Add(Kit.Badge(good ? Kit.IcCheck : Kit.IcClose, Kit.A(good ? Kit.Green : Kit.Red, 0.18f), 22, good ? Kit.Green : Kit.Red));
            var t = Kit.Wrap(text, 12, Kit.Ink); t.style.marginLeft = 9; t.style.flexShrink = 1;
            r.Add(t);
            return r;
        }

        private static VisualElement Mod(string icon, string name, string detail, bool active)
        {
            var r = Kit.Row(); r.style.marginTop = 9; r.style.opacity = active ? 1f : 0.55f;
            r.Add(Kit.Badge(icon, Kit.A(Kit.Purple, 0.16f), 28, Kit.Purple));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 12, Kit.Ink, true));
            t.Add(Kit.Text(detail, 10, Kit.Soft));
            r.Add(t);
            r.Add(Kit.Chip(active ? "ativo" : "inativo", active ? Kit.Green : Kit.Soft));
            return r;
        }

        private void BuildAbsorb()
        {
            var body = Window("Aquisição concluída", Kit.IcHandshake, Kit.Green, 1000, 640, out _, "Lua Nova Games agora é sua. Quem fica?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);
            var lh = Kit.Row();
            lh.Add(Kit.Caps("Funcionários da Lua Nova", 11, Kit.Soft)); lh.Add(Kit.Spacer());
            lh.Add(Kit.Chip("Equipe: 4 de 5 vagas", Kit.Orange, false, Kit.IcUsers));
            left.Add(lh);
            left.Add(Enter(Person("Íris Campos", "Design · Sênior", "R$ 6.000 / sem", "Demitir custa R$ 12.000", 1, true)));
            left.Add(Enter(Person("Téo Bastos", "Programação · Pleno", "R$ 3.500 / sem", "Demitir custa R$ 7.000", 5, true)));
            left.Add(Enter(Person("Ravi Souto", "Programação · Júnior", "R$ 2.000 / sem", "Demitir custa R$ 4.000", 2, false)));
            var ips = Kit.Caps("IPs absorvidos", 11, Kit.Soft); ips.style.marginTop = 16;
            left.Add(ips);
            var ir = Kit.Row(); ir.style.marginTop = 8;
            foreach (string g in new[] { "Peças do Céu", "Peças do Céu 2", "Astro Gato" })
            {
                var chip = Kit.Chip(g, Kit.Blue, false, Kit.IcGamepad); chip.style.marginRight = 8;
                ir.Add(chip);
            }
            left.Add(Enter(ir));

            row.Add(Kit.Gap(22));
            var right = Kit.Tile(18); right.style.width = 320; right.style.flexShrink = 0; Kit.Pad(right, 18, 18);
            right.Add(Kit.Caps("Conta final", 11, Kit.Soft));
            right.Add(Bill("Preço da aquisição", "R$ 410.000", Kit.Ink));
            right.Add(Bill("Taxas de demissão (1)", "R$ 4.000", Kit.Red));
            right.Add(Kit.Divider(10));
            right.Add(Kit.Text("Custo total", 11, Kit.Soft, true));
            var total = Kit.Text("R$ 0", 30, Kit.Ink, true);
            right.Add(total);
            CountUp(total, 0, 414000, 0.6f, 0.9f, Kit.Money);
            right.Add(Kit.Divider(10));
            right.Add(Bill("Valuation com os IPs", "+ R$ 96.000", Kit.Green));
            right.Add(Bill("Nova folha semanal", "+ R$ 9.500", Kit.Orange));
            right.Add(Kit.Spacer());
            right.Add(Kit.Confirm("Concluir integração", Kit.IcCheck));
            row.Add(Enter(right, 0.4f));
        }

        private static VisualElement Person(string name, string role, string salary, string fee, int tone, bool keep)
        {
            var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 8; Kit.Pad(r, 10, 14);
            r.Add(Kit.Avatar(tone, 46));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 14, Kit.Ink, true));
            t.Add(Kit.Text(role, 11, Kit.Soft));
            r.Add(t);
            var s = Kit.Col(); s.style.alignItems = Align.FlexEnd; s.style.marginRight = 14;
            s.Add(Kit.Text(salary, 12, Kit.Ink, true));
            s.Add(Kit.Text(fee, 10, Kit.Red));
            r.Add(s);
            var kl = Kit.Text(keep ? "Fica" : "Sai", 12, keep ? Kit.Teal : Kit.Soft, true); kl.style.width = 34;
            r.Add(kl);
            r.Add(Widgets.Toggle(keep, v => { kl.text = v ? "Fica" : "Sai"; kl.style.color = v ? Kit.Teal : Kit.Soft; }));
            return r;
        }

        private static VisualElement Bill(string label, string value, Color c)
        {
            var r = Kit.Row(); r.style.marginTop = 8;
            r.Add(Kit.Text(label, 12, Kit.Soft)); r.Add(Kit.Spacer());
            r.Add(Kit.Text(value, 13, c, true));
            return r;
        }

        protected override void OnTick(float dt)
        {
            if (_state != 0 || _gauge == null) return;
            float target = Clock < 0.4f ? 0f : Chance(_mult);
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
            if (_shownChance > 0.005f)
            {
                p.strokeColor = col;
                p.BeginPath(); p.Arc(c, rad, new Angle(-90f), new Angle(-90f + 360f * _shownChance)); p.Stroke();
            }
        }
    }

    /// <summary>Encerrar a run (GDD 2.4, 12.1): oferta de um rival, venda voluntária (com aviso de prejuízo) e IPO.</summary>
    internal sealed class SellCompanyScreen : GalleryScreen
    {
        private int _state;
        private VisualElement _spin;


        public override void SetState(int index)
        {
            _state = index;
            Root.Clear();
            Build();
        }

        protected override void Build()
        {
            Scrim(0.55f);
            _spin = null;
            if (_state == 0) BuildOffer(); else BuildExits();
        }

        private void BuildOffer()
        {
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(820, 22); Kit.Pad(card, 28, 34); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 34f));

            var chip = Kit.Chip("Tempo pausado", Kit.Ink, true, Kit.IcPause);
            card.Add(chip);
            var logos = Kit.Row(); logos.style.marginTop = 16;
            logos.Add(Pop(Kit.SquareBadge(Kit.IcSkull, Kit.Red, 72, 20), 0.3f));
            var arrow = Kit.Icon(Kit.IcHandshake, 30, Kit.Soft); arrow.style.marginLeft = 22; arrow.style.marginRight = 22;
            logos.Add(arrow);
            logos.Add(Pop(Kit.SquareBadge(Kit.IcRocket, Kit.Teal, 72, 20), 0.45f));
            card.Add(logos);

            var title = Kit.Text("MegaByte Corp quer comprar a Pixel Forja", 26, Kit.Ink, true); title.style.marginTop = 16;
            card.Add(title);
            card.Add(Kit.Text("Proposta fechada — é aceitar ou recusar.", 14, Kit.Soft));
            var offer = Kit.Text("R$ 0", 60, Kit.Cash, true); offer.style.marginTop = 10;
            card.Add(offer);
            CountUp(offer, 0, 1240000, 0.6f, 1.3f, Kit.Money);
            card.Add(Kit.Text("3,0× o seu valuation atual", 13, Kit.Soft, true));

            var row = Kit.Row(Align.Stretch); row.style.marginTop = 22; row.style.alignSelf = Align.Stretch;
            row.Add(Outcome(Kit.IcCheck, Kit.Green, "Se aceitar", new[] { "A run termina aqui", "R$ 1.240.000 vão para o seu bolso", "≈ 36 Pontos de Legado", "Você volta no tempo e recomeça" }, 1.4f));
            row.Add(Kit.Gap(14));
            row.Add(Outcome(Kit.IcClose, Kit.Red, "Se recusar", new[] { "A run continua", "MegaByte fica mais hostil", "Mais assédio aos seus funcionários", "Pode não haver outra oferta assim" }, 1.5f));
            card.Add(row);

            var foot = Kit.Row(); foot.style.marginTop = 22;
            foot.Add(Kit.Ghost("Recusar", Kit.IcClose));
            foot.Add(Kit.Gap(14));
            foot.Add(Kit.Button("Vender e encerrar a run", Kit.IcHourglass, Kit.Cash, Color.white));
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
            var body = Window("Encerrar a run", Kit.IcHourglass, Kit.Gold, 1040, 620, out _, "Vender é o único jeito de levar dinheiro para a próxima vida");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            row.Add(Enter(Exit(Kit.IcStore, Kit.Cash, "Vender a empresa", "R$ 38.000", "pelo valuation de hoje",
                "Disponível a qualquer momento.", "Vender", false)));
            row.Add(Kit.Gap(14));
            row.Add(Enter(Exit(Kit.IcBriefcase, Kit.Purple, "Proposta de rival", "Nenhuma", "rivais procuram você",
                "Chegam como evento. Costumam pagar acima do valuation.", null, false)));
            row.Add(Kit.Gap(14));
            row.Add(Enter(Exit(Kit.IcChartLine, Kit.Blue, "IPO", "Bloqueado", "abrir capital na bolsa",
                "Requer fase Estúdio e a pesquisa de IPO.", null, true)));

            var warn = Kit.Row(Align.FlexStart); warn.style.marginTop = 16; warn.style.backgroundColor = Kit.A(Kit.Red, 0.12f);
            Kit.Radius(warn, 14); Kit.Pad(warn, 14, 18);
            var face = Kit.Badge(Kit.IcFrown, Kit.Red, 44);
            _spin = face;
            warn.Add(face);
            var wt = Kit.Col(); wt.style.marginLeft = 14; wt.style.flexShrink = 1;
            wt.Add(Kit.Text("Calma aí, isso é prejuízo!", 17, Kit.Ink, true));
            wt.Add(Kit.Wrap("Você colocou R$ 50.000 nessa garagem e sairia com R$ 38.000. Até a máquina do tempo ficou com pena. Tem certeza?", 13, Kit.Soft));
            warn.Add(wt);
            body.Add(Enter(warn, 0.7f));
        }

        private static VisualElement Exit(string icon, Color c, string title, string value, string caption, string note, string action, bool locked)
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
                var b = Kit.Button(action, null, c, Color.white, null, 38); b.style.alignSelf = Align.Stretch; b.style.marginTop = 12;
                e.Add(b);
            }
            return e;
        }

        protected override void OnTick(float dt)
        {
            if (_spin != null) _spin.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 5f) * 10f));
        }
    }

    /// <summary>Menu de pausa (GDD 20.6): salvar manualmente, carregar, opções, sair.</summary>
    internal sealed class PauseMenuScreen : GalleryScreen
    {
        private VisualElement _icon;

        protected override void Build()
        {
            Scrim(0.6f);
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(440, 22); Kit.Pad(card, 26, 28); card.style.borderBottomWidth = 6; card.style.alignItems = Align.Stretch;
            center.Add(Enter(card, 0.05f, 30f));

            var head = Kit.Col(); head.style.alignItems = Align.Center; head.style.marginBottom = 16;
            _icon = Kit.Badge(Kit.IcPause, Kit.Teal, 60);
            head.Add(_icon);
            var t = Kit.Text("Jogo pausado", 26, Kit.Ink, true); t.style.marginTop = 10;
            head.Add(t);
            head.Add(Kit.Text("Pixel Forja · Semana 37, Ano 4", 12, Kit.Soft));
            card.Add(head);

            ResetCascade(0.2f);
            card.Add(Item(Kit.Confirm("Continuar", Kit.IcPlay)));
            card.Add(Item(Kit.Ghost("Salvar jogo", Kit.IcSave)));
            card.Add(Item(Kit.Ghost("Carregar", Kit.IcFolder)));
            card.Add(Item(Kit.Ghost("Opções", Kit.IcGear)));
            card.Add(Item(Kit.Ghost("Dicas e tutorial", Kit.IcQuestion)));
            card.Add(Item(Kit.Ghost("Conquistas", Kit.IcTrophy)));
            card.Add(Item(Kit.Ghost("Menu principal", Kit.IcHouse)));
            card.Add(Item(Kit.Button("Sair do jogo", Kit.IcPower, Kit.A(Kit.Red, 0.14f), Kit.Red)));

            var foot = Kit.Row(); foot.style.justifyContent = Justify.Center; foot.style.marginTop = 12;
            foot.Add(Kit.Icon(Kit.IcSave, 10, Kit.Faint));
            var ft = Kit.Text("Autosave há 2 min · Slot 1", 11, Kit.Faint); ft.style.marginLeft = 6;
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

    /// <summary>
    /// Avisos sobre o HUD: conquista (GDD 20.5), evento global da linha do tempo (15.2), novo console,
    /// dica de tutorial (20.3) e notificações rápidas. Mostrados juntos aqui só para validar o visual.
    /// </summary>
    internal sealed class ToastsScreen : GalleryScreen
    {
        private VisualElement _shine, _trophy, _tipArrow;

        public override float SettleSeconds => 3.0f;

        protected override void Build()
        {
            // evento global — faixa central
            var mid = Kit.Centered(); mid.pickingMode = PickingMode.Ignore;
            Root.Add(mid);
            var news = Kit.Card(760, 20); Kit.Pad(news, 0, 0); news.style.overflow = Overflow.Hidden; news.style.borderBottomWidth = 5;
            var nh = Kit.Row(); nh.style.backgroundColor = Kit.Red; Kit.Pad(nh, 8, 18);
            nh.Add(Kit.Icon(Kit.IcNews, 13, Color.white));
            var nc = Kit.Caps("Evento global · Ano 11", 11, Color.white, 2f); nc.style.marginLeft = 8;
            nh.Add(nc);
            news.Add(nh);
            var nb = Kit.Row(); Kit.Pad(nb, 18, 22);
            nb.Add(Kit.SquareBadge(Kit.IcArrowDown, Kit.Red, 64, 18));
            var nt = Kit.Col(); nt.style.marginLeft = 16; nt.style.flexShrink = 1; nt.style.flexGrow = 1;
            nt.Add(Kit.Text("Crash do mercado de games!", 24, Kit.Ink, true));
            nt.Add(Kit.Wrap("As vendas globais caem pela metade por 2 anos. Consoles antigos perdem popularidade e jogos ruins são punidos sem dó.", 13, Kit.Soft));
            var chips = Kit.Row(); chips.style.marginTop = 8;
            var c1 = Kit.Chip("Vendas −50%", Kit.Red, true); c1.style.marginRight = 6; chips.Add(c1);
            chips.Add(Kit.Chip("Dura 2 anos", Kit.Soft));
            nt.Add(chips);
            nb.Add(nt);
            nb.Add(Kit.Confirm("Entendi"));
            news.Add(nb);
            mid.Add(news);
            Animate(0.2f, 0.55f, k =>
            {
                news.style.opacity = Mathf.Clamp01(k * 1.5f);
                news.style.scale = new Scale(new Vector2(0.8f + 0.2f * k, 0.8f + 0.2f * k));
            }, OutBack);

            // conquista — canto inferior direito
            var ach = Kit.Card(360, 16); ach.style.position = Position.Absolute; ach.style.right = 20; ach.style.bottom = 20;
            ach.style.flexDirection = FlexDirection.Row; ach.style.alignItems = Align.Center; ach.style.overflow = Overflow.Hidden;
            Kit.Border(ach, Kit.Gold, 2); ach.style.borderBottomWidth = 4;
            _trophy = Kit.Badge(Kit.IcTrophy, Kit.Gold, 50);
            ach.Add(_trophy);
            var at = Kit.Col(); at.style.marginLeft = 12;
            at.Add(Kit.Caps("Conquista desbloqueada", 10, Kit.Gold));
            at.Add(Kit.Text("Saindo da Garagem", 17, Kit.Ink, true));
            at.Add(Kit.Text("Alcance a fase Indie pela primeira vez", 11, Kit.Soft));
            ach.Add(at);
            _shine = new VisualElement(); _shine.style.position = Position.Absolute; _shine.style.top = -20; _shine.style.bottom = -20;
            _shine.style.width = 46; _shine.style.backgroundColor = Kit.A(Color.white, 0.55f); _shine.style.rotate = new Rotate(new Angle(18f));
            _shine.pickingMode = PickingMode.Ignore;
            ach.Add(_shine);
            Root.Add(ach);
            SlideIn(ach, 420f, 0f, 0.8f);

            // novo console — canto inferior esquerdo
            var con = Kit.Card(340, 16); con.style.position = Position.Absolute; con.style.left = 20; con.style.bottom = 20;
            var ch = Kit.Row();
            ch.Add(Kit.SquareBadge(Kit.IcGamepad, Kit.Blue, 50, 14));
            var ct = Kit.Col(); ct.style.marginLeft = 12; ct.style.flexShrink = 1;
            ct.Add(Kit.Caps("Novo console · Ano 17", 10, Kit.Blue));
            ct.Add(Kit.Text("PlayPocket chegou!", 17, Kit.Ink, true));
            ct.Add(Kit.Text("Portátil · popularidade alta e duradoura", 11, Kit.Soft));
            ch.Add(ct);
            con.Add(ch);
            var cf = Kit.Row(); cf.style.marginTop = 10;
            cf.Add(Kit.Text("Licença R$ 18.000", 13, Kit.Cash, true)); cf.Add(Kit.Spacer());
            cf.Add(Kit.Button("Ver no P&D", Kit.IcFlask, Kit.Blue, Color.white, null, 32));
            con.Add(cf);
            Root.Add(con);
            SlideIn(con, -400f, 0f, 1.0f);

            // notificações rápidas — coluna da direita, sob o caixa
            var col = Kit.Col(); col.style.position = Position.Absolute; col.style.right = 20; col.style.top = 130; col.style.width = 330;
            Root.Add(col);
            col.Add(Note(Kit.IcRocket, Kit.Red, "MegaByte Corp lançou Reino de Aço III", "Nota 8,7 · o gênero RPG saturou", 1.2f));
            col.Add(Note(Kit.IcUsers, Kit.Orange, "Lia Tanaka subiu para Sênior", "Salário agora é R$ 6.000 / sem", 1.35f));
            col.Add(Note(Kit.IcFlask, Kit.Teal, "Pesquisa concluída", "Renderizador 2D avançado", 1.5f));

            // dica de tutorial — aponta para o card de Pesquisa do HUD
            var tip = Kit.Card(320, 14); tip.style.position = Position.Absolute; tip.style.left = 200; tip.style.top = 86;
            tip.style.backgroundColor = Kit.Ink; Kit.Pad(tip, 14, 16);
            _tipArrow = new VisualElement(); _tipArrow.style.position = Position.Absolute; _tipArrow.style.left = -8; _tipArrow.style.top = 22;
            _tipArrow.style.width = 18; _tipArrow.style.height = 18; _tipArrow.style.backgroundColor = Kit.Ink; _tipArrow.style.rotate = new Rotate(new Angle(45f));
            tip.Add(_tipArrow);
            var th = Kit.Row();
            th.Add(Kit.Icon(Kit.IcBulb, 13, Kit.Gold));
            var tc = Kit.Caps("Dica", 10, Kit.Gold); tc.style.marginLeft = 6;
            th.Add(tc);
            tip.Add(th);
            var tt = Kit.Wrap("Pontos de Pesquisa (PP) nascem enquanto você desenvolve jogos. Gaste no painel de P&D para destravar tecnologia.", 12, Kit.Paper);
            tt.style.marginTop = 6;
            tip.Add(tt);
            var tf = Kit.Row(); tf.style.marginTop = 10;
            tf.Add(Kit.Text("Não mostrar dicas", 11, Kit.Faint)); tf.Add(Kit.Spacer());
            tf.Add(Kit.Button("Entendi", null, Kit.Teal, Color.white, null, 30));
            tip.Add(tf);
            Root.Add(tip);
            SlideIn(tip, -40f, 0f, 1.6f);
        }

        private VisualElement Note(string icon, Color c, string title, string sub, float delay)
        {
            var n = Kit.Card(0, 14); n.style.flexDirection = FlexDirection.Row; n.style.alignItems = Align.Center; n.style.marginBottom = 8;
            Kit.Pad(n, 9, 12);
            n.Add(Kit.Badge(icon, c, 30));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexShrink = 1;
            t.Add(Kit.Text(title, 12, Kit.Ink, true));
            t.Add(Kit.Text(sub, 10, Kit.Soft));
            n.Add(t);
            SlideIn(n, 360f, 0f, delay);
            return n;
        }

        protected override void OnTick(float dt)
        {
            float cycle = (Clock - 1.3f) % 2.6f;
            _shine.style.left = cycle < 0f ? -80f : -80f + cycle / 0.9f * 520f;
            float s = 1f + 0.08f * Mathf.Sin(Clock * 5f);
            _trophy.style.scale = new Scale(new Vector2(s, s));
            _tipArrow.parent.style.left = 200f + Mathf.Sin(Clock * 3f) * 4f;
        }
    }
}
