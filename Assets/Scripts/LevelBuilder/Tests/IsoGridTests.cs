using GameLike.LevelBuilder.Grid;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.LevelBuilder.Tests
{
    /// <summary>
    /// Cobre a lógica determinística de <see cref="IsoGrid"/> (LEVEL_BUILDER.md
    /// §17): round-trip célula↔mundo incluindo o eixo de andar, o deslocamento
    /// de andar e a ordenação de fallback.
    /// </summary>
    [TestFixture]
    public sealed class IsoGridTests
    {
        private GameObject _gridGO;
        private UnityEngine.Grid _grid;

        [SetUp]
        public void SetUp()
        {
            _gridGO = new GameObject("TestGrid");
            _grid = _gridGO.AddComponent<UnityEngine.Grid>();
            _grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            _grid.cellSize   = new Vector3(1f, 0.5f, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gridGO);
        }

        [Test]
        public void CellToWorld_WorldToCell_RoundTrips_AtGroundStorey()
        {
            foreach (var cell in SampleCells())
            {
                var world = IsoGrid.CellToWorld(_grid, cell, storey: 0);
                var back  = IsoGrid.WorldToCell(_grid, world, storey: 0);
                Assert.AreEqual(cell, back, $"Round-trip falhou para {cell} no andar 0.");
            }
        }

        [Test]
        public void CellToWorld_WorldToCell_RoundTrips_AtUpperStorey()
        {
            const int storey = 3;
            foreach (var cell in SampleCells())
            {
                var world = IsoGrid.CellToWorld(_grid, cell, storey);
                var back  = IsoGrid.WorldToCell(_grid, world, storey);
                Assert.AreEqual(cell, back, $"Round-trip falhou para {cell} no andar {storey}.");
            }
        }

        [Test]
        public void StoreyOffset_AddsExpectedWorldHeightPerStorey()
        {
            Assert.AreEqual(Vector3.zero, IsoGrid.StoreyOffset(0));
            Assert.AreEqual(new Vector3(0f, IsoGrid.StoreyWorldHeight, 0f), IsoGrid.StoreyOffset(1));
            Assert.AreEqual(new Vector3(0f, 2f * IsoGrid.StoreyWorldHeight, 0f), IsoGrid.StoreyOffset(2));
        }

        [Test]
        public void CellToWorld_HigherStorey_IsOffsetUpByStoreyHeight()
        {
            var cell = new Vector3Int(2, 3, 0);
            var w0 = IsoGrid.CellToWorld(_grid, cell, 0);
            var w1 = IsoGrid.CellToWorld(_grid, cell, 1);
            Assert.AreEqual(w0 + new Vector3(0f, IsoGrid.StoreyWorldHeight, 0f), w1);
        }

        [Test]
        public void SortingOrderForCell_UpperStorey_RendersInFrontBand()
        {
            int lower = IsoGrid.SortingOrderForCell(new Vector3Int(0, 0, 0), storey: 0);
            int upper = IsoGrid.SortingOrderForCell(new Vector3Int(0, 0, 0), storey: 1);
            Assert.Greater(upper, lower, "Andar superior deve cair numa faixa de ordem maior (à frente).");
        }

        [Test]
        public void SortingOrderForCell_DeeperCell_RendersBehind_WithinSameStorey()
        {
            int front = IsoGrid.SortingOrderForCell(new Vector3Int(0, 0, 0), storey: 0);
            int back  = IsoGrid.SortingOrderForCell(new Vector3Int(3, 3, 0), storey: 0);
            Assert.Greater(front, back, "Célula mais ao fundo (maior x+y) deve ter ordem menor.");
        }

        private static Vector3Int[] SampleCells() => new[]
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(3, 5, 0),
            new Vector3Int(-2, 4, 0),
            new Vector3Int(-6, -7, 0),
        };
    }
}
