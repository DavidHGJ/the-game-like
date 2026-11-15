# Simulação da economia — sem marketing

Metas cumpridas: **20 de 21** (GDD 23.14). Valores atuais do balanceamento.

- **Chances (Garagem):** dinheiro inicial real e regra de falência; o jogador continua tentando e a simulação vai até o último lançamento. Dois fracassos seguidos têm de quebrar a empresa antes do 3º lançamento (mas depois do 2º); fracasso + acerto tem de seguir lançando jogos.
- **Lucro por jogo:** sem falência e caixa começando em zero; a coluna de caixa vira o capital que a fase exige. Nota ruim dá prejuízo; média, boa e ótima dão lucro.
- Receita e unidades são a venda completa prevista de cada jogo. Lucro/jogo = receita prevista − tudo o que a empresa gastou enquanto o jogo era produzido.

# Garagem — chances do jogador

## Garagem · Pequeno (começo real)

Equipe: fundador + 0 · jogos de 8 semanas · caixa inicial R$ 75.000

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Caixa mínimo | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Fracasso, fracasso, tenta de novo (3 · 3 · 8) | 4.955 | R$ 74.325 | R$ 79.253 | -R$ 4.928 | -R$ 23.625 | ✅ ok · faliu na sem. 19 |
| Fraco, fraco, tenta de novo (5 · 5 · 8) | 8.340 | R$ 125.110 | R$ 79.572 | R$ 45.537 | -R$ 22.110 | ❌ deveria falir e não faliu |
| Fracasso, acerto, segue (3 · 8 · 6,5 · 6,5) | 9.349 | R$ 140.246 | R$ 79.892 | R$ 60.353 | -R$ 23.625 | ✅ ok · terminou com R$ 184.924 |
| Fraco, acerto, segue (5 · 7 · 6,5 · 6,5) | 8.300 | R$ 124.511 | R$ 79.892 | R$ 44.618 | -R$ 22.110 | ✅ ok · terminou com R$ 122.014 |
| Acerta de cara (8 · 8 · 6,5) | 12.863 | R$ 192.950 | R$ 79.572 | R$ 113.377 | -R$ 18.264 | ✅ ok · terminou com R$ 283.643 |
| Só médios (6,5 ×4) | 8.766 | R$ 131.490 | R$ 79.892 | R$ 51.597 | -R$ 18.264 | — · terminou com R$ 149.899 |

# Lucro por jogo no tamanho principal da fase

## Garagem · Pequeno

Equipe: fundador + 0 · jogos de 8 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 4.955 | R$ 74.325 | R$ 79.572 | -R$ 5.247 | R$ 99.295 | ✅ ok |
| Fraca (5) | 5.055 | R$ 75.825 | R$ 79.572 | -R$ 3.747 | R$ 97.110 | — |
| Média (6,5) | 8.766 | R$ 131.490 | R$ 79.572 | R$ 51.917 | R$ 93.264 | ✅ ok |
| Boa (8) | 14.912 | R$ 223.680 | R$ 79.572 | R$ 144.107 | R$ 93.264 | ✅ ok |
| Ótima (9,5) | 225.807 | R$ 3.387.105 | R$ 79.572 | R$ 3.307.532 | R$ 93.264 | ✅ ok |
| Variada (5 · 7 · 6) | 7.609 | R$ 114.145 | R$ 79.572 | R$ 34.572 | R$ 97.110 | — |

## Indie · Médio

Equipe: fundador + 1 · jogos de 15 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 17.648 | R$ 670.624 | R$ 775.658 | -R$ 105.034 | R$ 1.038.002 | ✅ ok |
| Fraca (5) | 18.267 | R$ 694.146 | R$ 775.658 | -R$ 81.512 | R$ 990.958 | — |
| Média (6,5) | 31.299 | R$ 1.189.362 | R$ 775.658 | R$ 413.704 | R$ 820.146 | ✅ ok |
| Boa (8) | 53.034 | R$ 2.015.292 | R$ 775.658 | R$ 1.239.634 | R$ 820.146 | ✅ ok |
| Ótima (9,5) | 370.042 | R$ 14.061.596 | R$ 775.658 | R$ 13.285.938 | R$ 820.146 | ✅ ok |
| Variada (5 · 7 · 6) | 27.243 | R$ 1.035.246 | R$ 775.658 | R$ 259.588 | R$ 902.410 | — |

## Escritório · Grande

