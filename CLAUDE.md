# CLAUDE.md — The Game Like

Este arquivo orienta o Claude Code (e qualquer agente de IA) a trabalhar neste projeto. Leia-o por completo antes de gerar ou modificar código.

## O que é o The Game Like

> **Nota sobre nomes:** o jogo se chama oficialmente **The Game Like**. Internamente, todo identificador de código (namespaces, assemblies, pastas) continua usando o codename técnico `GameLike` (sem espaço, sem artigo) — `GameLike.Core`, `GameLike.Systems`, etc. Isso é intencional: o nome de marca/loja e o codename de engenharia não precisam coincidir, e renomear identificadores de código não traz benefício real. O Bundle Identifier (`com.placeholderstudio.gamelike`) e o Company Name ainda são placeholder (ver seção "Player Settings" abaixo) e podem ser ajustados quando o estúdio/empresa for definido — o nome do jogo em si já está fechado.

The Game Like é um **tycoon roguelite** de gestão de uma desenvolvedora de jogos, feito em **Unity 6000.3 (C#)** por um **desenvolvedor solo**. O jogador funda uma game dev, constrói o valor de mercado (valuation) da empresa, e decide quando vendê-la. O dinheiro da venda vira progressão permanente, e o fundador "volta no tempo" para recomeçar mais forte — as empresas de runs anteriores viram rivais.

O design completo está no **Game Design Document (GDD v1.0)**, a fonte da verdade para todas as regras de jogo. Quando uma decisão de gameplay não estiver clara no código, consulte o GDD antes de assumir.

## Princípios inegociáveis

1. **Código limpo, modular e tipado.** Priorize tipagem forte sempre.
2. **Nomes em inglês.** Variáveis, métodos, classes, funções — tudo em inglês. Comentários e docs podem ser em português.
3. **Sem comentários poluindo o código.** Apenas XML docs em assinaturas de classes, métodos e funções públicas, ou comentários estritamente necessários para manutenção.
4. **Sem bibliotecas externas sem autorização explícita.** Use apenas Unity e .NET base.
5. **Extensibilidade primeiro.** Todo conteúdo de jogo (gêneros, temas, eventos, etc.) é dado configurável via ScriptableObjects, nunca hardcoded. O GDD repete "extensível" deliberadamente — respeite isso.

## Arquitetura

O projeto usa uma abordagem **híbrida** em três camadas. Entender essa separação é essencial.

### Camada 1 — Definição (ScriptableObjects, imutável em runtime)
Todo o conteúdo do GDD vira assets `.asset`: `GenreSO`, `SubgenreSO`, `ThemeSO`, `PlatformSO`, `EngineModuleSO`, `TraitSO`, `NarrativeEventSO`, `ToolSO`, `ConsoleSO`. São criados e editados pelo editor do Unity. **Nunca mudam durante o jogo.** Os agentes de conteúdo preenchem as tabelas de pesos aqui.

### Camada 2 — Estado (classes C# puras, `[Serializable]`, mutável)
O estado vivo da run: `CompanyState`, `ProjectState`, `EmployeeInstance`, `RunState`, `MetaProgressionState`. São classes serializáveis normais, **não** ScriptableObjects, porque mudam constantemente e precisam ser salvas/carregadas por slot. Uma instância de estado referencia os ScriptableObjects de definição mas guarda os valores mutáveis (XP atual, humor, burnout, etc.).

**Esta separação definição/estado é a decisão mais importante da arquitetura.** Nunca grave estado mutável de runtime dentro de um ScriptableObject de definição.

### Camada 3 — Sistemas (managers + EventBus)
A lógica que opera sobre o estado. Managers são `MonoBehaviour` que possuem um domínio coeso (`TimeManager`, `EconomyManager`, etc.). Eles **se comunicam via EventBus, não se chamam diretamente** nos pontos de alto fan-out.

## Localização (i18n)

O jogo nasce multi-idioma, mesmo preenchendo só **pt-BR** no conteúdo inicial. Todo texto visível ao jogador — nomes e descrições de gênero/tema/console, textos de eventos narrativos, UI, tooltips — é referenciado por **chave de localização**, nunca como string literal direta em ScriptableObject ou em código.

- **Pacote autorizado:** `com.unity.localization` (pacote oficial Unity, **única exceção explícita** à regra de "sem bibliotecas externas sem autorização" — é parte do ecossistema Unity, não uma lib de terceiro, e essa autorização está registrada aqui formalmente).
- Campos de texto em `DefinitionAsset` e similares usam `LocalizedString` em vez de `string` sempre que o conteúdo é visível ao jogador. Campos puramente técnicos (Id, nomes de enum internos) continuam `string` normal.
- Idiomas planejados: pt-BR (inicial), en-US (lançamento Steam). Estrutura pronta para adicionar mais sem refatoração.

## IDs estáveis em ScriptableObjects de definição

Todo ScriptableObject de definição (`GenreSO`, `ThemeSO`, `ConsoleSO`, etc.) herda de `DefinitionAsset` (`Data/Base/DefinitionAsset.cs`), que expõe um campo `Id` (string, GUID) **gerado automaticamente** na criação do asset e **imutável** depois disso.

- **Saves e código referenciam conteúdo pelo `Id`, nunca pelo nome do arquivo/asset.** Renomear ou mover um `.asset` no editor nunca invalida um save existente, porque o `Id` não depende do nome.
- A geração é automática via `Editor/Data/DefinitionAssetIdAssigner.cs` (`AssetPostprocessor`), que roda uma única vez por asset, na importação. Nenhum código de gameplay ou agente de conteúdo deve gerar ou editar um `Id` manualmente.
- O assembly `GameLike.Editor` só compila no editor (`"includePlatforms": ["Editor"]` no `.asmdef`), então essa lógica de tooling nunca entra em build de release.
- Convenção de nome de arquivo (sem prefixo, pasta por tipo): `Data/Genres/ActionRPG.asset`, `Data/Themes/Cyberpunk.asset`, `Data/Consoles/PlayBox2.asset`.

## Sistema de layout de escritório (RoomBlock + Furniture)

O escritório (GDD seção 3.4, evolução visual por fase) é construído como uma **ferramenta de produção sua**, não uma mecânica que o jogador usa — você monta o layout de cada fase uma vez, manualmente, no editor do Unity. Não é gerado por agente nem editável em runtime pelo jogador.

- **Unidade de tile:** 32×32 pixels, calibrada a partir da referência visual de estilo (ver `ART_STYLE.md`).
- **RoomBlock:** unidade espacial de **7×7 tiles fixos** (`RoomBlockSO.SizeInTiles`). Blocões se posicionam num grid (em unidades de blocão, não de tile) e formam o layout retangular maior da sala. Cada `RoomBlockSO` é só a "skin" visual (paleta de piso/parede via Tile Palette do Unity) — não contém posição nem mobília.
- **Conexão entre blocões:** a parede entre dois blocões adjacentes é `aberta` (passagem/porta) ou `fechada` (parede sólida) — representado por `BlockConnection` dentro de `OfficeLayoutSO`. É a única coisa que varia na fronteira; o skin do blocão em si não.
- **Mobília:** cada peça é um **GameObject/sprite independente posicionado livremente sobre o tilemap**, não pintada como tile — escolha deliberada para que trocar uma peça por um upgrade seja só substituir a referência ao `FurnitureDefinitionSO`, sem repintar nada.
- **Composição visual de mobília (base + estofado + cor):** `FurnitureDefinitionSO.VisualLayers` é uma pilha de `FurnitureVisualLayer` (sprite + flag `tintable`). Camadas marcadas como tintable são recoloridas via tint em runtime — uma cadeira com 3 variantes de cor de estofado usa **um sprite só**, não três. Reduz drasticamente o volume de pixel art necessário por peça.
- **Upgrade é puramente mecânico-por-variante:** cada `FurnitureDefinitionSO` carrega um único `FurnitureEffect` (categoria + magnitude). Trocar de cadeira nunca afeta nada fora do efeito declarado naquela variante — sem efeitos colaterais implícitos.
- **`OfficeLayoutSO`** é o asset final por fase (Garage/Indie/Office/Studio): array de blocões posicionados + conexões + mobília posicionada. **Pendência de ferramenta:** ainda não existe a cena de edição Tilemap nem o script "bake cena → asset" — por ora o `OfficeLayoutSO` só tem a estrutura de dados pronta para receber essa ferramenta. Ver `agents.md`, Fase 1, para o item de roadmap.

## UI (UI Toolkit)

O jogo usa **UI Toolkit** (UXML + USS), não uGUI/Canvas tradicional. Razão: o jogo é HUD denso e orientado a dados — 8 painéis (GDD seção 16.2) com listas dinâmicas (funcionários, projetos, histórico), e UI Toolkit performa melhor que uGUI nesse perfil, além de separar estrutura (UXML) de estilo (USS) de forma parecida com HTML/CSS, o que facilita ajustar visual sem tocar em lógica.

- **Estrutura:** `Assets/UI/Common/` para componentes reutilizáveis entre telas (botões, cards, tooltips); `Assets/UI/Screens/` para UXML/USS específicos de cada tela/painel. Código de UI (controllers que conectam UXML a dados) vive em `Assets/Scripts/UI/` (assembly `GameLike.UI`).
- **Posição na cadeia de dependência:** `Core ← Events ← Data ← State ← Systems ← UI`. UI é a camada mais externa — consome tudo, nada depende dela. UI nunca contém lógica de gameplay; ela lê estado e dispara intenções (que se tornam chamadas a managers ou eventos), nunca calcula regra de jogo por conta própria.
- **Ainda não há nenhuma tela construída** — isso é Fase 5 do roadmap (`agents.md`). Esta seção documenta a decisão e a convenção de pastas antes da implementação, para quando chegar a hora não haver ambiguidade de onde as coisas vão.

## Balanceamento (BalanceAsset + BalanceProvider)

Os valores numéricos de balanceamento do GDD (Seção 19) — salários, thresholds de valuation, custos de P&D, Engine Budget, Pontos de Legado, pesos da fórmula de qualidade — vivem em **vários `ScriptableObject` pequenos por domínio** (`EconomyBalanceSO`, `ResearchBalanceSO`, `EngineBalanceSO`, `LegacyProgressionBalanceSO`, `QualityFormulaBalanceSO`), todos derivando de `BalanceAsset` (`Data/Balance/BalanceAsset.cs`).

- **Por que vários SOs pequenos, não constantes em código ou um SO gigante único:** editar um valor no Inspector tem efeito imediato (sem recompilar), inclusive em Play Mode — é o equivalente prático a hot-reload para esses números, que você vai ajustar com frequência durante playtesting. Quebrar por domínio em vez de um arquivo único evita rolar uma tela gigante de Inspector procurando um campo.
- **Existe exatamente uma instância de cada tipo de `BalanceAsset` no projeto.** Eles vivem em `Assets/Resources/Balance/`. Managers nunca recebem a referência por Inspector manual — pedem pelo tipo via `BalanceProvider.Get<EconomyBalanceSO>()`.
- **Regra de ouro:** managers leem o valor do `BalanceAsset` **a cada uso**, nunca copiam para um campo local esperando que fique atualizado. Copiar invalida o ganho de edição em tempo real — é o erro mais fácil de cometer aqui. Ver `EconomyManager.OnWeekTick` como referência correta.
- **Tabelas associativas** (compatibilidade Gênero×Tema, perfil de gênero por tamanho) **não são balanceamento simples** — continuam morando nos próprios `DefinitionAsset` (`GenreSO`, `ThemeSO`, etc.), seguindo o pipeline de conteúdo descrito acima. `BalanceAsset` é só para valores soltos de configuração de sistema.
- **Fórmulas compostas** (a equação da nota de qualidade, a curva de probabilidade de M&A) são lógica e ficam em código. Apenas os *pesos* dessa lógica (os 40/20/15/10/10/5% da fórmula de qualidade) são configuráveis via `QualityFormulaBalanceSO` — a estrutura da equação em si não.
- `Resources.LoadAll` é usado deliberadamente em `BalanceProvider` para descobrir os assets sem referência manual — aceitável aqui porque são poucos SOs de configuração, não um padrão a generalizar para conteúdo (gêneros, temas, etc. não usam `Resources`).
- Validação de consistência (ex: soma dos pesos da fórmula de qualidade = 1.0) acontece em dois níveis: visualmente no Inspector (`QualityFormulaBalanceEditor`) e via teste automatizado (`QualityFormulaBalanceTests`) — qualquer um que edite os defaults e quebrar a soma é avisado em ambos os lugares.

## Política GDD-first (sincronização GDD ↔ código)

O GDD é sempre editado **antes** do código quando uma regra de gameplay muda — nunca o contrário. Fluxo obrigatório para qualquer mudança de design, mesmo pequena:

1. Edite a seção relevante do GDD primeiro.
2. Só então implemente/ajuste o código correspondente.
3. Se a mudança afeta o schema de save (GDD seção 19 ou estruturas de estado), siga a política de versionamento descrita acima.

**Nunca** ajuste uma regra de balanceamento ou mecânica diretamente no código/config sem que o GDD já reflita a mudança. Se o Claude Code ou um agente perceber uma inconsistência entre o GDD e o código durante a implementação, a ação correta é **parar e perguntar ao desenvolvedor**, nunca silenciosamente seguir o código ou silenciosamente seguir o GDD — a divergência em si é o sinal de que algo precisa de decisão humana.

## Fluxo de conteúdo (agentes → JSON → ScriptableObject)

Agentes de IA **nunca escrevem `.asset` à mão** — o formato YAML serializado do Unity (com GUIDs internos) só é gerado corretamente pelo próprio editor. O fluxo sancionado é:

1. O agente escreve um `.json` em `Assets/Content/<Tipo>/<key>.json` (ex: `Assets/Content/Genres/action_rpg.json`), seguindo a forma do DTO correspondente (ex: `GenreContentDto`). Ver `Assets/Content/Genres/action_rpg.example.json` como modelo de referência.
2. Você roda **Tools → GameLike → Import Content → \<Tipo\>** no menu do Unity.
3. O importer (`Editor/Content/<Tipo>ContentImporter.cs`) lê todos os JSONs da pasta, cria o `.asset` se não existir (casando pelo campo `key`, um slug estável) ou atualiza os campos de um existente, preservando o `Id` já atribuído.
4. `AssetDatabase.Refresh()` dispara o `DefinitionAssetIdAssigner`, que atribui um `Id` novo apenas a assets recém-criados.

A escala de peso textual do GDD (`"---".."+++"`) é convertida para `WeightLevel` (-3..+3) por `WeightScaleParser`, compartilhado por todos os importers — nenhum importer reinterpreta a escala por conta própria.

**Pendência conhecida:** a integração do texto pt-BR (`displayNamePtBr`, `descriptionPtBr`) com o Unity Localization Package ainda não está automatizada — o importer hoje só loga o texto recebido para colagem manual nas Localization Tables. Automatizar essa ponte é um item do roadmap (ver `agents.md`).

## Testes automatizados

O projeto usa **Unity Test Framework** desde o início, não como reboco posterior. `Assets/Scripts/Tests/EditMode/` contém os testes de lógica pura (cálculos, regras de combinação, EventBus) — a maioria dos testes do projeto vive aqui, já que a maior parte da lógica de gameplay (fórmula de qualidade, combinação gênero+subgênero, probabilidade de M&A) é determinística e não depende de frame de Unity.

- **Prioridade de cobertura:** lógica matemática/determinística do GDD (fórmula de qualidade seção 19.7, combinação gênero+subgênero seção 18.4, probabilidade de aceitação de M&A seção 12.3) — são regras fáceis de quebrar silenciosamente se alguém ajustar um peso ou uma constante.
- **Padrão de teste:** ver `Tests/EditMode/Core/EventBusTests.cs` como modelo — `[TestFixture]`, `[Test]`, nomes descritivos no formato `Metodo_Condicao_ResultadoEsperado`, e `[TearDown]` limpando estado compartilhado (o `EventBus` é estático, sempre chame `EventBus.Clear()` no `TearDown` de testes que o usam).
- **PlayMode tests** (`Tests/PlayMode/`, a criar quando necessário) ficam reservados para o que exige de fato rodar frames de Unity — corrotinas do `SceneLoader`, comportamento de `MonoBehaviour` ao longo do tempo.
- Todo sistema novo de lógica pura nasce com pelo menos um teste cobrindo o caminho feliz e um caso de borda.

## Save (formato e versionamento)

- **Formato:** JSON serializado e comprimido com gzip. Padrão de mercado para jogos indie/médios com save estruturado e complexo (listas aninhadas de funcionários, projetos, histórico). Build de desenvolvimento pode expor um modo "salvar sem compressão" para debug manual do JSON cru.
- **Versionamento de schema obrigatório desde o primeiro save.** O objeto raiz do save inclui um campo `schemaVersion` (int). Mudanças de estrutura no save (decorrentes de mudanças no GDD) incrementam essa versão e são acompanhadas de uma função de migração que transforma um save de versão N em versão N+1 antes de carregá-lo. Nunca quebre compatibilidade silenciosamente — se o schema mudou, escreva a migração, mesmo que trivial.
- Conteúdo referenciado no save (gênero do projeto, tema, console, etc.) usa o `Id` estável do `DefinitionAsset`, nunca o nome do asset.

## Estratégia de cenas (Boot persistente + aditivas)

O jogo usa uma **cena Boot persistente** que nunca é descarregada, mais **cenas de conteúdo** (`MainMenu`, `Gameplay`, `SkillTree`, ...) que carregam e descarregam aditivamente por cima dela.

- **`Boot`** contém todos os managers de tempo de vida da aplicação (`TimeManager`, `EconomyManager`, `SceneLoader`, e futuros como `SaveManager`) sob um `GameObject` marcado com `DontDestroyOnLoad` via `Bootstrap.cs`. Carrega primeiro e permanece ativa do início ao fim da execução.
- **`SceneLoader`** (`Core/Scenes/SceneLoader.cs`) é o único ponto de troca de cena de conteúdo. Chame `SceneLoader.SwitchTo("Gameplay")` — ele carrega a nova cena aditivamente, a torna ativa, e só então descarrega a anterior.
- **Por quê:** o jogador pausa a run, abre a árvore de habilidades, volta ao jogo — tudo sem destruir o estado da run. Com Boot persistente, o estado simplesmente continua vivo na memória entre transições.
- **`Bootstrap.cs`** define qual cena de conteúdo carrega primeiro via campo configurável no Inspector (`firstContentScene`), não hardcoded.
- Os payloads de transição de cena (`SceneLoadStartedEvent`, `SceneLoadCompletedEvent`, `SceneUnloadedEvent`) vivem em `Core/Events/`, não em `Events/Payloads/` — são estruturais, não eventos de domínio do jogo.

## Input System

O projeto usa o **Input System novo** (pacote `com.unity.inputsystem`), não o Input Manager legado. Mesmo o jogo sendo mouse+UI predominante, a escolha pensa no longo prazo: rebind de atalhos (pausar, acelerar tempo, abrir painéis) de forma limpa, e compatibilidade futura com controle (relevante para Steam Deck, já que o jogo é Steam-first). Ao implementar input, use Actions (`InputActionAsset`), não `Input.GetKey` direto — qualquer captura de tecla nova deve nascer como uma Action mapeada, nunca hardcoded.

## Player Settings (placeholder — trocar antes do lançamento)

- **Company Name:** `PlaceholderStudio`
- **Bundle Identifier:** `com.placeholderstudio.gamelike`

Estes valores ainda não são definitivos. **Atenção:** o Company Name compõe o caminho de save do jogador (`%appdata%/PlaceholderStudio/GameLike/...` no Windows, equivalente no Mac/Linux). Trocar o nome da empresa depois que houver saves reais em teste invalida esses saves — troque o quanto antes, antes de gerar qualquer save que precise persistir entre sessões de teste.

## O EventBus (padrão central)

Localização: `Assets/Scripts/Core/Events/`.

- **Type-based publish/subscribe.** Para criar um evento novo, basta criar um novo struct payload que implementa `IGameEvent`. **Nunca** edite o `EventBus.cs` para adicionar um evento — essa é a vantagem do design (Opção B, genérico por tipo).
- **Publicar:** `EventBus.Publish(new GameLaunchedEvent(...))`.
- **Escutar:** `EventBus.Subscribe<GameLaunchedEvent>(handler)` em `OnEnable`, `EventBus.Unsubscribe<GameLaunchedEvent>(handler)` em `OnDisable`. **Sempre pareie subscribe/unsubscribe** para evitar vazamento de handlers entre cenas/runs.

### Regra de ouro: evento avisa, método pergunta
Use EventBus apenas para **notificar fatos que já aconteceram** (passado) e que têm **alto fan-out** (muitos sistemas reagem): semana passou, jogo lançado, fase mudou, empresa vendida, funcionário contratado/demitido, crise iniciada.

Para **consultar dados** (presente) — "qual o teto de pontos deste projeto?" — chame o método/sistema diretamente ou leia do ScriptableObject. **Nunca pergunte via evento.** É aqui que event-driven vira inferno; não cruze essa linha.

Se você se pegar criando mais de algumas dezenas de tipos de evento, provavelmente está usando evento onde deveria ser chamada direta. Pare e reavalie.

### Logging
`Assets/Scripts/Core/Logging/EventBusLogger.cs` instrumenta todo `Publish`. Saída colorida por categoria no console do Unity, filtrável por severidade e categoria. Está dentro de `#if UNITY_EDITOR || DEVELOPMENT_BUILD` — **não existe na build de release** (custo zero para o jogador). Ao criar um payload, defina `Category` e `Severity` corretos para que o log seja útil.

## Estrutura de pastas e Assembly Definitions

O projeto usa **Assembly Definitions (`.asmdef`)** desde o início — não é opcional, é parte da arquitetura. Sem elas, qualquer alteração em qualquer script recompila o projeto inteiro, e nada impede uma camada de baixo nível (definição) de referenciar acidentalmente uma de alto nível (sistemas). Os asmdef fazem o compilador proteger a separação de camadas.

```
Assets/Scripts/
├── Core/               [GameLike.Core]     sem dependências
│   ├── Events/         EventBus, IGameEvent, enums de categoria/severidade
│   └── Logging/        EventBusLogger
├── Events/             [GameLike.Events]   depende de: Core
│   └── Payloads/       Structs de evento (IGameEvent) por domínio
├── Data/                [GameLike.Data]     depende de: Core, Events
│   └── (futuro)        ScriptableObjects de definição (Genre, Theme, Platform...)
├── State/               [GameLike.State]    depende de: Core, Events, Data
│   └── (futuro)        Classes de estado serializáveis (CompanyState, RunState...)
└── Systems/             [GameLike.Systems]  depende de: Core, Events, Data, State
    └── (managers, um subfolder por domínio: Time/, Economy/, ...)
```

**Regra de dependência (de baixo para cima): `Core ← Events ← Data ← State ← Systems`.** Uma camada nunca referencia a que está acima dela. Se `Data` precisar de algo de `Systems`, é sinal de erro de design — pare e reavalie antes de adicionar a referência ao `.asmdef`.

Ao criar um novo `.cs`, primeiro decida em qual pasta/assembly ele pertence segundo essa regra, e só então escreva o código.

## Convenções de código

- **Namespaces** seguem a estrutura de pastas: `GameLike.Core.Events`, `GameLike.Systems.Time`, etc.
- **Payloads de evento** são `readonly struct` implementando `IGameEvent` (value types, sem alocação no publish).
- **Managers** são `sealed class : MonoBehaviour`, expõem estado via propriedades read-only, e centralizam mutações em um único método "choke point" que dispara o evento correspondente.
- **XML docs** em português nas assinaturas públicas; o código em inglês.
- Campos serializados usam `[SerializeField] private`, nunca `public`.

## Fluxo de exemplo (lançamento de jogo)

1. `ProjectManager` calcula a nota usando a fórmula da GDD seção 19.7, lendo pesos dos ScriptableObjects.
2. `ProjectManager` dispara `EventBus.Publish(new GameLaunchedEvent(...))`.
3. Reagem independentemente: `EconomyManager` (projeta vendas), `ValuationManager` (recalcula valuation), `ReputationManager` (ajusta reputação), `MarketManager` (satura gênero/tema), `FranchiseManager` (atualiza média se for sequência), `AchievementManager` (checa desbloqueios).
4. `ProjectManager` não conhece nenhum deles.

## Estado atual do projeto

Fundação inicial gerada (arquitetura de eventos + logging + exemplos de manager/listener). Ainda **não** implementados: ScriptableObjects de definição, classes de estado, e a maioria dos managers de sistema. Consulte `agents.md` para o roadmap de implementação e a ordem recomendada.

## Ao implementar algo novo

1. Confirme a regra no **GDD v1.0** (seção relevante).
2. Decida a camada: é definição (SO), estado (classe serializável) ou sistema (manager)?
3. Se o sistema reage a um fato de alto fan-out, escute via EventBus; se anuncia um, publique via EventBus.
4. Respeite os princípios inegociáveis (tipagem, inglês, sem libs externas, extensível).
5. Atualize `agents.md` se concluir um item do roadmap.
