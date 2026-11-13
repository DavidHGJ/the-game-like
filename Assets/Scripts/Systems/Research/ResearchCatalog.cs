using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Meta;
using UnityEngine;

namespace GameLike.Systems.Research
{
    /// <summary>Faixa de custo de uma pesquisa (GDD 19.3).</summary>
    public enum ResearchTier
    {
        Basic,
        Intermediate,
        Advanced
    }

    /// <summary>O que uma pesquisa desbloqueia.</summary>
    public enum UnlockKind
    {
        Theme,
        Size,
        Engine,
        Marketing,
        Feature
    }

    /// <summary>Peso de um módulo de engine (GDD 19.4).</summary>
    public enum EngineModuleWeight
    {
        None,
        Light,
        Medium,
        Heavy
    }

    /// <summary>Uma pesquisa do catálogo de P&amp;D (definição imutável montada em runtime).</summary>
    public sealed class ResearchNode
    {
        public readonly string Id;
        public readonly string Label;
        public readonly ResearchTier Tier;
        public readonly CompanyPhase MinPhase;
        public readonly UnlockKind Kind;
        public readonly string Target;
        public readonly EngineModuleWeight ModuleWeight;
        public readonly string Group;

        public ResearchNode(string id, string label, ResearchTier tier, CompanyPhase minPhase, UnlockKind kind, string target,
            EngineModuleWeight moduleWeight = EngineModuleWeight.None, string group = null)
        {
            Id = id;
            Label = label;
            Tier = tier;
            MinPhase = minPhase;
            Kind = kind;
            Target = target;
            ModuleWeight = moduleWeight;
            Group = group;
        }
    }

    /// <summary>
    /// Catálogo de P&amp;D da run (GDD 3.1 / 6): temas, escopos, módulos de engine (19.4), ações de
    /// marketing (adendo 23.6) e recursos de negócio (franquias, M&amp;A, IPO).
    /// <b>PROVISÓRIO:</b> módulos/recursos em código até virarem <c>EngineModuleSO</c>/<c>ToolSO</c>.
    /// </summary>
    public static class ResearchCatalog
    {
        public const string FeatureFranchise = "feature:franchise";
        public const string FeatureAcquisition = "feature:mna";
        public const string FeatureIpo = "feature:ipo";

        public static string ThemeNodeId(string themeKey) => "theme:" + themeKey;
        public static string SizeNodeId(ProjectSize size) => "size:" + size.ToString().ToLowerInvariant();
        public static string EngineNodeId(string key) => "engine:" + key;
        public static string MarketingNodeId(string key) => "marketing:" + key;

        /// <summary>Módulos que toda empresa já tem ao nascer — a "engine básica" (200 EB).</summary>
        public static readonly string[] StarterEngineModules = { "render2d_basic", "audio_basic" };

        public static List<ResearchNode> Build(IEnumerable<ThemeSO> themes)
        {
            var list = new List<ResearchNode>();

            Engine(list, "render2d_basic", "Renderizador 2D básico", EngineModuleWeight.Light, CompanyPhase.Garage, "Renderização");
            Engine(list, "audio_basic", "Motor de áudio básico", EngineModuleWeight.Light, CompanyPhase.Garage, "Áudio");
            Engine(list, "physics2d_basic", "Física 2D básica", EngineModuleWeight.Light, CompanyPhase.Garage, "Física");
            Engine(list, "anim_spritesheet", "Animação spritesheet", EngineModuleWeight.Light, CompanyPhase.Garage, "Animação");
            Engine(list, "render2d_adv", "Renderizador 2D avançado", EngineModuleWeight.Medium, CompanyPhase.Garage, "Renderização");
            Engine(list, "physics2d_adv", "Física 2D avançada", EngineModuleWeight.Medium, CompanyPhase.Indie, "Física");
            Engine(list, "pathfinding", "Pathfinding", EngineModuleWeight.Medium, CompanyPhase.Indie, "IA");
            Engine(list, "render3d_basic", "Renderizador 3D básico", EngineModuleWeight.Medium, CompanyPhase.Indie, "Renderização");
            Engine(list, "audio_3d", "Áudio posicional 3D", EngineModuleWeight.Medium, CompanyPhase.Indie, "Áudio");
            Engine(list, "anim_skeletal", "Animação esqueletal", EngineModuleWeight.Medium, CompanyPhase.Indie, "Animação");
            Engine(list, "render3d_adv", "Renderizador 3D avançado", EngineModuleWeight.Heavy, CompanyPhase.Office, "Renderização");
            Engine(list, "physics3d", "Física 3D", EngineModuleWeight.Heavy, CompanyPhase.Office, "Física");
            Engine(list, "anim_procedural", "Animação procedural", EngineModuleWeight.Heavy, CompanyPhase.Office, "Animação");
            Engine(list, "dedicated_servers", "Servidores dedicados", EngineModuleWeight.Heavy, CompanyPhase.Office, "Rede");
            Engine(list, "ml_npcs", "Machine learning NPCs", EngineModuleWeight.Heavy, CompanyPhase.Studio, "IA");
            Engine(list, "raytracing", "Ray tracing", EngineModuleWeight.Heavy, CompanyPhase.Studio, "Renderização");

            if (themes != null)
            {
                foreach (var t in themes)
                {
                    if (t == null) continue;
                    list.Add(new ResearchNode(
                        ThemeNodeId(t.name), t.DisplayLabel, ResearchTier.Basic,
                        CompanyPhase.Garage, UnlockKind.Theme, t.name));
                }
            }

            list.Add(new ResearchNode(SizeNodeId(ProjectSize.Medium), "Projetos Médios",
                ResearchTier.Intermediate, CompanyPhase.Indie, UnlockKind.Size, ProjectSize.Medium.ToString()));
            list.Add(new ResearchNode(SizeNodeId(ProjectSize.Large), "Projetos Grandes",
                ResearchTier.Advanced, CompanyPhase.Office, UnlockKind.Size, ProjectSize.Large.ToString()));
            list.Add(new ResearchNode(SizeNodeId(ProjectSize.AAA), "Projetos AAA",
                ResearchTier.Advanced, CompanyPhase.Studio, UnlockKind.Size, ProjectSize.AAA.ToString()));

            Marketing(list, "wishlist", "Campanha de wishlist", ResearchTier.Basic, CompanyPhase.Garage);
            Marketing(list, "demo", "Demo jogável", ResearchTier.Intermediate, CompanyPhase.Garage);
            Marketing(list, "beta", "Beta fechado", ResearchTier.Basic, CompanyPhase.Indie);
            Marketing(list, "influencers", "Influencer Network", ResearchTier.Intermediate, CompanyPhase.Office);
            Marketing(list, "gameshow", "Game show", ResearchTier.Advanced, CompanyPhase.Office);

            list.Add(new ResearchNode(FeatureFranchise, "Gestão de Franquias", ResearchTier.Advanced,
                CompanyPhase.Office, UnlockKind.Feature, "franchise"));
            list.Add(new ResearchNode(FeatureAcquisition, "Fusões e Aquisições (M&A)", ResearchTier.Advanced,
                CompanyPhase.Office, UnlockKind.Feature, "mna"));
            list.Add(new ResearchNode(FeatureIpo, "Abertura de capital (IPO)", ResearchTier.Advanced,
                CompanyPhase.Studio, UnlockKind.Feature, "ipo"));
            return list;
        }

