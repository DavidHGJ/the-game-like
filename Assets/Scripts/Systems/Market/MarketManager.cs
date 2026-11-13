using GameLike.Core.Events;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Market
{
    /// <summary>
    /// Mercado orgânico (GDD 8): mantém a tendência de cada gênero e tema oscilando por
    /// um passeio aleatório com reversão à média, e aplica saturação quando o jogador
    /// lança um jogo (lançar sempre a mesma combinação derruba a tendência — GDD 8.1).
    /// A tendência do tema alimenta o componente 5 da fórmula de qualidade e o hype.
    /// </summary>
    public sealed class MarketManager : MonoBehaviour
    {
        [SerializeField] private float driftPerWeek = 0.015f;
        [SerializeField] private float meanReversion = 0.01f;
        [SerializeField] private float genreSaturationHit = 0.12f;
        [SerializeField] private float themeSaturationHit = 0.2f;

        private RunState _run;
        private System.Random _rng;

        private void OnEnable()
        {
            EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
            EventBus.Subscribe<GameLaunchedEvent>(OnGameLaunched);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);
            EventBus.Unsubscribe<GameLaunchedEvent>(OnGameLaunched);
        }

        /// <summary>Liga o manager ao estado da run (feito pelo GameController).</summary>
        public void Bind(RunState run)
        {
            _run = run;
            _rng = new System.Random(run != null ? run.runNumber * 104729 + 3 : 3);
        }

        /// <summary>Tendência atual do tema (0..1, 0.5 neutro).</summary>
        public float ThemeTrend01(string themeKey) => _run?.market?.GetThemeTrend(themeKey) ?? 0.5f;

        /// <summary>Tendência atual do gênero (0..1, 0.5 neutro).</summary>
        public float GenreTrend01(string genreKey) => _run?.market?.GetGenreTrend(genreKey) ?? 0.5f;

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.market == null) return;
            Drift(_run.market.genreTrends);
            Drift(_run.market.themeTrends);
        }

        private void Drift(System.Collections.Generic.List<MarketState.TrendEntry> list)
        {
            if (_rng == null) _rng = new System.Random(3);
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                float noise = ((float)_rng.NextDouble() - 0.5f) * 2f * driftPerWeek;
                float reversion = (0.5f - entry.value01) * meanReversion;
                entry.value01 = Mathf.Clamp01(entry.value01 + noise + reversion);
                list[i] = entry;
            }
        }

        private void OnGameLaunched(GameLaunchedEvent e)
        {
            // Satura a combinação recém-lançada (lê o último projeto, já movido para o histórico).
            if (_run == null || _run.market == null || _run.releasedProjects.Count == 0) return;
            var p = _run.releasedProjects[_run.releasedProjects.Count - 1];

            if (!string.IsNullOrEmpty(p.genreKey))
                _run.market.SetGenreTrend(p.genreKey, _run.market.GetGenreTrend(p.genreKey) - genreSaturationHit);
            if (!string.IsNullOrEmpty(p.themeKey))
                _run.market.SetThemeTrend(p.themeKey, _run.market.GetThemeTrend(p.themeKey) - themeSaturationHit);
        }
    }
}
