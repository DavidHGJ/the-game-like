using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using GameLike.State;
using GameLike.State.Meta;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using UnityEngine;

namespace GameLike.Systems.Save
{
    /// <summary>
    /// Objeto raiz do save (GDD 21.6): inclui <see cref="schemaVersion"/> para
    /// versionamento obrigatório + migração, a run atual e a meta-progressão.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int schemaVersion;
        public RunState run;
        public MetaProgressionState meta;
    }

    /// <summary>
    /// Persistência do jogo: JSON serializado e comprimido com gzip (GDD 21.6),
    /// com versão de schema e ponto de migração obrigatórios. A
    /// (de)serialização em bytes é pura e testável; a escrita em disco usa
    /// <see cref="Application.persistentDataPath"/> por slot.
    /// </summary>
    public static class SaveManager
    {
        /// <summary>Versão atual do schema de save. Incremente ao mudar a estrutura e adicione a migração.</summary>
        /// <remarks>
        /// v2: <see cref="GameLike.State.ProjectState"/> ganhou campos de console/plataforma
        /// (consoleId/Display/Key, consolePopularity01) — GDD 15. Saves v1 migram sem perda:
        /// projetos antigos ficam sem console (alcance de plataforma neutro).
        /// </remarks>
        public const int CurrentSchemaVersion = 7;

        private static string SlotPath(int slot) =>
            Path.Combine(Application.persistentDataPath, $"save_{slot}.gz");

        /// <summary>Serializa um save para bytes (JSON + gzip). Puro — testável sem disco.</summary>
        public static byte[] SerializeToBytes(SaveData data)
        {
            string json = JsonUtility.ToJson(data);
            byte[] raw = Encoding.UTF8.GetBytes(json);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
                gzip.Write(raw, 0, raw.Length);
            return output.ToArray();
        }

        /// <summary>Desserializa bytes (gzip + JSON) para um save, aplicando migração se necessário.</summary>
        public static SaveData DeserializeFromBytes(byte[] bytes)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            string json = reader.ReadToEnd();
            var data = JsonUtility.FromJson<SaveData>(json);
            return Migrate(data);
        }

        /// <summary>Grava a run + meta num slot. Retorna false em erro de IO.</summary>
        public static bool Save(int slot, RunState run, MetaProgressionState meta)
        {
            try
            {
                var data = new SaveData { schemaVersion = CurrentSchemaVersion, run = run, meta = meta };
                File.WriteAllBytes(SlotPath(slot), SerializeToBytes(data));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Falha ao salvar slot {slot}: {ex.Message}");
                return false;
            }
        }

        /// <summary>Carrega um slot. Retorna null se não existe ou falhou.</summary>
        public static SaveData Load(int slot)
        {
            try
            {
                var path = SlotPath(slot);
                if (!File.Exists(path)) return null;
                return DeserializeFromBytes(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Falha ao carregar slot {slot}: {ex.Message}");
                return null;
            }
        }

        public static bool HasSave(int slot) => File.Exists(SlotPath(slot));

        /// <summary>
        /// Lê apenas o nome da empresa salva num slot (para o botão "Continuar" do menu),
        /// sem religar managers. Retorna null se o slot não existe ou falhou.
        /// </summary>
        public static string PeekCompanyName(int slot)
        {
            var data = Load(slot);
            return data?.run?.company?.name;
        }

        public static void Delete(int slot)
        {
            var path = SlotPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Migra um save de versão N para a versão atual. Hoje só há a v1; novas versões
        /// adicionam casos aqui, nunca quebrando saves antigos silenciosamente (GDD 21.6).
        /// </summary>
        private static SaveData Migrate(SaveData data)
        {
            if (data == null) return null;

            // v1 → v2: campos de console foram adicionados ao ProjectState. Nada a
            // transformar nos dados existentes — os novos campos já desserializam com
            // seus defaults (sem console = alcance de plataforma neutro). Só avança a versão.
            if (data.schemaVersion < 2)
                data.schemaVersion = 2;

            // v2 → v3: produção em fases, cauda de vendas, rivais, árvore de habilidades e
            // fundador. Só campos novos — basta garantir coleções válidas; jogos lançados em
            // saves antigos seguem sem cauda de vendas (já receberam a receita de uma vez).
            if (data.schemaVersion < 3)
                data.schemaVersion = 3;

            // v3 → v4: inflação e variação de custo por run (GDD 23.12). Saves antigos não têm o
            // fator sorteado: valem custo base (1) e a inflação começa a contar do zero.
            if (data.schemaVersion < 4)
            {
                if (data.run != null && data.run.company != null && data.run.company.costVariation <= 0f)
                    data.run.company.costVariation = 1f;
                data.schemaVersion = 4;
            }

            // v4 → v5: pontos por aptidão (GDD 23.16). A alocação da fase em andamento era em pontos
            // absolutos e vira fração (soma 1); o fundador ganha aptidão de base sorteada e a run começa
            // dela. Funcionários sem aptidão recebem a da especialidade na primeira consulta.
            if (data.schemaVersion < 5)
            {
                var project = data.run?.currentProject;
                if (project != null && project.paramAllocated != null && project.paramAllocated.Length == ProjectState.ParameterCount)
                {
                    var current = GameLike.Data.Definitions.ParameterCatalog.AvailableFor(
                        (GameLike.Data.Definitions.ProductionPhase)Mathf.Clamp(project.phaseIndex, 0, GameLike.Data.Definitions.ParameterCatalog.PhaseCount - 1),
                        project.size);
                    var shares = new float[ProjectState.ParameterCount];
                    float total = 0f;
                    foreach (var gp in current) total += Mathf.Max(0f, project.paramAllocated[(int)gp]);
                    foreach (var gp in current)
                        shares[(int)gp] = total > 0f ? Mathf.Max(0f, project.paramAllocated[(int)gp]) / total : 1f / current.Count;
                    project.paramAllocated = shares;
                }
                if (data.meta != null)
                {
                    data.meta.EnsureCollections();
                    if (data.meta.founder.aptitude == null || data.meta.founder.aptitude.Length != ProjectState.AreaCount)
                        data.meta.founder.aptitude = AptitudeCalculator.RollFounder(new System.Random());
                    if (data.run != null) data.run.founderAptitude = AptitudeCalculator.Copy(data.meta.founder.aptitude);
                }
                data.schemaVersion = 5;
            }

            // v5 → v6: vendas semana a semana (GDD 23.19). Jogos lançados com a cauda antiga (total e duração fixos)
            // continuam de onde estavam: a 1ª semana e a próxima saem da cauda antiga, e a queda vira 1 − retenção.
            if (data.schemaVersion < 6)
            {
                if (data.run?.releasedProjects != null)
                    foreach (var released in data.run.releasedProjects)
                    {
                        if (released == null || released.firstWeekUnits > 0) continue;
                        if (released.salesWeeksTotal <= 0 || released.salesWeek >= released.salesWeeksTotal)
                        {
                            released.salesEnded = true;
                            continue;
                        }
                        released.firstWeekUnits = SalesCalculator.LegacyTailUnitsForWeek(released.forecastUnits, 0, released.salesWeeksTotal, released.salesRetention);
                        released.nextWeekUnits = SalesCalculator.LegacyTailUnitsForWeek(released.forecastUnits, released.salesWeek, released.salesWeeksTotal, released.salesRetention);
                        released.salesDecay = Mathf.Clamp(1f - released.salesRetention, 0.005f, 0.95f);
                        released.salesEnded = released.firstWeekUnits <= 0;
                    }
                data.schemaVersion = 6;
            }

            // v6 → v7: expansão também exige jogos lançados na fase (GDD 23.19). Saves antigos não contavam isso:
            // vale o total de jogos já lançados, para não travar quem já estava pronto para expandir.
            if (data.schemaVersion < 7)
            {
                if (data.run?.company != null) data.run.company.gamesInPhase = data.run.company.gamesReleased;
                data.schemaVersion = 7;
            }

            data.run?.EnsureCollections();
            data.meta?.EnsureCollections();

            if (data.schemaVersion != CurrentSchemaVersion)
                Debug.LogWarning($"[SaveManager] Save v{data.schemaVersion} != atual v{CurrentSchemaVersion}. Migração incompleta.");
            return data;
        }
    }
}
