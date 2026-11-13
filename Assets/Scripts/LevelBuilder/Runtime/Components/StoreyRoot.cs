using GameLike.LevelBuilder.Grid;
using UnityEngine;

namespace GameLike.LevelBuilder.Components
{
    /// <summary>
    /// Root de um andar (storey) de uma planta de escritório
    /// (LEVEL_BUILDER.md §11). Cada andar tem seu próprio root contendo os
    /// Tilemaps de Floor/Wall, o pai "Furniture" e o <c>ZoneLayer</c> daquele
    /// nível. O root é posicionado em world Y += <c>storey ×
    /// IsoGrid.StoreyWorldHeight</c> para o empilhamento visual entre andares.
    ///
    /// O índice de andar entra no modelo de dados desde a Fase 0 (barato); a
    /// UX/render multi-andar é a Fase 5.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class StoreyRoot : MonoBehaviour
    {
        [SerializeField, Min(0)] private int storeyIndex;

        /// <summary>Índice deste andar (0 = térreo).</summary>
        public int StoreyIndex => storeyIndex;

        private void OnEnable()  => ApplyOffset();
        private void OnValidate() => ApplyOffset();

        /// <summary>Reposiciona o root na altura de mundo correspondente ao seu andar.</summary>
        public void ApplyOffset()
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, IsoGrid.StoreyOffset(storeyIndex).y, p.z);
        }

        /// <summary>Define o índice de andar e reaplica o deslocamento (uso pelo editor).</summary>
        public void SetStorey(int index)
        {
            storeyIndex = Mathf.Max(0, index);
            ApplyOffset();
        }
    }
}
