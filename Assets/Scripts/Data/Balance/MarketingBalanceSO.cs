using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Valores de balanceamento de Marketing (GDD 10) e das campanhas de hype/wishlist.
    /// Hype acumula por ações e por progresso passivo, e decai com o tempo; wishlists
    /// convertem o hype em expectativa que vira vendas no lançamento (GDD 19.6).
    /// Valores iniciais — calibrar em playtesting.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Marketing Balance", fileName = "MarketingBalance")]
    public sealed class MarketingBalanceSO : BalanceAsset
    {
        [Header("Hype passivo e decaimento")]
        [Tooltip("Ganho passivo de hype por semana de desenvolvimento (0..1).")]
        [Range(0f, 0.2f)] [SerializeField] private float passiveHypeGainPerWeek = 0.015f;
        [Tooltip("Decaimento de hype por semana (0..1) — o hype esfria se nada o alimenta.")]
        [Range(0f, 0.2f)] [SerializeField] private float hypeDecayPerWeek = 0.02f;

        [Header("Campanhas — custo em $ e ganho de hype (0..1)")]
        [SerializeField] private long announcementCost = 2_000;
        [SerializeField] private float announcementHype = 0.15f;

        [SerializeField] private long wishlistCampaignCost = 5_000;
        [SerializeField] private float wishlistCampaignHype = 0.10f;
        [Tooltip("Burst de wishlists da campanha de wishlist, como fração do potencial semanal.")]
        [SerializeField] private float wishlistCampaignBurst = 3f;

        [SerializeField] private long demoCost = 8_000;
        [SerializeField] private float demoHype = 0.25f;

        [SerializeField] private long gameShowCost = 20_000;
        [SerializeField] private float gameShowHype = 0.40f;

        [Header("Ações adicionais (GDD 10.2 / adendo 23.6)")]
        [SerializeField] private long devlogCost = 800;
        [SerializeField] private float devlogHype = 0.05f;
        [SerializeField] private long pressKitCost = 1_500;
        [SerializeField] private float pressKitHype = 0.07f;
        [SerializeField] private long closedBetaCost = 3_000;
        [SerializeField] private float closedBetaHype = 0.06f;
        [Tooltip("Fração dos bugs conhecidos eliminada por um beta fechado.")]
        [Range(0f, 1f)] [SerializeField] private float closedBetaBugReduction = 0.3f;
        [SerializeField] private long influencersCost = 12_000;
        [SerializeField] private float influencersHype = 0.30f;

        public long DevlogCost => devlogCost;
        public float DevlogHype => devlogHype;
        public long PressKitCost => pressKitCost;
        public float PressKitHype => pressKitHype;
        public long ClosedBetaCost => closedBetaCost;
        public float ClosedBetaHype => closedBetaHype;
        public float ClosedBetaBugReduction => closedBetaBugReduction;
        public long InfluencersCost => influencersCost;
        public float InfluencersHype => influencersHype;

        public float PassiveHypeGainPerWeek => passiveHypeGainPerWeek;
        public float HypeDecayPerWeek => hypeDecayPerWeek;

        public long AnnouncementCost => announcementCost;
        public float AnnouncementHype => announcementHype;
        public long WishlistCampaignCost => wishlistCampaignCost;
        public float WishlistCampaignHype => wishlistCampaignHype;
        public float WishlistCampaignBurst => wishlistCampaignBurst;
        public long DemoCost => demoCost;
        public float DemoHype => demoHype;
        public long GameShowCost => gameShowCost;
        public float GameShowHype => gameShowHype;
    }
}
