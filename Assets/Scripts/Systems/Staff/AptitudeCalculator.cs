using GameLike.Data.Balance;
using GameLike.State;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Staff
{
    /// <summary>
    /// Regras de aptidão por área (adendo 23.16): sorteio inicial do fundador e dos funcionários,
    /// pontos gerados por semana, ganho por prática e treinamento. Lógica pura, sem estado próprio.
    /// </summary>
    public static class AptitudeCalculator
    {
        public const float Min = 1f;
        public const float Max = 10f;
        /// <summary>Aptidão que rende exatamente os pontos-base por semana.</summary>
        public const float Reference = 5f;

        /// <summary>Garante um vetor de aptidão válido (5 áreas, 1..10); vazio vira o valor dado.</summary>
        public static float[] Ensure(float[] aptitude, float fill = Reference)
        {
            if (aptitude != null && aptitude.Length == ProjectState.AreaCount)
            {
                for (int i = 0; i < aptitude.Length; i++) aptitude[i] = Mathf.Clamp(aptitude[i] <= 0f ? fill : aptitude[i], Min, Max);
                return aptitude;
            }
            var result = new float[ProjectState.AreaCount];
            for (int i = 0; i < result.Length; i++) result[i] = fill;
            return result;
        }

        /// <summary>Copia um vetor de aptidão (a run começa de uma cópia da base do fundador).</summary>
        public static float[] Copy(float[] aptitude)
        {
            var source = Ensure(aptitude);
            var result = new float[source.Length];
            System.Array.Copy(source, result, source.Length);
            return result;
        }

        /// <summary>
        /// Aptidões de base do fundador: uma área forte (6..7,5), uma fraca (2..3) e o resto mediano
        /// (3,5..5,5). Sorteado uma vez e guardado no perfil.
        /// </summary>
        public static float[] RollFounder(System.Random rng)
        {
            rng ??= new System.Random();
            var result = new float[ProjectState.AreaCount];
            for (int i = 0; i < result.Length; i++) result[i] = Round1(3.5f + 2f * (float)rng.NextDouble());
            int strong = rng.Next(result.Length);
            int weak = (strong + 1 + rng.Next(result.Length - 1)) % result.Length;
            result[strong] = Round1(6f + 1.5f * (float)rng.NextDouble());
            result[weak] = Round1(2f + (float)rng.NextDouble());
            return result;
        }

        /// <summary>
        /// Aptidões de um funcionário: alta na área da especialidade (4,5 júnior → 9 estrela),
        /// baixa nas outras; generalista é mediano em tudo.
        /// </summary>
        public static float[] ForEmployee(Specialty specialty, Seniority seniority, System.Random rng)
        {
            rng ??= new System.Random();
            float level = (int)seniority;
            int area = SpecialtyMap.ToAreaIndex(specialty);
            var result = new float[ProjectState.AreaCount];
            for (int i = 0; i < result.Length; i++)
            {
                float jitter = (float)rng.NextDouble() - 0.5f;
                float value = area < 0 || area >= result.Length
                    ? 3f + 1f * level + jitter
                    : i == area ? 4.5f + 1.5f * level + jitter : 1.5f + 0.5f * level + jitter;
                result[i] = Round1(Mathf.Clamp(value, Min, Max));
            }
            return result;
        }

        /// <summary>Aptidão de um funcionário, criando a partir da especialidade quando ainda não há (saves antigos).</summary>
        public static float[] Of(EmployeeInstance employee)
        {
            if (employee == null) return Ensure(null);
            if (employee.aptitude == null || employee.aptitude.Length != ProjectState.AreaCount)
                employee.aptitude = ForEmployee(employee.specialty, employee.seniority, new System.Random(employee.id != null ? employee.id.GetHashCode() : 0));
            return employee.aptitude;
        }

        /// <summary>Pontos que uma pessoa gera numa semana dedicando a fração dada à área.</summary>
        public static float WeeklyPoints(float aptitude, float share01, float productivity, GameLoopBalanceSO loop)
        {
            float basePoints = loop != null ? loop.BasePointsPerWeek : 3f;
            return Mathf.Max(0f, share01) * basePoints * Mathf.Clamp(aptitude, Min, Max) / Reference * Mathf.Max(0f, productivity);
        }

        /// <summary>Prática (23.16): soma aptidão pelos pontos produzidos na área, menos quanto mais perto de 10.</summary>
        public static void Practice(float[] aptitude, int area, float points, GameLoopBalanceSO loop)
        {
            if (aptitude == null || area < 0 || area >= aptitude.Length || points <= 0f) return;
            float perPoint = loop != null ? loop.PracticePerPoint : 0.02f;
            float room = (Max - aptitude[area]) / (Max - Reference);
            aptitude[area] = Mathf.Clamp(aptitude[area] + points * perPoint * Mathf.Clamp(room, 0.1f, 2f), Min, Max);
        }

        /// <summary>Preço de um treinamento na área, pela aptidão atual (23.16).</summary>
        public static long TrainingCost(float aptitude, GameLoopBalanceSO loop)
        {
            long perLevel = loop != null ? loop.TrainingCostPerLevel : 2000;
            return (long)Mathf.Round(perLevel * Mathf.Clamp(aptitude, Min, Max));
        }

        /// <summary>Aplica um treinamento (sem cobrar). Falso se a área já está no máximo.</summary>
        public static bool Train(float[] aptitude, int area, GameLoopBalanceSO loop)
        {
            if (aptitude == null || area < 0 || area >= aptitude.Length || aptitude[area] >= Max) return false;
            aptitude[area] = Mathf.Min(Max, aptitude[area] + (loop != null ? loop.TrainingGain : 0.5f));
            return true;
        }

        private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;
    }
}
