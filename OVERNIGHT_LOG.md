# OVERNIGHT_LOG.md — Sessão autônoma (2026-10-31 → 11-01)

Log de uma sessão de trabalho autônoma do agente de IA. Objetivo pedido pelo dev: **acordar com uma versão inicial jogável (vertical slice)**, mais o início do pivô **2D → 3D isométrico**. O dev autorizou assumir decisões provisórias para não travar, registrando-as aqui, e concedeu controle da tela (Unity) para verificar compilação e testar.

## ✅ Resultado principal: o LOOP CORE ESTÁ JOGÁVEL E TESTADO

Testei de ponta a ponta dentro do editor (Play Mode), sem erros no Console:

1. Começa como empresa na fase **Garage** com caixa inicial (**$50.000**).
2. **Criar um jogo**: nome + gênero (ActionRPG/Puzzle/Shooter/Simulation) + tamanho (Small/Medium/Large/AAA) + distribuir foco da equipe em 5 áreas (Design/Arte/Áudio/Tech/Conteúdo).
3. **Produção** avança por semanas (o tempo corre; dá pra pausar e acelerar), cobrando o **custo semanal da equipe**.
4. **Lançamento automático** ao fim do prazo → calcula **nota de qualidade (0–10)** pela aderência da alocação ao perfil do gênero → gera **vendas e receita**.
5. A receita entra no caixa, o **valuation sobe** e a empresa **muda de fase** ao cruzar os thresholds (testado: **Garage → Indie**).
6. Volta ao formulário para o **próximo projeto** — progressão financeira sentida entre ciclos.

**Exemplo real do teste:** ActionRPG, Small, foco alto em Conteúdo(8)/Arte(7) → **Nota 7,0/10**, **Receita $129.030**, caixa $163.030, valuation $381.575, fase virou Indie.

> É o critério de sucesso da Fase 2 do `agents.md`. Veja o print em `OVERNIGHT_SHOT.png` (ao lado deste arquivo), se anexado.

## ⚠️ Limitação da sessão (contexto)

O agente não edita cenas/prefabs pela GUI "na mão", mas teve **controle da tela do Unity** para: verificar o Console a cada compilação, rodar o menu de setup e testar no Play. Mesmo assim, modelagem 3D e montagem artística de cena continuam sendo trabalho seu no editor.

---

## Como chegar ao Play (setup — já foi rodado, mas aqui está para reproduzir)

1. Abrir o projeto no Unity 6000.3.10f1.
2. Menu **Tools → GameLike → Setup → ★ Do Everything (recomendado)**. Isso cria:
   - BalanceAssets em `Assets/Resources/Balance/` (Economy, QualityFormula, GameLoop);
   - Gêneros de exemplo em `Assets/Resources/Genres/` (ActionRPG, Puzzle, Shooter, Simulation);
   - a cena **`Assets/Scenes/GameplaySlice.unity`** com o GameObject `[GameLike]` (managers do loop) — e já a deixa aberta.
3. Apertar **Play**. (Dica: deixe o zoom da Game view em **1x** para os painéis não cortarem; se quiser a Game view grande, passe o mouse sobre ela e aperte **Shift+Space**.)

Jogar: escolha gênero/tamanho, ajuste o foco com os botões +/−, clique **Iniciar desenvolvimento**, e use **Velocidade/Pausar** (painel esquerdo) para correr o tempo até o lançamento.

---

## Arquitetura do que foi adicionado (respeitando as camadas do CLAUDE.md)

- **Estado** (`Assets/Scripts/State/Run/`): `CompanyState`, `RunState`, `ProjectState` (+ enums `DevArea`, `ProjectStatus`).
- **Systems** (`Assets/Scripts/Systems/`): `GameController` (composition root), `ProjectManager` (ciclo do projeto), `QualityCalculator` (nota), `EconomyManager` (reescrito para operar sobre `CompanyState`).
- **Data/Balance**: `GameLoopBalanceSO` (tunáveis do loop) + `startingCash`/campos lidos dos SOs existentes.
- **UI** (`Assets/Scripts/UI/Slice/`): `SliceHarnessUI` — painel IMGUI de teste (ver decisão 3).
- **Editor** (`Assets/Scripts/Editor/Setup/`): `GameLikeSliceSetup` (menu de setup automatizado).
- Comunicação via EventBus mantida: `GameLaunchedEvent`, `PhaseChangedEvent`, `MoneyChangedEvent`, `WeekTickEvent`.

---

## Decisões provisórias assumidas (CONFERIR com o dev / GDD)

