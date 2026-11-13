using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Coloca o personagem "dev" sentado na cadeira do escritório (cena GameOffice),
    /// nomeado <c>DevCharacter</c> — que é o âncora de onde saem as bolinhas (orbs)
    /// do HUD (o GameUi procura por esse nome). O
    /// <c>Dev.fbx</c> já é modelado em pose sentada (base nos pés, assento ~0.47m),
    /// então basta posicioná-lo na célula da cadeira e alinhar a rotação ao monitor.
    /// Ferramenta de produção, não-destrutiva: roda por menu ou em batch.
    /// </summary>
    public static class DevCharacterSetup
    {
        private const string ScenePath = "Assets/Scenes/GameOffice.unity";
        private const string DevFbxPath = "Assets/Art/Characters/Dev/Dev.fbx";
        private const string ObjectName = "DevCharacter";

        // Calibração visual (ajustar e re-rodar se preciso).
        private const int ExtraYawSteps = 0;        // 0..3 passos de 90° somados à rotação da cadeira.
        private const float SeatYOffset = 0f;        // deslocamento vertical (pivô do Dev já na base/pés).
        private const float UniformScale = 1f;       // escala do Dev (1 = metros reais).
        private const float ForwardNudge = 0.05f;    // leve avanço na direção que ele encara (para a mesa).

        [MenuItem("Tools/GameLike/Office 3D/5 - Place Dev In Chair")]
        public static void Place()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var layout = LoadGarageLayout();
            if (layout == null)
            {
                Debug.LogError("[DevCharacterSetup] Nenhum OfficeLayoutSO encontrado em Resources/Office.");
                return;
            }

            if (!TryFindChair(layout, out Vector3 pos, out Quaternion rot))
            {
                Debug.LogError("[DevCharacterSetup] Nenhuma cadeira (office_chair/gaming_chair) no layout da garagem.");
                return;
            }

            var devFbx = AssetDatabase.LoadAssetAtPath<GameObject>(DevFbxPath);
            if (devFbx == null)
            {
                Debug.LogError($"[DevCharacterSetup] Dev.fbx não encontrado em {DevFbxPath}.");
                return;
            }

            // Remove um DevCharacter anterior, se houver (idempotente).
            var existing = GameObject.Find(ObjectName);
            if (existing != null) Object.DestroyImmediate(existing);

            var dev = (GameObject)PrefabUtility.InstantiatePrefab(devFbx);
            dev.name = ObjectName;
            Quaternion finalRot = rot * Quaternion.Euler(0f, ExtraYawSteps * 90f, 0f);
            Vector3 forward = finalRot * Vector3.forward;
            dev.transform.position = pos + new Vector3(0f, SeatYOffset, 0f) + forward * ForwardNudge;
            dev.transform.rotation = finalRot;
            dev.transform.localScale = Vector3.one * UniformScale;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DevCharacterSetup] DevCharacter posicionado em {dev.transform.position} (rot {finalRot.eulerAngles.y:0}°). " +
                      "É o âncora das bolinhas do HUD.");
        }

        private static OfficeLayoutSO LoadGarageLayout()
        {
            var all = Resources.LoadAll<OfficeLayoutSO>("Office");
            OfficeLayoutSO fallback = null;
            foreach (var l in all)
            {
                if (l == null) continue;
                if (l.Phase == CompanyPhase.Garage) return l;
                if (fallback == null) fallback = l;
            }
            return fallback;
        }

        /// <summary>
        /// Acha a primeira cadeira do layout e devolve sua posição/rotação de mundo,
        /// pela mesma matemática do <see cref="OfficeBuilder"/>.
        /// </summary>
        private static bool TryFindChair(OfficeLayoutSO layout, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            float cs = layout.CellSize;

            FurniturePlacement? chosen = null;
            foreach (var p in layout.Placements)
            {
                if (p.furniture == null) continue;
                string key = p.furniture.name != null ? p.furniture.name.ToLowerInvariant() : "";
                if (key.Contains("office_chair")) { chosen = p; break; }       // preferência: cadeira de escritório
                if (chosen == null && key.Contains("chair")) chosen = p;        // qualquer cadeira como fallback
            }
            if (chosen == null) return false;

            var p0 = chosen.Value;
            var footprint = OfficeGrid.RotatedFootprint(p0.furniture.FootprintInCells, p0.rotationSteps);
            pos = OfficeGrid.FootprintCenter(p0.cell.x, p0.cell.y, footprint.x, footprint.y,
                layout.Cols, layout.Rows, cs);
            rot = Quaternion.Euler(0f, p0.rotationSteps * 90f, 0f);
            return true;
        }
    }
}
