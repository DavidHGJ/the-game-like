# agents.md — The Game Like

Guia operacional para agentes de IA trabalhando no The Game Like. Complementa o `CLAUDE.md` (que descreve a arquitetura). Este arquivo foca no **roadmap**, na **ordem de implementação** e nas **tarefas delegáveis**.

## Antes de tudo

Leia o `CLAUDE.md` por completo. Ele define a arquitetura de três camadas (Definição / Estado / Sistemas), o padrão EventBus e os princípios inegociáveis. Nada aqui faz sentido sem aquele contexto. O **GDD v1.1** é a fonte da verdade para regras de jogo, e o **`ART_STYLE.md`** é a fonte da verdade para qualquer decisão visual (paleta, tile, outline, composição de sprite) — consulte-o antes de criar ou revisar qualquer asset visual.

## Filosofia de implementação

O jogo é uma teia de sistemas interconectados. Construir tudo de uma vez é receita para o caos. A ordem abaixo segue a **dependência entre sistemas** e prioriza chegar a um **loop core jogável** cedo, para validar diversão antes de expandir.

## Roadmap de implementação

### Fase 0 — Fundação (em andamento)
- [x] EventBus genérico type-based (`Core/Events/EventBus.cs`)
- [x] `IGameEvent` + enums de categoria/severidade
- [x] EventBusLogger colorido (`Core/Logging/`)
- [x] Payloads de exemplo (tempo, economia, projeto, empresa)
- [x] Manager de exemplo (`TimeManager`) e listener de exemplo (`EconomyManager`)
- [x] Assembly Definitions por camada (`Core`, `Events`, `Data`, `State`, `Systems`)
- [x] Estratégia de cenas: Boot persistente + SceneLoader aditivo (`Core/Bootstrap/`, `Core/Scenes/`)
- [x] Decisões de setup do projeto: render pipeline (URP 3D — migrado de URP 2D em 2026-10-31, ver `ART_STYLE.md`), Input System (novo), Player Settings (placeholder) — ver `README.md`
- [ ] **Ação manual necessária (editor Unity):** trocar o Universal Renderer de 2D para 3D e configurar a câmera ortográfica em ângulo isométrico — ver `README.md`, checklist de setup
- [ ] Definir `CompanyState` e `RunState` (camada de estado)
- [ ] Bootstrap/composition root que instancia os managers numa cena

### Fase 1 — Estruturas de dados (ScriptableObjects de definição)
Destrava o trabalho de conteúdo em paralelo. Criar as classes SO **vazias de dados mas completas de campos**, para os agentes de conteúdo preencherem depois.
- [x] `WeightLevel` / `ProjectSize` / `ParameterWeightBySize` — escala -3..+3 do GDD (`Data/Definitions/WeightScale.cs`)
- [x] `GenreSO` — perfil de qualidade por tamanho de projeto (GDD 18.5), 19 parâmetros (`Data/Definitions/GenreSO.cs`)
- [x] Pipeline de importação de conteúdo (JSON → asset): `DefinitionAsset` (Id estável), `DefinitionAssetIdAssigner` (auto-gera Id), `GenreContentDto` + `WeightScaleParser` + `GenreContentImporter` (menu Tools → GameLike → Import Content → Genres)
- [x] Sistema de balanceamento: `BalanceAsset` base + `EconomyBalanceSO` / `ResearchBalanceSO` / `EngineBalanceSO` / `LegacyProgressionBalanceSO` / `QualityFormulaBalanceSO` + `BalanceProvider` (`Data/Balance/`)
- [x] Sistema de layout de escritório (fundação de dados): `FurnitureEffectType`/`FurnitureEffect`, `FurnitureCategory`, `FurnitureVisualLayer` (composição base+estofado+cor via tint), `FurnitureDefinitionSO`, `RoomBlockSO` (7x7 tiles fixo), `OfficeLayoutTypes` (`PlacedRoomBlock`/`BlockConnection`/`PlacedFurniture`), `OfficeLayoutSO` (`Data/Office/`) — ver `CLAUDE.md`, seção "Sistema de layout de escritório", e `ART_STYLE.md` para a referência visual e unidade de tile (32x32)
- [x] **Ferramenta de autoria de cenários: Map Builder in-game** (substitui a ideia de cena de edição com Tilemap + bake, da direção 2D). Opção "Map Builder" no menu inicial (editor/development build) ou `Tools → GameLike → ★ Map Builder in-game (Play)`; salva JSON em `Assets/Resources/OfficeMaps/`, que o jogo carrega por fase. Ver `CLAUDE.md`, "Sistema de layout de escritório".
- [ ] **Ação manual necessária:** criar uma instância de cada `BalanceAsset` em `Assets/Resources/Balance/` (Assets → Create → GameLike → Balance → ...) — os SOs existem como classe mas nenhuma instância foi criada ainda
- [ ] `SubgenreSO` — gênero pai + peso de combinação (GDD 18.4) + regra de combinação determinística
- [ ] `ThemeSO` — categoria + compatibilidade com gêneros (GDD 18.7)
- [ ] `PlatformSO` / `ConsoleSO` — ano de lançamento, popularidade, licença, curva (GDD 15)
- [ ] `EngineModuleSO` — custo EB, custo semanal, categoria (GDD 6.2 / 19.4)
- [ ] `TraitSO` — efeitos de traço de personalidade (GDD 7.4)
- [ ] `ToolSO` — ferramentas de P&D (GDD 6.3)
- [ ] `NarrativeEventSO` — texto, opções, habilidade requerida, consequências (GDD 11)
- [ ] Replicar o padrão `ContentDto` + `ContentImporter` de `GenreSO` para cada SO acima (mesmo molde, ver `Editor/Content/GenreContentImporter.cs`)
- [ ] Integrar `displayNamePtBr`/`descriptionPtBr` do importer com o Unity Localization Package de fato (hoje só loga o texto — ver `CLAUDE.md`, seção "Fluxo de conteúdo")

