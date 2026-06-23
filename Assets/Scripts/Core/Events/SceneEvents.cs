namespace GameLike.Core.Events
{
    /// <summary>
    /// Raised right before a content scene starts loading additively.
    /// Useful for showing loading UI.
    /// </summary>
    public readonly struct SceneLoadStartedEvent : IGameEvent
    {
        public readonly string SceneName;

        public SceneLoadStartedEvent(string sceneName)
        {
            SceneName = sceneName;
        }

        public EventCategory Category => EventCategory.System;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Loading scene \"{SceneName}\"";
    }

    /// <summary>
    /// Raised after a content scene finished loading additively and was set active.
    /// </summary>
    public readonly struct SceneLoadCompletedEvent : IGameEvent
    {
        public readonly string SceneName;

        public SceneLoadCompletedEvent(string sceneName)
        {
            SceneName = sceneName;
        }

        public EventCategory Category => EventCategory.System;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Scene \"{SceneName}\" loaded and active";
    }

    /// <summary>
    /// Raised after a previous content scene was unloaded, before the next one loads.
    /// </summary>
    public readonly struct SceneUnloadedEvent : IGameEvent
    {
        public readonly string SceneName;

        public SceneUnloadedEvent(string sceneName)
        {
            SceneName = sceneName;
        }

        public EventCategory Category => EventCategory.System;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Scene \"{SceneName}\" unloaded";
    }
}