        private static void Engine(List<ResearchNode> list, string key, string label, EngineModuleWeight weight, CompanyPhase phase, string group)
        {
            var tier = weight == EngineModuleWeight.Light ? ResearchTier.Basic
                : weight == EngineModuleWeight.Medium ? ResearchTier.Intermediate : ResearchTier.Advanced;
            list.Add(new ResearchNode(EngineNodeId(key), label, tier, phase, UnlockKind.Engine, key, weight, group));
        }

        private static void Marketing(List<ResearchNode> list, string key, string label, ResearchTier tier, CompanyPhase phase)
        {
            list.Add(new ResearchNode(MarketingNodeId(key), label, tier, phase, UnlockKind.Marketing, key));
        }
    }

    /// <summary>
    /// Engine da empresa (adendo 23.7): Engine Budget e custo semanal a partir dos módulos
    /// pesquisados. Lógica pura — testável sem cena.
    /// </summary>
    public static class EngineCalculator
    {
        public static int ModuleBudget(EngineModuleWeight weight, EngineBalanceSO b) => weight switch
        {
            EngineModuleWeight.Light => b != null ? b.LightModule.engineBudgetCost : 100,
            EngineModuleWeight.Medium => b != null ? b.MediumModule.engineBudgetCost : 250,
            EngineModuleWeight.Heavy => b != null ? b.HeavyModule.engineBudgetCost : 500,
            _ => 0
        };

        public static long ModuleWeeklyCost(EngineModuleWeight weight, EngineBalanceSO b) => weight switch
        {
            EngineModuleWeight.Light => b != null ? b.LightModule.weeklyUpkeepCost : 100,
            EngineModuleWeight.Medium => b != null ? b.MediumModule.weeklyUpkeepCost : 300,
            EngineModuleWeight.Heavy => b != null ? b.HeavyModule.weeklyUpkeepCost : 700,
            _ => 0
        };

        /// <summary>Engine Budget total: soma dos módulos desbloqueados, com o bônus do fundador.</summary>
        public static int TotalBudget(IReadOnlyList<ResearchNode> catalog, ResearchState research, EngineBalanceSO b)
        {
            if (catalog == null || research == null) return 0;
            int sum = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i].Kind == UnlockKind.Engine && research.IsUnlocked(catalog[i].Id))
                    sum += ModuleBudget(catalog[i].ModuleWeight, b);
            return Mathf.RoundToInt(sum * FounderPerks.EngineBudgetMultiplier);
        }

        /// <summary>Custo semanal da engine enquanto há jogo em produção.</summary>
        public static long WeeklyCost(IReadOnlyList<ResearchNode> catalog, ResearchState research, EngineBalanceSO b)
        {
            if (catalog == null || research == null) return 0;
            long sum = 0;
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i].Kind == UnlockKind.Engine && research.IsUnlocked(catalog[i].Id))
                    sum += ModuleWeeklyCost(catalog[i].ModuleWeight, b);
            return sum;
        }

        /// <summary>Componente "Engine Budget vs. teto" da nota (GDD 19.7), 0..1.</summary>
        public static float BudgetScore01(int engineBudget, ProjectSize size, EngineBalanceSO b)
        {
            int reference = b != null ? b.ReferenceBudgetFor(size) : 200;
            if (reference <= 0) return 1f;
            return Mathf.Clamp01(engineBudget / (float)reference);
        }
    }
}
