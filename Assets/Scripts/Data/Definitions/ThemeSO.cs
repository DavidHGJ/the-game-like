using System;
using System.Collections.Generic;
using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.Definitions
{
    /// <summary>
    /// Categorias de tema do GDD 18.6 (agrupamento organizacional).
    /// </summary>
    public enum ThemeCategory
    {
        FictionFantasy,
        HorrorSuspense,
        LifestyleDaily,
        Historical,
        SportsVehicles,
        NatureExploration,
        MetaPop
    }

    /// <summary>Peso de compatibilidade de um tema com um gênero específico (GDD 18.7).</summary>
    [Serializable]
    public struct GenreCompatibility
    {
        [Tooltip("Slug estável do gênero (nome do asset, ex: action_rpg).")]
        public string genreKey;
        public WeightLevel weight;
    }

    /// <summary>
    /// Define um tema (Fantasia Medieval, Cyberpunk, Futebol, ...) — GDD 18.6/18.7.
    /// Guarda a categoria e a tabela de compatibilidade Tema×Gênero, que afeta hype,
    /// avaliação e vendas (GDD 5.1). A combinação Gênero+Tema é o maior peso de
    /// compatibilidade do jogo. Preenchido via pipeline JSON → importer.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Theme", fileName = "NewTheme")]
    public sealed class ThemeSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;
        [SerializeField] private ThemeCategory category;

        [Header("Compatibilidade Tema × Gênero (GDD 18.7)")]
        [SerializeField] private List<GenreCompatibility> genreCompatibilities = new List<GenreCompatibility>();

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;
        public ThemeCategory Category => category;
        public IReadOnlyList<GenreCompatibility> GenreCompatibilities => genreCompatibilities;

        /// <summary>
        /// Peso de compatibilidade (-3..+3) deste tema com o gênero dado (por slug).
        /// Retorna <see cref="WeightLevel.Neutral"/> quando não há entrada específica.
        /// </summary>
        public WeightLevel CompatibilityFor(string genreKey)
        {
            if (string.IsNullOrEmpty(genreKey)) return WeightLevel.Neutral;
            for (int i = 0; i < genreCompatibilities.Count; i++)
                if (string.Equals(genreCompatibilities[i].genreKey, genreKey, StringComparison.OrdinalIgnoreCase))
                    return genreCompatibilities[i].weight;
            return WeightLevel.Neutral;
        }
    }
}
