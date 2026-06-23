# ART_STYLE.md — The Game Like

Guia de estilo visual. É a "constituição" de arte do projeto, equivalente em peso ao `CLAUDE.md` para código — qualquer pixel novo deve respeitar o que está aqui, e qualquer mudança de estilo passa primeiro por este arquivo (mesma política GDD-first de sincronização, aplicada à arte).

## Referência de estilo

Referência-base aprovada: cena de escritório pixel art isométrico (arquivo de referência mantido fora do controle de versão de código, salvo separadamente pela equipe de arte). Características identificadas e adotadas:

- **Projeção isométrica clássica de jogo** (não isométrica matematicamente pura) — ângulos de aproximadamente 30°, blocos com face de topo, frente e lado bem diferenciadas.
- **Paleta saturada mas não neon** — tons de madeira (laranja-marrom) nos móveis, verde-azulado no piso, branco-creme nas paredes, azul-petróleo em estofados. Cores de "escritório real", não fantasiosas.
- **Shading em 3 tons por superfície** — cada face visível de um objeto (topo/frente/lado) tem sua própria variação de luz, sem flat color puro nem gradiente suave.
- **Sem dithering** — transições de sombra em blocos sólidos de cor, não em padrão pontilhado.
- **Densidade de detalhe média-alta** — pequenos objetos de cena (livros, plantas, post-its) dão vida sem virar ruído visual.

## Unidade de tile

**32×32 pixels** é a unidade base de todo asset do jogo (mobília, decoração, piso, parede). Decisão registrada em conversa de design — ver `CLAUDE.md`, seção "Sistema de layout de escritório", para o raciocínio completo (equilíbrio entre densidade de detalhe possível e esforço de produção, considerando a quantidade de objetos por cena).

- Personagens podem ocupar 1×2 tiles de altura (mais altos que largos).
- Móveis grandes (mesa, estante) podem ocupar múltiplos tiles de footprint — ver `FurnitureDefinitionSO.FootprintInTiles`.

## Outline

**Outline sempre presente, mas não preto puro.** Cada objeto tem um contorno de aproximadamente 1px num tom **bem mais escuro que a cor base do objeto** (não um preto neutro universal) — por exemplo, uma cadeira azul-petróleo tem outline num azul-petróleo quase-preto, não um preto genérico. Isso mantém a separação de silhueta da referência sem o contorno "gritar" mais que o conteúdo.

## Paleta

Paleta de cores formal **a definir como arquivo `.aseprite`/`.gpl` compartilhado** quando a primeira leva de sprites for produzida — por ora, a referência de cores é a imagem-base aprovada (tons de madeira, verde-azulado, branco-creme, azul-petróleo). Todo sprite novo deve usar cores dessa paleta compartilhada assim que ela existir; não criar cores ad-hoc por sprite.

## Composição de mobília (base + estofado + cor)

Para reduzir volume de desenho em peças com variantes (ver GDD seção 3, upgrades de mobília), móveis com variação de cor usam **camadas separadas tintáveis** em vez de um sprite completo por variante de cor:

- Camada de base (estrutura, metal, madeira) — cor fixa, não tintável.
- Camada de estofado/acabamento — marcada como tintável (`FurnitureVisualLayer.tintable = true`), recolorida via tint em runtime.

Isso significa: **desenhe a peça uma vez, em tons neutros/médios na camada tintável** — o tint multiplica a cor base do sprite, então partir de um cinza médio ou da cor "padrão" da variante mais comum dá o resultado mais previsível ao aplicar tints diferentes.

## Pendências de estilo

- Paleta formal compartilhada (arquivo `.aseprite` ou `.gpl`) — a criar quando a primeira sprite real for produzida.
- Guia de proporção de personagem (cabeça/corpo/pernas em grid isométrico) — a criar junto com o primeiro personagem desenhado de verdade (o fundador).
- Paleta de UI (cores de HUD/painéis) — provavelmente distinta da paleta de cenário, a definir na Fase 5 do roadmap (UI Toolkit).
