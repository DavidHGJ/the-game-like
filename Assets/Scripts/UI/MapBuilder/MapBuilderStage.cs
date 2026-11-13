using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEngine;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Suaviza a pose de uma peça no Map Builder: ela desliza e GIRA de verdade até o alvo,
    /// e dá um "pop" de escala ao ser colocada.
    /// </summary>
    internal sealed class MapPieceMotion : MonoBehaviour
    {
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private float _pop = 1f;
        private Vector3 _baseScale = Vector3.one;

        private void Awake() => _baseScale = transform.localScale;

        public void Snap(Vector3 position, Quaternion rotation)
        {
            _position = position; _rotation = rotation;
            transform.localPosition = position; transform.localRotation = rotation;
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            _position = position; _rotation = rotation;
        }

        public void Pop() => _pop = 0f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-22f * dt);
            transform.localPosition = Vector3.Lerp(transform.localPosition, _position, k);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _rotation, 1f - Mathf.Exp(-16f * dt));
            if (_pop >= 1f) return;
            _pop = Mathf.Min(1f, _pop + dt * 6f);
            float overshoot = 1f + 0.12f * Mathf.Sin(_pop * Mathf.PI);
            transform.localScale = _baseScale * (Mathf.Lerp(0.7f, 1f, _pop) * overshoot);
        }
    }

    /// <summary>Moldura no chão (4 barras) que marca um footprint: seleção, fantasma ou célula.</summary>
    internal sealed class CellFrame
    {
        private const float Thickness = 0.07f, Height = 0.03f;

        private readonly GameObject _root;
        private readonly Transform[] _bars = new Transform[4];
        private readonly Material _material;

        public CellFrame(Transform parent, string name)
        {
            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);
            _material = MapBuilderStage.UnlitMaterial(Color.white);
            for (int i = 0; i < 4; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(bar.GetComponent<Collider>());
                bar.name = "Bar";
                bar.transform.SetParent(_root.transform, false);
                var renderer = bar.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _bars[i] = bar.transform;
            }
            _root.SetActive(false);
        }

        public void Hide() => _root.SetActive(false);

        public void Show(OfficeLayoutSO layout, Vector2Int cell, Vector2Int footprint, Color color)
        {
            float cs = layout.CellSize;
            Vector3 center = OfficeGrid.FootprintCenter(cell.x, cell.y, footprint.x, footprint.y, layout.Cols, layout.Rows, cs);
            float w = footprint.x * cs, d = footprint.y * cs;
            center.y = Height;
            _root.transform.localPosition = center;
            _bars[0].localPosition = new Vector3(0f, 0f, d * 0.5f); _bars[0].localScale = new Vector3(w + Thickness, Height, Thickness);
            _bars[1].localPosition = new Vector3(0f, 0f, -d * 0.5f); _bars[1].localScale = new Vector3(w + Thickness, Height, Thickness);
            _bars[2].localPosition = new Vector3(w * 0.5f, 0f, 0f); _bars[2].localScale = new Vector3(Thickness, Height, d + Thickness);
            _bars[3].localPosition = new Vector3(-w * 0.5f, 0f, 0f); _bars[3].localScale = new Vector3(Thickness, Height, d + Thickness);
            MapBuilderStage.SetColor(_material, color);
            _root.SetActive(true);
        }
    }

    /// <summary>
    /// O palco 3D do Map Builder: desenha o cenário da sessão com o MESMO construtor do jogo
    /// (<see cref="OfficeBuilder"/>) — então o que o dev vê é o que a partida mostra — e cuida
    /// do que é só da ferramenta: grade, molduras, fantasma da peça armada, marcadores de luz,
    /// etiquetas de assento e a escolha da peça sob o ponteiro.
    /// </summary>
    internal sealed class MapBuilderStage
    {
        public static readonly Color ValidColor = new Color(0.25f, 0.85f, 0.45f);
        public static readonly Color InvalidColor = new Color(0.92f, 0.28f, 0.24f);
        public static readonly Color SelectedColor = Color.white;
        public static readonly Color HoverColor = new Color(1f, 0.82f, 0.30f);

        private readonly MapBuilderSession _session;
        private readonly Transform _office, _tools;
        private readonly List<GameObject> _pieces = new List<GameObject>();
        private readonly CellFrame _selection, _cursor;
        private Transform _furnitureRoot, _grid, _lightMarkers, _seatLabels;
        private GameObject _ghost;
        private FurnitureDef3DSO _ghostDef;
        private bool _guides = true, _people = true;

        private OfficeLayoutSO Layout => _session.Layout;

        public bool Guides => _guides;
        public bool People => _people;

        private readonly Camera _camera;

        public MapBuilderStage(MapBuilderSession session, Camera camera)
        {
            _session = session;
            _camera = camera;
            _office = new GameObject("Office").transform;
            _tools = new GameObject("BuilderGuides").transform;
            _selection = new CellFrame(_tools, "SelectionFrame");
            _cursor = new CellFrame(_tools, "CursorFrame");
            RebuildAll();
        }

        // ------------------------------------------------------------------ montagem

        /// <summary>Remonta tudo a partir da sessão (abrir cenário, desfazer, redimensionar, remover).</summary>
        public void RebuildAll()
        {
            DropGhost();
            _pieces.Clear();
            for (int i = _office.childCount - 1; i >= 0; i--)
            {
                var child = _office.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }

            OfficeBuilder.BuildShell(Layout, _office);
            _furnitureRoot = new GameObject(OfficeBuilder.FurnitureRootName).transform;
            _furnitureRoot.SetParent(_office, false);
            foreach (var placement in Layout.EditorPlacements) _pieces.Add(SpawnPiece(placement));

            RefreshDerived();
            RebuildGrid();
            _selection.Hide();
            _cursor.Hide();
            OfficeAtmosphere.Apply(Layout, _camera);
        }

        /// <summary>Refaz só a casca (cores de chão/parede mudaram).</summary>
        public void RefreshShell()
        {
            OfficeBuilder.BuildShell(Layout, _office);
            RebuildGrid();
            OfficeAtmosphere.Apply(Layout, _camera);
        }

        /// <summary>Refaz o que depende da mobília: luzes de teto, assentos, bonecos e guias deles.</summary>
        public void RefreshDerived()
        {
            var lights = OfficeBuilder.BuildCeilingLights(Layout, _office);
            OfficeBuilder.BuildSeats(Layout, _office);
            OfficeCrew.Populate(Layout, _office, _people ? Layout.Seats.Count : 0);

            Replace(ref _lightMarkers, "LightMarkers");
            for (int i = 0; i < lights.childCount; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(marker.GetComponent<Collider>());
                marker.name = "LightMarker";
                marker.transform.SetParent(_lightMarkers, false);
                marker.transform.localPosition = lights.GetChild(i).localPosition;
                marker.transform.localScale = Vector3.one * 0.28f;
                var renderer = marker.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = UnlitMaterial(new Color(1f, 0.86f, 0.35f));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Replace(ref _seatLabels, "SeatLabels");
            for (int i = 0; i < Layout.Seats.Count; i++)
            {
                OfficeBuilder.SeatPose(Layout, Layout.Seats[i], out Vector3 position, out _);
                var label = new GameObject("SeatLabel_" + i);
                label.transform.SetParent(_seatLabels, false);
                label.transform.localPosition = position + Vector3.up * 1.75f;
                var text = label.AddComponent<TextMesh>();
                text.text = i == 0 ? "F" : i.ToString();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 64;
                text.characterSize = 0.07f;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.fontStyle = FontStyle.Bold;
                text.color = i == 0 ? new Color(1f, 0.78f, 0.15f) : new Color(0.25f, 0.85f, 1f);
                var renderer = label.GetComponent<MeshRenderer>();
                if (text.font != null) renderer.sharedMaterial = text.font.material;
            }
            ApplyGuideVisibility();
            OfficeAtmosphere.RefreshReflection(Layout, _camera);
        }

        private void Replace(ref Transform group, string name)
        {
            if (group != null) Object.Destroy(group.gameObject);
            group = new GameObject(name).transform;
            group.SetParent(_tools, false);
        }

        private GameObject SpawnPiece(FurniturePlacement placement)
        {
            var go = OfficeBuilder.CreatePiece(placement, Layout, _furnitureRoot);
            if (go == null)
            {
                go = new GameObject(placement.furniture != null ? placement.furniture.EditorName : "Vazio");
                go.transform.SetParent(_furnitureRoot, false);
            }
            var motion = go.AddComponent<MapPieceMotion>();
            motion.Snap(go.transform.localPosition, go.transform.localRotation);
            return go;
        }

        private void RebuildGrid()
        {
            Replace(ref _grid, "Grid");
            float cs = Layout.CellSize;
            float width = Layout.Cols * cs, depth = Layout.Rows * cs;
            var material = UnlitMaterial(Color.Lerp(Layout.FloorColor, Color.black, 0.22f));
            for (int x = 0; x <= Layout.Cols; x++)
                GridLine(new Vector3(x * cs - width * 0.5f, 0.006f, 0f), new Vector3(0.025f, 0.01f, depth), material);
            for (int y = 0; y <= Layout.Rows; y++)
                GridLine(new Vector3(0f, 0.006f, y * cs - depth * 0.5f), new Vector3(width, 0.01f, 0.025f), material);
            ApplyGuideVisibility();
        }

        private void GridLine(Vector3 position, Vector3 scale, Material material)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(line.GetComponent<Collider>());
            line.name = "Line";
            line.transform.SetParent(_grid, false);
            line.transform.localPosition = position;
            line.transform.localScale = scale;
            var renderer = line.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // ------------------------------------------------------------------ guias e vista

        public void SetGuides(bool on)
        {
            _guides = on;
            ApplyGuideVisibility();
        }

        public void SetPeople(bool on)
        {
            _people = on;
            RefreshDerived();
        }

        private void ApplyGuideVisibility()
        {
            if (_grid != null) _grid.gameObject.SetActive(_guides);
            if (_lightMarkers != null) _lightMarkers.gameObject.SetActive(_guides);
            if (_seatLabels != null) _seatLabels.gameObject.SetActive(_guides);
        }

        /// <summary>Etiquetas viradas para a câmera e paredes escondidas quando ficariam na frente.</summary>
        public void FaceCamera(Camera camera, Vector3 targetForward)
        {
            if (_seatLabels != null)
                for (int i = 0; i < _seatLabels.childCount; i++)
                    _seatLabels.GetChild(i).rotation = camera.transform.rotation;

            bool showBack = targetForward.z <= 0.01f, showLeft = targetForward.x <= 0.01f;
            for (int i = 0; i < _office.childCount; i++)
            {
                // parede + capa + rodapé compartilham o prefixo do nome
                var child = _office.GetChild(i);
                if (child.name.StartsWith(OfficeBuilder.BackWallName)) child.gameObject.SetActive(showBack);
                else if (child.name.StartsWith(OfficeBuilder.LeftWallName)) child.gameObject.SetActive(showLeft);
            }

            // Peça de parede some junto com a parede em que está pendurada.
            var placements = Layout.EditorPlacements;
            for (int i = 0; i < _pieces.Count && i < placements.Count; i++)
            {
                var p = placements[i];
                if (p.furniture == null || !p.furniture.IsWallPiece) continue;
                bool onBackWall = p.cell.y <= p.cell.x;
                bool visible = onBackWall ? showBack : showLeft;
                if (_pieces[i].activeSelf != visible) _pieces[i].SetActive(visible);
            }
        }

        // ------------------------------------------------------------------ peças

        public void AddPiece(int index)
        {
            var go = SpawnPiece(Layout.EditorPlacements[index]);
            _pieces.Insert(index, go);
            go.GetComponent<MapPieceMotion>().Pop();
        }

        public void RemovePiece(int index)
        {
            if (index < 0 || index >= _pieces.Count) return;
            Object.Destroy(_pieces[index]);
            _pieces.RemoveAt(index);
            RefreshDerived();
        }

        /// <summary>
        /// Alinha o palco com a sessão depois de uma operação que pode ter mexido em várias
        /// peças (mover um móvel leva junto o que está em cima; remover pode derrubar ou tirar).
        /// Se a quantidade mudou, remonta; senão cada peça desliza para a pose nova.
        /// </summary>
        public void SyncPieces()
        {
            if (_pieces.Count != Layout.EditorPlacements.Count)
            {
                RebuildAll();
                return;
            }
            for (int i = 0; i < _pieces.Count; i++) SettlePiece(i);
            RefreshDerived();
        }

        /// <summary>Leva a peça (com animação) para a pose que ela tem na sessão.</summary>
        public void SettlePiece(int index)
        {
            if (index < 0 || index >= _pieces.Count) return;
            OfficeBuilder.PiecePose(Layout.EditorPlacements[index], Layout, out Vector3 position, out Quaternion rotation);
            _pieces[index].GetComponent<MapPieceMotion>().MoveTo(position, rotation);
        }

        /// <summary>Mostra a peça numa célula provisória (arrasto), sem alterar a sessão.</summary>
        public void PreviewPieceAt(int index, Vector2Int cell, bool onTop)
        {
            if (index < 0 || index >= _pieces.Count) return;
            var placement = Layout.EditorPlacements[index];
            placement.cell = cell;
            placement.onTop = onTop;
            OfficeBuilder.PiecePose(placement, Layout, out Vector3 position, out Quaternion rotation);
            _pieces[index].GetComponent<MapPieceMotion>().MoveTo(position, rotation);
        }

        // ------------------------------------------------------------------ fantasma e molduras

        /// <summary>Mostra a peça armada na célula, com a moldura verde (cabe) ou vermelha (não cabe).</summary>
        public void ShowGhost(FurnitureDef3DSO def, Vector2Int cell, int rotation, Vector2Int align, bool onTop, bool valid)
        {
            var placement = new FurniturePlacement { furniture = def, cell = cell, rotationSteps = rotation, align = align, onTop = onTop };
            OfficeBuilder.PiecePose(placement, Layout, out Vector3 position, out Quaternion pose);
            if (_ghost == null || _ghostDef != def)
            {
                DropGhost();
                _ghostDef = def;
                _ghost = OfficeBuilder.CreatePiece(placement, Layout, _tools);
                if (_ghost == null) { _ghost = new GameObject("Ghost"); _ghost.transform.SetParent(_tools, false); }
                _ghost.name = "Ghost";
                _ghost.AddComponent<MapPieceMotion>().Snap(position, pose);
            }
            _ghost.GetComponent<MapPieceMotion>().MoveTo(position, pose);
            _cursor.Show(Layout, cell, OfficeGrid.RotatedFootprint(def.FootprintInCells, rotation), valid ? ValidColor : InvalidColor);
        }

        public void DropGhost()
        {
            if (_ghost != null) Object.Destroy(_ghost);
            _ghost = null;
            _ghostDef = null;
        }

        public void ShowCursor(Vector2Int cell, Vector2Int footprint, Color color) => _cursor.Show(Layout, cell, footprint, color);
        public void HideCursor() => _cursor.Hide();

        public void ShowSelection(int index)
        {
            if (!_session.Valid(index)) { _selection.Hide(); return; }
            var p = Layout.EditorPlacements[index];
            _selection.Show(Layout, p.cell, OfficeGrid.RotatedFootprint(p.furniture.FootprintInCells, p.rotationSteps), SelectedColor);
        }

        public void HideSelection() => _selection.Hide();

        // ------------------------------------------------------------------ ponteiro

        /// <summary>
        /// Ponto do chão sob o raio, em coordenadas de grid contínuas (0..cols, 0..rows).
        /// Devolve false se o raio não cruza o chão.
        /// </summary>
        public bool GroundPoint(Ray ray, out Vector2 gridPoint)
        {
            gridPoint = default;
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return false;
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) return false;
            Vector3 hit = ray.origin + ray.direction * t;
            float cs = Layout.CellSize;
            gridPoint = new Vector2(hit.x / cs + Layout.Cols * 0.5f, hit.z / cs + Layout.Rows * 0.5f);
            return true;
        }

        public bool InsideRoom(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Layout.Cols && cell.y < Layout.Rows;

        /// <summary>
        /// Peça sob o ponteiro: a primeira cujo volume o raio atinge (dá para clicar no corpo de
        /// uma estante alta, não só no chão dela); se nenhuma, a que ocupa a célula do chão.
        /// </summary>
        public int PickPiece(Ray ray)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            bool bestOnTop = false;
            for (int i = 0; i < _pieces.Count; i++)
            {
                if (!_session.Valid(i) || !TryBounds(_pieces[i], out Bounds bounds)) continue;
                // Tapete: só o chão dele conta, senão ele "rouba" o clique de tudo que está em cima.
                if (Layout.EditorPlacements[i].furniture.Category == FurnitureCategory.FloorFixture) continue;
                // Peça apoiada ganha do móvel de baixo: senão o volume da mesa "engole" o abajur.
                bool onTop = Layout.EditorPlacements[i].onTop;
                if (!bounds.IntersectRay(ray, out float distance)) continue;
                if (best >= 0 && bestOnTop && !onTop) continue;
                if (best >= 0 && bestOnTop == onTop && distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
                bestOnTop = onTop;
            }
            if (best >= 0) return best;
            if (!GroundPoint(ray, out Vector2 point)) return -1;
            return OfficeLayoutRules.PlacementAt(Layout.EditorPlacements,
                new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)));
        }

        private static bool TryBounds(GameObject piece, out Bounds bounds)
        {
            bounds = default;
            var renderers = piece.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        // ------------------------------------------------------------------ materiais

        public static Material UnlitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var material = new Material(shader);
            SetColor(material, color);
            return material;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
