#if UNITY_EDITOR
using GameLike.Data.Balance;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Data
{
    /// <summary>
    /// Editor-only sanity checks for balance assets. Currently validates that
    /// <see cref="QualityFormulaBalanceSO"/>'s weights sum to 1.0 — a silent
    /// mismatch here would skew every game's quality score without an obvious
    /// symptom, so it is checked proactively whenever the asset is modified
    /// in the Inspector, rather than relying on someone noticing during play.
    /// </summary>
    [CustomEditor(typeof(QualityFormulaBalanceSO))]
    public sealed class QualityFormulaBalanceEditor : UnityEditor.Editor
    {
        private const float Tolerance = 0.001f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var balance = (QualityFormulaBalanceSO)target;
            var total = balance.TotalWeight;

            if (Mathf.Abs(total - 1f) > Tolerance)
            {
                EditorGUILayout.HelpBox(
                    $"A soma dos pesos é {total:0.000}, deveria ser 1.000. " +
                    "Ajuste os campos para que a fórmula de qualidade não fique sub ou sobre-ponderada.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Soma dos pesos: {total:0.000} ✓", MessageType.Info);
            }
        }
    }
}
#endif
