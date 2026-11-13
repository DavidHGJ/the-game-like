using UnityEngine;

namespace GameLike.LevelBuilder.Grid
{
    /// <summary>
    /// Helper compartilhado de conversão célula↔mundo para o grid isométrico do
    /// Level Builder (LEVEL_BUILDER.md §5.1), incluindo o eixo de andar
    /// (<c>storey</c>). Editor e runtime consomem exclusivamente este helper —
    /// é <b>proibido</b> reimplementar a matemática isométrica à mão (§5.1).
    ///
    /// A conversão planar delega a <see cref="UnityEngine.Grid.CellToWorld"/> /
    /// <see cref="UnityEngine.Grid.WorldToCell"/> do componente Grid configurado
    /// como "Isometric Z as Y". O andar é um deslocamento vertical em world Y:
    /// <c>storey × <see cref="StoreyWorldHeight"/></c> (§6, empilhamento visual).
    /// </summary>
    public static class IsoGrid
    {
        /// <summary>
        /// Altura em unidades de mundo entre um andar e o seguinte. Deve exceder
        /// a profundidade de tela de um andar para que andares nunca se
        /// interleavem incorretamente (LEVEL_BUILDER.md §6). Default a
        /// <b>calibrar</b> na cena de teste contra a arte real.
        /// </summary>
        public const float StoreyWorldHeight = 4f;

        /// <summary>
        /// Faixa de <c>sortingOrder</c> reservada por andar no fallback de ordem
        /// explícita (<c>IsometricSorter</c>). Larga o bastante para o andar
        /// superior nunca colidir com a faixa do inferior.
        /// </summary>
        public const int StoreyOrderSpan = 10000;

        /// <summary>Converte uma célula (no andar dado) para a posição de mundo do seu ponto de contato.</summary>
        public static Vector3 CellToWorld(UnityEngine.Grid grid, Vector3Int cell, int storey)
        {
            return grid.CellToWorld(cell) + StoreyOffset(storey);
        }

        /// <summary>Converte uma posição de mundo (no andar dado) para a célula correspondente.</summary>
        public static Vector3Int WorldToCell(UnityEngine.Grid grid, Vector3 world, int storey)
        {
            return grid.WorldToCell(world - StoreyOffset(storey));
        }

        /// <summary>Deslocamento de mundo aplicado ao root de um andar.</summary>
        public static Vector3 StoreyOffset(int storey)
        {
            return new Vector3(0f, storey * StoreyWorldHeight, 0f);
        }

        /// <summary>
        /// Fallback de ordem explícita (LEVEL_BUILDER.md §6): calcula o
        /// <c>sortingOrder</c> de um renderer a partir da sua célula frontal (a
        /// mais baixa na tela) e do andar. O caminho primário é o eixo de
        /// transparência custom da câmera; este é o backup para mobília
        /// multi-célula instável.
        ///
        /// Regra: andar superior sempre à frente (faixa própria); dentro de um
        /// andar, células mais "ao fundo" (maior <c>x+y</c>) renderizam atrás.
        /// O sinal da componente planar é <b>calibrado</b> na cena de teste.
        /// </summary>
        public static int SortingOrderForCell(Vector3Int frontCell, int storey)
        {
            return storey * StoreyOrderSpan - (frontCell.x + frontCell.y);
        }
    }
}
