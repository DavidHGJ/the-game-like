using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.State;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using GameLike.Systems.Research;
using UnityEngine;

namespace GameLike.Systems.Marketing
{
    /// <summary>Ações de marketing disponíveis durante o desenvolvimento (GDD 10.2).</summary>
    public enum MarketingCampaign
    {
        Announcement = 0,
        WishlistCampaign = 1,
        Demo = 2,
        GameShow = 3,
        Devlog = 4,
        PressKit = 5,
        ClosedBeta = 6,
        Influencers = 7
    }

    /// <summary>Custo e efeito de uma campanha, já resolvidos a partir do balanceamento.</summary>
    public readonly struct CampaignInfo
    {
        public readonly MarketingCampaign Type;
        public readonly string Label;
        public readonly long Cost;
        public readonly float Hype;
        /// <summary>Chave da pesquisa de marketing exigida; nula para as ações básicas (adendo 23.6).</summary>
        public readonly string ResearchKey;
        public readonly string Effect;

        public CampaignInfo(MarketingCampaign type, string label, long cost, float hype, string researchKey, string effect)
        {
            Type = type; Label = label; Cost = cost; Hype = hype; ResearchKey = researchKey; Effect = effect;
        }
    }

    /// <summary>
    /// Marketing do projeto em andamento (GDD 5.5 / 10): o hype decai toda semana e sobe com
    /// campanhas; wishlists acumulam até o lançamento. Cada campanha pode ser feita uma vez
    /// por projeto, e as avançadas exigem P&amp;D (adendo 23.6).
    /// </summary>
    public sealed class MarketingManager : MonoBehaviour
    {
        public static readonly MarketingCampaign[] AllCampaigns =
        {
            MarketingCampaign.Announcement, MarketingCampaign.Devlog, MarketingCampaign.PressKit,
            MarketingCampaign.WishlistCampaign, MarketingCampaign.Demo, MarketingCampaign.ClosedBeta,
            MarketingCampaign.Influencers, MarketingCampaign.GameShow
        };

        private RunState _run;
        private EconomyManager _economy;
        private ResearchManager _research;

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        public void Bind(RunState run, EconomyManager economy, ResearchManager research = null)
        {
            _run = run;
            _economy = economy;
            _research = research;
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.ended || !_run.HasActiveProject) return;
            var p = _run.currentProject;
            var mb = BalanceProvider.Get<MarketingBalanceSO>();
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            float decay = mb != null ? mb.HypeDecayPerWeek : 0.02f;
            float passive = mb != null ? mb.PassiveHypeGainPerWeek : 0.015f;
            p.hype01 = Mathf.Clamp01(p.hype01 * (1f - decay) + passive * p.themeHypeMultiplier);
            long potential = loop != null ? loop.BaseWishlistsFor(p.size) / Mathf.Max(1, p.targetWeeks) : 1_000;
            p.wishlists += (long)(p.hype01 * potential);
        }

        /// <summary>Custo, efeito e requisito de uma campanha (lidos do balanceamento a cada uso).</summary>
        public CampaignInfo InfoOf(MarketingCampaign type)
        {
            var mb = BalanceProvider.Get<MarketingBalanceSO>();
            switch (type)
            {
                case MarketingCampaign.Announcement:
                    return Make(type, "Anúncio oficial", mb != null ? mb.AnnouncementCost : 2_000, mb != null ? mb.AnnouncementHype : 0.15f, null, null);
                case MarketingCampaign.Devlog:
                    return Make(type, "Devlog público", mb != null ? mb.DevlogCost : 800, mb != null ? mb.DevlogHype : 0.05f, null, null);
                case MarketingCampaign.PressKit:
                    return Make(type, "Press kit", mb != null ? mb.PressKitCost : 1_500, mb != null ? mb.PressKitHype : 0.07f, null, null);
                case MarketingCampaign.WishlistCampaign:
                    return Make(type, "Campanha de wishlist", mb != null ? mb.WishlistCampaignCost : 5_000, mb != null ? mb.WishlistCampaignHype : 0.10f, "wishlist", "muitas wishlists");
                case MarketingCampaign.Demo:
                    return Make(type, "Demo jogável", mb != null ? mb.DemoCost : 8_000, mb != null ? mb.DemoHype : 0.25f, "demo", null);
                case MarketingCampaign.ClosedBeta:
                    return Make(type, "Beta fechado", mb != null ? mb.ClosedBetaCost : 3_000, mb != null ? mb.ClosedBetaHype : 0.06f, "beta", "menos bugs");
                case MarketingCampaign.Influencers:
                    return Make(type, "Influencers", mb != null ? mb.InfluencersCost : 12_000, mb != null ? mb.InfluencersHype : 0.30f, "influencers", null);
                default:
                    return Make(type, "Game show", mb != null ? mb.GameShowCost : 20_000, mb != null ? mb.GameShowHype : 0.40f, "gameshow", null);
            }
        }

        private static CampaignInfo Make(MarketingCampaign type, string label, long cost, float hype, string research, string extra)
        {
            string effect = "+" + Mathf.RoundToInt(hype * 100f) + " hype" + (extra != null ? " · " + extra : "");
            return new CampaignInfo(type, label, cost, hype, research, effect);
        }

        public bool IsUnlocked(MarketingCampaign type)
        {
            string key = InfoOf(type).ResearchKey;
            return key == null || _research == null || _research.IsMarketingUnlocked(key);
        }

        public bool IsUsed(MarketingCampaign type) =>
            _run != null && _run.HasActiveProject && _run.currentProject.campaignsUsed.Contains((int)type);

        public bool CanRun(MarketingCampaign type) =>
            _run != null && !_run.ended && _run.HasActiveProject && IsUnlocked(type) && !IsUsed(type)
            && _run.company.cash >= InfoOf(type).Cost;

        /// <summary>Executa uma campanha no projeto ativo. Retorna false se não puder (sem caixa, bloqueada, já feita).</summary>
        public bool RunCampaign(MarketingCampaign type)
        {
            if (!CanRun(type)) return false;
            var p = _run.currentProject;
            var info = InfoOf(type);
            var mb = BalanceProvider.Get<MarketingBalanceSO>();
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();

            _economy?.ApplyDelta(-info.Cost, $"Marketing: {info.Label}");
            p.marketingSpent += info.Cost;
            p.campaignsUsed.Add((int)type);

            float gain = info.Hype * p.themeHypeMultiplier * FounderPerks.HypeMultiplier;
            if (type == MarketingCampaign.GameShow || type == MarketingCampaign.Influencers) gain *= FounderPerks.ShowHypeMultiplier;
            if (Random.value < FounderPerks.ViralChance) gain *= 2f;
            p.hype01 = Mathf.Clamp01(p.hype01 + gain);

            if (type == MarketingCampaign.WishlistCampaign)
            {
                float burst = mb != null ? mb.WishlistCampaignBurst : 3f;
                long potential = loop != null ? loop.BaseWishlistsFor(p.size) / Mathf.Max(1, p.targetWeeks) : 1_000;
                p.wishlists += (long)(burst * potential);
            }
            else if (type == MarketingCampaign.ClosedBeta)
            {
                float cut = mb != null ? mb.ClosedBetaBugReduction : 0.3f;
                p.bugCount = Mathf.RoundToInt(p.bugCount * (1f - cut));
            }
            return true;
        }

        /// <summary>Custo de uma campanha (para a UI exibir).</summary>
        public long CostOf(MarketingCampaign type) => InfoOf(type).Cost;
    }
}
