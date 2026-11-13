using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using UnityEngine;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>Uma peça de mobília dentro de um <see cref="OfficeMapData"/>.</summary>
    [Serializable]
    public sealed class OfficeMapPiece
    {
        /// <summary>Id estável do <see cref="FurnitureDef3DSO"/> (referência que vale).</summary>
        public string furnitureId;
        /// <summary>Nome do asset da peça — só para leitura humana e como fallback do Id.</summary>
        public string furnitureKey;
        public Vector2Int cell;
        public int rotationSteps;
        public Vector2Int align;
        public bool onTop;
    }

    /// <summary>
    /// Cenário de escritório em forma de dado puro (JSON), salvo pelo Map Builder in-game.
    /// É o espelho serializável de um <see cref="OfficeLayoutSO"/>: a mobília é referenciada
    /// pelo Id estável da definição, nunca pelo asset.
    /// </summary>
    [Serializable]
    public sealed class OfficeMapData
    {
        public const int CurrentSchemaVersion = 3;

        public int schemaVersion = CurrentSchemaVersion;
        public string name = "NovoCenario";
        public CompanyPhase phase = CompanyPhase.Garage;
        [Tooltip("true = o jogo usa este cenário na fase indicada.")]
        public bool usedInGame = true;

        public int cols = 8;
        public int rows = 7;
        public float cellSize = 1f;
        public float wallHeight = 2.8f;
        public float wallThickness = 0.2f;
        public Color floorColor = new Color(0.62f, 0.60f, 0.56f);
        public Color wallColor = new Color(0.55f, 0.70f, 0.50f);
        public SurfaceStyle floorStyle = SurfaceStyle.Plain;
        public SurfaceStyle wallStyle = SurfaceStyle.Plain;
        public bool sideWallCustom;
        public Color sideWallColor = new Color(0.55f, 0.70f, 0.50f);
        public SurfaceStyle sideWallStyle = SurfaceStyle.Plain;

        public List<OfficeMapPiece> pieces = new List<OfficeMapPiece>();
        public List<CeilingLight> lights = new List<CeilingLight>();
        public List<SeatPoint> seats = new List<SeatPoint>();
    }
}
