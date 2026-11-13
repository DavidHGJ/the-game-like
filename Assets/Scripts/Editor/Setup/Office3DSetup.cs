using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Core;
using GameLike.Systems.Economy;
using GameLike.Systems.Events;
using GameLike.Systems.Market;
using GameLike.Systems.Marketing;
using GameLike.Systems.Office;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using GameLike.Systems.Time;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Setup do escritório 3D data-driven: cria as definições de mobília 3D a
    /// partir dos FBX, autora o layout da Garage (sem sobreposição) e monta a
    /// cena de jogo com câmera ortográfica iso, luz, managers e o
    /// <see cref="OfficeView"/>. Ferramenta de produção do dev.
    /// </summary>
    public static class Office3DSetup
    {
        private const string FbxFolder = "Assets/Art/Furniture";
        private const string DefFolder = "Assets/Data/Furniture3D";
        private const string PrefabFolder = "Assets/Art/FurniturePrefabs";
        private const string MatFolder = "Assets/Art/Materials";
        private const string LayoutFolder = "Assets/Resources/Office";
        private const string ScenePath = "Assets/Scenes/GameOffice.unity";

        // Peças que ganham prefab próprio (FBX + material glow + luz modelada na peça),
        // em vez de referenciar o FBX cru. key -> nome do FBX.
        private static readonly (string key, string fbx)[] LightPieces =
        {
            ("workstation", "Workstation"), ("monitor", "Monitor"),
            ("floor_lamp", "FloorLamp"), ("desk_lamp", "DeskLamp"),
            ("light_point", "LightPoint"), ("arcade", "Arcade"),
            ("server_rack", "ServerRack"), ("window", "Window"),
            // pacote 2
            ("laptop", "Laptop"), ("tv_stand", "TvStand"), ("wall_tv", "WallTv"),
            ("vending_machine", "VendingMachine"), ("lava_lamp", "LavaLamp"), ("neon_sign", "NeonSign"),
        };

        private static bool _onlyMissingDefs;

        [MenuItem("Tools/GameLike/Office 3D/★ Build Everything (3D)")]
        public static void BuildEverything()
        {
            CreateFurnitureDefs();
            CreateGarageLayout();
            BuildGameScene();
            Debug.Log("[Office3D] Concluído. Abra a cena GameOffice e aperte Play.");
        }

        /// <summary>
        /// Recria apenas as definições de mobília e o layout da Garage (sem mexer
        /// na cena de jogo, para preservar managers já configurados). Alvo de
        /// execução headless após regenerar os FBX.
        /// </summary>
        public static void RebuildFurnitureAndLayout()
        {
            CreateFurnitureDefs();
            BuildFurniturePrefabs();
            CreateGarageLayout();
            Debug.Log("[Office3D] Defs + prefabs + layout da Garage recriados (cena intacta).");
        }

        [MenuItem("Tools/GameLike/Office 3D/1 - Create Furniture Defs")]
        public static void CreateFurnitureDefs()
        {
            EnsureFolder(DefFolder);
            // Mesas / estações.
            MakeDef("workstation", "Estação", "Workstation", FurnitureCategory.Desk, new Vector2Int(2, 1), true, false, FurnitureEffectType.EmployeeProductivity, 0.05f);
            MakeDef("desk", "Mesa", "Desk", FurnitureCategory.Desk, new Vector2Int(2, 1), true, false, FurnitureEffectType.None, 0f);
            MakeDef("coffee_table", "Mesa de centro", "CoffeeTable", FurnitureCategory.Decoration, new Vector2Int(1, 1), true, false, FurnitureEffectType.None, 0f);
            // Cadeiras / assentos.
            MakeDef("office_chair", "Cadeira", "OfficeChair", FurnitureCategory.Chair, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.03f);
            MakeDef("gaming_chair", "Cadeira gamer", "GamingChair", FurnitureCategory.Chair, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeProductivity, 0.04f);
            MakeDef("sofa", "Sofá", "Sofa", FurnitureCategory.Decoration, new Vector2Int(2, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.03f);
            MakeDef("armchair", "Poltrona", "Armchair", FurnitureCategory.Decoration, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.02f);
            MakeDef("beanbag", "Puff", "Beanbag", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.02f);
            // Armazenamento / estantes.
            MakeDef("bookshelf", "Estante", "Bookshelf", FurnitureCategory.Storage, new Vector2Int(1, 1), true, false, FurnitureEffectType.ResearchPointGenerationRate, 0.02f);
            MakeDef("server_rack", "Rack de servidor", "ServerRack", FurnitureCategory.Storage, new Vector2Int(1, 1), true, false, FurnitureEffectType.ResearchPointGenerationRate, 0.03f);
            MakeDef("mini_fridge", "Frigobar", "MiniFridge", FurnitureCategory.Storage, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.02f);
            MakeDef("cardboard_box", "Caixa", "CardboardBox", FurnitureCategory.Storage, new Vector2Int(1, 1), false, false, FurnitureEffectType.None, 0f);
            // Decoração / ambiente.
            MakeDef("monitor", "Monitor", "Monitor", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.None, 0f);
            MakeDef("plant", "Planta", "Plant", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.02f);
            MakeDef("plant_small", "Plantinha", "PlantSmall", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.01f);
            MakeDef("floor_lamp", "Luminária", "FloorLamp", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.01f);
            MakeDef("desk_lamp", "Abajur", "DeskLamp", FurnitureCategory.Decoration, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.01f);
            MakeDef("light_point", "Ponto de luz", "LightPoint", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.None, 0f);
            MakeDef("water_cooler", "Bebedouro", "WaterCooler", FurnitureCategory.Decoration, new Vector2Int(1, 1), false, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.02f);
            MakeDef("arcade", "Fliperama", "Arcade", FurnitureCategory.Decoration, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.04f);
            MakeDef("whiteboard", "Quadro branco", "Whiteboard", FurnitureCategory.Decoration, new Vector2Int(2, 1), true, false, FurnitureEffectType.ResearchPointGenerationRate, 0.02f);
            // Chão.
            MakeDef("rug", "Tapete", "Rug", FurnitureCategory.FloorFixture, new Vector2Int(2, 2), true, false, FurnitureEffectType.None, 0f);
            MakeDef("rug_round", "Tapete redondo", "RugRound", FurnitureCategory.FloorFixture, new Vector2Int(2, 2), false, false, FurnitureEffectType.None, 0f);
            // Parede.
            MakeDef("poster", "Pôster", "Poster", FurnitureCategory.WallFixture, new Vector2Int(1, 1), false, true, FurnitureEffectType.HypeGenerationRate, 0.01f);
            MakeDef("wall_shelf", "Prateleira", "WallShelf", FurnitureCategory.WallFixture, new Vector2Int(1, 1), false, true, FurnitureEffectType.ResearchPointGenerationRate, 0.01f);
            MakeDef("clock", "Relógio", "Clock", FurnitureCategory.WallFixture, new Vector2Int(1, 1), false, true, FurnitureEffectType.None, 0f);
            MakeDef("window", "Janela", "Window", FurnitureCategory.WallFixture, new Vector2Int(2, 1), false, true, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.01f);

            // Garagem (cenário inicial — fase 1).
            MakeDef("car", "Carro", "Car", FurnitureCategory.Decoration, new Vector2Int(3, 2), true, false, FurnitureEffectType.None, 0f);
            MakeDef("cabinet", "Armário", "Cabinet", FurnitureCategory.Storage, new Vector2Int(1, 1), true, false, FurnitureEffectType.ResearchPointGenerationRate, 0.01f);
            MakeDef("old_desk", "Mesinha", "OldDesk", FurnitureCategory.Desk, new Vector2Int(1, 1), true, false, FurnitureEffectType.None, 0f);
            MakeDef("old_pc", "PC antigo", "OldPC", FurnitureCategory.Decoration, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeProductivity, 0.02f);
            MakeDef("old_chair", "Cadeira antiga", "OldChair", FurnitureCategory.Chair, new Vector2Int(1, 1), true, false, FurnitureEffectType.EmployeeMoraleRecoveryRate, 0.01f);

            // Luzes embutidas (filhas que giram com a peça). O foco SAI de onde fica
            // a lâmpada da peça, na direção que ela aponta.
            var warm = new Color(1.0f, 0.90f, 0.68f);
            // Luminária de pé: cúpula no topo (y~1.5), abre embaixo -> spot pra baixo.
            SetLights("floor_lamp", new FurnitureLight { localPosition = new Vector3(0f, 1.50f, 0f), color = warm, intensity = 3.2f, range = 6.5f, spot = true, spotAngle = 95f });
            // Abajur de mesa: cúpula na ponta do braço (frente+alto), abre pra baixo.
            SetLights("desk_lamp", new FurnitureLight { localPosition = new Vector3(0f, 0.40f, 0.25f), color = warm, intensity = 2.4f, range = 3.0f, spot = true, spotAngle = 78f });
            // Rack: LED verde sutil na frente dos slots.
            SetLights("server_rack", new FurnitureLight { localPosition = new Vector3(0.1f, 1.0f, 0.33f), color = new Color(0.55f, 0.9f, 0.6f), intensity = 0.45f, range = 1.4f });
            // Telas (monitor/estação/fliperama): SEM point light — o brilho é a própria
            // tela acesa (material glow + Bloom), nada de poça na mesa.
            SetLights("workstation");
            SetLights("monitor");
            SetLights("arcade");
            // Janela: luz de dia suave entrando pro interior.
            SetLights("window", new FurnitureLight { localPosition = new Vector3(0f, 0.65f, 0.28f), color = new Color(1f, 0.97f, 0.9f), intensity = 1.1f, range = 4.5f });
            SetLights("light_point", new FurnitureLight { localPosition = new Vector3(0f, 0.15f, 0f), color = warm, intensity = 2.2f, range = 5.5f });

            DefinePack2();
            ApplyStackingFlags();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Office3D] Definições de mobília criadas em " + DefFolder);
        }

        /// <summary>
        /// Pacote 2 de mobília (FBX gerados por <c>Tools/Blender/gen_furniture_pack2.py</c>):
        /// peças pequenas que vão em cima de móveis, móveis de chão e peças de parede. Sem efeito
        /// de gameplay por enquanto (o GDD ainda não define o efeito dessas peças).
        /// </summary>
        private static void DefinePack2()
        {
            const FurnitureEffectType none = FurnitureEffectType.None;
            var one = new Vector2Int(1, 1);
            var wide = new Vector2Int(2, 1);
            // Em cima de móveis.
            MakeDef("laptop", "Notebook", "Laptop", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("books", "Livros", "Books", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("mug", "Caneca", "Mug", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("cactus", "Cacto", "Cactus", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("lava_lamp", "Luminária de lava", "LavaLamp", FurnitureCategory.Decoration, one, false, false, none, 0f);
            MakeDef("trophy", "Troféu", "Trophy", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("printer", "Impressora", "Printer", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("desk_fan", "Ventilador", "DeskFan", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("game_console", "Videogame", "GameConsole", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("speaker", "Caixa de som", "Speaker", FurnitureCategory.Decoration, one, true, false, none, 0f);
            // Chão.
            MakeDef("filing_cabinet", "Arquivo", "FilingCabinet", FurnitureCategory.Storage, one, true, false, none, 0f);
            MakeDef("trash_bin", "Lixeira", "TrashBin", FurnitureCategory.Decoration, one, false, false, none, 0f);
            MakeDef("round_table", "Mesa redonda", "RoundTable", FurnitureCategory.Desk, new Vector2Int(2, 2), false, false, none, 0f);
            MakeDef("standing_desk", "Mesa alta", "StandingDesk", FurnitureCategory.Desk, wide, true, false, none, 0f);
            MakeDef("partition", "Divisória", "Partition", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("vending_machine", "Máquina de lanches", "VendingMachine", FurnitureCategory.Decoration, one, true, false, none, 0f);
            MakeDef("tv_stand", "Rack com TV", "TvStand", FurnitureCategory.Decoration, wide, true, false, none, 0f);
            MakeDef("workbench", "Bancada", "Workbench", FurnitureCategory.Desk, wide, true, false, none, 0f);
            MakeDef("bicycle", "Bicicleta", "Bicycle", FurnitureCategory.Decoration, wide, true, false, none, 0f);
            MakeDef("coffee_station", "Cantinho do café", "CoffeeStation", FurnitureCategory.Storage, one, true, false, none, 0f);
            MakeDef("metal_shelf", "Estante de metal", "MetalShelf", FurnitureCategory.Storage, one, true, false, none, 0f);
            MakeDef("ping_pong", "Pingue-pongue", "PingPong", FurnitureCategory.Decoration, new Vector2Int(3, 2), true, false, none, 0f);
            MakeDef("stool", "Banqueta", "Stool", FurnitureCategory.Chair, one, false, false, none, 0f);
            // Parede.
            MakeDef("corkboard", "Mural de cortiça", "Corkboard", FurnitureCategory.WallFixture, one, false, true, none, 0f);
            MakeDef("wall_tv", "TV de parede", "WallTv", FurnitureCategory.WallFixture, wide, false, true, none, 0f);
            MakeDef("neon_sign", "Neon", "NeonSign", FurnitureCategory.WallFixture, one, false, true, none, 0f);
            MakeDef("ac_unit", "Ar-condicionado", "AcUnit", FurnitureCategory.WallFixture, one, false, true, none, 0f);

            SetLights("lava_lamp", new FurnitureLight { localPosition = new Vector3(0f, 0.2f, 0f), color = new Color(1f, 0.5f, 0.42f), intensity = 1.3f, range = 2.6f });
            SetLights("neon_sign", new FurnitureLight { localPosition = new Vector3(0f, 0.28f, 0.25f), color = new Color(1f, 0.45f, 0.6f), intensity = 1.8f, range = 4f });
            SetLights("vending_machine", new FurnitureLight { localPosition = new Vector3(-0.13f, 1.2f, 0.55f), color = new Color(0.8f, 0.92f, 1f), intensity = 0.8f, range = 2.5f });
        }

        /// <summary>
        /// Quem pode ir em cima de quem: peças pequenas (placeableOnTop) e móveis com tampo
        /// (supportsItemsOnTop, com a altura do tampo em metros; 0 = topo da malha).
        /// </summary>
        private static void ApplyStackingFlags()
        {
            foreach (string key in new[] { "monitor", "desk_lamp", "plant_small", "old_pc", "light_point", "laptop", "books", "mug",
                         "cactus", "lava_lamp", "trophy", "printer", "desk_fan", "game_console", "speaker" })
                SetStacking(key, true, false, 0f);
            SetStacking("cardboard_box", true, true, 0f);
            SetStacking("desk", false, true, 0.765f);
            SetStacking("workstation", false, true, 0.765f);
            SetStacking("old_desk", false, true, 0.76f);
            SetStacking("coffee_table", false, true, 0f);
            SetStacking("mini_fridge", false, true, 0f);
            SetStacking("filing_cabinet", false, true, 1.0f);
            SetStacking("round_table", false, true, 0.76f);
            SetStacking("standing_desk", false, true, 1.075f);
            SetStacking("workbench", false, true, 0.92f);
            SetStacking("ping_pong", false, true, 0.785f);
            // Cadeiras: aqui a altura é a do ASSENTO (onde o quadril do boneco apoia).
            SetStacking("office_chair", false, false, 0.53f);
            SetStacking("gaming_chair", false, false, 0.58f);
            SetStacking("old_chair", false, false, 0.49f);
            SetStacking("stool", false, false, 0.65f);
        }

        private static void SetStacking(string key, bool onTop, bool support, float height)
        {
            var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>($"{DefFolder}/{key}.asset");
            if (def == null) return;
            def.SetStacking(onTop, support, height);
            EditorUtility.SetDirty(def);
        }

        /// <summary>
        /// Acrescenta o pacote 2 SEM tocar no que já existe: cria só as definições que faltam,
        /// marca quem empilha em quem e gera prefab (com luz/tela acesa) só para as peças novas.
        /// Prefabs e definições já ajustados à mão ficam intactos.
        /// </summary>
        [MenuItem("Tools/GameLike/Office 3D/9 - Adicionar mobília nova (pacote 2)")]
        public static void AddFurniturePack2()
        {
            EnsureFolder(DefFolder);
            AssetDatabase.Refresh();
            _onlyMissingDefs = true;
            try { DefinePack2(); }
            finally { _onlyMissingDefs = false; }
            ApplyStackingFlags();
            AssetDatabase.SaveAssets();
            BuildFurniturePrefabs(true);
            Debug.Log("[Office3D] Pacote 2 de mobília adicionado.");
        }

        /// <summary>Entrada para o modo batch (CI).</summary>
        public static void BatchAddFurniturePack2()
        {
            AddFurniturePack2();
            GameLike.Editor.Office.FurnitureCatalogSync.Sync();
            Debug.Log("GAMELIKE_PACK2_OK");
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/GameLike/Office 3D/2 - Create Garage Layout")]
        public static void CreateGarageLayout()
        {
            EnsureFolder(LayoutFolder);
            string path = $"{LayoutFolder}/GarageLayout.asset";
            var layout = AssetDatabase.LoadAssetAtPath<OfficeLayoutSO>(path);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<OfficeLayoutSO>();
                AssetDatabase.CreateAsset(layout, path);
            }

            layout.EditorSetSize(8, 7);
            layout.EditorPlacements.Clear();

            // GARAGEM HUMILDE — cenário inicial (fase 1): carro + cantinho improvisado.
            // Parede do fundo (y=0): pôster, janela, relógio.
            Place(layout, "poster", 1, 0, 0);
            Place(layout, "window", 3, 0, 0);
            Place(layout, "clock", 6, 0, 0);

            // Carro estacionado (ocupa 3×2) no lado esquerdo.
            Place(layout, "car", 1, 3, 0);

            // Cantinho de trabalho improvisado (direita): tapete, mesinha, PC antigo,
            // cadeira de frente, planta e luminária.
            Place(layout, "rug", 5, 3, 0);
            Place(layout, "old_desk", 5, 3, 0);
            Place(layout, "old_pc", 6, 3, 0);
            Place(layout, "old_chair", 5, 4, 2);
            Place(layout, "floor_lamp", 7, 3, 0);
            Place(layout, "plant", 7, 5, 0);

            // Armário com caixas + caixas soltas (canto fundo-direito).
            Place(layout, "cabinet", 7, 0, 2);
            Place(layout, "cardboard_box", 6, 1, 0);
            Place(layout, "cardboard_box", 7, 1, 0);

            // Luzes de teto (poucas, garagem simples).
            layout.EditorCeilingLights.Clear();
            var ceil = new Color(1f, 0.96f, 0.88f);
            layout.EditorCeilingLights.Add(new CeilingLight { cell = new Vector2Int(2, 3), color = ceil, intensity = 2.2f });
            layout.EditorCeilingLights.Add(new CeilingLight { cell = new Vector2Int(6, 3), color = ceil, intensity = 2.0f });

            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Office3D] Layout da Garage criado em " + path);
        }

        [MenuItem("Tools/GameLike/Office 3D/5 - Bake Garage Into Open Scene")]
        public static void BakeGarageIntoScene()
        {
            var layout = AssetDatabase.LoadAssetAtPath<OfficeLayoutSO>($"{LayoutFolder}/GarageLayout.asset");
            if (layout == null)
            {
                Debug.LogError("[Office3D] GarageLayout nao encontrado. Rode '2 - Create Garage Layout' antes.");
                return;
            }
            var root = GameObject.Find("Garage (baked)");
            if (root == null) root = new GameObject("Garage (baked)");
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            OfficeBuilder.Build(layout, root.transform);
            OfficeLighting.Apply();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[Office3D] Garagem instanciada na cena (edit mode): " + root.transform.childCount + " objetos filhos.");
        }

        [MenuItem("Tools/GameLike/Office 3D/3 - Build Game Scene")]
        public static void BuildGameScene()
        {
            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Câmera ortográfica iso: olha do canto +X,+Z para o canto -X,-Z (paredes ao fundo).
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.93f, 0.89f, 0.80f); // creme suave (estilo tycoon)
                cam.transform.rotation = Quaternion.Euler(30f, 225f, 0f);
                cam.transform.position = -cam.transform.forward * 40f;
            }

            OfficeLighting.Apply();
            if (cam != null) OfficeGlow.ApplyBloom(cam);

            var managers = new GameObject("[GameLike]");
            managers.AddComponent<TimeManager>();
            managers.AddComponent<EconomyManager>();
            managers.AddComponent<ProjectManager>();
            managers.AddComponent<StaffManager>();
            managers.AddComponent<MarketingManager>();
            managers.AddComponent<MarketManager>();
            managers.AddComponent<EventManager>();
            managers.AddComponent<GameController>();

            var office = new GameObject("Office");
            office.AddComponent<OfficeView>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[Office3D] Cena de jogo 3D criada: " + ScenePath);
        }

        private static void MakeDef(string key, string editorName, string fbxName, FurnitureCategory category,
            Vector2Int footprint, bool rotatable, bool isWall, FurnitureEffectType effectType, float magnitude)
        {
            string path = $"{DefFolder}/{key}.asset";
            var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>(path);
            if (def != null && _onlyMissingDefs) return;
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<FurnitureDef3DSO>();
                AssetDatabase.CreateAsset(def, path);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FbxFolder}/{fbxName}/{fbxName}.fbx");
            var so = new SerializedObject(def);
            so.FindProperty("editorName").stringValue = editorName;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("prefab").objectReferenceValue = prefab;
            var fp = so.FindProperty("footprintInCells");
            fp.FindPropertyRelative("x").intValue = footprint.x;
            fp.FindPropertyRelative("y").intValue = footprint.y;
            so.FindProperty("rotatable").boolValue = rotatable;
            so.FindProperty("isWallPiece").boolValue = isWall;
            var eff = so.FindProperty("effect");
            eff.FindPropertyRelative("type").enumValueIndex = (int)effectType;
            eff.FindPropertyRelative("magnitude").floatValue = magnitude;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }

        private static void SetLights(string key, params FurnitureLight[] lights)
        {
            var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>($"{DefFolder}/{key}.asset");
            if (def == null) return;
            def.EditorSetLights(lights);
            EditorUtility.SetDirty(def);
        }

        /// <summary>
        /// Constrói um prefab próprio para cada peça que emite luz: envolve o FBX,
        /// troca as telas/abajures por material glow (asset), e adiciona a(s) luz(es)
        /// como filhas <b>modeladas no objeto</b> (editáveis abrindo o prefab). O def
        /// passa a apontar para esse prefab, então a luz viaja com a peça — não é mais
        /// injetada pela cena. (Requisito de arquitetura: luz pertence ao objeto.)
        /// </summary>
        [MenuItem("Tools/GameLike/Office 3D/4 - Build Furniture Prefabs (luz no objeto)")]
        public static void BuildFurniturePrefabs() => BuildFurniturePrefabs(false);

        /// <summary>Como acima; com <paramref name="onlyMissing"/> pula as peças que já têm prefab.</summary>
        public static void BuildFurniturePrefabs(bool onlyMissing)
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MatFolder);
            // Cores em HDR (bem acima de 1) para o Bloom fazer a própria TELA/cúpula
            // brilhar (o brilho fica na superfície, não numa poça na mesa).
            var screenGlow = MakeUnlit($"{MatFolder}/Screen_Glow.mat", new Color(0.55f, 1.0f, 2.1f));
            var warmGlow = MakeUnlit($"{MatFolder}/Warm_Glow.mat", new Color(2.0f, 1.65f, 1.05f));
            var windowGlow = MakeUnlit($"{MatFolder}/Window_Glow.mat", new Color(2.2f, 2.25f, 2.4f));

            foreach (var (key, fbxName) in LightPieces)
            {
                var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>($"{DefFolder}/{key}.asset");
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{FbxFolder}/{fbxName}/{fbxName}.fbx");
                if (def == null || fbx == null)
                {
                    Debug.LogWarning($"[Office3D] Prefab pulado ({key}): def ou FBX ausente.");
                    continue;
                }
                if (onlyMissing && AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{fbxName}.prefab") != null) continue;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.name = fbxName;

                // Troca os materiais "acesos" por materiais glow (assets) no objeto.
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                        if (n.Contains("pane")) { mats[i] = windowGlow; changed = true; }
                        else if (n.Contains("screen") || n.Contains("marquee")) { mats[i] = screenGlow; changed = true; }
                        else if (n.Contains("shade") || n.Contains("bulb")) { mats[i] = warmGlow; changed = true; }
                    }
                    if (changed) r.sharedMaterials = mats;
                }

                // Luzes modeladas no objeto (filhas), a partir da spec do def.
                foreach (var spec in def.Lights)
                {
                    var lightGo = new GameObject("Light");
                    lightGo.transform.SetParent(inst.transform, false);
                    lightGo.transform.localPosition = spec.localPosition;
                    var light = lightGo.AddComponent<Light>();
                    light.color = spec.color == default ? Color.white : spec.color;
                    light.intensity = spec.intensity <= 0f ? 1.2f : spec.intensity;
                    light.range = spec.range <= 0f ? 3.5f : spec.range;
                    light.renderMode = LightRenderMode.ForcePixel;
                    if (spec.spot)
                    {
                        light.type = LightType.Spot;
                        light.spotAngle = spec.spotAngle <= 0f ? 80f : spec.spotAngle;
                        light.innerSpotAngle = light.spotAngle * 0.6f;
                        var dir = spec.spotDirection == Vector3.zero ? Vector3.down : spec.spotDirection.normalized;
                        lightGo.transform.localRotation = Quaternion.LookRotation(dir, Vector3.forward);
                        light.shadows = LightShadows.Soft;
                        light.shadowStrength = 0.35f;
                    }
                    else
                    {
                        light.type = LightType.Point;
                        light.shadows = LightShadows.None;
                    }
                }

                string prefabPath = $"{PrefabFolder}/{fbxName}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                Object.DestroyImmediate(inst);

                // O def passa a apontar para o prefab (com luz), não o FBX cru.
                var so = new SerializedObject(def);
                so.FindProperty("prefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Office3D] Prefabs de mobília com luz criados em {PrefabFolder} ({LightPieces.Length} peças).");
        }

        private static Material MakeUnlit(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void Place(OfficeLayoutSO layout, string furnitureKey, int x, int y, int rotationSteps)
        {
            var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>($"{DefFolder}/{furnitureKey}.asset");
            if (def == null)
            {
                Debug.LogWarning($"[Office3D] Definição não encontrada: {furnitureKey}. Rode '1 - Create Furniture Defs' antes.");
                return;
            }
            layout.EditorPlacements.Add(new FurniturePlacement
            {
                furniture = def,
                cell = new Vector2Int(x, y),
                rotationSteps = rotationSteps
            });
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes) if (s.path == path) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