> **Nota sobre a escala de pesos:** o GDD usa `--- / -- / - / ~ / + / ++ / +++` mapeado para inteiros `-3..+3`. Já implementado como `WeightLevel` enum. A regra de combinação gênero+subgênero está na GDD seção 18.4 — é determinística, implemente exatamente como descrito, com teste cobrindo os casos de borda (positivo+positivo, negativo+negativo, misto).

### Fase 2 — Vertical slice (loop core cru)
Objetivo: provar que o loop é divertido. Sem arte, sem polish.
- [ ] `ProjectState` + fluxo de criação de jogo (conceito → sliders → produção)
- [ ] Sistema de pontos por fase (GDD 5.3) com tetos por tamanho
- [ ] Fórmula de qualidade (GDD 19.7) lendo pesos dos SOs
- [ ] Lançamento → nota → vendas → receita (loop fechado)
- [ ] `TimeManager` dirigindo o tick (já existe stub)
- [ ] Conteúdo de pelo menos **3-5 gêneros variados** importado via `GenreContentImporter` (não apenas 1 — variedade é o que valida se a fórmula diferencia estrategicamente os gêneros)

**Critério de sucesso (definição de "pronto" desta fase):** é possível jogar **vários ciclos seguidos** — criar um jogo, distribuir sliders, lançar, receber nota e receita, e iniciar o próximo projeto com o dinheiro acumulado — sem travar, sem dado incoerente, e sentindo a progressão financeira ao longo dos ciclos. Não é sobre estar bonito ou completo; é sobre o loop sustentar repetição sem quebrar e sem ser obviamente sem graça.

### Fase 3 — Expansão sistema por sistema (ordem de dependência)
- [ ] P&D (PP, engines customizadas, Engine Budget) — GDD 6
- [ ] Funcionários (contratação, XP, traços, burnout) — GDD 7
- [ ] Mercado (tendências, saturação, rivais) — GDD 8
- [ ] Marketing (hype, wishlists) — GDD 10
- [ ] Eventos narrativos — GDD 11
- [ ] Fases da empresa + transição — GDD 3
- [ ] Contratos — GDD 9
- [ ] M&A — GDD 12
- [ ] Franquias — GDD 13
- [ ] Valuation completo — GDD 4

### Fase 4 — Meta-progressão
- [ ] `MetaProgressionState` (persiste entre runs)
- [ ] Árvore de habilidades do fundador (GDD 14) — 6 árvores, duas moedas
- [ ] Reset de run + máquina do tempo + empresas-fantasma rivais
- [ ] Pontos de Legado e Dinheiro Pessoal (GDD 14.2 / 19.5)

