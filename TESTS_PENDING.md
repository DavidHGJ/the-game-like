# TESTS_PENDING.md — Verificações no Unity (fazer quando o computador liberar)

Checklist do que precisa ser testado/verificado **dentro do editor Unity** (precisa da tela),
acumulado enquanto o dev usa o computador. O trabalho de **código** segue em paralelo sem a tela;
este arquivo lista o que fazer de uma vez quando o editor liberar.

Procedimento padrão de cada ciclo: editar → focar Unity → **Ctrl+R** (recompilar, **fora** do Play) →
conferir **0 erros** no Console → rodar/testar → voltar ao edit mode.

---

## Ordem de setup sugerida (uma vez, no editor)
1. **Ctrl+R** — compilar e conferir 0 erros.
2. **Tools → GameLike → Setup → 1 - Create Balance Assets** (se faltar algum balance .asset).
3. **Tools → GameLike → Setup → 2 - Import Genres (JSON → Resources)** — importa os 6 gêneros
   §18.5 do JSON para `Resources/Genres` (limpa os antigos antes).
4. **Tools → GameLike → Setup → 3 - Build Playable Scene** (recria `GameplaySlice`).
5. **Tools → GameLike → Setup → 5 - Build 3D Garage Office** — monta a cena 3D `GarageOffice`.
6. Rodar testes e Play (seções abaixo).
   *(Atalho: "★ Do Everything" cobre 1–3; a Garage 3D é o passo 5 à parte.)*

## 0. Compilação (sempre primeiro)
- [ ] Focar Unity, **Ctrl+R**, confirmar **0 erros** e **0 warnings novos** no Console.
- [ ] Se houver erro, corrigir antes de qualquer teste.
- [ ] Arquivos novos/alterados nesta leva: `QualityCalculator`, `QualityFormulaBalanceSO`,
      `GameLoopBalanceSO`, `ProductionCalculator`, `SalesCalculator`, `ProjectManager`,
      `ProjectState`, `SliceHarnessUI`, `GenreContentImporter`, `GameLikeSliceSetup`,
      `GarageOfficeBuilder`, 6 JSONs em `Assets/Content/Genres/`, 2 novos testes.

## 1. Fórmula de Qualidade §19.7 (já alterada no código)
Arquivos: `QualityCalculator.cs`, `QualityFormulaBalanceSO.cs`.
- [ ] Rodar **Test Runner → EditMode** (`QualityCalculatorTests`): esperado **tudo verde**.
  - `Compute_IsWithinZeroToTen` — nota sempre 0..10.
  - `MatchedAllocation_ScoresHigherThanMismatched` — alocação casada pontua mais.
  - `GenreIdealProfile_SumsToOne`, `NeutralGenre_IdealProfileIsUniform`.
- [ ] No Play: variar a alocação e ver a nota responder (componente de sliders = 40%).
- [ ] Confirmar no Inspector de `QualityFormulaBalance.asset` que os 6 pesos somam 1.0 e que
      existem os 4 campos de "valor neutro" (Tema×Gênero, Engine Budget, Mercado, Eventos).
- [ ] Sanidade: com perfil neutro (6/10) nos 4 componentes sem sistema, uma alocação perfeita
      (sliders=10, equipe alta) deve dar nota alta mas **não 10 cravado** (os 45% neutros puxam
      para ~6–8). Verificar que faz sentido.

## 2. Perfis de Gênero §18.5 (a implementar no código — testar depois)
- [ ] Reimportar/recriar os gêneros (menu **Tools → GameLike → Setup**) e abrir cada
      `Assets/Resources/Genres/*.asset` no Inspector: conferir que os **19 parâmetros** estão
      preenchidos por tamanho (Pequeno/Médio/Grande/AAA) e coerentes com o gênero.
- [ ] No Play: criar o mesmo tamanho de projeto em **gêneros diferentes** e confirmar que o
      **perfil ideal muda** (a dica "Aderência ao gênero %" reage à alocação de forma diferente
      por gênero) — critério §21.11 (a fórmula diferencia gêneros).

## 3. Produção por Pontos §5.3 (a implementar — testar depois)
- [ ] Confirmar teto por parâmetro por tamanho: Pequeno 300 / Médio 450 / Grande 550 / AAA 600.
- [ ] Confirmar 100 pontos-base por pessoa por fase e que o tamanho da equipe por fase respeita
      §7.6 (Garage 1 / Indie 2+1 / Escritório 5+1 / Estúdio 8+1).
- [ ] No Play: equipe maior (fase mais avançada) deve render mais pontos → nota/depth maior.

## 4. Hype → Vendas §19.6 (a implementar — testar depois)
- [ ] Confirmar bandas de hype (0-20 fraca / 21-50 mediana / 51-80 boa / 81-100 excelente).
- [ ] Confirmar conversão ~**80 vendas por 100 wishlists** na semana de lançamento.
- [ ] No Play: lançar 2 jogos com hype diferente e ver a diferença nas vendas/receita.
      Substitui a curva de vendas inventada (exponencial) — confirmar que a receita ficou
      coerente e o caixa não explode nem zera sem sentido.

## 5. Economia §19.1 / §19.2 (já no código — reconfirmar no fluxo)
- [ ] Thresholds de fase: Garage→Indie **$250k**, Indie→Escritório **$500k**, →Estúdio **$5M**.
- [ ] Custo semanal da equipe usando salários §19.2 (Junior $2k, etc.); fundador não onera.
- [ ] No Play: rodar vários ciclos e ver a progressão de fase disparar nos thresholds certos.

## 6. Cena 3D do Escritório (a montar — precisa do editor)
- [ ] Rodar o gerador de mobília se faltar peça (`Tools → GameLike → ...` / script Blender).
- [ ] Montar a cena 3D da fase **Garage** com os prefabs (mesa, cadeira, monitor, planta) +
      câmera ortográfica iso (30°/45°, size ~7.5) + luz direcional — substituindo a `IsoDemo`
      de primitivas.
- [ ] Conferir na Game view: enquadramento iso, materiais chapados, sombras suaves, sem erros.
- [ ] (Se integrar ao loop) confirmar que a cena abre junto com o `GameplaySlice` sem quebrar o
      harness.

## 7. Loop completo §21.11 (critério de sucesso da vertical slice)
- [ ] Rodar **vários ciclos seguidos** (criar → distribuir → lançar → nota/receita → próximo)
      **sem travar** e **sem dado incoerente**.
- [ ] Testar com **3–5 gêneros variados** e ao menos 2 tamanhos.
- [ ] Vender a empresa (IPO) e reiniciar run — confirmar o fecho e o recomeço.
- [ ] Console **sem erros** ao fim (lembrar: linha vermelha de `CompanySoldEvent` é esperada,
      é `Severity.Critical` renderizada como LogError, **não** é erro real).

## 8. Regressão
- [ ] Reabrir `Assets/Scenes/GameplaySlice.unity` e confirmar que ainda abre e roda.
- [ ] Deixar o editor em **edit mode** e estado estável ao terminar.

---

### Observações
- Nada será commitado no git sem o dev pedir.
- Divergência GDD-first em aberto (ver `DEV_LOG_GDD.md`): GDD diz "2D pixel art", projeto é 3D —
  confirmar atualização do GDD para v1.2.
