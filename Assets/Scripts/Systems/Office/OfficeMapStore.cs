using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Onde os cenários do Map Builder in-game são salvos e de onde o jogo os lê.
    /// Cada cenário é um JSON (<see cref="OfficeMapData"/>).
    /// <list type="bullet">
    /// <item>No editor: <c>Assets/Resources/OfficeMaps/&lt;nome&gt;.json</c> — fica no projeto,
    /// entra no controle de versão e vai junto no build.</item>
    /// <item>Num build: lê os que vieram no build (Resources) e grava em
    /// <c>persistentDataPath/OfficeMaps</c>; um arquivo gravado ali com o mesmo nome tem prioridade.</item>
    /// </list>
    /// O jogo usa, em cada fase, o cenário marcado <see cref="OfficeMapData.usedInGame"/> daquela fase.
    /// </summary>
    public static class OfficeMapStore
    {
        private const string ResourcesFolder = "OfficeMaps";

        private static FurnitureCatalogSO _catalog;

        /// <summary>Pasta gravável dos cenários neste ambiente (editor ou build).</summary>
        public static string WritableFolder =>
            Application.isEditor
                ? Path.Combine(Application.dataPath, "Resources", ResourcesFolder)
                : Path.Combine(Application.persistentDataPath, ResourcesFolder);

        /// <summary>Catálogo de mobília (null se ainda não foi gerado pelo editor).</summary>
        public static FurnitureCatalogSO Catalog
        {
            get
            {
                if (_catalog == null) _catalog = Resources.Load<FurnitureCatalogSO>(FurnitureCatalogSO.ResourcePath);
                return _catalog;
            }
        }

        /// <summary>Tira do nome tudo que não pode virar nome de arquivo.</summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Cenario";
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            return sb.Length == 0 ? "Cenario" : sb.ToString();
        }

        /// <summary>Todos os cenários salvos, em ordem de fase e nome.</summary>
        public static List<OfficeMapData> ListAll()
        {
            var byName = new Dictionary<string, OfficeMapData>(StringComparer.OrdinalIgnoreCase);

            if (!Application.isEditor)
            {
                foreach (var asset in Resources.LoadAll<TextAsset>(ResourcesFolder))
                {
                    var map = Parse(asset.text, asset.name);
                    if (map != null) byName[map.name] = map;
                }
            }

            string folder = WritableFolder;
            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, "*.json"))
                {
                    var map = Parse(File.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
                    if (map != null) byName[map.name] = map;
                }
            }

            var list = new List<OfficeMapData>(byName.Values);
            list.Sort((a, b) => a.phase != b.phase
                ? a.phase.CompareTo(b.phase)
                : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        /// <summary>O cenário que o jogo deve usar na fase (null se nenhum está marcado).</summary>
        public static OfficeMapData FindForPhase(CompanyPhase phase)
        {
            foreach (var map in ListAll())
                if (map.usedInGame && map.phase == phase) return map;
            return null;
        }

        /// <summary>
        /// Grava o cenário e devolve o caminho do arquivo. Se ele está marcado para uso no jogo,
        /// desmarca os outros cenários da mesma fase (só um vale por fase).
        /// </summary>
        public static string Save(OfficeMapData map)
        {
            map.name = SanitizeName(map.name);
            map.schemaVersion = OfficeMapData.CurrentSchemaVersion;
            if (map.usedInGame)
            {
                foreach (var other in ListAll())
                {
                    if (!other.usedInGame || other.phase != map.phase) continue;
                    if (string.Equals(other.name, map.name, StringComparison.OrdinalIgnoreCase)) continue;
                    other.usedInGame = false;
                    Write(other);
                }
            }
            return Write(map);
        }

        /// <summary>Apaga o arquivo do cenário na pasta gravável. Devolve false se não existia.</summary>
        public static bool Delete(string mapName)
        {
            string path = PathFor(mapName);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
            return true;
        }

        /// <summary>true se já existe um cenário gravado com esse nome.</summary>
        public static bool Exists(string mapName) => File.Exists(PathFor(mapName));

        /// <summary>Interpreta o JSON de um cenário, migrando versões antigas. Null se inválido.</summary>
        public static OfficeMapData Parse(string json, string fallbackName)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            OfficeMapData map;
            try { map = JsonUtility.FromJson<OfficeMapData>(json); }
            catch (ArgumentException) { return null; }
            if (map == null) return null;
            if (string.IsNullOrWhiteSpace(map.name)) map.name = fallbackName;
            return Migrate(map);
        }

        /// <summary>Serializa o cenário para JSON legível.</summary>
        public static string ToJson(OfficeMapData map) => JsonUtility.ToJson(map, true);

        private static OfficeMapData Migrate(OfficeMapData map)
        {
            // v1 -> v2: entraram align/onTop por peça e floorStyle/wallStyle. Os valores padrão
            // (centro, no chão, liso) reproduzem um cenário v1 exatamente, então nada a converter.
            // v2 -> v3: parede lateral própria (sideWall*) e atividade por assento. Padrões =
            // lateral igual à do fundo e boneco trabalhando, que é o que a v2 mostrava.
            map.schemaVersion = OfficeMapData.CurrentSchemaVersion;
            map.pieces ??= new List<OfficeMapPiece>();
            map.lights ??= new List<CeilingLight>();
            map.seats ??= new List<SeatPoint>();
            return map;
        }

        private static string PathFor(string mapName) =>
            Path.Combine(WritableFolder, SanitizeName(mapName) + ".json");

        private static string Write(OfficeMapData map)
        {
            Directory.CreateDirectory(WritableFolder);
            string path = PathFor(map.name);
            File.WriteAllText(path, ToJson(map), new UTF8Encoding(false));
            return path;
        }
    }
}
