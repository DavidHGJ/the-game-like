using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Escala de produção do GDD 5.3 / adendo 23.16. Os pontos nascem por semana, da aptidão de cada
    /// pessoa; um tamanho de jogo "espera" os pontos da sua equipe de referência com aptidão 5 ao longo
    /// das semanas do tamanho. A razão entre o entregue e o esperado é o volume de cada fase.
    /// </summary>
    public static class ProductionCalculator
    {
        /// <summary>Pontos esperados no jogo inteiro para o tamanho (equipe de referência × semanas × pontos-base).</summary>
        public static float RequiredPoints(ProjectSize size, GameLoopBalanceSO loop)
        {
            if (loop == null) return 0f;
            return Mathf.Max(1, loop.ReferenceTeamFor(size)) * Mathf.Max(1, loop.WeeksFor(size)) * loop.BasePointsPerWeek;
        }

        /// <summary>
        /// Cobertura de escopo (0..1) prevista só pelo tamanho da equipe, com aptidão de referência:
        /// equipe ÷ equipe de referência do tamanho, com piso. Usada nas simulações rápidas.
        /// </summary>
        public static float ScopeCoverage01(int teamSize, ProjectSize size, GameLoopBalanceSO loop)
        {
            int referenceTeam = loop != null ? loop.ReferenceTeamFor(size) : 1;
            float coverage = Mathf.Max(1, teamSize) / (float)Mathf.Max(1, referenceTeam);
            float floor = loop != null ? loop.ScopeCoverageFloor : 0.15f;
            return Mathf.Clamp(coverage, floor, 1f);
        }
    }
}