Equipe: fundador + 3 · jogos de 22 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 21.393 | R$ 1.604.475 | R$ 2.740.059 | -R$ 1.135.584 | R$ 5.139.389 | ✅ ok |
| Fraca (5) | 23.140 | R$ 1.735.500 | R$ 2.740.059 | -R$ 1.004.559 | R$ 4.877.339 | — |
| Média (6,5) | 36.693 | R$ 2.751.975 | R$ 2.740.059 | R$ 11.916 | R$ 2.844.389 | ✅ ok |
| Boa (8) | 59.412 | R$ 4.455.900 | R$ 2.740.059 | R$ 1.715.841 | R$ 2.808.141 | ✅ ok |
| Ótima (9,5) | 247.247 | R$ 18.543.525 | R$ 2.740.059 | R$ 15.803.466 | R$ 2.808.141 | ✅ ok |
| Variada (5 · 7 · 6) | 32.481 | R$ 2.436.075 | R$ 2.740.059 | -R$ 303.984 | R$ 3.815.032 | — |

## Estúdio · AAA

Equipe: fundador + 7 · jogos de 33 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 175.143 | R$ 10.508.580 | R$ 12.332.503 | -R$ 1.823.923 | R$ 16.372.668 | ✅ ok |
| Fraca (5) | 192.675 | R$ 11.560.500 | R$ 12.332.503 | -R$ 772.003 | R$ 14.268.708 | — |
| Média (6,5) | 323.246 | R$ 19.394.760 | R$ 12.332.503 | R$ 7.062.256 | R$ 12.300.676 | ✅ ok |
| Boa (8) | 544.007 | R$ 32.640.420 | R$ 12.332.503 | R$ 20.307.916 | R$ 12.300.676 | ✅ ok |
| Ótima (9,5) | 1.573.128 | R$ 94.387.680 | R$ 12.332.503 | R$ 82.055.176 | R$ 12.300.676 | ✅ ok |
| Variada (5 · 7 · 6) | 282.739 | R$ 16.964.340 | R$ 12.332.503 | R$ 4.631.836 | R$ 13.080.644 | — |

# Outros tamanhos (informativo, sem meta)

## Indie · Pequeno

Equipe: fundador + 0 · jogos de 8 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 4.955 | R$ 74.325 | R$ 371.201 | -R$ 296.876 | R$ 1.261.032 | — |
| Fraca (5) | 5.055 | R$ 75.825 | R$ 371.201 | -R$ 295.376 | R$ 1.256.487 | — |
| Média (6,5) | 8.766 | R$ 131.490 | R$ 371.201 | -R$ 239.711 | R$ 1.089.537 | — |
| Boa (8) | 14.912 | R$ 223.680 | R$ 371.201 | -R$ 147.521 | R$ 812.967 | — |
| Ótima (9,5) | 225.807 | R$ 3.387.105 | R$ 371.201 | R$ 3.015.904 | R$ 418.889 | — |
| Variada (5 · 7 · 6) | 7.609 | R$ 114.145 | R$ 371.201 | -R$ 257.056 | R$ 1.141.512 | — |

## Escritório · Médio

Equipe: fundador + 1 · jogos de 15 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 17.648 | R$ 670.624 | R$ 1.543.164 | -R$ 872.540 | R$ 3.458.087 | — |
| Fraca (5) | 18.267 | R$ 694.146 | R$ 1.543.164 | -R$ 849.018 | R$ 3.387.521 | — |
| Média (6,5) | 31.299 | R$ 1.189.362 | R$ 1.543.164 | -R$ 353.802 | R$ 2.355.475 | — |
| Boa (8) | 53.034 | R$ 2.015.292 | R$ 1.543.164 | R$ 472.127 | R$ 1.626.981 | — |
| Ótima (9,5) | 370.042 | R$ 14.061.596 | R$ 1.543.164 | R$ 12.518.431 | R$ 1.626.981 | — |
| Variada (5 · 7 · 6) | 27.243 | R$ 1.035.246 | R$ 1.543.164 | -R$ 507.917 | R$ 2.602.817 | — |

## Estúdio · Grande

Equipe: fundador + 3 · jogos de 22 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 21.393 | R$ 1.604.475 | R$ 6.380.581 | -R$ 4.776.106 | R$ 16.430.150 | — |
| Fraca (5) | 23.140 | R$ 1.735.500 | R$ 6.380.581 | -R$ 4.645.081 | R$ 16.338.571 | — |
| Média (6,5) | 36.693 | R$ 2.751.975 | R$ 6.380.581 | -R$ 3.628.606 | R$ 13.937.039 | — |
| Boa (8) | 59.412 | R$ 4.455.900 | R$ 6.380.581 | -R$ 1.924.681 | R$ 10.529.339 | — |
| Ótima (9,5) | 247.247 | R$ 18.543.525 | R$ 6.380.581 | R$ 12.162.943 | R$ 6.532.629 | — |
| Variada (5 · 7 · 6) | 32.481 | R$ 2.436.075 | R$ 6.380.581 | -R$ 3.944.506 | R$ 14.443.514 | — |


