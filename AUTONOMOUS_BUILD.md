# AUTONOMOUS_BUILD.md — Build autônomo até versão funcional

Mandato do dev (ausente): montar um **cenário 3D funcional sem sobreposição**, um **criador de
fases 3D** (como o planejado no 2D, mas 3D), e preparar **toda a base do jogo** para ter a
**experiência visual da fase** (não só itens/menus), estilo **Game Dev Tycoon** (HUD com
círculos de recursos no topo, nome do projeto + barra de progresso, escritório isométrico de
fundo, popups de etapa de dev e de resultado) — mais clean/fácil. Concluir a funcionalidade
core. Não perguntar; decidir o recomendado e anotar; continuar após resets de token/hora.

Referências visuais enviadas: Game Dev Tycoon (garagem iso, escritório com staff nomeado,
popup "Game Complete" com XP por área, popup "Development Stage" com sliders de alocação).

---

## 🔎 Causa do bug da cena 3D (resolvido no design novo)
A câmera iso fica no canto **-X,-Z**; as paredes foram postas **nesse mesmo canto**, ficando
**entre a câmera e os móveis do fundo** → ocultavam tudo em z<0 ("paredes por cima"). Os FBX
estão corretos. **Correção:** paredes sempre no canto **oposto** à câmera (far side: +X/+Z se
a câmera olha de -X,-Z), e layout em grid sem sobreposição.

---

## Decisões iniciais tomadas (dev valida depois)
1. **Sistema 3D novo e limpo** para o escritório, em vez de retrofit do `LevelBuilder` 2D
   existente (que é acoplado a sprites/Tilemap/sortingOrder). O LevelBuilder 2D fica no repo
   (compila, testes passam) mas **deprecado** para a direção 3D.
2. **Mobília = prefab 3D** referenciado por uma definição (`FurnitureDef3DSO`), com footprint em
   células, categoria, efeito (GDD) e custo. Reaproxima o `FurnitureDefinitionSO` 2D, mas 3D.
3. **Criador de fases 3D** = `EditorWindow` que edita um `OfficeLayoutSO` por um grid top-down
   (clicar célula p/ colocar a peça selecionada, girar, remover) + "rebuild na cena". É a
   ferramenta de autoria do dev (o escritório não é editável pelo jogador em runtime).
4. **Escritório como cenário de gameplay**: a cena de jogo carrega o `OfficeLayoutSO` da fase
   atual via `OfficeBuilder`, e o HUD (UI Toolkit) fica por cima. Câmera ortográfica iso fixa.
5. **UI**: migrar do harness IMGUI para **UI Toolkit** (CLAUDE.md Fase 5), começando pelo HUD
   estilo tycoon. O harness IMGUI fica como fallback de debug até a UI Toolkit cobrir o loop.
6. **Unidade de grid 3D**: célula = 1×1 unidade de mundo (ajustável no `OfficeLayoutSO`); móveis
   em metros reais (mesa ~1.6). `cellSize` default 1.0, calibrar contra a arte.
7. **Escopo de "core"**: priorizar a experiência visual + loop já GDD-fiel; depois expandir
   sistemas do GDD (funcionários, etapas de dev com sliders, popup de resultado, P&D) na ordem
   que mais entrega valor visual/jogável. Sistemas muito grandes (M&A, franquias, árvore
   completa) ficam para o fim.

---

## Roadmap (ordem de execução)
- [ ] **F1 — Fundação 3D de escritório**
  - [ ] `FurnitureDef3DSO` (prefab, footprint, categoria, efeito, custo).
  - [ ] `OfficeLayoutSO` (grid, chão/parede, lista de placements) + `OfficePhase`.
  - [ ] `OfficeGrid` (cell↔world 3D no plano XZ).
  - [ ] `OfficeBuilder` (runtime): chão + 2 paredes no far-side + móveis sem sobreposição.
  - [ ] Prefabs dos 7 móveis a partir dos FBX + assets `FurnitureDef3DSO`.
- [ ] **F2 — Criador de fases 3D** (`OfficeLayoutWindow` editor) + autorar a Garage limpa.
- [ ] **F3 — Integração visual**: cena de jogo com o escritório de fundo + câmera iso.
- [ ] **F4 — HUD estilo tycoon (UI Toolkit)**: círculos de recursos (Bugs/Design/Tech/Research),
      nome do projeto + barra de progresso, caixa/fans, relógio.
- [ ] **F5 — Etapas de dev + popup de resultado** (refs: Development Stage, Game Complete).
- [ ] **F6 — Funcionários** (staff no escritório, contratação) — popula o escritório.
- [ ] **F7 — Expansão GDD** (P&D, mercado, eventos, etc.) conforme der.

## Dúvidas guardadas para o dev (decisão recomendada entre parênteses)
- Estilo final da UI: mantenho UI Toolkit custom "clean" tipo GDT (sim, recomendado) ou outro?
- Paleta/materiais dos móveis: provisórios agora; formalizar paleta compartilhada depois.
- Nomes dos consoles/plataformas do GDD (§15) — usar os paródicos do GDD quando chegar lá.
- Divergência GDD-first 2D→3D ainda pendente de confirmação (atualizar GDD p/ v1.2).

## Log de progresso
- (início) Causa do bug da cena identificada; plano definido.
- **F1 ✅** Fundação 3D: `FurnitureDef3DSO`, `OfficeLayoutSO`, `OfficeGrid`, `OfficeBuilder`,
  `OfficeView`. Compila 0 erros. 8 defs de mobília criadas, 7 FBX (incl. Workstation combinada).
- **F2 ✅** Criador de fases 3D (`OfficeLayoutWindow`): paleta, grid 9×8, rotação, preview,
  salvar. Testado no editor — autora e previa o layout.
- **F3 ✅** Cena `GameOffice` monta o escritório em runtime como cenário. **Bug das paredes
  resolvido** (paredes no canto oposto à câmera). Escritório limpo, sem sobreposição.
- Observações: a Garage 9×8 ficou um pouco vazia p/ uma garagem; considerar sala menor/mais
  clutter (polish). Câmera do GameOffice precisa de enquadramento melhor (F4).
- **Visual (feedback do dev: feio/sem luz/opaco) ✅** Bevel nos modelos + material menos fosco
  (Blender), e iluminação nova (`OfficeLighting`: ambiente plano + luz principal quente + fill +
  sombra suave) + **fundo creme** estilo GDT. Verificado no Unity: escritório ficou limpo e
  agradável.
- **F4 (quase) HUD UI Toolkit** — `HUD.uxml` + `HUD.uss` + `HudController` + `HudSetup` (menu
  "Office 3D → 4 - Build HUD"). Barra superior (chips Fase/Equipe/Jogos, projeto+progresso,
  caixa/valuation/data), barra inferior (pausar/velocidade/novo projeto/vender/nova empresa),
  modais de novo projeto e de resultado. **Renderiza corretamente quando o PanelSettings está
  atribuído** (confirmado no editor).
  - ⚠️ **PENDÊNCIA/BUG:** a atribuição automática do `HudPanelSettings` ao `UIDocument` pelo
    `HudSetup` não persiste (volta a "None" ao entrar em Play). Atribuí manualmente e o HUD
    apareceu, mas não ficou salvo. **Próximo passo:** corrigir no `HudSetup` (atribuir via
    `SerializedObject`/`PrefabUtility` e salvar), ou investigar por que o campo reverte. Depois
    disso o HUD está pronto.

## SESSÃO 2026-11-02/03 — Build de todos os fluxos de gameplay do GDD + HUD video-game
Mandato: continuar autônomo, cobrir **todos os fluxos de gameplay do GDD**, tudo funcionando,
telas estilo GDT, HUD video-game com 3D de fundo. Decisões não-triviais registradas aqui.

### Toolchain novo desta sessão
- **Compile-check headless** via `Assets/Scripts/Editor/CI/BatchCompile.cs` + Unity batchmode
  (`-executeMethod GameLike.Editor.CI.BatchCompile.Check`). Script em scratchpad `compile.sh`.
  Baseline confirmado: **0 erros CS**. Uso após cada bloco de C# para manter o build verde.
- Decisão: NÃO commitar (dev commita). Manter build sempre compilando.

### Plano de execução (ordem por valor de gameplay + dependência)
- [ ] **A — Funcionários (GDD 7):** `EmployeeInstance`, especialidade, senioridade, traços, humor,
  burnout, XP; `TalentMarket` (pool rotativo); `StaffManager` (contratar/demitir, salário semanal,
  XP ao lançar, burnout no crunch). Substitui o placeholder `TeamQualityForPhase` por nível real
  da equipe. Popula o escritório visualmente (depois).
- [ ] **B — Marketing/Hype/Wishlists + Bugs (GDD 5.5/10):** hype acumula na produção e decai;
  wishlists; bugs acumulam por pouca Programação/QA; fase de bugfix. Substitui o proxy do
  `SalesCalculator`.
- [ ] **C — Temas (GDD 18.6/18.7):** `ThemeSO` + compat Tema×Gênero → componente 2 da fórmula de
  qualidade + multiplicador de hype. Conteúdo JSON de temas. Tema no modal de novo projeto.
- [ ] **D — Mercado/Tendências (GDD 8):** tendência por gênero/tema, saturação no lançamento →
  componente 5 da fórmula.
- [ ] **E — Eventos narrativos (GDD 11):** `NarrativeEventSO` + `EventManager` (aleatório, pausa o
  tempo, escolhas com consequências ocultas) + popup no HUD + ~15-25 eventos em JSON.
- [ ] **F — P&D (GDD 6):** Pontos de Pesquisa, árvore básica de desbloqueios.
- [ ] **G — Valuation completo (GDD 4).**
- [ ] **H — Meta-progressão + árvore de habilidades (GDD 14) + tela de fim de run (GDD 20.4).**
- [ ] **I — Save (GDD 20.6):** JSON+gzip, schemaVersion + migração.
- [ ] **J — HUD video-game:** círculos de recurso (Design/Tech/Bugs/Research), painéis (RH,
  Mercado, P&D), surface de tudo acima, 3D de fundo.
- [ ] **K — Conteúdo:** mais gêneros/temas/eventos em JSON.
- [ ] **L — Blender:** "dev" sentado + escritório mais rico.

### Decisões de design tomadas nesta sessão (dev valida depois)
- **Especialidades de funcionário mapeadas às 5 DevAreas** (Design/Arte/Áudio/Tech/Conteúdo)
  + Generalista (fundador). GDD 7.1 lista só Programação/Design mas declara extensível;
  mapear às áreas deixa a composição da equipe influenciar a entrega por área (concretiza o
  componente 3 da fórmula de qualidade). Alt: só Prog+Design — rejeitado por ser raso demais.
- **Qualidade da equipe real** substitui o placeholder `TeamQualityForPhase`:
  `StaffCalculator.TeamQuality01` = 50% nível médio (senioridade·humor·burnout) + 50% cobertura
  de especialistas vs. alocação.
- **Custos semanais reorganizados:** folha salarial = `StaffManager`; overhead por fase =
  `EconomyManager.OnWeekTick` (novo campo `WeeklyOperatingFor` no EconomyBalance). O
  `ProjectManager` não cobra mais "custo de desenvolvimento" (era proxy de salário). Overhead por
  fase é adição ao GDD (slice) — **pendência de confirmar no GDD** (GDD 2.3 fala em custo de
  manutenção exponencial; valores a calibrar).
- **Hype/wishlists reais** (MarketingManager) substituem o proxy derivado da nota no
  SalesCalculator. Hype acumula na produção + campanhas, decai; wishlists = hype × potencial/semana.
- **Bugs** acumulam por pouca Tech/QA e equipe fraca; penalizam a nota no lançamento
  (`BugCalculator`). Config em GameLoopBalance.
- **Temas:** `ThemeSO` + compat Tema×Gênero (JSON → importer). Compat alimenta o componente 2 da
  qualidade (`QualityCalculator.WeightToScore`) e um multiplicador de hype (0.55..1.45).
- **Mercado:** `MarketState` (tendência por gênero/tema, save-ready) + `MarketManager` (passeio
  aleatório + reversão à média + saturação no lançamento). Tendência do tema → componente 5 da
  qualidade.
- **Eventos narrativos usam `string` pt-BR direto** (não `LocalizedString`) — a localização ainda
  não está ligada e o texto do evento é o gameplay. **Pendência:** migrar p/ chave de localização
  quando a ponte com o Localization Package existir. Opções com habilidade do fundador ficam
  **bloqueadas** no slice (meta-progressão/árvore ainda não existe — GDD 14).
- **PanelSettings do HUD:** persistência corrigida no `HudSetup` via `SerializedObject`
  (`m_PanelSettings`/`sourceAsset`) + `clearColor=false` (3D aparece atrás do HUD).

### Log desta sessão
- Toolchain de compile headless criado e validado (baseline 0 erros).
- **A ✅ Funcionários:** `EmployeeInstance`/enums, `StaffCalculator` (+testes), `CandidateGenerator`,
  `StaffManager` (contratar/demitir, folha, XP+promoção, burnout). Integrado ao loop e à qualidade.
- **B ✅ Marketing/Hype/Bugs:** `MarketingBalanceSO`, `MarketingManager` (campanhas), `BugCalculator`,
  `SalesCalculator` refeito p/ hype+wishlists reais. Bugs penalizam nota.
- **C ✅ Temas:** `ThemeSO` + `ThemeContentImporter`, 8 temas em JSON importados (compat Tema×Gênero
  serializada + Id atribuído). Integrado à qualidade e ao hype.
