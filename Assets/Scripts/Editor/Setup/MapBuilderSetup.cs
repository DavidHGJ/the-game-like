using System.IO;
using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Apoio do Map Builder: cria cenários (vazio por fase / cenário de teste), gera assentos
    /// automáticos nas cadeiras e garante que os cenários existentes tenham boneco e assento
    /// do fundador. Ferramenta de produção — só existe no editor.
    /// </summary>
    public static class MapBuilderSetup
    {
        private const string OfficeFolder = "Assets/Resources/Office";
        private const string DefFolder = "Assets/Data/Furniture3D";
        private const string CharacterPath = "Assets/Art/Characters/Dev/Dev.fbx";

        /// <summary>Boneco padrão (Dev.fbx) usado nos assentos.</summary>
        public static GameObject DefaultCharacter() => AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);

        private static FurnitureDef3DSO Def(string key) =>
            AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>(DefFolder + "/" + key + ".asset");

        private static Vector2Int DefaultSize(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Garage => new Vector2Int(8, 7),
            CompanyPhase.Indie => new Vector2Int(10, 9),
            CompanyPhase.Office => new Vector2Int(14, 11),
            _ => new Vector2Int(18, 14)
        };

        private static OfficeLayoutSO CreateAsset(string baseName, CompanyPhase phase)
        {
            if (!Directory.Exists(OfficeFolder)) Directory.CreateDirectory(OfficeFolder);
            var layout = ScriptableObject.CreateInstance<OfficeLayoutSO>();
            var size = DefaultSize(phase);
            layout.EditorSetPhase(phase);
            layout.EditorSetSize(size.x, size.y);
            layout.EditorSetCharacterPrefab(DefaultCharacter());
            string path = AssetDatabase.GenerateUniqueAssetPath(OfficeFolder + "/" + baseName + ".asset");
            AssetDatabase.CreateAsset(layout, path);
            return layout;
        }

        /// <summary>Cria um cenário vazio para a fase, já em Resources/Office (o jogo carrega por fase).</summary>
        public static OfficeLayoutSO CreateEmpty(CompanyPhase phase)
        {
            var layout = CreateAsset(phase + "Layout", phase);
            AssetDatabase.SaveAssets();
            return layout;
        }

        /// <summary>
        /// Cria um assento em cada cadeira que ainda não tem um (na ordem em que as cadeiras
        /// aparecem no cenário). Devolve quantos assentos foram criados.
        /// </summary>
        public static int AddSeatsOnChairs(OfficeLayoutSO layout)
        {
            if (layout == null) return 0;
            int added = 0;
            var seats = layout.EditorSeats;
            foreach (var p in layout.EditorPlacements)
            {
                if (p.furniture == null || p.furniture.Category != FurnitureCategory.Chair) continue;
                bool has = false;
                for (int i = 0; i < seats.Count; i++)
                    if (OfficeBuilder.TryFindChairAt(layout, seats[i].cell, out var chair) && chair.cell == p.cell) { has = true; break; }
                if (has) continue;
                seats.Add(new SeatPoint { cell = p.cell });
                added++;
            }
            if (layout.CharacterPrefab == null) layout.EditorSetCharacterPrefab(DefaultCharacter());
            EditorUtility.SetDirty(layout);
            return added;
        }

        /// <summary>
        /// Cenário de TESTE da fase Indie: três estações de trabalho com assentos (fundador + 2
        /// funcionários), sofá, plantas e luzes — para validar a ferramenta e os bonecos.
        /// </summary>
        [MenuItem("Tools/GameLike/Office 3D/6 - Gerar cenário de teste (Indie)")]
        public static OfficeLayoutSO CreateTestScenario()
        {
            var layout = CreateAsset("IndieTestLayout", CompanyPhase.Indie);
            layout.EditorSetColors(new Color(0.66f, 0.62f, 0.56f), new Color(0.56f, 0.66f, 0.74f));
            var list = layout.EditorPlacements;

            void Put(string key, int x, int y, int rot = 0)
            {
                var def = Def(key);
                if (def != null) list.Add(new FurniturePlacement { furniture = def, cell = new Vector2Int(x, y), rotationSteps = rot });
                else Debug.LogWarning("[MapBuilder] Móvel não encontrado: " + key);
            }

            Put("window", 2, 0); Put("poster", 5, 0); Put("whiteboard", 7, 0); Put("clock", 9, 0);
            Put("workstation", 1, 2); Put("office_chair", 1, 3, 2);
            Put("workstation", 4, 2); Put("office_chair", 4, 3, 2);
            Put("workstation", 7, 2); Put("gaming_chair", 7, 3, 2);
            Put("rug", 4, 5); Put("sofa", 4, 7, 2); Put("coffee_table", 4, 6);
            Put("plant", 0, 0); Put("plant_small", 9, 8); Put("floor_lamp", 0, 8);
            Put("bookshelf", 0, 4, 1); Put("water_cooler", 9, 4); Put("mini_fridge", 9, 6);

            layout.EditorCeilingLights.Add(new CeilingLight { cell = new Vector2Int(2, 4), color = new Color(1f, 0.95f, 0.85f), intensity = 2.2f });
            layout.EditorCeilingLights.Add(new CeilingLight { cell = new Vector2Int(7, 4), color = new Color(1f, 0.95f, 0.85f), intensity = 2.2f });
            AddSeatsOnChairs(layout);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            Debug.Log("[MapBuilder] Cenário de teste criado: " + AssetDatabase.GetAssetPath(layout) + " (" + layout.EditorSeats.Count + " assentos).");
            return layout;
        }

        /// <summary>
        /// Garante que todo cenário em Resources/Office tem boneco e ao menos o assento do
        /// fundador (cria assentos nas cadeiras se o cenário não tiver nenhum) e que existe um
        /// cenário para a fase Indie (o de teste). Idempotente; usado também em modo batch.
        /// </summary>
        [MenuItem("Tools/GameLike/Office 3D/7 - Garantir assentos nos cenários")]
        public static void EnsureSeats()
        {
            bool hasIndie = false;
            foreach (var guid in AssetDatabase.FindAssets("t:OfficeLayoutSO", new[] { OfficeFolder }))
            {
                var layout = AssetDatabase.LoadAssetAtPath<OfficeLayoutSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (layout == null) continue;
                if (layout.Phase == CompanyPhase.Indie) hasIndie = true;
                if (layout.CharacterPrefab == null) layout.EditorSetCharacterPrefab(DefaultCharacter());
                if (layout.EditorSeats.Count == 0) AddSeatsOnChairs(layout);
                EditorUtility.SetDirty(layout);
            }
            if (!hasIndie) CreateTestScenario();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Entrada para o modo batch (CI).</summary>
        public static void BatchEnsure()
        {
            EnsureSeats();
            AssetDatabase.Refresh();
            Debug.Log("GAMELIKE_MAPBUILDER_OK");
            EditorApplication.Exit(0);
        }
    }
}
