using System.Collections.Generic;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Lógica de franquias (GDD 13): média histórica de uma franquia e o bônus (ou
    /// peso) de hype inicial que uma sequência herda. Uma franquia saudável (média
    /// alta) dá hype inicial maior que um IP novo; uma franquia queimada (média baixa)
    /// vira peso — começa com hype menor que um IP novo (GDD 13.4). Puro e testável.
    /// </summary>
    public static class FranchiseCalculator
    {
        /// <summary>Média das notas dos jogos lançados de uma franquia (0 se nenhum).</summary>
        public static float AverageScore(IReadOnlyList<ProjectState> released, string franchiseId)
        {
            if (released == null || string.IsNullOrEmpty(franchiseId)) return 0f;
            float sum = 0f; int n = 0;
            for (int i = 0; i < released.Count; i++)
                if (released[i].franchiseId == franchiseId) { sum += released[i].finalScore; n++; }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>Número de jogos lançados numa franquia.</summary>
        public static int Count(IReadOnlyList<ProjectState> released, string franchiseId)
        {
            if (released == null || string.IsNullOrEmpty(franchiseId)) return 0;
            int n = 0;
            for (int i = 0; i < released.Count; i++)
                if (released[i].franchiseId == franchiseId) n++;
            return n;
        }

        /// <summary>
        /// Hype inicial (0..1) de uma sequência a partir da média da franquia. Média 7+
        /// dá um empurrão; média baixa resulta em hype inicial pífio (franquia queimada).
        /// Um IP novo começa em 0; esta função só se aplica a sequências.
        /// </summary>
        public static float InitialHype01(float franchiseAverage)
        {
            if (franchiseAverage <= 0f) return 0f;
            // Nota 5 → ~0.1; nota 7 → ~0.28; nota 9 → ~0.46; nota 10 → ~0.55.
            float bonus = (franchiseAverage - 4f) * 0.09f;
            return Mathf.Clamp(bonus, 0f, 0.6f);
        }
    }
}
