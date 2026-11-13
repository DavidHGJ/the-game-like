using GameLike.Systems.Updates;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>Comparação de versões e leitura da resposta de releases do atualizador.</summary>
    [TestFixture]
    public sealed class GameUpdaterTests
    {
        private const string Release =
            "{\"tag_name\":\"v0.3.0\",\"body\":\"novidades\",\"draft\":false,\"assets\":[" +
            "{\"name\":\"notas.txt\",\"browser_download_url\":\"https://exemplo/notas.txt\",\"size\":10}," +
            "{\"name\":\"TheGameLike-0.3.0-win64.zip\",\"browser_download_url\":\"https://exemplo/jogo.zip\",\"size\":2048}]}";

        [Test]
        public void CompareVersions_NewerPatchMinorMajor_IsPositive()
        {
            Assert.Greater(GameUpdater.CompareVersions("0.2.1", "0.2.0"), 0);
            Assert.Greater(GameUpdater.CompareVersions("v0.10.0", "0.9.9"), 0);
            Assert.Greater(GameUpdater.CompareVersions("1.0", "0.99.99"), 0);
        }

        [Test]
        public void CompareVersions_SameOrOlder_IsNotPositive()
        {
            Assert.AreEqual(0, GameUpdater.CompareVersions("v1.2.0", "1.2"));
            Assert.Less(GameUpdater.CompareVersions("0.1.9", "0.2.0"), 0);
        }

        [Test]
        public void Parse_NewerRelease_ReturnsZipAsset()
        {
            var update = GameUpdater.Parse(Release, "0.2.0");
            Assert.IsNotNull(update);
            Assert.AreEqual("0.3.0", update.version);
            Assert.AreEqual("https://exemplo/jogo.zip", update.downloadUrl);
            Assert.AreEqual(2048, update.sizeBytes);
        }

        [Test]
        public void Parse_InstalledIsCurrentOrNewer_ReturnsNull()
        {
            Assert.IsNull(GameUpdater.Parse(Release, "0.3.0"));
            Assert.IsNull(GameUpdater.Parse(Release, "1.0.0"));
        }

        [Test]
        public void Parse_GarbageOrNoZip_ReturnsNull()
        {
            Assert.IsNull(GameUpdater.Parse("nao e json", "0.1.0"));
            Assert.IsNull(GameUpdater.Parse("{\"tag_name\":\"v9.0.0\",\"assets\":[]}", "0.1.0"));
        }
    }
}
