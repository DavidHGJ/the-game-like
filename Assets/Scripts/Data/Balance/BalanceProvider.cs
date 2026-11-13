using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Central lookup for balance assets, so managers request a balance type
    /// instead of every manager needing a manually wired Inspector reference to
    /// every <see cref="BalanceAsset"/> it uses.
    ///
    /// Loads every <see cref="BalanceAsset"/> from <c>Resources/Balance</c> at first
    /// use and caches them by type. Managers call <see cref="Get{T}"/> live — they
    /// must never copy a balance value into a local variable expecting it to stay
    /// current.
    ///
    /// Valores ajustados na tela in-game de Parâmetros são gravados como JSON por tipo
    /// (<see cref="SaveOverride"/>) e reaplicados por cima dos assets na carga: no editor em
    /// <c>Assets/Resources/BalanceOverrides</c> (vai no build e no controle de versão), num
    /// build em <c>persistentDataPath/BalanceOverrides</c> (que tem prioridade).
    /// </summary>
    public static class BalanceProvider
    {
        private const string ResourcesFolder = "Balance";
        private const string OverridesFolder = "BalanceOverrides";

        private static readonly Dictionary<System.Type, BalanceAsset> Cache = new();
        private static readonly Dictionary<System.Type, string> Defaults = new();
        private static bool _loaded;

        /// <summary>
        /// Returns the single instance of balance type <typeparamref name="T"/>. Se não houver
        /// asset desse tipo em Resources/Balance, cria uma instância em memória com os valores
        /// padrão do código (e avisa), para o jogo nunca ficar sem o parâmetro.
        /// </summary>
        public static T Get<T>() where T : BalanceAsset
        {
            EnsureLoaded();
            if (Cache.TryGetValue(typeof(T), out var asset) && asset != null) return asset as T;

            Debug.LogWarning($"[BalanceProvider] Sem asset de {typeof(T).Name} em Resources/{ResourcesFolder}; " +
                             "usando os valores padrão do código. Rode Tools → GameLike → Setup → Create Missing Balance Assets.");
            var created = ScriptableObject.CreateInstance<T>();
            created.name = typeof(T).Name.Replace("SO", "");
            created.hideFlags = HideFlags.DontSave;
            Register(created);
            return created;
        }

        /// <summary>Todos os assets de balanceamento carregados, em ordem de nome.</summary>
        public static List<BalanceAsset> All()
        {
            EnsureLoaded();
            var list = new List<BalanceAsset>(Cache.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        private static string WritableFolder =>
            Application.isEditor
                ? Path.Combine(Application.dataPath, "Resources", OverridesFolder)
                : Path.Combine(Application.persistentDataPath, OverridesFolder);

        private static string OverridePath(BalanceAsset asset) =>
            Path.Combine(WritableFolder, asset.GetType().Name + ".json");

        /// <summary>Grava os valores atuais do asset como ajuste salvo. Devolve o caminho do arquivo.</summary>
        public static string SaveOverride(BalanceAsset asset)
        {
            Directory.CreateDirectory(WritableFolder);
            string path = OverridePath(asset);
            File.WriteAllText(path, JsonUtility.ToJson(asset, true), new UTF8Encoding(false));
            return path;
        }

        /// <summary>true se o asset tem um ajuste salvo.</summary>
        public static bool HasOverride(BalanceAsset asset) =>
            File.Exists(OverridePath(asset)) || Resources.Load<TextAsset>(OverridesFolder + "/" + asset.GetType().Name) != null;

        /// <summary>Apaga o ajuste salvo e devolve o asset aos valores com que ele foi carregado.</summary>
        public static void ResetToDefaults(BalanceAsset asset)
        {
            string path = OverridePath(asset);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
            if (Defaults.TryGetValue(asset.GetType(), out string json)) JsonUtility.FromJsonOverwrite(json, asset);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            foreach (var asset in Resources.LoadAll<BalanceAsset>(ResourcesFolder)) Register(asset);
        }

        private static void Register(BalanceAsset asset)
        {
            Cache[asset.GetType()] = asset;
            Defaults[asset.GetType()] = JsonUtility.ToJson(asset);
            ApplyOverrides(asset);
        }

        private static void ApplyOverrides(BalanceAsset asset)
        {
            string typeName = asset.GetType().Name;
            if (!Application.isEditor)
            {
                var shipped = Resources.Load<TextAsset>(OverridesFolder + "/" + typeName);
                if (shipped != null) Overwrite(shipped.text, asset, typeName);
            }
            string path = OverridePath(asset);
            if (File.Exists(path)) Overwrite(File.ReadAllText(path), asset, typeName);
        }

        private static void Overwrite(string json, BalanceAsset asset, string typeName)
        {
            try { JsonUtility.FromJsonOverwrite(json, asset); }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning("[BalanceProvider] Ajuste salvo de " + typeName + " ilegível, ignorado: " + e.Message);
            }
        }

        /// <summary>
        /// Forces a re-scan of the Resources folder on next <see cref="Get{T}"/>.
        /// Intended for editor/test scenarios where assets may have been
        /// created or replaced after the cache was first populated.
        /// </summary>
        public static void InvalidateCache()
        {
            Cache.Clear();
            Defaults.Clear();
            _loaded = false;
        }
    }
}
