using System.Reflection;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre a lógica determinística da nota de qualidade (slice): o perfil ideal
    /// do gênero é uma distribuição válida, e uma alocação bem casada com o gênero
    /// pontua mais que uma mal casada. Usa reflection para configurar pesos de
    /// gênero sem depender do editor ou de assets em disco.
    /// </summary>
    [TestFixture]
    public sealed class QualityCalculatorTests
    {
        private GenreSO _genre;

        [SetUp]
        public void SetUp() => _genre = ScriptableObject.CreateInstance<GenreSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_genre);

        private static void SetParam(GenreSO genre, string field, int value)
        {
            var f = typeof(GenreSO).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Campo de gênero inexistente: {field}");
            var weight = new ParameterWeightBySize
            {
                small = (WeightLevel)value,
                medium = (WeightLevel)value,
                large = (WeightLevel)value,
                aaa = (WeightLevel)value
            };
            f.SetValue(genre, weight);
        }

        [Test]
        public void GenreIdealProfile_SumsToOne()
        {
            SetParam(_genre, "content", 3);
            SetParam(_genre, "narrative", 2);

            var profile = QualityCalculator.GenreIdealProfile(_genre, ProjectSize.Medium);

            float sum = 0f;
            foreach (var v in profile) sum += v;
            Assert.AreEqual(1f, sum, 0.001f, "O perfil ideal deve ser uma distribuição (soma 1).");
        }

        [Test]
        public void NeutralGenre_IdealProfileIsUniform()
        {
            var profile = QualityCalculator.GenreIdealProfile(_genre, ProjectSize.Medium);

            float expected = 1f / ProjectState.AreaCount;
            foreach (var v in profile)
                Assert.AreEqual(expected, v, 0.001f, "Gênero neutro deve ter perfil uniforme entre as áreas.");
        }

        [Test]
        public void Compute_IsWithinZeroToTen()
        {
            SetParam(_genre, "content", 3);
            var alloc = new float[] { 1, 1, 1, 1, 1 };

            float score = QualityCalculator.Compute(_genre, ProjectSize.Small, alloc, 0.6f);

            Assert.GreaterOrEqual(score, 0f);
            Assert.LessOrEqual(score, 10f);
        }

        [Test]
        public void MatchedAllocation_ScoresHigherThanMismatched()
        {
            // Gênero que exige fortemente a área Conteúdo.
            SetParam(_genre, "narrative", 3);
            SetParam(_genre, "content", 3);
            SetParam(_genre, "implementedNarrative", 3);

            // Índices de DevArea: Design=0, Art=1, Audio=2, Tech=3, Content=4.
            var matched = new float[] { 0, 0, 0, 0, 10 };    // foco em Conteúdo
            var mismatched = new float[] { 10, 0, 0, 0, 0 };  // foco em Design

            float matchedScore = QualityCalculator.Compute(_genre, ProjectSize.Medium, matched, 0.6f);
            float mismatchedScore = QualityCalculator.Compute(_genre, ProjectSize.Medium, mismatched, 0.6f);

            Assert.Greater(matchedScore, mismatchedScore,
                "Alocar foco onde o gênero pede deve pontuar mais que o contrário.");
        }
    }
}
