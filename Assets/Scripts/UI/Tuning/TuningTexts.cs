using System;
using System.Collections.Generic;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Nomes e explicações em português de cada parâmetro da tela de Parâmetros. A chave é o nome
    /// do campo no código; um campo novo sem entrada aqui ainda aparece (com o nome derivado do
    /// código e o Tooltip do asset). Ao criar um parâmetro, acrescente a entrada correspondente.
    /// </summary>
    internal static class TuningTexts
    {
        private static readonly Dictionary<string, string> Groups = new Dictionary<string, string>
        {
            { "Economy", "Economia" },
            { "Engine", "Engine" },
            { "GameLoop", "Desenvolvimento e vendas" },
            { "GameTuning", "Tempo, inflação e fim de run" },
            { "LegacyProgression", "Legado e árvore" },
            { "Marketing", "Marketing e hype" },
            { "QualityFormula", "Nota dos jogos" },
            { "Research", "Pesquisa" },
        };

        private static readonly Dictionary<string, (string label, string help)> Fields = new Dictionary<string, (string, string)>
        {
            // ---------------- Economia
            { "garageToIndieThreshold", ("Valuation para sair da Garagem", "Valor de mercado que a empresa precisa atingir para liberar a expansão para Indie.") },
            { "indieToOfficeThreshold", ("Valuation para sair do Indie", "Valor de mercado que libera a expansão para Escritório.") },
            { "officeToStudioThreshold", ("Valuation para sair do Escritório", "Valor de mercado que libera a expansão para Estúdio.") },
            { "juniorWeeklySalary", ("Salário semanal — júnior", "Quanto um funcionário júnior custa por semana.") },
            { "midWeeklySalary", ("Salário semanal — pleno", "Quanto um funcionário pleno custa por semana.") },
            { "seniorWeeklySalary", ("Salário semanal — sênior", "Quanto um funcionário sênior custa por semana.") },
            { "starWeeklySalary", ("Salário semanal — estrela", "Quanto um funcionário estrela custa por semana.") },
            { "garageWeeklyOperating", ("Custo fixo semanal — Garagem", "Aluguel/contas pagos toda semana na Garagem, além dos salários.") },
            { "indieWeeklyOperating", ("Custo fixo semanal — Indie", "Aluguel/contas pagos toda semana na fase Indie.") },
            { "officeWeeklyOperating", ("Custo fixo semanal — Escritório", "Aluguel/contas pagos toda semana na fase Escritório.") },
            { "studioWeeklyOperating", ("Custo fixo semanal — Estúdio", "Aluguel/contas pagos toda semana na fase Estúdio.") },
            { "expandToIndieCost", ("Custo da expansão para Indie", "Quanto o jogador paga para mudar da Garagem para o Indie.") },
            { "expandToOfficeCost", ("Custo da expansão para Escritório", "Quanto o jogador paga para mudar para o Escritório.") },
            { "expandToStudioCost", ("Custo da expansão para Estúdio", "Quanto o jogador paga para mudar para o Estúdio.") },
            { "stagnationLossPerWeek", ("Perda por estagnação (por semana)", "Fração do valuation perdida a cada semana em que a expansão está disponível e não é feita. 0,005 = 0,5% por semana.") },
            { "stagnationLossMax", ("Perda máxima por estagnação", "Teto da perda acumulada por não expandir. 0,30 = no máximo 30% do valuation.") },

            // ---------------- Engine
            { "lightModule", ("Módulo de engine leve", "Custo de um módulo pequeno de engine.") },
            { "mediumModule", ("Módulo de engine médio", "Custo de um módulo intermediário de engine.") },
            { "heavyModule", ("Módulo de engine pesado", "Custo de um módulo grande de engine.") },
            { "engineBudgetCost", ("Engine Budget que ocupa", "Quanto do orçamento técnico (EB) o módulo consome.") },
            { "weeklyUpkeepCost", ("Manutenção por semana", "Quanto o módulo custa por semana enquanto há jogo em produção.") },
            { "basicEngineBudget", ("EB de referência — Pequeno", "Engine Budget que um jogo Pequeno precisa para tirar nota cheia nesse quesito.") },
            { "intermediateEngineBudget", ("EB de referência — Médio", "Engine Budget de referência para jogos Médios.") },
            { "advancedEngineBudget", ("EB de referência — Grande", "Engine Budget de referência para jogos Grandes.") },
            { "completeEngineBudget", ("EB de referência — AAA", "Engine Budget de referência para jogos AAA.") },

            // ---------------- Desenvolvimento e vendas
            { "startingCash", ("Dinheiro inicial", "Caixa com que a empresa começa a primeira run (antes do capital pessoal investido).") },
            { "smallWeeks", ("Duração — Pequeno (semanas)", "Semanas totais de desenvolvimento de um jogo Pequeno, divididas entre as 5 fases.") },
            { "mediumWeeks", ("Duração — Médio (semanas)", "Semanas totais de desenvolvimento de um jogo Médio.") },
            { "largeWeeks", ("Duração — Grande (semanas)", "Semanas totais de desenvolvimento de um jogo Grande.") },
            { "aaaWeeks", ("Duração — AAA (semanas)", "Semanas totais de desenvolvimento de um jogo AAA.") },
            { "smallTeam", ("Equipe mínima — Pequeno", "Pessoas necessárias para tocar um jogo Pequeno.") },
            { "mediumTeam", ("Equipe mínima — Médio", "Pessoas necessárias para tocar um jogo Médio.") },
            { "largeTeam", ("Equipe mínima — Grande", "Pessoas necessárias para tocar um jogo Grande.") },
            { "aaaTeam", ("Equipe mínima — AAA", "Pessoas necessárias para tocar um jogo AAA.") },
            { "smallBaseUnits", ("Vendas-base — Pequeno", "Unidades que um jogo Pequeno nota 10 vende num mercado neutro. Notas menores vendem menos.") },
            { "mediumBaseUnits", ("Vendas-base — Médio", "Unidades que um jogo Médio nota 10 vende num mercado neutro.") },
            { "largeBaseUnits", ("Vendas-base — Grande", "Unidades que um jogo Grande nota 10 vende num mercado neutro.") },
            { "aaaBaseUnits", ("Vendas-base — AAA", "Unidades que um jogo AAA nota 10 vende num mercado neutro.") },
            { "smallPrice", ("Preço sugerido — Pequeno", "Preço de referência por unidade de um jogo Pequeno.") },
            { "mediumPrice", ("Preço sugerido — Médio", "Preço de referência por unidade de um jogo Médio.") },
            { "largePrice", ("Preço sugerido — Grande", "Preço de referência por unidade de um jogo Grande.") },
            { "aaaPrice", ("Preço sugerido — AAA", "Preço de referência por unidade de um jogo AAA.") },
            { "salesScoreExponent", ("Peso da nota nas vendas", "Quanto a nota pesa: vendas = vendas-base × (nota/10) elevado a este número. Maior = notas baixas vendem muito menos.") },
            { "salesFloorShare", ("Piso de vendas", "Fração das vendas-base que todo jogo lançado vende, mesmo com nota baixa. 0,3 = um fracasso ainda vende 30%. Faz o fracasso devolver parte do custo.") },
            { "salesScoreThreshold", ("Nota de virada", "Abaixo desta nota o jogo vende só o piso; acima dela as vendas crescem até o máximo na nota 10. Separa fracasso de acerto.") },
            { "pointsPerPersonPerPhase", ("Pontos por pessoa em cada fase", "Quantos pontos cada pessoa da equipe dá para distribuir em cada fase de produção.") },
            { "productionPhaseCount", ("Número de fases de produção", "Quantas fases o desenvolvimento tem (hoje 5: pré-produção, arte, áudio, tecnologia, conteúdo).") },
            { "smallParamCap", ("Teto por parâmetro — Pequeno", "Máximo de pontos que um único parâmetro (ex.: Gráficos) aceita num jogo Pequeno.") },
            { "mediumParamCap", ("Teto por parâmetro — Médio", "Máximo de pontos por parâmetro num jogo Médio.") },
            { "largeParamCap", ("Teto por parâmetro — Grande", "Máximo de pontos por parâmetro num jogo Grande.") },
            { "aaaParamCap", ("Teto por parâmetro — AAA", "Máximo de pontos por parâmetro num jogo AAA.") },
            { "smallReferenceTeam", ("Equipe ideal — Pequeno", "Tamanho de equipe que entrega 100% do escopo de um Pequeno. Equipe menor entrega menos.") },
            { "mediumReferenceTeam", ("Equipe ideal — Médio", "Tamanho de equipe que entrega 100% do escopo de um Médio.") },
            { "largeReferenceTeam", ("Equipe ideal — Grande", "Tamanho de equipe que entrega 100% do escopo de um Grande.") },
            { "aaaReferenceTeam", ("Equipe ideal — AAA", "Tamanho de equipe que entrega 100% do escopo de um AAA.") },
            { "garageMaxTeam", ("Equipe máxima — Garagem", "Quantas pessoas cabem na Garagem, contando o fundador.") },
            { "indieMaxTeam", ("Equipe máxima — Indie", "Quantas pessoas cabem no Indie, contando o fundador.") },
            { "officeMaxTeam", ("Equipe máxima — Escritório", "Quantas pessoas cabem no Escritório, contando o fundador.") },
            { "studioMaxTeam", ("Equipe máxima — Estúdio", "Quantas pessoas cabem no Estúdio, contando o fundador.") },
            { "scopeCoverageFloor", ("Entrega mínima com equipe pequena", "Mesmo com equipe muito menor que a ideal, o jogo entrega pelo menos esta fração do escopo. 0,15 = 15%.") },
            { "bugBasePerWeek", ("Bugs por semana", "Quantos bugs surgem por semana de desenvolvimento com pouco foco em tecnologia.") },
            { "bugToleranceFree", ("Bugs tolerados", "Até este número de bugs no lançamento, a nota não é afetada.") },
            { "bugScorePenaltyMax", ("Perda máxima de nota por bugs", "Quantos pontos (de 0 a 10) a nota pode perder por excesso de bugs.") },
            { "smallBaseWishlists", ("Wishlists-base — Pequeno", "Wishlists que um jogo Pequeno nota 10 junta. Viram vendas na semana de lançamento.") },
            { "mediumBaseWishlists", ("Wishlists-base — Médio", "Wishlists que um jogo Médio nota 10 junta.") },
            { "largeBaseWishlists", ("Wishlists-base — Grande", "Wishlists que um jogo Grande nota 10 junta.") },
            { "aaaBaseWishlists", ("Wishlists-base — AAA", "Wishlists que um jogo AAA nota 10 junta.") },
            { "salesTailWeeks", ("Semanas vendendo após o lançamento", "Por quantas semanas um jogo continua vendendo depois de lançado.") },
            { "tailRetentionWorst", ("Queda de vendas — nota 0", "Fração das vendas que se mantém de uma semana para a outra num jogo nota 0. Menor = as vendas somem mais rápido.") },
            { "tailRetentionBest", ("Queda de vendas — nota 10", "Fração das vendas que se mantém semana a semana num jogo nota 10. Maior = vende por mais tempo.") },
            { "priceElasticity", ("Sensibilidade ao preço", "Quanto o preço afeta as vendas. Maior = cobrar acima do sugerido derruba mais as vendas.") },
            { "launchBudgetHypePer10k", ("Hype por R$ 10 mil de lançamento", "Hype ganho com os primeiros R$ 10 mil investidos no lançamento (o retorno diminui conforme investe mais).") },
            { "reviewerNoise", ("Variação entre críticos", "Quantos pontos cada crítico pode dar acima ou abaixo da nota real do jogo.") },
            { "bugFixPerWeekBase", ("Bugs corrigidos por semana (base)", "Bugs que a empresa corrige por semana quando o jogo está pronto e esperando lançamento.") },
            { "bugFixPerWeekPerPerson", ("Bugs corrigidos por pessoa", "Bugs a mais corrigidos por semana para cada pessoa da equipe.") },
            { "waitingHypeLossPerWeek", ("Hype perdido esperando lançar", "Hype que se perde por semana com o jogo pronto e não lançado. 0,03 = 3%.") },
            { "crunchDurationMultiplier", ("Duração da fase em crunch", "Multiplica a duração da fase quando o crunch está ligado. 0,75 = a fase dura 75% do normal.") },
            { "crunchBugMultiplier", ("Bugs em crunch", "Multiplica os bugs gerados durante crunch. 1,6 = 60% mais bugs.") },
            { "crunchBurnoutPerWeek", ("Desgaste por semana de crunch", "Quanto de burnout a equipe acumula por semana de crunch.") },

            // ---------------- Tempo, inflação e fim de run
            { "secondsPerWeek", ("Duração de uma semana (segundos)", "Quantos segundos reais dura uma semana de jogo na velocidade normal. Nas velocidades rápidas é dividido.") },
            { "weeklyInflationPercent", ("Inflação por semana (%)", "Quanto todos os custos sobem a cada semana da run. 0 = sem inflação; 0,2 dá cerca de 11% ao ano.") },
            { "runCostVariationPercent", ("Variação de custo entre runs (%)", "Cada run sorteia um custo entre -X% e +X% do normal. 10 = uma run pode ser até 10% mais cara ou mais barata.") },
            { "bankruptcyGraceWeeks", ("Semanas no vermelho até falir", "Quantas semanas seguidas a empresa pode ficar com caixa negativo antes da falência.") },
            { "ipoPremium", ("Multiplicador do IPO", "Quanto o IPO paga em relação ao valuation. 1,5 = 150% do valor de mercado.") },

            // ---------------- Legado e árvore
            { "plForReachingIndie", ("Legado por chegar ao Indie", "Pontos de Legado ganhos ao alcançar a fase Indie.") },
            { "plForReachingOffice", ("Legado por chegar ao Escritório", "Pontos de Legado ganhos ao alcançar o Escritório.") },
            { "plForReachingStudio", ("Legado por chegar ao Estúdio", "Pontos de Legado ganhos ao alcançar o Estúdio.") },
            { "plPerGameAboveScoreSeven", ("Legado por jogo acima de 7", "Pontos de Legado por cada jogo lançado com nota acima de 7.") },
            { "plForFranchiseWithThreeOrMoreGames", ("Legado por franquia (3+ jogos)", "Pontos de Legado por ter uma franquia com 3 ou mais jogos.") },
            { "plForRunWithoutMajorCrisis", ("Legado por run sem crise grave", "Pontos de Legado por terminar a run sem nenhuma crise grave.") },
            { "plForValuationAboveOneMillion", ("Legado por valuation acima de 1 milhão", "Pontos de Legado ao passar de R$ 1 milhão de valor de mercado.") },
            { "plForValuationAboveTenMillion", ("Legado por valuation acima de 10 milhões", "Pontos de Legado ao passar de R$ 10 milhões de valor de mercado.") },
            { "incrementalNodeFirstCost", ("Custo da 1ª compra de um nó repetível", "Preço em dinheiro pessoal da primeira compra de um nó que pode ser comprado várias vezes.") },
            { "incrementalNodeCostMultiplier", ("Aumento de preço a cada compra", "Multiplica o preço do nó repetível a cada nova compra. 2 = dobra.") },
            { "uniqueNodeMinCostPL", ("Custo mínimo de nó único (Legado)", "Menor preço, em Pontos de Legado, de um nó que só se compra uma vez.") },
            { "uniqueNodeMaxCostPL", ("Custo máximo de nó único (Legado)", "Maior preço, em Pontos de Legado, de um nó que só se compra uma vez.") },
            { "legacyResetCost", ("Custo para refazer a árvore", "Dinheiro pessoal cobrado para desfazer as escolhas da árvore de habilidades.") },

            // ---------------- Marketing e hype
            { "passiveHypeGainPerWeek", ("Hype ganho sozinho por semana", "Hype que o jogo ganha por semana de desenvolvimento sem nenhuma campanha. 0,015 = 1,5 ponto.") },
            { "hypeDecayPerWeek", ("Hype que esfria por semana", "Hype perdido por semana se nada alimentar o interesse.") },
            { "announcementCost", ("Anúncio — custo", "Quanto custa a campanha de anúncio do jogo.") },
            { "announcementHype", ("Anúncio — hype", "Hype ganho com o anúncio. 0,15 = 15 pontos de hype.") },
            { "wishlistCampaignCost", ("Campanha de wishlist — custo", "Quanto custa a campanha de wishlist.") },
            { "wishlistCampaignHype", ("Campanha de wishlist — hype", "Hype ganho com a campanha de wishlist.") },
            { "wishlistCampaignBurst", ("Campanha de wishlist — wishlists extras", "Quantas semanas de wishlists a campanha traz de uma vez.") },
            { "demoCost", ("Demo — custo", "Quanto custa lançar uma demo.") },
            { "demoHype", ("Demo — hype", "Hype ganho com a demo.") },
            { "gameShowCost", ("Feira de games — custo", "Quanto custa levar o jogo a uma feira.") },
            { "gameShowHype", ("Feira de games — hype", "Hype ganho na feira.") },
            { "devlogCost", ("Devlog — custo", "Quanto custa publicar um devlog.") },
            { "devlogHype", ("Devlog — hype", "Hype ganho com o devlog.") },
            { "pressKitCost", ("Kit de imprensa — custo", "Quanto custa montar o kit de imprensa.") },
            { "pressKitHype", ("Kit de imprensa — hype", "Hype ganho com o kit de imprensa.") },
            { "closedBetaCost", ("Beta fechado — custo", "Quanto custa rodar um beta fechado.") },
            { "closedBetaHype", ("Beta fechado — hype", "Hype ganho com o beta fechado.") },
            { "closedBetaBugReduction", ("Beta fechado — bugs eliminados", "Fração dos bugs conhecidos que o beta fechado elimina. 0,3 = 30%.") },
            { "influencersCost", ("Influenciadores — custo", "Quanto custa a campanha com influenciadores.") },
            { "influencersHype", ("Influenciadores — hype", "Hype ganho com influenciadores.") },

            // ---------------- Nota dos jogos
            { "sliderProfileWeight", ("Peso da distribuição de pontos", "Quanto da nota vem de acertar a mistura de pontos que o gênero pede. Os 6 pesos devem somar 1.") },
            { "themeGenreCompatibilityWeight", ("Peso da combinação gênero × tema", "Quanto da nota vem de o tema combinar com o gênero.") },
            { "teamLevelWeight", ("Peso do nível da equipe", "Quanto da nota vem da habilidade da equipe.") },
            { "engineBudgetUsageWeight", ("Peso da engine", "Quanto da nota vem de ter Engine Budget suficiente para o tamanho do jogo.") },
            { "marketTrendWeight", ("Peso da tendência de mercado", "Quanto da nota vem de o gênero/tema estar em alta.") },
            { "developmentEventsWeight", ("Peso dos eventos", "Quanto da nota vem do que aconteceu durante o desenvolvimento.") },
            { "neutralThemeGenreScore", ("Nota neutra — combinação", "Nota usada para a combinação gênero × tema quando não há dado.") },
            { "neutralEngineBudgetScore", ("Nota neutra — engine", "Nota usada para a engine quando não há dado.") },
            { "neutralMarketTrendScore", ("Nota neutra — mercado", "Nota usada para o mercado quando não há dado.") },
            { "neutralDevelopmentEventsScore", ("Nota neutra — eventos", "Nota usada para os eventos quando não há dado.") },
            { "weakHypeUpperBound", ("Hype fraco vai até", "Abaixo deste hype (0 a 100) o lançamento é considerado fraco.") },
            { "mediumHypeUpperBound", ("Hype médio vai até", "Até este hype o lançamento é médio.") },
            { "goodHypeUpperBound", ("Hype bom vai até", "Até este hype é bom; acima, excelente.") },
            { "salesPer100Wishlists", ("Vendas a cada 100 wishlists", "Quantas vendas cada 100 wishlists geram na semana de lançamento.") },

            // ---------------- Pesquisa
            { "basicResearchCostPP", ("Pesquisa básica — custo em PP", "Pontos de Pesquisa para começar uma pesquisa básica.") },
            { "intermediateResearchCostPP", ("Pesquisa intermediária — custo em PP", "Pontos de Pesquisa para uma pesquisa intermediária.") },
            { "advancedResearchCostPP", ("Pesquisa avançada — custo em PP", "Pontos de Pesquisa para uma pesquisa avançada.") },
            { "basicResearchWeeklyCost", ("Pesquisa básica — custo por semana", "Dinheiro gasto por semana enquanto uma pesquisa básica roda.") },
            { "intermediateResearchWeeklyCost", ("Pesquisa intermediária — custo por semana", "Dinheiro gasto por semana numa pesquisa intermediária.") },
            { "advancedResearchWeeklyCost", ("Pesquisa avançada — custo por semana", "Dinheiro gasto por semana numa pesquisa avançada.") },
            { "ppPerDevelopmentWeek", ("PP por semana desenvolvendo", "Pontos de Pesquisa ganhos a cada semana com um jogo em produção.") },
            { "ppPerGameLaunched", ("PP por jogo lançado", "Pontos de Pesquisa ganhos ao lançar um jogo.") },
            { "ppPerResearchCompleted", ("PP por pesquisa concluída", "Pontos de Pesquisa ganhos ao terminar uma pesquisa.") },
            { "researchWeeksBasic", ("Duração — pesquisa básica", "Semanas para concluir uma pesquisa básica.") },
            { "researchWeeksIntermediate", ("Duração — pesquisa intermediária", "Semanas para concluir uma pesquisa intermediária.") },
            { "researchWeeksAdvanced", ("Duração — pesquisa avançada", "Semanas para concluir uma pesquisa avançada.") },

            // ---------------- Conteúdo (gênero, tema, plataforma)
            { "displayNamePt", ("Nome mostrado no jogo", "Como este item aparece para o jogador.") },
            { "gameDesign", ("Game Design", "Quanto este gênero pede de Game Design (pré-produção), por tamanho de jogo.") },
            { "narrative", ("Narrativa", "Quanto pede de Narrativa (pré-produção).") },
            { "artDirection", ("Direção de Arte", "Quanto pede de Direção de Arte (pré-produção).") },
            { "gameFeel", ("Game Feel", "Quanto pede de Game Feel (pré-produção).") },
            { "scope", ("Escopo", "Quanto pede de Escopo (pré-produção).") },
            { "graphics", ("Gráficos", "Quanto pede de Gráficos (fase Arte e Visual).") },
            { "animation", ("Animação", "Quanto pede de Animação (fase Arte e Visual).") },
            { "userInterface", ("Interface", "Quanto pede de Interface (fase Arte e Visual).") },
            { "vfx", ("Efeitos visuais", "Quanto pede de Efeitos visuais (fase Arte e Visual).") },
            { "soundtrack", ("Trilha sonora", "Quanto pede de Trilha sonora (fase Áudio).") },
            { "soundDesign", ("Efeitos sonoros", "Quanto pede de Efeitos sonoros (fase Áudio).") },
            { "voiceActing", ("Dublagem", "Quanto pede de Dublagem (fase Áudio).") },
            { "programming", ("Programação", "Quanto pede de Programação (fase Tecnologia).") },
            { "performanceOptimization", ("Otimização", "Quanto pede de Otimização (fase Tecnologia).") },
            { "stabilityTesting", ("Testes e estabilidade", "Quanto pede de Testes (fase Tecnologia).") },
            { "gameplay", ("Jogabilidade", "Quanto pede de Jogabilidade (fase Conteúdo e Design).") },
            { "content", ("Conteúdo", "Quanto pede de Conteúdo (fase Conteúdo e Design).") },
            { "implementedNarrative", ("Narrativa no jogo", "Quanto pede de Narrativa implementada (fase Conteúdo e Design).") },
            { "balancing", ("Balanceamento", "Quanto pede de Balanceamento (fase Conteúdo e Design).") },
            { "small", ("Jogo Pequeno", "Importância deste parâmetro num jogo Pequeno.") },
            { "medium", ("Jogo Médio", "Importância num jogo Médio.") },
            { "large", ("Jogo Grande", "Importância num jogo Grande.") },
            { "aaa", ("Jogo AAA", "Importância num jogo AAA.") },
            { "category", ("Categoria", "Tipo do item, usado para agrupar.") },
            { "genreCompatibilities", ("Combinação com cada gênero", "O quanto este tema combina com cada gênero. Afeta nota, hype e vendas.") },
            { "genreKey", ("Gênero", "Identificador do gênero.") },
            { "weight", ("Combinação", "De muito ruim (---) a excelente (+++).") },
            { "manufacturer", ("Fabricante", "Empresa fictícia que fabrica a plataforma (só texto).") },
            { "releaseYear", ("Ano de lançamento", "Ano da run em que a plataforma fica disponível (a run começa no Ano 1).") },
            { "licenseCost", ("Licença", "Quanto custa lançar um jogo nesta plataforma.") },
            { "initialPopularity", ("Popularidade no lançamento", "De 0 a 1: quão popular a plataforma é assim que sai. Mais popular = mais vendas.") },
            { "peakPopularity", ("Popularidade no auge", "De 0 a 1: popularidade no pico da vida da plataforma.") },
            { "lifespanYears", ("Anos de vida", "Por quantos anos a plataforma continua relevante.") },
        };

        private static readonly Dictionary<string, string> EnumValues = new Dictionary<string, string>
        {
            { "WeightLevel.StronglyNegative", "Muito ruim (---)" },
            { "WeightLevel.Negative", "Ruim (--)" },
            { "WeightLevel.SlightlyNegative", "Fraco (-)" },
            { "WeightLevel.Neutral", "Neutro (~)" },
            { "WeightLevel.SlightlyPositive", "Bom (+)" },
            { "WeightLevel.Positive", "Ótimo (++)" },
            { "WeightLevel.StronglyPositive", "Excelente (+++)" },
            { "ThemeCategory.FictionFantasy", "Ficção e fantasia" },
            { "ThemeCategory.HorrorSuspense", "Terror e suspense" },
            { "ThemeCategory.LifestyleDaily", "Dia a dia" },
            { "ThemeCategory.Historical", "Histórico" },
            { "ThemeCategory.SportsVehicles", "Esportes e veículos" },
            { "ThemeCategory.NatureExploration", "Natureza e exploração" },
            { "ThemeCategory.MetaPop", "Cultura pop" },
            { "PlatformCategory.PC", "PC" },
            { "PlatformCategory.Console", "Console de mesa" },
            { "PlatformCategory.Handheld", "Portátil" },
            { "PlatformCategory.Mobile", "Celular" },
            { "PlatformCategory.VR", "Realidade virtual" },
        };

        /// <summary>Título do grupo de balanceamento (null se não houver tradução).</summary>
        public static string Group(string typeKey) => Groups.TryGetValue(typeKey, out var title) ? title : null;

        /// <summary>Nome e explicação do campo (false se não houver tradução).</summary>
        public static bool TryField(string fieldName, out string label, out string help)
        {
            if (Fields.TryGetValue(fieldName, out var text)) { label = text.label; help = text.help; return true; }
            label = help = null;
            return false;
        }

        /// <summary>Nome em português de um valor de enum (o nome do código se não houver).</summary>
        public static string EnumValue(Type type, string name) =>
            EnumValues.TryGetValue(type.Name + "." + name, out var label) ? label : name;
    }
}
