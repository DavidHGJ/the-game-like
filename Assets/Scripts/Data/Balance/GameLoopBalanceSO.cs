using GameLike.Core.Events;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Valores de balanceamento do loop core (criação → produção → lançamento →
    /// vendas). <b>PROVISÓRIO — vertical slice:</b> os números aqui são palpites
    /// iniciais para o loop ser jogável; devem ser conferidos contra o GDD e
    /// recalibrados em playtesting. Isolados neste SO justamente para serem fáceis
    /// de ajustar no Inspector sem recompilar.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Game Loop Balance", fileName = "GameLoopBalance")]
    public sealed class GameLoopBalanceSO : BalanceAsset
    {
        [Header("Início")]
        [SerializeField] private long startingCash = 50_000;

        [Header("Duração de desenvolvimento por tamanho (semanas)")]
        [SerializeField] private int smallWeeks = 8;
        [SerializeField] private int mediumWeeks = 14;
        [SerializeField] private int largeWeeks = 22;
        [SerializeField] private int aaaWeeks = 34;

        [Header("Tamanho da equipe por tamanho de projeto")]
        [SerializeField] private int smallTeam = 1;
        [SerializeField] private int mediumTeam = 2;
        [SerializeField] private int largeTeam = 4;
        [SerializeField] private int aaaTeam = 8;

        [Header("Vendas — 1ª semana por tamanho (nota 10, hype 50, preço na média) (GDD 23.19)")]
        [SerializeField] private long smallFirstWeekUnits = 5_000;
        [SerializeField] private long mediumFirstWeekUnits = 22_000;
        [SerializeField] private long largeFirstWeekUnits = 28_000;
        [SerializeField] private long aaaFirstWeekUnits = 190_000;

        [Header("Vendas — preço por unidade por tamanho")]
        [SerializeField] private int smallPrice = 15;
        [SerializeField] private int mediumPrice = 20;
        [SerializeField] private int largePrice = 40;
        [SerializeField] private int aaaPrice = 60;

        [Header("Curva nota→vendas: fração de unidades-base = (nota/10)^expoente")]
        [SerializeField] private float salesScoreExponent = 2.4f;
        [Tooltip("Piso de vendas (GDD 23.14): fração das unidades-base que todo jogo lançado vende, mesmo com nota baixa. 0 = sem piso.")]
        [Range(0f, 1f)] [SerializeField] private float salesFloorShare;
        [Tooltip("Nota de virada (GDD 23.14): abaixo dela o jogo vende só o piso; acima, as vendas crescem pela curva. 0 = curva desde a nota 0.")]
        [Range(0f, 9f)] [SerializeField] private float salesScoreThreshold;

        [Header("Produção por aptidão (GDD 23.16)")]
        [Tooltip("Pontos que uma pessoa com aptidão 5 gera por semana de produção (GDD 23.16).")]
        [SerializeField] private float basePointsPerWeek = 3f;
        [Tooltip("Aptidão ganha por ponto produzido na área (prática); diminui perto de 10 (GDD 23.16).")]
        [SerializeField] private float practicePerPoint = 0.02f;
        [Tooltip("Aptidão ganha por um treinamento (GDD 23.16).")]
        [SerializeField] private float trainingGain = 0.5f;
        [Tooltip("Preço do treinamento por ponto de aptidão atual: preço = valor × aptidão (GDD 23.16).")]
        [SerializeField] private long trainingCostPerLevel = 2000;
        [Header("Nota do jogo: quão perto do perfeito (GDD 23.18)")]
        [Tooltip("Parte da nota que vem de gênero × tema (0,5 = metade); o resto vem das fases (GDD 23.18).")]
        [Range(0f, 1f)] [SerializeField] private float themeShare = 0.5f;
        [Tooltip("Acerto do tema por compatibilidade, de −−− a +++ (GDD 23.18).")]
        [SerializeField] private float[] themeAccuracyByWeight = { 0f, 0.15f, 0.35f, 0.5f, 0.65f, 0.85f, 1f };
        [Tooltip("Contraste do perfil do gênero: cada nível de peso (−−− a +++) multiplica o % ideal do bloco por este fator (2 = dobra) (GDD 23.18).")]
        [SerializeField] private float genreWeightContrast = 2f;
        [Tooltip("Tolerância do bloco, relativa ao % ideal dele (0,1 = errar até 10% do ideal não conta) (GDD 23.18).")]
        [Range(0f, 0.5f)] [SerializeField] private float blockToleranceRelative = 0.1f;
        [Tooltip("Bônus do bloco por pontos = este fator × fração do ideal de pontos atingida (0,5 = metade) (GDD 23.18).")]
        [Range(0f, 1f)] [SerializeField] private float pointsBonusFactor = 0.5f;
        [Tooltip("Quanto do que falta no bloco cada nível de módulo de engine ligado a ele ameniza (leve 1, médio 2, pesado 3) (GDD 23.18).")]
        [Range(0f, 0.5f)] [SerializeField] private float engineReliefPerLevel = 0.10f;
        [Tooltip("Máximo que a engine ameniza do que falta num bloco (GDD 23.18).")]
        [Range(0f, 1f)] [SerializeField] private float engineReliefMax = 0.40f;
        [Tooltip("Número de fases de produção (GDD 5.2): Pré-produção, Arte, Áudio, Tecnologia, Conteúdo.")]
        [SerializeField] private int productionPhaseCount = 5;

        [Header("Equipe de referência que 'cobre' cada escopo (cobertura = equipe/referência)")]
        [SerializeField] private int smallReferenceTeam = 1;
        [SerializeField] private int mediumReferenceTeam = 3;
        [SerializeField] private int largeReferenceTeam = 6;
        [SerializeField] private int aaaReferenceTeam = 9;

        [Header("Equipe máxima (staff) por fase da empresa (GDD 7.6)")]
        [SerializeField] private int garageMaxTeam = 1;
        [SerializeField] private int indieMaxTeam = 3;   // 2 + fundador
        [SerializeField] private int officeMaxTeam = 6;  // 5 + fundador
        [SerializeField] private int studioMaxTeam = 9;  // 8 + fundador

        [Tooltip("Piso da cobertura de escopo — mesmo muito sub-dimensionado, a entrega não zera.")]
        [Range(0f, 1f)] [SerializeField] private float scopeCoverageFloor = 0.15f;

        [Header("Bugs (GDD 5.5 / 23.18)")]
        [Tooltip("Bugs gerados por ponto produzido, com equipe mediana e sem foco em Tecnologia (GDD 23.18).")]
        [SerializeField] private float bugsPerPoint = 0.1f;
        [Tooltip("Desconto na nota final = proporção bugs ÷ pontos × este peso (GDD 23.18).")]
        [SerializeField] private float bugPenaltyWeight = 1f;
        [Tooltip("Desconto máximo da nota final por bugs (0,5 = até metade) (GDD 23.18).")]
        [Range(0f, 1f)] [SerializeField] private float bugPenaltyMax = 0.5f;

        [Header("Hype → Vendas (GDD 19.6)")]
        [Tooltip("Wishlists-base por tamanho a nota 10; escalam com a nota e convertem via GDD 19.6.")]
        [SerializeField] private long smallBaseWishlists = 4_000;
        [SerializeField] private long mediumBaseWishlists = 18_000;
        [SerializeField] private long largeBaseWishlists = 60_000;
        [SerializeField] private long aaaBaseWishlists = 220_000;

        [Header("Vendas semana a semana (GDD 23.19)")]
        [Tooltip("Queda das vendas de uma semana para a outra sem hype (0,15 = vende 15% a menos por semana).")]
        [Range(0.01f, 0.9f)] [SerializeField] private float weeklyDecayBase = 0.15f;
        [Tooltip("Quanto o hype máximo reduz a queda semanal (0,6 = com hype 100 a queda cai 60%).")]
        [Range(0f, 1f)] [SerializeField] private float hypeDecayReductionMax = 0.6f;
        [Tooltip("As vendas acabam quando a semana fica abaixo desta fração da 1ª semana.")]
        [Range(0.001f, 0.5f)] [SerializeField] private float salesEndShare = 0.02f;
        [Tooltip("Variação aleatória da semana: até quanto ela pode vender a MENOS que a queda normal (0,25 = −25%).")]
        [Range(0f, 0.9f)] [SerializeField] private float weeklyVariationDown = 0.25f;
        [Tooltip("Variação aleatória da semana: até quanto ela pode vender a MAIS que a queda normal (0,15 = +15%).")]
        [Range(0f, 0.9f)] [SerializeField] private float weeklyVariationUp = 0.15f;
        [Tooltip("Bugs aceleram a queda: soma este peso × (bugs ÷ pontos) à queda semanal.")]
        [SerializeField] private float bugDecayWeight = 0.3f;
        [Header("Estouro: jogo de 9 para cima (GDD 23.19)")]
        [Tooltip("Multiplicador das vendas de um jogo com nota final a partir de 9 (no jogo Pequeno; os maiores usam a escala do tamanho).")]
        [SerializeField] private float hitMultiplier9 = 5f;
        [Tooltip("Multiplicador das vendas de um jogo com nota final a partir de 9,5.")]
        [SerializeField] private float hitMultiplier95 = 10f;
        [Tooltip("Multiplicador das vendas de um jogo nota 10.")]
        [SerializeField] private float hitMultiplier10 = 15f;
        [Tooltip("Quanto do estouro vale em cada tamanho (1 = inteiro). O Pequeno estoura cheio; jogos maiores já vendem muito e estouram menos.")]
        [SerializeField] private float hitScaleSmall = 1f;
        [SerializeField] private float hitScaleMedium = 0.4f;
        [SerializeField] private float hitScaleLarge = 0.2f;
        [SerializeField] private float hitScaleAaa = 0.1f;

        [Tooltip("Efeito rebote: quanto o hype ACELERA a queda de um jogo abaixo da nota de virada (hype × quão ruim × este peso).")]
        [SerializeField] private float hypeBacklashWeight = 1.5f;
        [Tooltip("Reputação perdida quando hype alto encontra nota ruim (0,1 = até 10 pontos com hype 100 e nota 0).")]
        [Range(0f, 1f)] [SerializeField] private float hypeBacklashReputationMax = 0.1f;
        [Tooltip("Fator de vendas da 1ª semana com hype 0.")]
        [SerializeField] private float hypeSalesFactorMin = 0.5f;
        [Tooltip("Fator de vendas da 1ª semana com hype 100.")]
        [SerializeField] private float hypeSalesFactorMax = 1.5f;
        [Tooltip("Preço até esta fração do preço de referência conta como barato.")]
        [SerializeField] private float cheapPriceRatio = 0.8f;
        [Tooltip("Preço a partir desta fração do preço de referência conta como caro.")]
        [SerializeField] private float expensivePriceRatio = 1.2f;
        [Tooltip("Multiplicador das vendas da 1ª semana com preço barato.")]
        [SerializeField] private float cheapPriceFactor = 1.25f;
        [Tooltip("Multiplicador das vendas da 1ª semana com preço caro.")]
        [SerializeField] private float expensivePriceFactor = 0.7f;
        [Tooltip("Hype extra no lançamento quando o tema está em alta no mercado (0,2 = até +20 de hype).")]
        [Range(0f, 1f)] [SerializeField] private float trendingThemeHypeBonus = 0.2f;

        [Header("Lançamento (adendo 23.3)")]
        [Tooltip("Hype ganho pelos primeiros 10 mil de budget de lançamento (retorno decrescente).")]
        [SerializeField] private float launchBudgetHypePer10k = 0.12f;
        [Tooltip("Variação máxima (em pontos) da nota de cada casa de crítica em torno da nota calculada.")]
        [SerializeField] private float reviewerNoise = 1f;

        [Header("Pronto para lançar / correção de bugs (adendo 23.3)")]
        [SerializeField] private int bugFixPerWeekBase = 3;
        [SerializeField] private int bugFixPerWeekPerPerson = 2;
        [Tooltip("Hype perdido por semana com o jogo pronto e não lançado.")]
        [Range(0f, 0.2f)] [SerializeField] private float waitingHypeLossPerWeek = 0.03f;

        [Header("Crunch (GDD 5.6)")]
        [Tooltip("Multiplicador da duração da fase em crunch (menor = mais rápido).")]
        [Range(0.4f, 1f)] [SerializeField] private float crunchDurationMultiplier = 0.75f;
        [SerializeField] private float crunchBugMultiplier = 1.6f;
        [SerializeField] private float crunchBurnoutPerWeek = 0.05f;

        public float WeeklyDecayBase => weeklyDecayBase;
        public float HypeDecayReductionMax => hypeDecayReductionMax;
        public float SalesEndShare => salesEndShare;
        public float WeeklyVariationDown => weeklyVariationDown;
        public float WeeklyVariationUp => weeklyVariationUp;
        public float BugDecayWeight => Mathf.Max(0f, bugDecayWeight);
        public float HypeSalesFactorMin => hypeSalesFactorMin;
        public float HypeSalesFactorMax => hypeSalesFactorMax;
        public float CheapPriceRatio => cheapPriceRatio;
        public float ExpensivePriceRatio => expensivePriceRatio;
        public float CheapPriceFactor => cheapPriceFactor;
        public float ExpensivePriceFactor => expensivePriceFactor;
        public float TrendingThemeHypeBonus => trendingThemeHypeBonus;
        public float HypeBacklashWeight => Mathf.Max(0f, hypeBacklashWeight);

        /// <summary>Multiplicador de estouro das vendas pela nota final (GDD 23.19): 1 abaixo de 9.</summary>
        public float HitMultiplier(float score) =>
            score >= 9.995f ? Mathf.Max(1f, hitMultiplier10) : score >= 9.5f ? Mathf.Max(1f, hitMultiplier95) : score >= 9f ? Mathf.Max(1f, hitMultiplier9) : 1f;

        /// <summary>Estouro pela nota e pelo tamanho: 1 + (multiplicador da faixa − 1) × escala do tamanho (GDD 23.19).</summary>
        public float HitMultiplier(float score, ProjectSize size)
        {
            float scale = size switch
            {
                ProjectSize.Small => hitScaleSmall,
                ProjectSize.Medium => hitScaleMedium,
                ProjectSize.Large => hitScaleLarge,
                ProjectSize.AAA => hitScaleAaa,
                _ => hitScaleMedium
            };
            return 1f + (HitMultiplier(score) - 1f) * Mathf.Max(0f, scale);
        }
        public float HypeBacklashReputationMax => Mathf.Clamp01(hypeBacklashReputationMax);
        public float LaunchBudgetHypePer10k => launchBudgetHypePer10k;
        public float ReviewerNoise => reviewerNoise;
        public int BugFixPerWeekBase => bugFixPerWeekBase;
        public int BugFixPerWeekPerPerson => bugFixPerWeekPerPerson;
        public float WaitingHypeLossPerWeek => waitingHypeLossPerWeek;
        public float CrunchDurationMultiplier => crunchDurationMultiplier;
        public float CrunchBugMultiplier => crunchBugMultiplier;
        public float CrunchBurnoutPerWeek => crunchBurnoutPerWeek;

        public long StartingCash => startingCash;
        public float SalesScoreExponent => salesScoreExponent;
        public float SalesFloorShare => salesFloorShare;
        public float SalesScoreThreshold => salesScoreThreshold;

        /// <summary>
        /// Fração das unidades-base que um jogo com esta nota vende (GDD 23.14): piso + curva de
        /// potência a partir da nota de virada. Com piso 0 e virada 0 é (nota/10)^expoente.
        /// </summary>
        public float SalesShareForScore(float score)
        {
            float threshold = Mathf.Clamp(salesScoreThreshold, 0f, 9.5f);
            float above = Mathf.Clamp01((score - threshold) / (10f - threshold));
            float floor = Mathf.Clamp01(salesFloorShare);
            return floor + (1f - floor) * Mathf.Pow(above, salesScoreExponent);
        }

        public float BasePointsPerWeek => Mathf.Max(0.1f, basePointsPerWeek);
        public float PracticePerPoint => Mathf.Max(0f, practicePerPoint);
        public float TrainingGain => Mathf.Max(0f, trainingGain);
        public long TrainingCostPerLevel => System.Math.Max(0L, trainingCostPerLevel);
        public float ThemeShare => Mathf.Clamp01(themeShare);
        public float BlockToleranceRelative => Mathf.Clamp(blockToleranceRelative, 0f, 0.5f);
        public float GenreWeightContrast => Mathf.Max(1f, genreWeightContrast);
        public float PointsBonusFactor => Mathf.Clamp01(pointsBonusFactor);
        public float EngineReliefPerLevel => Mathf.Clamp(engineReliefPerLevel, 0f, 0.5f);
        public float EngineReliefMax => Mathf.Clamp01(engineReliefMax);

        /// <summary>Acerto do tema (0..1) pela compatibilidade gênero × tema, de −−− (−3) a +++ (+3) (GDD 23.18).</summary>
        public float ThemeAccuracy(int weight)
        {
            if (themeAccuracyByWeight == null || themeAccuracyByWeight.Length < 7) return Mathf.Clamp01(0.5f + weight / 6f);
            return Mathf.Clamp01(themeAccuracyByWeight[Mathf.Clamp(weight + 3, 0, 6)]);
        }
        public int ProductionPhaseCount => productionPhaseCount;
        public float ScopeCoverageFloor => scopeCoverageFloor;

        public float BugsPerPoint => Mathf.Max(0f, bugsPerPoint);
        public float BugPenaltyWeight => Mathf.Max(0f, bugPenaltyWeight);
        public float BugPenaltyMax => Mathf.Clamp01(bugPenaltyMax);

        /// <summary>Semanas de desenvolvimento alvo para o tamanho dado.</summary>
        public int WeeksFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallWeeks,
            ProjectSize.Medium => mediumWeeks,
            ProjectSize.Large => largeWeeks,
            ProjectSize.AAA => aaaWeeks,
            _ => mediumWeeks
        };

        /// <summary>Tamanho de equipe para o tamanho de projeto dado.</summary>
        public int TeamFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallTeam,
            ProjectSize.Medium => mediumTeam,
            ProjectSize.Large => largeTeam,
            ProjectSize.AAA => aaaTeam,
            _ => mediumTeam
        };

        /// <summary>Unidades vendidas na 1ª semana com nota 10, hype 50 e preço na média, pelo tamanho (GDD 23.19).</summary>
        public long FirstWeekUnitsFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallFirstWeekUnits,
            ProjectSize.Medium => mediumFirstWeekUnits,
            ProjectSize.Large => largeFirstWeekUnits,
            ProjectSize.AAA => aaaFirstWeekUnits,
            _ => mediumFirstWeekUnits
        };

        /// <summary>Preço por unidade para o tamanho dado.</summary>
        public int PriceFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallPrice,
            ProjectSize.Medium => mediumPrice,
            ProjectSize.Large => largePrice,
            ProjectSize.AAA => aaaPrice,
            _ => mediumPrice
        };

        /// <summary>Equipe de referência que cobre plenamente o escopo dado.</summary>
        public int ReferenceTeamFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallReferenceTeam,
            ProjectSize.Medium => mediumReferenceTeam,
            ProjectSize.Large => largeReferenceTeam,
            ProjectSize.AAA => aaaReferenceTeam,
            _ => mediumReferenceTeam
        };

        /// <summary>Equipe máxima (staff, incluindo fundador) permitida pela fase (GDD 7.6).</summary>
        public int MaxTeamForPhase(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Garage => garageMaxTeam,
            CompanyPhase.Indie => indieMaxTeam,
            CompanyPhase.Office => officeMaxTeam,
            CompanyPhase.Studio => studioMaxTeam,
            _ => garageMaxTeam
        };

        /// <summary>Wishlists-base (a nota 10) para o tamanho dado.</summary>
        public long BaseWishlistsFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallBaseWishlists,
            ProjectSize.Medium => mediumBaseWishlists,
            ProjectSize.Large => largeBaseWishlists,
            ProjectSize.AAA => aaaBaseWishlists,
            _ => mediumBaseWishlists
        };
    }
}
