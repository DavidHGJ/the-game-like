using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Economy;
using GameLike.Systems.Research;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Testes do sistema de P&amp;D (GDD 6): custo em PP, desbloqueio após a duração,
    /// cobrança semanal no caixa, tamanho Pequeno sempre livre e a regra de geração de PP
    /// do adendo 23.5 (só em desenvolvimento, ao lançar e ao concluir pesquisa).
    /// </summary>
    [TestFixture]
    public class ResearchManagerTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            foreach (var o in _spawned)
                if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private List<ThemeSO> MakeThemes(params string[] keys)
        {
            var list = new List<ThemeSO>();
            foreach (var k in keys)
            {
                var t = ScriptableObject.CreateInstance<ThemeSO>();
                t.name = k;
                _spawned.Add(t);
                list.Add(t);
            }
            return list;
        }

        private (ResearchManager rm, RunState run, EconomyManager eco) Setup(List<ThemeSO> themes)
        {
            var go = new GameObject("research-test");
            _spawned.Add(go);
            var eco = go.AddComponent<EconomyManager>();
            var rm = go.AddComponent<ResearchManager>();
            var company = new CompanyState("Test", 100_000);
            var run = new RunState(company);
            eco.Bind(company);
            rm.Bind(run, eco, themes);
            return (rm, run, eco);
        }

        [Test]
        public void StartResearch_Basic_UnlocksAfterDuration_AndChargesWeekly()
        {
            var themes = MakeThemes("a", "b", "c"); // a,b semeados; c pesquisável (básico)
            var (rm, run, eco) = Setup(themes);
            run.research.points = 50;
            long cashBefore = eco.Balance;

            Assert.IsTrue(rm.StartResearch("theme:c"), "deveria iniciar a pesquisa");
            Assert.AreEqual(40, run.research.points, "custo básico 10 PP");
            Assert.IsFalse(rm.IsThemeUnlocked("c"), "ainda não desbloqueado durante a pesquisa");

            rm.AdvanceWeek(); // semana 1 de 2
            Assert.IsFalse(rm.IsThemeUnlocked("c"));
            rm.AdvanceWeek(); // semana 2 -> conclui
            Assert.IsTrue(rm.IsThemeUnlocked("c"), "desbloqueado após a duração");

            // Adendo 23.5: pesquisa ativa não gera PP por semana; só +2 PP ao CONCLUIR (40 -> 42).
            Assert.AreEqual(42, run.research.points);
            Assert.AreEqual(cashBefore - 400, eco.Balance, "pesquisa custa dinheiro por semana");
        }

        [Test]
        public void SmallSizeIsFree_MediumLockedByDefault()
        {
            var (rm, _, _) = Setup(MakeThemes("a", "b"));
            Assert.IsTrue(rm.IsSizeUnlocked(ProjectSize.Small));
            Assert.IsFalse(rm.IsSizeUnlocked(ProjectSize.Medium));
        }

        [Test]
        public void Idle_NeverGeneratesPP()
        {
            // Adendo 23.5: PP nunca nasce com a empresa parada.
            var (rm, run, _) = Setup(MakeThemes("a", "b"));
            run.research.points = 0;
            for (int i = 0; i < 10; i++) rm.AdvanceWeek();
            Assert.AreEqual(0, run.research.points);
        }

        [Test]
        public void Developing_GeneratesPPPerWeek_ButNotWhileAwaitingAllocation()
        {
            var (rm, run, _) = Setup(MakeThemes("a", "b"));
            run.research.points = 0;
            run.currentProject = new ProjectState("Jogo", "g", "G", ProjectSize.Small, null)
            {
                status = ProjectStatus.InDevelopment,
                awaitingAllocation = true
            };
            rm.AdvanceWeek();
            Assert.AreEqual(0, run.research.points, "esperando alocação não é desenvolver");

            run.currentProject.awaitingAllocation = false;
            rm.AdvanceWeek();
            Assert.AreEqual(2, run.research.points, "+2 PP por semana de desenvolvimento");
        }

        [Test]
        public void GameLaunched_GivesPP()
        {
            var (rm, run, _) = Setup(MakeThemes("a", "b"));
            run.research.points = 0;
            rm.RegisterGameLaunched();
            Assert.AreEqual(5, run.research.points);
        }

        [Test]
        public void StarterEngine_Has200EngineBudget()
        {
            var (rm, _, _) = Setup(MakeThemes("a", "b"));
            Assert.AreEqual(200, rm.EngineBudget, "engine básica = 2 módulos leves (GDD 19.4)");
        }

        [Test]
        public void StartResearch_FailsWithoutEnoughPP()
        {
            var (rm, run, _) = Setup(MakeThemes("a", "b", "c"));
            run.research.points = 5; // básico custa 10
            Assert.IsFalse(rm.StartResearch("theme:c"));
            Assert.AreEqual(5, run.research.points);
        }
    }
}
