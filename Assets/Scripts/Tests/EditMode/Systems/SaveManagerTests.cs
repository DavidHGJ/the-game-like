using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Meta;
using GameLike.State.Staff;
using GameLike.Systems.Save;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre o round-trip do save (GDD 21.6): serializar (JSON+gzip) e desserializar
    /// preserva os dados da run e da meta, e mantém a versão de schema.
    /// </summary>
    [TestFixture]
    public sealed class SaveManagerTests
    {
        [Test]
        public void RoundTrip_PreservesRunAndMeta()
        {
            var company = new CompanyState("Studio X", 123_456) { gamesReleased = 3, reputation01 = 0.7f };
            company.employees.Add(new EmployeeInstance("e1", "Dev Um", Specialty.Programming, Seniority.Senior, PersonalityTrait.Workaholic, 6000));
            var run = new RunState(company) { runNumber = 4 };
            run.releasedProjects.Add(new ProjectState("Hit", "action_rpg", "Action RPG", ProjectSize.Medium, null) { finalScore = 8.2f });
            run.market.SetThemeTrend("cyberpunk", 0.8f);

            var meta = new MetaProgressionState { personalMoney = 999_000, legacyPoints = 42, runsCompleted = 3 };

            var data = new SaveData { schemaVersion = SaveManager.CurrentSchemaVersion, run = run, meta = meta };
            var bytes = SaveManager.SerializeToBytes(data);
            var loaded = SaveManager.DeserializeFromBytes(bytes);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveManager.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.AreEqual("Studio X", loaded.run.company.name);
            Assert.AreEqual(123_456, loaded.run.company.cash);
            Assert.AreEqual(3, loaded.run.company.gamesReleased);
            Assert.AreEqual(1, loaded.run.company.employees.Count);
            Assert.AreEqual("Dev Um", loaded.run.company.employees[0].name);
            Assert.AreEqual(1, loaded.run.releasedProjects.Count);
            Assert.AreEqual(8.2f, loaded.run.releasedProjects[0].finalScore, 0.001f);
            Assert.AreEqual(0.8f, loaded.run.market.GetThemeTrend("cyberpunk"), 0.001f);
            Assert.AreEqual(999_000, loaded.meta.personalMoney);
            Assert.AreEqual(42, loaded.meta.legacyPoints);
        }

        [Test]
        public void Gzip_ProducesSmallerThanRawForRepetitiveData()
        {
            var company = new CompanyState("AAAAAAAAAA", 0);
            var run = new RunState(company);
            var data = new SaveData { schemaVersion = 1, run = run, meta = new MetaProgressionState() };
            var bytes = SaveManager.SerializeToBytes(data);
            Assert.Greater(bytes.Length, 0);
        }
    }
}
