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

        [Header("Vendas — unidades-base por tamanho (nota 10, mercado neutro)")]
        [SerializeField] private long smallBaseUnits = 20_000;
        [SerializeField] private long mediumBaseUnits = 90_000;
        [SerializeField] private long largeBaseUnits = 300_000;
        [SerializeField] private long aaaBaseUnits = 1_200_000;

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

        [Header("Produção por pontos (GDD 5.3)")]
        [Tooltip("Pontos-base que cada pessoa contribui por fase de produção (GDD 5.3).")]
        [SerializeField] private int pointsPerPersonPerPhase = 100;
        [Tooltip("Número de fases de produção (GDD 5.2): Pré-produção, Arte, Áudio, Tecnologia, Conteúdo.")]
        [SerializeField] private int productionPhaseCount = 5;

        [Header("Teto de pontos por parâmetro por tamanho (GDD 5.3)")]
        [SerializeField] private int smallParamCap = 300;
        [SerializeField] private int mediumParamCap = 450;
        [SerializeField] private int largeParamCap = 550;
        [SerializeField] private int aaaParamCap = 600;

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

        [Header("Bugs (GDD 5.5)")]
        [Tooltip("Bugs-base gerados por semana com zero foco em Tech e equipe mediana.")]
        [SerializeField] private float bugBasePerWeek = 2.5f;
        [Tooltip("Bugs abaixo deste total não penalizam a nota.")]
        [SerializeField] private int bugToleranceFree = 5;
        [Tooltip("Penalidade máxima de nota (0..10) por excesso de bugs no lançamento.")]
        [SerializeField] private float bugScorePenaltyMax = 3f;

        [Header("Hype → Vendas (GDD 19.6)")]
        [Tooltip("Wishlists-base por tamanho a nota 10; escalam com a nota e convertem via GDD 19.6.")]
        [SerializeField] private long smallBaseWishlists = 4_000;
        [SerializeField] private long mediumBaseWishlists = 18_000;
        [SerializeField] private long largeBaseWishlists = 60_000;
        [SerializeField] private long aaaBaseWishlists = 220_000;

        [Header("Lançamento e cauda de vendas (adendo 23.3 / 23.4)")]
        [Tooltip("Semanas em que um jogo continua vendendo depois do lançamento.")]
        [SerializeField] private int salesTailWeeks = 26;
        [Tooltip("Retenção semanal das vendas com nota 0 (cai rápido).")]
        [Range(0.1f, 0.95f)] [SerializeField] private float tailRetentionWorst = 0.55f;
        [Tooltip("Retenção semanal das vendas com nota 10 (cauda longa).")]
        [Range(0.1f, 0.98f)] [SerializeField] private float tailRetentionBest = 0.88f;
        [Tooltip("Elasticidade de preço: unidades x (preçoRef/preço)^elasticidade.")]
        [SerializeField] private float priceElasticity = 1.3f;
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

        public int SalesTailWeeks => salesTailWeeks;
        public float TailRetentionWorst => tailRetentionWorst;
        public float TailRetentionBest => tailRetentionBest;
        public float PriceElasticity => priceElasticity;
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

        public int PointsPerPersonPerPhase => pointsPerPersonPerPhase;
        public int ProductionPhaseCount => productionPhaseCount;
        public float ScopeCoverageFloor => scopeCoverageFloor;

        public float BugBasePerWeek => bugBasePerWeek;
        public int BugToleranceFree => bugToleranceFree;
        public float BugScorePenaltyMax => bugScorePenaltyMax;

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

        /// <summary>Unidades-base (vendas a nota 10) para o tamanho dado.</summary>
        public long BaseUnitsFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallBaseUnits,
            ProjectSize.Medium => mediumBaseUnits,
            ProjectSize.Large => largeBaseUnits,
            ProjectSize.AAA => aaaBaseUnits,
            _ => mediumBaseUnits
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

        /// <summary>Teto de pontos por parâmetro para o tamanho dado (GDD 5.3).</summary>
        public int ParamCapFor(ProjectSize size) => size switch
        {
            ProjectSize.Small => smallParamCap,
            ProjectSize.Medium => mediumParamCap,
            ProjectSize.Large => largeParamCap,
            ProjectSize.AAA => aaaParamCap,
            _ => mediumParamCap
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