# Simulação da economia — marketing completo (anúncio, wishlist, demo)

Metas cumpridas: **13 de 21** (GDD 23.14). Valores atuais do balanceamento.

- **Chances (Garagem):** dinheiro inicial real e regra de falência; o jogador continua tentando e a simulação vai até o último lançamento. Dois fracassos seguidos têm de quebrar a empresa antes do 3º lançamento (mas depois do 2º); fracasso + acerto tem de seguir lançando jogos.
- **Lucro por jogo:** sem falência e caixa começando em zero; a coluna de caixa vira o capital que a fase exige. Nota ruim dá prejuízo; média, boa e ótima dão lucro.
- Receita e unidades são a venda completa prevista de cada jogo. Lucro/jogo = receita prevista − tudo o que a empresa gastou enquanto o jogo era produzido.

# Garagem — chances do jogador

## Garagem · Pequeno (começo real)

Equipe: fundador + 0 · jogos de 8 semanas · caixa inicial R$ 75.000

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Caixa mínimo | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Fracasso, fracasso, tenta de novo (3 · 3 · 8) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | ❌ faliu na semana 5 antes de lançar o 1º jogo |
| Fraco, fraco, tenta de novo (5 · 5 · 8) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | ❌ faliu na semana 5 antes de lançar o 1º jogo |
| Fracasso, acerto, segue (3 · 8 · 6,5 · 6,5) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | ❌ faliu na semana 5 antes de lançar o 1º jogo |
| Fraco, acerto, segue (5 · 7 · 6,5 · 6,5) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | ❌ faliu na semana 5 antes de lançar o 1º jogo |
| Acerta de cara (8 · 8 · 6,5) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | ❌ faliu na semana 5 antes de lançar o 1º jogo · a empresa não cresceu |
| Só médios (6,5 ×4) | 0 | R$ 0 | R$ 0 | R$ 0 | -R$ 678.848 | — · faliu na sem. 5 |

# Lucro por jogo no tamanho principal da fase

## Garagem · Pequeno

Equipe: fundador + 0 · jogos de 8 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 7.726 | R$ 115.890 | R$ 1.600.036 | -R$ 1.484.146 | R$ 4.577.527 | ✅ ok |
| Fraca (5) | 10.670 | R$ 160.050 | R$ 1.600.036 | -R$ 1.439.986 | R$ 4.489.207 | — |
| Média (6,5) | 17.072 | R$ 256.080 | R$ 1.600.036 | -R$ 1.343.956 | R$ 4.297.117 | ❌ deveria dar lucro |
| Boa (8) | 28.521 | R$ 427.815 | R$ 1.600.036 | -R$ 1.172.221 | R$ 3.971.520 | ❌ deveria dar lucro |
| Ótima (9,5) | 484.039 | R$ 7.260.585 | R$ 1.600.036 | R$ 5.660.548 | R$ 1.803.393 | ✅ ok |
| Variada (5 · 7 · 6) | 14.988 | R$ 224.825 | R$ 1.600.036 | -R$ 1.375.211 | R$ 4.349.295 | — |

## Indie · Médio

Equipe: fundador + 1 · jogos de 15 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 26.013 | R$ 988.494 | R$ 2.314.154 | -R$ 1.325.660 | R$ 5.017.827 | ✅ ok |
| Fraca (5) | 36.018 | R$ 1.368.684 | R$ 2.314.154 | -R$ 945.470 | R$ 4.257.371 | — |
| Média (6,5) | 58.011 | R$ 2.204.418 | R$ 2.314.154 | -R$ 109.736 | R$ 2.652.301 | ❌ deveria dar lucro |
| Boa (8) | 97.446 | R$ 3.702.948 | R$ 2.314.154 | R$ 1.388.793 | R$ 2.538.858 | ✅ ok |
| Ótima (9,5) | 763.666 | R$ 29.019.308 | R$ 2.314.154 | R$ 26.705.153 | R$ 2.538.858 | ✅ ok |
| Variada (5 · 7 · 6) | 50.862 | R$ 1.932.781 | R$ 2.314.154 | -R$ 381.373 | R$ 3.488.035 | — |

## Escritório · Grande

Equipe: fundador + 3 · jogos de 22 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 47.476 | R$ 3.560.700 | R$ 4.297.379 | -R$ 736.679 | R$ 5.899.199 | ✅ ok |
| Fraca (5) | 64.081 | R$ 4.806.075 | R$ 4.297.379 | R$ 508.696 | R$ 4.535.995 | — |
| Média (6,5) | 89.394 | R$ 6.704.550 | R$ 4.297.379 | R$ 2.407.171 | R$ 4.535.995 | ✅ ok |
| Boa (8) | 132.118 | R$ 9.908.850 | R$ 4.297.379 | R$ 5.611.471 | R$ 4.535.995 | ✅ ok |
| Ótima (9,5) | 580.049 | R$ 43.503.675 | R$ 4.297.379 | R$ 39.206.296 | R$ 4.535.995 | ✅ ok |
| Variada (5 · 7 · 6) | 80.875 | R$ 6.065.675 | R$ 4.297.379 | R$ 1.768.296 | R$ 4.535.995 | — |

