using System.Collections.Generic;
using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Central lookup for balance assets, so managers request a balance type
    /// once (typically cached in a field) instead of every manager needing a
    /// manually wired Inspector reference to every <see cref="BalanceAsset"/>
    /// it uses.
    ///
    /// Loads every <see cref="BalanceAsset"/> from a single well-known
    /// Resources folder at first use and caches them by type for the
    /// lifetime of the application. Since there is exactly one instance per
    /// balance type in the project (see BalanceAsset), type-based lookup is
    /// unambiguous.
    ///
    /// Editing a field on a balance asset in the Inspector takes effect on
    /// the very next read, because managers call <see cref="Get{T}"/> live —
    /// they must never copy a balance value into a local variable expecting
    /// it to stay current.
    /// </summary>
    public static class BalanceProvider
    {
        private const string ResourcesFolder = "Balance";

        private static readonly Dictionary<System.Type, BalanceAsset> Cache = new();
        private static bool _loaded;

        /// <summary>
        /// Returns the single instance of balance type <typeparamref name="T"/>.
        /// Logs an error and returns null if no asset of that type exists
        /// under Resources/Balance — this is a setup error, not a runtime one.
        /// </summary>
        public static T Get<T>() where T : BalanceAsset
        {
            EnsureLoaded();

            if (Cache.TryGetValue(typeof(T), out var asset))
            {
                return asset as T;
            }

            Debug.LogError($"[BalanceProvider] No asset of type {typeof(T).Name} found under " +
                            $"Resources/{ResourcesFolder}. Create one via the Assets menu.");
            return null;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;

            var assets = Resources.LoadAll<BalanceAsset>(ResourcesFolder);
            foreach (var asset in assets)
            {
                Cache[asset.GetType()] = asset;
            }

            _loaded = true;
        }

        /// <summary>
        /// Forces a re-scan of the Resources folder on next <see cref="Get{T}"/>.
        /// Intended for editor/test scenarios where assets may have been
        /// created or replaced after the cache was first populated.
        /// </summary>
        public static void InvalidateCache()
        {
            Cache.Clear();
            _loaded = false;
        }
    }
}
