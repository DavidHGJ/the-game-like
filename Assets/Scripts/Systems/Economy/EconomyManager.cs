using GameLike.Core.Events;
using GameLike.Data.Balance;
using UnityEngine;

namespace GameLike.Systems.Economy
{
    /// <summary>
    /// Reference implementation of an event listener that also demonstrates
    /// the balance-asset access pattern. Subscribes in OnEnable, unsubscribes
    /// in OnDisable — the discipline that keeps the EventBus from leaking
    /// handlers across scene loads or runs.
    ///
    /// Reads salary values live from <see cref="EconomyBalanceSO"/> via
    /// <see cref="BalanceProvider"/> on every tick — never cached into a
    /// local field — so an Inspector edit to a salary takes effect on the
    /// very next week tick, including mid-Play Mode.
    ///
    /// This is still a stub: the real implementation will aggregate actual
    /// hired employees' salaries from the active run state (CompanyState,
    /// to be added) instead of a single flat placeholder cost.
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        private long _balance;

        private void OnEnable()
        {
            EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            var balance = BalanceProvider.Get<EconomyBalanceSO>();
            if (balance == null) return; // setup error already logged by the provider

            // Placeholder: charges a single junior salary as a stand-in for
            // "whatever the hired team currently costs". Replace once
            // CompanyState tracks actual employees.
            var weeklyCost = balance.JuniorWeeklySalary;
            if (weeklyCost == 0) return;

            ApplyDelta(-weeklyCost, "Weekly operating cost (placeholder)");
        }

        /// <summary>
        /// Single choke point for balance changes. Applies the delta and announces
        /// the new state so valuation, UI and others can react.
        /// </summary>
        public void ApplyDelta(long delta, string reason)
        {
            _balance += delta;
            EventBus.Publish(new MoneyChangedEvent(_balance, delta, reason));
        }
    }
}

