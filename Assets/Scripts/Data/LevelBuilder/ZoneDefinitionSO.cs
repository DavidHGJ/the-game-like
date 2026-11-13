using System;
using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Flags de gameplay de uma zona de área (LEVEL_BUILDER.md §10.1).
    /// Combináveis: uma mesma zona pode ser, por exemplo, área de trabalho e
    /// caminhável ao mesmo tempo. Extensível — adicionar flags conforme novos
    /// usos de semântica de área surgirem, sem quebrar zonas existentes.
    /// </summary>
    [Flags]
    public enum ZoneFlags
    {
        None     = 0,
        Walkable = 1 << 0,
        Blocked  = 1 << 1,
        WorkArea = 1 << 2
    }

    /// <summary>
    /// Definição de uma zona de área semântica (cozinha, bloqueado, área de
    /// trabalho — LEVEL_BUILDER.md §10.1). Zonas são <b>cell-based</b> e
    /// <b>não renderizam in-game</b>: são consumidas pela lógica de gameplay
    /// (walkability de funcionários, áreas funcionais) e, no editor, exibidas
    /// como overlay translúcido colorido.
    ///
    /// Distinta de <see cref="FurnitureGroupSO"/> (que é object-based, para
    /// upgrades de mobília). Referenciada por <see cref="DefinitionAsset.Id"/>
    /// estável, nunca por nome de arquivo.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Level Builder/Zone Definition", fileName = "NewZone")]
    public sealed class ZoneDefinitionSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;

        [Header("Editor")]
        [Tooltip("Cor do overlay translúcido desenhado na SceneView para esta zona (não aparece no jogo).")]
        [SerializeField] private Color editorColor = new Color(0.2f, 0.6f, 1f, 0.35f);

        [Header("Semântica de Gameplay")]
        [SerializeField] private ZoneFlags flags = ZoneFlags.None;

        public LocalizedString DisplayName => displayName;
        public Color EditorColor => editorColor;
        public ZoneFlags Flags => flags;

        public bool IsWalkable => (flags & ZoneFlags.Walkable) != 0;
        public bool IsBlocked  => (flags & ZoneFlags.Blocked)  != 0;
        public bool IsWorkArea => (flags & ZoneFlags.WorkArea) != 0;
    }
}
