using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Meta;
using GameLike.Systems.Staff;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Regras de produção por semana (adendo 23.16) num lugar só: duração das fases, pontos que uma
    /// pessoa gera num item e bugs esperados por semana. O <see cref="ProjectManager"/> e o simulador
    /// de desenvolvimento usam as mesmas funções, então a simulação nunca diverge do jogo.
    /// </summary>
    public static class DevelopmentRules
    {
        /// <summary>Semanas da fase (fatia da duração do jogo), com ou sem crunch.</summary>
        public static int PhaseWeeks(int targetWeeks, int phaseIndex, bool crunch, GameLoopBalanceSO loop)
        {
            float share = ParameterCatalog.DurationShare((ProductionPhase)Mathf.Clamp(phaseIndex, 0, ParameterCatalog.PhaseCount - 1));
            int weeks = Mathf.Max(1, Mathf.RoundToInt(targetWeeks * share));
            if (crunch) weeks = Mathf.Max(1, Mathf.CeilToInt(weeks * (loop != null ? loop.CrunchDurationMultiplier : 0.75f)));
            return weeks;
        }

        /// <summary>
        /// Pontos que uma pessoa gera numa semana no item, com a fração dada: aptidão na área do item,
        /// produtividade, bônus de equipe pequena e crunch (mais pontos por semana numa fase mais curta).
        /// </summary>
        public static float WeeklyPoints(float[] aptitude, GameParameter parameter, float share01, float productivity,
            int teamSize, bool crunch, GameLoopBalanceSO loop)
        {
            int area = QualityCalculator.AreaIndexOf(parameter);
            float points = AptitudeCalculator.WeeklyPoints(aptitude[area], share01, productivity, loop);
            points *= FounderPerks.SmallTeamPointMultiplier(Mathf.Max(1, teamSize));
            if (crunch) points /= Mathf.Max(0.1f, loop != null ? loop.CrunchDurationMultiplier : 0.75f);
            return points;
        }


        /// <summary>Qualidade técnica da equipe (0..1) para os bugs: aptidão média em Tecnologia ÷ 10 (adendo 23.18).</summary>
        public static float TechQuality01(System.Collections.Generic.IReadOnlyList<float[]> aptitudes)
        {
            if (aptitudes == null || aptitudes.Count == 0) return 0.5f;
            float sum = 0f;
            foreach (var a in aptitudes) sum += a != null && a.Length > (int)GameLike.State.DevArea.Tech ? a[(int)GameLike.State.DevArea.Tech] : 5f;
            return Mathf.Clamp01(sum / aptitudes.Count / AptitudeCalculator.Max);
        }
        /// <summary>
        /// Bugs que nascem dos pontos produzidos (adendo 23.18): pontos × bugs por ponto × fatores de equipe,
        /// foco em Tecnologia, Estabilidade e crunch. Antes do sorteio da parte fracionária.
        /// </summary>
        public static float ExpectedBugs(float points, float teamQuality01, float[] areaAllocation, float stabilityShare01, bool crunch, GameLoopBalanceSO loop)
        {
            float perPoint = loop != null ? loop.BugsPerPoint : 0.1f;
            float teamFactor = Mathf.Clamp01(1.3f - teamQuality01);
            float techFactor = 1f - 0.5f * BugCalculator.TechFocus01(areaAllocation);
            float stabilityFactor = 1f - 0.5f * Mathf.Clamp01(stabilityShare01 / 0.5f);
            float crunchFactor = crunch ? (loop != null ? loop.CrunchBugMultiplier : 1.6f) : 1f;
            return Mathf.Max(0f, points) * perPoint * teamFactor * techFactor * stabilityFactor * crunchFactor * FounderPerks.BugMultiplier;
        }
    }
}
