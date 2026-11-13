using System.Collections.Generic;
using GameLike.State;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Staff
{
    /// <summary>
    /// Cálculos puros e determinísticos sobre a equipe (GDD 7). Traduz a composição
    /// do time (senioridade, humor, burnout, especialidade) nos insumos que a fórmula
    /// de qualidade consome: o nível médio da equipe (componente 3, GDD 19.7) e a
    /// cobertura de especialistas para a alocação escolhida. Sem estado, testável fora
    /// do Unity.
    /// </summary>
    public static class StaffCalculator
    {
        private const float FounderLevel01 = 0.33f;   // fundador ~ pleno
        private const float FounderCoverage = 0.5f;   // generalista cobre parcialmente qualquer área

        /// <summary>Fator de produtividade de um membro por humor e burnout (0..1).</summary>
        public static float ProductivityFactor(EmployeeInstance e)
        {
            float mood = Mathf.Clamp01(0.6f + 0.4f * e.mood01);
            float burnout = Mathf.Clamp01(1f - 0.5f * e.burnout01);
            return mood * burnout;
        }

        /// <summary>Nível de um membro normalizado 0..1 (Junior 0 → Star 1), já descontando produtividade.</summary>
        public static float MemberLevel01(EmployeeInstance e)
        {
            float norm = Mathf.Clamp01((e.SeniorityMultiplier - 1.0f) / (2.2f - 1.0f));
            return Mathf.Clamp01(norm * ProductivityFactor(e));
        }

        /// <summary>
        /// Nível médio da equipe 0..1, incluindo o fundador como baseline. É o
        /// componente "nível médio da equipe alocada" da fórmula de qualidade.
        /// </summary>
        public static float TeamLevel01(IReadOnlyList<EmployeeInstance> staff)
        {
            float sum = FounderLevel01;
            int count = 1;
            if (staff != null)
            {
                for (int i = 0; i < staff.Count; i++)
                {
                    sum += MemberLevel01(staff[i]);
                    count++;
                }
            }
            return Mathf.Clamp01(sum / count);
        }

        /// <summary>
        /// Cobertura de especialistas (0..1) para a alocação dada: quanto do foco do
        /// projeto está coberto por alguém especializado naquela área. O fundador
        /// (generalista) cobre parcialmente todas as áreas.
        /// </summary>
        public static float SpecialtyCoverage01(IReadOnlyList<EmployeeInstance> staff, float[] allocation)
        {
            var best = new float[ProjectState.AreaCount];
            for (int i = 0; i < best.Length; i++) best[i] = FounderCoverage;

            if (staff != null)
            {
                for (int s = 0; s < staff.Count; s++)
                {
                    int area = SpecialtyMap.ToAreaIndex(staff[s].specialty);
                    float contribution = Mathf.Clamp01(0.7f + 0.3f * MemberLevel01(staff[s]));
                    if (area >= 0 && area < best.Length)
                        best[area] = Mathf.Max(best[area], contribution);
                    else
                        for (int a = 0; a < best.Length; a++)
                            best[a] = Mathf.Max(best[a], FounderCoverage * contribution);
                }
            }

            float weightedSum = 0f, weightTotal = 0f;
            for (int i = 0; i < best.Length; i++)
            {
                float w = allocation != null && i < allocation.Length ? Mathf.Max(0f, allocation[i]) : 0f;
                weightedSum += w * best[i];
                weightTotal += w;
            }
            if (weightTotal <= 0.0001f) return FounderCoverage;
            return Mathf.Clamp01(weightedSum / weightTotal);
        }

        /// <summary>
        /// Qualidade de equipe combinada (0..1) entregue à fórmula de qualidade:
        /// metade nível médio, metade cobertura de especialistas para a alocação.
        /// </summary>
        public static float TeamQuality01(IReadOnlyList<EmployeeInstance> staff, float[] allocation)
        {
            return Mathf.Clamp01(0.5f * TeamLevel01(staff) + 0.5f * SpecialtyCoverage01(staff, allocation));
        }
    }
}
