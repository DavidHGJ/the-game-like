using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State.Staff;
using GameLike.Systems.Marketing;
using GameLike.Systems.Projects;
using GameLike.Systems.Research;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Quanto o jogador investe em marketing durante o desenvolvimento, no simulador.</summary>
    public enum SimMarketing
    {
        None,
        Light,
        Full
    }

    /// <summary>Um cenário de simulação: em que fase a empresa está, que jogos faz e com que nota.</summary>
    public sealed class SimScenario
    {
        public string name = "";
        public CompanyPhase phase = CompanyPhase.Garage;
        public ProjectSize size = ProjectSize.Small;
        /// <summary>Nota de cada jogo, em ordem (um jogo por item).</summary>
        public float[] scores = { 7f };
        /// <summary>Funcionários contratados (o fundador não recebe salário).</summary>
        public int employees;
        public Seniority employeeSeniority = Seniority.Junior;
        /// <summary>Caixa no início. Negativo = o dinheiro inicial do balanceamento.</summary>
        public long startingCash = -1;
        /// <summary>Proporção bugs ÷ pontos dos jogos (acelera a queda das vendas, GDD 23.19). Típico ≈ 0,07.</summary>
        public float bugRatio = 0.07f;
        /// <summary>Licença da plataforma paga ao começar cada jogo (PC = 5.000 no conteúdo atual).</summary>
        public long licenseCost = 5_000;
        public SimMarketing marketing = SimMarketing.None;
        /// <summary>Módulos de engine leves que a empresa tem (a engine inicial tem 2).</summary>
        public int lightEngineModules = 2;
        /// <summary>Fator de custo da run (variação por run do GDD 23.12). 1 = custo base.</summary>
        public float costVariation = 1f;
        /// <summary>true = ignora a falência (mede só se os jogos se pagam; o caixa mínimo vira o capital necessário).</summary>
        public bool ignoreBankruptcy;
        /// <summary>true = encerra na semana em que o último jogo é lançado (mede se o jogador consegue chegar lá).</summary>
        public bool stopAtLastLaunch;
        /// <summary>Limite de segurança de semanas simuladas.</summary>
        public int maxWeeks = 520;
    }

    /// <summary>Números de um jogo dentro da simulação.</summary>
    public sealed class SimGame
    {
        public float score;
        public int devWeeks;
        public int launchWeek;
        public long units;
        public long revenue;
        /// <summary>Unidades e receita que o jogo venderia na cauda inteira (mesmo se a empresa quebrar antes).</summary>
        public long forecastUnits;
        public long forecastRevenue;
        /// <summary>Tudo o que a empresa gastou enquanto o jogo estava em produção.</summary>
        public long devCost;
        public float hypeAtLaunch;
        public long wishlists;
        public long Profit => revenue - devCost;
        /// <summary>Lucro do jogo com a venda completa prevista (receita prevista − custo de produção).</summary>
        public long ForecastProfit => forecastRevenue - devCost;
    }

    /// <summary>Resultado de um cenário.</summary>
    public sealed class SimResult
    {
        public SimScenario scenario;
        public readonly List<SimGame> games = new List<SimGame>();
        /// <summary>Caixa no fim de cada semana (índice 0 = semana 1).</summary>
        public readonly List<long> cashByWeek = new List<long>();
        public long startingCash;
        public long finalCash;
        public long minCash;
        public int minCashWeek;
        /// <summary>Semana da falência (0 = não faliu).</summary>
        public int bankruptWeek;
        /// <summary>Semana da primeira receita de vendas (0 = não vendeu).</summary>
        public int firstSaleWeek;
        public int weeks;
        public bool Bankrupt => bankruptWeek > 0;
        public long TotalRevenue { get { long s = 0; foreach (var g in games) s += g.revenue; return s; } }
        public long Net => finalCash - startingCash;
    }

    /// <summary>
    /// Simulação rápida da economia, sem cena: roda semana a semana com as MESMAS fórmulas e os
    /// mesmos valores de balanceamento do jogo — custo fixo da fase, folha, engine, licença,
    /// inflação/variação, hype e wishlists acumulados, vendas de lançamento, críticas na semana
    /// seguinte, cauda de vendas semanal e a regra de falência. A nota de cada jogo é dada pelo
    /// cenário (é o que se quer testar), sem o sorteio das casas de crítica. Serve para validar
    /// o balanceamento em segundos, em todas as faixas de nota.
    /// </summary>
    public static class EconomySimulator
    {
        private sealed class Running
        {
            public SimGame game;
            public long firstWeekUnits;
            public float nextUnits;
            public float decay;
            public bool ended;
            public int price;
            public int salesWeek;
            public bool awaitingReview;
        }

        /// <summary>Roda o cenário e devolve a linha do tempo e os números de cada jogo.</summary>
        public static SimResult Run(SimScenario scenario)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            var marketing = BalanceProvider.Get<MarketingBalanceSO>();
            var quality = BalanceProvider.Get<QualityFormulaBalanceSO>();
            var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
            var engine = BalanceProvider.Get<EngineBalanceSO>();

            var result = new SimResult { scenario = scenario };
            long cash = scenario.startingCash >= 0 ? scenario.startingCash : loop.StartingCash;
            result.startingCash = cash;
            result.minCash = cash;

            long salary = SalaryFor(scenario.employeeSeniority, economy) * Mathf.Max(0, scenario.employees);
            long engineUpkeep = EngineCalculator.ModuleWeeklyCost(EngineModuleWeight.Light, engine) * Mathf.Max(0, scenario.lightEngineModules);
            int grace = tuning.BankruptcyGraceWeeks;

            var selling = new List<Running>();
            int nextGame = 0;
            SimGame current = null;
            int devWeeksLeft = 0, devWeeksTotal = 0, devWeekIndex = 0;
            float hype = 0f;
            long wishlists = 0;
            int debtWeeks = 0;

            for (int week = 1; week <= scenario.maxWeeks; week++)
            {
                float costFactor = tuning.InflationFactor(week) * (scenario.costVariation > 0f ? scenario.costVariation : 1f);
                long Cost(long amount) => (long)Math.Round(amount * (double)costFactor);

                // começa o próximo jogo assim que não há nenhum em produção
                if (current == null && nextGame < scenario.scores.Length)
                {
                    current = new SimGame { score = Mathf.Clamp(scenario.scores[nextGame], 0f, 10f) };
                    devWeeksTotal = DevelopmentWeeks(scenario.size, loop);
                    devWeeksLeft = devWeeksTotal;
                    devWeekIndex = 0;
                    current.devWeeks = devWeeksTotal;
                    hype = 0f;
                    wishlists = 0;
                    long license = Cost(scenario.licenseCost);
                    cash -= license;
                    current.devCost += license;
                    nextGame++;
                }

                long spentThisWeek = Cost(economy.WeeklyOperatingFor(scenario.phase)) + Cost(salary);
                cash -= spentThisWeek;

                // jogos lançados: críticas na semana seguinte, depois vendas semana a semana
                for (int i = 0; i < selling.Count; i++)
                {
                    var r = selling[i];
                    if (r.awaitingReview) { r.awaitingReview = false; continue; }
                    if (r.ended) continue;
                    long units = (long)Math.Round(r.nextUnits);
                    r.nextUnits = SalesCalculator.NextWeekUnits(r.nextUnits, r.decay, 0.5f, loop);
                    if (SalesCalculator.SalesEnded(r.nextUnits, r.firstWeekUnits, loop)) r.ended = true;
                    long revenue = units * r.price;
                    r.salesWeek++;
                    r.game.units += units;
                    r.game.revenue += revenue;
                    cash += revenue;
                    if (revenue > 0 && result.firstSaleWeek == 0) result.firstSaleWeek = week;
                }

                if (current != null)
                {
                    long engineCost = Cost(engineUpkeep);
                    cash -= engineCost;
                    spentThisWeek += engineCost;
                    cash -= RunMarketing(scenario, devWeekIndex, devWeeksTotal, costFactor, marketing, loop, ref hype, ref wishlists, out long campaignCost);
                    spentThisWeek += campaignCost;
                    current.devCost += spentThisWeek;

                    // hype e wishlists crescem a cada semana de produção (MarketingManager)
                    hype = Mathf.Clamp01(hype * (1f - marketing.HypeDecayPerWeek) + marketing.PassiveHypeGainPerWeek);
                    long potential = loop.BaseWishlistsFor(scenario.size) / Mathf.Max(1, loop.WeeksFor(scenario.size));
                    wishlists += (long)(hype * potential);

                    devWeekIndex++;
                    if (--devWeeksLeft <= 0)
                    {
                        selling.Add(Launch(current, scenario.size, hype, wishlists, scenario.bugRatio, loop, quality, week));
                        result.games.Add(current);
                        current = null;
                    }
                }

                result.cashByWeek.Add(cash);
                if (cash < result.minCash) { result.minCash = cash; result.minCashWeek = week; }

                debtWeeks = cash < 0 ? debtWeeks + 1 : 0;
                if (!scenario.ignoreBankruptcy && debtWeeks > grace)
                {
                    result.bankruptWeek = week;
                    break;
                }

                bool allLaunched = nextGame >= scenario.scores.Length && current == null;
                if (scenario.stopAtLastLaunch && allLaunched) break;
                bool allSold = selling.TrueForAll(r => !r.awaitingReview && r.ended);
                if (allLaunched && allSold) break;
            }

            result.finalCash = cash;
            result.weeks = result.cashByWeek.Count;
            return result;
        }

        /// <summary>Soma das semanas das 5 fases, arredondadas como no jogo (adendo 23.2).</summary>
        public static int DevelopmentWeeks(ProjectSize size, GameLoopBalanceSO loop)
        {
            int target = loop.WeeksFor(size);
            int total = 0;
            for (int i = 0; i < ParameterCatalog.PhaseCount; i++)
                total += Mathf.Max(1, Mathf.RoundToInt(target * ParameterCatalog.DurationShare((ProductionPhase)i)));
            return total;
        }

        private static Running Launch(SimGame game, ProjectSize size, float hype, long wishlists, float bugRatio,
            GameLoopBalanceSO loop, QualityFormulaBalanceSO quality, int week)
        {
            int price = loop.PriceFor(size);
            long first = SalesCalculator.FirstWeekUnits(game.score, size, hype, wishlists, price, price, 1f, loop, quality);
            float decay = SalesCalculator.WeeklyDecay(hype, SalesCalculator.Delivery(game.score, loop), bugRatio, loop);
            long expected = (long)SalesCalculator.ExpectedTotalUnits(first, decay, loop);
            game.launchWeek = week;
            game.forecastUnits = expected;
            game.forecastRevenue = expected * price;
            game.hypeAtLaunch = hype;
            game.wishlists = wishlists;
            return new Running
            {
                game = game,
                firstWeekUnits = first,
                nextUnits = first,
                decay = decay,
                ended = first <= 0,
                price = price,
                awaitingReview = true
            };
        }

        /// <summary>
        /// Campanhas no estilo de um jogador: Leve = anúncio na 1ª semana; Completo = anúncio,
        /// campanha de wishlist no meio e demo no fim. Devolve quanto foi gasto.
        /// </summary>
        private static long RunMarketing(SimScenario scenario, int devWeek, int devWeeks, float costFactor,
            MarketingBalanceSO mb, GameLoopBalanceSO loop, ref float hype, ref long wishlists, out long spent)
        {
            spent = 0;
            if (scenario.marketing == SimMarketing.None) return 0;
            long potential = loop.BaseWishlistsFor(scenario.size) / Mathf.Max(1, loop.WeeksFor(scenario.size));

            if (devWeek == 0)
            {
                spent += (long)Math.Round(mb.AnnouncementCost * (double)costFactor);
                hype = Mathf.Clamp01(hype + mb.AnnouncementHype);
            }
            if (scenario.marketing == SimMarketing.Full)
            {
                if (devWeek == devWeeks / 2)
                {
                    spent += (long)Math.Round(mb.WishlistCampaignCost * (double)costFactor);
                    hype = Mathf.Clamp01(hype + mb.WishlistCampaignHype);
                    wishlists += (long)(mb.WishlistCampaignBurst * potential);
                }
                if (devWeek == devWeeks - 2)
                {
                    spent += (long)Math.Round(mb.DemoCost * (double)costFactor);
                    hype = Mathf.Clamp01(hype + mb.DemoHype);
                }
            }
            return spent;
        }

        private static long SalaryFor(Seniority seniority, EconomyBalanceSO economy) => seniority switch
        {
            Seniority.Mid => economy.MidWeeklySalary,
            Seniority.Senior => economy.SeniorWeeklySalary,
            Seniority.Star => economy.StarWeeklySalary,
            _ => economy.JuniorWeeklySalary
        };
    }
}