1. **Pivô 3D limitado a docs + dados nesta sessão.** O diorama 3D do escritório (modelagem, cena de autoria, câmera calibrada) exige arte e trabalho no editor com você. Foco da noite = loop jogável, que é agnóstico de 2D/3D. Docs já reescritos para a direção 3D low-poly (`ART_STYLE.md`, `CLAUDE.md`, `agents.md`, `README.md`). O LevelBuilder 2D antigo foi **mantido intacto** (compila) — será reformulado para 3D numa sessão com você no editor.
2. **GDD é externo (não acessado).** Todos os números (caixa inicial, salários, semanas por tamanho, unidades-base e preços de venda, curva nota→vendas) são **palpites iniciais** em `GameLoopBalanceSO`/`EconomyBalanceSO`, fáceis de ajustar no Inspector. Conferir contra o GDD seção 19.
3. **UI do slice = IMGUI (`OnGUI`), não UI Toolkit.** Escolha pragmática para um harness de teste confiável e 100% operável por cliques. O CLAUDE.md manda UI Toolkit para a UI de produção (Fase 5) — isto é um painel de desenvolvimento provisório, claramente marcado, a ser substituído.
4. **Fórmula de qualidade simplificada.** Dos 6 componentes do GDD (19.7), só existem gênero e equipe hoje; os demais usam um valor neutro. O componente de gênero agrega os 19 parâmetros do `GenreSO` em 5 áreas. Pesos lidos do `QualityFormulaBalanceSO`. Ver XML docs em `QualityCalculator`.
5. **Gêneros de exemplo criados direto em Resources** (via script de editor), não pelo pipeline JSON→importer. Atalho do slice para garantir gêneros em runtime; o pipeline de conteúdo oficial segue válido para o trabalho real de conteúdo.
6. **Valuation provisória** = caixa + 1,5× receita acumulada + bônus por jogo lançado (placeholder; GDD seção 4 define a real).

---

## Dúvidas guardadas para o dev

1. **Quer que eu siga de fato para o 3D** (câmera ortográfica iso + cena 3D low-poly de placeholder com cubos) nesta ou na próxima sessão, ou prefere expandir o loop (P&D, funcionários, mercado) primeiro? O loop atual não depende do visual.
2. **Os números provisórios** (dificuldade econômica, duração de projetos, escala de vendas) — quer calibrar junto, ou colo os do GDD quando você me passar a seção 19?
3. **Perfis de gênero**: os 4 gêneros de exemplo têm perfis que inventei. Substituir pelos perfis reais do GDD (seção 18.5) via o pipeline JSON quando você quiser.
4. O harness IMGUI serve por agora ou você quer que eu já comece a UI Toolkit de verdade?

---

## Próximos passos que pretendo encaminhar ainda nesta sessão (sem quebrar o que funciona)

- [x] Testes EditMode para `QualityCalculator` (lógica determinística — pedido pelo CLAUDE.md).
- [x] Pequenos polimentos no harness (mostrar unidades vendidas, dica do perfil ideal do gênero, fim de run "vender empresa").
- [x] Cena 3D de demonstração com câmera ortográfica isométrica, isolada, para materializar a direção visual.

---

## Atualização (madrugada — após o loop base)

Tudo abaixo foi compilado e verificado no editor (Console 0 erros):

1. **Testes EditMode da `QualityCalculator`** (`Assets/Scripts/Tests/EditMode/Systems/QualityCalculatorTests.cs`): perfil ideal soma 1, gênero neutro dá perfil uniforme, nota fica em 0..10, e alocação casada pontua mais que descasada. **Rodei no Test Runner: 10/10 verdes.**
2. **Polimento do loop:** o harness agora mostra **unidades vendidas** no histórico e a **"Aderência ao gênero: NN%"** ao vivo enquanto você ajusta o foco; adicionei **"Vender empresa (IPO)"** que encerra a run com uma tela de resumo (preço de venda, jogos, melhor nota) — fecho de ciclo roguelite. Testado no Play.
   - Observação: ao vender, o Console mostra uma linha **vermelha** `[Acquisition] Company sold via IPO...`. **Não é erro** — `CompanySoldEvent` tem severidade `Critical` e o `EventBusLogger` do projeto renderiza eventos críticos como `LogError` (por design, para visibilidade). Comportamento esperado.
   - Corrigi a transição de fase para **avançar em cascata** (um lançamento muito lucrativo pode cruzar mais de um threshold de uma vez).
3. **Cena 3D isométrica de demonstração:** `Assets/Scenes/IsoDemo.unity`, gerada por **Tools → GameLike → Setup → 4 - Build 3D Iso Demo**. Câmera ortográfica (30°/45°, size 7.5), luz direcional suave, e um mini-escritório low-poly feito de primitivas com materiais de cor chapada (chão, paredes, mesas, monitores, "devs", plantinhas). É só um **alvo visual** da direção aprovada — não é o escritório final nem usa o LevelBuilder. Veja em `OVERNIGHT_SHOT_ISO.*` se anexado.

### Observações de balanceamento (provisório, conferir no GDD)
- Projeto **Medium** custa ~56k de desenvolvimento, mas o caixa inicial é 50k → o caixa pode ficar levemente negativo no meio do dev antes da receita entrar. Não quebra nada, mas sugere subir o caixa inicial ou baixar o custo/duração. Ajuste em `GameLoopBalanceSO`/`EconomyBalanceSO` no Inspector.
- As vendas parecem generosas (um ActionRPG Medium nota 7,3 rendeu ~$833k). Calibrar `salesScoreExponent`, unidades-base e preços.

### Estado dos arquivos ao fim desta sessão
- A cena aberta no editor pode ser a `IsoDemo` (3D). Para **jogar o loop**, abra `Assets/Scenes/GameplaySlice.unity` e aperte Play. Para **ver o 3D**, abra `Assets/Scenes/IsoDemo.unity`.
- Nada foi commitado no git (como combinado). Os arquivos estão todos em disco.
