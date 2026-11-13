using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>
    /// Resolve, na cena ativa, os Tilemaps de uma camada/andar do Level Builder
    /// pela convenção de hierarquia criada por <see cref="LevelBuilderSceneSetup"/>
    /// (<c>OfficeGrid → Storey_{n} → Tilemap_Floor / Tilemap_Walls</c>). A fonte
    /// da verdade é a cena (LEVEL_BUILDER.md §4) — não há asset de dados a parte.
    /// </summary>
    public static class LevelBuilderSceneRefs
    {
        /// <summary>Retorna o Tilemap da camada/andar pedidos, ou null se a cena não tiver o setup.</summary>
        public static Tilemap GetTilemap(int storey, BuildLayer layer)
        {
            string tilemapName = layer switch
            {
                BuildLayer.Floor => LevelBuilderSceneSetup.FloorTilemapName,
                BuildLayer.Wall  => LevelBuilderSceneSetup.WallTilemapName,
                _                => null
            };
            if (tilemapName == null) return null;

            var grid = GameObject.Find(LevelBuilderSceneSetup.GridName);
            if (grid == null) return null;

            var storeyTf = grid.transform.Find($"Storey_{storey}");
            if (storeyTf == null) return null;

            var tilemapTf = storeyTf.Find(tilemapName);
            return tilemapTf != null ? tilemapTf.GetComponent<Tilemap>() : null;
        }

        /// <summary>Retorna o componente <see cref="UnityEngine.Grid"/> raiz da cena, ou null.</summary>
        public static UnityEngine.Grid GetGrid()
        {
            var grid = GameObject.Find(LevelBuilderSceneSetup.GridName);
            return grid != null ? grid.GetComponent<UnityEngine.Grid>() : null;
        }

        /// <summary>Retorna o root "Furniture" do andar pedido (irmão dos Tilemaps), ou null.</summary>
        public static Transform GetFurnitureRoot(int storey)
        {
            var grid = GameObject.Find(LevelBuilderSceneSetup.GridName);
            if (grid == null) return null;

            var storeyTf = grid.transform.Find($"Storey_{storey}");
            if (storeyTf == null) return null;

            return storeyTf.Find(LevelBuilderSceneSetup.FurnitureRootName);
        }

        /// <summary>True se a cena tem a fundação do Level Builder (um OfficeGrid).</summary>
        public static bool SceneIsSetup() => GameObject.Find(LevelBuilderSceneSetup.GridName) != null;
    }
}