- **D ✅ Mercado:** `MarketState` + `MarketManager`, tendência no componente 5 + saturação.
- **E ✅ Eventos:** `NarrativeEventSO` + `EventManager` (pausa/escolhe/aplica efeitos) +
  `NarrativeEventContentImporter`, 11 eventos em JSON importados.
- **Setup headless:** `BalanceAssetsSetup` (cria os 7 balance assets, incl. os faltantes
  Engine/Legacy/Research/Marketing) + `BatchSetup` (balance + import temas/eventos/gêneros).
- **HUD video-game (UI Toolkit):** expandido — chip de reputação, painel lateral de produção
  (hype/wishlists/bugs) + botões de campanha, grid de tema no novo projeto, modal de RH
  (contratar/demitir + candidatos), popup de evento com opções, bugs no resultado. Cena
  `GameOffice` agora instancia todos os managers novos (via `GameSceneEnsure`, não-destrutivo).
- **G ✅ Valuation completo (GDD 4):** `ValuationCalculator` (liquidez + receita + catálogo +
  reputação + equipe + nota média + diversidade). Recalcula a cada semana + no lançamento.
- **H ✅ Fim de run + meta (GDD 2.4/14/19.5):** `MetaProgressionState`, `LegacyCalculator`,
  `RunEndedEvent`. IPO (venda), **falência** (caixa < 0 encerra a run), Pontos de Legado por
  fase/notas/pico/sem-falência. Dinheiro pessoal acumula e vira capital inicial da próxima run
  (slice: converte 100% — árvore de habilidades para gastá-lo é pendência, GDD 14). HUD mostra
  o fim de run.
- **I ✅ Save (GDD 20.6/21.6):** `SaveManager` JSON+gzip, `schemaVersion`=1 + ponto de migração.
  Autosave no lançamento e no fim de run (slot 0). `GameController.LoadGame` religa os managers.
- **Testes:** EditMode 35/35, PlayMode 2/2 (smoke do loop completo em runtime — valida wiring
  de todos os managers sem exceção). Round-trip de save testado.

### Verificação visual (build standalone + screenshot via player)
Método: `BatchBuild.BuildWindows` (build headless) + `StartupScreenshot` (dev-only, arg
`-glshot <png>`: inicia run demo, deixa produzir, captura e encerra). Confirmado:
- **Escritório 3D isométrico renderiza perfeito** (paredes verdes ao fundo, piso, mesa+monitor,
  cadeira, tapete, planta, caixas, janelas, fundo creme, iluminação) — estilo GDT. ✅
- **HUD renderiza sobre o 3D** (chips, barra de progresso, painel de produção com hype + botões
  de campanha, barra de ações). ✅
- **2 bugs de UI encontrados e corrigidos nesta verificação:**
  1. **`&` literal no HUD.uxml** (`"Equipe & Mercado"`) invalidava o XML **inteiro** → root
     vazio → todo `Q()` null → `HudController.Init` lançava NullReference a cada frame. Fix:
     `&amp;`. (UXML quebra por 1 caractere — sempre escapar `&` `<` `>`.) `HudController` agora
     desiste graciosamente se a árvore não carregar (sem flood).
  2. **`-unity-font-definition: initial;` no `.root` do HUD.uss** removia a fonte herdada do tema
     → texto invisível (layout/cores OK, sem glifos). Fix: remover a linha.

### Pendências/próximos (não feitos nesta sessão)
- Verificação visual do HUD+3D em Play (build standalone em andamento p/ screenshot).
- Sistemas GDD ainda não implementados: P&D/engines (GDD 6), Contratos (9), M&A (12),
  Franquias/sequências (13), Plataformas/consoles timeline (15), Árvore de habilidades UI (14),
  Achievements (20.5). Backend de meta já pronto p/ a árvore (PL + dinheiro pessoal).
- Migração dos textos (gêneros/temas/eventos) para o Localization Package (hoje string/JSON).
- Enriquecer o escritório (dev sentado, clutter) + autorar fases Indie/Office/Studio.

## 🔆 RAIZ DOS PROBLEMAS VISUAIS RESOLVIDA: Renderer 2D → 3D (02/11)
O projeto foi criado como **URP 2D** (o pipeline `UniversalRP.asset` usava o `Renderer2D.asset`).
O Renderer 2D **não faz iluminação/sombra 3D** — era a raiz do "iluminação não funciona /
estranho / chapado". **Correção aplicada** (menu `Setup → Switch to 3D Renderer (URP)`,
script `Switch3DRenderer.cs`): criado `Assets/Settings/Renderer3D.asset` (Universal Renderer 3D)
e apontado como default no pipeline. **Verificado em Play: a luz direcional 3D agora funciona**
(paredes com faces de tons diferentes, diorama 3D correto). → **Não recomeçar o projeto**;
a raiz era config, agora corrigida. (Atualizar GDD/agents: perspectiva 3D, URP 3D.)

## Próximos passos (retomar daqui)
1. Corrigir a persistência do PanelSettings do HUD (bug acima) → HUD 100% funcional em Play.
2. Remover de vez o harness IMGUI (já removido da cena pelo HudSetup).
3. Enriquecer o escritório: "dev" sentado (modelo Blender) + mais clutter / sala Garage menor.
4. Autorar layouts das fases Indie/Escritório/Estúdio no criador de fases.
5. Popups de etapa de dev (sliders) e integrar ao loop; depois funcionários, P&D, etc.

## SESSÃO NOTURNA 2026-11-04 — Menu + sistemas desbloqueáveis + personagem (em andamento)
Mandato do dev (áudios): (1) jogo abre no MENU (novo/continuar/opções/sair; continuar mostra nome da empresa); (2) nomes amigáveis (fim dos action_rpg/anos_80_retro crus); (3) P&D/pesquisa: ganhar PP e desbloquear tema/tamanho/engine/plataforma (nada aberto de cara) — GDD 6 + 3.2; (4) amarrar loop econômico (gastar p/ desenvolver: engine EB+$/sem, pesquisa PP+$/sem; custo fixo por fase garagem<estúdio) — auditar GDD inteiro vs. código; (5) HUD cantos + contextual (bolinhas por área subindo ao produzir, barra de vendas no lançamento, barra de produção); (6) personagem Blender sentado na cadeira (corrigir posição) + animações (digitar / ocioso olhar-lados/cima/cochilar c/ balãozinho) + bolinhas de ponto saindo da cabeça somando ao total. Trabalhar a noite toda, não perguntar, continuar após reset. Menu visual = Game Boy 3D (FBX já em Assets/Art/Menu/GameBoy) — fase visual depois do fluxo.

### Auditoria GDD rápida (o que falta p/ o loop fazer sentido)
- **P&D/Pesquisa (GDD 6, 19.3): NÃO existe.** PP, pesquisas (básica 10PP/$200sem, interm 30PP/$800sem, avançada 80PP/$2000sem), geração (+2/sem pesquisa, +5/jogo, +3/sem estudo), reset por run. Desbloqueios gated por FASE (teto, GDD 3.2) × P&D (acesso).
- **Engine customizada (GDD 6.2, 19.4): NÃO existe.** Módulos (leve 100EB/$100, médio 250/$300, pesado 500/$700), EB total por engine, custo semanal durante dev. Hoje "engine" é só um pick no modal sem efeito/custo.
- **Tamanho de jogo:** hoje todos abertos; GDD 3.2 limita escopo por fase (Garage=Pequeno, Indie=Peq/Médio, Escr/Estúdio=Todos) — além do unlock por P&D que o dev quer.
- **Custo fixo por fase:** já existe `EconomyBalance.WeeklyOperatingFor(phase)` — VERIFICAR valores (garagem barata → estúdio caro) e exibir no HUD.
- **Nomes amigáveis:** displayName é LocalizedString não resolvido (sem tabelas) → UI cai no slug. JSONs têm displayNamePtBr. Fix: campo string displayNamePt no SO + importer + UI.

### Backlog (ordem)
- [x] **M1 Menu + boot** — MainMenu.unity (1ª no build), `GameBoot` (intent), `MenuController`, GameController.Start lê intent (não auto-inicia), SaveManager.PeekCompanyName. Compila 0 erros. Falta verificação visual do menu.
- [ ] **M2 Nomes amigáveis** — displayNamePt nos SO + importer + UI (HUD modal e labels).
- [ ] **M3 P&D/Pesquisa + unlocks** — ResearchState/Manager (PP, set de desbloqueados), gating de tema/tamanho/engine/plataforma no modal; geração de PP; custo semanal de pesquisa.
- [ ] **M4 Engine (EB) + custo de dev** — módulos, EB, $/semana durante dev; amarrar ao loop econômico.
- [ ] **M5 HUD cantos + contextual** — bolinhas por área, barra de vendas, barra de produção.
- [ ] **M6 Personagem Blender** — dev sentado (corrigir cadeira), anims digitar/ocioso/cochilo, bolinhas de ponto.
- [ ] Verificação visual (build+screenshot) de menu e HUD.

### Decisões tomadas (dev valida depois)
- Menu: fluxo via `GameBoot` estático + `SceneManager.LoadScene("GameOffice")` (slice) em vez do Boot-persistente aditivo (fica p/ depois; não bloqueia). GameController ganha fallback de dev (abrir GameOffice direto no editor inicia run padrão).

### Progresso noturno (feito, com testes)
- **M2 ✅ Nomes amigáveis:** `DefinitionAsset.displayNamePt` + `DisplayLabel` (fallback prettify do slug). Importers (Genre/Theme) populam de `displayNamePtBr`. HUD e ProjectState usam `DisplayLabel` (mantendo slug como chave de compat). Conteúdo re-importado (Resources/Genres+Themes): assets agora têm "Action RPG", "Anos 80 / Retrô" etc.
- **M3 ✅ P&D/Pesquisa (backend):** `ResearchState` (PP + unlocked + pesquisa ativa, por run) em `RunState`. `ResearchCatalog` (nós de tema = básico/Garage; tamanhos Médio=interm/Indie, Grande/AAA=avançado/Office/Studio). `ResearchManager` (PP: +2/sem ativa, +5/jogo, +3/sem estudo; custo semanal no caixa; desbloqueio por duração 2/4/8 sem; seed de 2 temas iniciais; Pequeno sempre livre). Eventos `ResearchPoints/Started/Completed`. Ligado ao GameController (Research prop + Bind) e à cena (`GameSceneEnsure` adiciona o componente). Modal gateado: só temas e tamanhos desbloqueados aparecem. **Testes EditMode 42/42 (4 novos de P&D).**
- **Custo fixo por fase:** já existia e escala certo (`EconomyBalance`: garagem 300, indie 1.200, office 4.000, studio 12.000 / semana). Economia já cobra salários + overhead + pesquisa. **Falta: engine (EB+$/sem) p/ fechar "gastar pra desenvolver" (M4).**

### Pendências de UI do P&D (próximo)
- UI de P&D no HUD pra gastar PP e iniciar pesquisa (hoje só backend + seed). Sem ela, o jogador fica nos 2 temas iniciais + Pequeno. Entra junto do HUD novo (M5).

## Sessão 2026-11-04 (tarde) — Arte do interior (estilo ithappy) + editor de fase

- **Personagem dev v2:** reescrito `Tools/Blender/gen_character.py` — saiu do "cúbico": cabeça esférica, tronco oval afunilado, membros cilíndricos low-poly. Re-exporta `Assets/Art/Characters/Dev/Dev.fbx`. (Pendente a pedido do dev: avaliar usar o pacote "Free Office Characters" da Asset Store como base — ver bloqueio abaixo.)
- **Biblioteca de móveis v2 (24 peças) estilo ithappy:** `Tools/Blender/gen_furniture.py` reescrito — paleta clara/saturada (teal, coral, mostarda, sage, creme, madeira), geometria arredondada (chanfro 0.02/3 + primitivas esféricas/cilíndricas). Peças: Workstation, Desk, CoffeeTable, OfficeChair, GamingChair, Sofa, Armchair, Beanbag, Bookshelf (livros coloridos), ServerRack, MiniFridge, CardboardBox, Monitor, Plant, PlantSmall, FloorLamp, WaterCooler, Arcade, Whiteboard, Rug, RugRound, Poster, WallShelf, Clock. Preview claro em `Tools/Blender/furniture_preview.png` (view transform Standard — o AgX do Blender desaturava e não representava a cor real do asset).
- **Wiring no Unity:** `Office3DSetup.CreateFurnitureDefs` estendido p/ as 24 peças (categoria, footprint, rotatable, efeito de gameplay por peça). Novo `RebuildFurnitureAndLayout` (não reconstrói a cena → preserva managers). Layout da Garage (`Resources/Office/GarageLayout.asset`) refeito com 23 peças (zona de trabalho + armazenamento/P&D + lounge + parede). 24 `FurnitureDef3DSO` criados headless (editor fechado). Compila 0 erros (44 assemblies).
- **Screenshot headless in-engine:** novo `Assets/Scripts/Editor/CI/OfficeShot.cs` — monta o layout via `OfficeBuilder`, câmera iso + `OfficeLighting`, renderiza via URP `SubmitRenderRequest` → PNG. Bug corrigido: `NewScene(Single)` descarregava o layout carregado por `Resources.Load` (fake-null) → criar a cena ANTES do load. Resultado: garagem mobiliada renderizada no motor, visual batendo com a referência ithappy.

