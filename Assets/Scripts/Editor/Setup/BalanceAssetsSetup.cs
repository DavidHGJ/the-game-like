using System;
using System.IO;
using System.Linq;
using GameLike.Data.Balance;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Cria uma instância de cada <see cref="BalanceAsset"/> concreto em
    /// <c>Assets/Resources/Balance/</c> caso ainda não exista (há exatamente uma
    /// instância por tipo — ver <c>BalanceProvider</c>). Idempotente: nunca
    /// sobrescreve um asset já criado (preserva valores editados no Inspector).
    /// Usado via menu ou em batch (<c>-executeMethod</c>).
    /// </summary>
    public static class BalanceAssetsSetup
    {
        private const string FolderPath = "Assets/Resources/Balance";

        [MenuItem("Tools/GameLike/Setup/Create Missing Balance Assets")]
        public static void CreateMissing()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            var balanceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(BalanceAsset).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

            int created = 0;
            foreach (var type in balanceTypes)
            {
                string assetName = type.Name.EndsWith("SO") ? type.Name[..^2] : type.Name;
                string path = $"{FolderPath}/{assetName}.asset";
                if (AssetDatabase.LoadAssetAtPath<BalanceAsset>(path) != null) continue;

                var instance = ScriptableObject.CreateInstance(type);
                instance.name = assetName;
                AssetDatabase.CreateAsset(instance, path);
                created++;
                Debug.Log($"[BalanceAssetsSetup] Criado {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceAssetsSetup] Concluído — {created} asset(s) novo(s), " +
                      $"{balanceTypes.Length} tipo(s) de balance no total.");
        }
    }
}
