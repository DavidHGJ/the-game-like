using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using GameLike.Systems.Projects;
using GameLike.Systems.Research;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>Painel Financeiro vivo (GDD 4.2): caixa, valuation, histórico, custos e fatores do valuation.</summary>
    internal sealed class FinanceLive : ManagementScreen
    {
        private VisualElement _chart;
        private float _draw;
        private List<long> _history;
        private long _threshold;

        protected override int PanelIndex => 0;

        protected override void Build()
        {
            var gc = Ui.Gc;
            var run = gc.Run;
            var c = run.company;
            var content = Shell("Semana " + gc.Time.CurrentWeek + " · Ano " + gc.Time.CurrentYear + " — fase " + Labels.Phase(c.phase));

            long burn = gc.WeeklyBurn;
            var stats = Kit.Row(Align.Stretch);
            content.Add(stats);
            string runway = c.cash < 0 ? "no vermelho!" : burn > 0 ? (c.cash / burn) + " semanas de fôlego" : "sem custos fixos";
            stats.Add(Enter(StatCard(Kit.IcCoins, Kit.Cash, "Caixa", out var cash, runway, c.cash < 0 ? Kit.Red : Kit.Soft)));
            stats.Add(Enter(StatCard(Kit.IcChartLine, Kit.Teal, "Valuation", out var val, "pico " + Kit.Money(c.peakValuation))));
            stats.Add(Enter(StatCard(Kit.IcArrowUp, Kit.Green, "Entradas (última semana)", out var inc, "vendas e contratos")));
            var last = StatCard(Kit.IcArrowDown, Kit.Red, "Saídas (última semana)", out var exp, "custo fixo ≈ " + Kit.Money(burn) + " / sem");
            last.style.marginRight = 0;
            stats.Add(Enter(last));
            cash.text = Kit.Money(c.cash); val.text = Kit.Money(c.valuation);
            inc.text = Kit.Money(c.lastWeekIncome); exp.text = Kit.Money(c.lastWeekExpense);
            if (c.cash < 0) cash.style.color = Kit.Red;

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            content.Add(row);

            _history = c.valuationHistory;
            _threshold = gc.NextPhaseThreshold;
            var chartCard = Kit.Outline(16); chartCard.style.flexGrow = 1; Kit.Pad(chartCard, 16, 18);
            var ch = Kit.Row();
            ch.Add(Kit.Text("Histórico de valuation", 15, Kit.Ink, true));
            ch.Add(Kit.Spacer());
            if (_threshold > 0) ch.Add(Kit.Chip("Próxima fase: " + Kit.Money(_threshold), Kit.Gold, false, Kit.IcFlag));
            chartCard.Add(ch);
            _chart = new VisualElement(); _chart.style.flexGrow = 1; _chart.style.marginTop = 10;
            _chart.generateVisualContent += DrawChart;
            chartCard.Add(_chart);
            if (_history.Count < 2)
                chartCard.Add(Kit.Text("O gráfico aparece depois de algumas semanas.", 11, Kit.Soft));

            if (c.expansionAvailable)
            {
                var ex = Kit.Row(); ex.style.marginTop = 10; ex.style.backgroundColor = Kit.A(Kit.Gold, 0.18f); Kit.Radius(ex, 12); Kit.Pad(ex, 8, 12);
                ex.Add(Kit.Icon(Kit.IcWarn, 13, Kit.Gold));
                var et = Kit.Wrap("Expansão disponível há " + c.stagnationWeeks + " semana(s): o valuation está estagnando.", 12, Kit.Ink);
                et.style.marginLeft = 8; et.style.flexGrow = 1; et.style.flexShrink = 1;
                ex.Add(et);
                ex.Add(Kit.Button("Ver expansão", null, Kit.Teal, Color.white, () => Ui.Open(new ExpansionLive()), 32));
                chartCard.Add(ex);
            }
            row.Add(Enter(chartCard));

            row.Add(Kit.Gap(16));
            var side = Kit.Col(); side.style.width = 380; side.style.flexShrink = 0;
            row.Add(side);

            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            long payroll = gc.Staff != null ? gc.Staff.WeeklyPayroll : 0;
            long operating = economy != null ? economy.WeeklyOperatingFor(c.phase) : 0;
            long research = run.research != null ? run.research.activeWeeklyCost : 0;
            long engine = run.IsDeveloping && gc.Research != null ? gc.Research.EngineWeeklyCost : 0;
            long max = System.Math.Max(1, System.Math.Max(System.Math.Max(payroll, operating), System.Math.Max(research, engine)));
            var expCard = Kit.Tile(16);
            expCard.Add(Kit.Text("Custos fixos por semana", 14, Kit.Ink, true));
            expCard.Add(Expense("Salários", payroll, max, Kit.Orange));
            expCard.Add(Expense("Aluguel e contas", operating, max, Kit.Soft));
            expCard.Add(Expense("Pesquisa (P&D)", research, max, Kit.Teal));
            expCard.Add(Expense("Engine (em produção)", engine, max, Kit.Sky));
            side.Add(Enter(expCard));

            var facCard = Kit.Tile(16); facCard.style.marginTop = 12; facCard.style.flexGrow = 1;
            facCard.Add(Kit.Text("O que forma o valuation", 14, Kit.Ink, true));
            foreach (var part in ValuationCalculator.Breakdown(c, run.releasedProjects))
            {
                var r = Kit.Row(); r.style.marginTop = 8;
                r.Add(Kit.Text(part.Key, 12, Kit.Ink)); r.Add(Kit.Spacer());
                r.Add(Kit.Text("+ " + Kit.Money(part.Value), 12, Kit.Green, true));
                facCard.Add(r);
            }
            float perk = FounderPerks.ValuationMultiplier;
            if (perk > 1.001f)
            {
                var r = Kit.Row(); r.style.marginTop = 8;
                r.Add(Kit.Text("Faro Empresarial (fundador)", 12, Kit.Ink)); r.Add(Kit.Spacer());
                r.Add(Kit.Text("× " + perk.ToString("0.00").Replace('.', ','), 12, Kit.Green, true));
                facCard.Add(r);
            }
            facCard.Add(Kit.Spacer());
            var sell = Kit.Button("Encerrar a run…", Kit.IcHourglass, Kit.Gold, Color.white, Ui.OpenSellOptions, 38); sell.style.marginTop = 10;
            facCard.Add(sell);
            side.Add(Enter(facCard));
        }

        private static VisualElement Expense(string name, long value, long max, Color color)
        {
            var c = Kit.Col(); c.style.marginTop = 10;
            var r = Kit.Row();
            r.Add(Kit.Text(name, 12, Kit.Ink)); r.Add(Kit.Spacer());
            r.Add(Kit.Text(Kit.Money(value), 12, Kit.Ink, true));
            c.Add(r);
            var bar = Kit.Bar(8, color, out _, Mathf.Clamp01(value / (float)max)); bar.style.marginTop = 4;
            c.Add(bar);
            return c;
        }

        protected override void OnTick(float dt)
        {
            _draw = Mathf.Clamp01((Clock - 0.4f) / 1.2f);
            _chart.MarkDirtyRepaint();
        }

        private void DrawChart(MeshGenerationContext mgc)
        {
            var r = _chart.contentRect;
            if (r.width < 20f || r.height < 20f) return;
            var p = mgc.painter2D;
            float w = r.width, h = r.height;
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.strokeColor = Kit.Track; p.lineWidth = 1.5f;
            for (int g = 0; g <= 4; g++)
            {
                float y = h * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(w, y)); p.Stroke();
            }
            if (_history == null || _history.Count < 2) return;

            long max = _threshold;
            for (int i = 0; i < _history.Count; i++) if (_history[i] > max) max = _history[i];
            float top = Mathf.Max(1f, max * 1.1f);

            if (_threshold > 0)
            {
                float ty = h - _threshold / top * h;
                p.strokeColor = Kit.A(Kit.Gold, 0.9f); p.lineWidth = 2f;
                for (float x = 0; x < w; x += 14f)
                {
                    p.BeginPath(); p.MoveTo(new Vector2(x, ty)); p.LineTo(new Vector2(Mathf.Min(x + 8f, w), ty)); p.Stroke();
                }
            }

            int n = _history.Count;
            int shown = Mathf.Max(2, Mathf.CeilToInt(n * _draw));
            Vector2 At(int i) => new Vector2(i / (float)(n - 1) * w, h - _history[i] / top * h);
            p.fillColor = Kit.A(Kit.Teal, 0.12f);
            p.BeginPath(); p.MoveTo(new Vector2(0, h));
            for (int i = 0; i < shown; i++) p.LineTo(At(i));
            p.LineTo(new Vector2(At(shown - 1).x, h)); p.ClosePath(); p.Fill();
            p.strokeColor = Kit.Teal; p.lineWidth = 4f;
            p.BeginPath(); p.MoveTo(At(0));
            for (int i = 1; i < shown; i++) p.LineTo(At(i));
            p.Stroke();
            Vector2 tip = At(shown - 1);
            Kit.Disc(p, tip, 10f + 2f * Mathf.Sin(Clock * 5f), Kit.A(Kit.Teal, 0.25f));
            Kit.Disc(p, tip, 5f, Kit.Teal);
        }
    }

    /// <summary>Painel Projetos vivo (GDD 5.8 / 13): jogo em produção, catálogo com vendas e franquias.</summary>
    internal sealed class ProjectsLive : ManagementScreen
    {
        protected override int PanelIndex => 1;

        protected override void Build()
        {
            var gc = Ui.Gc;
            var run = gc.Run;
            int selling = 0;
            foreach (var g in run.releasedProjects) if (g.IsSelling) selling++;
            var content = Shell(run.releasedProjects.Count + " lançados · " + selling + " ainda vendendo");

            var p = run.currentProject;
            var current = Kit.Tile(16); current.style.flexDirection = FlexDirection.Row; current.style.alignItems = Align.Center;
            Kit.Pad(current, 14, 18);
            if (p != null && run.HasActiveProject)
            {
                current.Add(Kit.SquareBadge(Labels.GenreIcon(p.genreKey), Kit.Blue, 52));
                var ct = Kit.Col(); ct.style.marginLeft = 14; ct.style.width = 320;
                bool ready = p.status == ProjectStatus.ReadyToLaunch;
                ct.Add(Kit.Chip(ready ? "Pronto para lançar" : p.awaitingAllocation ? "Aguardando alocação" : "Em desenvolvimento", ready ? Kit.Green : Kit.Blue, true));
                var cn = Kit.Text(p.name, 18, Kit.Ink, true); cn.style.marginTop = 4;
                ct.Add(cn);
                ct.Add(Kit.Text(p.genreDisplay + " · " + (p.themeDisplay ?? "sem tema") + " · " + (p.consoleDisplay ?? "—") + " · " + Labels.Size(p.size), 11, Kit.Soft));
                current.Add(ct);
                var pc = Kit.Col(); pc.style.flexGrow = 1; pc.style.marginLeft = 10; pc.style.marginRight = 20;
                var pl = Kit.Row();
                int phase = Mathf.Clamp(p.phaseIndex, 0, 4);
                pl.Add(Kit.Text(ready ? "Produção concluída" : "Fase " + (phase + 1) + " de 5 — " + ParameterCatalog.PhaseLabel((ProductionPhase)phase), 12, Kit.Ink, true));
                pl.Add(Kit.Spacer());
                pl.Add(Kit.Text(p.bugCount + " bugs · hype " + Mathf.RoundToInt(p.hype01 * 100f), 12, Kit.Soft));
                pc.Add(pl);
                var bar = Kit.Bar(12, Kit.PhaseColors[phase], out _, ProjectManager.OverallProgress01(p)); bar.style.marginTop = 6;
                pc.Add(bar);
                current.Add(pc);
                current.Add(Kit.Primary(ready ? "Lançar" : p.awaitingAllocation ? "Alocar" : "Marketing", null, Ui.OpenProjectContext));
            }
            else
            {
                current.Add(Kit.Badge(Kit.IcGamepad, Kit.Track, 46, Kit.Faint));
                var t = Kit.Text("Nenhum jogo em produção.", 15, Kit.Soft, true); t.style.marginLeft = 14; t.style.flexGrow = 1;
                current.Add(t);
                current.Add(Kit.Primary("Novo projeto", Kit.IcPlus, Ui.OpenNewProject));
            }
            content.Add(Enter(current));

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            content.Add(row);
            var list = Kit.Col(); list.style.flexGrow = 1;
            row.Add(list);
            var head = Kit.Row(); Kit.Pad(head, 0, 14); head.style.marginBottom = 6;
            head.Add(HeadCell("Jogo", 0, true)); head.Add(HeadCell("Nota", 64)); head.Add(HeadCell("Unidades", 100));
            head.Add(HeadCell("Receita", 120)); head.Add(HeadCell("Vendas", 110)); head.Add(HeadCell("Situação", 150));
            list.Add(head);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1;
            list.Add(scroll);
            if (run.releasedProjects.Count == 0)
                scroll.Add(LiveBits.Note(Kit.IcInfo, Kit.Blue, "Seu catálogo aparece aqui depois do primeiro lançamento."));
            for (int i = run.releasedProjects.Count - 1; i >= 0; i--) scroll.Add(Enter(Game(run.releasedProjects[i])));

            row.Add(Kit.Gap(16));
            var side = Kit.Col(); side.style.width = 300; side.style.flexShrink = 0;
            row.Add(side);
            side.Add(Kit.Caps("Franquias", 11, Kit.Soft));
            var seen = new HashSet<string>();
            foreach (var g in run.releasedProjects)
            {
                if (string.IsNullOrEmpty(g.franchiseId) || !seen.Add(g.franchiseId)) continue;
                int count = FranchiseCalculator.Count(run.releasedProjects, g.franchiseId);
                float avg = FranchiseCalculator.AverageScore(run.releasedProjects, g.franchiseId);
                string first = g.name;
                foreach (var o in run.releasedProjects) if (o.id == g.franchiseId) first = o.name;
                side.Add(Enter(Franchise(first, count, avg)));
            }
            if (seen.Count == 0)
            {
                var tip = LiveBits.Note(Kit.IcCrown, Kit.Gold, gc.CanMakeSequels
                    ? "Crie uma sequência de um jogo do catálogo para começar uma franquia."
                    : "Sequências exigem a pesquisa Gestão de Franquias (fase Escritório).");
                tip.style.marginTop = 10;
                side.Add(Enter(tip));
            }
        }

        private VisualElement Game(ProjectState g)
        {
            var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
            r.style.marginBottom = 8; Kit.Pad(r, 10, 14);
            var nameCell = Kit.Row(); nameCell.style.flexGrow = 1; nameCell.style.flexBasis = 0;
            nameCell.Add(Kit.SquareBadge(Labels.GenreIcon(g.genreKey), Kit.Blue, 40, 10));
            var nt = Kit.Col(); nt.style.marginLeft = 10; nt.style.flexShrink = 1;
            nt.Add(Kit.Text(g.name + (g.isSequel ? "  (sequência)" : ""), 14, Kit.Ink, true));
            nt.Add(Kit.Text(g.genreDisplay + " · " + (g.themeDisplay ?? "—") + " · " + (g.consoleDisplay ?? "—"), 10, Kit.Soft));
            nameCell.Add(nt);
            r.Add(nameCell);

            var scoreCell = Kit.Col(); scoreCell.style.width = 64;
            var pill = Kit.Text(Labels.Score(g.finalScore), 14, Color.white, true);
            pill.style.backgroundColor = Labels.ScoreColor(g.finalScore); Kit.Radius(pill, 9); Kit.Pad(pill, 4, 9); pill.style.alignSelf = Align.FlexStart;
            scoreCell.Add(pill);
            r.Add(scoreCell);
            var u = Kit.Text(Kit.Short(g.unitsSold), 13, Kit.Ink, true); u.style.width = 100; r.Add(u);
            var rv = Kit.Text(Kit.Money(g.revenue), 13, Kit.Cash, true); rv.style.width = 120; r.Add(rv);

            var spark = new VisualElement(); spark.style.width = 96; spark.style.height = 30; spark.style.marginRight = 14;
            var data = g.weeklyUnits;
            Color color = g.IsSelling ? Kit.Blue : Kit.Faint;
            spark.generateVisualContent += m =>
            {
                var rect = spark.contentRect;
                if (rect.width < 4f || data == null || data.Count < 2) return;
                long max = 1;
                foreach (long v in data) if (v > max) max = v;
                var p = m.painter2D;
                p.strokeColor = color; p.lineWidth = 2.5f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
                p.BeginPath();
                for (int i = 0; i < data.Count; i++)
                {
                    var pt = new Vector2(i / (float)(data.Count - 1) * rect.width, rect.height - 2f - data[i] / (float)max * (rect.height - 4f));
                    if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                }
                p.Stroke();
            };
            r.Add(spark);

            var st = Kit.Row(); st.style.width = 150;
            st.Add(g.IsSelling
                ? Kit.Chip("Vendendo · sem. " + g.salesWeek + "/" + g.salesWeeksTotal, Kit.Green)
                : Kit.Chip("Vendas encerradas", Kit.Soft));
            r.Add(st);
            return r;
        }

        private static VisualElement Franchise(string name, int games, float avg)
        {
            Color c = Labels.ScoreColor(avg);
            var f = Kit.Outline(14); f.style.marginTop = 10; Kit.Pad(f, 12, 14);
            var r = Kit.Row();
            r.Add(Kit.Badge(Kit.IcCrown, Kit.A(Kit.Gold, 0.2f), 34, Kit.Gold));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Wrap(name, 14, Kit.Ink, true));
            t.Add(Kit.Text(games + (games == 1 ? " jogo" : " jogos"), 11, Kit.Soft));
            r.Add(t);
            r.Add(Kit.Text(Labels.Score(avg), 22, c, true));
            f.Add(r);
            var bar = Kit.Bar(8, c, out _, avg / 10f); bar.style.marginTop = 8;
            f.Add(bar);
            float hype = FranchiseCalculator.InitialHype01(avg) * 100f;
            var n = Kit.Text(hype > 0f ? "Próxima sequência começa com +" + Mathf.RoundToInt(hype) + " de hype" : "Média baixa: a marca virou peso", 11, c, true);
            n.style.marginTop = 6;
            f.Add(n);
            return f;
        }
    }

    /// <summary>Painel RH vivo (GDD 7): equipe com ações e mercado de talentos.</summary>
    internal sealed class StaffLive : ManagementScreen
    {
        private VisualElement _host;
        private int _tab;

        protected override int PanelIndex => 2;

        protected override void Build()
        {
            var gc = Ui.Gc;
            var c = gc.Run.company;
            var content = Shell("Equipe " + c.EffectiveTeamSize + " de " + gc.Staff.MaxTeam + " (com você) · folha " + Kit.Money(gc.Staff.WeeklyPayroll) + " / semana");
            AddTabs(new[] { "Equipe", "Mercado de talentos" }, i => { _tab = i; Show(); });
            _host = new ScrollView(); _host.style.flexGrow = 1;
            content.Add(_host);
            if (_tab > 0) SelectTab(_tab); else Show();
        }

        private void Show()
        {
            _host.Clear();
            ResetCascade(0.1f);
            if (_tab == 0) BuildTeam(); else BuildMarket();
        }

        private void BuildTeam()
        {
            var gc = Ui.Gc;
            var grid = LiveBits.Wrap();
            _host.Add(grid);
            grid.Add(Enter(FounderCard()));
            var staff = gc.Run.company.employees;
            for (int i = 0; i < staff.Count; i++) grid.Add(Enter(Employee(staff[i], i)));
            int free = gc.Staff.MaxTeam - gc.Run.company.EffectiveTeamSize;
            for (int i = 0; i < free && i < 3; i++) grid.Add(Enter(Seat(true)));
            if (gc.Run.company.phase != CompanyPhase.Studio) grid.Add(Enter(Seat(false)));
        }

        private VisualElement FounderCard()
        {
            var meta = Ui.Gc.Meta;
            var c = Kit.Outline(16); c.style.width = 296; c.style.marginRight = 14; c.style.marginBottom = 14; Kit.Pad(c, 16, 16);
            var top = Kit.Row();
            var holder = Kit.Avatar(3, 56); holder.Clear();
            holder.Add(new FounderArt(56) { Skin = meta.founder.skin, Hair = meta.founder.hair, HairStyle = meta.founder.hairStyle, Shirt = meta.founder.shirt, Accessory = meta.founder.accessory });
            top.Add(holder);
            var t = Kit.Col(); t.style.marginLeft = 12;
            t.Add(Kit.Text(meta.founder.name, 16, Kit.Ink, true));
            t.Add(Kit.Text("Fundador · faz de tudo", 11, Kit.Soft));
            var lv = Kit.Chip("Fundador", Kit.Sky, true); lv.style.marginTop = 4; lv.style.alignSelf = Align.FlexStart;
            t.Add(lv);
            top.Add(t);
            c.Add(top);
            var n = Kit.Wrap("Rende 100 pontos por fase e cobre todas as áreas pela metade.", 11, Kit.Soft); n.style.marginTop = 10;
            c.Add(n);
            c.Add(Kit.Divider(10));
            c.Add(Kit.Text("sem salário", 12, Kit.Soft, true));
            return c;
        }

        private VisualElement Employee(EmployeeInstance e, int tone)
        {
            var staff = Ui.Gc.Staff;
            long cash = Ui.Gc.Run.company.cash;
            var c = Kit.Outline(16); c.style.width = 296; c.style.marginRight = 14; c.style.marginBottom = 14; Kit.Pad(c, 16, 16);
            var top = Kit.Row();
            top.Add(Kit.Avatar(tone, 56));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexShrink = 1;
            t.Add(Kit.Wrap(e.name, 16, Kit.Ink, true));
            t.Add(Kit.Text(Labels.SpecialtyName(e.specialty), 11, Kit.Soft));
            var lv = Kit.Chip(Labels.SeniorityName(e.seniority), Labels.SpecialtyColor(e.specialty), true); lv.style.marginTop = 4; lv.style.alignSelf = Align.FlexStart;
            t.Add(lv);
            top.Add(t);
            c.Add(top);
            string trait = Labels.TraitName(e.trait);
            if (trait != null)
            {
                var chip = Kit.Chip(trait, e.trait == PersonalityTrait.BurnoutProne ? Kit.Red : Kit.Soft); chip.style.marginTop = 10; chip.style.alignSelf = Align.FlexStart;
                c.Add(chip);
            }
            int next = e.seniority == Seniority.Junior ? 150 : e.seniority == Seniority.Mid ? 400 : e.seniority == Seniority.Senior ? 800 : e.xp;
            c.Add(LiveBits.Meter("Experiência", next > 0 ? e.xp / (float)next : 1f, Kit.Blue, e.seniority == Seniority.Star ? "nível máximo" : e.xp + " / " + next + " XP"));
            c.Add(LiveBits.Meter("Humor", e.mood01, e.mood01 > 0.5f ? Kit.Green : Kit.Gold, e.mood01 > 0.5f ? "satisfeito" : "desanimado"));
            c.Add(LiveBits.Meter("Burnout", e.burnout01, e.burnout01 > 0.7f ? Kit.Red : Kit.Orange, e.burnout01 > 0.7f ? "no limite!" : "sob controle"));
            c.Add(Kit.Divider(10));
            c.Add(Kit.Text(Kit.Money(e.weeklySalary) + " / sem", 12, Kit.Ink, true));

            var acts = Kit.Row(); acts.style.marginTop = 8;
            acts.Add(Action("Treinar", Kit.Short(staff.TrainingCost(e)), Kit.Blue, cash >= staff.TrainingCost(e), () => staff.Train(e)));
            acts.Add(Action("Férias", Kit.Short(staff.VacationCost(e)), Kit.Teal, cash >= staff.VacationCost(e), () => staff.GiveVacation(e)));
            acts.Add(Action("Bônus", Kit.Short(staff.BonusCost(e)), Kit.Gold, cash >= staff.BonusCost(e), () => staff.GiveBonus(e)));
            c.Add(acts);
            var fire = Kit.Button("Demitir (" + Kit.Money(e.weeklySalary * 2) + ")", null, Kit.A(Kit.Red, 0.14f), Kit.Red, () =>
            {
                staff.Fire(e); Sfx.Play(SfxId.Spend); Rebuild();
            }, 30);
            fire.style.marginTop = 8;
            c.Add(fire);
            return c;
        }

        private VisualElement Action(string label, string cost, Color color, bool enabled, System.Func<bool> act)
        {
            var b = Kit.Col(); b.style.flexGrow = 1; b.style.flexBasis = 0; b.style.marginRight = 6; b.style.alignItems = Align.Center;
            b.style.backgroundColor = Kit.A(color, 0.14f); Kit.Radius(b, 9); Kit.Pad(b, 5, 2);
            b.Add(Kit.Text(label, 11, Color.Lerp(color, Kit.Ink, 0.3f), true));
            b.Add(Kit.Text("R$ " + cost, 10, Kit.Soft, true));
            Kit.Hover(b, 1.06f, 0.94f);
            b.RegisterCallback<ClickEvent>(_ =>
            {
                if (act()) { Sfx.Play(SfxId.Success); Rebuild(); } else Sfx.Play(SfxId.Fail);
            });
            LiveBits.SetEnabled(b, enabled);
            return b;
        }

        private static VisualElement Seat(bool free)
        {
            var c = Kit.Tile(16); c.style.width = 296; c.style.marginRight = 14; c.style.marginBottom = 14;
            c.style.alignItems = Align.Center; c.style.justifyContent = Justify.Center; c.style.minHeight = 220;
            c.Add(Kit.Badge(free ? Kit.IcUserPlus : Kit.IcLock, Kit.Track, 52, Kit.Faint));
            var t = Kit.Text(free ? "Vaga aberta" : "Mais vagas", 14, Kit.Soft, true); t.style.marginTop = 10;
            c.Add(t);
            c.Add(Kit.Text(free ? "Contrate no mercado de talentos" : "Expanda a empresa de fase", 11, Kit.Faint));
            return c;
        }

        private void BuildMarket()
        {
            var staff = Ui.Gc.Staff;
            var info = Kit.Row(); info.style.marginBottom = 12;
            info.Add(Kit.Icon(Kit.IcHistory, 12, Kit.Soft));
            var it = Kit.Text("Novos candidatos em " + staff.WeeksUntilRefresh + " semana(s)", 12, Kit.Soft, true); it.style.marginLeft = 6;
            info.Add(it);
            info.Add(Kit.Spacer());
            if (!staff.CanHire) info.Add(Kit.Chip("Equipe cheia — demita ou expanda de fase para contratar", Kit.Red, false, Kit.IcWarn));
            _host.Add(Enter(info));

            var grid = LiveBits.Wrap();
            _host.Add(grid);
            var candidates = staff.Candidates;
            for (int i = 0; i < candidates.Count; i++) grid.Add(Enter(Candidate(candidates[i], i + 3)));
        }

        private VisualElement Candidate(EmployeeInstance e, int tone)
        {
            var staff = Ui.Gc.Staff;
            var c = Kit.Outline(16); c.style.width = 296; c.style.marginRight = 14; c.style.marginBottom = 14; Kit.Pad(c, 16, 16); c.style.alignItems = Align.Center;
            c.Add(Kit.Avatar(tone, 72));
            var n = Kit.Text(e.name, 16, Kit.Ink, true); n.style.marginTop = 10;
            c.Add(n);
            c.Add(Kit.Text(Labels.SpecialtyName(e.specialty), 11, Kit.Soft));
            var lv = Kit.Chip(Labels.SeniorityName(e.seniority), Labels.SpecialtyColor(e.specialty), true); lv.style.marginTop = 6;
            c.Add(lv);
            var st = Kit.Stars(1 + (int)e.seniority, 4, 14); st.style.marginTop = 10;
            c.Add(st);
            string trait = Labels.TraitName(e.trait);
            var chip = Kit.Chip(trait ?? "Sem traço marcante", Kit.Soft); chip.style.marginTop = 10;
            c.Add(chip);
            c.Add(Kit.Divider(12));
            c.Add(Kit.Text(Kit.Money(e.weeklySalary) + " / sem", 14, Kit.Ink, true));
            var hire = Kit.Button("Contratar", Kit.IcUserPlus, Kit.Orange, Color.white, () =>
            {
                if (staff.Hire(e)) { Sfx.Play(SfxId.Success); Rebuild(); } else Sfx.Play(SfxId.Fail);
            }, 36);
            hire.style.marginTop = 10; hire.style.alignSelf = Align.Stretch;
            LiveBits.SetEnabled(hire, staff.CanHire);
            c.Add(hire);
            return c;
        }
    }

    /// <summary>Painel Mercado vivo (GDD 8.2 / 8.3): procura por gênero/tema, plataformas e rivais.</summary>
    internal sealed class MarketLive : ManagementScreen
    {
        private VisualElement _host;
        private int _tab;

        protected override int PanelIndex => 3;

        protected override void Build()
        {
            var content = Shell("Ano " + Ui.Gc.Time.CurrentYear + " — o que está em alta e quem está na disputa");
            AddTabs(new[] { "Tendências", "Rivais" }, i => { _tab = i; Show(); });
            _host = new ScrollView(); _host.style.flexGrow = 1;
            content.Add(_host);
            if (_tab > 0) SelectTab(_tab); else Show();
        }

        private void Show()
        {
            _host.Clear();
            ResetCascade(0.1f);
            if (_tab == 0) BuildTrends(); else BuildRivals();
        }

        private void BuildTrends()
        {
            var gc = Ui.Gc;
            var market = gc.Run.market;
            if (market.globalEventWeeksLeft > 0 || Mathf.Abs(market.globalSalesMultiplier - 1f) > 0.01f)
            {
                bool bad = market.globalSalesMultiplier < 1f;
                var ev = LiveBits.Note(Kit.IcNews, bad ? Kit.Red : Kit.Green,
                    "Evento global ativo: vendas em " + Mathf.RoundToInt(market.globalSalesMultiplier * 100f) + "% do normal por mais " + market.globalEventWeeksLeft + " semanas.");
                ev.style.marginBottom = 12;
                _host.Add(Enter(ev));
            }

            var row = Kit.Row(Align.Stretch);
            _host.Add(row);
            var genres = Column("Gêneros", Kit.IcGamepad);
            foreach (var g in gc.AvailableGenres) genres.Add(Trend(g.DisplayLabel, market.GetGenreTrend(g.name)));
            row.Add(Enter(genres));

            var themes = Column("Temas", Kit.IcHat);
            foreach (var t in gc.AvailableThemes) themes.Add(Trend(t.DisplayLabel, market.GetThemeTrend(t.name)));
            row.Add(Enter(themes));

            var plats = Column("Plataformas", Kit.IcDesktop);
            plats.style.marginRight = 0;
            int year = gc.Time.CurrentYear;
            foreach (var c in gc.ConsolesAvailableNow())
            {
                float pop = c.PopularityAtYear(year);
                var col = Kit.Col(); col.style.marginTop = 10;
                var r = Kit.Row();
                r.Add(Kit.Badge(Labels.PlatformIcon(c.Category), Kit.A(Kit.Teal, 0.18f), 26, Kit.Teal));
                var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
                t.Add(Kit.Text(c.DisplayLabel, 13, Kit.Ink, true));
                t.Add(Kit.Text("Licença " + Kit.Money(gc.LicenseCost(c)), 10, Kit.Soft));
                r.Add(t);
                r.Add(Kit.Text(Mathf.RoundToInt(pop * 100f) + "%", 13, Kit.Teal, true));
                col.Add(r);
                var bar = Kit.Bar(8, Kit.Teal, out _, pop); bar.style.marginTop = 5;
                col.Add(bar);
                plats.Add(col);
            }
            ConsoleSO upcoming = null;
            foreach (var c in gc.AllConsoles)
                if (c != null && c.ReleaseYear > year && (upcoming == null || c.ReleaseYear < upcoming.ReleaseYear)) upcoming = c;
            if (upcoming != null)
            {
                var soon = LiveBits.Note(Kit.IcClock, Kit.Purple, "Próximo console: " + upcoming.DisplayLabel + " no Ano " + upcoming.ReleaseYear + ".");
                soon.style.marginTop = 12;
                plats.Add(soon);
            }
            row.Add(Enter(plats));
        }

        private static VisualElement Column(string title, string icon)
        {
            var c = Kit.Outline(16); c.style.flexGrow = 1; c.style.flexBasis = 0; c.style.marginRight = 14; Kit.Pad(c, 14, 16);
            var h = Kit.Row(); h.style.marginBottom = 4;
            h.Add(Kit.Icon(icon, 14, Kit.Purple));
            var t = Kit.Text(title, 15, Kit.Ink, true); t.style.marginLeft = 8;
            h.Add(t);
            h.Add(Kit.Spacer());
            h.Add(Kit.Caps("Procura", 9, Kit.Faint, 1f));
            c.Add(h);
            return c;
        }

        private static VisualElement Trend(string name, float value01)
        {
            int level = value01 >= 0.7f ? 2 : value01 >= 0.56f ? 1 : value01 <= 0.3f ? -2 : value01 <= 0.44f ? -1 : 0;
            string icon = level > 0 ? Kit.IcArrowUp : level < 0 ? Kit.IcArrowDown : Kit.IcMinus;
            Color c = level > 0 ? Kit.Green : level < 0 ? Kit.Red : Kit.Faint;
            var r = Kit.Row(); r.style.marginTop = 10;
            r.Add(Kit.Badge(icon, Kit.A(c, 0.16f), 26, c));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 13, Kit.Ink, true));
            string label = level == 2 ? "Explodindo" : level == 1 ? "Em alta" : level == 0 ? "Estável" : level == -1 ? "Esfriando" : "Saturado";
            if (FounderPerks.ShowsExactTrends) label += " · " + Mathf.RoundToInt(value01 * 100f) + "%";
            t.Add(Kit.Text(label, 10, c, true));
            r.Add(t);
            var bar = Kit.Bar(8, level < 0 ? Kit.Red : Kit.Purple, out _, value01); bar.style.width = 110;
            r.Add(bar);
            return r;
        }

        private void BuildRivals()
        {
            var gc = Ui.Gc;
            var grid = LiveBits.Wrap();
            _host.Add(grid);
            var rivals = gc.Run.rivals;
            for (int i = 0; i < rivals.Count; i++)
                if (!rivals[i].acquired) grid.Add(Enter(Rival(rivals[i])));

            bool researched = gc.Research != null && gc.Research.IsUnlocked(ResearchCatalog.FeatureAcquisition);
            var note = LiveBits.Note(Kit.IcBriefcase, Kit.Purple, researched
                ? "M&A liberado: no Escritório você compra rivais pequenos; no Estúdio, os grandes."
                : "Para comprar rivais, pesquise Fusões e Aquisições (M&A) no P&D — disponível a partir da fase Escritório.");
            note.style.marginTop = 6;
            _host.Add(Enter(note));
        }

        private VisualElement Rival(RivalState r)
        {
            Color[] tones = { Kit.Teal, Kit.Purple, Kit.Red, Kit.Orange, Kit.Blue, Kit.Pink };
            string[] icons = { Kit.IcRocket, Kit.IcStar, Kit.IcSkull, Kit.IcFire, Kit.IcGhost, Kit.IcDragon };
            int k = Mathf.Abs(r.iconIndex) % tones.Length;
            var card = Kit.Outline(16); card.style.width = 300; card.style.marginRight = 14; card.style.marginBottom = 14; Kit.Pad(card, 16, 16);
            var top = Kit.Row();
            top.Add(Kit.SquareBadge(icons[k], tones[k], 48));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexShrink = 1;
            t.Add(Kit.Wrap(r.name, 15, Kit.Ink, true));
            t.Add(Kit.Text("Fase " + Labels.Phase(r.phase) + (r.inCrisis ? " · em crise" : ""), 11, r.inCrisis ? Kit.Red : Kit.Soft));
            top.Add(t);
            card.Add(top);
            if (r.fromPastRun)
            {
                var chip = Kit.Chip("Sua empresa da Run " + r.pastRunNumber, Kit.Gold, false, Kit.IcHourglass); chip.style.marginTop = 8; chip.style.alignSelf = Align.FlexStart;
                card.Add(chip);
            }
            card.Add(Kit.Divider(10));
            card.Add(Kit.Text("Valuation estimado", 10, Kit.Soft, true));
            card.Add(Kit.Text(Kit.Money(r.valuation), 20, Kit.Ink, true));
            var lg = Kit.Text("Último jogo", 10, Kit.Soft, true); lg.style.marginTop = 8;
            card.Add(lg);
            card.Add(Kit.Text(string.IsNullOrEmpty(r.lastGameName) ? "— nenhum ainda" : r.lastGameName + " · " + Labels.Score(r.lastGameScore), 12, Kit.Ink));
            Color rc = r.relation01 < 0.35f ? Kit.Red : r.relation01 < 0.65f ? Kit.Gold : Kit.Green;
            card.Add(LiveBits.Meter("Relação", r.relation01, rc, r.relation01 < 0.35f ? "Hostil" : r.relation01 < 0.65f ? "Neutra" : "Amigável"));

            bool can = Ui.Gc.Rivals != null && Ui.Gc.Rivals.CanTarget(r);
            var buy = Kit.Button(r.offerCooldownWeeks > 0 ? "Aguarde " + r.offerCooldownWeeks + " sem." : "Propor compra", Kit.IcBriefcase, Kit.Purple, Color.white,
                () => Ui.OpenAcquisition(r), 34);
            buy.style.marginTop = 12;
            LiveBits.SetEnabled(buy, can);
            card.Add(buy);
            return card;
        }
    }

    /// <summary>Painel P&amp;D vivo (GDD 6 / adendo 23.5 e 23.7): gastar PP em pesquisas e ver a engine da empresa.</summary>
    internal sealed class ResearchLive : ManagementScreen
    {
        private static readonly string[] Categories = { "Engine", "Temas", "Escopo", "Marketing", "Negócios" };
        private static readonly UnlockKind[] Kinds = { UnlockKind.Engine, UnlockKind.Theme, UnlockKind.Size, UnlockKind.Marketing, UnlockKind.Feature };

        private VisualElement _host, _gauge, _activeFill;
        private int _tab, _category;
        private float _gaugeFill, _gaugeTarget;

        protected override int PanelIndex => 4;

        protected override void Build()
        {
            var content = Shell("PP nasce ao desenvolver e lançar jogos — o P&D recomeça a cada run");
            AddTabs(new[] { "Pesquisa", "Engine" }, i => { _tab = i; Show(); });
            _host = new ScrollView(); _host.style.flexGrow = 1;
            content.Add(_host);
            if (_tab > 0) SelectTab(_tab); else Show();
        }

        private void Show()
        {
            _host.Clear(); _gauge = null; _activeFill = null;
            ResetCascade(0.1f);
            if (_tab == 0) BuildResearch(); else BuildEngine();
        }

        private void BuildResearch()
        {
            var gc = Ui.Gc;
            var rm = gc.Research;
            var top = Kit.Row(Align.Stretch);
            _host.Add(top);

            var pp = Kit.Tile(16); pp.style.width = 260; pp.style.marginRight = 14; Kit.Pad(pp, 14, 16);
            var pr = Kit.Row();
            pr.Add(Kit.Badge(Kit.IcFlask, Kit.Teal, 40));
            var pv = Kit.Text(rm.Points.ToString(), 34, Kit.Ink, true); pv.style.marginLeft = 12;
            pr.Add(pv);
            var pu = Kit.Text("PP", 14, Kit.Soft, true); pu.style.marginLeft = 6; pu.style.marginTop = 12;
            pr.Add(pu);
            pp.Add(pr);
            var b = BalanceProvider.Get<ResearchBalanceSO>();
            var pn = Kit.Wrap("+" + (b != null ? b.PpPerDevelopmentWeek : 2) + " por semana desenvolvendo · +" + (b != null ? b.PpPerGameLaunched : 5) + " por jogo lançado", 11, Kit.Soft);
            pn.style.marginTop = 6;
            pp.Add(pn);
            top.Add(Enter(pp));

            var active = rm.Active;
            var slot = active != null ? Kit.Outline(16) : Kit.Tile(16);
            slot.style.flexGrow = 1; Kit.Pad(slot, 14, 16);
            if (active != null)
            {
                Kit.BorderColor(slot, Kit.Teal);
                var s1 = Kit.Row();
                s1.Add(Kit.Badge(Labels.ResearchIcon(active), Kit.Teal, 40));
                var s1t = Kit.Col(); s1t.style.marginLeft = 12; s1t.style.flexGrow = 1;
                s1t.Add(Kit.Text("Pesquisando agora", 11, Kit.Soft, true));
                s1t.Add(Kit.Text(active.Label, 15, Kit.Ink, true));
                s1.Add(s1t);
                s1.Add(Kit.Text(Kit.Money(gc.Run.research.activeWeeklyCost) + " / sem", 12, Kit.Red, true));
                slot.Add(s1);
                var bar = Kit.Bar(10, Kit.Teal, out _activeFill, rm.ActiveProgress01); bar.style.marginTop = 10;
                slot.Add(bar);
                var s1n = Kit.Text("faltam " + rm.ActiveWeeksRemaining + " semana(s)", 11, Kit.Soft); s1n.style.marginTop = 4;
                slot.Add(s1n);
            }
            else
            {
                slot.style.flexDirection = FlexDirection.Row; slot.style.alignItems = Align.Center;
                slot.Add(Kit.Badge(Kit.IcBulb, Kit.Track, 40, Kit.Faint));
                var st = Kit.Col(); st.style.marginLeft = 12;
                st.Add(Kit.Text("Nenhuma pesquisa em andamento", 15, Kit.Ink, true));
                st.Add(Kit.Text("Escolha uma abaixo. Uma por vez.", 11, Kit.Soft));
                slot.Add(st);
            }
            top.Add(Enter(slot));

            var cats = Widgets.Segmented(Categories, _category, i => { _category = i; Rebuild(); }, Kit.Teal);
            cats.style.marginTop = 16; cats.style.marginBottom = 12;
            _host.Add(Enter(cats));

            var grid = LiveBits.Wrap();
            _host.Add(grid);
            var catalog = rm.Catalog;
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i].Kind == Kinds[_category]) grid.Add(Enter(Node(catalog[i])));
        }

        private VisualElement Node(ResearchNode n)
        {
            var gc = Ui.Gc;
            var rm = gc.Research;
            bool unlocked = rm.IsUnlocked(n.Id);
            bool isActive = rm.Active != null && rm.Active.Id == n.Id;
            bool phaseLocked = (int)n.MinPhase > (int)gc.Run.company.phase;
            int cost = rm.CostPP(n);
            bool can = rm.CanResearch(n);

            var c = Kit.Outline(14); c.style.width = 300; c.style.marginRight = 12; c.style.marginBottom = 12; Kit.Pad(c, 12, 14);
            c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center;
            Color col = unlocked ? Kit.Green : phaseLocked ? Kit.Faint : Kit.Teal;
            if (isActive) Kit.BorderColor(c, Kit.Teal);
            c.Add(Kit.Badge(phaseLocked ? Kit.IcLock : Labels.ResearchIcon(n), Kit.A(col, 0.16f), 40, col));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Wrap(n.Label, 13, phaseLocked ? Kit.Faint : Kit.Ink, true));
            string detail = cost + " PP · " + Kit.Money(rm.WeeklyCost(n)) + "/sem · " + rm.WeeksToComplete(n) + " sem";
            if (n.Kind == UnlockKind.Engine)
                detail = "+" + EngineCalculator.ModuleBudget(n.ModuleWeight, BalanceProvider.Get<EngineBalanceSO>()) + " EB · " + detail;
            t.Add(Kit.Wrap(detail, 10, Kit.Soft));
            c.Add(t);

            if (unlocked) c.Add(Kit.Badge(Kit.IcCheck, Kit.Green, 26));
            else if (isActive) c.Add(Kit.Chip("Pesquisando", Kit.Teal, true));
            else if (phaseLocked) { c.Add(Kit.Chip("Fase " + Labels.Phase(n.MinPhase), Kit.Soft)); c.style.opacity = 0.75f; }
            else
            {
                var start = Kit.RoundButton(Kit.IcPlus, Kit.Teal, Color.white, 30, () =>
                {
                    if (rm.StartResearch(n.Id)) { Sfx.Play(SfxId.Success); Rebuild(); } else Sfx.Play(SfxId.Fail);
                });
                LiveBits.SetEnabled(start, can);
                c.Add(start);
            }
            return c;
        }

        private void BuildEngine()
        {
            var gc = Ui.Gc;
            var rm = gc.Research;
            var engine = BalanceProvider.Get<EngineBalanceSO>();
            int eb = rm.EngineBudget;

            var row = Kit.Row(Align.Stretch);
            _host.Add(row);
            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);
            left.Add(Kit.Caps("Módulos instalados", 11, Kit.Soft));
            int modules = 0;
            foreach (var n in rm.Catalog)
            {
                if (n.Kind != UnlockKind.Engine || !rm.IsUnlocked(n.Id)) continue;
                modules++;
                var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 8; Kit.Pad(r, 10, 14);
                r.Add(Kit.Badge(Kit.IcChip, Kit.A(Kit.Sky, 0.16f), 38, Kit.Sky));
                var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1;
                t.Add(Kit.Text((n.Group ?? "").ToUpperInvariant(), 9, Kit.Soft, true));
                t.Add(Kit.Text(n.Label, 14, Kit.Ink, true));
                r.Add(t);
                r.Add(Kit.Text("+" + EngineCalculator.ModuleBudget(n.ModuleWeight, engine) + " EB · " + Kit.Money(EngineCalculator.ModuleWeeklyCost(n.ModuleWeight, engine)) + "/sem", 11, Kit.Soft, true));
                left.Add(Enter(r));
            }
            var hint = LiveBits.Note(Kit.IcInfo, Kit.Blue,
                "A engine da empresa junta todos os módulos pesquisados. Quanto mais Engine Budget, melhor a nota em escopos maiores — e maior o custo semanal enquanto há jogo em produção. Novos módulos ficam na aba Pesquisa → Engine.");
            hint.style.marginTop = 12;
            left.Add(Enter(hint));

            row.Add(Kit.Gap(20));
            var right = Kit.Tile(18); right.style.width = 420; right.style.flexShrink = 0; right.style.alignItems = Align.Center; Kit.Pad(right, 18, 20);
            right.Add(Kit.Caps("Engine da " + gc.Run.company.name, 11, Kit.Soft));
            var gaugeWrap = new VisualElement(); gaugeWrap.style.width = 210; gaugeWrap.style.height = 210; gaugeWrap.style.marginTop = 12;
            gaugeWrap.style.alignItems = Align.Center; gaugeWrap.style.justifyContent = Justify.Center;
            _gauge = Kit.Fill(); _gauge.generateVisualContent += DrawGauge;
            gaugeWrap.Add(_gauge);
            gaugeWrap.Add(Kit.Text(eb.ToString(), 44, Kit.Ink, true));
            gaugeWrap.Add(Kit.Text("ENGINE BUDGET", 10, Kit.Soft, true));
            right.Add(gaugeWrap);
            int complete = engine != null ? engine.CompleteEngineBudget : 2000;
            _gaugeTarget = Mathf.Clamp01(eb / (float)complete);
            _gaugeFill = 0f;

            var refs = Kit.Col(); refs.style.alignSelf = Align.Stretch; refs.style.marginTop = 12;
            foreach (ProjectSize size in new[] { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large, ProjectSize.AAA })
            {
                int reference = engine != null ? engine.ReferenceBudgetFor(size) : 200;
                bool ok = eb >= reference;
                refs.Add(LiveBits.Line(ok ? Kit.IcCheck : Kit.IcClose, "Projetos " + Labels.Size(size), reference + " EB", ok ? Kit.Green : Kit.Red));
            }
            right.Add(refs);
            var stats = Kit.Row(); stats.style.alignSelf = Align.Stretch; stats.style.justifyContent = Justify.SpaceBetween; stats.style.marginTop = 10;
            stats.Add(Small("Módulos", modules.ToString()));
            stats.Add(Small("Custo em produção", Kit.Money(rm.EngineWeeklyCost) + "/sem"));
            right.Add(stats);
            row.Add(Enter(right, 0.3f));
        }

        private static VisualElement Small(string cap, string val)
        {
            var c = Kit.Col(); c.style.alignItems = Align.Center;
            c.Add(Kit.Text(val, 18, Kit.Ink, true));
            c.Add(Kit.Text(cap, 10, Kit.Soft, true));
            return c;
        }

        protected override void OnTick(float dt)
        {
            if (_activeFill != null) _activeFill.style.opacity = 0.75f + 0.25f * Mathf.Sin(Clock * 4f);
            if (_gauge == null) return;
            _gaugeFill = Mathf.MoveTowards(_gaugeFill, _gaugeTarget, dt * 0.8f);
            _gauge.MarkDirtyRepaint();
        }

        private void DrawGauge(MeshGenerationContext mgc)
        {
            var r = _gauge.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center; float rad = r.width * 0.5f - 12f;
            p.lineCap = LineCap.Round; p.lineWidth = 16f;
            p.strokeColor = Kit.Track;
            p.BeginPath(); p.Arc(c, rad, new Angle(135f), new Angle(405f)); p.Stroke();
            if (_gaugeFill <= 0.005f) return;
            p.strokeColor = Kit.Sky;
            p.BeginPath(); p.Arc(c, rad, new Angle(135f), new Angle(135f + 270f * _gaugeFill)); p.Stroke();
            Vector2 tip = Kit.OnCircle(c, rad, 135f + 270f * _gaugeFill);
            Kit.Disc(p, tip, 11f + 2f * Mathf.Sin(Clock * 5f), Kit.A(Kit.Sky, 0.3f));
            Kit.Disc(p, tip, 6f, Color.white);
        }
    }

    /// <summary>
    /// Painel Relações (GDD 9 e 10.3). Contratos/parcerias e declarações do CEO ainda não têm
    /// sistema por trás — o painel diz isso com honestidade em vez de mostrar botões falsos.
    /// </summary>
    internal sealed class RelationsLive : ManagementScreen
    {
        protected override int PanelIndex => 5;

        protected override void Build()
        {
            var c = Ui.Gc.Run.company;
            var content = Shell("Parcerias, contratos e a sua imagem pública");
            var row = Kit.Row(Align.Stretch);
            content.Add(row);

            var rep = Kit.Tile(18); rep.style.width = 360; rep.style.flexShrink = 0; Kit.Pad(rep, 18, 18);
            rep.Add(Kit.Caps("Reputação", 11, Kit.Soft));
            var rv = Kit.Text(Mathf.RoundToInt(c.reputation01 * 100f) + " / 100", 34, Kit.Ink, true); rv.style.marginTop = 6;
            rep.Add(rv);
            var bar = Kit.Bar(12, c.reputation01 >= 0.5f ? Kit.Green : Kit.Gold, out _, c.reputation01); bar.style.marginTop = 8;
            rep.Add(bar);
            var rn = Kit.Wrap("Sobe com boas notas e boas decisões em eventos. Pesa no valuation.", 12, Kit.Soft); rn.style.marginTop = 10;
            rep.Add(rn);
            row.Add(Enter(rep));
            row.Add(Kit.Gap(18));

            var soon = Kit.Col(); soon.style.flexGrow = 1;
            row.Add(soon);
            soon.Add(Kit.Caps("Em construção", 11, Kit.Soft));
            soon.Add(Enter(Coming(Kit.IcContract, "Contratos e parcerias", "Publicação, outsourcing, licenciamento de engine, exclusividade, co-desenvolvimento, patrocínio e IP externo — com prazo, multa e nota mínima.")));
            soon.Add(Enter(Coming(Kit.IcBullhorn, "Declarações do CEO", "Falar ao público com impacto previsto e risco de sair pela culatra.")));
            soon.Add(Enter(LiveBits.Note(Kit.IcInfo, Kit.Blue, "Por enquanto, a relação com o mundo acontece pelos eventos narrativos e pelos rivais (painel Mercado).")));
        }

        private static VisualElement Coming(string icon, string title, string text)
        {
            var card = Kit.Outline(16); card.style.marginTop = 10; card.style.marginBottom = 4; Kit.Pad(card, 14, 16);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center; card.style.opacity = 0.85f;
            card.Add(Kit.Badge(icon, Kit.Track, 42, Kit.Faint));
            var t = Kit.Col(); t.style.marginLeft = 14; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Text(title, 15, Kit.Ink, true));
            t.Add(Kit.Wrap(text, 12, Kit.Soft));
            card.Add(t);
            card.Add(Kit.Chip("Em breve", Kit.Soft, false, Kit.IcClock));
            return card;
        }
    }

    /// <summary>Painel Fundador vivo (GDD 14 / 20.2 / 20.5): habilidades permanentes, perks e conquistas.</summary>
    internal sealed class FounderLive : ManagementScreen
    {
        private FounderArt _art;

        protected override int PanelIndex => 6;

        protected override void Build()
        {
            var gc = Ui.Gc;
            var meta = gc.Meta;
            var content = Shell("Suas habilidades destravam opções nos eventos e mudam as regras a seu favor");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            content.Add(row);

            var left = Kit.Col(); left.style.width = 360; left.style.flexShrink = 0;
            row.Add(left);
            var stage = Kit.Tile(18); stage.style.height = 320; stage.style.alignItems = Align.Center; stage.style.justifyContent = Justify.FlexEnd;
            stage.style.overflow = Overflow.Hidden; Kit.Pad(stage, 0, 0);
            stage.style.backgroundColor = Kit.A(FounderArt.Shirts[Mathf.Abs(meta.founder.shirt) % FounderArt.Shirts.Length], 0.16f);
            _art = new FounderArt(300) { Skin = meta.founder.skin, Hair = meta.founder.hair, HairStyle = meta.founder.hairStyle, Shirt = meta.founder.shirt, Accessory = meta.founder.accessory };
            stage.Add(_art);
            var plate = Kit.Card(0, 12); plate.style.position = Position.Absolute; plate.style.bottom = 14; plate.style.alignItems = Align.Center; Kit.Pad(plate, 8, 18);
            plate.Add(Kit.Text(meta.founder.name, 18, Kit.Ink, true));
            plate.Add(Kit.Text("CEO · " + gc.Run.company.name + " · Run " + gc.Run.runNumber, 11, Kit.Soft));
            stage.Add(plate);
            left.Add(Enter(stage));

            var wallet = Kit.Tile(16); wallet.style.marginTop = 12; Kit.Pad(wallet, 14, 16);
            wallet.Add(LiveBits.Line(Kit.IcCoins, "Dinheiro pessoal", Kit.Money(meta.personalMoney), Kit.Cash));
            wallet.Add(LiveBits.Line(Kit.IcHourglass, "Pontos de Legado", meta.legacyPoints + " PL", Kit.Gold));
            wallet.Add(LiveBits.Line(Kit.IcFlag, "Runs concluídas", meta.runsCompleted.ToString()));
            wallet.Add(LiveBits.Line(Kit.IcTrophy, "Melhor venda", Kit.Money(meta.bestSale)));
            left.Add(Enter(wallet));

            var ach = Kit.Tile(16); ach.style.marginTop = 12; Kit.Pad(ach, 14, 16);
            int total = 0;
            foreach (string _ in AchievementManager.AllIds) total++;
            ach.Add(Kit.Text("Conquistas " + meta.achievements.Count + " / " + total, 13, Kit.Ink, true));
            var ar = LiveBits.Wrap(); ar.style.marginTop = 8;
            foreach (string id in AchievementManager.AllIds)
            {
                bool has = meta.achievements.Contains(id);
                var b = Kit.Badge(Kit.IcTrophy, has ? Kit.Gold : Kit.Track, 26, has ? Color.white : Kit.Faint);
                b.style.marginRight = 6; b.style.marginBottom = 6;
                if (AchievementManager.TryGetInfo(id, out string title, out string desc)) b.tooltip = title + " — " + desc;
                ar.Add(b);
            }
            ach.Add(ar);
            left.Add(Enter(ach));

            row.Add(Kit.Gap(20));
            var right = new ScrollView(); right.style.flexGrow = 1;
            row.Add(right);
            right.Add(Kit.Caps("Habilidades permanentes", 11, Kit.Soft));
            foreach (var tree in SkillTreeCatalog.All) right.Add(Enter(Skill(tree, meta)));
            var foot = LiveBits.Note(Kit.IcHourglass, Kit.Gold, "As habilidades são compradas na árvore ao encerrar a run, com o dinheiro da venda e os Pontos de Legado.");
            foot.style.marginTop = 10;
            right.Add(Enter(foot));
        }

        private static VisualElement Skill(SkillTreeDef tree, State.Meta.MetaProgressionState meta)
        {
            var style = Labels.SkillStyle(tree.Skill);
            int level = FounderPerks.Level(tree.Skill);
            var r = Kit.Outline(14); r.style.marginTop = 8; Kit.Pad(r, 11, 16);
            var top = Kit.Row();
            top.Add(Kit.Badge(style.icon, style.color, 42));
            var t = Kit.Col(); t.style.marginLeft = 14; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Text(tree.Name, 15, Kit.Ink, true));
            t.Add(Kit.Wrap(tree.RootName + ": " + tree.RootDescription, 11, Kit.Soft));
            top.Add(t);
            var lv = Kit.Text("Nv " + level, 18, level > 0 ? style.color : Kit.Faint, true); lv.style.marginLeft = 10;
            top.Add(lv);
            r.Add(top);
            var perks = Kit.Row(); perks.style.marginTop = 8; perks.style.flexWrap = Wrap.Wrap;
            foreach (var perk in tree.Perks)
            {
                bool has = meta.perks.Contains(perk.Id);
                var chip = Kit.Chip(perk.Name, has ? style.color : Kit.Faint, has, has ? Kit.IcCheck : null);
                chip.style.marginRight = 6; chip.style.marginBottom = 4; chip.tooltip = perk.Description;
                perks.Add(chip);
            }
            r.Add(perks);
            return r;
        }

        protected override void OnTick(float dt) => _art.Tick(dt);
    }
}