### Bloqueio: download do pacote de personagens da Asset Store
- A Unity Asset Store **não** permite download anônimo: o pacote precisa ser **"Add to My Assets"** (grátis) na conta Unity do dev e baixado pelo **Package Manager → My Assets** dentro do editor. Chrome não estava conectado nesta sessão; e mesmo conectado, o import final é pela conta/editor do dev. **Ação do dev:** logar na conta Unity, Add to My Assets no pacote, e Package Manager → My Assets → Download/Import. Depois eu uso os personagens direto (são rigados) e modelo peças complementares no mesmo estilo (sem copiar malha — só referência de estilo, por direito autoral).

### Próximo
- Afinar cores de chão/parede da Garage (mais claro/limpo, por fase) e resolver sobreposições do layout.
- Pôr o personagem sentado na cadeira na cena + animações (digitar/ocioso/cochilo) e bolinhas de ponto saindo da cabeça.
- UI de P&D no HUD + engine (EB+$/sem) — M4/M5.

## Sessão 2026-11-04 (noite) — Rotação de encaixe + iluminação embutida

- **Rotação pra encaixar:** cada peça tem `rotationSteps` (0..3 = 0/90/180/270°) autorável no layout e no editor de fase ("Girar 90°"). Calibrado via `OfficeShot.CaptureCalib` (peça em rot 0/1/2/3 lado a lado): estação rot0 = telas voltadas à câmera; cadeira rot2 = encara os monitores. Layout da Garage reautorado (cadeiras de frente pras telas, lounge, armazenamento). Peça de parede já auto-orienta no `OfficeBuilder` (encosta na parede do fundo/esquerda).
- **Iluminação embutida:** `FurnitureDef3DSO.lights` (array de `FurnitureLight`: localPosition, color, intensity, range) — point lights filhas que **giram com a peça**. `OfficeBuilder.AttachLights` instancia (sem sombra, ForcePixel). Atribuídas via `Office3DSetup.SetLights` a: floor_lamp, desk_lamp, light_point, workstation, monitor, arcade, server_rack. Editáveis no Inspector do def.
- **Ponto de luz avulso:** nova peça `light_point` (FBX `LightPoint` — orbe + point light quente) que o dev planta em qualquer célula no editor. Também `desk_lamp`. 26 defs agora.
- **Superfícies "acesas":** telas/abajures/lâmpadas/LEDs viram material **Unlit** plano na cor certa (tela azul, abajur quente) em `OfficeBuilder.Polished` — fica sempre "ligado". (Tentativa anterior com emissão HDR na Lit importada do FBX renderizava magenta/verde — abandonada.)
- Compila 0 erros. Render in-engine confirmado: halos de luz nas paredes/chão, telas azuis, cadeiras encarando os monitores.

## Sessão 2026-11-04 (noite 2) — Luz física por tipo

- `FurnitureLight` ganhou `spot` + `spotAngle` + `spotDirection`. `OfficeBuilder.AttachLights`: spot (luminárias) aponta pra baixo (cone, sombra suave), point (teto/LED) abrangente.
- **Telas:** luz movida pra FRENTE da tela (+Z local) — ilumina quem senta, não as costas. Tela inteira = material Unlit "aceso".
- **Luminária/abajur:** vira spot pra baixo (poça no chão) + corpo unlit "vaza" a cor. floor_lamp 95°, desk_lamp 85°.
- **Ponto de luz:** point abrangente e alto (y1.8, range 7.5) = luz de ambiente/"teto".
- Arcade/monitor/server com luz à frente. Render in-engine confirma: poças de luz corretas, telas iluminando pra frente.

## Sessão 2026-11-04 (noite 3) — Luz modelada NO prefab (não na cena)

- Correção de arquitetura (apontada pelo dev): a luz estava sendo injetada pela cena (`OfficeBuilder.AttachLights`) em vez de pertencer ao objeto.
- Novo `Office3DSetup.BuildFurniturePrefabs` (menu "Office 3D → 4"): para cada peça que emite luz, cria um **prefab** em `Assets/Art/FurniturePrefabs/` envolvendo o FBX, trocando telas/abajures por material glow **asset** (`Screen_Glow.mat`, `Warm_Glow.mat` em Assets/Art/Materials), e adicionando a(s) **luz(es) como filhas modeladas na peça** (a partir de `def.Lights`). O def passa a apontar para o prefab (não o FBX cru). 7 prefabs: Workstation, Monitor, FloorLamp, DeskLamp, LightPoint, Arcade, ServerRack.
- `OfficeBuilder`: só injeta luz via dados se a peça NÃO tiver luz própria (fallback); respeita material Unlit vindo do prefab (não re-swapa). Agora o builder só instancia o prefab — a luz viaja com o objeto e é editável abrindo o prefab.
- Render in-engine idêntico (luzes do prefab, sem duplicar). Compila 0 erros.

## Sessão 2026-11-04 (noite 4) — Glow real (bloom) + verificação em Play

- Glow material confirmado em TODAS as peças: telas (Workstation/Monitor/Arcade → Screen_Glow), cúpulas/lâmpada (FloorLamp/DeskLamp/LightPoint → Warm_Glow). Materiais em HDR (>1) p/ o bloom pegar.
- **Bloom:** novo `Editor/Setup/OfficeGlow.cs` — perfil `Assets/Settings/OfficeBloom.asset` (Bloom threshold 0.9, intensity 1.2, scatter 0.72), `ApplyBloom(cam)` liga post-processing na câmera + Volume global. `AddBloomToGameScene` injeta na cena GameOffice sem reconstruir. `GameLike.Editor` passou a referenciar `Unity.RenderPipelines.Core/Universal.Runtime`.
- **Pitfall importante:** render headless de editor (`cam.Render`/`SubmitRenderRequest` p/ RenderTexture em batchmode) **NÃO roda pós-processamento** — o bloom não aparece no `OfficeShot`. Verificação real = **build dev + `-glshot`** (StartupScreenshot usa `ScreenCapture.CaptureScreenshot`, que captura pós-pós-processamento). No Unity 6 o PostProcessData saiu do renderer (é global) — não precisa atribuir.
- Build dev OK (0 erros) + captura em Play confirmou: bloom nas telas dos monitores, fliperama ciano, luminárias com halo quente. HUD + nomes amigáveis funcionando.

## Sessão 2026-11-04 (noite 5) — Glow na tela + abajur correto + Map Builder (luz de teto)

- **Brilho na TELA (não poça na mesa):** materiais glow em HDR forte (Screen_Glow azul ~2.1, Warm_Glow quente ~2.0). Removidas as point lights das telas (workstation/monitor/arcade) — o brilho é a própria tela acesa + Bloom. Fim da poça esquisita na mesa.
- **Abajur de mesa redesenhado** (`gen_furniture.build_desk_lamp`): haste + braço pra frente + cúpula na ponta apontando PRA BAIXO. Luz embutida agora sai da CÚPULA (cabeça), não do pé. Princípio: o foco de luz de cada peça sai de onde fica a lâmpada dela.
- **Map Builder (OfficeLayoutWindow) estendido:** tamanho da sala (Larg×Prof = paredes) + cores de chão/parede no cabeçalho; abas **Mobília / Luz de teto**; no modo Luz, clicar numa célula põe/tira um ponto de luz de teto (☀). Contagem no rodapé. `OfficeLayoutSO.ceilingLights` (List<CeilingLight>: cell, color, intensity, range, dropFromCeiling); `OfficeBuilder.PlaceCeilingLight` instancia point light à altura da parede. Garagem agora tem 2 luzes de teto.
- Verificado em render headless: telas acesas, abajur jogando da cúpula, 2 poças de luz de teto do alto. Compila 0 erros.
- **Pendente:** posicionamento mais limpo do layout (referências ithappy: fileiras de mesas alinhadas, janelas que brilham) — o dev vai arranjar no builder; e personagem sentado + animações.

## Sessão 2026-11-04 (noite 6) — Map builder: janelas + layout organizado
- Peça **Janela** (`gen_furniture.build_window`): moldura branca + vidro aceso (material 'pane' -> `Window_Glow.mat` branco HDR). Wall fixture (auto-orienta na parede). Luz de dia suave entrando (`SetLights("window")`). 8º prefab com luz. No build: janelas brilham com bloom, como nas referências.
- **Layout da garagem reorganizado:** parede do fundo = pôster/janela/quadro/janela/relógio; mesas em fileira com cadeiras de frente; lounge no canto; armazenamento na parede direita; 2 luzes de teto.
- Map builder (OfficeLayoutWindow) agora cobre: tamanho da sala (paredes), cores chão/parede, Mobília/Luz de teto, girar/posicionar/remover, preview. Janela entra como peça de parede na paleta.

## Sessão 2026-11-04 (noite 7) — HUD: produção + círculos por área + barra de vendas
- Painel PRODUÇÃO: barra de **Progresso** (progress01) + **Semana X/Y** (weeksInDevelopment/targetWeeks).
- **5 círculos por área de dev** (Design/Arte/Áudio/Tech/Conteúdo) em `area-circles`, construídos em `BuildAreaCircles`; enchem tipo líquido (height%) = progress01 × (alloc[i]/maxAlloc). CSS em HUD.uss (.area-circle, .area-circle-fill, .area-circle-label/cap).
- **Barra orçamento × vendas** no modal de resultado (`res-budget-fill`/`res-sales-fill`): orçamento = devCostSpent+marketingSpent, vendas = revenue, escaladas ao maior. Preenchida em `OnLaunch`.
- Compila 0 erros. Verificado no build: painel mostra barra 25%, Semana 2/8, 5 círculos enchendo.
- Pitfall: já existia `AreaNames` em HudController (modal novo projeto) — usei `AreaCircleNames`/`AreaCircleInitials`.

## Sessão 2026-11-04 (noite 8) — HUD remontado no design aprovado (HUD 5 cantos)
- O dev apontou que o HUD não batia com o mockup aprovado (artifact "HUD 5 + gráfico"). Remontado:
- Layout de **cantos, centro livre**, cards claros flutuando: sup-esq stats compactos + flask P&D (chip-research-v); sup-dir dinheiro; inf-esq card do projeto (título, sub, barra de progresso, 5 círculos por área, hype, marketing, **sparkline Caixa/semana**); inf-dir botões (.abtn). HUD.uxml reescrito (nomes de elementos preservados p/ o controller), HUD.uss layout trocado por tema claro em cantos (modais mantidos).
- **Sparkline** desenhado via `generateVisualContent`/`painter2D` (área + linha) sobre histórico de caixa (ring buffer 16 semanas, amostrado 1x/semana em `SampleCash`); delta ▲/▼ colorido. Flask P&D lê `_gc.Research.Points`.
- Verificado no build: cara igual ao mockup. Compila 0 erros.

### Dúvidas acumuladas p/ o dev (menu ainda pendente)
1. Gráfico do card: fiz "Caixa/semana" (sempre tem dado); o mockup dizia "Vendas/semana" (só após lançar). Trocar p/ vendas, manter caixa, ou alternar (Vendas/Hype/Caixa)?
2. Menu Game Boy 3D: joystick-segue-mouse + botões diamante + fogo são interativos/VFX. Faço 1ª versão (modelo + tela com nome da empresa + botões clicáveis) e deixo fogo/joystick como polimento?
3. Map builder: quer edição clicando direto na cena 3D, ou a grade atual basta?

## Sessão 2026-11-04 (noite 10) — Menu Game Boy 3D (1ª versão)
- Decisões do dev: gráfico alternável (feito); menu igual ao mockup em 3D (pode ajustar modelo); map builder arrastar+clicar (a fazer).
- `GameBoyMenuController` (GameLike.UI): tela mostra nome da última empresa (TextMesh), 4 botões diamante = opções (Top=Novo, Right=Continuar, Bottom=Sair, Left=Opções) clicáveis por raycast + afundam; joystick inclina seguindo o mouse (Input System novo — `Mouse.current`); "calor" pela velocidade do mouse -> partículas de fogo + point light + leve escala (Super Saiyajin). Rótulos 3D por botão. `activeInputHandler=1` (só Input System) -> GameLike.UI.asmdef passou a referenciar Unity.InputSystem.
- `GameBoyMenuSetup` (editor, menu Tools→GameLike→Menu→★ Build Game Boy Menu): monta MainMenu.unity com câmera 3/4, luz, ParticleSystem de fogo, colliders nos botões, liga refs no controller, define como 1ª cena do build.
- `BatchBuild`: flag `-menuFirst` abre no menu (p/ screenshot); senão abre no escritório. Ambas as cenas no build.
- Pendente de calibração visual (após 1º render): eixos de inclinação do joystick, direção de "afundar" do botão, lado Z da câmera (face -Y Blender), orientação dos textos.

## Sessão 2026-11-04 (noite 11) — Menu Game Boy remodelado p/ o mockup
- Dev: o 1º modelo não batia com o design (handheld.html) e o menu estava nos botões, não na tela. Corrigido:
- `gen_gameboy.py` v2: corpo creme clássico, LED vermelho de power, bezel escuro, D-pad (cruz), botões A (verde) / B (vermelho), start/select — igual ao mockup (sem os 4 diamantes).
- `GameBoyMenuController` v2: o **menu fica DENTRO da tela** — logo "THE GAME LIKE", lista de opções (Novo jogo/Continuar/Opções/Sair) com destaque teal na selecionada, nome da empresa, e joystick na própria tela (knob) que segue o mouse. Seleção acompanha o mouse (raycast na tela), clique confirma, A/B afundam, fogo pela velocidade do mouse. TextMesh na tela (characterSize calibrado).
- **Pitfall crítico (framing):** escala ×5 no root do prefab FBX do Game Boy NÃO era aplicada ao render (só ao transform/bounds) — device renderizava ×1. Solução: escala 1 + câmera ORTOGRÁFICA enquadrada pela bounds real (orthographicSize). Câmera perspectiva + WorldToScreenPoint divergiam do render; ortho resolveu.
- Verificado no build: menu igual ao mockup.
### Polir depois: tela um pouco mais escura (mockup #13322f), re-add "funde sua dev", calibrar fogo/tilt, posição do knob vs nome da empresa.

