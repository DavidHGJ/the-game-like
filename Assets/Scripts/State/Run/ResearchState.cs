using System.Collections.Generic;

namespace GameLike.State
{
    /// <summary>
    /// Estado de P&amp;D de UMA run (GDD 6). Guarda os Pontos de Pesquisa (PP)
    /// acumulados, o conjunto de desbloqueios já conquistados e a pesquisa em
    /// andamento. Reseta a cada run — é criado junto com o <see cref="RunState"/>
    /// e nunca carregado entre runs (GDD 6.1: "P&amp;D reseta completamente a cada run").
    /// </summary>
    [System.Serializable]
    public sealed class ResearchState
    {
        /// <summary>Pontos de Pesquisa acumulados nesta run.</summary>
        public int points;

        /// <summary>Ids de nós de pesquisa já desbloqueados (ex: "theme:cyberpunk", "size:medium").</summary>
        public List<string> unlocked = new List<string>();

        /// <summary>Pesquisa em andamento (vazio/null = nenhuma).</summary>
        public string activeId;
        public int activeWeeksRemaining;

        /// <summary>Duração total da pesquisa ativa (para a barra de progresso).</summary>
        public int activeWeeksTotal;
        public long activeWeeklyCost;

        public bool IsUnlocked(string id) =>
            !string.IsNullOrEmpty(id) && unlocked.Contains(id);

        public bool HasActiveResearch => !string.IsNullOrEmpty(activeId);
    }
}
