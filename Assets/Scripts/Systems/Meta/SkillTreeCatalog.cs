using System.Collections.Generic;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State.Meta;
using UnityEngine;

namespace GameLike.Systems.Meta
{
    /// <summary>Nó único (PL) de uma árvore de habilidades (GDD 14.3).</summary>
    public sealed class PerkDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Description;
        public readonly int CostPL;
        public readonly FounderSkill Skill;
        /// <summary>Falso enquanto o sistema que o perk afeta ainda não existe (UI mostra "em breve").</summary>
        public readonly bool Implemented;

        public PerkDef(string id, string name, string description, int costPL, FounderSkill skill, bool implemented)
        {
            Id = id; Name = name; Description = description; CostPL = costPL; Skill = skill; Implemented = implemented;
        }
    }

    /// <summary>Uma das 6 árvores do fundador: raiz incremental ($) + 4 filhos únicos (PL).</summary>
    public sealed class SkillTreeDef
    {
        public readonly FounderSkill Skill;
        public readonly string Name;
        public readonly string Summary;
        public readonly string RootName;
        public readonly string RootDescription;
        public readonly PerkDef[] Perks;

        public SkillTreeDef(FounderSkill skill, string name, string summary, string rootName, string rootDescription, params PerkDef[] perks)
        {
            Skill = skill; Name = name; Summary = summary; RootName = rootName; RootDescription = rootDescription; Perks = perks;
        }
    }

    /// <summary>
    /// Conteúdo das árvores de habilidades do fundador (GDD 14.4–14.9) e as regras de compra
    /// (19.5). <b>PROVISÓRIO:</b> o GDD pede ScriptableObjects; mantido em código até o
    /// pipeline de conteúdo de habilidades existir (registrado em AUTONOMOUS_BUILD).
    /// </summary>
    public static class SkillTreeCatalog
    {
        public const string NegociadorNato = "negociador_nato", OlhoNoMercado = "olho_no_mercado",
            VisaoLongoPrazo = "visao_longo_prazo", FusaoEstrategica = "fusao_estrategica",
            AntiBurnout = "anti_burnout", RetencaoTalentos = "retencao_talentos", MentorNato = "mentor_nato",
            EquipeEnxuta = "equipe_enxuta", ArquitetoEngine = "arquiteto_engine", CodeReview = "code_review",
            PrototipagemRapida = "prototipagem_rapida", Otimizador = "otimizador", TimingPerfeito = "timing_perfeito",
            ViralNatural = "viral_natural", RelacoesImprensa = "relacoes_imprensa", RecuperacaoCrise = "recuperacao_crise",
            CeoCelebridade = "ceo_celebridade", DiscursoAfiado = "discurso_afiado", GerenciamentoCrise = "gerenciamento_crise",
            EmbaixadorMarca = "embaixador_marca", ContatosMercado = "contatos_mercado",
            ParceiroEstrategico = "parceiro_estrategico", Insider = "insider", Headhunter = "headhunter";

        private static readonly SkillTreeDef[] Trees =
        {
            new SkillTreeDef(FounderSkill.Business, "Visão de Negócio", "Valuation, negociações e M&A",
                "Faro Empresarial", "+5% no valuation da empresa por nível.",
                P(NegociadorNato, "Negociador Nato", "Licenças de plataforma custam 25% menos.", 5, FounderSkill.Business, true),
                P(OlhoNoMercado, "Olho no Mercado", "O painel de Mercado mostra o valor exato das tendências.", 10, FounderSkill.Business, true),
                P(VisaoLongoPrazo, "Visão de Longo Prazo", "A estagnação de fase corrói o valuation pela metade.", 15, FounderSkill.Business, true),
                P(FusaoEstrategica, "Fusão Estratégica", "A pesquisa de M&A custa metade dos PP.", 25, FounderSkill.Business, true)),
            new SkillTreeDef(FounderSkill.Leadership, "Liderança", "Burnout, retenção e eficiência da equipe",
                "Cultura Forte", "+5% de humor inicial dos contratados por nível.",
                P(AntiBurnout, "Anti-Burnout", "A equipe acumula burnout 30% mais devagar.", 5, FounderSkill.Leadership, true),
                P(RetencaoTalentos, "Retenção de Talentos", "Rivais têm menos chance de levar seus funcionários.", 10, FounderSkill.Leadership, false),
                P(MentorNato, "Mentor Nato", "Funcionários ganham 25% mais XP por jogo lançado.", 15, FounderSkill.Leadership, true),
                P(EquipeEnxuta, "Equipe Enxuta", "Equipes de até 3 pessoas rendem 10% mais pontos por fase.", 10, FounderSkill.Leadership, true)),
            new SkillTreeDef(FounderSkill.Technical, "Técnica", "Qualidade dos jogos, engines e P&D",
                "Curva de Aprendizado", "+10% de PP gerado durante o desenvolvimento por nível.",
                P(ArquitetoEngine, "Arquiteto de Engine", "Módulos de engine custam 25% menos PP.", 10, FounderSkill.Technical, true),
                P(CodeReview, "Code Review", "25% menos bugs gerados durante o desenvolvimento.", 5, FounderSkill.Technical, true),
                P(PrototipagemRapida, "Prototipagem Rápida", "Projetos Pequenos ficam prontos 15% mais rápido.", 10, FounderSkill.Technical, true),
                P(Otimizador, "Otimizador", "+20% de Engine Budget na engine da empresa.", 20, FounderSkill.Technical, true)),
            new SkillTreeDef(FounderSkill.Marketing, "Marketing", "Hype, vendas e impacto das campanhas",
                "Presença Digital", "+5% de hype gerado por todas as campanhas por nível.",
                P(TimingPerfeito, "Timing Perfeito", "A tela de lançamento mostra a tendência do tema nas próximas semanas.", 5, FounderSkill.Marketing, false),
                P(ViralNatural, "Viral Natural", "25% de chance de uma campanha render o dobro de hype.", 10, FounderSkill.Marketing, true),
                P(RelacoesImprensa, "Relações com Imprensa", "+0,3 na nota recebida da crítica.", 20, FounderSkill.Marketing, true),
                P(RecuperacaoCrise, "Recuperação de Crise", "Quedas de reputação em eventos são 40% menores.", 10, FounderSkill.Marketing, true)),
            new SkillTreeDef(FounderSkill.Charisma, "Carisma", "Declarações públicas e imagem do CEO",
                "Presença Pública", "Destrava as opções de [Carisma] nos eventos. Cada nível soma +2% de reputação inicial.",
                P(CeoCelebridade, "CEO Celebridade", "Ganhos de reputação em eventos são 40% maiores.", 10, FounderSkill.Charisma, true),
                P(DiscursoAfiado, "Discurso Afiado", "Desbloqueia declarações exclusivas de alto impacto.", 15, FounderSkill.Charisma, false),
                P(GerenciamentoCrise, "Gerenciamento de Crise", "Perdas de caixa em eventos são 30% menores.", 10, FounderSkill.Charisma, true),
                P(EmbaixadorMarca, "Embaixador da Marca", "Game show e influencers rendem 30% mais hype.", 10, FounderSkill.Charisma, true)),
            new SkillTreeDef(FounderSkill.Networking, "Networking", "Contratos, talentos raros e informação",
                "Agenda Cheia", "Destrava as opções de [Networking] nos eventos. Cada nível facilita 5% a compra de rivais.",
                P(ContatosMercado, "Contatos no Mercado", "O mercado de talentos já começa com candidatos melhores.", 15, FounderSkill.Networking, true),
                P(ParceiroEstrategico, "Parceiro Estratégico", "Reduz o custo de co-desenvolvimento e publisher.", 10, FounderSkill.Networking, false),
                P(Insider, "Insider", "Você é avisado antes de um rival lançar um jogo.", 10, FounderSkill.Networking, false),
                P(Headhunter, "Headhunter", "Aumenta bastante a qualidade dos candidatos disponíveis.", 20, FounderSkill.Networking, true))
        };

        private static PerkDef P(string id, string name, string desc, int cost, FounderSkill skill, bool implemented) =>
            new PerkDef(id, name, desc, cost, skill, implemented);

        public static IReadOnlyList<SkillTreeDef> All => Trees;

        public static PerkDef FindPerk(string id)
        {
            for (int t = 0; t < Trees.Length; t++)
                for (int p = 0; p < Trees[t].Perks.Length; p++)
                    if (Trees[t].Perks[p].Id == id) return Trees[t].Perks[p];
            return null;
        }

        /// <summary>Custo em $ da PRÓXIMA compra do nó raiz no nível dado (GDD 19.5: 10.000, dobra a cada compra).</summary>
        public static long RootCost(int currentLevel)
        {
            var b = BalanceProvider.Get<LegacyProgressionBalanceSO>();
            double first = b != null ? b.IncrementalNodeFirstCost : 10_000;
            double mult = b != null ? b.IncrementalNodeCostMultiplier : 2f;
            return (long)(first * System.Math.Pow(mult, Mathf.Max(0, currentLevel)));
        }

        public static long ResetCost()
        {
            var b = BalanceProvider.Get<LegacyProgressionBalanceSO>();
            return b != null ? b.LegacyResetCost : 50_000;
        }

        /// <summary>Compra um nível do nó raiz com Dinheiro Pessoal. Falso se não há dinheiro.</summary>
        public static bool TryBuyRoot(MetaProgressionState meta, FounderSkill skill)
        {
            if (meta == null || skill == FounderSkill.None) return false;
            meta.EnsureCollections();
            int i = (int)skill - 1;
            long cost = RootCost(meta.skillLevels[i]);
            if (meta.personalMoney < cost) return false;
            meta.personalMoney -= cost;
            meta.skillLevels[i]++;
            return true;
        }

        /// <summary>Compra um nó único com Pontos de Legado. Falso se já tem, não existe ou falta PL.</summary>
        public static bool TryBuyPerk(MetaProgressionState meta, string perkId)
        {
            var perk = FindPerk(perkId);
            if (meta == null || perk == null || !perk.Implemented) return false;
            meta.EnsureCollections();
            if (meta.perks.Contains(perkId) || meta.legacyPoints < perk.CostPL) return false;
            meta.legacyPoints -= perk.CostPL;
            meta.legacySpent += perk.CostPL;
            meta.perks.Add(perkId);
            return true;
        }

        /// <summary>Devolve os PL gastos pagando o custo de reset em $ (GDD 14.3). Raízes não resetam.</summary>
        public static bool TryResetPerks(MetaProgressionState meta)
        {
            if (meta == null) return false;
            meta.EnsureCollections();
            long cost = ResetCost();
            if (meta.perks.Count == 0 || meta.personalMoney < cost) return false;
            meta.personalMoney -= cost;
            meta.legacyPoints += meta.legacySpent;
            meta.legacySpent = 0;
            meta.perks.Clear();
            return true;
        }
    }

    /// <summary>
    /// Consulta dos efeitos das habilidades permanentes do fundador sobre os sistemas da run.
    /// É "método pergunta": os calculadores leem daqui a cada uso. Ligado pelo GameController.
    /// </summary>
    public static class FounderPerks
    {
        private static MetaProgressionState _meta;

        public static void Bind(MetaProgressionState meta) => _meta = meta;

        public static int Level(FounderSkill skill)
        {
            if (_meta == null || skill == FounderSkill.None || _meta.skillLevels == null) return 0;
            int i = (int)skill - 1;
            return i >= 0 && i < _meta.skillLevels.Length ? _meta.skillLevels[i] : 0;
        }

        public static bool Has(string perkId) => _meta != null && _meta.perks != null && _meta.perks.Contains(perkId);

        public static float ValuationMultiplier => 1f + 0.05f * Level(FounderSkill.Business);
        public static float HireMoodBonus => 0.05f * Level(FounderSkill.Leadership);
        public static float ResearchPointMultiplier => 1f + 0.10f * Level(FounderSkill.Technical);
        public static float HypeMultiplier => 1f + 0.05f * Level(FounderSkill.Marketing);
        public static float StartingReputationBonus => 0.02f * Level(FounderSkill.Charisma);
        public static float AcquisitionEase => 0.05f * Level(FounderSkill.Networking);

        public static float LicenseCostMultiplier => Has(SkillTreeCatalog.NegociadorNato) ? 0.75f : 1f;
        public static float StagnationMultiplier => Has(SkillTreeCatalog.VisaoLongoPrazo) ? 0.5f : 1f;
        public static float BurnoutMultiplier => Has(SkillTreeCatalog.AntiBurnout) ? 0.7f : 1f;
        public static float XpMultiplier => Has(SkillTreeCatalog.MentorNato) ? 1.25f : 1f;
        public static float BugMultiplier => Has(SkillTreeCatalog.CodeReview) ? 0.75f : 1f;
        public static float SmallProjectDurationMultiplier => Has(SkillTreeCatalog.PrototipagemRapida) ? 0.85f : 1f;
        public static float EngineBudgetMultiplier => Has(SkillTreeCatalog.Otimizador) ? 1.2f : 1f;
        public static float EngineResearchCostMultiplier => Has(SkillTreeCatalog.ArquitetoEngine) ? 0.75f : 1f;
        public static float AcquisitionResearchCostMultiplier => Has(SkillTreeCatalog.FusaoEstrategica) ? 0.5f : 1f;
        public static float ReviewBonus => Has(SkillTreeCatalog.RelacoesImprensa) ? 0.3f : 0f;
        public static float ViralChance => Has(SkillTreeCatalog.ViralNatural) ? 0.25f : 0f;
        public static float ReputationLossMultiplier => Has(SkillTreeCatalog.RecuperacaoCrise) ? 0.6f : 1f;
        public static float ReputationGainMultiplier => Has(SkillTreeCatalog.CeoCelebridade) ? 1.4f : 1f;
        public static float EventCashLossMultiplier => Has(SkillTreeCatalog.GerenciamentoCrise) ? 0.7f : 1f;
        public static float ShowHypeMultiplier => Has(SkillTreeCatalog.EmbaixadorMarca) ? 1.3f : 1f;
        public static bool ShowsExactTrends => Has(SkillTreeCatalog.OlhoNoMercado);

        public static float CandidateQualityBias =>
            (Has(SkillTreeCatalog.ContatosMercado) ? 0.3f : 0f) + (Has(SkillTreeCatalog.Headhunter) ? 0.5f : 0f);

        /// <summary>Bônus de pontos por fase para equipes pequenas (Equipe Enxuta).</summary>
        public static float SmallTeamPointMultiplier(int teamSize) =>
            Has(SkillTreeCatalog.EquipeEnxuta) && teamSize <= 3 ? 1.1f : 1f;
    }
}