## Sessão 2026-11-05 — Menu Game Boy: diagonal, joystick, balanço, fundo, bloom, marca
- Ajustes do dev: vista 3/4 (diagonal), device balança com o mouse, pad -> joystick (inclina na direção do mouse), fundo com padrão radial ("zebrado em círculo"), iluminação/shader melhores, bloom na tela, nome "THE GAME LIKE" acima.
- `gen_gameboy.py`: D-pad -> GB_Joystick (base+stick+ball). Controller: joystick inclina pro mouse, device balança (SwayDevice), conteúdo da tela em espaço LOCAL (segue tilt/diagonal).
- **Pitfall:** com `bake_space_transform` no FBX, o frame local da tela fica world-alinhado (altura=Y, não Z) e **espelhado em X** -> texto saía de ponta-cabeça/espelhado. Fix: LocalPoint usa (fx*halfX, fy*halfY, out), FaceRot=identity, e localScale.x=-1 no texto p/ desespelhar.
- Setup: câmera ortográfica diagonal (Euler 15,202,0), luz key+fill+rim, `ApplyScreenGlow` (unlit teal HDR), `BuildBackground` (textura radial zebrada gerada), `BuildBrand` (marca acima), `OfficeGlow.ApplyBloom(cam)`.
- Verificado no build: menu diagonal com tudo legível.
### Polir: destaque da opção selecionada contrasta pouco com a tela teal; bloom da tela sutil; padrão do fundo pode ser mais visível.

## Visão do jogo (áudio do dev 2026-11-05) — alinhamento p/ GDD/roadmap
- MENU: Game Boy gamificado. Fundo chamativo "zebrado em círculo" PASTEL/CARTOON (cores chapadas, não realista), igual à vibe do mockup CSS/HTML. Painel/tela muito semelhante ao mockup, com GLOW de tela (brilho de videogame ligado). Ao mexer o mouse, saem CHAMAS CARTOON (não textura realista — estilo cartoon, talvez shader). Fundo também cartunesco/pastel.
- GAMEPLAY (futuro, não agora): criar empresa (nome). Depois criação simples de personagem (blusa + cabeça + complemento de cabeça; raso, não é o foco). Começa com pouco dinheiro, ~4 gêneros aleatórios desbloqueados, todos os estilos (RPG etc.) MENOS fusões, plataformas limitadas. Consoles vão lançando com os anos (histórico real, já no GDD). Desenvolver jogos gera PP (pontos de pesquisa). Gastar PP p/ pesquisar/desbloquear gêneros (o personagem pesquisa, gasta PP). Mais pra frente: construção de ENGINES — desbloqueia features (ex: áudio surround), monta a versão da engine com as features, e ao desenvolver um jogo escolhe ativar a feature (gasta recurso). MARKETING não vem desbloqueado nem aparece como está — vira um BOTÃO à parte p/ campanhas. Tudo isso a ajustar depois.

## Sessão 2026-11-05 (cont) — Menu: modelo arredondado + tela escura + chamas cartoon + fundo pastel
- Dev: modelo muito quadrado; pediu mais arredondado (olhei referências de Game Boy/clay/Game Boy Color no navegador). `gen_gameboy.py`: corpo com bevel de apoio + **Subdivision Surface level 2** (cantos bem suaves), controles com subsurf level 1. Shading suave.
- Tela **escura** (URP Lit, base escura + emissão teal suave = LCD ligado). Opções nítidas (mint/branco), destaque HDR.
- **Fundo cartoon pastel:** sunburst radial (textura gerada, 30 fatias, periwinkle/lilás). Não realista.
- **Chamas cartoon:** sprite de chama gerado (silhueta, borda nítida), ParticleSystem alpha-blend, cores chapadas amarelo->laranja->vermelho, pequenas, saindo da BASE/atrás (não cobrem a tela). Controlado por "calor" (velocidade do mouse) + brasinha idle.
- `OfficeGlow.ApplyBloom(cam)` no menu. Verificado no build.
### Tunáveis: marca "THE GAME LIKE" lavada no fundo pastel (escurecer/contorno); glow da tela sutil; chamas só no idle baixo (cheias no mouse-move).

## 2026-11-05 — Opções de modelo + fogo (aguardando escolha do David)
Pedido: gerar 4 variantes do modelo 3D do Game Boy e 4 designs de fogo (fogo saindo de TRÁS, não na frente). Decisão de design: "THE GAME LIKE" em BAIXO RELEVO (côncavo) no topo da carcaça.
- `Tools/Blender/gen_gameboy.py`: novo `engrave_text(body, text, z, size, front_y, depth)` — grava via boolean DIFFERENCE um sólido de texto que atravessa a superfície frontal real (`-D/2`, não o constante `FACE_Y` — senão variantes finas perdiam a gravação). Rotação +90° X deixa o texto virado p/ câmera, lendo correto (sem espelhar). Helper `apply_all_mods`.
- `scratchpad/gen_options.py` (gerador de folhas de review, headless Blender): 4 variantes (1 Creme clássico / 2 Gordinho menta / 3 Slim grafite / 4 Pillow pêssego) + 4 fogos cartoon de EMISSÃO chapada (A Labareda quente / B Chamas baixas / C Comic pontudo / D Fogo mágico azul), todos emitindo por trás (+Y). Tiling 2x2 via numpy -> `options_models.png` / `options_fire.png`. Enviados ao David.
- Pendente escolha "modelo + fogo". Ao escolher: gravar o modelo no `gen_gameboy.py` definitivo e traduzir o fogo para o ParticleSystem animado do menu (não render estático). Refino conhecido: respingo de sombra do boolean em volta das letras (malha densa); contorno "comic" (C) empastado.

## 2026-11-05 (cont.) — Modelo do Game Boy detalhado (ref. DMG) + gravura
Fogo A (labareda coral) escolhido pelo David. Gravura "THE GAME LIKE" refeita (pequena/limpa): boolean em malha subsurf aplicada + `shade_smooth_by_angle` mata o respingo. Assada no modelo real (`gen_gameboy.build()`), FBX regenerado.
Detalhamento seguindo foto de referência do DMG enviada pelo David:
- `engrave_text` ganhou param `x` (gravar fora do centro) -> letras **A/B** ao lado dos botões e **SELECT/START** sob os pills.
- `cut_screen_well` (nicho rebaixado com cantos arredondados) + `make_bezel_frame` (anel limpo em malha própria que COBRE a borda serrilhada do nicho; serrilhado é inerente a boolean sobre subsurf denso).
- `cut_speaker_slits`: ranhuras diagonais (bevel nas pontas/paredes, recorte raso) movidas pra dentro (saíram da beira).
- Botões A/B em diagonal; Start/Select agora são pills salientes separados; LED de bateria no canto da tela.
- Corpo mais arredondado (bevel 0.024 + subsurf 2) p/ "menos chapado".
- Export inclui GB_ScreenBezel/GB_Battery/GB_Start/GB_Select (estáticos); controller continua achando GB_Screen/Joystick/BtnA/BtnB.
Pendência menor: leve textura na borda interna da tela (teal x bezel). Próximo: traduzir fogo A pro ParticleSystem (coral, atrás) e validar o menu em build.

## 2026-11-05 (cont.) — Fogo A no menu + captura via EDITOR (sem build)
- `GameBoyMenuSetup.ConfigureFire`: paleta "Labareda quente" (A) amarelo->laranja->coral; chamas mais altas/largas (startSize 0.06-0.14, lifetime 0.62, gravityModifier -0.45). Emissor `Fire` rotacionado Euler(-90,0,0) emitindo p/ CIMA, posição (0,0.02,-0.10) = atrás do console. Rate controlado por "calor" (velocidade do mouse) no controller.
- Ajustes do modelo desta rodada (aprovado pelo David): corpo mais grosso (D=0.046, FACE_Y=-0.021), bezel da tela mais fino, botões A/B e bola do joystick menores, risco de junção lateral puxado p/ frente (visível), tela maior cobrindo a borda do nicho.
- **Captura pelo editor (novo, mais rápido que build):** `Editor/CI/MenuShot.cs` abre MainMenu + EnterPlaymode (watchdog 60s). `StartupScreenshot` ganhou ramo de MENU (sem GameController): emite rajada no PS "Fire" p/ o fogo aparecer no print, captura e `EditorApplication.Exit(0)`. IMPORTANTE: rodar o editor SEM `-batchmode` (com janela real) — em -batchmode não há game view e o ScreenCapture sai vazio. Comando: `Unity -projectPath ... -executeMethod GameLike.Editor.CI.MenuShot.Capture -glshot <png> -logFile -` (sem -quit).
- Print validado: modelo novo + fogo coral atrás subindo + menu na tela + bloom. Pendências a decidir com David: (1) deixar labaredas mais coesas/verticais; (2) remover o "THE GAME LIKE" flutuante (hoje aparece 3x: flutuante + gravado + na tela).

## 2026-11-05 (cont.) — Menu: maior, outro lado, fogo vertical, polish + VÍDEO
- Câmera: console BEM maior (orthoSize factor 1.45->1.08) e diagonal espelhada (camRot Y 202->158, key light acompanhou).
- Fogo mais VERTICAL/coeso: renderMode Stretch (lengthScale 2.6), coluna estreita (shape 0.17x0.06x0.04), gravityModifier -0.7, quase sem rotation. Paleta A (coral) mantida.
- Removido o "THE GAME LIKE" flutuante (BuildBrand) — o nome já está gravado no modelo e na tela.
- Polish de luz/shaders: key 1.5 quente + rim 0.6 atrás (contorno sem lavar) + fill frio; glow da tela em emissão HDR teal (bloom); botões/joystick glossy (smoothness 0.78) e carcaça fosca (0.32); heat light laranja mais perto (calor na tela). Fogo leve HDR (1.2) p/ brilhar sem virar borrão.
- **Pipeline de VÍDEO pelo editor (sem build):** `UI/MenuVideoCapture.cs` (arg `-glvideo <dir>`) dirige um mouse sintético (`GameBoyMenuController.SetExternalMouse`) e grava 110 PNGs via ScreenCapture.CaptureScreenshotAsTexture. Rodar editor SEM -batchmode. Encode: sem ffmpeg e Blender sem FFMPEG -> GIF via .NET/WIC `scratchpad/make_gif.ps1` (GifBitmapEncoder + patch manual de GCE/delay + NETSCAPE loop). Saída menu_demo*.gif enviada ao David.

## 2026-11-05 (cont.) — Auditoria do estado real + início de Plataformas (rumo à v1)
**Correção importante:** auditei o código e o jogo está MUITO mais avançado do que o agents.md marcava.
- UI do loop (Milestone A) e boa parte de B JÁ FEITOS: `HudController` (951 linhas) + `HUD.uxml` = HUD interativa completa (topo com fase/equipe/jogos/rep/PP/caixa/valuation/data/sparkline; painel de produção; campanhas de marketing; barra de ações; modais de novo projeto/RH/resultado/evento). Cena `GameOffice` costura GameController+managers+HUD+escritório 3D. Menu→GameOffice funciona (ambas no build settings). Pesquisa/PP destrava temas e tamanhos.
- Capturei a cena de gameplay (`scratchpad/gameoffice_hud.png`) via pipeline de editor (MenuShot agora aceita `-glscene`): jogo rodando, HUD + office 3D + personagem na mesa.
- `ROADMAP_V1.md` reescrito com o estado auditado. Gaps reais p/ v1: (1) **Plataformas/Consoles** (não existem — maior gap), (2) fechar gasto de PP na pesquisa, (3) conteúdo+ícones, (4) balance, (5) fluxo/fecho, (6) polish.
- **Começado (1) Plataformas/Consoles** (GDD seção 15, source of truth, timeline completa Game Poy ano3 ... Steambox ano31 + eventos crash/boom): `Data/Definitions/PlatformCategory.cs` (PC/Console/Handheld/Mobile/VR) e `Data/Definitions/ConsoleSO.cs` (releaseYear, licenseCost, initialPopularity/peakPopularity/lifespan/popularityCurve, IsAvailable, PopularityAtYear). Falta: ConsoleContentDto+Importer, JSONs da timeline, integração no modal de projeto + efeito em vendas + disponibilidade por ano.

## 2026-11-05 (cont.) — Plataformas/Consoles implementado (GDD 15) — o maior gap de v1
Sessão: dev pediu "retoma e vai trabalhando". Peguei o maior gap do ROADMAP_V1 (Plataformas) e implementei ponta a ponta. **Pitfall da sessão:** o editor Unity estava aberto (lock) — uma instância de background sobrou de sessão ANTERIOR minha (PID com `-logFile` em scratchpad de outra sessão), ociosa e sem auto-refresh em background. Não deu p/ compile-check headless. Código revisado estaticamente; **verificação de compile/testes/visual fica pendente de liberar o lock** (fechar o editor de background ou focar p/ recompilar).

