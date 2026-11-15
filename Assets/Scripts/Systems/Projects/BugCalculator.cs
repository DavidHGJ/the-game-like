using GameLike.Data.Balance;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Modelo de bugs (GDD 5.5 / adendo 23.18): bugs nascem de cada ponto produzido, menos com equipe
    /// melhor e foco em Tecnologia; no lançamento pesam pela proporção bugs ÷ pontos do jogo.
    /// </summary>
    public static class BugCalculator
    {
        /// <summary>Fração 0..1 da alocação dedicada à área de Tecnologia (Programação/QA).</summary>
        public static float TechFocus01(float[] allocation)
        {
            if (allocation == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < allocation.Length; i++) sum += Mathf.Max(0f, allocation[i]);
            if (sum <= 0.0001f) return 0f;
            int tech = (int)DevArea.Tech;
            float techWeight = tech < allocation.Length ? Mathf.Max(0f, allocation[tech]) : 0f;
            return Mathf.Clamp01(techWeight / sum);
        }

        /// <summary>
        /// Desconto (0..máximo) da nota final pelos bugs: proporção bugs ÷ pontos do jogo × peso (adendo 23.18).
        /// Mais bugs para os mesmos pontos = desconto maior.
        /// </summary>
        public static float Penalty01(float bugs, float totalPoints, GameLoopBalanceSO loop)
        {
            if (bugs <= 0f) return 0f;
            float weight = loop != null ? loop.BugPenaltyWeight : 1f;
            float max = loop != null ? loop.BugPenaltyMax : 0.5f;
            float ratio = bugs / Mathf.Max(1f, totalPoints);
            return Mathf.Clamp(ratio * weight, 0f, max);
        }
    }
}