### Fase 5 — UX e suporte
- [x] Decisão de UI: UI Toolkit (UXML/USS), assembly `GameLike.UI` e estrutura de pastas já criados — ver `CLAUDE.md`, seção "UI (UI Toolkit)"
- [ ] Menu principal (GDD 20.1)
- [ ] Customização e evolução visual do fundador (GDD 20.2)
- [ ] Tutorial opcional + tooltips contextuais (GDD 20.3)
- [ ] Save system: autosave + manual + múltiplos slots (GDD 20.6)
- [ ] Achievements (GDD 20.5)

## Tarefas delegáveis a agentes de conteúdo (paralelas ao código)

Estas não dependem de código pronto além das classes SO e do importer da Fase 1. Podem rodar em background. Todas usam a escala `--- / -- / - / ~ / + / ++ / +++` (texto, não número) e estão descritas no GDD.

**Formato de entrega:** um `.json` por item de conteúdo, em `Assets/Content/<Tipo>/<key>.json`, seguindo o DTO correspondente. `GenreSO` já tem o pipeline completo (`GenreContentDto` + `GenreContentImporter`) — use-o como molde exato para os próximos tipos. Após o agente escrever os JSONs, rode **Tools → GameLike → Import Content → \<Tipo\>** no Unity para gerar/atualizar os `.asset`.

1. **Perfil de qualidade por gênero** (GDD 18.5) — para cada um dos ~45 gêneros, preencher o peso de cada parâmetro nos 4 tamanhos de projeto. Pipeline já existe — ver `Assets/Content/Genres/action_rpg.example.json`.
2. **Peso subgênero × gênero pai** (GDD 18.4) — para cada um dos ~40 subgêneros. Requer `SubgenreSO` + `SubgenreContentImporter` (a criar, mesmo molde do Genre).
3. **Compatibilidade Tema × Gênero** (GDD 18.7) — matriz ~60 temas × ~45 gêneros. Requer `ThemeSO` + importer.
4. **Compatibilidade Gênero × Plataforma** (GDD 18.8) — matriz ~45 gêneros × 5 tipos de plataforma. Requer `PlatformSO`/`ConsoleSO` + importer.
5. **Expansão de eventos narrativos** (GDD 11) — dos ~25 exemplos para 100+ eventos. Requer `NarrativeEventSO` + importer.

> Nunca crie ou edite um `.asset` à mão. O fluxo é sempre JSON → menu de importação → asset. Não invente parâmetros fora dos definidos no GDD. O campo `key` do JSON deve ser um slug estável (snake_case) que não muda mesmo se o nome de exibição mudar — é ele que o importer usa para casar com um asset já existente.

## Regras para agentes

- **Não quebre a separação de camadas.** Estado mutável nunca vai em SO de definição.
- **Não edite o `EventBus.cs`** para adicionar eventos — crie um payload novo.
- **Não adicione bibliotecas externas** sem autorização explícita do desenvolvedor.
- **Não hardcode conteúdo** que deveria ser ScriptableObject.
- **Consulte o GDD** antes de assumir qualquer regra de jogo; se o GDD for ambíguo, pergunte ao desenvolvedor em vez de inventar.
- **Marque progresso** atualizando os checkboxes deste arquivo ao concluir um item.
- **Respeite os valores de balanceamento** da GDD seção 19 como ponto de partida — eles são marcados como "a testar e ajustar", então estruture o código para que sejam fáceis de mudar (constantes em SO/config, não números mágicos espalhados).

## Glossário rápido (termos do GDD)

- **Run** — um ciclo de vida completo de uma empresa, do zero até a venda.
- **Valuation** — valor de mercado da empresa; KPI central.
- **PP** — Pontos de Pesquisa (moeda de P&D, reseta por run).
- **EB** — Engine Budget (limite de entrega de uma engine por fase de produção).
- **PL** — Pontos de Legado (moeda de meta-progressão, nós únicos da árvore).
- **Dinheiro Pessoal** — moeda de meta-progressão obtida na venda; compra nós incrementais.
- **Fases** — Garage → Indie → Escritório (Office) → Estúdio (Studio).
- **Empresa-fantasma** — versão de uma run anterior, agora rival NPC.