### Feito (código + conteúdo, revisado — falta compilar no editor)
- **`ConsoleSO`** (já existia de antes, mantido): `PlatformCategory` (PC/Console/Handheld/Mobile/VR), releaseYear, licenseCost, initial/peakPopularity, lifespan, popularityCurve; `IsAvailable(year)`, `PopularityAtYear(year)`.
- **Pipeline de conteúdo:** `ConsoleContentDto` + `ConsoleContentImporter` (menu `Import Content → Consoles`, grava em `Assets/Resources/Consoles`). **21 JSONs** da timeline do GDD §15.2 gerados em `Assets/Content/Consoles/` (PC ano1 → RealGlass ano51; popularidade das palavras do GDD convertida p/ 0..1). Integrado ao `BatchSetup.Run`.
- **Disponibilidade por ano:** `GameController.ConsolesAvailableNow()` lê `TimeManager.CurrentYear`, filtra e ordena por ano. Carrega consoles de `Resources/Consoles` no Awake (molde de genres/themes).
- **Efeito em vendas:** `SalesCalculator.PlatformReach(pop01)` = Lerp(0.5, 1.55) e novo param `platformReach01` em `Compute` (default 1 = neutro). `ProjectManager.Release` aplica o reach do console do projeto. Sem console = neutro (saves v1).
- **Licença + popularidade:** `GameController.StartProject` ganhou param `ConsoleSO console`; cobra `licenseCost` do caixa e captura `PopularityAtYear` no início → `ProjectState.consolePopularity01` (usado no lançamento).
- **UI:** modal de novo projeto ganhou seção **PLATAFORMA** (`np-consoles` no HUD.uxml) com botões por console disponível (nome + custo de licença); `HudController` monta via `BuildConsoleButtons()` ao abrir o modal, `SelectedConsole()` passa ao `StartProject`. Card do projeto mostra a plataforma (`ConsoleLabel`).
- **Save:** `ProjectState` ganhou `consoleId/Display/Key` + `consolePopularity01`. `SaveManager` schema **v1→v2** + migração trivial (projetos v1 ficam sem console = alcance neutro).
- **Testes:** `ConsoleSOTests` (disponibilidade/curva) + 5 casos de `PlatformReach`/reach em `SalesCalculatorTests`. Callers antigos de `StartProject` intactos (param opcional).

### VERIFICADO EM GAME (2026-11-05, via computer-use no editor aberto do dev) ✅ FASE 1 COMPLETA
O dev deu autonomia p/ mexer na tela. Rodei pelo editor:
- `Tools → GameLike → Import Content → Consoles` → **21 `.asset`** criados em `Resources/Consoles` com Id estável e campos corretos (pc: cat 0/ano1/$5000; game_poy: cat 1/ano3/$3000).
- Editor recompilou tudo com **0 erros CS** (DLLs gerados).
- Test Runner → Run All (EditMode): **50/50 passam** (EditMode.dll 34→43, +9 de console/PlatformReach), 0 falhas.
- Play: modal de novo projeto mostra **"PC ($5.000)"** na seção PLATAFORMA (só PC no Ano 1, correto); iniciar projeto **cobrou a licença** (caixa 49.400→42.600); card do projeto mostra a plataforma **"PC"**.
Fase 1 (Plataformas/Consoles) está completa, testada e rodando em game.

### 2026-11-05 (cont.) — Personagem na cadeira + build jogável nos Downloads
- **DevCharacter (personagem sentado):** novo `Editor/Setup/DevCharacterSetup.cs` (menu `Office 3D → 5 - Place Dev In Chair`). Monta o layout da garagem, acha a cadeira (office_chair) pela mesma matemática do `OfficeBuilder`, e posiciona o `Dev.fbx` (já modelado sentado, pivô nos pés) na célula, nomeado **`DevCharacter`** — que é o âncora das orbs (o `HudController` procura esse nome). Calibração por constantes no topo (ExtraYawSteps/Seat/Scale/ForwardNudge); ficou bom com rot 180° (encarando o monitor). Salvo em `GameOffice.unity`. Verificado em Play: dev sentado encarando o monitor, e **as orbs agora saem dele** rumo ao card de produção.
- **Build jogável:** `BatchBuild.BuildToDownloads` (menu `Build → Windows → Downloads (jogável)`) — release (sem watermark), abre no menu, grava em `C:/Users/david/Downloads/TheGameLike/`. **Build succeeded, 0 erros, ~105 MB.** `BuildDevForShot` (menu `Build → Dev gameplay (p/ screenshot)`) gera build dev que abre no gameplay p/ `-glshot`.
- **Pitfall:** `RevealInFinder` no fim do build abre um Explorer que rouba o foco; com o File Explorer negado no computer-use, o shell vira frontmost e BLOQUEIA todos os cliques (open_application não traz o Unity à frente em background mode). Além disso, o exe solto do build não é "app instalado" → computer-use MASCARA a janela. ⇒ Prints limpos fullscreen do build ficaram bloqueados nesta sessão; entreguei prints do Play (render idêntico). **Remover o RevealInFinder** ou não depender de foco do editor após build. Prints do build via `-glshot` (não precisa de computer-use) assim que o editor for focável.

### Próximo
1. ROADMAP (2): fechar gasto de PP na pesquisa (UI de P&D no HUD) — PEDIDO EXPLÍCITO DO DEV nesta sessão (mecânica central, sem ela o loop fica repetitivo). Implementar + verificar quando o editor estiver focável.
2. ROADMAP (3) conteúdo+ícones, (4) balance.
3. (Deferido) compat Gênero×Plataforma (GDD 18.8) + gating de plataforma por P&D.
4. Pendência antiga: pôr o `OfficeWorker` na cena `GameOffice` como `DevCharacter` (âncora das orbs) + validar captura.

## 2026-11-05 (noite) — Feedback amplo do David + protótipos de UI p/ escolha
David mandou áudio longo + print do menu CSS (handheld). Resumo: o **menu CSS 2D** ficou melhor que
o 3D atual → avaliar voltar pro 2D. Vários problemas de HUD/fluxo + pedido explícito de **mockups
in-game, várias variantes, p/ ele escolher** antes de eu implementar.

**Protótipos entregues (artifact HTML):** `scratchpad/mockups.html` publicado →
https://claude.ai/artifact/D2hhuJfkcDsxqQoBnyZJhT (slug 616a7e3f-eb7c-40f7-924f-13e819f65aae).
Tema claro/creme coerente com o chip "Garagem". Seções: Menu 2D (3 carcaças), HUD topo (3), Barra
de ações (3), Novo projeto (seletores laterais clicáveis: gênero=ícone / tema=imagem / tamanho PT /
plataforma c/ valor / custo total), Fases de dev (atributos por fase, ícones+cores, bolinhas, bugs,
hype), Lançamento (roleta de reviewers + vendas por semana), e um board das correções de lógica.
**Aguardando escolha do David** (menu A/B/C, topo A/B/C, ações A/B/C) antes de implementar no UI Toolkit.

### Correções de LÓGICA que ele apontou (tratar via GDD-first; registradas p/ implementar)
- **BUG Pesquisa passiva:** PP está sendo gerado ocioso. Só pode gerar ao **desenvolver jogo** ou
  ao **desbloquear item** — NUNCA parado. (Revisar `ResearchManager`: hoje +2/sem com pesquisa ativa
  + estudo — o "+/sem" ocioso tem que sair; PP só de produção/jogo lançado/desbloqueio.)
- **BUG Pausar não funciona** + ao iniciar projeto o tempo deve **pausar por padrão**.
- **Remover "avançar tempo"** da barra inferior.
- **Fases de desenvolvimento:** produção deixa de ser fase única. Fases sequenciais (Concepção→…);
  distribuir pontos **por fase**; mostrar **só os atributos da fase atual** (ícones+cor fixa por área),
  não o bloco fixo design/arte/áudio/tech/conteúdo. Conferir ordem/atributos no GDD.
- **Gênero ≠ Tema (reorganizar conteúdo):** gênero = estrutura (RPG, Action RPG, Souls-like, Aventura,
  Puzzle, Simulação, Estratégia, RTS, Tower Defense…) com ícone. Tema = ambientação (terror, futebol,
  carros, navios, espaço…) com imagem. Hoje "horror/shooter" estão como gênero — ERRADO. **Isto bate
  no GDD 18 (gêneros/temas) — parar e alinhar GDD antes de recodar o conteúdo.**
- **Bolinhas de bug:** entre as orbs geradas surgem bugs; fase de correção no fim; demora p/ lançar
  após pronto **reduz hype**. (Bug backend já existe — falta a mecânica de orb/fase de correção.)
- **Vendas ao longo do tempo:** pico no lançamento, decai por semana conforme a nota. (SalesCalculator
  já modela venda; verificar se está distribuído por semanas no HUD.)
- **Nota por várias casas:** média de avaliadoras reveladas uma a uma (roleta). Novo sistema de review.
- **Marketing desbloqueável:** "Anúncio" não fica sempre visível — vira botão de campanha após destravar.
- **Tamanho em pt-BR** (Pequeno/Médio/Grande/AAA), não inglês. Modal claro (não dark).
- **Áudio:** jogo mudo — etapa própria mais à frente.
- **Manter:** chip "Garagem", gráficos da cena 3D, PP no topo (referência de qualidade).

### Pendência de direção (decisão do David)
Menu: **voltar ao 2D CSS** (como o print) vs. seguir 3D. Os 3 mockups de menu são 2D; se ele
escolher um, troco o `GameBoyMenuController` 3D por uma tela 2D (UI Toolkit/UXML) — mais barato e
já aprovado por ele visualmente. **GDD-first:** a divergência gênero/tema precisa de edição do GDD
antes de mexer no conteúdo (`Content/Genres` tem horror/shooter/puzzle crus hoje).

