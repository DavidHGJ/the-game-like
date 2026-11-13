using GameLike.Data.LevelBuilder;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>Camada ativa de edição (LEVEL_BUILDER.md §4).</summary>
    public enum BuildLayer { Floor, Wall, Furniture, Zones }

    /// <summary>Pincel ativo (LEVEL_BUILDER.md §14).</summary>
    public enum BuildBrush { Paint, Erase, Rectangle, Pick }

    /// <summary>
    /// Estado compartilhado do Level Builder entre a <see cref="LevelBuilderWindow"/>
    /// (que o edita) e a <see cref="LevelBuilderTool"/> (que o lê na SceneView).
    /// Persistido entre domain reloads e sessões via <see cref="ScriptableSingleton{T}"/>.
    /// </summary>
    [FilePath("UserSettings/LevelBuilderState.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class LevelBuilderState : ScriptableSingleton<LevelBuilderState>
    {
        public BuildLayer layer = BuildLayer.Floor;
        public BuildBrush brush = BuildBrush.Paint;
        [Min(1)] public int brushSize = 1;
        [Min(0)] public int storey = 0;

        [Tooltip("Tile selecionado na palette para Floor/Wall.")]
        public TileBase selectedTile;

        [Tooltip("Wall: quantos blocos a parede empilha no eixo Z ao pintar uma célula.")]
        [Min(1)] public int wallHeight = 2;

        [Tooltip("Wall: nível Z inicial da coluna (0 = chão). Útil pra colocar blocos avulsos no topo.")]
        [Min(0)] public int wallBaseLevel = 0;

        [Tooltip("Furniture: definição de mobília selecionada para colocar.")]
        public FurnitureDefinitionSO selectedFurniture;

        [Tooltip("Furniture: direção de orientação atual (R cicla — Fase 3b).")]
        public IsoDirection furnitureDirection = IsoDirection.SouthEast;

        [Tooltip("Furniture: nível Z de apoio (altura inicial). 0 = plano do chão; aumente para suspender a peça (não duplica, só posiciona).")]
        [Min(0)] public int furnitureBaseLevel = 0;

        public void Save() => Save(true);
    }
}
