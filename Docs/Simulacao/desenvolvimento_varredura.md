# Varredura completa do desenvolvimento (GDD 23.18)

2268 combinações: 4 tamanhos × 3 equipes × 9 divisões × 7 temas × 3 engines. Nota do 1º jogo, média de 7 gêneros × 5 sorteios do fundador médio, sem o sorteio da crítica e sem corrigir bugs antes de lançar. Tabela inteira em `desenvolvimento_varredura.csv`.

- **Equipes:** Fraca = fundador iniciante (~3) + júniores; Média = fundador sorteado + plenos; Forte = fundador expert (8) + sêniores. Pequeno = só o fundador; Médio +2; Grande +5; AAA +8.
- **Divisões:** erro 0 = igual aos pesos do gênero; erro 1 = tudo no bloco que o gênero menos quer (fase de bloco único não tem escolha).
- **Engine:** inicial = renderizador e áudio básicos; completa = amenização máxima em todo bloco que tem módulo.

## Visão geral

- Menor nota: **0,6** — AAA (Estúdio) · equipe Fraca · Péssima (tudo no bloco menos importante) · tema −−− · Sem engine
- Maior nota: **9,6** — Pequeno (Garagem) · equipe Forte · Perfeita · tema +++ · Sem engine
- Média: 5,6

| Faixa de nota | Combinações | % |
|---|---|---|
| 0 a 1 | 21 | 1% |
| 1 a 2 | 47 | 2% |
| 2 a 3 | 142 | 6% |
| 3 a 4 | 281 | 12% |
| 4 a 5 | 418 | 18% |
| 5 a 6 | 380 | 17% |
| 6 a 7 | 412 | 18% |
| 7 a 8 | 305 | 13% |
| 8 a 9 | 217 | 10% |
| 9 a 10 | 45 | 2% |

## Coerência (melhorar nunca pode baixar a nota)

Nenhuma violação: divisão, tema, equipe e engine melhores nunca baixam a nota.

## Efeito de cada eixo (nota média, com todo o resto variando)

### Gênero × tema

| Gênero × tema | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |
|---|---|---|---|---|---|
| −−− | **3,2** | 0,6 | 4,8 | 70% | −7% |
| −− | **3,9** | 1,3 | 5,5 | 70% | −7% |
| − | **4,9** | 2,2 | 6,5 | 70% | −7% |
| Neutro | **5,6** | 2,9 | 7,2 | 70% | −7% |
| + | **6,3** | 3,6 | 7,9 | 70% | −7% |
| ++ | **7,2** | 4,5 | 8,8 | 70% | −7% |
| +++ | **7,9** | 5,2 | 9,6 | 70% | −7% |

### Divisão nas fases

| Divisão nas fases | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |
|---|---|---|---|---|---|
| Perfeita | **7,0** | 4,6 | 9,6 | 100% | −7% |
| Boa (erro 25%) | **6,5** | 4,0 | 9,1 | 90% | −7% |
| Média (erro 50%) | **5,7** | 3,0 | 8,8 | 73% | −7% |
| Ruim (erro 75%) | **4,7** | 2,0 | 7,8 | 51% | −7% |
| Péssima (tudo no bloco menos importante) | **3,3** | 0,6 | 6,5 | 22% | −7% |
| Divide igual | **6,4** | 3,8 | 9,1 | 88% | −7% |
| Invertida | **4,8** | 2,2 | 7,7 | 54% | −7% |
| Mista: 3 fases certas, 2 péssimas | **5,7** | 3,0 | 8,8 | 72% | −7% |
| Mista: só a Pré-produção péssima | **6,1** | 3,6 | 8,7 | 82% | −7% |

### Equipe

| Equipe | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |
|---|---|---|---|---|---|
| Fraca | **5,5** | 0,6 | 9,2 | 69% | −8% |
| Média | **5,5** | 0,6 | 9,3 | 70% | −8% |
| Forte | **5,7** | 0,6 | 9,6 | 71% | −6% |

### Engine

| Engine | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |
|---|---|---|---|---|---|
| Sem engine | **5,5** | 0,6 | 9,6 | 68% | −7% |
| Engine inicial | **5,5** | 0,7 | 9,6 | 69% | −7% |
| Engine completa | **5,7** | 1,2 | 9,6 | 73% | −7% |

### Tamanho

| Tamanho | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |
|---|---|---|---|---|---|
| Pequeno (Garagem) | **5,6** | 0,9 | 9,6 | 71% | −7% |
| Médio (Indie) | **5,7** | 0,8 | 9,5 | 72% | −7% |
| Grande (Escritório) | **5,5** | 0,6 | 9,3 | 68% | −8% |
| AAA (Estúdio) | **5,5** | 0,6 | 9,3 | 69% | −8% |

## Pequeno (Garagem): divisão × tema (equipe média, engine inicial)

| Divisão | −−− | −− | − | Neutro | + | ++ | +++ |
|---|---|---|---|---|---|---|---|
| Perfeita | 4,6 | 5,3 | 6,2 | 6,9 | 7,6 | 8,5 | 9,2 |
| Boa (erro 25%) | 4,1 | 4,8 | 5,7 | 6,4 | 7,1 | 8,0 | 8,7 |
| Média (erro 50%) | 3,4 | 4,1 | 5,0 | 5,7 | 6,4 | 7,3 | 8,0 |
| Ruim (erro 75%) | 2,4 | 3,0 | 4,0 | 4,6 | 5,3 | 6,2 | 6,9 |
| Péssima (tudo no bloco menos importante) | 1,0 | 1,7 | 2,6 | 3,3 | 4,0 | 4,9 | 5,6 |
| Divide igual | 3,9 | 4,5 | 5,5 | 6,1 | 6,8 | 7,7 | 8,4 |
| Invertida | 2,2 | 2,9 | 3,8 | 4,5 | 5,2 | 6,1 | 6,8 |
| Mista: 3 fases certas, 2 péssimas | 3,6 | 4,3 | 5,2 | 5,9 | 6,6 | 7,5 | 8,2 |
| Mista: só a Pré-produção péssima | 3,6 | 4,3 | 5,2 | 5,9 | 6,6 | 7,5 | 8,2 |