## Estúdio · AAA

Equipe: fundador + 7 · jogos de 33 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 227.543 | R$ 13.652.580 | R$ 13.919.318 | -R$ 266.738 | R$ 14.844.874 | ✅ ok |
| Fraca (5) | 317.787 | R$ 19.067.220 | R$ 13.919.318 | R$ 5.147.901 | R$ 14.042.361 | — |
| Média (6,5) | 527.042 | R$ 31.622.520 | R$ 13.919.318 | R$ 17.703.201 | R$ 14.042.361 | ✅ ok |
| Boa (8) | 905.684 | R$ 54.341.040 | R$ 13.919.318 | R$ 40.421.721 | R$ 14.042.361 | ✅ ok |
| Ótima (9,5) | 2.974.258 | R$ 178.455.480 | R$ 13.919.318 | R$ 164.536.161 | R$ 14.042.361 | ✅ ok |
| Variada (5 · 7 · 6) | 459.362 | R$ 27.561.740 | R$ 13.919.318 | R$ 13.642.421 | R$ 14.042.361 | — |

# Outros tamanhos (informativo, sem meta)

## Indie · Pequeno

Equipe: fundador + 0 · jogos de 8 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 7.726 | R$ 115.890 | R$ 1.891.665 | -R$ 1.775.775 | R$ 5.604.805 | — |
| Fraca (5) | 10.670 | R$ 160.050 | R$ 1.891.665 | -R$ 1.731.615 | R$ 5.565.204 | — |
| Média (6,5) | 17.072 | R$ 256.080 | R$ 1.891.665 | -R$ 1.635.585 | R$ 5.323.578 | — |
| Boa (8) | 28.521 | R$ 427.815 | R$ 1.891.665 | -R$ 1.463.850 | R$ 4.865.504 | — |
| Ótima (9,5) | 484.039 | R$ 7.260.585 | R$ 1.891.665 | R$ 5.368.920 | R$ 2.129.018 | — |
| Variada (5 · 7 · 6) | 14.988 | R$ 224.825 | R$ 1.891.665 | -R$ 1.666.840 | R$ 5.370.924 | — |

## Escritório · Médio

Equipe: fundador + 1 · jogos de 15 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 26.013 | R$ 988.494 | R$ 3.081.660 | -R$ 2.093.166 | R$ 7.372.700 | — |
| Fraca (5) | 36.018 | R$ 1.368.684 | R$ 3.081.660 | -R$ 1.712.976 | R$ 6.612.244 | — |
| Média (6,5) | 58.011 | R$ 2.204.418 | R$ 3.081.660 | -R$ 877.242 | R$ 4.940.776 | — |
| Boa (8) | 97.446 | R$ 3.702.948 | R$ 3.081.660 | R$ 621.287 | R$ 3.345.693 | — |
| Ótima (9,5) | 763.666 | R$ 29.019.308 | R$ 3.081.660 | R$ 25.937.647 | R$ 3.345.693 | — |
| Variada (5 · 7 · 6) | 50.862 | R$ 1.932.781 | R$ 3.081.660 | -R$ 1.148.879 | R$ 5.353.190 | — |

## Estúdio · Grande

Equipe: fundador + 3 · jogos de 22 semanas · começa com caixa zero

| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | Capital necessário | Resultado |
|---|---:|---:|---:|---:|---:|---|
| Ruim (3) | 47.476 | R$ 3.560.700 | R$ 7.937.901 | -R$ 4.377.201 | R$ 16.991.849 | — |
| Fraca (5) | 64.081 | R$ 4.806.075 | R$ 7.937.901 | -R$ 3.131.826 | R$ 14.500.949 | — |
| Média (6,5) | 89.394 | R$ 6.704.550 | R$ 7.937.901 | -R$ 1.233.351 | R$ 10.703.849 | — |
| Boa (8) | 132.118 | R$ 9.908.850 | R$ 7.937.901 | R$ 1.970.948 | R$ 8.260.483 | — |
| Ótima (9,5) | 580.049 | R$ 43.503.675 | R$ 7.937.901 | R$ 35.565.773 | R$ 8.260.483 | — |
| Variada (5 · 7 · 6) | 80.875 | R$ 6.065.675 | R$ 7.937.901 | -R$ 1.872.226 | R$ 11.690.399 | — |


