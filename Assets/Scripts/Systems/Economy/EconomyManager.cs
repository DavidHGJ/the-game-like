using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Economy
{
    /// <summary>
    /// Autoridade sobre o caixa da empresa. Opera sobre o <see cref="CompanyState"/>
    /// da run ativa (ligado via <see cref="Bind"/> pelo GameController), centraliza
    /// toda mutação de saldo em <see cref="ApplyDelta"/> e anuncia o novo estado
    /// com <see cref="MoneyChangedEvent"/> para que valuation, UI e outros reajam.
    ///
    /// Custos recorrentes (salários/P&D) são cobrados pelos managers de domínio
    /// (ex: <c>ProjectManager</c> cobra o custo de desenvolvimento por semana),
    /// não aqui — este manager não decide "o que" cobrar, só aplica o delta.
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        private CompanyState _company;

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        /// <summary>Liga o manager ao estado da empresa da run ativa.</summary>
        public void Bind(CompanyState company) => _company = company;

        /// <summary>
        /// Cobra o custo operacional fixo da semana (overhead por fase, GDD 2.3). A
        /// folha salarial é cobrada pelo <c>StaffManager</c>; aqui é só o overhead.
        /// Lê o valor do balanceamento a cada uso.
        /// </summary>
        private void OnWeekTick(WeekTickEvent e)
        {
            if (_company == null) return;
            _company.weeksElapsed++;
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            long operating = economy != null ? economy.WeeklyOperatingFor(_company.phase) : 0;
            if (operating > 0) ApplyDelta(-operating, "Custo operacional");
        }

        /// <summary>Saldo atual (0 se nenhuma empresa estiver ligada).</summary>
        public long Balance => _company?.cash ?? 0;

        /// <summary>
        /// Quanto os custos estão mais caros (ou baratos) agora: inflação acumulada nas semanas
        /// da run vezes o fator sorteado para esta run (GDD 23.12). 1 = custo base.
        /// </summary>
        public float CostMultiplier
        {
            get
            {
                if (_company == null) return 1f;
                var tuning = BalanceProvider.Get<GameTuningBalanceSO>();
                float inflation = tuning != null ? tuning.InflationFactor(_company.weeksElapsed) : 1f;
                float variation = _company.costVariation > 0f ? _company.costVariation : 1f;
                return inflation * variation;
            }
        }

        /// <summary>
        /// Único choke point de mudança de saldo. Aplica o delta ao caixa da
        /// empresa e anuncia o novo estado. Todo custo (delta negativo) passa pelo
        /// <see cref="CostMultiplier"/>; receitas entram como vieram.
        /// </summary>
        public void ApplyDelta(long delta, string reason)
        {
            if (_company == null) return;
            if (delta < 0) delta = (long)System.Math.Round(delta * (double)CostMultiplier);
            _company.cash += delta;
            EventBus.Publish(new MoneyChangedEvent(_company.cash, delta, reason));
        }
    }
}
