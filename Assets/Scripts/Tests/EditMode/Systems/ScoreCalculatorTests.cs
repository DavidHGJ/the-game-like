using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Regras da nota "quão perto do perfeito" (adendo 23.18): acerto do bloco, bônus de pontos, amenização
    /// da engine só no bloco ligado, limite de 100%, escala do tema e desconto por bugs.
    /// </summary>
    [TestFixture]
    public sealed class ScoreCalculatorTests
    {
        private GameLoopBalanceSO _loop;
        private GenreSO _genre;

        [SetUp]
        public void SetUp()
        {
            _loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();
            _genre = ScriptableObject.CreateInstance<GenreSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loop);
            Object.DestroyImmediate(_genre);
        }

        [Test]
        public void DivisionAccuracy_ExactOrWithinTolerance_IsPerfect_AndFarIsZero()
        {
            Assert.AreEqual(1f, ScoreCalculator.DivisionAccuracy(0.40f, 0.40f, 0.05f), 0.0001f);
            Assert.AreEqual(1f, ScoreCalculator.DivisionAccuracy(0.43f, 0.40f, 0.1f), 0.0001f, "dentro da tolerância (10% de 40 = 4 p.p.)");
            Assert.AreEqual(0.75f, ScoreCalculator.DivisionAccuracy(0.26f, 0.40f, 0.1f), 0.0001f, "errou 14 p.p., 4 de tolerância: 10 de 40 = 25%");
            Assert.AreEqual(0f, ScoreCalculator.DivisionAccuracy(0f, 0.40f, 0.1f), 0.0001f, "zero num bloco = erro total, mesmo com tolerância");
            Assert.AreEqual(0f, ScoreCalculator.DivisionAccuracy(0f, 0.40f, 0f), 0.0001f, "nada no bloco = errou tudo");
        }

        [Test]
        public void BlockScore_PointsAddHalfOfTheIdealReached()
        {
            // exemplo do dev: acerto 35%, fez 100 de 300 pontos (33%) → +16,5 = 51,5%
            Assert.AreEqual(0.35f + 0.5f / 3f, ScoreCalculator.BlockScore(0.35f, 100f, 300f, 0f, _loop), 0.001f);
            Assert.AreEqual(0.85f, ScoreCalculator.BlockScore(0.35f, 900f, 300f, 0f, _loop), 0.001f, "pontos contam só até o ideal");
        }

        [Test]
        public void BlockScore_EngineRelievesPartOfWhatIsMissing_NeverAllOfIt()
        {
            float withoutEngine = ScoreCalculator.BlockScore(0.35f, 100f, 300f, 0f, _loop);
            float maxEngine = ScoreCalculator.BlockScore(0.35f, 100f, 300f, 99f, _loop);
            Assert.AreEqual(withoutEngine + (1f - withoutEngine) * _loop.EngineReliefMax, maxEngine, 0.001f);
            Assert.Less(maxEngine, 1f, "a engine ameniza, não resolve");
        }

        [Test]
        public void BlockScore_AboveHundred_IsHundred()
        {
            Assert.AreEqual(1f, ScoreCalculator.BlockScore(0.9f, 300f, 300f, 4f, _loop), 0.0001f, "passou de 100% = bloco perfeito");
        }

        [Test]
        public void ThemeScale_FollowsTheDevTable()
        {
            float[] expected = { 0f, 0.15f, 0.35f, 0.5f, 0.65f, 0.85f, 1f };
            for (int w = -3; w <= 3; w++) Assert.AreEqual(expected[w + 3], _loop.ThemeAccuracy(w), 0.0001f);
        }

        private float[] IdealShares(ProjectSize size)
        {
            var shares = new float[ParameterCatalog.ParameterCount];
            for (int p = 0; p < ParameterCatalog.PhaseCount; p++)
            {
                var phase = (ProductionPhase)p;
                var parameters = ParameterCatalog.AvailableFor(phase, size);
                var ideal = QualityCalculator.IdealPhaseShares(_genre, size, phase);
                for (int i = 0; i < parameters.Count; i++) shares[(int)parameters[i]] = ideal[i];
            }
            return shares;
        }

        [Test]
        public void PerfectGame_ScoresTen()
        {
            var shares = IdealShares(ProjectSize.Small);
            var points = new float[ParameterCatalog.ParameterCount];
            var result = ScoreCalculator.Compute(_genre, ProjectSize.Small, 1f, shares, points, 24f, null, 0f, _loop);
            Assert.AreEqual(10f, result.score, 0.001f, "tema +++ e divisão perfeita, sem bugs = 10");
        }

        [Test]
        public void ThemeAndPhases_AreHalfEach()
        {
            var shares = IdealShares(ProjectSize.Small);
            var points = new float[ParameterCatalog.ParameterCount];
            Assert.AreEqual(5f, ScoreCalculator.Compute(_genre, ProjectSize.Small, 0f, shares, points, 24f, null, 0f, _loop).score, 0.001f,
                "tema −−− com divisão perfeita = metade");
            var nothing = new float[ParameterCatalog.ParameterCount];
            Assert.AreEqual(5f, ScoreCalculator.Compute(_genre, ProjectSize.Small, 1f, nothing, points, 24f, null, 0f, _loop).score, 0.001f,
                "tema +++ sem pôr nada nos blocos e sem pontos = exatamente metade (fase sem escolha não conta)");
        }

        [Test]
        public void EngineHelpsOnlyItsOwnBlock()
        {
            var nothing = new float[ParameterCatalog.ParameterCount];
            var points = new float[ParameterCatalog.ParameterCount];
            var engine = new float[ParameterCatalog.ParameterCount];
            engine[(int)GameParameter.SoundDesign] = 4f;
            var result = ScoreCalculator.Compute(_genre, ProjectSize.Medium, 0.5f, nothing, points, 126f, engine, 0f, _loop);
            Assert.Greater(result.blocks[(int)GameParameter.SoundDesign], 0f);
            Assert.AreEqual(result.blockDivision[(int)GameParameter.Soundtrack], result.blocks[(int)GameParameter.Soundtrack], 0.0001f,
                "a engine de áudio não ameniza a trilha");
            Assert.Greater(result.blocks[(int)GameParameter.SoundDesign], result.blockDivision[(int)GameParameter.SoundDesign]);
        }

        [Test]
        public void Bugs_DiscountByProportionToPoints()
        {
            Assert.AreEqual(0.15f, BugCalculator.Penalty01(15f, 100f, _loop), 0.0001f, "15 bugs em 100 pontos = −15%");
            Assert.Less(BugCalculator.Penalty01(5f, 100f, _loop), BugCalculator.Penalty01(15f, 100f, _loop));
            Assert.AreEqual(_loop.BugPenaltyMax, BugCalculator.Penalty01(1000f, 100f, _loop), 0.0001f, "o desconto tem teto");
        }
    }
}
