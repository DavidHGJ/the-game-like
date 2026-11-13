using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Grupo lógico de mobília (ex.: "Desks") — LEVEL_BUILDER.md §10.2.
    /// É <b>object-based</b>: cada <c>FurnitureInstance</c> referencia um grupo
    /// (default = a <see cref="FurnitureCategory"/> da definição), e o sistema
    /// de upgrades usa esse grupo como handle para encontrar e substituir todas
    /// as peças do grupo quando o jogador compra um upgrade ("estas peças são
    /// as mesas → trocar quando o upgrade de mesas for comprado").
    ///
    /// O Level Builder apenas <b>produz</b> essa marcação; a troca em si é do
    /// sistema de upgrades (fora do escopo do builder, §12). Distinto de
    /// <see cref="ZoneDefinitionSO"/> (cell-based, semântica de área).
    /// Referenciado por <see cref="DefinitionAsset.Id"/> estável.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Level Builder/Furniture Group", fileName = "NewFurnitureGroup")]
    public sealed class FurnitureGroupSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;

        public LocalizedString DisplayName => displayName;
    }
}
