using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>Detalhe da nota de um jogo (adendo 23.18) — usado pelo lançamento, simulação e testes.</summary>
    public sealed class ScoreBreakdown
    {
        /// <summary>Acerto do tema (0..1).</summary>
        public float theme;
        /// <summary>Acerto de cada fase (0..1), média dos blocos.</summary>
        public readonly float[] phases = new float[ParameterCatalog.PhaseCount];
        /// <summary>Acerto final de cada bloco (0..1), por parâmetro; -1 = bloco fora do jogo.</summary>
        public readonly float[] blocks = new float[ParameterCatalog.ParameterCount];
        /// <summary>Acerto só da divisão de cada bloco (0..1), antes de pontos e engine.</summary>
        public readonly float[] blockDivision = new float[ParameterCatalog.ParameterCount];
        /// <summary>Acerto médio das fases (0..1).</summary>
        public float phasesAverage;
        /// <summary>Acerto médio só da divisão (0..1), sem pontos nem engine.</summary>
        public float divisionAverage;
        /// <summary>Desconto por bugs (0..1) aplicado sobre a nota.</summary>
        public float bugPenalty;
        /// <summary>Nota final (0..10), sem o sorteio da crítica.</summary>
        public float score;
    }

    /// <summary>
    /// Nota do jogo "quão perto do perfeito" (adendo 23.18). Nunca é explicada ao jogador.
    /// Nota = 10 × (parte do tema × acerto do tema + parte das fases × acerto das fases) × (1 − desconto de bugs).
    /// Cada fase é a média dos seus blocos; cada bloco = acerto da divisão + bônus de pontos + amenização
    /// da engine sobre o que falta, limitado a 100%.
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// Acerto da divisão de um bloco (0..1): distância entre o % posto e o % ideal, descontada uma tolerância
        /// relativa ao ideal (0,1 = errar até 10% do ideal não conta). Deixar zero num bloco é sempre 100% de erro.
        /// </summary>
        public static float DivisionAccuracy(float share01, float ideal01, float toleranceRelative)
        {
            if (ideal01 <= 0.0001f) return share01 <= 0.0001f ? 1f : 0f;
            if (share01 <= 0.0001f) return 0f;
            float off = Mathf.Max(0f, Mathf.Abs(share01 - ideal01) - toleranceRelative * ideal01);
            return Mathf.Clamp01(1f - off / ideal01);
        }

        /// <summary>
        /// Acerto final do bloco (0..1): divisão + fator × fração do ideal de pontos (até o ideal), depois a
        /// engine ameniza parte do que ainda falta. Passou de 100% vale 100%.
        /// </summary>
        public static float BlockScore(float division01, float points, float idealPoints, float engineLevels, GameLoopBalanceSO loop)
        {
            float bonusFactor = loop != null ? loop.PointsBonusFactor : 0.5f;
            float reliefPerLevel = loop != null ? loop.EngineReliefPerLevel : 0.1f;
            float reliefMax = loop != null ? loop.EngineReliefMax : 0.4f;
            float pointsFraction = idealPoints > 0f ? Mathf.Clamp01(points / idealPoints) : 0f;
            float value = division01 + bonusFactor * pointsFraction;
            if (value >= 1f) return 1f;
            float relief = Mathf.Min(reliefMax, Mathf.Max(0f, engineLevels) * reliefPerLevel);
            value += (1f - value) * relief;
            return Mathf.Clamp01(value);
        }

        /// <summary>Acerto do tema (0..1) pela compatibilidade gênero × tema.</summary>
        public static float ThemeAccuracy(ThemeSO theme, string genreKey, GameLoopBalanceSO loop)
        {
            int weight = theme != null ? (int)theme.CompatibilityFor(genreKey) : 0;
            return loop != null ? loop.ThemeAccuracy(weight) : Mathf.Clamp01(0.5f + weight / 6f);
        }

        /// <summary>
        /// Nota completa. <paramref name="sharesByParameter"/> = % escolhido em cada bloco (soma 1 por fase);
        /// <paramref name="engineLevelsByParameter"/> = níveis de módulo de engine ligados a cada bloco (pode ser nulo).
        /// </summary>
        public static ScoreBreakdown Compute(GenreSO genre, ProjectSize size, float themeAccuracy01,
            float[] sharesByParameter, float[] pointsByParameter, float requiredPoints, float[] engineLevelsByParameter,
            float bugs, GameLoopBalanceSO loop)
        {
            var result = new ScoreBreakdown { theme = Mathf.Clamp01(themeAccuracy01) };
            for (int i = 0; i < result.blocks.Length; i++) { result.blocks[i] = -1f; result.blockDivision[i] = -1f; }
            float tolerance = loop != null ? loop.BlockToleranceRelative : 0.1f;
            float totalPoints = 0f, phaseSum = 0f, divisionSum = 0f;
            int phasesWithChoice = 0;
            int divisionCount = 0;

            for (int p = 0; p < ParameterCatalog.PhaseCount; p++)
            {
                var phase = (ProductionPhase)p;
                var parameters = ParameterCatalog.AvailableFor(phase, size);
                if (parameters.Count == 0) { result.phases[p] = 1f; continue; }
                var ideal = QualityCalculator.IdealPhaseShares(genre, size, phase);
                float phaseExpected = requiredPoints * ParameterCatalog.DurationShare(phase);
                float blockSum = 0f;
                for (int i = 0; i < parameters.Count; i++)
                {
                    int idx = (int)parameters[i];
                    float share = sharesByParameter != null ? Mathf.Max(0f, sharesByParameter[idx]) : 0f;
                    float points = pointsByParameter != null ? Mathf.Max(0f, pointsByParameter[idx]) : 0f;
                    float engine = engineLevelsByParameter != null ? engineLevelsByParameter[idx] : 0f;
                    totalPoints += points;
                    float division = DivisionAccuracy(share, ideal[i], tolerance);
                    float block = BlockScore(division, points, phaseExpected * ideal[i], engine, loop);
                    result.blockDivision[idx] = division;
                    result.blocks[idx] = block;
                    blockSum += block;
                    divisionSum += division;
                    divisionCount++;
                }
                result.phases[p] = blockSum / parameters.Count;
                // Fase de bloco único não tem escolha: não entra na média das fases (adendo 23.18).
                if (parameters.Count > 1) { phaseSum += result.phases[p]; phasesWithChoice++; }
            }

            result.phasesAverage = phasesWithChoice > 0 ? phaseSum / phasesWithChoice : 1f;
            result.divisionAverage = divisionCount > 0 ? divisionSum / divisionCount : 1f;
            float themeShare = loop != null ? loop.ThemeShare : 0.5f;
            float accuracy = themeShare * result.theme + (1f - themeShare) * result.phasesAverage;
            result.bugPenalty = BugCalculator.Penalty01(bugs, totalPoints, loop);
            result.score = Mathf.Clamp(10f * accuracy * (1f - result.bugPenalty), 0f, 10f);
            return result;
        }
    }
}
