using System;
using System.Collections.Generic;
using GameLike.Data.Base;
using UnityEngine;

namespace GameLike.Data.Definitions
{
    /// <summary>Categorias de evento narrativo (GDD 11.2).</summary>
    public enum NarrativeCategory
    {
        PublicStatement,
        OperationalCrisis,
        EthicalDilemma,
        Opportunity,
        HumanResources,
        Market,
        Absurd
    }

    /// <summary>Habilidades do fundador (GDD 14) — podem desbloquear opções de evento.</summary>
    public enum FounderSkill
    {
        None = 0,
        Business,
        Leadership,
        Technical,
        Marketing,
        Charisma,
        Networking
    }

    /// <summary>Tipo de efeito que uma opção de evento aplica ao estado da run.</summary>
    public enum EventEffectType
    {
        Cash,            // delta em $
        Reputation,      // delta em reputation01 (0..1)
        Hype,            // delta no hype do projeto ativo (0..1)
        TeamBurnout,     // delta no burnout de toda a equipe (0..1)
        TeamMood,        // delta no humor de toda a equipe (0..1)
        ThemeTrend,      // delta na tendência do tema do projeto ativo (0..1)
        Quality          // delta no saldo de decisões de desenvolvimento do projeto ativo (-1..+1, GDD 19.7)
    }

    /// <summary>Uma consequência de escolher uma opção (oculta ao jogador antes de escolher, GDD 11.1).</summary>
    [Serializable]
    public struct EventEffect
    {
        public EventEffectType type;
        public float amount;
    }

    /// <summary>Uma opção de resposta de um evento (2 a 4 por evento — GDD 11.1).</summary>
    [Serializable]
    public sealed class NarrativeOption
    {
        [TextArea] public string text;
        [Tooltip("Habilidade do fundador exigida para desbloquear esta opção (None = sempre disponível).")]
        public FounderSkill requiredSkill;
        public List<EventEffect> effects = new List<EventEffect>();
    }

    /// <summary>
    /// Evento narrativo (GDD 11): texto curto e caricato + 2-4 opções com consequências
    /// ocultas. Pausa o tempo e exige resposta imediata; cada evento é isolado (sem
    /// encadeamento). Extensível — adicionar N eventos nunca toca no código.
    ///
    /// <b>Slice:</b> o texto é <c>string</c> pt-BR direto (não <c>LocalizedString</c>),
    /// porque a integração com o Localization Package ainda é pendência e o texto do
    /// evento é o próprio gameplay — migrar para chave de localização depois.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Narrative Event", fileName = "NewEvent")]
    public sealed class NarrativeEventSO : DefinitionAsset
    {
        [Header("Conteúdo")]
        [SerializeField] private string title;
        [TextArea(2, 5)] [SerializeField] private string body;
        [SerializeField] private NarrativeCategory category;

        [Header("Elegibilidade")]
        [Tooltip("Peso relativo de sorteio (maior = mais frequente).")]
        [SerializeField] private float weight = 1f;
        [Tooltip("Se verdadeiro, só dispara quando há um projeto em desenvolvimento.")]
        [SerializeField] private bool requiresActiveProject;

        [Header("Opções (2 a 4)")]
        [SerializeField] private List<NarrativeOption> options = new List<NarrativeOption>();

        public string Title => title;
        public string Body => body;
        public NarrativeCategory Category => category;
        public float Weight => Mathf.Max(0.01f, weight);
        public bool RequiresActiveProject => requiresActiveProject;
        public IReadOnlyList<NarrativeOption> Options => options;
    }
}
