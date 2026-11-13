using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using UnityEngine;

namespace GameLike.Systems.Staff
{
    /// <summary>
    /// Autoridade sobre a equipe (GDD 7): mantém o mercado de talentos rotativo,
    /// contrata/demite respeitando o teto de equipe da fase (GDD 7.6), cobra a folha
    /// salarial toda semana (GDD 19.2), e aplica XP/burnout ao longo do tempo. Anuncia
    /// fatos via EventBus; não conhece quem reage. Lê valores de balanceamento a cada
    /// uso via <see cref="BalanceProvider"/>.
    /// </summary>
    public sealed class StaffManager : MonoBehaviour
    {
        [SerializeField] private int candidatePoolSize = 6;
        [SerializeField] private int marketRefreshEveryWeeks = 8;

        private RunState _run;
        private EconomyManager _economy;
        private readonly List<EmployeeInstance> _candidates = new List<EmployeeInstance>();
        private System.Random _rng;
        private int _weeksSinceRefresh;

        public IReadOnlyList<EmployeeInstance> Candidates => _candidates;
        public IReadOnlyList<EmployeeInstance> Employees => _run?.company?.employees;

        private void OnEnable()
        {
            EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
            EventBus.Subscribe<GameLaunchedEvent>(OnGameLaunched);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);
            EventBus.Unsubscribe<GameLaunchedEvent>(OnGameLaunched);
        }

        /// <summary>Liga o manager ao estado da run e à economia (feito pelo GameController).</summary>
        public void Bind(RunState run, EconomyManager economy)
        {
            _run = run;
            _economy = economy;
            _rng = new System.Random(run != null ? run.runNumber * 7919 + 17 : 17);
            _weeksSinceRefresh = 0;
            RefreshMarket(FounderPerks.CandidateQualityBias);
        }

        /// <summary>Teto de equipe (fundador + funcionários) permitido pela fase atual.</summary>
        public int MaxTeam
        {
            get
            {
                var loop = BalanceProvider.Get<GameLoopBalanceSO>();
                return loop != null ? loop.MaxTeamForPhase(_run != null ? _run.company.phase : CompanyPhase.Garage) : 1;
            }
        }

        /// <summary>Verdadeiro se há espaço para contratar na fase atual.</summary>
        public bool CanHire => _run != null && _run.company.EffectiveTeamSize < MaxTeam;

        /// <summary>Gera um novo pool de candidatos (GDD 7.2).</summary>
        public void RefreshMarket(float qualityBias = 0f)
        {
            _candidates.Clear();
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            if (_rng == null) _rng = new System.Random(17);
            for (int i = 0; i < candidatePoolSize; i++)
                _candidates.Add(CandidateGenerator.Create(_rng, s => SalaryFor(s, economy), qualityBias));
            _weeksSinceRefresh = 0;
            EventBus.Publish(new TalentMarketRefreshedEvent(_candidates.Count));
        }

        /// <summary>Contrata um candidato do pool, se houver espaço na fase.</summary>
        public bool Hire(EmployeeInstance candidate)
        {
            if (_run == null || candidate == null || !CanHire) return false;
            if (!_candidates.Remove(candidate)) return false;

            candidate.hired = true;
            candidate.mood01 = Mathf.Clamp01(candidate.mood01 + FounderPerks.HireMoodBonus);
            _run.company.employees.Add(candidate);
            _run.company.teamSize = _run.company.EffectiveTeamSize;
            EventBus.Publish(new EmployeeHiredEvent(candidate.id, candidate.name, candidate.specialty.ToString()));
            return true;
        }

        /// <summary>Demite um funcionário (GDD 12.4: taxa de demissão). Retorna a taxa cobrada.</summary>
        /// <summary>Contrata diretamente alguém que não veio do mercado (ex.: absorvido em uma aquisição).</summary>
        public bool HireDirect(EmployeeInstance employee)
        {
            if (_run == null || employee == null || !CanHire) return false;
            employee.hired = true;
            _run.company.employees.Add(employee);
            _run.company.teamSize = _run.company.EffectiveTeamSize;
            EventBus.Publish(new EmployeeHiredEvent(employee.id, employee.name, employee.specialty.ToString()));
            return true;
        }

        public long Fire(EmployeeInstance employee)
        {
            if (_run == null || employee == null) return 0;
            if (!_run.company.employees.Remove(employee)) return 0;

            long severance = employee.weeklySalary * 2; // 2 semanas de aviso prévio
            _economy?.ApplyDelta(-severance, $"Demissão de {employee.name}");
            _run.company.teamSize = _run.company.EffectiveTeamSize;
            EventBus.Publish(new EmployeeLeftEvent(employee.id, employee.name, LeaveReason.Fired));
            return severance;
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.ended) return;

            ChargePayroll();
            TickMoodAndBurnout();

