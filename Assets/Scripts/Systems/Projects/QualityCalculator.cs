using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Calcula a nota de qualidade (0..10) de um projeto no loop core.
    ///
    /// Implementa a fórmula do GDD seção 19.7 como soma ponderada de 6 componentes,
    /// cada um normalizado em 0..10:
    ///   1. Perfil de sliders vs. perfil ideal do gênero — peso 40%
    ///   2. Compatibilidade Tema × Gênero — peso 20%
    ///   3. Nível médio da equipe — peso 15%
    ///   4. Engine Budget utilizado vs. teto — peso 10%
    ///   5. Tendência de mercado do tema no lançamento — peso 10%
    ///   6. Decisões nos eventos de desenvolvimento — peso 5%
    /// Nota final = Σ (peso_i × componente_i).
    ///
    /// <b>Vertical slice:</b> têm sistema os componentes 1 (perfil de sliders),
    /// 3 (equipe) e 4 (Engine Budget, aproximado pela cobertura de escopo da equipe
    /// vs. o escopo — GDD 5.3). Os componentes 2 (Tema×Gênero), 5 (tendência) e 6
    /// (eventos) retornam um valor neutro configurável (<see cref="QualityFormulaBalanceSO"/>)
    /// até que Temas, Mercado e Eventos existam — é a "fórmula inicial, pesos a
    /// calibrar" do GDD. Os PESOS e os valores neutros vêm de <see cref="QualityFormulaBalanceSO"/>.
    /// </summary>
    public static class QualityCalculator
    {
        /// <summary>
        /// Perfil ideal do gênero como distribuição sobre as 5 <see cref="DevArea"/>
        /// (soma 1), derivado dos pesos dos 19 parâmetros do gênero para o tamanho dado.
        /// </summary>
        public static float[] GenreIdealProfile(GenreSO genre, ProjectSize size)
        {
            var raw = new float[ProjectState.AreaCount];

            raw[(int)DevArea.Design] = Avg(
                genre.GameDesign.ForSize(size),
                genre.Scope.ForSize(size),
                genre.Gameplay.ForSize(size),
                genre.Balancing.ForSize(size));

            raw[(int)DevArea.Art] = Avg(
                genre.ArtDirection.ForSize(size),
                genre.Graphics.ForSize(size),
                genre.Animation.ForSize(size),
                genre.UserInterface.ForSize(size),
                genre.Vfx.ForSize(size));

            raw[(int)DevArea.Audio] = Avg(
                genre.Soundtrack.ForSize(size),
                genre.SoundDesign.ForSize(size),
                genre.VoiceActing.ForSize(size));

            raw[(int)DevArea.Tech] = Avg(
                genre.GameFeel.ForSize(size),
                genre.Programming.ForSize(size),
                genre.PerformanceOptimization.ForSize(size),
                genre.StabilityTesting.ForSize(size));

            raw[(int)DevArea.Content] = Avg(
                genre.Narrative.ForSize(size),
                genre.Content.ForSize(size),
                genre.ImplementedNarrative.ForSize(size));

            // Mapeia média de peso (-3..+3) para um valor não-negativo (0..6) e normaliza.
            float sum = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Mathf.Max(0f, raw[i] + 3f);
                sum += raw[i];
            }

            if (sum <= 0.0001f)
            {
                for (int i = 0; i < raw.Length; i++) raw[i] = 1f / raw.Length;
                return raw;
            }

            for (int i = 0; i < raw.Length; i++) raw[i] /= sum;
            return raw;
        }

        /// <summary>
        /// Aderência (0..1) da alocação do jogador ao perfil ideal do gênero.
        /// Determinística (sem ruído) — usada tanto na nota quanto como dica na UI.
        /// </summary>
        public static float AllocationMatch(GenreSO genre, ProjectSize size, float[] allocation)
        {
            var ideal = GenreIdealProfile(genre, size);
            var alloc = Normalize(allocation);

            float l1 = 0f;
            for (int i = 0; i < ProjectState.AreaCount; i++)
                l1 += Mathf.Abs(ideal[i] - alloc[i]);
            return Mathf.Clamp01(1f - 0.5f * l1);
        }

        /// <summary>Converte um peso de compatibilidade (-3..+3) num score 0..10 (neutro ≈ 5.5).</summary>
        public static float WeightToScore(WeightLevel weight) => Mathf.Clamp(5.5f + (int)weight * 1.5f, 0f, 10f);

        /// <summary>
        /// Nota final 0..10. <paramref name="allocation"/> é a distribuição de foco
        /// do jogador (soma ≈ 1); <paramref name="teamQuality01"/> é a qualidade da
        /// equipe aproximada (0..1). <paramref name="theme"/>/<paramref name="genreKey"/>
        /// alimentam o componente 2 (Tema×Gênero); <paramref name="marketTrend01"/>
        /// (0..1, 0.5 = neutro) alimenta o componente 5. Nulos usam o valor neutro.
        /// </summary>
        public static float Compute(GenreSO genre, ProjectSize size, float[] allocation,
            float teamQuality01, float scopeCoverage01 = 1f,
            ThemeSO theme = null, string genreKey = null, float marketTrend01 = 0.5f)
        {
            var qf = BalanceProvider.Get<QualityFormulaBalanceSO>();

            // Componente 1 (40%): aderência da alocação ao perfil ideal do gênero → 0..10.
            float sliderProfileScore = AllocationMatch(genre, size, allocation) * 10f;
            // Componente 3 (15%): nível médio da equipe → 0..10.
            float teamLevelScore = Mathf.Clamp01(teamQuality01) * 10f;
            // Componente 4 (10%): "Engine Budget vs. teto" — no slice, proxy = cobertura de
            // escopo (capacidade de entrega da equipe vs. escopo, GDD 5.3). Substituir pelo
            // Engine Budget real (GDD 5.4/6.2) quando existir.
            float engineBudgetScore = Mathf.Clamp01(scopeCoverage01) * 10f;

            // Componente 2 (20%): compatibilidade Tema×Gênero (GDD 18.7). Neutro se sem tema.
            float themeGenreScore = theme != null
                ? WeightToScore(theme.CompatibilityFor(genreKey))
                : (qf != null ? qf.NeutralThemeGenreScore : 6f);
            // Componente 5 (10%): tendência de mercado do tema no lançamento (0..1 → 0..10).
            float marketTrendScore = Mathf.Clamp01(marketTrend01) * 10f;
            // Componente 6 (5%): decisões em eventos de dev — neutro até Eventos existirem.
            float devEventsScore = qf != null ? qf.NeutralDevelopmentEventsScore : 6f;

            float wSlider = qf != null ? qf.SliderProfileWeight : 0.40f;
            float wTheme = qf != null ? qf.ThemeGenreCompatibilityWeight : 0.20f;
            float wTeam = qf != null ? qf.TeamLevelWeight : 0.15f;
            float wEngine = qf != null ? qf.EngineBudgetUsageWeight : 0.10f;
            float wMarket = qf != null ? qf.MarketTrendWeight : 0.10f;
            float wEvents = qf != null ? qf.DevelopmentEventsWeight : 0.05f;

            float weightedTen = wSlider * sliderProfileScore
                                + wTheme * themeGenreScore
                                + wTeam * teamLevelScore
                                + wEngine * engineBudgetScore
                                + wMarket * marketTrendScore
                                + wEvents * devEventsScore;

            // Normaliza pela soma real dos pesos (robusto se os pesos não somarem 1.0
            // durante a calibração) e aplica um pequeno ruído de lançamento.
            float totalWeight = wSlider + wTheme + wTeam + wEngine + wMarket + wEvents;
            float score = totalWeight > 0.0001f ? weightedTen / totalWeight : 0f;
            score += Random.Range(-0.2f, 0.2f);
            return Mathf.Clamp(score, 0f, 10f);
        }

        // ------------------------------------------------------------------
        // Produção em fases (adendo 23.2): os 19 parâmetros, fase a fase.
        // ------------------------------------------------------------------

        /// <summary>Área de alto nível (<see cref="DevArea"/>) a que um parâmetro pertence.</summary>
        public static int AreaIndexOf(GameParameter parameter) => parameter switch
        {
            GameParameter.GameDesign or GameParameter.Scope or GameParameter.Gameplay or GameParameter.Balancing
                => (int)DevArea.Design,
            GameParameter.ArtDirection or GameParameter.Graphics or GameParameter.Animation
                or GameParameter.UserInterface or GameParameter.Vfx => (int)DevArea.Art,
            GameParameter.Soundtrack or GameParameter.SoundDesign or GameParameter.VoiceActing => (int)DevArea.Audio,
            GameParameter.GameFeel or GameParameter.Programming or GameParameter.PerformanceOptimization
                or GameParameter.StabilityTesting => (int)DevArea.Tech,
            _ => (int)DevArea.Content
        };

        /// <summary>
        /// Distribuição ideal (soma 1) dos pontos de uma fase entre os parâmetros disponíveis
        /// para o escopo, derivada dos pesos do gênero (GDD 18.5). A ordem segue
        /// <see cref="ParameterCatalog.AvailableFor"/>.
        /// </summary>
        public static float[] IdealPhaseShares(GenreSO genre, ProjectSize size, ProductionPhase phase)
        {
            var parameters = ParameterCatalog.AvailableFor(phase, size);
            var shares = new float[parameters.Count];
            float sum = 0f;
            for (int i = 0; i < parameters.Count; i++)
            {
                int w = genre != null ? (int)genre.WeightFor(parameters[i], size) : 0;
                shares[i] = Mathf.Max(0.25f, w + 3f);
                sum += shares[i];
            }
            for (int i = 0; i < shares.Length; i++) shares[i] = sum > 0f ? shares[i] / sum : 1f / shares.Length;
            return shares;
        }

        /// <summary>Aderência (0..1) dos pontos entregues em UMA fase ao ideal do gênero.</summary>
        public static float PhaseMatch(GenreSO genre, ProjectSize size, ProductionPhase phase, float[] paramPoints)
        {
            var parameters = ParameterCatalog.AvailableFor(phase, size);
            if (parameters.Count == 0) return 1f;
            var ideal = IdealPhaseShares(genre, size, phase);
            float total = 0f;
            for (int i = 0; i < parameters.Count; i++) total += Mathf.Max(0f, paramPoints[(int)parameters[i]]);
            if (total <= 0.0001f) return 0f;
            float l1 = 0f;
            for (int i = 0; i < parameters.Count; i++)
                l1 += Mathf.Abs(ideal[i] - Mathf.Max(0f, paramPoints[(int)parameters[i]]) / total);
            return Mathf.Clamp01(1f - 0.5f * l1);
        }

        /// <summary>Aderência média (0..1) das 5 fases ao perfil ideal do gênero — componente 1 da nota.</summary>
        public static float ParameterMatch(GenreSO genre, ProjectSize size, float[] paramPoints)
        {
            if (paramPoints == null || paramPoints.Length < ParameterCatalog.ParameterCount) return 0f;
            float sum = 0f;
            for (int p = 0; p < ParameterCatalog.PhaseCount; p++)
                sum += PhaseMatch(genre, size, (ProductionPhase)p, paramPoints);
            return sum / ParameterCatalog.PhaseCount;
        }

        /// <summary>Converte os pontos por parâmetro na distribuição por <see cref="DevArea"/> (soma 1).</summary>
        public static float[] AreaSharesFromParameters(float[] paramPoints)
        {
            var areas = new float[ProjectState.AreaCount];
            float sum = 0f;
            if (paramPoints != null)
                for (int i = 0; i < paramPoints.Length && i < ParameterCatalog.ParameterCount; i++)
                {
                    float v = Mathf.Max(0f, paramPoints[i]);
                    areas[AreaIndexOf((GameParameter)i)] += v;
                    sum += v;
                }
            for (int i = 0; i < areas.Length; i++) areas[i] = sum > 0.0001f ? areas[i] / sum : 1f / areas.Length;
            return areas;
        }

        /// <summary>
        /// Nota final (0..10) pela fórmula do GDD 19.7 usando os 19 parâmetros entregues nas
        /// fases, o Engine Budget real (adendo 23.7) e o saldo de decisões em eventos.
        /// </summary>
        public static float ComputeDetailed(GenreSO genre, ProjectSize size, float[] paramPoints,
            float teamQuality01, float scopeCoverage01, float engineScore01,
            ThemeSO theme, string genreKey, float marketTrend01, float devEventsBonus, bool applyNoise = true)
        {
            var qf = BalanceProvider.Get<QualityFormulaBalanceSO>();

            // A aderência vale cheia só se a equipe deu conta do escopo (GDD 5.3).
            float coverage = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(scopeCoverage01));
            float sliderProfileScore = ParameterMatch(genre, size, paramPoints) * 10f * coverage;
            float teamLevelScore = Mathf.Clamp01(teamQuality01) * 10f;
            float engineBudgetScore = Mathf.Clamp01(engineScore01) * 10f;
            float themeGenreScore = theme != null
                ? WeightToScore(theme.CompatibilityFor(genreKey))
                : (qf != null ? qf.NeutralThemeGenreScore : 6f);
            float marketTrendScore = Mathf.Clamp01(marketTrend01) * 10f;
            float neutralEvents = qf != null ? qf.NeutralDevelopmentEventsScore : 6f;
            float devEventsScore = Mathf.Clamp(neutralEvents + devEventsBonus * 4f, 0f, 10f);

            float wSlider = qf != null ? qf.SliderProfileWeight : 0.40f;
            float wTheme = qf != null ? qf.ThemeGenreCompatibilityWeight : 0.20f;
            float wTeam = qf != null ? qf.TeamLevelWeight : 0.15f;
            float wEngine = qf != null ? qf.EngineBudgetUsageWeight : 0.10f;
            float wMarket = qf != null ? qf.MarketTrendWeight : 0.10f;
            float wEvents = qf != null ? qf.DevelopmentEventsWeight : 0.05f;

            float weightedTen = wSlider * sliderProfileScore + wTheme * themeGenreScore + wTeam * teamLevelScore
                                + wEngine * engineBudgetScore + wMarket * marketTrendScore + wEvents * devEventsScore;
            float totalWeight = wSlider + wTheme + wTeam + wEngine + wMarket + wEvents;
            float score = totalWeight > 0.0001f ? weightedTen / totalWeight : 0f;
            if (applyNoise) score += Random.Range(-0.2f, 0.2f);
            return Mathf.Clamp(score, 0f, 10f);
        }

        private static float Avg(params WeightLevel[] levels)
        {
            if (levels == null || levels.Length == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < levels.Length; i++) sum += (int)levels[i];
            return sum / levels.Length;
        }

        private static float[] Normalize(float[] values)
        {
            var result = new float[ProjectState.AreaCount];
            float sum = 0f;
            if (values != null)
                for (int i = 0; i < result.Length && i < values.Length; i++)
                {
                    result[i] = Mathf.Max(0f, values[i]);
                    sum += result[i];
                }

            if (sum <= 0.0001f)
            {
                for (int i = 0; i < result.Length; i++) result[i] = 1f / result.Length;
                return result;
            }

            for (int i = 0; i < result.Length; i++) result[i] /= sum;
            return result;
        }
    }
}
