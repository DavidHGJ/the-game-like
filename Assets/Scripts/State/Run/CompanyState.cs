using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.State.Staff;

namespace GameLike.State
{
    /// <summary>
    /// Estado vivo da empresa do jogador (camada de Estado, serializável). Guarda
    /// caixa, valuation, fase atual e estatísticas acumuladas da run. Mutado pelos
    /// managers (Economy, Project, Valuation) através dos seus choke points.
    /// </summary>
    [Serializable]
    public sealed class CompanyState
    {
        public string name;
        public long cash;
        public long valuation;
        public CompanyPhase phase = CompanyPhase.Garage;
        public int gamesReleased;
        public long lifetimeRevenue;

        /// <summary>Semanas de run já passadas — base da inflação (GDD 23.12).</summary>
        public int weeksElapsed;
        /// <summary>Fator de custo sorteado no começo da run (GDD 23.12). 1 = custo base.</summary>
        public float costVariation = 1f;

        /// <summary>
        /// Funcionários contratados (não inclui o fundador). A equipe efetiva de um
        /// projeto é <c>employees.Count + 1</c> (fundador), limitada pela fase (GDD 7.6).
        /// </summary>
        public List<EmployeeInstance> employees = new List<EmployeeInstance>();

        /// <summary>Distintos gêneros (por Id) já lançados nesta run — diversidade (GDD 14.2/20.5).</summary>
        public List<string> genresReleased = new List<string>();

        /// <summary>Tamanho da equipe no último projeto (fundador + funcionários). Compat/telemetria.</summary>
        public int teamSize = 1;

        /// <summary>Maior valuation atingido na run — métrica de Pontos de Legado (GDD 19.5).</summary>
        public long peakValuation;

        /// <summary>Reputação pública 0..1 (GDD 4) — afeta nota-base e hype.</summary>
        public float reputation01 = 0.5f;

        /// <summary>O valuation já permite expandir de fase e o jogador ainda não expandiu (GDD 2.1).</summary>
        public bool expansionAvailable;

        /// <summary>Semanas com a expansão disponível e não feita — gera estagnação (adendo 23.8).</summary>
        public int stagnationWeeks;

        /// <summary>Entradas e saídas de caixa da última semana fechada (painel financeiro).</summary>
        public long lastWeekIncome;
        public long lastWeekExpense;

        /// <summary>Histórico semanal de valuation (gráfico do painel financeiro, GDD 4.2).</summary>
        public List<long> valuationHistory = new List<long>();

        /// <summary>Valor dos IPs absorvidos em aquisições de rivais (GDD 12.4) — entra no valuation.</summary>
        public long acquiredIpValue;
        public int acquiredGames;

        /// <summary>Semanas seguidas com o caixa negativo — a falência vem depois da carência (GDD 20.4).</summary>
        public int debtWeeks;

        /// <summary>Equipe efetiva = fundador + funcionários contratados.</summary>
        public int EffectiveTeamSize => 1 + (employees?.Count ?? 0);

        public CompanyState(string name, long startingCash)
        {
            this.name = name;
            cash = startingCash;
        }
    }
}
