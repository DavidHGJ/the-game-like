using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Constrói, em runtime, o escritório 3D de uma fase a partir de um
    /// <see cref="OfficeLayoutSO"/>: chão, as duas paredes do canto ao fundo
    /// (do lado oposto à câmera iso — evitando a oclusão que escondia os móveis)
    /// e a mobília posicionada no grid, sem sobreposição. É data-driven: trocar o
    /// layout troca o escritório, sem cena bakeada.
    ///
    /// Usado tanto pelo gameplay quanto pelo criador de fases (preview ao vivo).
    /// </summary>
    public static class OfficeBuilder
    {
        private static readonly Dictionary<Color, Material> MaterialCache = new Dictionary<Color, Material>();

        /// <summary>Reconstrói o escritório sob <paramref name="parent"/> (limpa os filhos antes).</summary>
        public static void Build(OfficeLayoutSO layout, Transform parent)
        {
            if (layout == null || parent == null) return;
            ClearChildren(parent);
            BuildShell(layout, parent);

            var furnitureRoot = new GameObject(FurnitureRootName);
            furnitureRoot.transform.SetParent(parent, false);
            foreach (var p in layout.Placements)
                CreatePiece(p, layout, furnitureRoot.transform);

            BuildCeilingLights(layout, parent);
            BuildSeats(layout, parent);
        }

        /// <summary>Nome do filho que agrupa a mobília montada.</summary>
        public const string FurnitureRootName = "Furniture";
        /// <summary>Nome do filho que agrupa as luzes de teto montadas.</summary>
        public const string CeilingLightsRootName = "CeilingLights";
        /// <summary>Nome do filho que agrupa os assentos montados.</summary>
        public const string SeatsRootName = "Seats";
        /// <summary>Nomes das peças da casca da sala.</summary>
        public const string FloorName = "Floor", BackWallName = "Wall_Back", LeftWallName = "Wall_Left";

        /// <summary>(Re)constrói só a casca da sala — chão e as duas paredes do fundo.</summary>
        public static void BuildShell(OfficeLayoutSO layout, Transform parent)
        {
            if (layout == null || parent == null) return;
            RemoveChild(parent, FloorName);
            RemoveChild(parent, BackWallName);
            RemoveChild(parent, LeftWallName);
            foreach (string trim in TrimNames) RemoveChild(parent, trim);

            float cs = layout.CellSize;
            float width = layout.Cols * cs;   // X
            float depth = layout.Rows * cs;   // Z
            float wt = layout.WallThickness;
            float wh = layout.WallHeight;

            var floorMat = SurfaceMaterial(layout.FloorColor, layout.FloorStyle, width, depth, FloorSmoothness(layout.FloorStyle));
            var backMat = SurfaceMaterial(layout.WallColor, layout.WallStyle, width + wt, wh, 0.08f);
            var leftMat = SurfaceMaterial(layout.SideWallColor, layout.SideWallStyle, depth + wt, wh, 0.08f);

            // Chão: cubo fino, topo em y=0.
            var floor = Primitive(PrimitiveType.Cube, FloorName, parent, floorMat);
            floor.transform.localScale = new Vector3(width, 0.1f, depth);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            // Parede ao fundo (borda -Z), estende no eixo X.
            var backWall = Primitive(PrimitiveType.Cube, BackWallName, parent, backMat);
            backWall.transform.localScale = new Vector3(width + wt, wh, wt);
            backWall.transform.localPosition = new Vector3(0f, wh * 0.5f, -depth * 0.5f - wt * 0.5f);

            // Parede à esquerda (borda -X), estende no eixo Z.
            var leftWall = Primitive(PrimitiveType.Cube, LeftWallName, parent, leftMat);
            leftWall.transform.localScale = new Vector3(wt, wh, depth + wt);
            leftWall.transform.localPosition = new Vector3(-width * 0.5f - wt * 0.5f, wh * 0.5f, 0f);

            // Acabamento: capa clara no topo das paredes e rodapé — o detalhe que dá cara de maquete.
            var trimMat = ColorMaterial(TrimColor);
            const float cap = 0.06f, capOver = 0.03f, baseH = 0.11f, baseT = 0.03f;
            Trim(BackWallName + "_Cap", parent, trimMat, new Vector3(width + wt + capOver * 2f, cap, wt + capOver * 2f),
                new Vector3(0f, wh + cap * 0.5f, -depth * 0.5f - wt * 0.5f));
            Trim(LeftWallName + "_Cap", parent, trimMat, new Vector3(wt + capOver * 2f, cap, depth + wt + capOver * 2f),
                new Vector3(-width * 0.5f - wt * 0.5f, wh + cap * 0.5f, 0f));
            Trim(BackWallName + "_Base", parent, trimMat, new Vector3(width, baseH, baseT),
                new Vector3(0f, baseH * 0.5f, -depth * 0.5f + baseT * 0.5f));
            Trim(LeftWallName + "_Base", parent, trimMat, new Vector3(baseT, baseH, depth),
                new Vector3(-width * 0.5f + baseT * 0.5f, baseH * 0.5f, 0f));
        }

        private static readonly Color TrimColor = new Color(0.93f, 0.92f, 0.89f);
        private static readonly string[] TrimNames =
        {
            BackWallName + "_Cap", LeftWallName + "_Cap", BackWallName + "_Base", LeftWallName + "_Base"
        };

        private static void Trim(string name, Transform parent, Material material, Vector3 scale, Vector3 position)
        {
            var go = Primitive(PrimitiveType.Cube, name, parent, material);
            go.transform.localScale = scale;
            go.transform.localPosition = position;
        }

        /// <summary>Quanto o piso brilha/reflete o ambiente, conforme o acabamento.</summary>
        private static float FloorSmoothness(SurfaceStyle style)
        {
            switch (style)
            {
                case SurfaceStyle.Tiles:
                case SurfaceStyle.Checker: return 0.62f;
                case SurfaceStyle.WoodPlanks:
                case SurfaceStyle.WoodPanel: return 0.48f;
                case SurfaceStyle.Concrete: return 0.30f;
                case SurfaceStyle.Carpet:
                case SurfaceStyle.Brick: return 0.04f;
                default: return 0.34f;
            }
        }

        /// <summary>(Re)constrói as luzes de teto (pontos de luz pendurados à altura da parede).</summary>
        public static Transform BuildCeilingLights(OfficeLayoutSO layout, Transform parent)
        {
            RemoveChild(parent, CeilingLightsRootName);
            var lightsRoot = new GameObject(CeilingLightsRootName);
            lightsRoot.transform.SetParent(parent, false);
            foreach (var cl in layout.CeilingLights)
                PlaceCeilingLight(cl, layout, lightsRoot.transform);
            return lightsRoot.transform;
        }

        /// <summary>
        /// (Re)constrói os assentos: um Transform por assento, já na posição/rotação em que o
        /// boneco senta.
        /// </summary>
        public static Transform BuildSeats(OfficeLayoutSO layout, Transform parent)
        {
            RemoveChild(parent, SeatsRootName);
            var seatsRoot = new GameObject(SeatsRootName);
            seatsRoot.transform.SetParent(parent, false);
            for (int i = 0; i < layout.Seats.Count; i++)
            {
                SeatPose(layout, layout.Seats[i], out Vector3 seatPos, out Quaternion seatRot);
                var seat = new GameObject("Seat_" + i);
                seat.transform.SetParent(seatsRoot.transform, false);
                seat.transform.localPosition = seatPos;
                seat.transform.localRotation = seatRot;
            }
            return seatsRoot.transform;
        }

        /// <summary>
        /// Posição e rotação (locais ao escritório) em que um boneco senta num assento: o centro
        /// da cadeira que ocupa a célula (com a rotação dela) mais o giro/ajuste do assento.
        /// </summary>
        public static void SeatPose(OfficeLayoutSO layout, SeatPoint seat, out Vector3 position, out Quaternion rotation)
        {
            float cs = layout.CellSize;
            position = OfficeGrid.CellCenter(seat.cell.x, seat.cell.y, layout.Cols, layout.Rows, cs);
            int steps = seat.rotationSteps;
            if (TryFindChairAt(layout, seat.cell, out FurniturePlacement chair))
            {
                PiecePose(chair, layout, out position, out _);
                steps += chair.rotationSteps;
            }
            rotation = Quaternion.Euler(0f, steps * 90f, 0f);
            position += rotation * seat.offset;
        }

        /// <summary>
        /// Altura do assento (onde o quadril do boneco apoia): a declarada na cadeira ou, sem
        /// cadeira/sem valor, meio metro.
        /// </summary>
        public static float SeatHeight(OfficeLayoutSO layout, SeatPoint seat)
        {
            const float fallback = 0.5f;
            if (!TryFindChairAt(layout, seat.cell, out FurniturePlacement chair)) return fallback;
            return chair.furniture.SurfaceHeight > 0f ? chair.furniture.SurfaceHeight : fallback;
        }

        /// <summary>Procura uma cadeira (categoria Chair) cujo footprint cobre a célula dada.</summary>
        public static bool TryFindChairAt(OfficeLayoutSO layout, Vector2Int cell, out FurniturePlacement chair)
        {
            chair = default;
            if (layout == null) return false;
            var placements = layout.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (p.furniture == null || p.furniture.Category != FurnitureCategory.Chair) continue;
                var fp = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
                if (cell.x < p.cell.x || cell.y < p.cell.y || cell.x >= p.cell.x + fp.x || cell.y >= p.cell.y + fp.y) continue;
                chair = p;
                return true;
            }
            return false;
        }

        private static void PlaceCeilingLight(CeilingLight cl, OfficeLayoutSO layout, Transform parent)
        {
            float cs = layout.CellSize;
            Vector3 pos = OfficeGrid.CellCenter(cl.cell.x, cl.cell.y, layout.Cols, layout.Rows, cs);
            float drop = cl.dropFromCeiling > 0f ? cl.dropFromCeiling : 0.15f;
            pos.y = layout.WallHeight - drop;

            var go = new GameObject("CeilingLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = cl.color == default ? new Color(1f, 0.95f, 0.85f) : cl.color;
            light.intensity = cl.intensity <= 0f ? 2.2f : cl.intensity;
            light.range = cl.range <= 0f ? Mathf.Max(5f, cs * 4f) : cl.range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        /// <summary>
        /// Posição e rotação (locais ao escritório) de uma peça: centro do footprint girado no
        /// chão, ou — para peça de parede — encostada na parede do fundo mais próxima, à
        /// meia-altura. É a única conta de pose de mobília: jogo e Map Builder usam esta.
        /// </summary>
        public static void PiecePose(FurniturePlacement p, OfficeLayoutSO layout, out Vector3 position, out Quaternion rotation)
        {
            float cs = layout.CellSize;
            var footprint = OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps);
            position = OfficeGrid.FootprintCenter(p.cell.x, p.cell.y, footprint.x, footprint.y,
                layout.Cols, layout.Rows, cs);
            rotation = Quaternion.Euler(0f, p.rotationSteps * 90f, 0f);
            if (!p.furniture.IsWallPiece)
            {
                position += AlignShift(p, footprint, cs);
                if (p.onTop) position.y = SupportHeight(p, layout, footprint);
                return;
            }

            float width = layout.Cols * cs;
            float depth = layout.Rows * cs;
            if (p.cell.y <= p.cell.x) // mais perto da parede -Z (fundo)
            {
                position = new Vector3(position.x, layout.WallHeight * 0.45f, -depth * 0.5f + 0.06f);
                rotation = Quaternion.identity;
            }
            else // mais perto da parede -X (esquerda)
            {
                position = new Vector3(-width * 0.5f + 0.06f, layout.WallHeight * 0.45f, position.z);
                rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        /// <summary>
        /// Deslocamento que encosta a peça na borda/canto do footprint pedido em <c>align</c>:
        /// o volume real da peça toca a borda da célula, sem passar dela. Peça maior que a
        /// célula não se mexe; sem malha medida, anda um quarto de célula.
        /// </summary>
        private static Vector3 AlignShift(FurniturePlacement p, Vector2Int footprint, float cs)
        {
            if (p.align == Vector2Int.zero) return Vector3.zero;
            float halfW = footprint.x * cs * 0.5f, halfD = footprint.y * cs * 0.5f;
            if (!FurnitureBounds.TryGetRotatedFootprint(p.furniture, p.rotationSteps, out Vector2 min, out Vector2 max))
                return new Vector3(p.align.x * halfW * 0.5f, 0f, p.align.y * halfD * 0.5f);
            float x = p.align.x > 0 ? Mathf.Max(0f, halfW - max.x) : p.align.x < 0 ? Mathf.Min(0f, -halfW - min.x) : 0f;
            float z = p.align.y > 0 ? Mathf.Max(0f, halfD - max.y) : p.align.y < 0 ? Mathf.Min(0f, -halfD - min.y) : 0f;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Altura do tampo do móvel que sustenta a peça (0 se não houver mais suporte).</summary>
        private static float SupportHeight(FurniturePlacement p, OfficeLayoutSO layout, Vector2Int footprint)
        {
            int support = OfficeLayoutRules.SupportAt(layout.Placements, p.cell, footprint);
            return support >= 0 ? FurnitureBounds.SurfaceHeight(layout.Placements[support].furniture) : 0f;
        }

        /// <summary>
        /// Instancia uma peça sob <paramref name="parent"/> já na pose final, com materiais
        /// polidos e luzes embutidas. Devolve null se a peça não tem o que mostrar.
        /// </summary>
        public static GameObject CreatePiece(FurniturePlacement p, OfficeLayoutSO layout, Transform parent)
        {
            if (p.furniture == null) return null;
            var lights = p.furniture.Lights;
            bool hasLights = lights != null && lights.Length > 0;
            if (p.furniture.Prefab == null && !hasLights) return null;

            PiecePose(p, layout, out Vector3 pos, out Quaternion rot);

            GameObject inst;
            if (p.furniture.Prefab != null)
            {
                inst = Object.Instantiate(p.furniture.Prefab, parent);
                inst.name = p.furniture.EditorName;
            }
            else
            {
                inst = new GameObject(p.furniture.EditorName);
                inst.transform.SetParent(parent, false);
            }
            inst.transform.localPosition = pos;
            inst.transform.localRotation = rot;
            if (p.furniture.Prefab != null) PolishMaterials(inst);
            // Se o prefab já traz luz modelada nele, respeita-a; só injeta via dados
            // quando a peça não tem nenhuma luz própria (fallback).
            if (hasLights && inst.GetComponentInChildren<Light>() == null)
                AttachLights(inst.transform, lights);
            return inst;
        }

        private static void RemoveChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null) return;
            if (Application.isPlaying) { child.gameObject.SetActive(false); child.SetParent(null, false); Object.Destroy(child.gameObject); }
            else Object.DestroyImmediate(child.gameObject);
        }

        /// <summary>
        /// Cria point lights filhas (giram junto com a peça) a partir das fontes
        /// declaradas na definição. Sem sombra por padrão (muitas sombras de ponto
        /// matam a performance e o visual diorama já tem a direcional principal).
        /// </summary>
        private static void AttachLights(Transform parent, FurnitureLight[] lights)
        {
            foreach (var spec in lights)
            {
                var go = new GameObject("EmbeddedLight");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = spec.localPosition;
                var light = go.AddComponent<Light>();
                light.color = spec.color == default ? Color.white : spec.color;
                light.intensity = spec.intensity <= 0f ? 1.2f : spec.intensity;
                light.range = spec.range <= 0f ? 3.5f : spec.range;
                light.renderMode = LightRenderMode.ForcePixel;

                if (spec.spot)
                {
                    // Holofote: joga a luz principalmente numa direção (p/ baixo por
                    // padrão), como uma luminária aberta embaixo.
                    light.type = LightType.Spot;
                    light.spotAngle = spec.spotAngle <= 0f ? 80f : spec.spotAngle;
                    light.innerSpotAngle = light.spotAngle * 0.6f;
                    var dir = spec.spotDirection == Vector3.zero ? Vector3.down : spec.spotDirection.normalized;
                    go.transform.localRotation = Quaternion.LookRotation(dir, Vector3.forward);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.35f;
                }
                else
                {
                    light.type = LightType.Point;
                    light.shadows = LightShadows.None;
                }
            }
        }

        private static readonly Dictionary<Material, Material> PolishCache = new Dictionary<Material, Material>();

        /// <summary>
        /// Substitui os materiais importados do FBX por variantes com um leve
        /// brilho (menos "opaco/chapado"), preservando a cor base. Low-poly fica
        /// melhor com uma reflexão especular sutil sob a luz direcional.
        /// </summary>
        private static void PolishMaterials(GameObject inst)
        {
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                    dst[i] = Polished(src[i]);
                r.sharedMaterials = dst;
            }
        }

        private static Material Polished(Material src)
        {
            if (src == null) return null;
            // Material já "aceso" (Unlit) vindo do prefab: respeita, não re-swapa.
            if (src.shader != null && src.shader.name.Contains("Unlit")) return src;
            if (PolishCache.TryGetValue(src, out var cached) && cached != null) return cached;

            // Superfícies "acesas" (tela, abajur, lâmpada, LED): material Unlit plano
            // na cor desejada — fica sempre brilhante/"ligado" sem depender de
            // emissão HDR (que renderiza com cor errada na Lit importada do FBX). A
            // luz real continua vindo da point light embutida na peça.
            string sn = src.name != null ? src.name.ToLowerInvariant() : "";
            bool isScreen = sn.Contains("screen");
            bool isWarm = sn.Contains("shade") || sn.Contains("bulb");
            bool isLed = sn.Contains("led") || sn.Contains("marquee");
            if (isScreen || isWarm || isLed)
            {
                var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                var glow = new Material(unlitShader != null ? unlitShader : src.shader);
                Color baseCol = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                              : (src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white);
                Color c = isScreen ? new Color(0.52f, 0.78f, 1.0f)
                        : isWarm ? new Color(1.0f, 0.93f, 0.74f)
                        : baseCol;
                if (glow.HasProperty("_BaseColor")) glow.SetColor("_BaseColor", c);
                if (glow.HasProperty("_Color")) glow.SetColor("_Color", c);
                PolishCache[src] = glow;
                return glow;
            }

            var m = new Material(src);
            Finish(sn, out float smoothness, out float metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            // Reforça a leitura de relevo mantendo o especular ambiente do URP.
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 1f);
            if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", 1f);
            PolishCache[src] = m;
            return m;
        }

        /// <summary>
        /// Acabamento físico pelo nome do material do modelo: metal reflete o ambiente, vidro e
        /// plástico brilham, madeira tem brilho suave, tecido/papelão/planta são foscos. É o que
        /// faz cada material reagir diferente à luz em vez de tudo parecer o mesmo plástico.
        /// </summary>
        private static void Finish(string materialName, out float smoothness, out float metallic)
        {
            metallic = 0f;
            if (Has(materialName, "metal", "rim", "steel", "chrome", "trophy", "crank"))
            {
                smoothness = 0.68f;
                metallic = 0.85f;
            }
            else if (Has(materialName, "fabric", "chair", "sofa", "rug", "leaf", "cactus", "cardboard", "carton", "paper", "book", "partition", "note")) smoothness = 0.10f;
            else if (Has(materialName, "glass", "water", "bottle", "mug", "cup", "ceramic", "pot")) smoothness = 0.72f;
            else if (Has(materialName, "wood", "bench", "cork", "frame", "shelf", "table", "stand")) smoothness = 0.36f;
            else if (Has(materialName, "plastic", "dark", "console", "printer", "cooler", "fridge", "vending", "car", "bike", "ac_", "beige")) smoothness = 0.55f;
            else smoothness = 0.30f;
        }

        private static bool Has(string text, params string[] words)
        {
            foreach (string word in words)
                if (text.Contains(word)) return true;
            return false;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        /// <summary>
        /// Material de chão/parede: cor lisa ou, se houver estilo, a textura gerada na cor,
        /// repetida conforme o tamanho real da superfície (em metros).
        /// </summary>
        private static Material SurfaceMaterial(Color color, SurfaceStyle style, float widthMeters, float heightMeters, float smoothness)
        {
            var texture = SurfaceTextures.Get(style, color);
            if (texture == null) return ColorMaterial(color, smoothness);

            var tiling = new Vector2(widthMeters / SurfaceTextures.MetersPerTile, heightMeters / SurfaceTextures.MetersPerTile);
            string key = (int)style + "|" + ColorUtility.ToHtmlStringRGB(color) + "|" + tiling.x.ToString("0.00") + "x" + tiling.y.ToString("0.00") + "|" + smoothness.ToString("0.00");
            if (SurfaceCache.TryGetValue(key, out var cached) && cached != null && cached.GetTexture("_BaseMap") == texture) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", texture); mat.SetTextureScale("_BaseMap", tiling); }
            if (mat.HasProperty("_MainTex")) { mat.SetTexture("_MainTex", texture); mat.SetTextureScale("_MainTex", tiling); }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            SurfaceCache[key] = mat;
            return mat;
        }

        private static readonly Dictionary<string, Material> SurfaceCache = new Dictionary<string, Material>();

        private static Material ColorMaterial(Color color, float smoothness = 0.08f)
        {
            // A chave junta cor e brilho: o canal alfa (sempre 1 nas cores de sala) leva o brilho.
            var cacheKey = new Color(color.r, color.g, color.b, smoothness);
            if (MaterialCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            MaterialCache[cacheKey] = mat;
            return mat;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                // Em play o Destroy é adiado: solta o filho antes para ele não ser achado de novo neste frame.
                if (Application.isPlaying) { child.SetActive(false); child.transform.SetParent(null, false); Object.Destroy(child); }
                else Object.DestroyImmediate(child);
            }
        }
    }
}
