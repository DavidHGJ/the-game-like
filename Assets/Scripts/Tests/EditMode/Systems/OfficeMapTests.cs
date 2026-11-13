using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Regras de encaixe do grid do escritório e ida-e-volta do cenário em JSON
    /// (o formato salvo pelo Map Builder in-game).
    /// </summary>
    [TestFixture]
    public sealed class OfficeMapTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private FurnitureDef3DSO Def(string name, int width, int depth, FurnitureCategory category, bool wall = false)
        {
            var def = ScriptableObject.CreateInstance<FurnitureDef3DSO>();
            def.name = name;
            def.AssignIdIfEmpty("id-" + name);
            var so = new SerializedObject(def);
            so.FindProperty("footprintInCells").vector2IntValue = new Vector2Int(width, depth);
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("isWallPiece").boolValue = wall;
            so.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(def);
            return def;
        }

        private FurnitureCatalogSO Catalog(params FurnitureDef3DSO[] defs)
        {
            var catalog = ScriptableObject.CreateInstance<FurnitureCatalogSO>();
            catalog.SetContents(defs, null);
            _created.Add(catalog);
            return catalog;
        }

        private static FurniturePlacement At(FurnitureDef3DSO def, int x, int y, int rotation = 0) =>
            new FurniturePlacement { furniture = def, cell = new Vector2Int(x, y), rotationSteps = rotation };

        [Test]
        public void CanPlace_EmptyRoomInsideBounds_ReturnsTrue()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            Assert.IsTrue(OfficeLayoutRules.CanPlace(new List<FurniturePlacement>(), 8, 7, desk, new Vector2Int(6, 6), 0));
        }

        [Test]
        public void CanPlace_FootprintCrossesWall_ReturnsFalse()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var none = new List<FurniturePlacement>();
            Assert.IsFalse(OfficeLayoutRules.CanPlace(none, 8, 7, desk, new Vector2Int(7, 0), 0));
            Assert.IsFalse(OfficeLayoutRules.CanPlace(none, 8, 7, desk, new Vector2Int(-1, 0), 0));
        }

        [Test]
        public void CanPlace_RotationSwapsFootprint_ChecksRotatedBounds()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var none = new List<FurniturePlacement>();
            Assert.IsTrue(OfficeLayoutRules.CanPlace(none, 8, 7, desk, new Vector2Int(7, 0), 1));
            Assert.IsFalse(OfficeLayoutRules.CanPlace(none, 8, 7, desk, new Vector2Int(7, 6), 1));
        }

        [Test]
        public void CanPlace_OverlapsBlockingPiece_ReturnsFalse()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var chair = Def("chair", 1, 1, FurnitureCategory.Chair);
            var placed = new List<FurniturePlacement> { At(desk, 2, 2) };
            Assert.IsFalse(OfficeLayoutRules.CanPlace(placed, 8, 7, chair, new Vector2Int(3, 2), 0));
            Assert.IsTrue(OfficeLayoutRules.CanPlace(placed, 8, 7, chair, new Vector2Int(4, 2), 0));
        }

        [Test]
        public void CanPlace_RugAndWallPieceDoNotBlock_ReturnsTrue()
        {
            var rug = Def("rug", 2, 2, FurnitureCategory.FloorFixture);
            var poster = Def("poster", 1, 1, FurnitureCategory.WallFixture, true);
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var placed = new List<FurniturePlacement> { At(rug, 2, 2), At(poster, 2, 0) };
            Assert.IsTrue(OfficeLayoutRules.CanPlace(placed, 8, 7, desk, new Vector2Int(2, 2), 0));
            Assert.IsTrue(OfficeLayoutRules.CanPlace(placed, 8, 7, desk, new Vector2Int(2, 0), 0));
        }

        [Test]
        public void CanPlace_IgnoresThePieceBeingMoved_ReturnsTrue()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var placed = new List<FurniturePlacement> { At(desk, 2, 2) };
            Assert.IsFalse(OfficeLayoutRules.CanPlace(placed, 8, 7, desk, new Vector2Int(3, 2), 0));
            Assert.IsTrue(OfficeLayoutRules.CanPlace(placed, 8, 7, desk, new Vector2Int(3, 2), 0, 0));
        }

        [Test]
        public void PlacementAt_DeskOverRug_PrefersTheDesk()
        {
            var rug = Def("rug", 2, 2, FurnitureCategory.FloorFixture);
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var placed = new List<FurniturePlacement> { At(desk, 2, 2), At(rug, 2, 2) };
            Assert.AreEqual(0, OfficeLayoutRules.PlacementAt(placed, new Vector2Int(3, 2)));
            Assert.AreEqual(1, OfficeLayoutRules.PlacementAt(placed, new Vector2Int(3, 3)));
            Assert.AreEqual(-1, OfficeLayoutRules.PlacementAt(placed, new Vector2Int(6, 6)));
        }

        [Test]
        public void AnchorCell_PointerAtFootprintCenter_ReturnsMinCorner()
        {
            Assert.AreEqual(new Vector2Int(3, 4), OfficeLayoutRules.AnchorCell(new Vector2(4f, 4.5f), new Vector2Int(2, 1)));
            Assert.AreEqual(new Vector2Int(3, 4), OfficeLayoutRules.AnchorCell(new Vector2(3.6f, 4.4f), new Vector2Int(1, 1)));
        }

        [Test]
        public void MapData_JsonRoundTrip_PreservesLayout()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var chair = Def("chair", 1, 1, FurnitureCategory.Chair);
            var catalog = Catalog(desk, chair);

            var source = new OfficeMapData
            {
                name = "Teste", phase = CompanyPhase.Indie, usedInGame = false, cols = 10, rows = 9,
                floorColor = new Color(0.1f, 0.2f, 0.3f), wallColor = new Color(0.4f, 0.5f, 0.6f)
            };
            source.pieces.Add(new OfficeMapPiece { furnitureId = desk.Id, furnitureKey = "desk", cell = new Vector2Int(1, 2), rotationSteps = 1 });
            source.pieces.Add(new OfficeMapPiece { furnitureId = chair.Id, furnitureKey = "chair", cell = new Vector2Int(1, 4), rotationSteps = 2 });
            source.seats.Add(new SeatPoint { cell = new Vector2Int(1, 4), rotationSteps = 1 });
            source.lights.Add(new CeilingLight { cell = new Vector2Int(5, 5), intensity = 2f });

            var parsed = OfficeMapStore.Parse(OfficeMapStore.ToJson(source), "fallback");
            var layout = OfficeLayoutSO.FromMapData(parsed, catalog);
            _created.Add(layout);

            Assert.AreEqual(CompanyPhase.Indie, layout.Phase);
            Assert.AreEqual(10, layout.Cols);
            Assert.AreEqual(9, layout.Rows);
            Assert.AreEqual(2, layout.Placements.Count);
            Assert.AreSame(desk, layout.Placements[0].furniture);
            Assert.AreEqual(new Vector2Int(1, 2), layout.Placements[0].cell);
            Assert.AreEqual(1, layout.Placements[0].rotationSteps);
            Assert.AreEqual(1, layout.Seats.Count);
            Assert.AreEqual(1, layout.CeilingLights.Count);

            var back = layout.ToMapData("Teste", false);
            Assert.AreEqual(desk.Id, back.pieces[0].furnitureId);
            Assert.AreEqual("chair", back.pieces[1].furnitureKey);
            Assert.IsFalse(back.usedInGame);
        }

        [Test]
        public void ApplyMapData_UnknownFurniture_IsDroppedAndCounted()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            var catalog = Catalog(desk);
            var data = new OfficeMapData();
            data.pieces.Add(new OfficeMapPiece { furnitureId = "id-que-nao-existe", furnitureKey = "sumiu" });
            data.pieces.Add(new OfficeMapPiece { furnitureId = "id-antigo", furnitureKey = "desk" });

            var layout = ScriptableObject.CreateInstance<OfficeLayoutSO>();
            _created.Add(layout);
            int missing = layout.ApplyMapData(data, catalog);

            Assert.AreEqual(1, missing);
            Assert.AreEqual(1, layout.Placements.Count);
            Assert.AreSame(desk, layout.Placements[0].furniture, "sem Id válido, a peça é resolvida pelo nome do asset");
        }

        [Test]
        public void Resolve_SmallPieceOverTabletop_GoesOnTop()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            desk.SetStacking(false, true, 0.75f);
            var lamp = Def("lamp", 1, 1, FurnitureCategory.Decoration);
            lamp.SetStacking(true, false, 0f);
            var placed = new List<FurniturePlacement> { At(desk, 2, 2) };

            Assert.IsTrue(OfficeLayoutRules.Resolve(placed, 8, 7, lamp, new Vector2Int(3, 2), 0, Vector2Int.zero, -1, out bool onTop));
            Assert.IsTrue(onTop);
            Assert.IsTrue(OfficeLayoutRules.Resolve(placed, 8, 7, lamp, new Vector2Int(5, 5), 0, Vector2Int.zero, -1, out onTop));
            Assert.IsFalse(onTop, "sem móvel embaixo, a peça fica no chão");
        }

        [Test]
        public void Resolve_PieceThatCannotStack_IsBlockedByTheDesk()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            desk.SetStacking(false, true, 0.75f);
            var chair = Def("chair", 1, 1, FurnitureCategory.Chair);
            var placed = new List<FurniturePlacement> { At(desk, 2, 2) };
            Assert.IsFalse(OfficeLayoutRules.Resolve(placed, 8, 7, chair, new Vector2Int(2, 2), 0, Vector2Int.zero, -1, out _));
        }

        [Test]
        public void Resolve_TwoPiecesOnSameTabletopCell_NeedDifferentCorners()
        {
            var desk = Def("desk", 2, 1, FurnitureCategory.Desk);
            desk.SetStacking(false, true, 0.75f);
            var lamp = Def("lamp", 1, 1, FurnitureCategory.Decoration);
            lamp.SetStacking(true, false, 0f);
            var corner = new Vector2Int(-1, -1);
            var first = At(lamp, 2, 2);
            first.onTop = true;
            first.align = corner;
            var placed = new List<FurniturePlacement> { At(desk, 2, 2), first };

            Assert.IsFalse(OfficeLayoutRules.Resolve(placed, 8, 7, lamp, new Vector2Int(2, 2), 0, corner, -1, out _));
            Assert.IsFalse(OfficeLayoutRules.Resolve(placed, 8, 7, lamp, new Vector2Int(2, 2), 0, Vector2Int.zero, -1, out _));
            Assert.IsTrue(OfficeLayoutRules.Resolve(placed, 8, 7, lamp, new Vector2Int(2, 2), 0, new Vector2Int(1, 1), -1, out bool onTop));
            Assert.IsTrue(onTop);
        }

        [Test]
        public void CanPlace_PieceOnTopDoesNotBlockTheFloor()
        {
            var lamp = Def("lamp", 1, 1, FurnitureCategory.Decoration);
            var chair = Def("chair", 1, 1, FurnitureCategory.Chair);
            var rider = At(lamp, 4, 4);
            rider.onTop = true;
            Assert.IsTrue(OfficeLayoutRules.CanPlace(new List<FurniturePlacement> { rider }, 8, 7, chair, new Vector2Int(4, 4), 0));
        }

        [Test]
        public void MapData_JsonRoundTrip_PreservesAlignStackingAndStyles()
        {
            var lamp = Def("lamp", 1, 1, FurnitureCategory.Decoration);
            var catalog = Catalog(lamp);
            var source = new OfficeMapData { floorStyle = SurfaceStyle.WoodPlanks, wallStyle = SurfaceStyle.Brick };
            source.pieces.Add(new OfficeMapPiece { furnitureId = lamp.Id, cell = new Vector2Int(2, 2), align = new Vector2Int(1, -1), onTop = true });

            var layout = OfficeLayoutSO.FromMapData(OfficeMapStore.Parse(OfficeMapStore.ToJson(source), "x"), catalog);
            _created.Add(layout);

            Assert.AreEqual(SurfaceStyle.WoodPlanks, layout.FloorStyle);
            Assert.AreEqual(SurfaceStyle.Brick, layout.WallStyle);
            Assert.AreEqual(new Vector2Int(1, -1), layout.Placements[0].align);
            Assert.IsTrue(layout.Placements[0].onTop);
            Assert.AreEqual(OfficeMapData.CurrentSchemaVersion, layout.ToMapData("x", true).schemaVersion);
        }

        [Test]
        public void Parse_Version1Map_LoadsWithDefaults()
        {
            const string v1 = "{\"schemaVersion\":1,\"name\":\"Antigo\",\"cols\":8,\"rows\":7,\"pieces\":[{\"furnitureKey\":\"lamp\",\"cell\":{\"x\":1,\"y\":1},\"rotationSteps\":0}]}";
            var map = OfficeMapStore.Parse(v1, "x");
            Assert.AreEqual(OfficeMapData.CurrentSchemaVersion, map.schemaVersion);
            Assert.AreEqual(SurfaceStyle.Plain, map.wallStyle);
            Assert.AreEqual(Vector2Int.zero, map.pieces[0].align);
            Assert.IsFalse(map.pieces[0].onTop);
        }

        [Test]
        public void Parse_InvalidJson_ReturnsNull()
        {
            Assert.IsNull(OfficeMapStore.Parse("", "x"));
            Assert.IsNull(OfficeMapStore.Parse("isto não é json", "x"));
        }

        [Test]
        public void SanitizeName_StripsPathCharacters()
        {
            Assert.AreEqual("Garagem_2", OfficeMapStore.SanitizeName("  Garagem 2 "));
            Assert.AreEqual("abc", OfficeMapStore.SanitizeName("../a\\b:c?"));
            Assert.AreEqual("Cenario", OfficeMapStore.SanitizeName("///"));
        }
    }
}
