using System;
using System.Collections.Generic;

namespace GameLike.State
{
    /// <summary>
    /// Estado do mercado na run (GDD 8): tendência atual de cada gênero e tema (0..1,
    /// 0.5 = neutro). Tendências flutuam com o tempo e caem por saturação quando o
    /// jogador repete a mesma combinação. Serializável (listas, não dicionário) para
    /// persistir no save. Mutado pelo <c>MarketManager</c>.
    /// </summary>
    [Serializable]
    public sealed class MarketState
    {
        [Serializable]
        public struct TrendEntry
        {
            public string key;
            public float value01;
        }

        public List<TrendEntry> genreTrends = new List<TrendEntry>();
        public List<TrendEntry> themeTrends = new List<TrendEntry>();

        /// <summary>Multiplicador global de vendas (eventos da linha do tempo, GDD 15.2). 1 = normal.</summary>
        public float globalSalesMultiplier = 1f;

        /// <summary>Semanas restantes do evento global ativo; 0 = nenhum.</summary>
        public int globalEventWeeksLeft;

        public float GetGenreTrend(string key) => Get(genreTrends, key);
        public float GetThemeTrend(string key) => Get(themeTrends, key);
        public void SetGenreTrend(string key, float value) => Set(genreTrends, key, value);
        public void SetThemeTrend(string key, float value) => Set(themeTrends, key, value);

        private static float Get(List<TrendEntry> list, string key)
        {
            if (string.IsNullOrEmpty(key)) return 0.5f;
            for (int i = 0; i < list.Count; i++)
                if (list[i].key == key) return list[i].value01;
            return 0.5f; // ainda não observado → neutro
        }

        private static void Set(List<TrendEntry> list, string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            value = UnityEngine.Mathf.Clamp01(value);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].key == key)
                {
                    var e = list[i];
                    e.value01 = value;
                    list[i] = e;
                    return;
                }
            }
            list.Add(new TrendEntry { key = key, value01 = value });
        }
    }
}
