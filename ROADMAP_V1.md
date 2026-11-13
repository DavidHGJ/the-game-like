# ROADMAP_V1 — Caminho até uma v1 jogável

> Plano enxuto, **ordenado por dependência**, até uma **v1 que um jogador senta e joga sozinho**.
> Complementa `agents.md` (roadmap amplo, desatualizado) e o GDD (regras). Atualize os checkboxes.
>
> **Revisado em 2026-11-05 após auditoria do código real** — o estado está MUITO mais avançado do que
> os checkboxes do `agents.md` indicavam.

## Onde estamos (auditado, honesto)

O jogo **já é totalmente jogável com a UI final** (não só no harness de dev).

- ✅ **Loop core ligado e jogável** (`GameController` + managers): empresa → projeto → produção → nota →
  vendas → valuation → fases → **vender → meta-progressão (dinheiro/legado) → próxima run mais forte**;
  falência; autosave.
- ✅ **HUD interativa completa em UI Toolkit** (`HudController`, 951 linhas, `HUD.uxml`): topo (fase, equipe,
  jogos, reputação, PP, caixa, valuation, data, sparkline), painel de produção (progresso, áreas, hype,
  wishlists, bugs), **campanhas de marketing**, barra de ações (velocidade, Novo Projeto, Equipe, Vender),
  e **modais**: novo projeto (gênero/tema/tamanho/sliders/sequência/aderência), RH, resultado, evento narrativo.
- ✅ **Escritório 3D isométrico** montado (office builder + mobília + personagem na mesa) na cena `GameOffice`.
- ✅ **Menu Game Boy 3D** (`MainMenu`) → Novo jogo/Continuar → carrega `GameOffice` (ambas no build settings).
- ✅ **Pesquisa/PP** existe e **destrava temas e tamanhos** no modal de projeto.
- ✅ Conteúdo: 6 gêneros, 8 temas, eventos, 7 balance assets. Save/load + autosave.

**Conclusão:** Milestones A (UI do loop) e grande parte de B (equipe/marketing/pesquisa) **já estão feitos**.
O que falta pra v1 é mecânica faltante (plataformas), conteúdo, tuning e polish — não reconstruir UI.

---

## O que REALMENTE falta pra v1 mínima

### 1. Plataformas / Consoles  ⟵ **MAIOR GAP — IMPLEMENTADO (falta verificar compile/visual)**
Mecânica central do GDD (seção 15) e da visão: escolher em qual console lançar; consoles lançam ao longo
dos anos (história real).
- [x] `ConsoleSO` (herda `DefinitionAsset`): ano de lançamento, popularidade/curva, licença/custo, fabricante, categoria (`PlatformCategory`).
- [x] `ConsoleContentDto` + `ConsoleContentImporter` (molde de `GenreContentImporter`, grava em `Resources/Consoles`) + **21 JSONs** da timeline do GDD (`Assets/Content/Consoles/`).
- [x] Disponibilidade por ano (`ConsoleSO.IsAvailable` + `GameController.ConsolesAvailableNow()` lê `TimeManager.CurrentYear`).
- [x] Integrado na criação de projeto: dropdown de PLATAFORMA no modal (`HudController`), cobrança de licença no caixa, captura da popularidade no início → `SalesCalculator.PlatformReach` escala as vendas no lançamento.
- [x] Save: `ProjectState` ganhou campos de console; schema v1→v2 + migração trivial em `SaveManager`.
- [x] Testes EditMode: `ConsoleSOTests` + casos de `PlatformReach` em `SalesCalculatorTests`.
- [x] **VERIFICADO EM GAME (2026-11-05):** import rodado (21 `.asset` com Id estável); compila 0 erros; **50/50 testes EditMode passam**; em Play, o modal mostra "PC ($5.000)" no Ano 1, iniciar projeto cobra a licença (caixa 49.400→42.600) e o card mostra a plataforma "PC". **Fase 1 completa e funcional.**
- [ ] (Deferido) Compatibilidade Gênero × Plataforma (GDD 18.8) — por TIPO (`PlatformCategory`); hoje só a popularidade escala vendas.
- [ ] (Deferido) Gating de plataforma por Pesquisa/PP (a visão quer desbloquear plataformas via P&D além do ano).

### 2. Pesquisa: fechar o gasto de PP
A pesquisa destrava temas/tamanhos, mas confirmar/adicionar a **UI pra gastar PP** (desbloquear gêneros/
temas/tamanhos/engine features) — na visão, o personagem "pesquisa" gastando PP.
- [ ] Verificar se há tela de gasto de PP; se não, um painel/modal de Pesquisa simples.

### 3. Conteúdo + ícones (placeholder da internet, padronizar depois)
- [ ] Mais gêneros/temas (de 6/8 para ~15+/~20+) e mais eventos, via pipeline JSON→importer.
- [ ] Ícones de gênero/console (molde da internet por ora; arte própria no padrão depois).
- [ ] (Opcional v1) `SubgenreSO` + combinação (GDD 18.4).

### 4. Balanceamento
- [ ] Passe de tuning jogando: salários, thresholds de fase, custos de P&D, curva de vendas, pesos da nota,
      efeito de plataforma. Estruturado em `BalanceAsset` (fácil de ajustar).

### 5. Fluxo e fecho de v1
- [ ] Tela de **fim de run / meta** clara (venda → dinheiro/legado → recomeço) — hoje o fecho existe em dados;
      garantir que a UI comunica bem.
- [ ] (Deferível) **Criação de personagem** rasa (camisa + cabeça + acessório) — parte da visão, não bloqueia v1 mínima.
- [ ] Verificar Novo jogo/Continuar/slots ponta a ponta.

### 6. Polish visual (em paralelo, não crítico)
- [ ] Menu Game Boy (em andamento: modelo + fogo + iluminação).
- [ ] Feedback de lançamento, escritório, transições.

---

## Ordem recomendada

1. **Plataformas/Consoles (1)** — maior gap de mecânica, alinhado à visão.
2. **Pesquisa gasto de PP (2)** — fecha o sistema de P&D que já está meio pronto.
3. **Conteúdo + ícones (3)** e **Balanceamento (4)** em paralelo, conforme dá pra jogar mais.
4. **Fluxo/fecho (5)**.
5. **Polish (6)** em paralelo o tempo todo.

## Nota
Atualizar os checkboxes do `agents.md` (Fases 2–3 estão na prática feitas) quando sobrar tempo — não bloqueante.
