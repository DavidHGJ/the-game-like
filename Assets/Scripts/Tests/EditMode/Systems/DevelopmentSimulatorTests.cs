using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using GameLike.Systems.Projects;
using GameLike.Systems.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Suíte do simulador de desenvolvimento (adendo 23.16): ele precisa produzir os mesmos pontos que o
    /// jogo e responder às alavancas certas (aptidão, distribuição, treino, equipe).
    /// </summary>
    [TestFixture]
    public sealed class DevelopmentSimulatorTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private GenreSO _genre;

        [SetUp]
        public void SetUp()
        {
            FounderPerks.Bind(null);
            _genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(_genre);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
            EventBus.Clear();
        }

        private List<DevGameResult> Run(DevScenario scenario) => DevelopmentSimulator.Run(scenario, _genre, 1);

        [Test]
        public void Simulator_FirstGame_MatchesTheRealProjectManager()
        {
            var aptitude = new[] { 4f, 6f, 3f, 5f, 4.5f };
            var sim = Run(new DevScenario { founderAptitude = aptitude, games = 1 })[0];

            var go = new GameObject("dev-parity");
            _spawned.Add(go);
            var eco = go.AddComponent<EconomyManager>();
            var pm = go.AddComponent<ProjectManager>();
            var company = new CompanyState("Test", 1_000_000);
            var run = new RunState(company) { founderAptitude = (float[])aptitude.Clone() };
            eco.Bind(company);
            pm.Bind(run, eco, null, null, _ => _genre, _ => null);
            var project = new ProjectState("Jogo", _genre.Id, "G", ProjectSize.Small, null);
            pm.StartProject(_genre, null, project);
            int guard = 0;
            while (project.status == ProjectStatus.InDevelopment && guard++ < 200)
            {
                if (project.awaitingAllocation) pm.ConfirmAllocation(pm.IdealAllocation(project), false);
                pm.AdvanceWeek();
            }
            float real = 0f;
            foreach (float v in project.paramPoints) real += v;

            Assert.AreEqual(real, sim.points, 0.01f, "o simulador usa as mesmas regras do jogo");
        }

        [Test]
        public void Practice_RaisesAptitudeGameAfterGame()
        {
            var games = Run(new DevScenario { founderAptitude = new[] { 3f, 3f, 3f, 3f, 3f }, games = 10 });
            Assert.Greater(games[9].founderAptitude, games[0].founderAptitude);
            Assert.GreaterOrEqual(games[9].points, games[0].points, "mais aptidão, mais pontos");
        }

        [Test]
        public void BetterFounder_ScoresHigher()
        {
            float beginner = Run(new DevScenario { founderAptitude = DevelopmentReport.Beginner, games = 1 })[0].score;
            float allRounder = Run(new DevScenario { founderAptitude = DevelopmentReport.AllRounder, games = 1 })[0].score;
            Assert.Greater(allRounder, beginner);
        }

        [Test]
        public void Shares_AlwaysSumToOne_AndIdealIsNotWorseThanEven()
        {
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);
            // gênero sem pesos tem ideal = dividir igual; o invertido precisa de um ideal desigual para diferir
            var shares = DevelopmentSimulator.Shares(genre, ProjectSize.Small, ProductionPhase.PreProduction, DevAllocation.Inverted);
            float sum = 0f;
            foreach (float s in shares) sum += s;
            Assert.AreEqual(1f, sum, 0.001f, "qualquer estratégia soma 100%");

            float ideal = Run(new DevScenario { founderAptitude = DevelopmentReport.AllRounder, games = 1 })[0].phasesAccuracy;
            float even = Run(new DevScenario { founderAptitude = DevelopmentReport.AllRounder, games = 1, allocation = DevAllocation.Even })[0].phasesAccuracy;
            Assert.GreaterOrEqual(ideal, even - 0.001f);
        }

        [Test]
        public void Training_RaisesTheWeakestArea_AndCosts()
        {
            var none = Run(new DevScenario { founderAptitude = DevelopmentReport.Specialist, games = 6 });
            var trained = Run(new DevScenario { founderAptitude = DevelopmentReport.Specialist, games = 6, training = DevTraining.WeakestEachGame });
            Assert.Greater(trained[5].founderWeakest, none[5].founderWeakest);
            long cost = 0;
            foreach (var g in trained) cost += g.trainingCost;
            Assert.Greater(cost, 0);
        }

        [Test]
        public void Team_ProducesMorePoints_ThanTheFounderAlone()
        {
            var solo = Run(new DevScenario { founderAptitude = DevelopmentReport.AllRounder, size = ProjectSize.Medium, games = 1 })[0];
            var team = Run(new DevScenario
            {
                founderAptitude = DevelopmentReport.AllRounder, size = ProjectSize.Medium, games = 1,
                hires = { new DevHire(Specialty.Art, Seniority.Junior), new DevHire(Specialty.Programming, Seniority.Junior) }
            })[0];
            Assert.Greater(team.points, solo.points);
        }


        /// <summary>Gênero com pesos bem desiguais, para a divisão certa e a errada diferirem.</summary>
        private GenreSO WeightedGenre()
        {
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);
            string W(int v) => "{\"small\":" + v + ",\"medium\":" + v + ",\"large\":" + v + ",\"aaa\":" + v + "}";
            JsonUtility.FromJsonOverwrite("{\"gameDesign\":" + W(3) + ",\"narrative\":" + W(-3) + ",\"artDirection\":" + W(1) +
                ",\"gameFeel\":" + W(2) + ",\"scope\":" + W(-2) + ",\"graphics\":" + W(3) + ",\"animation\":" + W(-3) +
                ",\"userInterface\":" + W(0) + ",\"vfx\":" + W(-1) + ",\"soundtrack\":" + W(3) + ",\"soundDesign\":" + W(-3) +
                ",\"voiceActing\":" + W(-2) + ",\"programming\":" + W(3) + ",\"performanceOptimization\":" + W(-2) +
                ",\"stabilityTesting\":" + W(0) + ",\"gameplay\":" + W(3) + ",\"content\":" + W(-3) +
                ",\"implementedNarrative\":" + W(-2) + ",\"balancing\":" + W(1) + "}", genre);
            return genre;
        }

        private float Score(GenreSO genre, float[] aptitude, DevAllocation allocation, DevCombo combo = DevCombo.Neutral) =>
            DevelopmentSimulator.Run(new DevScenario { founderAptitude = aptitude, allocation = allocation, combo = combo, games = 1 }, genre, 1)[0].score;

        [Test]
        public void Expert_WithWrongDivision_ScoresBelow_Average_WithRightDivision()
        {
            var genre = WeightedGenre();
            float expertWrong = Score(genre, DevelopmentReport.Expert, DevAllocation.Inverted);
            float averageRight = Score(genre, DevelopmentReport.AllRounder, DevAllocation.Ideal);
            Assert.Less(expertWrong, averageRight, "aptidão alta não compensa a divisão errada (GDD 23.17)");
        }

        [Test]
        public void RightDivision_Ranks_Good_Even_Wrong_ForEveryTeam()
        {
            var genre = WeightedGenre();
            foreach (var team in new[] { DevelopmentReport.Beginner, DevelopmentReport.AllRounder, DevelopmentReport.Expert })
            {
                float good = Score(genre, team, DevAllocation.Ideal);
                float even = Score(genre, team, DevAllocation.Even);
                float wrong = Score(genre, team, DevAllocation.Inverted);
                Assert.Greater(good, even);
                Assert.Greater(even, wrong);
            }
        }

        [Test]
        public void WithRightDivision_MoreAptitude_ScoresHigher()
        {
            var genre = WeightedGenre();
            float beginner = Score(genre, DevelopmentReport.Beginner, DevAllocation.Ideal);
            float average = Score(genre, DevelopmentReport.AllRounder, DevAllocation.Ideal);
            float expert = Score(genre, DevelopmentReport.Expert, DevAllocation.Ideal);
            Assert.Greater(average, beginner);
            Assert.Greater(expert, average, "as dobras acima do teto ainda valem");
        }

        [Test]
        public void GoodGenreThemeCombo_ScoresAbove_BadCombo()
        {
            var genre = WeightedGenre();
            Assert.Greater(Score(genre, DevelopmentReport.AllRounder, DevAllocation.Ideal, DevCombo.Good),
                Score(genre, DevelopmentReport.AllRounder, DevAllocation.Ideal, DevCombo.Bad));
        }

        [Test]
        public void Sweep_CoversTheWholeRange_AndIsCoherent()
        {
            var rows = DevelopmentSweep.RunAll(new[] { WeightedGenre() }, 1);
            Assert.AreEqual(DevelopmentSweep.Sizes.Length * DevelopmentSweep.Teams.Length * DevelopmentSweep.Divisions.Length *
                            DevelopmentSweep.Themes.Length * DevelopmentSweep.Engines.Length, rows.Count);
            var problems = DevelopmentSweep.Violations(rows);
            Assert.IsEmpty(problems, "melhorar divisão, tema, equipe ou engine nunca pode baixar a nota:\n" + string.Join("\n", problems.GetRange(0, System.Math.Min(5, problems.Count))));
        }
        [Test]
        public void Report_RunsAllStandardScenarios()
        {
            var results = DevelopmentReport.RunAll(new[] { _genre }, 2);
            Assert.AreEqual(DevelopmentReport.StandardScenarios().Count, results.Count);
            string md = DevelopmentReport.ToMarkdown(results, 1, 2);
            StringAssert.Contains("Matriz", md);
            foreach (var r in results) Assert.AreEqual(r.scenario.games, r.games.Count);
        }
    }
}
