using GameLike.LevelBuilder.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>
    /// Cria e configura a fundação isométrica do Level Builder na cena ativa
    /// (LEVEL_BUILDER.md Fase 0): Sorting Layer único "Iso", Grid "Isometric Z
    /// as Y", andar térreo (<see cref="StoreyRoot"/>), Tilemaps de Floor e Wall
    /// (Renderer Mode Individual, Tile Anchor calibrado) e a câmera ortográfica
    /// com eixo de transparência custom + Pixel Perfect.
    ///
    /// Os valores numéricos (cell size, tile anchor, eixo de transparência) são
    /// defaults a <b>calibrar</b> contra a arte real na cena de teste (§19).
    /// </summary>
    public static class LevelBuilderSceneSetup
    {
        public const string SortingLayerName = "Iso";
        public const string GridName         = "OfficeGrid";
        public const string FloorTilemapName = "Tilemap_Floor";
        public const string WallTilemapName  = "Tilemap_Walls";
        public const string FurnitureRootName = "Furniture";

        // Cell Size: X/Y = diamante do footprint (32x16 @ PPU 32). Z = altura por NÍVEL ao empilhar
        // parede (no "Isometric Z as Y", Z é altura). Com Z=2 cada nível sobe 1 bloco inteiro (32px),
        // então "Altura = N" = N blocos. Z grande também faz o WorldToCell resolver a célula do chão
        // em z=0 (Z pequeno confundia altura com profundidade e desalinhava o pincel).
        private static readonly Vector3 IsoCellSize  = new Vector3(1f, 0.5f, 2f);
        // Anchor (0,0,0): o pivô bottom-center do sprite (vértice da frente da base do cubo) cai
        // em CellToWorld(cell) = vértice inferior da célula → a base do cubo encaixa no diamante
        // da célula, e o ghost (que lê este anchor) fica colado no grid.
        private static readonly Vector3 TileAnchor   = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 SortAxis     = new Vector3(0f, 1f, -0.26f);
        private const int   AssetsPpu      = 32;
        private const int   RefResolutionX = 640;
        private const int   RefResolutionY = 360;

        [MenuItem("Tools/GameLike/Level Builder/Setup Scene", false, 2)]
        public static void SetupScene()
        {
            EnsureSortingLayer(SortingLayerName);
            EnsureLightsTargetSortingLayer(SortingLayerName);
            EnsureTransparencySort();

            var existing = GameObject.Find(GridName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Level Builder — Setup Scene",
                    $"Já existe um '{GridName}' na cena. Substituir por um setup novo (remove o atual)?",
                    "Substituir", "Cancelar");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing);
            }

            // Grid raiz (Isometric Z as Y)
            var gridGO = new GameObject(GridName, typeof(UnityEngine.Grid));
            Undo.RegisterCreatedObjectUndo(gridGO, "Setup Level Builder Scene");
            var grid = gridGO.GetComponent<UnityEngine.Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSize   = IsoCellSize;

            // Andar térreo
            var storeyGO = new GameObject("Storey_0", typeof(StoreyRoot));
            storeyGO.transform.SetParent(gridGO.transform, false);
            storeyGO.GetComponent<StoreyRoot>().SetStorey(0);

            // Floor e Wall compartilham o MESMO Order in Layer: a Sorting Layer única "Iso" usa
            // UM sort unificado governado pelo eixo de transparência (profundidade por posição de
            // mundo, célula a célula). Orders diferentes seriam avaliados ANTES do eixo e forçariam
            // toda parede a desenhar por cima de todo chão — a parede de trás cobriria o chão da
            // frente (LEVEL_BUILDER.md: sort iso unificado entre chão/parede/mobília/personagem).
            CreateTilemap(storeyGO.transform, FloorTilemapName, sortingOrder: 0);
            CreateTilemap(storeyGO.transform, WallTilemapName,  sortingOrder: 0);

            var furniture = new GameObject(FurnitureRootName);
            furniture.transform.SetParent(storeyGO.transform, false);

            ConfigureCamera();

            Selection.activeGameObject = gridGO;
            EditorSceneManager.MarkSceneDirty(gridGO.scene);

            Debug.Log("[LevelBuilderSceneSetup] Fundação criada: Grid iso + Storey_0 + " +
                      "Tilemap_Floor/Walls (Individual, Sorting Layer 'Iso') + câmera (eixo custom). " +
                      "Abra o Level Builder (Tools → GameLike → Level Builder → Window) para pintar.");
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);

            var tilemap = go.GetComponent<Tilemap>();
            tilemap.tileAnchor = TileAnchor;

            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.mode             = TilemapRenderer.Mode.Individual;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder     = sortingOrder;
            // Sprites de cubo (altura) precisam de Sort Order "Top Right" para oclusão correta
            // (front-most sobrepõe o fundo). Bounds de culling manual evita que blocos altos
            // sumam quando o chunk da célula sai da tela. Valores calibrados na cena de teste.
            renderer.sortOrder                = TilemapRenderer.SortOrder.TopRight;
            renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
            renderer.chunkCullingBounds       = new Vector3(0.5f, 0f, 0f);
            return tilemap;
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[LevelBuilderSceneSetup] Main Camera não encontrada — configure o eixo de transparência manualmente.");
                return;
            }

            Undo.RecordObject(cam, "Configure Level Builder Camera");
            cam.orthographic          = true;
            cam.transparencySortMode  = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis  = SortAxis;
            EditorUtility.SetDirty(cam);

            EnsureTransparencySort();
            ConfigurePixelPerfect(cam);
        }

        /// <summary>
        /// Aplica o eixo de transparência custom isométrico. No URP 2D quem manda é o asset do
        /// <b>2D Renderer</b> (Renderer2DData) — NÃO a câmera nem o GraphicsSettings. Por isso
        /// configuramos os três, mas o que realmente corrige a oclusão iso (Scene + Game) é o
        /// Renderer2DData. Sem isto, cubos da frente não ocluem os de trás.
        /// </summary>
        public static void EnsureTransparencySort()
        {
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = SortAxis;

            int patched = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || asset.GetType().Name != "Renderer2DData") continue;

                var so       = new SerializedObject(asset);
                var modeProp = so.FindProperty("m_TransparencySortMode");
                var axisProp = so.FindProperty("m_TransparencySortAxis");
                if (modeProp == null || axisProp == null) continue;

                modeProp.enumValueIndex = (int)TransparencySortMode.CustomAxis;
                axisProp.vector3Value   = SortAxis;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                patched++;
            }
            AssetDatabase.SaveAssets();

            if (patched > 0)
                Debug.Log($"[LevelBuilderSceneSetup] {patched} 2D Renderer(s) ajustado(s): Transparency Sort = Custom Axis {SortAxis}. Oclusão iso corrigida (Scene + Game).");
            else
                Debug.LogWarning("[LevelBuilderSceneSetup] Nenhum Renderer2DData encontrado — ajuste manual: selecione o asset do 2D Renderer em Assets/Settings, Transparency Sort Mode = Custom Axis, Axis = (0, 1, -0.26).");
        }

        /// <summary>
        /// Adiciona/configura a Pixel Perfect Camera por reflexão para evitar
        /// acoplar o asmdef do editor à assembly do URP. Best-effort: se a API
        /// divergir entre versões, loga e segue (o eixo de transparência, que é
        /// o crítico para sorting, já foi configurado acima).
        /// </summary>
        private static void ConfigurePixelPerfect(Camera cam)
        {
            var ppcType = FindTypeInLoadedAssemblies("UnityEngine.Rendering.Universal.PixelPerfectCamera");
            if (ppcType == null)
            {
                Debug.Log("[LevelBuilderSceneSetup] PixelPerfectCamera (URP) não encontrada — pulando (não bloqueia a Fase 0).");
                return;
            }

            var ppc = cam.GetComponent(ppcType) ?? Undo.AddComponent(cam.gameObject, ppcType);
            var so  = new SerializedObject(ppc);
            TrySetInt(so, "m_AssetsPPU", AssetsPpu);
            TrySetInt(so, "m_RefResolutionX", RefResolutionX);
            TrySetInt(so, "m_RefResolutionY", RefResolutionY);
            so.ApplyModifiedProperties();
        }

        private static void TrySetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.intValue = value;
        }

        /// <summary>
        /// Reaplica TODAS as configurações corretas a uma cena já montada, sem recriar o grid
        /// (não perde os tiles): sorting layer, luz 2D, eixo de transparência (oclusão iso) e os
        /// Tilemaps Floor/Wall (anchor, Individual, Top Right, culling) + zera a posição da cadeia.
        /// Útil pra reparar uma cena que sofreu ajuste manual/arrasto acidental.
        /// </summary>
        [MenuItem("Tools/GameLike/Level Builder/Fix Scene Settings", false, 20)]
        public static void FixSceneSettings()
        {
            EnsureSortingLayer(SortingLayerName);
            EnsureLightsTargetSortingLayer(SortingLayerName);
            EnsureTransparencySort();

            var grid = GameObject.Find(GridName);
            if (grid == null)
            {
                Debug.LogWarning($"[LevelBuilderSceneSetup] '{GridName}' não encontrado — rode Setup Scene primeiro.");
                return;
            }

            // Reaplica o layout/cell size do Grid (inclui o Z de empilhamento de parede).
            var gridComp = grid.GetComponent<UnityEngine.Grid>();
            if (gridComp != null)
            {
                Undo.RecordObject(gridComp, "Fix Scene Settings");
                gridComp.cellLayout = GridLayout.CellLayout.IsometricZAsY;
                gridComp.cellSize   = IsoCellSize;
            }

            // Zera a cadeia de transform (Grid e andares) — desfaz arrastos acidentais que
            // desalinhariam tiles do grid de referência.
            Undo.RecordObject(grid.transform, "Fix Scene Settings");
            grid.transform.position = Vector3.zero;
            foreach (var storey in grid.GetComponentsInChildren<StoreyRoot>(true))
            {
                Undo.RecordObject(storey.transform, "Fix Scene Settings");
                storey.transform.localPosition = Vector3.zero;
                storey.ApplyOffset();
            }

            int fixedCount = 0;
            foreach (var name in new[] { FloorTilemapName, WallTilemapName })
            {
                var tf = FindDeep(grid.transform, name);
                if (tf == null) continue;
                var tilemap  = tf.GetComponent<Tilemap>();
                var renderer = tf.GetComponent<TilemapRenderer>();
                if (tilemap == null || renderer == null) continue;

                Undo.RecordObject(tf, "Fix Scene Settings");
                Undo.RecordObject(tilemap, "Fix Scene Settings");
                Undo.RecordObject(renderer, "Fix Scene Settings");

                tf.localPosition = Vector3.zero;            // desfaz movimentos acidentais
                tilemap.tileAnchor = TileAnchor;
                renderer.mode                     = TilemapRenderer.Mode.Individual;
                renderer.sortOrder                = TilemapRenderer.SortOrder.TopRight;
                renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
                renderer.chunkCullingBounds       = new Vector3(0.5f, 0f, 0f);
                renderer.sortingLayerName         = SortingLayerName;
                renderer.sortingOrder             = 0; // Floor e Wall no mesmo order → sort iso unificado pelo eixo

                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(renderer);
                fixedCount++;
            }

            Debug.Log($"[LevelBuilderSceneSetup] Cena reconfigurada: {fixedCount} tilemap(s) " +
                      $"(anchor {TileAnchor}, Individual, Top Right), luz/transparência/posição OK.");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Garante que a Sorting Layer existe, criando-a no TagManager via
        /// SerializedObject (sem editar o YAML à mão). No-op se já existir.
        /// </summary>
        public static void EnsureSortingLayer(string layerName)
        {
            foreach (var l in SortingLayer.layers)
                if (l.name == layerName) return;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[LevelBuilderSceneSetup] TagManager.asset não encontrado — crie a Sorting Layer 'Iso' manualmente.");
                return;
            }

            var so     = new SerializedObject(assets[0]);
            var layers = so.FindProperty("m_SortingLayers");
            int idx    = layers.arraySize;
            layers.InsertArrayElementAtIndex(idx);

            var entry = layers.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("name").stringValue   = layerName;
            entry.FindPropertyRelative("uniqueID").intValue  = (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF);
            entry.FindPropertyRelative("locked").boolValue   = false;
            so.ApplyModifiedProperties();

            Debug.Log($"[LevelBuilderSceneSetup] Sorting Layer '{layerName}' criada.");
        }

        /// <summary>
        /// Garante que todas as luzes 2D (URP <c>Light2D</c>) incluam a Sorting Layer dada
        /// nos seus alvos. Sem isso, sprites com material <i>Lit</i> na layer 'Iso' ficam
        /// pretos (não recebem luz). Feito por reflexão para não acoplar o asmdef ao URP.
        /// </summary>
        public static void EnsureLightsTargetSortingLayer(string layerName)
        {
            var light2dType = FindTypeInLoadedAssemblies("UnityEngine.Rendering.Universal.Light2D");
            if (light2dType == null)
            {
                Debug.LogWarning("[LevelBuilderSceneSetup] Tipo Light2D (URP) não encontrado — ajuste 'Target Sorting Layers' das luzes para Everything manualmente.");
                return;
            }

            var lights = Object.FindObjectsByType(light2dType, FindObjectsSortMode.None);
            int patched = 0;

            foreach (var light in lights)
            {
                var so   = new SerializedObject(light);
                var prop = FindSortingLayersProperty(so);
                if (prop == null) continue;

                // Faz a luz cobrir TODAS as sorting layers (equivalente a "Everything"),
                // garantindo que a layer 'Iso' (e futuras) recebam luz.
                prop.ClearArray();
                var all = SortingLayer.layers;
                for (int i = 0; i < all.Length; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).intValue = all[i].id;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(light);
                patched++;
            }

            if (patched > 0)
                Debug.Log($"[LevelBuilderSceneSetup] {patched} luz(es) 2D agora iluminam todas as Sorting Layers (inclui '{layerName}').");
            else
                Debug.LogWarning($"[LevelBuilderSceneSetup] Nenhuma luz 2D ajustada — verifique manualmente o 'Target Sorting Layers' (deixe em Everything).");
        }

        /// <summary>Busca um tipo pelo nome completo em todos os assemblies carregados (robusto a nome de assembly).</summary>
        private static System.Type FindTypeInLoadedAssemblies(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>Localiza a propriedade-array de target sorting layers da Light2D, robusto a renomeações.</summary>
        private static SerializedProperty FindSortingLayersProperty(SerializedObject so)
        {
            var it = so.GetIterator();
            while (it.Next(true))
                if (it.isArray && it.name.IndexOf("SortingLayers", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return it.Copy();
            return null;
        }
    }
}
