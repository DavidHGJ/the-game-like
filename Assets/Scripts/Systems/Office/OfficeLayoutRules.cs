using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Regras de encaixe da mobília no grid do escritório, compartilhadas pelas ferramentas de
    /// autoria: o que ocupa espaço, se uma peça cabe numa célula (no chão ou em cima de outro
    /// móvel) e qual peça está sob uma célula. Lógica pura (sem cena), coberta por testes.
    /// </summary>
    public static class OfficeLayoutRules
    {
        /// <summary>Tapetes e peças de parede não ocupam chão: outras peças podem ficar por cima.</summary>
        public static bool Blocks(FurnitureDef3DSO def) =>
            def != null && def.Category != FurnitureCategory.FloorFixture && !def.IsWallPiece;

        /// <summary>true se a colocação ocupa chão (peça apoiada em outro móvel não ocupa).</summary>
        public static bool BlocksFloor(FurniturePlacement p) => !p.onTop && Blocks(p.furniture);

        /// <summary>true se a célula está dentro do footprint (já girado) da colocação.</summary>
        public static bool Covers(FurniturePlacement p, Vector2Int cell)
        {
            if (p.furniture == null) return false;
            var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
            return cell.x >= p.cell.x && cell.y >= p.cell.y && cell.x < p.cell.x + fp.x && cell.y < p.cell.y + fp.y;
        }

        /// <summary>
        /// true se a peça cabe NO CHÃO com o canto mínimo em <paramref name="cell"/>: dentro da
        /// sala e, se ocupa chão, sem sobrepor outra peça que também ocupa.
        /// <paramref name="ignoreIndex"/> é a própria peça quando ela está sendo movida/girada.
        /// </summary>
        public static bool CanPlace(IReadOnlyList<FurniturePlacement> placements, int cols, int rows,
            FurnitureDef3DSO def, Vector2Int cell, int rotationSteps, int ignoreIndex = -1)
        {
            if (def == null) return false;
            var fp = OfficeGrid.RotatedFootprint(def.FootprintInCells, rotationSteps);
            if (!InsideRoom(cell, fp, cols, rows)) return false;
            if (!Blocks(def)) return true;

            for (int i = 0; i < placements.Count; i++)
            {
                if (i == ignoreIndex || !BlocksFloor(placements[i])) continue;
                if (Overlaps(cell, fp, placements[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Índice do móvel com tampo que sustenta uma peça nesse footprint (-1 se não há): tem
        /// de aceitar peças em cima e cobrir todas as células do footprint.
        /// </summary>
        public static int SupportAt(IReadOnlyList<FurniturePlacement> placements, Vector2Int cell, Vector2Int footprint, int ignoreIndex = -1)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                if (i == ignoreIndex) continue;
                var p = placements[i];
                if (!BlocksFloor(p) || !p.furniture.SupportsItemsOnTop) continue;
                bool all = true;
                for (int x = 0; x < footprint.x && all; x++)
                    for (int y = 0; y < footprint.y && all; y++)
                        all = Covers(p, new Vector2Int(cell.x + x, cell.y + y));
                if (all) return i;
            }
            return -1;
        }

        /// <summary>
        /// Decide onde a peça fica nessa célula. Se ela pode ir em cima de móveis e há um tampo
        /// ali, vai EM CIMA (<paramref name="onTop"/> = true) — desde que não haja outra peça
        /// apoiada no mesmo ponto; senão vale a regra do chão. Devolve false se não cabe.
        /// Duas peças apoiadas dividem a célula quando estão em posições (align) diferentes.
        /// </summary>
        public static bool Resolve(IReadOnlyList<FurniturePlacement> placements, int cols, int rows,
            FurnitureDef3DSO def, Vector2Int cell, int rotationSteps, Vector2Int align, int ignoreIndex, out bool onTop)
        {
            onTop = false;
            if (def == null) return false;
            var fp = OfficeGrid.RotatedFootprint(def.FootprintInCells, rotationSteps);
            if (!InsideRoom(cell, fp, cols, rows)) return false;

            if (def.PlaceableOnTop && !def.IsWallPiece && SupportAt(placements, cell, fp, ignoreIndex) >= 0)
            {
                onTop = true;
                for (int i = 0; i < placements.Count; i++)
                {
                    if (i == ignoreIndex) continue;
                    var other = placements[i];
                    if (!other.onTop || other.furniture == null || !Overlaps(cell, fp, other)) continue;
                    if (other.align == align || other.align == Vector2Int.zero || align == Vector2Int.zero) return false;
                }
                return true;
            }
            return CanPlace(placements, cols, rows, def, cell, rotationSteps, ignoreIndex);
        }

        /// <summary>
        /// Índice da peça sob a célula (-1 se vazia). Prefere a que está apoiada em cima, depois
        /// a que ocupa chão; tapete/peça de parede só é devolvido se não houver outra.
        /// </summary>
        public static int PlacementAt(IReadOnlyList<FurniturePlacement> placements, Vector2Int cell)
        {
            int floor = -1, fallback = -1;
            for (int i = placements.Count - 1; i >= 0; i--)
            {
                if (!Covers(placements[i], cell)) continue;
                if (placements[i].onTop) return i;
                if (Blocks(placements[i].furniture)) { if (floor < 0) floor = i; }
                else if (fallback < 0) fallback = i;
            }
            return floor >= 0 ? floor : fallback;
        }

        /// <summary>
        /// Canto mínimo do footprint para que a peça fique centrada num ponto do chão dado em
        /// coordenadas de grid contínuas (0..cols, 0..rows).
        /// </summary>
        public static Vector2Int AnchorCell(Vector2 gridPoint, Vector2Int rotatedFootprint) =>
            new Vector2Int(Mathf.RoundToInt(gridPoint.x - rotatedFootprint.x * 0.5f),
                           Mathf.RoundToInt(gridPoint.y - rotatedFootprint.y * 0.5f));

        private static bool InsideRoom(Vector2Int cell, Vector2Int fp, int cols, int rows) =>
            cell.x >= 0 && cell.y >= 0 && cell.x + fp.x <= cols && cell.y + fp.y <= rows;

        private static bool Overlaps(Vector2Int cell, Vector2Int fp, FurniturePlacement other)
        {
            var ofp = OfficeGrid.RotatedFootprint(other.furniture.FootprintInCells, other.rotationSteps);
            return !(cell.x + fp.x <= other.cell.x || other.cell.x + ofp.x <= cell.x
                  || cell.y + fp.y <= other.cell.y || other.cell.y + ofp.y <= cell.y);
        }
    }
}
