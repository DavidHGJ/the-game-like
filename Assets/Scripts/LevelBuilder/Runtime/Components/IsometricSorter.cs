using GameLike.LevelBuilder.Grid;
using UnityEngine;

namespace GameLike.LevelBuilder.Components
{
    /// <summary>
    /// Fallback de ordenação isométrica por ordem explícita
    /// (LEVEL_BUILDER.md §6). Aplica um <c>sortingOrder</c> calculado por
    /// <see cref="IsoGrid.SortingOrderForCell"/> (a partir da célula frontal do
    /// footprint + andar) a todos os <see cref="SpriteRenderer"/> filhos.
    ///
    /// O caminho <b>primário</b> de sorting é o eixo de transparência custom da
    /// câmera (configurado no setup de cena); este componente existe como
    /// backup para casos de mobília multi-célula em que o eixo se mostre
    /// instável. Mantêm-se ambos, conforme o doc.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class IsometricSorter : MonoBehaviour
    {
        [Tooltip("Célula frontal do footprint (a mais baixa na tela) usada como chave de profundidade.")]
        [SerializeField] private Vector3Int frontCell;
        [SerializeField, Min(0)] private int storey;

        private void OnEnable()   => Apply();
        private void OnValidate() => Apply();

        /// <summary>Atualiza a chave de profundidade e reaplica a ordenação (uso pelo runtime de mobília).</summary>
        public void Configure(Vector3Int newFrontCell, int newStorey)
        {
            frontCell = newFrontCell;
            storey    = newStorey;
            Apply();
        }

        private void Apply()
        {
            int order = IsoGrid.SortingOrderForCell(frontCell, storey);
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
                renderer.sortingOrder = order;
        }
    }
}
