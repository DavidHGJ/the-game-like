using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEngine;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// O cenário aberto no Map Builder e as operações de edição sobre ele (colocar, mover,
    /// girar, remover, assentos, luzes, tamanho), com desfazer/refazer por snapshots. Não toca
    /// na cena: quem desenha é o <see cref="MapBuilderStage"/>.
    /// </summary>
    internal sealed class MapBuilderSession
    {
        private const int MaxUndo = 80;
        public const int MinCells = 3, MaxCells = 40;

        private readonly FurnitureCatalogSO _catalog;
        private readonly List<string> _undo = new List<string>();
        private readonly List<string> _redo = new List<string>();

        public OfficeLayoutSO Layout { get; }
        public string Name { get; set; } = "NovoCenario";
        public bool UsedInGame { get; set; } = true;
        public bool Dirty { get; private set; }
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        private List<FurniturePlacement> Placements => Layout.EditorPlacements;
        private List<SeatPoint> Seats => Layout.EditorSeats;

        public MapBuilderSession(FurnitureCatalogSO catalog)
        {
            _catalog = catalog;
            Layout = ScriptableObject.CreateInstance<OfficeLayoutSO>();
            Layout.hideFlags = HideFlags.DontSave;
        }

        // ------------------------------------------------------------------ documento

        /// <summary>Abre um cenário, descartando o histórico. Devolve quantas peças não existem mais.</summary>
        public int Load(OfficeMapData data)
        {
            Name = data.name;
            UsedInGame = data.usedInGame;
            _undo.Clear();
            _redo.Clear();
            Dirty = false;
            return Layout.ApplyMapData(data, _catalog);
        }

        /// <summary>Cenário vazio para a fase, no tamanho padrão dela.</summary>
        public static OfficeMapData Blank(CompanyPhase phase, string name)
        {
            Vector2Int size = phase switch
            {
                CompanyPhase.Garage => new Vector2Int(8, 7),
                CompanyPhase.Indie => new Vector2Int(10, 9),
                CompanyPhase.Office => new Vector2Int(14, 11),
                _ => new Vector2Int(18, 14)
            };
            return new OfficeMapData { name = name, phase = phase, cols = size.x, rows = size.y };
        }

        public OfficeMapData Snapshot() => Layout.ToMapData(Name, UsedInGame);

        public void MarkSaved() => Dirty = false;

        /// <summary>Marca alteração em campo que não entra no histórico (nome, uso no jogo).</summary>
        public void Touch() => Dirty = true;

        private void Record()
        {
            _undo.Add(OfficeMapStore.ToJson(Snapshot()));
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);
            _redo.Clear();
            Dirty = true;
        }

        public bool Undo() => Step(_undo, _redo);
        public bool Redo() => Step(_redo, _undo);

        private bool Step(List<string> from, List<string> to)
        {
            if (from.Count == 0) return false;
            to.Add(OfficeMapStore.ToJson(Snapshot()));
            string json = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);
            var data = OfficeMapStore.Parse(json, Name);
            Layout.ApplyMapData(data, _catalog);
            Dirty = true;
            return true;
        }

        // ------------------------------------------------------------------ mobília

        /// <summary>true se a peça cabe ali — no chão ou em cima do móvel que estiver na célula.</summary>
        public bool CanPlace(FurnitureDef3DSO def, Vector2Int cell, int rotation, int ignoreIndex = -1) =>
            Resolve(def, cell, rotation, Vector2Int.zero, ignoreIndex, out _);

        /// <summary>Como <see cref="CanPlace"/>, dizendo se a peça ficaria apoiada em outro móvel.</summary>
        public bool Resolve(FurnitureDef3DSO def, Vector2Int cell, int rotation, Vector2Int align, int ignoreIndex, out bool onTop) =>
            OfficeLayoutRules.Resolve(Placements, Layout.Cols, Layout.Rows, def, cell, rotation, align, ignoreIndex, out onTop);

        /// <summary>Coloca a peça; devolve o índice dela ou -1 se não coube.</summary>
        public int Place(FurnitureDef3DSO def, Vector2Int cell, int rotation, Vector2Int align)
        {
            int steps = def.Rotatable ? Normalize(rotation) : 0;
            if (!Resolve(def, cell, steps, align, -1, out bool onTop)) return -1;
            Record();
            Placements.Add(new FurniturePlacement { furniture = def, cell = cell, rotationSteps = steps, align = align, onTop = onTop });
            return Placements.Count - 1;
        }

        public bool Move(int index, Vector2Int cell)
        {
            if (!Valid(index)) return false;
            var p = Placements[index];
            if (cell == p.cell || !Resolve(p.furniture, cell, p.rotationSteps, p.align, index, out bool onTop)) return false;
            Record();
            ShiftRiders(index, cell - p.cell);
            p.onTop = onTop;
            if (p.furniture.Category == FurnitureCategory.Chair)
            {
                for (int i = 0; i < Seats.Count; i++)
                {
                    if (!OfficeLayoutRules.Covers(p, Seats[i].cell)) continue;
                    var seat = Seats[i];
                    seat.cell += cell - p.cell;
                    Seats[i] = seat;
                }
            }
            p.cell = cell;
            Placements[index] = p;
            SettleRiders();
            return true;
        }

        public bool Rotate(int index, int direction)
        {
            if (!Valid(index)) return false;
            var p = Placements[index];
            if (!p.furniture.Rotatable) return false;
            int steps = Normalize(p.rotationSteps + direction);
            if (!TryFitRotated(p, steps, index, out Vector2Int cell)) return false;
            Record();
            if (cell != p.cell && p.furniture.Category == FurnitureCategory.Chair)
            {
                for (int i = 0; i < Seats.Count; i++)
                {
                    if (!OfficeLayoutRules.Covers(p, Seats[i].cell)) continue;
                    var seat = Seats[i];
                    seat.cell = cell;
                    Seats[i] = seat;
                }
            }
            p.rotationSteps = steps;
            p.cell = cell;
            Placements[index] = p;
            SettleRiders();
            return true;
        }

        /// <summary>
        /// Acha onde a peça cabe depois de girada: primeiro no mesmo canto; se o footprint
        /// girado bater na parede ou em outra peça, na célula livre mais próxima do centro
        /// original (até uma peça de distância) — assim dá para girar encostado na parede.
        /// </summary>
        private bool TryFitRotated(FurniturePlacement p, int steps, int index, out Vector2Int cell)
        {
            cell = p.cell;
            if (FitsRotated(p, cell, steps, index)) return true;

            var oldFootprint = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
            var newFootprint = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, steps);
            var center = new Vector2(p.cell.x + oldFootprint.x * 0.5f, p.cell.y + oldFootprint.y * 0.5f);
            int reach = Mathf.Max(oldFootprint.x, oldFootprint.y);
            float best = float.MaxValue;
            bool found = false;
            for (int dx = -reach; dx <= reach; dx++)
            for (int dy = -reach; dy <= reach; dy++)
            {
                var candidate = new Vector2Int(p.cell.x + dx, p.cell.y + dy);
                if (!FitsRotated(p, candidate, steps, index)) continue;
                var candidateCenter = new Vector2(candidate.x + newFootprint.x * 0.5f, candidate.y + newFootprint.y * 0.5f);
                float distance = (candidateCenter - center).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                cell = candidate;
                found = true;
            }
            return found;
        }

        public void Remove(int index)
        {
            if (!Valid(index)) return;
            Record();
            var p = Placements[index];
            if (p.furniture.Category == FurnitureCategory.Chair)
                Seats.RemoveAll(s => OfficeLayoutRules.Covers(p, s.cell));
            Placements.RemoveAt(index);
            SettleRiders();
        }

        /// <summary>Girar não muda de andar: quem está no chão continua no chão, quem está em cima continua em cima.</summary>
        private bool FitsRotated(FurniturePlacement p, Vector2Int cell, int steps, int index) =>
            Resolve(p.furniture, cell, steps, p.align, index, out bool onTop) && onTop == p.onTop;

        /// <summary>As peças apoiadas num móvel andam junto quando ele é movido.</summary>
        private void ShiftRiders(int supportIndex, Vector2Int delta)
        {
            var support = Placements[supportIndex];
            if (!OfficeLayoutRules.BlocksFloor(support) || !support.furniture.SupportsItemsOnTop) return;
            for (int i = 0; i < Placements.Count; i++)
            {
                var rider = Placements[i];
                if (!rider.onTop || !OfficeLayoutRules.Covers(support, rider.cell)) continue;
                rider.cell += delta;
                Placements[i] = rider;
            }
        }

        /// <summary>
        /// Depois de mexer num móvel, confere as peças apoiadas: a que ficou sem tampo embaixo
        /// desce para o chão se a célula estiver livre; senão é removida. Devolve quantas saíram.
        /// </summary>
        private int SettleRiders()
        {
            int removed = 0;
            for (int i = Placements.Count - 1; i >= 0; i--)
            {
                var rider = Placements[i];
                if (!rider.onTop || rider.furniture == null) continue;
                var fp = OfficeGrid.RotatedFootprint(rider.furniture.FootprintInCells, rider.rotationSteps);
                if (OfficeLayoutRules.SupportAt(Placements, rider.cell, fp, i) >= 0) continue;
                if (OfficeLayoutRules.CanPlace(Placements, Layout.Cols, Layout.Rows, rider.furniture, rider.cell, rider.rotationSteps, i))
                {
                    rider.onTop = false;
                    Placements[i] = rider;
                }
                else
                {
                    Placements.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Muda a posição da peça dentro da célula (centro, borda ou canto).</summary>
        public bool SetAlign(int index, Vector2Int align)
        {
            if (!Valid(index)) return false;
            var p = Placements[index];
            if (p.align == align) return false;
            if (p.onTop && !Resolve(p.furniture, p.cell, p.rotationSteps, align, index, out _)) return false;
            Record();
            p.align = align;
            Placements[index] = p;
            return true;
        }

        /// <summary>
        /// Configura a parede lateral: própria (cor + acabamento) ou igual à do fundo.
        /// <paramref name="record"/> = false enquanto um controle de cor é arrastado.
        /// </summary>
        public void SetSideWall(bool custom, Color color, SurfaceStyle style, bool record)
        {
            if (record) Record();
            else Dirty = true;
            Layout.EditorSetSideWall(custom, color, style);
        }

        /// <summary>Troca o acabamento de chão e parede.</summary>
        public void SetStyles(SurfaceStyle floor, SurfaceStyle wall)
        {
            if (floor == Layout.FloorStyle && wall == Layout.WallStyle) return;
            Record();
            Layout.EditorSetStyles(floor, wall);
        }

        public bool Valid(int index) => index >= 0 && index < Placements.Count && Placements[index].furniture != null;

        // ------------------------------------------------------------------ assentos e luzes

        public bool IsChair(int pieceIndex) =>
            Valid(pieceIndex) && Placements[pieceIndex].furniture.Category == FurnitureCategory.Chair;

        /// <summary>Índice do assento atrelado à cadeira (-1 se a cadeira não é assento).</summary>
        public int SeatOfChair(int pieceIndex)
        {
            if (!IsChair(pieceIndex)) return -1;
            var chair = Placements[pieceIndex];
            for (int i = 0; i < Seats.Count; i++)
                if (OfficeLayoutRules.Covers(chair, Seats[i].cell)) return i;
            return -1;
        }

        /// <summary>Cria o assento na cadeira, ou remove se ela já é um. Devolve false se não é cadeira.</summary>
        public bool ToggleSeat(int pieceIndex)
        {
            if (!IsChair(pieceIndex)) return false;
            int seat = SeatOfChair(pieceIndex);
            Record();
            if (seat >= 0) Seats.RemoveAt(seat);
            else Seats.Add(new SeatPoint { cell = Placements[pieceIndex].cell });
            return true;
        }

        public void ShiftSeat(int seatIndex, int direction)
        {
            int other = seatIndex + direction;
            if (seatIndex < 0 || seatIndex >= Seats.Count || other < 0 || other >= Seats.Count) return;
            Record();
            (Seats[seatIndex], Seats[other]) = (Seats[other], Seats[seatIndex]);
        }

        public void TurnSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= Seats.Count) return;
            Record();
            var seat = Seats[seatIndex];
            seat.rotationSteps = Normalize(seat.rotationSteps + 1);
            Seats[seatIndex] = seat;
        }

        /// <summary>Passa o assento para a próxima atividade padrão (trabalhando → pensando → relaxando → dormindo).</summary>
        public void CycleSeatActivity(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= Seats.Count) return;
            Record();
            var seat = Seats[seatIndex];
            seat.activity = (SeatActivity)(((int)seat.activity + 1) % 4);
            Seats[seatIndex] = seat;
        }

        public void RemoveSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= Seats.Count) return;
            Record();
            Seats.RemoveAt(seatIndex);
        }

        /// <summary>Põe ou tira a luz de teto da célula. Devolve false se a célula está fora da sala.</summary>
        public bool ToggleLight(Vector2Int cell)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= Layout.Cols || cell.y >= Layout.Rows) return false;
            Record();
            var lights = Layout.EditorCeilingLights;
            int index = lights.FindIndex(l => l.cell == cell);
            if (index >= 0) lights.RemoveAt(index);
            else lights.Add(new CeilingLight { cell = cell, color = new Color(1f, 0.95f, 0.85f), intensity = 2.2f });
            return true;
        }

        // ------------------------------------------------------------------ sala

        /// <summary>Redimensiona a sala; o que ficar para fora é removido.</summary>
        public bool Resize(int cols, int rows)
        {
            cols = Mathf.Clamp(cols, MinCells, MaxCells);
            rows = Mathf.Clamp(rows, MinCells, MaxCells);
            if (cols == Layout.Cols && rows == Layout.Rows) return false;
            Record();
            Layout.EditorSetSize(cols, rows);
            Placements.RemoveAll(p =>
            {
                if (p.furniture == null) return true;
                var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                return p.cell.x + fp.x > cols || p.cell.y + fp.y > rows;
            });
            Seats.RemoveAll(s => s.cell.x >= cols || s.cell.y >= rows);
            Layout.EditorCeilingLights.RemoveAll(l => l.cell.x >= cols || l.cell.y >= rows);
            SettleRiders();
            return true;
        }

        /// <summary>
        /// Troca as cores de chão/parede. <paramref name="record"/> = false enquanto o dev
        /// arrasta um controle de cor (só o primeiro ajuste entra no histórico).
        /// </summary>
        public void SetColors(Color floor, Color wall, bool record)
        {
            if (record) Record();
            else Dirty = true;
            Layout.EditorSetColors(floor, wall);
        }

        public void SetPhase(CompanyPhase phase)
        {
            if (phase == Layout.Phase) return;
            Record();
            Layout.EditorSetPhase(phase);
        }

        private static int Normalize(int steps) => ((steps % 4) + 4) % 4;
    }
}