            if (++_weeksSinceRefresh >= marketRefreshEveryWeeks)
                RefreshMarket(FounderPerks.CandidateQualityBias);
        }

        private void ChargePayroll()
        {
            var staff = _run.company.employees;
            long payroll = 0;
            for (int i = 0; i < staff.Count; i++) payroll += staff[i].weeklySalary;
            if (payroll > 0) _economy?.ApplyDelta(-payroll, "Folha salarial");
        }

        private void TickMoodAndBurnout()
        {
            bool crunching = _run.IsDeveloping;
            var staff = _run.company.employees;
            for (int i = staff.Count - 1; i >= 0; i--)
            {
                var m = staff[i];
                float burnoutRate = crunching ? 0.025f : -0.05f;
                if (m.trait == PersonalityTrait.BurnoutProne && crunching) burnoutRate *= 1.6f;
                if (m.trait == PersonalityTrait.Workaholic && crunching) burnoutRate *= 0.6f;
                if (burnoutRate > 0f) burnoutRate *= FounderPerks.BurnoutMultiplier;
                m.burnout01 = Mathf.Clamp01(m.burnout01 + burnoutRate);

                // Humor deriva para baseline (0.8) descontando burnout.
                float target = 0.8f - 0.4f * m.burnout01;
                m.mood01 = Mathf.MoveTowards(m.mood01, Mathf.Clamp01(target), 0.04f);

                // Burnout extremo + humor baixo → pedido de demissão (GDD 7.5).
                if (m.burnout01 >= 0.98f && m.mood01 < 0.3f && m.trait != PersonalityTrait.Loyal)
                {
                    staff.RemoveAt(i);
                    _run.company.teamSize = _run.company.EffectiveTeamSize;
                    EventBus.Publish(new EmployeeLeftEvent(m.id, m.name, LeaveReason.Burnout));
                }
            }
        }

        /// <summary>Semanas até o mercado de talentos renovar os candidatos.</summary>
        public int WeeksUntilRefresh => Mathf.Max(0, marketRefreshEveryWeeks - _weeksSinceRefresh);

        /// <summary>Folha salarial semanal atual.</summary>
        public long WeeklyPayroll
        {
            get
            {
                long sum = 0;
                var staff = _run?.company?.employees;
                if (staff != null) for (int i = 0; i < staff.Count; i++) sum += staff[i].weeklySalary;
                return sum;
            }
        }

        public long TrainingCost(EmployeeInstance e) => e != null ? e.weeklySalary : 0;
        public long VacationCost(EmployeeInstance e) => e != null ? e.weeklySalary / 2 : 0;
        public long BonusCost(EmployeeInstance e) => e != null ? e.weeklySalary * 2 : 0;

        /// <summary>Treinamento ativo (GDD 7.3): paga e ganha XP, podendo promover.</summary>
        public bool Train(EmployeeInstance e)
        {
            if (!Pay(e, TrainingCost(e), "Treinamento")) return false;
            e.xp += 60;
            TryPromote(e);
            return true;
        }

        /// <summary>Férias (GDD 7.5): paga e derruba o burnout.</summary>
        public bool GiveVacation(EmployeeInstance e)
        {
            if (!Pay(e, VacationCost(e), "Férias")) return false;
            e.burnout01 = Mathf.Clamp01(e.burnout01 - 0.5f);
            e.mood01 = Mathf.Clamp01(e.mood01 + 0.1f);
            return true;
        }

        /// <summary>Bônus (GDD 7.5): paga e melhora o humor.</summary>
        public bool GiveBonus(EmployeeInstance e)
        {
            if (!Pay(e, BonusCost(e), "Bônus")) return false;
            e.mood01 = Mathf.Clamp01(e.mood01 + 0.3f);
            e.burnout01 = Mathf.Clamp01(e.burnout01 - 0.1f);
            return true;
        }

        private bool Pay(EmployeeInstance e, long cost, string what)
        {
            if (_run == null || e == null || !_run.company.employees.Contains(e)) return false;
            if (_run.company.cash < cost) return false;
            _economy?.ApplyDelta(-cost, $"{what}: {e.name}");
            return true;
        }

        private void OnGameLaunched(GameLaunchedEvent e)
        {
            // XP passivo por projeto lançado (GDD 7.3) + checagem de promoção.
            var staff = _run?.company?.employees;
            if (staff == null) return;
            for (int i = 0; i < staff.Count; i++)
            {
                var m = staff[i];
                int gain = Mathf.RoundToInt((80 + (m.trait == PersonalityTrait.Ambitious ? 30 : 0)) * FounderPerks.XpMultiplier);
                m.xp += gain;
                TryPromote(m);
            }
        }

        private void TryPromote(EmployeeInstance m)
        {
            Seniority target = m.xp switch
            {
                >= 800 => Seniority.Star,
                >= 400 => Seniority.Senior,
                >= 150 => Seniority.Mid,
                _ => Seniority.Junior
            };
            if (target > m.seniority)
            {
                m.seniority = target;
                var economy = BalanceProvider.Get<EconomyBalanceSO>();
                m.weeklySalary = SalaryFor(target, economy);
                EventBus.Publish(new EmployeePromotedEvent(m.id, m.name, target.ToString()));
            }
        }

        private static long SalaryFor(Seniority s, EconomyBalanceSO economy)
        {
            if (economy == null)
                return s switch { Seniority.Junior => 2000, Seniority.Mid => 3500, Seniority.Senior => 6000, _ => 10000 };
            return s switch
            {
                Seniority.Junior => economy.JuniorWeeklySalary,
                Seniority.Mid => economy.MidWeeklySalary,
                Seniority.Senior => economy.SeniorWeeklySalary,
                Seniority.Star => economy.StarWeeklySalary,
                _ => economy.JuniorWeeklySalary
            };
        }
    }
}