Equipe fraca → forte com divisão Ruim e tema neutro: 4,5 → 4,6 → 5,1 · sem → com engine completa (mesmo caso, equipe média): 4,6 → 4,6 → 5,0.

## Médio (Indie): divisão × tema (equipe média, engine inicial)

| Divisão | −−− | −− | − | Neutro | + | ++ | +++ |
|---|---|---|---|---|---|---|---|
| Perfeita | 4,6 | 5,3 | 6,3 | 7,0 | 7,7 | 8,6 | 9,3 |
| Boa (erro 25%) | 4,3 | 5,0 | 5,9 | 6,6 | 7,3 | 8,2 | 8,9 |
| Média (erro 50%) | 3,4 | 4,1 | 5,1 | 5,7 | 6,4 | 7,4 | 8,1 |
| Ruim (erro 75%) | 2,3 | 3,0 | 3,9 | 4,6 | 5,3 | 6,3 | 7,0 |
| Péssima (tudo no bloco menos importante) | 0,9 | 1,6 | 2,5 | 3,2 | 3,9 | 4,9 | 5,6 |
| Divide igual | 4,2 | 4,9 | 5,8 | 6,5 | 7,2 | 8,1 | 8,8 |
| Invertida | 2,6 | 3,3 | 4,2 | 4,9 | 5,6 | 6,5 | 7,2 |
| Mista: 3 fases certas, 2 péssimas | 3,2 | 3,9 | 4,8 | 5,5 | 6,2 | 7,1 | 7,8 |
| Mista: só a Pré-produção péssima | 3,8 | 4,5 | 5,5 | 6,2 | 6,8 | 7,8 | 8,5 |

Equipe fraca → forte com divisão Ruim e tema neutro: 4,5 → 4,6 → 4,9 · sem → com engine completa (mesmo caso, equipe média): 4,6 → 4,6 → 5,0.

## Grande (Escritório): divisão × tema (equipe média, engine inicial)

| Divisão | −−− | −− | − | Neutro | + | ++ | +++ |
|---|---|---|---|---|---|---|---|
| Perfeita | 4,6 | 5,3 | 6,2 | 6,9 | 7,6 | 8,5 | 9,2 |
| Boa (erro 25%) | 4,0 | 4,7 | 5,7 | 6,3 | 7,0 | 7,9 | 8,6 |
| Média (erro 50%) | 3,2 | 3,9 | 4,8 | 5,5 | 6,2 | 7,1 | 7,8 |
| Ruim (erro 75%) | 2,1 | 2,8 | 3,7 | 4,4 | 5,1 | 6,0 | 6,7 |
| Péssima (tudo no bloco menos importante) | 0,7 | 1,4 | 2,3 | 3,0 | 3,7 | 4,6 | 5,3 |
| Divide igual | 4,0 | 4,7 | 5,6 | 6,3 | 7,0 | 7,9 | 8,6 |
| Invertida | 2,3 | 3,0 | 3,9 | 4,6 | 5,3 | 6,2 | 6,9 |
| Mista: 3 fases certas, 2 péssimas | 3,1 | 3,7 | 4,7 | 5,4 | 6,0 | 7,0 | 7,6 |
| Mista: só a Pré-produção péssima | 3,8 | 4,5 | 5,4 | 6,1 | 6,8 | 7,7 | 8,4 |

Equipe fraca → forte com divisão Ruim e tema neutro: 4,3 → 4,4 → 4,5 · sem → com engine completa (mesmo caso, equipe média): 4,3 → 4,4 → 4,7.

## AAA (Estúdio): divisão × tema (equipe média, engine inicial)

| Divisão | −−− | −− | − | Neutro | + | ++ | +++ |
|---|---|---|---|---|---|---|---|
| Perfeita | 4,6 | 5,3 | 6,2 | 6,9 | 7,6 | 8,5 | 9,2 |
| Boa (erro 25%) | 4,1 | 4,8 | 5,7 | 6,4 | 7,1 | 8,0 | 8,7 |
| Média (erro 50%) | 3,2 | 3,9 | 4,8 | 5,5 | 6,1 | 7,1 | 7,7 |
| Ruim (erro 75%) | 2,1 | 2,8 | 3,7 | 4,4 | 5,1 | 6,0 | 6,7 |
| Péssima (tudo no bloco menos importante) | 0,7 | 1,4 | 2,3 | 3,0 | 3,7 | 4,6 | 5,3 |
| Divide igual | 4,2 | 4,9 | 5,8 | 6,5 | 7,2 | 8,1 | 8,8 |
| Invertida | 2,4 | 3,1 | 4,1 | 4,7 | 5,4 | 6,4 | 7,0 |
| Mista: 3 fases certas, 2 péssimas | 3,1 | 3,7 | 4,7 | 5,3 | 6,0 | 6,9 | 7,6 |
| Mista: só a Pré-produção péssima | 3,8 | 4,5 | 5,4 | 6,1 | 6,8 | 7,7 | 8,4 |

Equipe fraca → forte com divisão Ruim e tema neutro: 4,3 → 4,4 → 4,5 · sem → com engine completa (mesmo caso, equipe média): 4,3 → 4,4 → 4,7.

