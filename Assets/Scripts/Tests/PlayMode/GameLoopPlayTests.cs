using System.Collections;
using GameLike.Core.Events;
using GameLike.Systems.Core;
using GameLike.Systems.Economy;
using GameLike.Systems.Events;
using GameLike.Systems.Market;
using GameLike.Systems.Marketing;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using GameLike.Systems.Time;
using GameLike.Data.Definitions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLike.Tests.PlayMode
{
    /// <summary>
    /// Smoke test de runtime do loop completo: monta o grafo de managers como a cena
    /// faz, inicia a run, cria um projeto e dirige as semanas manualmente até o
    /// lançamento. Valida que o wiring (Staff, Marketing, Market, Event, Economy)
    /// roda sem exceções e que o ciclo criar→produzir→lançar→receita fecha.
    /// </summary>
    public sealed class GameLoopPlayTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator FullLoop_CreateDevelopRelease_RunsWithoutErrors()
        {
            _go = new GameObject("[GameLikeTest]");
            _go.AddComponent<TimeManager>();
            _go.AddComponent<EconomyManager>();
            _go.AddComponent<ProjectManager>();
            _go.AddComponent<StaffManager>();
            _go.AddComponent<MarketingManager>();
            _go.AddComponent<MarketManager>();
            _go.AddComponent<EventManager>();
            var gc = _go.AddComponent<GameController>();

            // Deixa Awake/Start rodarem (StartNewRun liga os managers).
            yield return null;
            yield return null;

            gc.StartFreshForTesting(9); // slot de teste: nunca toca no save do jogador
            Assert.IsNotNull(gc.Run, "A run deveria iniciar no Start.");
            Assert.IsNotNull(gc.Run.company);
            Assert.Greater(gc.AvailableGenres.Count, 0, "Gêneros deveriam carregar de Resources.");

            // Congela o relógio automático; vamos dirigir as semanas manualmente.
            gc.Time.Pause();

            long startCash = gc.Run.company.cash;
            var genre = gc.AvailableGenres[0];
            ThemeSO theme = gc.AvailableThemes.Count > 0 ? gc.AvailableThemes[0] : null;
            var alloc = new float[] { 5, 5, 5, 5, 5 };

            gc.StartProject("Jogo de Teste", genre, ProjectSize.Small, alloc, theme);
            Assert.IsTrue(gc.Run.HasActiveProject, "O projeto deveria estar em desenvolvimento.");

            int targetWeeks = gc.Run.currentProject.targetWeeks;
            Assert.Greater(targetWeeks, 0);

            // Dirige semanas até lançar (com margem).
            int week = gc.Time.CurrentWeek;
            int year = gc.Time.CurrentYear;
            for (int i = 0; i < targetWeeks + 6 && gc.Run.company.gamesReleased == 0; i++)
            {
                week++;
                EventBus.Publish(new WeekTickEvent(week, year));
                yield return null;
            }

            Assert.AreEqual(1, gc.Run.company.gamesReleased, "O jogo deveria ter sido lançado.");
            Assert.AreEqual(1, gc.Run.releasedProjects.Count);
            var released = gc.Run.releasedProjects[0];
            Assert.GreaterOrEqual(released.finalScore, 0f);
            Assert.LessOrEqual(released.finalScore, 10f);
            Assert.AreNotEqual(startCash, gc.Run.company.cash, "O caixa deveria mudar (custos + receita).");
        }

        [UnityTest]
        public IEnumerator Hiring_AddsEmployee_WhenPhaseAllows()
        {
            _go = new GameObject("[GameLikeTest2]");
            _go.AddComponent<TimeManager>();
            _go.AddComponent<EconomyManager>();
            _go.AddComponent<ProjectManager>();
            _go.AddComponent<StaffManager>();
            _go.AddComponent<MarketingManager>();
            _go.AddComponent<MarketManager>();
            _go.AddComponent<EventManager>();
            var gc = _go.AddComponent<GameController>();

            yield return null;
            yield return null;

            // Garage permite só o fundador (teto 1) → não deve contratar.
            var staff = gc.Staff;
            Assert.IsNotNull(staff);
            Assert.IsFalse(staff.CanHire, "Na Garage (teto 1) não deveria haver espaço para contratar.");
            Assert.Greater(staff.Candidates.Count, 0, "O mercado de talentos deveria ter candidatos.");
        }
    }
}
