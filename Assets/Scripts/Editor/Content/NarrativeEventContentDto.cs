using System;

namespace GameLike.Editor.Content
{
    /// <summary>Forma de dados de um evento narrativo (GDD 11) para o importer.</summary>
    [Serializable]
    public sealed class NarrativeEventContentDto
    {
        public string id;
        public string key;
        public string title;
        public string body;
        public string category;         // nome do enum NarrativeCategory
        public float weight = 1f;
        public bool requiresActiveProject;
        public NarrativeOptionDto[] options;
    }

    [Serializable]
    public sealed class NarrativeOptionDto
    {
        public string text;
        public string requiredSkill;    // nome do enum FounderSkill (ou vazio/None)
        public EventEffectDto[] effects;
    }

    [Serializable]
    public sealed class EventEffectDto
    {
        public string type;             // nome do enum EventEffectType
        public float amount;
    }
}