## 2026-11-06 — Galeria de HUDs toggláveis no Unity (dev escolhe in-engine)
Dev pediu: parar de iterar em mockup web e montar os HUDs DENTRO do Unity como componentes
que ele liga/desliga na Hierarchy p/ comparar e escolher. Linguagem final decidida nos mockups:
diegética (GDT) **casada com o estilo real do jogo** (tokens do HUD.uss: card creme #fbf8f1 +
borda inferior 3px, teal #2a9d8f progresso, azul #2f74c9 ação, fonte neutra). **Sem emoji** —
ícones entram como sprites depois (nesta 1ª leva: legendas + cor nos círculos, como o GDT).

### Entregue (compila + importa limpo, verificado headless)
- **`Assets/UI/Gallery/Gallery.uss`** — estilos com os tokens reais do jogo.
- **11 UXML** em `Assets/UI/Gallery/`: Proj_A/B/C/D (painel do projeto no topo), Cmd_A/B/C (barra
  de comando), Dev_A/B/C (tela de desenvolvimento/fases), Menu_Creme (menu inicial 2D aprovado).
- **`Editor/Setup/HudGallerySetup.cs`** (menu **Tools → GameLike → UI Gallery → Build HUD Gallery
  Scene**): monta `Assets/Scenes/UIGallery.unity` com 4 grupos (pais vazios) e cada variante num
  GameObject filho com UIDocument (mesmo HudPanelSettings, sortingOrder por grupo). Default visível:
  Proj A + Cmd A (HUD de jogo composto). Dev/Menu começam desligados.
- Headless `HudGallerySetup.Build` rodou: **0 erros CS**, 11 UXML + USS importados sem warning,
  cena salva (27 KB). Unity 6000.3.10f1.

## 2026-11-06 — HUD animado fechado + Menu v1 escolhido + GARAGEM (assets 3D)
- **HUD animado é o principal** (David aprovou). Galeria limpa: `HudGallerySetup` agora só monta o
  `AnimatedHud` (gameplay) + 3 menus animados (`AnimatedMenu` variantes 0/1/2). As variações estáticas
  (Proj/Cmd/Dev/Menu_Creme UXML) foram **descartadas** (arquivos + Gallery.uss removidos).
- **Menu inicial: v1 (Console creme) escolhido.** `AnimatedMenu` variant 0 = handheld + sunburst pastel
  + menu na tela + LED + **console inclina seguindo o mouse** (parallax suave, PointerMove→lerp). Menu/UX
  congelados por ora (David: "não mexer mais nessa parte").
- **GARAGEM (1º cenário real):** 5 peças novas no gerador Blender `Tools/Blender/gen_furniture.py`:
  **Car** (carrinho low-poly caprichado: 2 níveis, vidros, para-choques cromados, faróis/lanternas, calotas),
  **Cabinet** (armário + caixas em cima), **OldDesk** (mesinha), **OldPC** (CRT bege + teclado + mouse),
  **OldChair** (cadeira de madeira). FBX exportados em `Assets/Art/Furniture/<Peça>/`. Preview:
  `Tools/Blender/furniture_preview.png`; close-up do carro em scratchpad.
- **Wiring Unity:** `Office3DSetup.CreateFurnitureDefs` ganhou os 5 `MakeDef` (car/cabinet/old_desk/old_pc/
  old_chair). `CreateGarageLayout` **refeito pra garagem HUMILDE** (8×7): carro estacionado à esquerda +
  cantinho (mesinha+PC+cadeira+tapete+planta+luminária) + armário com caixas no canto. (Antes era um
  escritório cheio — placeholder; agora é a garagem real.)
- **PENDENTE (precisa do editor):** rodar **Tools → GameLike → Office 3D → ★ Build Everything (3D)** (cria
  defs + layout) e **3 - Build Game Scene**, depois validar visual. Não rodei (editor aberto; sem foco p/
  computer-use e headless bloqueado pelo lock). PC-antigo pode precisar ser posto SOBRE a mesinha no editor
  de fases (grid põe no chão) — item de pitaco.

### Bug corrigido ao verificar in-engine (gotcha importante)
Ao abrir e dar Play, os 11 UIDocuments renderizavam NADA. Causa: `HudGallerySetup` carregava o
`HudPanelSettings` ANTES de `EditorSceneManager.NewScene(EmptyScene)` — e NewScene descarrega
assets não referenciados, deixando a ref de panel "fake-null"; as atribuições de `m_PanelSettings`
gravavam `{fileID: 0}` em silêncio (o UXML persistia porque era carregado DENTRO do loop, após o
NewScene). **Fix:** recarregar `panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath)`
logo APÓS o NewScene (igual o HudSetup faz). Verificado: scene agora grava
`m_PanelSettings {guid 058dc8cb...}` nos 11, e o HUD renderiza em Play (Proj A + Cmd A conferidos
via computer-use no editor do dev). Regra geral: refs de asset carregadas antes de trocar/criar
cena podem virar fake-null — recarregue depois.

## 2026-11-06 (cont.) — HUD ANIMADO aprovado + auras por círculo (Painter2D)
Dev escolheu (via pergunta) "direto no Unity com fonte de ícones". Entregue e **aprovado pelo David**
("ficou ótimo, muito bom"):
- **Fonte de ícones:** `Assets/UI/Fonts/fa-solid-900.ttf` (Font Awesome 6 Solid, baixado). Editor
  `IconFontSetup` (menu "UI Gallery → 1 - Create Icon Font Asset") gera FontAsset dinâmico em
  `Assets/UI/Fonts/Resources/IconFont.asset` (runtime carrega via `Resources.Load<FontAsset>("IconFont")`).
  Ícones viram Labels com o codepoint FA + `style.unityFontDefinition = new StyleFontDefinition(fontAsset)`.
- **`Assets/Scripts/UI/Gallery/AnimatedHud.cs`** (GameLike.UI): HUD 100% em código sobre o UIDocument,
  animado em `Update`. Círculos de recurso (Design/Arte/Áudio/Tech/Conteúdo) que **enchem** (líquido),
  barra de projeto segmentada, hype com flicker, barra de comando com ícones, pausa pulsando. A cada
  ponto: **burst** (onda de choque + "+1" flutuante + pop). Clicar num círculo dispara ponto (interativo).
  Adicionado à galeria como "0 - HUD ANIMADO (showcase)" (default ligado); estáticos Proj A/Cmd A desligados.
- **Auras por círculo (Painter2D)** — ajuste pedido pelo David, mantendo a base: Design=mira que fecha,
  Arte=mancha de tinta piscando, Áudio=equalizador radial, Tech=circuito com pulso de corrente,
  Conteúdo=camadas expandindo. Método `DrawAura(mgc, area)` por `generateVisualContent`, intensidade
  cresce com o preenchimento.

### Gotchas desta rodada (importantes)
- **"Build HUD Gallery Scene" NÃO pode rodar em Play mode** — `EditorSceneManager.NewScene` lança
  `InvalidOperationException` em play. Rodar sempre em edit mode. (Foi a causa de "não vejo nada": a
  cena na tela ficou inconsistente; a salva em disco estava certa — basta reabrir UIGallery + Play.)
- Warnings CS0618 em `AnimatedHud` (`VisualElement.transform` obsoleto) — funciona, mas migrar p/
  `style.scale`/`style.translate` quando sobrar tempo.
- Controle de tela (computer-use) travou: o shell do Windows fica frontmost (PowerToys/lockapp) e o
  David negou a permissão de clique na barra — não dá p/ apertar Play pelo agente nesse estado.
  Validação de compilação feita lendo `%LOCALAPPDATA%/Unity/Editor/Editor.log` (0 erros CS).
- **Pendência de validação visual das auras:** feito no código; falta ver em Play (David vai rodar).

### Como o dev usa / pendências
- Painéis runtime (UIDocument) só desenham no Game view em **Play mode** → abrir UIGallery, dar Play,
  e ligar/desligar os GameObjects p/ comparar. (Ou abrir cada .uxml no UI Builder p/ prévia estática.)
- **USS não tem** conic-gradient/box-shadow/clip-path → anéis viraram preenchimento de baixo p/ cima,
  sombra = borda inferior 3px, barra "chanfrada" (Persona) virou pílula arredondada. Fiel o possível.
- **Ícones:** próxima leva — gerar sprite set SVG→PNG e plugar nos círculos/botões (hoje legenda+cor).
- Ainda NÃO toquei no HUD de jogo real (HudController/HUD.uxml) nem nas correções de lógica — só a
  galeria de escolha. Ao David fechar as letras, aplico no HUD.uxml real + emendo nas correções.

## 2026-11-05 (cont.) — Asset de personagens importado (automatizado) + sistema de bolinhas
- **Pack de personagens importado 100% por mim** via computer-use (controle de tela): abri o editor, Package Manager → My Assets → "Free Office Characters Pack Lite" → Download → Import. Entrou em `Assets/OfficeCharactersPack/` (Meshes+Prefabs URP dos 4: OfficeWorker/Manager/Receptionist/Janitor, Humanoid + CharacterAnimator.controller). OfficeWorker = candidato ao dev na mesa. Troca é drop-in.
- **Sistema de bolinhas (orbs)** no `HudController`: orbs de progresso saem do dev (anchor) e voam em arco p/ a barra de produção enquanto desenvolve; orbs de PP disparam quando a pesquisa ganha pontos e voam p/ o chip de PP. Camada própria (`orb-layer`, pickingMode Ignore) por cima do HUD; animação por bézier quadrática no Update; mundo→painel via Camera.WorldToScreenPoint + RuntimePanelUtils.ScreenToPanel. Campo `workAnchor` (serializado) com fallback por nome ("WorkAnchor"/"DevCharacter") e fallback de tela. **Compila limpo (Console 0 erros/0 warnings).**
- Pendente p/ fechar: colocar o OfficeWorker na cena GameOffice como "DevCharacter" (= anchor das orbs) e validar com captura. `MenuShot` agora aceita `-glscene` p/ capturar qualquer cena.

## 2026-11-11/12 — Galeria completa de telas do GDD + feedback do David + virada p/ LOOP JOGÁVEL

### Telas (galeria) — FEITO
Análise do GDD → 23 telas novas na cena `UIGallery` (31 GameObjects com estados), todas em código sobre
UIDocument no padrão do `AnimatedHud` (kit compartilhado `GalleryKit`/`GalleryWidgets`/`GalleryScreen`,
host `GalleryScreenHost`, captura `GalleryCapture` via `-glgallery <pasta>` + `GalleryShot.Capture`).
Grupos: 1-Fora da run (slots, opções, novo fundador) · 2-Fluxo do projeto (conceito, sliders da fase,
marketing, lançamento, notas+vendas) · 3-Painéis (Financeiro, Projetos, RH×2, Mercado×2, P&D×2,
Relações×2, Fundador) · 4-Popups (evento, expansão, M&A×2, venda×2, pausa, avisos) · 5-Meta (resultado×2,
árvore de habilidades). David revisou no editor: "a maior parte tá muito legal".

### FEEDBACK VISUAL DO DAVID (2026-11-12) — GUARDADO PARA A FASE DE POLISH (não fazer agora)
Referências salvas em `References/UI/` (`popart_rank_burst_S.png`, `popart_onomatopeia_impact.png`).
1. **Faltam as partículas/"água"** que o HUD aprovado tem (barra enchendo com líquido ondulando, bolhas).
   Levar esse tratamento para as barras/medidores das telas novas (progresso, hype, EB, XP, etc.).
2. **Efeitos cartoon / pop-art estilo HQ** (ver referências: selo de rank "S" explodindo com raios e
   retícula; onomatopeia gigante "PHOOOM!" com estouro, halftone, contorno grosso). Usar em momentos-chave:
   nota muito boa → um "10" ESTOURANDO a tela/HUD com partículas; idem para venda, conquista, expansão,
   falência etc. A ideia é o HUD "estourar" na tela para celebrar.
3. **Menu inicial desconexo:** o menu é um Game Boy, mas Opções/outras telas "fogem" de dentro dele.
   Integrar: ao escolher uma opção (ex.: Opções), dar **zoom na tela do Game Boy** e o novo menu surge
   DENTRO da tela, como se o jogador tivesse entrado nela. Slots/novo fundador idem.
4. Depois do loop pronto: **planejar** (não só executar) esse HUD mais legal + tela inicial integrada.

### NOVO MANDATO (noite de 11→12/11): LOOP DE GAMEPLAY JOGÁVEL
- Transformar as telas em loop funcional seguindo o GDD, com parâmetros reais. Sem build; deixar tudo
  montado/salvo no Unity. Som provisório (qualquer efeito) para dar retorno.
- Trabalhar sem perguntar; decisões anotadas aqui. O David liberou controlar o editor aberto.

### Decisões de arquitetura desta fase
- **UI viva sem editar cena:** `GameUi` (assembly UI) nasce em runtime quando a cena tem `GameController`
  (RuntimeInitializeOnLoad) — nada de wiring manual no Inspector. Managers novos são garantidos por
  `GameController.Awake` (AddComponent se faltarem). ⇒ a cena `GameOffice` não precisa ser remontada.
- **Uma tela = um layout, duas fontes de dados:** cada tela da galeria ganhou um *modelo* (dados +
  callbacks). A galeria usa `Mock()`; o jogo monta o modelo a partir do estado real. UI não calcula regra.
- **HUD:** o `AnimatedHud` aprovado virou data-driven (`HudLive`): sem feed ele simula (galeria); com feed
  mostra o jogo real.
- **Validação:** editor do David aberto trava o projeto ⇒ espelho em `%TEMP%/glmirror/My project`
  (robocopy /MIR) para compilar/testar/capturar headless sem tocar no editor dele.

### ESTADO AO PARAR (2026-11-12 ~00:30, limite de uso atingido) — RETOMAR DAQUI
**Backend do loop: ESCRITO e COMPILANDO (espelho), 72/73 testes EditMode.**
- Novo: GameParameter/ParameterCatalog (19 parametros, 5 fases), ProjectManager em fases (alocacao por fase,
  pronto-para-lancar, correcao de bugs, Launch com 4 casas de critica, cauda de vendas semanal, preco),
  QualityCalculator.ComputeDetailed, SalesCalculator (PriceFactor/Tail/LaunchBudgetHype), ResearchManager
  (PP so em dev/lancamento/pesquisa concluida) + catalogo (engine/marketing/negocios) + EngineCalculator,
  MarketingManager (8 campanhas, desbloqueio por P&D, 1x por projeto), StaffManager (treinar/ferias/bonus,
  HireDirect), RivalManager + AcquisitionCalculator, TimelineManager, AchievementManager, SkillTreeCatalog +
  FounderPerks, LegacyCalculator.Breakdown, TimeManager (travas de tela, SetDate), GameController (expansao de
  fase paga + estagnacao, venda/IPO/oferta de rival, falencia com carencia de 4 semanas, fundador, save v3).
- **1 teste falhando:** GameLoopRulesTests.Project_RunsFivePhases_ThenWaitsForLaunch_ThenSellsOverWeeks —
  ver mensagem em scratchpad/tests_EditMode.xml (ainda nao investigado).
- PlayMode test antigo (GameLoopPlayTests) ainda nao foi rodado contra o backend novo.
**UI viva: NAO COMECADA** (so existe UI/Gallery/HudLive.cs com o contrato HudLive/HudActions).
Proximos passos, na ordem:
1. Corrigir o teste acima; rodar PlayMode.
2. AnimatedHud: modo vivo (Live != null) — o patch esta descrito na conversa (campos, card do projeto,
   UpdatePhaseLive, circulos por parametro, bugs/pesquisa/hype/dinheiro vindos do feed, barra de comando com
   callbacks + botoes dos paineis). O arquivo AnimatedHud.cs esta INTACTO (nenhum patch aplicado).
3. GameUi (runtime, sem editar cena): cria UIDocument com HudPanelSettings, liga HudLive ao GameController,
   abre as telas (modelos reais nas telas da galeria), trava o tempo em telas.
4. Som provisorio procedural (SfxPlayer).
5. Conteudo: tirar Horror de genero, Shooter -> FPS, novos generos.
6. So depois: plano do HUD pop-art + menu integrado ao Game Boy.
Validacao: scratchpad/mcompile.sh e mtest.sh (espelho em %TEMP%/glmirror). O editor do David esta aberto;
focar a janela faz ele recompilar. Nada foi commitado.

### PEDIDO DO DAVID (2026-11-12) — MAP BUILDER (fazer DEPOIS do loop jogavel)
Ferramenta para ELE montar os cenarios: escolher o tamanho da area; ver a lista completa de moveis; arrastar
para dentro, rotacionar e posicionar; um item para ATRELAR ASSENTOS (cadeiras onde o boneco senta e aparece),
com um jeito explicito de relacionar/especificar isso; a ferramenta gera o cenario pronto que fica de fundo.
Pode vir com um cenario de teste. Precisa de prefabs dos moveis. Base existente: OfficeLayoutWindow
(Level Builder 3D), OfficeLayoutSO/FurniturePlacement, OfficeBuilder — evoluir em vez de recomecar.

## 2026-11-12 — LOOP JOGAVEL NO JOGO REAL (cena GameOffice) — FEITO E VERIFICADO
**Verificado:** compila (0 erros), EditMode 73/73, PlayMode 2/2, e o piloto automatico (LoopAutoplay) jogou o
loop inteiro no Unity tirando 26 prints: fundador -> novo projeto -> 5 fases com sliders -> marketing ->
lancamento -> criticas na semana seguinte -> vendas semana a semana (grafico no HUD) -> 7 paineis -> expansao
de fase -> venda -> resultado -> arvore de habilidades -> nova run (empresa anterior vira rival).

### UI viva (Assets/Scripts/UI/Game)
- GameUi.cs: nasce sozinho em runtime quando a cena tem GameController (nao esta na cena, nao precisa de
  wiring). Cria 3 UIDocuments (HUD, Telas, Avisos) com Resources/HudPanelSettings (o asset foi MOVIDO para
  Assets/UI/Resources/ — os setups de editor ja apontam para o novo caminho). Alimenta o AnimatedHud aprovado
  via HudLive, abre as telas certas na hora certa, trava o tempo enquanto ha tela aberta, mostra avisos.
  Atalhos: Espaco = pausar, Esc = menu de pausa/fechar painel (InputAction, nao Input.GetKey).
- LiveFlow.cs (fundador, novo projeto, alocacao da fase, marketing, lancamento, criticas), LivePanels.cs
  (Financeiro, Projetos, RH, Mercado, P&D+Engine, Relacoes, Fundador), LivePopups.cs (evento, expansao,
  venda/oferta/IPO, M&A com absorcao, pausa), LiveMeta.cs (resultado da run, arvore de habilidades).
- HUD: barra de comando ganhou os 7 botoes de painel + menu; selo azul de alerta acima da barra quando o jogo
  pede algo (alocar fase, lancar, expansao, caixa negativo). Card de VENDAS sob o dinheiro: uma barra nova por
  semana ate o jogo parar de vender (pedido do David em 12/11).
- Sfx.cs: som PROVISORIO 100% sintetizado em runtime (sem arquivos): hover/clique, abrir/fechar, ponto de
  pesquisa, moeda por semana de venda, sucesso/erro, alerta, lancamento, nota, fanfarra, game over, desbloqueio.
  Volume no menu de pausa (PlayerPrefs sfxVolume).
- Relacoes (contratos/declaracoes do CEO): painel honesto "em construcao" — NAO ha sistema por tras ainda.
- As telas "vivas" sao classes novas que reusam o kit; as telas mock da galeria (UI/Gallery/Screens*.cs)
  continuam como vitrine de design. DUPLICACAO consciente: quando o David validar as vivas, apagar as mock
  (ou fazer a galeria instanciar as vivas com dados falsos).

### Regras que mudei a pedido do David (GDD secao 23 atualizada antes do codigo)
- Criticas saem na semana SEGUINTE ao lancamento; vendas comecam so depois, 1 semana por tick (GDD 23.4).
- PP so em desenvolvimento/lancamento/pesquisa concluida. Gênero x tema: Horror saiu de genero, Shooter -> FPS,
  entraram Plataforma e RPG (JSON em Content/Genres + compat nos 8 temas; importados e assets copiados).

### DECISOES PROVISORIAS PARA O DAVID VALIDAR
1. Valuation rebalanceado (ValuationCalculator): antes um unico jogo Pequeno nota 6,5 levava a R$ 531K e
   pulava a Garagem; agora ~R$ 235K (receita x0,5; reputacao x100K; nota media x8K; catalogo 10K/jogo).
2. Falencia com carencia: 4 semanas seguidas de caixa negativo (GameController.bankruptcyGraceWeeks). Antes
   era imediata. O HUD avisa a contagem.
3. IPO paga 1,5x o valuation (GDD nao define). Expansao: 40K / 150K / 1,5M; estagnacao 0,5%/sem ate 30%.
4. Engine simplificada: a empresa tem UMA engine = todos os modulos pesquisados (sem montar varias engines
   nem ligar recursos por fase). Comeca com 200 EB.
5. Arvore de habilidades: efeitos numericos concretos inventados (GDD so descreve qualitativamente); 6 perks
   marcados "Em breve" (sem sistema por tras) e nao compraveis. Raizes de Carisma/Networking destravam as
   opcoes [Carisma]/[Networking] dos eventos. Catalogo em codigo (SkillTreeCatalog), nao em SO.
6. Sequencias exigem a pesquisa Gestao de Franquias (fase Escritorio), como o GDD 13.2 manda — na pratica nao
   da para fazer sequencia na Garagem/Indie. Se quiser liberar antes, e mudar MinPhase no ResearchCatalog.
7. "Novo jogo" no menu zera a meta-progressao (GDD 20.1). Abrir a cena GameOffice direto no editor mantem a
   meta do save e comeca run nova.
8. Rivais: 3 NPCs fixos + ate 3 empresas de runs anteriores; ofertas de compra aparecem com valuation >= 120K.

### Ferramentas de validacao
- scratchpad/mcompile.sh [metodo] e mtest.sh EditMode|PlayMode: espelho em %TEMP%/glmirror (robocopy) para
  compilar/testar com o editor do David aberto. patch.sh: aplica patch perl sem nunca truncar o arquivo.
- LoopAutoplay (-glplay <pasta>) + Editor/CI/LoopShot.Run: teste de fumaca visual do loop (usa save slot 9).
- GOTCHA: perl s|..| com \|\| no padrao casa string vazia; perl > arquivo direto trunca se o script falhar.

### NAO FEITO / PROXIMO
- MAP BUILDER (pedido do David) — proximo item.
- Contratos e parcerias (GDD 9), declaracoes do CEO (10.3), pos-lancamento (DLC/port/promocao, 5.8), subgeneros
  (18.4), compat Genero x Plataforma (18.8), tutorial/tooltips (20.3), slots de save multiplos (20.6), opcoes
  de video/idioma, localizacao. O menu inicial (cena MainMenu, Game Boy 3D) nao foi tocado.
- Limpeza: HUD antigo e slice movidos para _Descartados/2026-11-12 (ver fim do arquivo).
- Polish visual (particulas de agua, pop-art, menu integrado ao Game Boy) — anotado acima, a planejar.

## 2026-11-12 — MAP BUILDER (pedido do David) — FEITO, compila, cenario de teste gerado
Menu: **Tools -> GameLike -> ★ Map Builder (montar cenarios)** (janela OfficeLayoutWindow reescrita).
- Area: fase do cenario + largura x profundidade em celulas (e botoes -/+); cores de chao/parede.
- Lista completa de moveis a esquerda (miniatura, tamanho, categoria), com busca e filtro por categoria.
  Clique arma a peca e clique na planta coloca; ou ARRASTE da lista para a planta. Fantasma verde/vermelho.
- Planta vista de cima (topo = parede do fundo, esquerda = parede lateral): clicar seleciona, arrastar move,
  R gira, Del remove, botao direito remove, Esc cancela. Marca branca na borda = frente da peca.
- **Assentos:** aba "Assentos" -> clique numa cadeira cria o assento. Ordem = quem senta onde: assento F e do
  fundador, #1 do 1o contratado, etc. (setas reordenam). Giro e ajuste fino por assento; campo do prefab do
  boneco; "Criar assentos em todas as cadeiras"; avisos (sem assento / menos assentos que a equipe da fase).
  Assento acompanha a cadeira ao mover e some se a cadeira for removida.
- Luz de teto: aba propria (clique poe/tira).
- "Preview na cena" monta o cenario pronto com os bonecos (slider de quantos); atualiza a cada mudanca.
- Dados: OfficeLayoutSO ganhou seats (SeatPoint) + characterPrefab. OfficeBuilder cria os transforms Seat_i;
  OfficeCrew (novo, runtime) senta o fundador (DevCharacter) e os funcionarios e refaz ao contratar/demitir.
- O jogo usa o cenario de Resources/Office cuja fase bate com a fase da empresa. Criados/atualizados:
  GarageLayout (assento do fundador na cadeira) e **IndieTestLayout** (cenario de TESTE: 3 estacoes + 3
  assentos). Menus: Office 3D -> "6 - Gerar cenario de teste" e "7 - Garantir assentos nos cenarios".
- Verificado no piloto automatico: fundador aparece sentado na garagem; ao expandir para Indie o cenario troca.
- NAO verificado: a janela do Map Builder em si foi so compilada (IMGUI precisa de teste manual: arrastar,
  mover, miniaturas). Os prints do piloto estao em Docs/Validacao/.
- Limitacao: prefabs = os FBX ja referenciados pelos FurnitureDef3DSO (32 moveis em Assets/Data/Furniture3D);
  nao criei prefabs novos. Cenarios Escritorio/Estudio ainda nao existem (o jogo cai no primeiro que achar).

## 2026-11-12 (tarde) — MAP BUILDER IN-GAME (pedido do David) — FEITO e verificado por roteiro automatico
Pedido: montar os cenarios DENTRO do jogo, vendo o 3D, movendo e girando as pecas de verdade, e salvar.
- **Como abrir:** opcao "Map Builder" no menu inicial (Game Boy; so editor/development build) ou
  Tools -> GameLike -> ★ Map Builder in-game (Play). Nao tem cena propria: `MapBuilderBoot.Open()` cria a cena em runtime.
- **O que faz:** mesma camera/luz/construtor do jogo (WYSIWYG). Paleta com miniaturas 3D, busca e filtro; clicar arma a
  peca (fantasma real + moldura verde/vermelha), clicar no chao coloca; clicar seleciona, arrastar move, R gira com
  animacao, Del remove, Ctrl+D duplica, Ctrl+Z/Y desfaz/refaz, Ctrl+S salva. Abas Assentos (clicar na cadeira) e Luzes
  (clicar na celula). Tamanho da sala, fase, cores de chao/parede. Vista: botao do meio arrasta, scroll zoom, Q/E gira
  90 graus, Home = enquadramento exato do jogo; G liga/desliga guias (grade, marcadores).
- **Onde salva:** `Assets/Resources/OfficeMaps/<nome>.json` (OfficeMapData, mobilia por Id estavel). Num build grava em
  persistentDataPath/OfficeMaps. O jogo usa, por fase, o cenario com "Usar no jogo" ligado (OfficeView -> OfficeMapStore).
- **Codigo:** UI/MapBuilder/* (Boot, Controller, Session, Stage, Camera, Input, Hud, FurnitureThumbnails, Autoshot);
  Systems/Office/OfficeMapStore + OfficeLayoutRules; Data/LevelBuilder/OfficeMapData + FurnitureCatalogSO;
  Editor/Office/FurnitureCatalogSync (catalogo automatico) e Editor/CI/MapBuilderShot. OfficeBuilder ganhou
  BuildShell/CreatePiece/PiecePose/BuildSeats/BuildCeilingLights publicos (Build continua igual).
- **Verificado (espelho):** compila, 85/85 testes EditMode (13 novos em OfficeMapTests), e o roteiro
  `MapBuilderShot.Run -glmapshot <pasta>` colocou sofa, selecionou, girou, colocou cadeira, criou assento, salvou e releu o
  arquivo (GAMELIKE_MAPBUILDER_SHOT_OK). Prints em Docs/Validacao/MapBuilder/.
- **NAO verificado por mim:** arrastar com o mouse de verdade, pan/zoom, janelas Abrir/Novo/Cores (so compiladas e
  revisadas) e o jogo carregando um cenario salvo numa partida real (coberto so por teste de ida-e-volta do JSON).

### DECISOES PROVISORIAS PARA O DAVID VALIDAR (Map Builder)
1. Cenario salvo em JSON (nao .asset): asset nao pode ser gravado em runtime/build. Alternativa: bake JSON -> OfficeLayoutSO.
2. JSON marcado "Usar no jogo" tem prioridade sobre os OfficeLayoutSO de Resources/Office. Os assets antigos aparecem em
   Abrir como "asset"; ao salvar viram JSON. A janela antiga do editor virou "Map Builder antigo (janela do editor)" —
   editar o asset por ela NAO muda o jogo se existir JSON em uso para a fase.
3. Opcao do menu so aparece no editor/development build (o GDD/CLAUDE.md dizem que o layout nao e mecanica do jogador).
4. Bloom do jogo nao esta no builder (o perfil e aplicado por script de editor na cena GameOffice) — brilhos de tela/abajur
   ficam um pouco mais fracos na ferramenta do que na partida.
5. Girar encostado na parede: a peca desliza para a celula livre mais proxima em vez de recusar.

## 2026-11-12 (noite) — MENU NOVO LIGADO + LIMPEZA + BUILD DE PLAYTEST (pedido do David)
- **Menu inicial = AnimatedMenu variante 0 (Console creme, aprovado em 06/11)**, agora ligado ao jogo por
  `UI/Game/MainMenuLive.cs` (nasce em runtime na cena MainMenu, como o GameUi). Novo jogo (pede confirmacao se ja
  existe save), Continuar — <empresa>, Opcoes (aviso "em breve"), Map Builder (so editor/dev build), Sair.
  AnimatedMenu ganhou so `Configure(variante, itens, callback)` — visual intocado.
- Cena MainMenu recriada (so camera) por `MainMenuSetup.Build`; Build Settings = MainMenu + GameOffice.
- **Descartado (movido para `_Descartados/2026-11-12/`, fora de Assets — nada foi apagado):** menu Game Boy 3D
  (GameBoyMenuController, GameBoyMenuSetup, MenuVideoCapture, Assets/Art/Menu, gen_gameboy.py, copia da cena antiga),
  menu UXML antigo (MenuController, MainMenu.uxml/uss), HUD antigo (HudController, HUD.uxml/uss, HudSetup) e o slice
  (SliceHarnessUI, GameLikeSliceSetup, cena GameplaySlice). GameSceneEnsure nao cria mais o HUD antigo.
- **NAO limpei (decidir com o David):** sistema 2D antigo (Assets/Scripts/LevelBuilder, Data/LevelBuilder 2D,
  Resources/Tiles, IsoDemoBuilder/GarageOfficeBuilder) e as cenas SampleScene/IsoDemo/GarageOffice/UIGallery.
- **Build:** `BatchBuild.BuildPlaytest -glout <pasta>` (development build, abre no menu). Entregue em
  `Downloads/TheGameLike_Playtest_2026-11-12/TheGameLike.exe`. Verificado rodando o proprio exe com `-glplay`:
  menu -> jogo -> loop completo, 28 telas, 0 excecoes. Prints em Docs/Validacao/Build_2026-11-12/.
- Decisoes provisorias: build e DEVELOPMENT (marca d'agua no canto) para o Map Builder aparecer; no build o Map
  Builder salva em persistentDataPath (nao volta para o projeto) — para autorar cenarios de verdade, usar pelo editor.

## 2026-11-12 (noite 2) — MAP BUILDER: empilhar, posicao na celula, texturas e 27 moveis novos (pedido do David)
- **Empilhar:** peca pequena (abajur, monitor, plantinha, PC antigo, notebook, livros, caneca...) passada por cima de um
  movel com tampo (mesas, mesinha, arquivo, frigobar, bancada, caixa...) vai EM CIMA dele, na altura do tampo. Mover o
  movel leva junto o que esta em cima; remover/girar deixa a peca cair no chao se houver espaco, senao ela sai (Ctrl+Z
  desfaz). Dados: `FurniturePlacement.onTop`; flags `placeableOnTop`/`supportsItemsOnTop`/`surfaceHeight` no
  FurnitureDef3DSO (definidas em Office3DSetup.ApplyStackingFlags). So 1 nivel (nada em cima de peca apoiada).
- **Posicao na celula:** seletor 3x3 (centro, 4 bordas, 4 cantos) no painel da peca armada/selecionada. A peca encosta
  na borda da celula pelo VOLUME real dela (FurnitureBounds), sem atravessar a parede. Dados: `FurniturePlacement.align`.
  Duas pecas apoiadas dividem a mesma celula do tampo se estiverem em posicoes diferentes.
- **Texturas:** chao e parede ganharam "acabamento" alem da cor (clicar no quadradinho Chao/Parede): Liso, Tijolinho,
  Tabuas, Ripado, Piso/Azulejo, Xadrez, Listras, Carpete, Concreto. Geradas por codigo na cor escolhida
  (Systems/Office/SurfaceTextures.cs), sem arquivo de imagem. Dados: `floorStyle`/`wallStyle` (OfficeMapData v2, com
  migracao v1->v2 trivial).
- **27 moveis novos** (Tools/Blender/gen_furniture_pack2.py; catalogo em Docs/Validacao/MapBuilder/12_moveis_pacote2.png):
  em cima de moveis — notebook, livros, caneca, cacto, luminaria de lava, trofeu, impressora, ventilador, videogame,
  caixa de som; chao — arquivo, lixeira, mesa redonda, mesa alta, divisoria, maquina de lanches, rack com TV, bancada,
  bicicleta, cantinho do cafe, estante de metal, pingue-pongue, banqueta; parede — mural de cortica, TV, neon, ar.
  Menu: Office 3D -> "9 - Adicionar mobilia nova (pacote 2)" (so cria o que falta; nao mexe em prefab/def existente).
- Verificado: 91/91 testes EditMode (6 novos), roteiro automatico no editor E no build (abajur empilhado no canto,
  tijolinho + tabuas, salvar/reler). Build: Downloads/TheGameLike_Playtest_2026-11-12_v2.

### DECISOES PROVISORIAS PARA O DAVID VALIDAR
1. Quem empilha em quem e fixo por peca (lista em ApplyStackingFlags). Estacao/mesa com monitor embutido aceita pecas
   no tampo (altura 0,765 m), mas a peca pode atravessar o monitor se ficar no centro — usar os cantos.
2. Moveis novos SEM efeito de gameplay (o GDD nao define); os antigos mantem os efeitos que ja tinham.
3. Peca de parede ignora a "posicao na celula" e continua em altura fixa (por isso ainda nao ha PORTA).
4. O seletor de posicao esta desenhado para a vista do jogo; com a camera girada (Q/E) ele nao gira junto.

## 2026-11-13 — Visual (atmosfera + boneco), Parametros, bugs, Git + releases + atualizacao automatica
### Modelos
- Tampos maiores para o que e apoiado caber: mesa/estacao/mesa alta 1,9 x 0,9 m, mesinha 0,96 x 0,9 m, bancada
  1,9 x 0,86 m; PC antigo compactado (0,75 x 0,72 m, teclado e mouse dentro do tampo). FBX reexportados.
### Atmosfera (Systems/Office/OfficeAtmosphere.cs — jogo e Map Builder usam o mesmo)
- Pos-processamento em runtime (bloom, tonemapping neutro, contraste/saturacao, leve tom quente, vinheta), SMAA, sombras
  suaves 4096, sombra das luzes dos abajures, SSAO (Editor/Setup/RenderQualitySetup — menu Setup -> ★ Qualidade visual),
  luz ambiente em 3 tons com rebote da cor do piso, sonda de reflexao da sala (piso/metal refletem o ambiente),
  chao externo que recebe a sombra da sala, capa branca no topo das paredes + rodape, brilho/metalico por tipo de
  material (OfficeBuilder.Finish). Parede LATERAL com cor/acabamento proprios (ou igual a do fundo).
- A camera do jogo agora afasta sozinha em salas maiores (OfficeGrid.GameCameraSize).
### Boneco (Systems/Office/OfficeCharacter.cs)
- Boneco novo arredondado montado por codigo (capsulas/esferas, articulado), com a aparencia do fundador (pele, cabelo,
  penteado, camisa, acessorio) e funcionarios variados. Animacao procedural: digitar, mouse, pensar, olhar em volta,
  espreguicar, relaxar, cochilar. Atividade padrao por assento (aba Assentos do Map Builder, etiqueta roxa).
- O campo characterPrefab do layout deixou de ser usado (Dev.fbx fica no projeto, sem uso).
### Tela de Parametros (UI/Tuning/TuningScreen.cs) — GDD 23.13
- Menu inicial -> "Parametros" e menu de pausa -> "Parametros do jogo (dev)". Gerada por reflexao: todo campo
  serializado dos 8 assets de balanceamento + generos, temas e plataformas vira um campo editavel. Salvar grava JSON em
  Assets/Resources/BalanceOverrides (editor) ou persistentDataPath/BalanceOverrides (build); definicoes (genero/tema/
  plataforma) so gravam pelo editor. "Restaurar valores originais" por grupo.
- Novo GameTuningBalanceSO: segundos por semana, inflacao semanal, variacao de custo por run, carencia de falencia,
  premio do IPO. Inflacao/variacao (GDD 23.12, escrito antes do codigo) entram no EconomyManager.ApplyDelta; ambas
  comecam em 0%. Save v3 -> v4.
### Bugs corrigidos
- Alocacao de fase: o controle agora PARA no orcamento (nao passa de 100%).
- Cenario errado depois de carregar: o OfficeView confere a fase a cada frame e, sem cenario para a fase, usa o da fase
  anterior mais proxima (antes pegava um qualquer).
### Git / releases / atualizacao
- Remoto novo: origin = github.com/DavidHGJ/the-game-like (PUBLICO). O remoto antigo virou "antigo".
- FORA do repositorio publico por decisao provisoria: GDD_*.txt (marcado Confidencial), DEV_LOG_GDD.md, References/,
  _Descartados/. Se o David quiser versionar o GDD, tirar do .gitignore (ou tornar o repo privado — mas ai a
  atualizacao automatica deixa de funcionar sem token).
- Publicar versao: `bash Tools/Release/release.sh 0.2.0 "notas"` (editor fechado). Build final + zip + release no GitHub.
- O jogo (build Windows) consulta a ultima release ao abrir o menu e oferece atualizar (Systems/Updates/GameUpdater.cs).
- Ferramentas do dev num build de release: criar um arquivo vazio `devtools.txt` ao lado do TheGameLike.exe.

### DECISOES PROVISORIAS PARA O DAVID VALIDAR
1. Precos mostrados nas telas ainda sao os de base; o cobrado ja inclui inflacao/variacao (quando ligadas).
2. Nomes dos parametros na tela vem do nome do campo no codigo (em ingles); as descricoes (tooltip) estao em portugues.
3. Catalogos em codigo (ResearchCatalog, SkillTreeCatalog, ParameterCatalog) ainda NAO aparecem na tela de Parametros.
4. Boneco feito de primitivas do Unity (nao e modelo do Blender com esqueleto).

## 2026-11-13 (tarde) — Ferramentas fora do build publico, ajustes do David importados, SIMULADOR DE ECONOMIA
- **Build publicado sem ferramentas:** `GameLike.Core.DevTools.Enabled` agora e decidido na compilacao (editor e
  development build = sim; build publicado = nao). Acabou o `devtools.txt`. No build publicado, ajustes gravados no
  computador (BalanceOverrides, OfficeMaps em persistentDataPath) sao ignorados.
- **Ajustes do David importados para os assets** (Tools -> GameLike -> Build -> Importar ajustes do jogo instalado;
  batch `ImportPlaytestSettings.BatchImport [-glfrom pasta]`): 46 valores em 6 grupos. Correcoes decididas pelo David:
  limites de hype 15/50/80 (ele tinha salvo 15.000/50.000/200.000 numa escala de 0 a 100) e dinheiro inicial R$ 75 mil,
  Garagem R$ 9 mil/semana. A pasta dele foi renomeada para `BalanceOverrides_importados_2026-11-13` (backup).
- **Nada publicado:** com esses valores toda run vai a falencia (simulacao abaixo). Release 0.1.4 aguardando o
  rebalanceamento. Build de release pronto e conferido sem ferramentas em Build/Release/0.1.4-pre (nao publicado).
- **Simulador de economia** (pedido do David: testar todas as faixas de nota sem jogar): `Systems/Simulation/
  EconomySimulator` (semana a semana com as mesmas formulas/valores do jogo) + `EconomyReport` (7 cenarios fase x
  tamanho, 6 faixas de nota, 3 jogos cada, metas: ruim = prejuizo, media/boa/otima = lucro, Garagem sem falir).
  Na tela de Parametros: item "Simulacao da economia" (topo). No editor: Tools -> GameLike -> Balanceamento -> Simular
  economia (grava Docs/Simulacao/economia.md). 9 testes do simulador (EconomySimulatorTests). 115/115 testes.
- Versao de teste com ferramentas (nao publicada): Downloads/TheGameLike_Dev_2026-11-13.
- Achados: (1) Garagem: as vendas entram ao longo de 26 semanas e a carencia e 4 semanas, entao ate jogo nota 9,5
  faliu; (2) Indie em diante: custo fixo semanal (45 mil/90 mil/250 mil) e muito maior que a receita dos jogos.

## 2026-11-13 (noite) — Rebalanceamento da RECEITA pelo simulador (GDD 23.14) + Garagem nova + release 0.1.4
- Regra do David (GDD 23.14, escrito antes do codigo): nao pode ficar facil — na Garagem, no maximo ~2 chances de
  acertar um jogo (nota >= 7); custos ficam como ele definiu; o ajuste e na receita das vendas.
- Formula de vendas ganhou piso e nota de virada (`salesFloorShare`, `salesScoreThreshold` em GameLoopBalanceSO;
  `SalesShareForScore`). Com 0/0 e a formula antiga.
- Busca automatica (`Editor/CI/BalanceSearch`, ~16 s, 41 mil combinacoes so na Garagem) escolheu, entre as que
  cumprem todas as metas, a que deixa o jogo medio com lucro perto de 30% do custo e muda o minimo:
  pequeno 40 mil un. (era 20 mil), medio 180 mil un. a R$ 30 (90 mil a R$ 20), grande 375 mil a R$ 60 (300 mil a
  R$ 40), AAA 1,5 mi (1,2 mi); expoente 1,5 (2,4), piso 20%, virada na nota 5, retencao 0,40-0,65 (0,55-0,93).
- Resultado (sem marketing): 21/21 metas. Dois fracassos seguidos quebram na semana 18 (depois do 2o lancamento);
  fracasso + acerto segue; jogo medio lucra 17-40% do custo conforme a fase. Com +-5% de custo, 2 casos limite viram.
- Com marketing completo as metas falham: as campanhas custam R$ 200 mil+ (valores do David) — informativo.
- Cenario da Garagem do David (5x5, 13 pecas) virou o padrao: Assets/Resources/OfficeMaps/GarageLayout.json.
- Release 0.1.4 publicada sem ferramentas de dev.

### DECISOES PROVISORIAS PARA O DAVID VALIDAR
1. "Dar certo" = nota >= 7; metas e alvo de 30% de lucro do jogo medio sao propostas (EconomyReport/BalanceSearch).
2. Nota 5 conta como fracasso (vende o mesmo piso que a nota 3).
3. Simulador nao usa tema/plataforma (neutros) nem sorteio das criticas.
