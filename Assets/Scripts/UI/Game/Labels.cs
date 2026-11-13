using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State.Staff;
using GameLike.Systems.Marketing;
using GameLike.Systems.Research;
using GameLike.UI.Gallery;
using UnityEngine;

namespace GameLike.UI.Game
{
    /// <summary>
    /// Rótulos pt-BR e ícones/cores de apresentação para enums e conteúdo. <b>PROVISÓRIO:</b>
    /// texto direto em código até a migração para o Localization Package (pendência conhecida).
    /// </summary>
    internal static class Labels
    {
        public static string Phase(CompanyPhase p) => p switch
        {
            CompanyPhase.Garage => "Garagem",
            CompanyPhase.Indie => "Indie",
            CompanyPhase.Office => "Escritório",
            _ => "Estúdio"
        };

        public static string PhaseIcon(CompanyPhase p) => p switch
        {
            CompanyPhase.Garage => Kit.IcCar,
            CompanyPhase.Indie => Kit.IcMug,
            CompanyPhase.Office => Kit.IcBuilding,
            _ => Kit.IcCrown
        };

        public static string PhaseBlurb(CompanyPhase p) => p switch
        {
            CompanyPhase.Garage => "Só o fundador",
            CompanyPhase.Indie => "Coworking pequeno",
            CompanyPhase.Office => "Sede própria",
            _ => "Campus corporativo"
        };

        public static string Size(ProjectSize s) => s switch
        {
            ProjectSize.Small => "Pequeno",
            ProjectSize.Medium => "Médio",
            ProjectSize.Large => "Grande",
            _ => "AAA"
        };

        public static string SpecialtyName(Specialty s) => s switch
        {
            Specialty.Design => "Design",
            Specialty.Art => "Arte",
            Specialty.Audio => "Áudio",
            Specialty.Programming => "Programação",
            Specialty.Content => "Conteúdo",
            _ => "Generalista"
        };

        public static Color SpecialtyColor(Specialty s) => s switch
        {
            Specialty.Design => Kit.Teal,
            Specialty.Art => Kit.Pink,
            Specialty.Audio => Kit.Purple,
            Specialty.Programming => Kit.Sky,
            Specialty.Content => Kit.Green,
            _ => Kit.Soft
        };

        public static string SeniorityName(Seniority s) => s switch
        {
            Seniority.Junior => "Júnior",
            Seniority.Mid => "Pleno",
            Seniority.Senior => "Sênior",
            _ => "Estrela"
        };

        public static string TraitName(PersonalityTrait t) => t switch
        {
            PersonalityTrait.Workaholic => "Workaholic",
            PersonalityTrait.Lazy => "Preguiçoso",
            PersonalityTrait.Perfectionist => "Perfeccionista",
            PersonalityTrait.Egocentric => "Egocêntrico",
            PersonalityTrait.Mentor => "Mentor",
            PersonalityTrait.Loyal => "Leal",
            PersonalityTrait.Ambitious => "Ambicioso",
            PersonalityTrait.Creative => "Criativo",
            PersonalityTrait.BurnoutProne => "Propenso a burnout",
            _ => null
        };

        public static string Category(NarrativeCategory c) => c switch
        {
            NarrativeCategory.PublicStatement => "Declarações públicas",
            NarrativeCategory.OperationalCrisis => "Crise operacional",
            NarrativeCategory.EthicalDilemma => "Dilema ético",
            NarrativeCategory.Opportunity => "Oportunidade",
            NarrativeCategory.HumanResources => "RH",
            NarrativeCategory.Market => "Mercado",
            _ => "Absurdo"
        };

        public static (string icon, Color color) CategoryStyle(NarrativeCategory c) => c switch
        {
            NarrativeCategory.PublicStatement => (Kit.IcMic, Kit.Purple),
            NarrativeCategory.OperationalCrisis => (Kit.IcWarn, Kit.Red),
            NarrativeCategory.EthicalDilemma => (Kit.IcScale, Kit.Orange),
            NarrativeCategory.Opportunity => (Kit.IcStar, Kit.Green),
            NarrativeCategory.HumanResources => (Kit.IcUsers, Kit.Sky),
            NarrativeCategory.Market => (Kit.IcGlobe, Kit.Blue),
            _ => (Kit.IcGhost, Kit.Pink)
        };

