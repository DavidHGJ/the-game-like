using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Market;
using GameLike.Systems.Meta;
using GameLike.Systems.Time;
using UnityEngine;

namespace GameLike.Systems.Events
{
    /// <summary>
    /// Dispara eventos narrativos aleatórios (GDD 11): a cada semana há uma chance de
    /// um evento elegível ocorrer, pausando o tempo e exigindo uma escolha. Resolver a
    /// escolha aplica as consequências (ocultas) e retoma o tempo. Os eventos são
    /// ScriptableObjects carregados de Resources/Events — adicionar eventos nunca toca
    /// neste código.
    /// </summary>
    public sealed class EventManager : MonoBehaviour
    {
        [SerializeField] private float eventChancePerWeek = 0.07f;
        [SerializeField] private int cooldownWeeks = 4;

        private readonly List<NarrativeEventSO> _pool = new List<NarrativeEventSO>();
        private RunState _run;
        private EconomyManager _economy;
        private TimeManager _time;
        private MarketManager _market;
        private System.Random _rng;
        private int _weeksUntilEligible;

        public NarrativeEventSO Current { get; private set; }

        private void Awake()
        {
            _pool.Clear();
            _pool.AddRange(Resources.LoadAll<NarrativeEventSO>("Events"));
        }

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        /// <summary>Liga o manager ao estado da run e aos sistemas que ele afeta.</summary>
        public void Bind(RunState run, EconomyManager economy, TimeManager time, MarketManager market)
        {
            _run = run;
            _economy = economy;
            _time = time;
            _market = market;
            _rng = new System.Random(run != null ? run.runNumber * 2749 + 11 : 11);
            _weeksUntilEligible = cooldownWeeks;
            Current = null;
        }

        /// <summary>True se a opção está disponível (habilidade do fundador presente). Slice: só as sem requisito.</summary>
        public bool IsOptionAvailable(NarrativeOption option)
        {
            if (option == null) return false;
            // Habilidades do fundador ainda não existem (meta-progressão, GDD 14):
            // opções com requisito ficam bloqueadas por enquanto.
            return option.requiredSkill == FounderSkill.None || FounderPerks.Level(option.requiredSkill) > 0;
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.ended || Current != null) return;
            if (_weeksUntilEligible > 0) { _weeksUntilEligible--; return; }
            if (_pool.Count == 0) return;
            if (_rng.NextDouble() > eventChancePerWeek) return;

            var picked = PickWeighted();
            if (picked == null) return;

            Current = picked;
            _time?.AddLock();
            EventBus.Publish(new NarrativeEventTriggeredEvent(picked.Id, picked.Title));
        }

        /// <summary>Resolve o evento ativo com a opção escolhida, aplicando as consequências.</summary>
        public void ResolveOption(int index)
        {
            if (Current == null) return;
            var options = Current.Options;
            if (index < 0 || index >= options.Count) return;

            var option = options[index];
            if (!IsOptionAvailable(option)) return;

            ApplyEffects(option.effects);

            string id = Current.Id;
            Current = null;
            _weeksUntilEligible = cooldownWeeks;
            _time?.RemoveLock();
            EventBus.Publish(new NarrativeEventResolvedEvent(id, index));
        }

        private void ApplyEffects(List<EventEffect> effects)
        {
            if (effects == null || _run == null) return;
            var company = _run.company;

            foreach (var fx in effects)
            {
                switch (fx.type)
                {
                    case EventEffectType.Cash:
                    {
                        float cash = fx.amount < 0f ? fx.amount * FounderPerks.EventCashLossMultiplier : fx.amount;
                        _economy?.ApplyDelta((long)cash, "Evento");
                        break;
                    }
                    case EventEffectType.Reputation:
                    {
                        float rep = fx.amount < 0f
                            ? fx.amount * FounderPerks.ReputationLossMultiplier
                            : fx.amount * FounderPerks.ReputationGainMultiplier;
                        if (rep <= -0.1f) _run.hadMajorCrisis = true;
                        company.reputation01 = Mathf.Clamp01(company.reputation01 + rep);
                        break;
                    }
                    case EventEffectType.Quality:
                        if (_run.HasActiveProject)
                            _run.currentProject.devEventsBonus = Mathf.Clamp(_run.currentProject.devEventsBonus + fx.amount, -1f, 1f);
                        break;
                    case EventEffectType.Hype:
                        if (_run.HasActiveProject)
                            _run.currentProject.hype01 = Mathf.Clamp01(_run.currentProject.hype01 + fx.amount);
                        break;
                    case EventEffectType.TeamBurnout:
                        ForEachEmployee(m => m.burnout01 = Mathf.Clamp01(m.burnout01 + fx.amount));
                        break;
                    case EventEffectType.TeamMood:
                        ForEachEmployee(m => m.mood01 = Mathf.Clamp01(m.mood01 + fx.amount));
                        break;
                    case EventEffectType.ThemeTrend:
                        if (_run.HasActiveProject && _run.market != null)
                        {
                            string key = _run.currentProject.themeKey;
                            if (!string.IsNullOrEmpty(key))
                                _run.market.SetThemeTrend(key, _run.market.GetThemeTrend(key) + fx.amount);
                        }
                        break;
                }
            }
        }

        private void ForEachEmployee(System.Action<EmployeeInstance> action)
        {
            var staff = _run?.company?.employees;
            if (staff == null) return;
            for (int i = 0; i < staff.Count; i++) action(staff[i]);
        }

        private NarrativeEventSO PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].RequiresActiveProject && !_run.HasActiveProject) continue;
                total += _pool[i].Weight;
            }
            if (total <= 0f) return null;

            float roll = (float)_rng.NextDouble() * total;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].RequiresActiveProject && !_run.HasActiveProject) continue;
                roll -= _pool[i].Weight;
                if (roll <= 0f) return _pool[i];
            }
            return null;
        }
    }
}
