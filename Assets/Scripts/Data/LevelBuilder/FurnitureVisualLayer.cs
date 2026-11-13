using System;
using UnityEngine;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// A single drawable layer of a composite furniture sprite (e.g. the
    /// wooden base of a chair, or its upholstery). Layers stack in the order
    /// they appear in <see cref="FurnitureDefinitionSO.VisualLayers"/> —
    /// later entries render on top of earlier ones.
    ///
    /// <see cref="tintable"/> marks layers whose color is applied at runtime
    /// via sprite tinting rather than requiring a separately painted sprite
    /// per color variant — this is the mechanism behind "same chair, three
    /// upholstery colors" without drawing three full sprites.
    /// </summary>
    [Serializable]
    public struct FurnitureVisualLayer
    {
        public string layerName; // e.g. "Base", "Upholstery" — for editor clarity only
        public Sprite sprite;
        [Tooltip("Se marcado, esta camada pode ser recolorida via tint em runtime (ex: estofado). Desmarcado para camadas com cor fixa (ex: metal da base).")]
        public bool tintable;
    }
}