        public static string Skill(FounderSkill s) => s switch
        {
            FounderSkill.Business => "Visão de Negócio",
            FounderSkill.Leadership => "Liderança",
            FounderSkill.Technical => "Técnica",
            FounderSkill.Marketing => "Marketing",
            FounderSkill.Charisma => "Carisma",
            FounderSkill.Networking => "Networking",
            _ => ""
        };

        public static (string icon, Color color) SkillStyle(FounderSkill s) => s switch
        {
            FounderSkill.Business => (Kit.IcBriefcase, Kit.Cash),
            FounderSkill.Leadership => (Kit.IcUsers, Kit.Orange),
            FounderSkill.Technical => (Kit.IcCode, Kit.Sky),
            FounderSkill.Marketing => (Kit.IcBullhorn, Kit.Pink),
            FounderSkill.Charisma => (Kit.IcComments, Kit.Purple),
            _ => (Kit.IcNetwork, Kit.Teal)
        };

        public static string GenreIcon(string key)
        {
            if (string.IsNullOrEmpty(key)) return Kit.IcGamepad;
            if (key.Contains("rpg")) return Kit.IcDragon;
            if (key.Contains("puzzle")) return Kit.IcCubes;
            if (key.Contains("sim")) return Kit.IcPlane;
            if (key.Contains("strat")) return Kit.IcFlag;
            if (key.Contains("shoot") || key.Contains("fps")) return Kit.IcBullseye;
            if (key.Contains("horror")) return Kit.IcGhost;
            if (key.Contains("plat")) return Kit.IcRocket;
            if (key.Contains("tycoon")) return Kit.IcBuilding;
            return Kit.IcGamepad;
        }

        public static (string icon, Color color) ThemeStyle(string key)
        {
            if (string.IsNullOrEmpty(key)) return (Kit.IcImage, Kit.Soft);
            if (key.Contains("cyber")) return (Kit.IcChip, Kit.Pink);
            if (key.Contains("fantasia")) return (Kit.IcHat, Kit.Purple);
            if (key.Contains("espaco")) return (Kit.IcShuttle, Kit.Sky);
            if (key.Contains("fazenda")) return (Kit.IcHouse, Kit.Green);
            if (key.Contains("futebol")) return (Kit.IcFutbol, Kit.Green);
            if (key.Contains("apocal")) return (Kit.IcSkull, Kit.Orange);
            if (key.Contains("terror")) return (Kit.IcGhost, Kit.RGB(70, 76, 96));
            if (key.Contains("retro") || key.Contains("anos")) return (Kit.IcGamepad, Kit.Gold);
            return (Kit.IcImage, Kit.Teal);
        }

        public static string PlatformIcon(PlatformCategory c) => c switch
        {
            PlatformCategory.PC => Kit.IcDesktop,
            PlatformCategory.Mobile => Kit.IcMobile,
            PlatformCategory.VR => Kit.IcVr,
            _ => Kit.IcGamepad
        };

        public static string CampaignIcon(MarketingCampaign c) => c switch
        {
            MarketingCampaign.Announcement => Kit.IcBullhorn,
            MarketingCampaign.WishlistCampaign => Kit.IcHeart,
            MarketingCampaign.Demo => Kit.IcGamepad,
            MarketingCampaign.GameShow => Kit.IcTrophy,
            MarketingCampaign.Devlog => Kit.IcVideo,
            MarketingCampaign.PressKit => Kit.IcNews,
            MarketingCampaign.ClosedBeta => Kit.IcUsers,
            _ => Kit.IcStar
        };

        public static string ResearchIcon(ResearchNode n) => n.Kind switch
        {
            UnlockKind.Theme => ThemeStyle(n.Target).icon,
            UnlockKind.Size => Kit.IcExpand,
            UnlockKind.Engine => Kit.IcChip,
            UnlockKind.Marketing => Kit.IcBullhorn,
            _ => Kit.IcBriefcase
        };

        public static string Tier(ResearchTier t) => t switch
        {
            ResearchTier.Basic => "Básica",
            ResearchTier.Intermediate => "Intermediária",
            _ => "Avançada"
        };

        public static string Score(float score) => score.ToString("0.0").Replace('.', ',');

        public static Color ScoreColor(float score) => score >= 8f ? Kit.Green : score >= 6f ? Kit.Gold : Kit.Red;

        public static string HypeBand(float hype100) =>
            hype100 <= 20f ? "Vendas fracas" : hype100 <= 50f ? "Vendas medianas" : hype100 <= 80f ? "Vendas boas" : "Vendas excelentes";
    }
}
