using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Modelo de produção por pontos do GDD seção 5.3.
    ///
    /// Cada pessoa da equipe contribui com um número de pontos-base por fase de
    /// produção. O total de pontos que a equipe consegue entregar é comparado com
    /// o que o escopo do projeto exige — a razão é a <b>cobertura de escopo</b>
    /// (0..1). Como a equipe é limitada pela <b>fase da empresa</b> (GDD 7.6) e não
    /// pelo tamanho do projeto, tentar um escopo grande com equipe pequena resulta
    /// em cobertura baixa (entrega sub-dimensionada) — a tensão central do GDD
    /// entre ambição de escopo e capacidade real da empresa.
    /// </summary>
    public static class ProductionCalculator
    {
        /// <summary>Total de pontos que a equipe entrega num projeto (GDD 5.3).</summary>
        public static int TotalProductionPoints(int teamSize, GameLoopBalanceSO loop)
        {
            if (loop == null) return Mathf.Max(1, teamSize) * 100 * 5;
            return Mathf.Max(1, teamSize) * loop.PointsPerPersonPerPhase * loop.ProductionPhaseCount;
        }

        /// <summary>
        /// Cobertura de escopo (0..1): pontos entregues pela equipe vs. pontos que
        /// o escopo exige (equipe de referência do tamanho). Aplicada com um piso
        /// para que uma equipe muito pequena ainda entregue algo, não zero.
        /// </summary>
        public static float ScopeCoverage01(int teamSize, ProjectSize size, GameLoopBalanceSO loop)
        {
            int delivered = TotalProductionPoints(teamSize, loop);
            int referenceTeam = loop != null ? loop.ReferenceTeamFor(size) : 1;
            int required = (loop != null ? loop.PointsPerPersonPerPhase * loop.ProductionPhaseCount : 500)
                           * Mathf.Max(1, referenceTeam);

            float coverage = required > 0 ? delivered / (float)required : 1f;
            float floor = loop != null ? loop.ScopeCoverageFloor : 0.15f;
            return Mathf.Clamp(coverage, floor, 1f);
        }
    }
}
