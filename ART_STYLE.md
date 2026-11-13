# ART_STYLE.md — The Game Like

Guia de estilo visual. É a "constituição" de arte do projeto, equivalente em peso ao `CLAUDE.md` para código — qualquer asset novo deve respeitar o que está aqui, e qualquer mudança de estilo passa primeiro por este arquivo (mesma política GDD-first de sincronização, aplicada à arte).

> **Mudança de direção (2026-10-31):** o projeto migrou de **pixel art isométrica 2D** para **3D low-poly com câmera isométrica**. A razão: produzir sprites isométricos para cada peça, direção e variante de cor (mais a ordenação manual de profundidade) era caro demais para um dev solo. Em 3D low-poly, a profundidade é resolvida pelo z-buffer da câmera (zero ordenação manual), rotacionar um móvel é girar o mesh, e trocar a cor de uma variante é trocar um material — não redesenhar arte. Este arquivo foi reescrito para refletir a nova direção; o histórico pixel art foi descartado.

## Direção visual

**3D low-poly estilizado**, lido através de uma **câmera ortográfica em ângulo isométrico de jogo**. O alvo é a estética "diorama de escritório" — limpa, colorida, legível de cima, com silhuetas simples e poucos polígonos por peça.

- **Projeção:** câmera **ortográfica** (sem perspectiva/distorção de fuga), rotacionada para o ângulo isométrico clássico de jogo (aprox. 30° de inclinação, 45° de giro). A ortográfica é o que dá a leitura "iso" estável e previsível de um tycoon de gestão, em vez do look realista de uma câmera em perspectiva.
- **Low-poly de verdade:** formas feitas de poucos polígonos, cantos legíveis, nada de high-poly ou escultura. Uma cadeira é reconhecível pela silhueta, não pelo detalhe.
- **Cores chapadas, não texturizadas:** cada peça usa **materiais de cor sólida** (albedo liso), sem texturas fotográficas. Variação visual vem da forma e da iluminação, não de mapas de textura densos.
- **Paleta saturada mas não neon** — tons de madeira (laranja-marrom) na mobília, verde-azulado no piso, branco-creme nas paredes, azul-petróleo em estofados. Cores de "escritório real", estilizadas, não fantasiosas. (Mesma intenção de paleta da referência original, agora aplicada a materiais 3D.)
- **Densidade de detalhe por adereços, não por polígono:** pequenos objetos de cena (plantas, canecas, monitores, pilhas de papel) dão vida ao diorama. A riqueza vem da quantidade e arranjo de peças simples, não de peças complexas.

## Iluminação e sombras

- **Luz direcional única** como fonte principal, em ângulo fixo coerente com a câmera iso, dando a cada face (topo/frente/lado) uma variação de brilho natural — o equivalente 3D do "shading em 3 tons por superfície" da direção anterior, agora produzido pela própria iluminação em vez de pintado à mão.
- **Sombras suaves e discretas** (ambient occlusion leve e/ou sombra projetada sutil) para assentar os objetos no chão sem escurecer a cena. Nada de sombras duras e dramáticas.
- **Sem outline por padrão.** O contorno escuro era uma necessidade do 2D para separar silhuetas; em 3D a separação vem da forma e da luz. Se em playtesting a leitura de silhueta exigir reforço, um outline estilizado (shader de borda) pode ser adicionado — mas só então, registrando a decisão aqui antes de implementar.

## Unidade de grid

A unidade base deixa de ser o tile de **32×32 pixels** e passa a ser a **célula de mundo 3D** do grid de colocação (ver `CLAUDE.md`, "Sistema de layout de escritório"). O tamanho exato da célula em unidades de mundo é **a calibrar** na cena de teste contra a arte real, uma vez definida a escala do primeiro mesh de mobília.

- **1 célula = 1 slot de footprint.** Móveis grandes (mesa, estante) ocupam múltiplas células — ver `FurnitureDefinitionSO.FootprintInCells`.
- **Personagens** ocupam 1 célula de footprint e têm altura proporcional (mais altos que largos), a definir junto com o primeiro personagem modelado (o fundador).
- **Andares** (storeys) empilham por um deslocamento vertical em world Y — ver `IsoGrid.StoreyWorldHeight`.

## Composição de mobília (base + variante de cor)

Para reduzir volume de produção em peças com variantes de cor (ver GDD seção 3, upgrades de mobília), o princípio continua o mesmo da direção anterior, agora em 3D:

- **Um mesh por peça**, não um mesh por variante de cor.
- A **cor da variante é um material/propriedade de material** aplicado ao submesh "tintável" (ex: o estofado de uma cadeira), enquanto partes de cor fixa (metal, madeira) usam material próprio. Trocar a cor de uma variante é trocar o material do submesh tintável — modele a peça uma vez.
- Concretamente: separe o mesh em submeshes por material (ex: `_Fixed` e `_Tintable`), ou use slots de material distintos, de forma que o runtime possa substituir só o material tintável. Isso substitui o antigo `FurnitureVisualLayer.tintable` (camadas de sprite) por slots de material.

## Convenção de assets 3D

- **Formato:** meshes como `.fbx` (ou `.obj`), importados para `Assets/Art/Furniture/<peca>/`. Cada `FurnitureDefinitionSO` referencia um **prefab** 3D, não mais uma pilha de sprites.
- **Escala e pivô:** todo mesh de mobília tem o **pivô no ponto de contato com o chão** (base da peça), para casar com a célula-âncora do grid — mesma lógica de ancoragem da direção anterior, agora em world space 3D.
- **Orientação base:** todo mesh é modelado voltado para uma direção canônica; as 4 orientações do jogo são **rotações de 90° no eixo Y** em runtime, não 4 meshes separados.
- **Materiais compartilhados:** materiais de cor (madeira, metal, as cores de estofado da paleta) vivem em `Assets/Art/Materials/` e são reusados entre peças. Não criar um material ad-hoc por peça — a paleta é compartilhada, como era com os sprites.

## Paleta

Paleta de cores formal **a definir como um conjunto de materiais compartilhados** (`Assets/Art/Materials/`) quando a primeira leva de meshes for produzida — por ora, a referência de cores é a intenção aprovada: tons de madeira, verde-azulado, branco-creme, azul-petróleo. Todo material novo deve usar cores dessa paleta compartilhada assim que ela existir; não criar cores ad-hoc por peça.

## Pendências de estilo

- Paleta formal compartilhada (conjunto de materiais `.mat`) — a criar quando o primeiro mesh real for produzido.
- Escala definitiva da célula de grid em unidades de mundo — a calibrar contra o primeiro mesh de mobília.
- Guia de proporção de personagem (altura/corpo em escala de grid 3D) — a criar junto com o primeiro personagem modelado (o fundador).
- Ângulo e parâmetros da câmera iso — primeiro protótipo na cena `Assets/Scenes/IsoDemo.unity` (gerada por `Tools → GameLike → Setup → 4 - Build 3D Iso Demo`): câmera **ortográfica**, rotação **(30°, 45°, 0)**, `orthographicSize ≈ 7.5`, luz direcional em (50°, -40°, 0) com sombra suave. Valores a refinar quando houver arte real; estes são o ponto de partida aprovado da direção.
- Decisão sobre outline estilizado (shader de borda) — só se a leitura de silhueta exigir em playtesting.
- Paleta de UI (cores de HUD/painéis) — provavelmente distinta da paleta de cenário, a definir na Fase 5 do roadmap (UI Toolkit).
