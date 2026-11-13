namespace GameLike.Core
{
    /// <summary>
    /// Diz se as ferramentas do dev (Map Builder, tela de Parâmetros) existem nesta execução.
    /// Decidido na compilação: ligadas no editor e em development build, desligadas no build
    /// publicado para os jogadores — não há arquivo, tecla ou senha que as reative lá.
    /// </summary>
    public static class DevTools
    {
        /// <summary>true no editor e em development build; false no build publicado.</summary>
        public static bool Enabled =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif
    }
}
