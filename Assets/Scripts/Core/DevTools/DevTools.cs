using System.IO;
using UnityEngine;

namespace GameLike.Core
{
    /// <summary>
    /// Diz se as ferramentas do dev (Map Builder, tela de Parâmetros) aparecem. Sempre no
    /// editor e em development build; num build de release, só se existir um arquivo
    /// <c>devtools.txt</c> ao lado do executável — assim o mesmo build serve para os jogadores
    /// (sem ferramentas) e para o dev (basta criar o arquivo vazio).
    /// </summary>
    public static class DevTools
    {
        private const string MarkerFile = "devtools.txt";
        private static bool? _enabled;

        /// <summary>true se as ferramentas do dev estão liberadas nesta execução.</summary>
        public static bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value;
                bool marker = false;
                try
                {
                    string folder = Path.GetDirectoryName(Application.dataPath);
                    marker = !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, MarkerFile));
                }
                catch (IOException) { marker = false; }
                _enabled = Application.isEditor || Debug.isDebugBuild || marker;
                return _enabled.Value;
            }
        }
    }
}
