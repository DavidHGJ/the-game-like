# The Game Like

Tycoon roguelite de gestão de uma desenvolvedora de jogos. Unity 6000.3, C#, solo dev.

## Como encaixar estes arquivos no projeto Unity

1. Crie um projeto Unity **6000.3** (2D ou 3D core, tanto faz por ora).
2. Copie a pasta `Assets/Scripts/` deste pacote para dentro de `Assets/` do seu projeto.
3. Copie `CLAUDE.md`, `agents.md` e este `README.md` para a raiz do projeto (ao lado de `Assets/`).
4. O Unity compila automaticamente ao detectar os scripts.

Os arquivos de fundação são C# puro e compilam em qualquer projeto Unity 6000.3 sem dependências externas. Nenhum depende de cena ou asset visual.

## Estrutura

```
Assets/Scripts/
├── Core/       [GameLike.Core]      EventBus, IGameEvent, EventBusLogger, Bootstrap, SceneLoader
├── Events/     [GameLike.Events]    Structs de evento de domínio (depende de Core)
├── Data/       [GameLike.Data]      ScriptableObjects de definição (depende de Core, Events)
├── State/      [GameLike.State]     Classes de estado serializáveis (depende de Core, Events, Data)
└── Systems/    [GameLike.Systems]   Managers por domínio — Time/, Economy/ (depende de todos acima)
```

Cada pasta tem seu próprio `.asmdef`. A regra de dependência é estritamente de baixo para cima: `Core ← Events ← Data ← State ← Systems`.

## Cenas (Boot persistente + aditivas)

`Bootstrap.cs` vive na cena `Boot`, que nunca é descarregada. Ela marca os managers persistentes com `DontDestroyOnLoad` e usa `SceneLoader` para carregar a primeira cena de conteúdo (configurável no Inspector, ex: `MainMenu`) aditivamente. Trocar de tela depois é `SceneLoader.SwitchTo("NomeDaCena")` — carrega a nova, torna ativa, descarrega a antiga, sem matar a Boot.

**Setup mínimo de cena Boot:**
1. Crie `Assets/Scenes/Boot.unity`.
2. Um GameObject vazio `[Bootstrap]` com o componente `Bootstrap`.
3. Um GameObject filho `PersistentServices` com `SceneLoader`, `TimeManager`, `EconomyManager`.
4. Arraste `PersistentServices` para o campo `persistentServicesRoot` do `Bootstrap`.
5. Defina `firstContentScene` (ex: `"MainMenu"`).
6. Adicione `Boot` e as cenas de conteúdo em Build Settings, com `Boot` em primeiro lugar.

## Checklist de configuração inicial (uma vez, ao criar o projeto)

- [ ] **Template:** 2D (URP) no Unity Hub
- [ ] **Input System:** Edit → Project Settings → Player → Active Input Handling → **Input System Package (New)**. Instale o pacote via Package Manager (`com.unity.inputsystem`) se ainda não vier com o template.
- [ ] **UI Toolkit:** geralmente já vem habilitado nos templates recentes do Unity 6. Confirme em Package Manager que `com.unity.modules.uielements` está presente.
- [ ] **Player Settings → Company Name:** `PlaceholderStudio` *(placeholder — ver aviso abaixo)*
- [ ] **Player Settings → Bundle Identifier:** `com.placeholderstudio.gamelike` *(placeholder)*
- [ ] **Após importar este pacote:** criar uma instância de cada `BalanceAsset` em `Assets/Resources/Balance/` via Assets → Create → GameLike → Balance → (cada tipo). Os SOs existem como classe, mas nenhuma instância foi criada ainda.

> ⚠️ **Troque o Company Name antes de gerar saves de teste reais.** Ele compõe o caminho de save no disco do jogador. Mudar depois invalida saves já criados.

## Arquitetura em uma frase

Três camadas: **Definição** (ScriptableObjects imutáveis) + **Estado** (classes serializáveis mutáveis) + **Sistemas** (managers que conversam via EventBus). Detalhes em `CLAUDE.md`.

## Documentação

- **`CLAUDE.md`** — arquitetura completa, padrões e convenções. Leitura obrigatória para qualquer um (humano ou IA) que for codar.
- **`agents.md`** — roadmap de implementação, ordem de dependência e tarefas delegáveis.
- **GDD v1.0** — design completo do jogo (documento separado, fonte da verdade das regras).

## Testar o EventBus rapidamente

Crie uma cena, adicione um GameObject com `TimeManager` e outro com `EconomyManager`, dê play. O console deve mostrar os `WeekTickEvent` coloridos sendo publicados conforme o tempo avança. Ajuste `secondsPerWeekAtBaseSpeed` no Inspector para acelerar.

Para silenciar o tick (verbose) e ver só eventos relevantes, ajuste `EventBusLogger.MinSeverity` ou adicione `EventCategory.Time` a `EventBusLogger.MutedCategories`.

## Princípios

Código limpo, modular, tipado. Nomes em inglês. Sem libs externas sem autorização. Conteúdo de jogo é sempre ScriptableObject, nunca hardcoded. Extensibilidade primeiro.
