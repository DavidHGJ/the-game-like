using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Projects;
using GameLike.Systems.Research;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Quanta gente o jogador contrata.</summary>
    public enum TeamStrategy
    {
        /// <summary>Só o que o tamanho de jogo da fase pede.</summary>
        Lean,
        /// <summary>Enche a fase até o máximo de pessoas.</summary>
        Full
    }

    /// <summary>Um tipo de jogador: quão bem ele faz jogos e como administra a empresa.</summary>
    public sealed class CareerProfile
    {
        public string name = "";
        /// <summary>Nota média dos jogos que esse jogador faz (com equipe adequada).</summary>
        public float skillMean = 6.5f;
        /// <summary>Quanto as notas variam de um jogo para outro (desvio padrão).</summary>
        public float skillSpread = 1.3f;
        public TeamStrategy team = TeamStrategy.Lean;
        public Seniority hireSeniority = Seniority.Junior;
        /// <summary>Semanas de gasto da fase nova que o jogador quer ter em caixa, além do custo, para expandir.</summary>
        public int expansionCushionWeeks = 12;
        /// <summary>true = vende a empresa quando está prestes a falir (não perde tudo).</summary>
        public bool sellsBeforeBankruptcy = true;
        /// <summary>Níveis das raízes da árvore do fundador (mesmos efeitos do jogo).</summary>
        public int businessLevel, marketingLevel, charismaLevel, technicalLevel;
    }

    /// <summary>Como uma run terminou.</summary>
    public enum CareerRunEnd
    {
        Bankrupt,
        SoldBeforeBankruptcy,
        TimeLimit
    }

    /// <summary>Resultado de uma run dentro de uma carreira.</summary>
    public sealed class CareerRun
    {
        public int index;
        public long startingCash;
        public CompanyPhase maxPhase;
        /// <summary>Semana em que chegou a cada fase (índice = fase; 0 = não chegou).</summary>
        public int[] phaseWeek = new int[4];
        public int weeks;
        public int games;
        public int hits;
        public float averageScore;
        public long peakValuation;
        /// <summary>Dinheiro que o fundador leva para a próxima run (0 se faliu).</summary>
        public long payout;
        public CareerRunEnd end;
    }

    /// <summary>
    /// Simulação longa (GDD 23.14): uma CARREIRA = várias runs seguidas. Cada run vai da Garagem
    /// até a falência, a venda ou o limite de tempo, semana a semana e com as mesmas regras e
    /// valores do jogo — custos fixos, salários, engine, pesquisa de tamanhos (PP e custo semanal),
    /// inflação/variação, hype/wishlists, vendas com cauda, reputação, valuation real
    /// (<see cref="ValuationCalculator"/>), liberação e custo de expansão, estagnação e falência.
    /// O dinheiro da venda vira o capital da run seguinte, como no jogo. As notas dos jogos são
    /// sorteadas pelo perfil do jogador e ajustadas pela equipe (cobertura de escopo e senioridade).
    /// </summary>
    public static class CareerSimulator
    {
        private const float ReviewerReputationPerPoint = 0.03f;
        /// <summary>Licença da plataforma inicial (PC) paga a cada jogo.</summary>
        private const long LicenseCost = 5_000;
        private static readonly string[] Genres = { "rpg", "action_rpg", "platformer", "puzzle", "simulation", "strategy", "fps" };

        private sealed class Selling
        {
            public long forecastUnits;
            public float retention;
            public int price;
            public int week;
            public bool awaitingReview;
            public float score;
        }

        private sealed class Project
        {
            public ProjectSize size;
            public float score;
            public int weeksLeft;
            public float hype;
            public long wishlists;
        }

        /// <summary>Roda uma carreira de <paramref name="runs"/> runs com o gerador dado.</summary>
        public static List<CareerRun> RunCareer(CareerProfile profile, int runs, int maxWeeksPerRun, System.Random rng)
        {
            var result = new List<CareerRun>();
            long personalMoney = 0;
            for (int i = 0; i < runs; i++)
            {
                var run = RunOnce(profile, personalMoney, maxWeeksPerRun, rng);
                run.index = i + 1;
                result.Add(run);
                personalMoney = run.payout;
            }
            return result;
        }

        /// <summary>Uma run completa, começando com o dinheiro inicial do jogo + o capital pessoal.</summary>
        public static CareerRun RunOnce(CareerProfile profile, long personalCapital, int maxWeeks, System.Random rng)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            var marketing = BalanceProvider.Get<MarketingBalanceSO>();
            var quality = BalanceProvider.Get<QualityFormulaBalanceSO>();
            var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
            var engine = BalanceProvider.Get<EngineBalanceSO>();
            var research = BalanceProvider.Get<ResearchBalanceSO>();

            var company = new CompanyState("Sim", loop.StartingCash + Math.Max(0, personalCapital));
            company.reputation01 = Mathf.Clamp01(company.reputation01 + 0.02f * profile.charismaLevel);
            float spread = tuning.RunCostVariationPercent / 100f;
            company.costVariation = 1f + (float)(rng.NextDouble() * 2 - 1) * spread;

            var run = new CareerRun { startingCash = company.cash, maxPhase = CompanyPhase.Garage };
            var released = new List<ProjectState>();
            var selling = new List<Selling>();
            var unlocked = new HashSet<ProjectSize> { ProjectSize.Small };
            Project current = null;
            ProjectSize researching = ProjectSize.Small;
            int researchWeeksLeft = 0;
            long researchWeeklyCost = 0;
            float pp = 0;
            int debtWeeks = 0;
            float scoreSum = 0;
            long engineUpkeep = EngineCalculator.ModuleWeeklyCost(EngineModuleWeight.Light, engine) * 2;
            float valuationMultiplier = 1f + 0.05f * profile.businessLevel;
            float hypeMultiplier = 1f + 0.05f * profile.marketingLevel;
            float ppMultiplier = 1f + 0.10f * profile.technicalLevel;

            for (int week = 1; week <= maxWeeks; week++)
            {
                company.weeksElapsed = week;
                double costFactor = tuning.InflationFactor(week) * company.costVariation;
                long Cost(long amount) => (long)Math.Round(amount * costFactor);

                // contrata o que a estratégia pede para a fase atual
                Hire(company, profile, loop, economy, unlocked);

                company.cash -= Cost(economy.WeeklyOperatingFor(company.phase));
                long payroll = 0;
                foreach (var e in company.employees) payroll += e.weeklySalary;
                company.cash -= Cost(payroll);

                // pesquisa de tamanho
                if (researchWeeksLeft > 0)
                {
                    company.cash -= Cost(researchWeeklyCost);
                    if (--researchWeeksLeft == 0)
                    {
                        unlocked.Add(researching);
                        pp += research.PpPerResearchCompleted * ppMultiplier;
                    }
                }
                else TryStartResearch(company.phase, unlocked, research, ref pp, ref researching, ref researchWeeksLeft, ref researchWeeklyCost);

                // vendas e críticas
                foreach (var s in selling)
                {
                    if (s.awaitingReview)
                    {
                        s.awaitingReview = false;
                        company.reputation01 = Mathf.Clamp01(company.reputation01 + (s.score - 6f) * ReviewerReputationPerPoint);
                        continue;
                    }
                    if (s.week >= loop.SalesTailWeeks) continue;
                    long units = SalesCalculator.TailUnitsForWeek(s.forecastUnits, s.week, loop.SalesTailWeeks, s.retention);
                    long revenue = units * s.price;
                    s.week++;
                    company.cash += revenue;
                    company.lifetimeRevenue += revenue;
                }

                // produção — fora da Garagem, se o tamanho da fase está sendo pesquisado, o jogador
                // espera a pesquisa em vez de começar um jogo menor (que não se paga numa fase cara)
                bool waitForResearch = current == null && company.phase != CompanyPhase.Garage
                                       && !unlocked.Contains(MainSize(company.phase)) && researchWeeksLeft > 0;
                if (current == null && !waitForResearch)
                {
                    var size = BiggestSize(company.phase, unlocked);
                    current = new Project
                    {
                        size = size,
                        weeksLeft = EconomySimulator.DevelopmentWeeks(size, loop),
                        score = SampleScore(profile, company, size, loop, rng)
                    };
                    company.cash -= Cost(LicenseCost);
                }
                if (current != null)
                {
                    company.cash -= Cost(engineUpkeep);
                    pp += research.PpPerDevelopmentWeek * ppMultiplier;
                    current.hype = Mathf.Clamp01(current.hype * (1f - marketing.HypeDecayPerWeek) + marketing.PassiveHypeGainPerWeek * hypeMultiplier);
                    current.wishlists += (long)(current.hype * (loop.BaseWishlistsFor(current.size) / Mathf.Max(1, loop.WeeksFor(current.size))));
                    if (--current.weeksLeft <= 0)
                    {
                        var sales = SalesCalculator.Compute(current.score, current.size, current.hype, current.wishlists, loop, quality);
                        selling.Add(new Selling
                        {
                            forecastUnits = sales.units,
                            retention = SalesCalculator.TailRetention(current.score, loop),
                            price = loop.PriceFor(current.size),
                            awaitingReview = true,
                            score = current.score
                        });
                        var project = new ProjectState("Jogo " + (run.games + 1), Genres[run.games % Genres.Length], "", current.size, null)
                        {
                            finalScore = current.score
                        };
                        released.Add(project);
                        company.gamesReleased++;
                        string genre = Genres[run.games % Genres.Length];
                        if (!company.genresReleased.Contains(genre)) company.genresReleased.Add(genre);
                        pp += research.PpPerGameLaunched * ppMultiplier;
                        run.games++;
                        if (current.score >= 7f) run.hits++;
                        scoreSum += current.score;
                        current = null;
                    }
                }

                // valuation, expansão e estagnação (GameController)
                double valuation = ValuationCalculator.Compute(company, released) * valuationMultiplier;
                if (company.expansionAvailable)
                {
                    company.stagnationWeeks++;
                    valuation *= 1f - Mathf.Min(economy.StagnationLossMax, company.stagnationWeeks * economy.StagnationLossPerWeek);
                }
                company.valuation = (long)Math.Max(0, valuation);
                if (company.valuation > run.peakValuation) run.peakValuation = company.valuation;
                if (!company.expansionAvailable && company.phase != CompanyPhase.Studio
                    && company.valuation >= economy.ThresholdAfter(company.phase))
                {
                    company.expansionAvailable = true;
                    company.stagnationWeeks = 0;
                }
                if (company.expansionAvailable) TryExpand(company, profile, economy, loop, run, week, Cost);

                // falência (com a saída de vender antes, se o jogador for esperto)
                debtWeeks = company.cash < 0 ? debtWeeks + 1 : 0;
                run.weeks = week;
                if (debtWeeks > tuning.BankruptcyGraceWeeks)
                {
                    run.end = CareerRunEnd.Bankrupt;
                    run.payout = 0;
                    break;
                }
                if (profile.sellsBeforeBankruptcy && debtWeeks == tuning.BankruptcyGraceWeeks)
                {
                    run.end = CareerRunEnd.SoldBeforeBankruptcy;
                    run.payout = company.valuation;
                    break;
                }
                if (week == maxWeeks)
                {
                    run.end = CareerRunEnd.TimeLimit;
                    run.payout = company.valuation;
                }
            }

            run.averageScore = run.games > 0 ? scoreSum / run.games : 0f;
            return run;
        }

        // ------------------------------------------------------------------ decisões do jogador

        /// <summary>Maior tamanho que a fase "usa" e que já foi pesquisado.</summary>
        private static ProjectSize BiggestSize(CompanyPhase phase, HashSet<ProjectSize> unlocked)
        {
            ProjectSize main = MainSize(phase);
            for (int s = (int)main; s >= 0; s--)
                if (unlocked.Contains((ProjectSize)s)) return (ProjectSize)s;
            return ProjectSize.Small;
        }

        /// <summary>Tamanho principal de cada fase (GDD 23.14).</summary>
        public static ProjectSize MainSize(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Indie => ProjectSize.Medium,
            CompanyPhase.Office => ProjectSize.Large,
            CompanyPhase.Studio => ProjectSize.AAA,
            _ => ProjectSize.Small
        };

        private static void TryStartResearch(CompanyPhase phase, HashSet<ProjectSize> unlocked, ResearchBalanceSO research,
            ref float pp, ref ProjectSize researching, ref int weeksLeft, ref long weeklyCost)
        {
            ProjectSize target = MainSize(phase);
            if (unlocked.Contains(target)) return;
            // GDD/ResearchCatalog: Médio é intermediário; Grande e AAA são avançados.
            bool advanced = target != ProjectSize.Medium;
            int cost = advanced ? research.AdvancedResearchCostPP : research.IntermediateResearchCostPP;
            if (pp < cost) return;
            pp -= cost;
            researching = target;
            weeksLeft = advanced ? research.ResearchWeeksAdvanced : research.ResearchWeeksIntermediate;
            weeklyCost = advanced ? research.AdvancedResearchWeeklyCost : research.IntermediateResearchWeeklyCost;
        }

        private static void Hire(CompanyState company, CareerProfile profile, GameLoopBalanceSO loop, EconomyBalanceSO economy,
            HashSet<ProjectSize> unlocked)
        {
            int maxTeam = loop.MaxTeamForPhase(company.phase);
            int wanted = profile.team == TeamStrategy.Full
                ? maxTeam
                : Mathf.Min(maxTeam, loop.TeamFor(BiggestSize(company.phase, unlocked)));
            while (company.employees.Count + 1 < wanted)
            {
                company.employees.Add(new EmployeeInstance("e" + company.employees.Count, "Sim", Specialty.Design,
                    profile.hireSeniority, PersonalityTrait.None, Salary(profile.hireSeniority, economy)));
            }
        }

        private static long Salary(Seniority seniority, EconomyBalanceSO economy) => seniority switch
        {
            Seniority.Mid => economy.MidWeeklySalary,
            Seniority.Senior => economy.SeniorWeeklySalary,
            Seniority.Star => economy.StarWeeklySalary,
            _ => economy.JuniorWeeklySalary
        };

        private static void TryExpand(CompanyState company, CareerProfile profile, EconomyBalanceSO economy, GameLoopBalanceSO loop,
            CareerRun run, int week, Func<long, long> cost)
        {
            CompanyPhase next = company.phase + 1;
            long expansion = cost(economy.ExpansionCostTo(next));
            int nextTeam = profile.team == TeamStrategy.Full ? loop.MaxTeamForPhase(next) : loop.TeamFor(MainSize(next));
            long nextBurn = cost(economy.WeeklyOperatingFor(next) + Salary(profile.hireSeniority, economy) * Mathf.Max(0, nextTeam - 1));
            if (company.cash < expansion + nextBurn * profile.expansionCushionWeeks) return;
            company.cash -= expansion;
            company.phase = next;
            company.expansionAvailable = false;
            company.stagnationWeeks = 0;
            run.phaseWeek[(int)next] = week;
            if (next > run.maxPhase) run.maxPhase = next;
        }

        /// <summary>
        /// Nota de um jogo: sorteada pelo perfil e ajustada pela equipe — cobertura de escopo (o
        /// componente de 40% da fórmula cai até a metade com equipe pequena demais, GDD 19.7) e
        /// nível da equipe (componente de 15%).
        /// </summary>
        private static float SampleScore(CareerProfile profile, CompanyState company, ProjectSize size, GameLoopBalanceSO loop, System.Random rng)
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            float score = profile.skillMean + (float)normal * profile.skillSpread;

            float coverage = ProductionCalculator.ScopeCoverage01(company.employees.Count + 1, size, loop);
            score -= 0.40f * score * (1f - Mathf.Lerp(0.5f, 1f, coverage));
            float teamLevel = company.employees.Count == 0 ? 0.5f : SeniorityLevel(company.employees);
            score += 0.15f * 10f * (teamLevel - 0.5f);
            return Mathf.Clamp(score, 1f, 10f);
        }

        private static float SeniorityLevel(List<EmployeeInstance> staff)
        {
            float sum = 0;
            foreach (var e in staff) sum += 0.35f + 0.2f * (int)e.seniority;
            return sum / staff.Count;
        }
    }
}
