#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEngine;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Roteiro de validação do Map Builder (só editor/development build): disparado por
    /// <c>-glmapshot &lt;pasta&gt;</c>, dirige a ferramenta com ponteiro sintético — arma um
    /// móvel, coloca, gira, cria assento, salva e relê o arquivo — tirando prints em cada
    /// passo, e encerra. Serve para conferir a ferramenta sem alguém clicando.
    /// </summary>
    internal sealed class MapBuilderAutoshot : MonoBehaviour
    {
        private const string TestMapName = "ZZ_AutoshotTeste";

        private MapBuilderController _controller;
        private string _folder;
        private int _shot;

        public void Begin(MapBuilderController controller, string folder)
        {
            _controller = controller;
            _folder = folder;
            Directory.CreateDirectory(folder);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var session = _controller.Session;
            var catalog = OfficeMapStore.Catalog;
            _controller.PointerOverride = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("aberto");

            var sofa = catalog != null ? catalog.Find(null, "sofa") : null;
            var chair = catalog != null ? catalog.Find(null, "office_chair") : null;
            if (sofa == null || chair == null)
            {
                Debug.LogError("[MapBuilderAutoshot] sofa/office_chair ausentes do catálogo.");
                Finish(3);
                yield break;
            }

            int before = session.Layout.Placements.Count;
            _controller.Arm(sofa);
            if (!TryPointAtFreeSpot(sofa, 0)) Debug.LogWarning("[MapBuilderAutoshot] sem espaço livre para o sofá.");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("fantasma");

            _controller.ClickOverride = true;
            yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("[MapBuilderAutoshot] colocar: " + before + " -> " + session.Layout.Placements.Count);
            _controller.Disarm();
            _controller.ClickOverride = true;
            yield return new WaitForSecondsRealtime(0.4f);
            Debug.Log("[MapBuilderAutoshot] selecionada=" + _controller.Selected);
            _controller.RotateCurrent(1);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Shot("girando");
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Shot("girada");

            // empilhar: abajur em cima do primeiro móvel com tampo, num canto; depois texturas.
            int expected = before + 1;
            var lamp = catalog.Find(null, "desk_lamp");
            int support = -1;
            for (int i = 0; i < session.Layout.Placements.Count && support < 0; i++)
                if (session.Layout.Placements[i].furniture.SupportsItemsOnTop && !session.Layout.Placements[i].onTop) support = i;
            if (lamp != null && support >= 0)
            {
                var supportCell = session.Layout.Placements[support].cell;
                _controller.Arm(lamp);
                _controller.SetAlign(new Vector2Int(-1, -1));
                PointAtCell(supportCell);
                yield return new WaitForSecondsRealtime(0.6f);
                _controller.ClickOverride = true;
                yield return new WaitForSecondsRealtime(0.6f);
                var placed = session.Layout.Placements[session.Layout.Placements.Count - 1];
                Debug.Log("[MapBuilderAutoshot] abajur: onTop=" + placed.onTop + " align=" + placed.align + " total=" + session.Layout.Placements.Count);
                if (placed.onTop && placed.furniture == lamp) expected++;
                _controller.Disarm();
                _controller.ClickOverride = true;
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("empilhado_no_canto");
            }
            else Debug.LogWarning("[MapBuilderAutoshot] sem abajur/tampo para testar empilhamento.");

            _controller.SetStyles(SurfaceStyle.WoodPlanks, SurfaceStyle.Brick);
            _controller.SetColors(new Color(0.72f, 0.52f, 0.34f), new Color(0.72f, 0.36f, 0.28f), true);
            _controller.Select(-1);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("tijolinho_e_tabuas");

            _controller.Arm(chair);
            TryPointAtFreeSpot(chair, 0);
            yield return new WaitForSecondsRealtime(0.4f);
            _controller.ClickOverride = true;
            yield return new WaitForSecondsRealtime(0.4f);
            _controller.SetTool(MapTool.Seats);
            int seatsBefore = session.Layout.Seats.Count;
            _controller.ClickOverride = true;
            yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("[MapBuilderAutoshot] assentos: " + seatsBefore + " -> " + session.Layout.Seats.Count);
            yield return Shot("assentos");

            _controller.SetTool(MapTool.Furniture);
            _controller.SetGuides(false);
            _controller.PointerOverride = new Vector2(-10f, -10f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("vista_do_jogo");

            _controller.View.Turn(1);
            _controller.View.ZoomBy(-2f);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("vista_girada");

            // salvar e reler: prova que o arquivo guarda o que está na tela.
            string realName = session.Name;
            bool realUsed = session.UsedInGame;
            session.Name = TestMapName;
            session.UsedInGame = false;
            bool saved = _controller.Save();
            OfficeMapData reread = null;
            foreach (var map in OfficeMapStore.ListAll())
                if (map.name == TestMapName) reread = map;
            Debug.Log("[MapBuilderAutoshot] salvar=" + saved + " relido=" + (reread != null)
                      + " pecas=" + (reread != null ? reread.pieces.Count : -1) + "/" + session.Layout.Placements.Count
                      + " assentos=" + (reread != null ? reread.seats.Count : -1) + "/" + session.Layout.Seats.Count);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("salvo");
            OfficeMapStore.Delete(TestMapName);
            session.Name = realName;
            session.UsedInGame = realUsed;

            bool ok = saved && reread != null && reread.pieces.Count == session.Layout.Placements.Count
                      && session.Layout.Placements.Count == expected + 1 && session.Layout.Seats.Count == seatsBefore + 1;
            Debug.Log(ok ? "GAMELIKE_MAPBUILDER_SHOT_OK" : "GAMELIKE_MAPBUILDER_SHOT_FAIL");
            Finish(ok ? 0 : 4);
        }

        /// <summary>Aponta o ponteiro sintético para o centro do primeiro espaço livre onde a peça cabe.</summary>
        private bool TryPointAtFreeSpot(FurnitureDef3DSO def, int rotation)
        {
            var layout = _controller.Session.Layout;
            var footprint = OfficeGrid.RotatedFootprint(def.FootprintInCells, rotation);
            for (int y = layout.Rows - footprint.y; y >= 0; y--)
            for (int x = layout.Cols - footprint.x; x >= 0; x--)
            {
                var cell = new Vector2Int(x, y);
                if (!_controller.Session.CanPlace(def, cell, rotation)) continue;
                Vector3 world = OfficeGrid.FootprintCenter(x, y, footprint.x, footprint.y, layout.Cols, layout.Rows, layout.CellSize);
                Vector3 screen = _controller.View.Camera.WorldToScreenPoint(world);
                if (_controller.Hud.IsPointerOverUi(screen)) continue;
                _controller.PointerOverride = new Vector2(screen.x, screen.y);
                return true;
            }
            return false;
        }

        private void PointAtCell(Vector2Int cell)
        {
            var layout = _controller.Session.Layout;
            Vector3 world = OfficeGrid.CellCenter(cell.x, cell.y, layout.Cols, layout.Rows, layout.CellSize);
            Vector3 screen = _controller.View.Camera.WorldToScreenPoint(world);
            _controller.PointerOverride = new Vector2(screen.x, screen.y);
        }

        private IEnumerator Shot(string label)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_folder, (++_shot).ToString("00") + "_" + label + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
        }

        private static void Finish(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
#endif
