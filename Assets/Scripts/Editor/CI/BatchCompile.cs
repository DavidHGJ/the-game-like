using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Helper de linha de comando para checar a compilação do projeto em batch mode,
    /// sem abrir o editor. Uso:
    /// <c>Unity.exe -batchmode -nographics -quit -projectPath . -logFile log.txt
    /// -executeMethod GameLike.Editor.CI.BatchCompile.Check</c>.
    ///
    /// Se os scripts falham ao compilar, o próprio Unity registra os erros CS no log
    /// e este método nem chega a ser chamado — então o log carrega a verdade. Quando
    /// a compilação está limpa, este método imprime um marcador único e sai com 0.
    /// </summary>
    public static class BatchCompile
    {
        private const string Marker = "GAMELIKE_COMPILE_OK";

        public static void Check()
        {
            int assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies).Length;
            Debug.Log($"{Marker} — assemblies: {assemblies}");
            EditorApplication.Exit(0);
        }
    }
}
