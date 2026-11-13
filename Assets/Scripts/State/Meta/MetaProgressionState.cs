using System;
using System.Collections.Generic;
using GameLike.Core.Events;

namespace GameLike.State.Meta
{
    /// <summary>
    /// Estado que persiste entre runs (GDD 2.2 / 14): dinheiro pessoal, Pontos de Legado,
    /// árvore de habilidades do fundador, identidade/visual do fundador, conquistas e as
    /// empresas de runs anteriores (que viram rivais).
    /// </summary>
    [Serializable]
    public sealed class MetaProgressionState
    {
        public const int SkillCount = 6;

        public long personalMoney;
        public int legacyPoints;
        public int runsCompleted;
        public long bestSale;
        public List<string> unlockedGenres = new List<string>();

        /// <summary>Nível do nó raiz (incremental, $) de cada árvore — índice = skill - 1.</summary>
        public int[] skillLevels = new int[SkillCount];
        /// <summary>Ids dos nós únicos (PL) já comprados.</summary>
        public List<string> perks = new List<string>();
        /// <summary>Total de PL gasto em nós únicos (devolvido no reset, GDD 14.3).</summary>
        public int legacySpent;

        public FounderProfile founder = new FounderProfile();
        public List<string> achievements = new List<string>();
        public List<PastCompany> pastCompanies = new List<PastCompany>();

        /// <summary>Garante coleções válidas após carregar um save antigo.</summary>
        public void EnsureCollections()
        {
            if (unlockedGenres == null) unlockedGenres = new List<string>();
            if (skillLevels == null || skillLevels.Length != SkillCount) skillLevels = new int[SkillCount];
            if (perks == null) perks = new List<string>();
            if (founder == null) founder = new FounderProfile();
            if (achievements == null) achievements = new List<string>();
            if (pastCompanies == null) pastCompanies = new List<PastCompany>();
        }
    }

    /// <summary>Identidade e aparência do fundador (GDD 20.2).</summary>
    [Serializable]
    public sealed class FounderProfile
    {
        public bool created;
        public string name = "Fundador";
        public int skin;
        public int hair;
        public int hairStyle = 1;
        public int shirt;
        public int accessory = 1;
    }

    /// <summary>Empresa de uma run encerrada — reaparece como rival (GDD 1.2 / 2.2).</summary>
    [Serializable]
    public sealed class PastCompany
    {
        public string name;
        public int runNumber;
        public long finalValuation;
        public CompanyPhase phase;
        public bool bankrupt;
    }
}
