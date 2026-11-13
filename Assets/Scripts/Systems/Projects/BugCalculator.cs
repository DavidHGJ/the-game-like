using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Modelo de bugs da produção (GDD 5.5): bugs acumulam passivamente quando há
    /// pouca Programação/QA e a equipe é fraca; lançar com bugs acima da tolerância
    /// penaliza a nota da crítica. Puro e determinístico (exceto o arredondamento),
    /// testável fora do Unity.
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

        /// <summary>Bugs gerados numa semana de produção, dado o foco em Tech e a qualidade da equipe.</summary>
        public static int BugsThisWeek(float[] allocation, float teamQuality01, GameLoopBalanceSO loop)
        {
            float basePerWeek = loop != null ? loop.BugBasePerWeek : 2.5f;
            float techFactor = 1f - TechFocus01(allocation);        // mais foco em Tech → menos bugs
            float teamFactor = Mathf.Clamp01(1.3f - teamQuality01); // equipe melhor → menos bugs
            float bugs = basePerWeek * techFactor * teamFactor;
            return Mathf.Max(0, Mathf.RoundToInt(bugs));
        }

        /// <summary>
        /// Penalidade de nota (0..penaltyMax) pelos bugs no lançamento. Bugs até a
        /// tolerância não penalizam; acima disso a penalidade cresce e satura.
        /// A escala de referência cresce com o tamanho do projeto (projetos maiores
        /// toleram mais bugs em termos absolutos).
        /// </summary>
        public static float ScorePenalty(int bugCount, ProjectSize size, GameLoopBalanceSO loop)
        {
            int tolerance = loop != null ? loop.BugToleranceFree : 5;
            float penaltyMax = loop != null ? loop.BugScorePenaltyMax : 3f;

            int excess = Mathf.Max(0, bugCount - tolerance);
            if (excess <= 0) return 0f;

            float referenceExcess = SizeReferenceBugs(size);
            float t = Mathf.Clamp01(excess / referenceExcess);
            return penaltyMax * t;
        }

        private static float SizeReferenceBugs(ProjectSize size) => size switch
        {
            ProjectSize.Small => 25f,
            ProjectSize.Medium => 45f,
            ProjectSize.Large => 70f,
            ProjectSize.AAA => 110f,
            _ => 45f
        };
    }
}
