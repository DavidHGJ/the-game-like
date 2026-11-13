using System;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Categories of gameplay effect a piece of furniture can grant.
    /// Intentionally generic — a specific FurnitureDefinitionSO picks one
    /// category and a magnitude; the formula/system that reads this value
    /// (e.g. the quality formula, employee productivity) decides how to
    /// apply it. Extend this enum as new effect types are needed; it is the
    /// single source of truth for "what kinds of bonus furniture can grant".
    /// </summary>
    public enum FurnitureEffectType
    {
        None,
        EmployeeProductivity,
        EmployeeMoraleRecoveryRate,
        ResearchPointGenerationRate,
        BurnoutAccumulationRate,
        HypeGenerationRate
    }

    /// <summary>
    /// A single effect a furniture variant grants, expressed as a category
    /// and a magnitude. Magnitude's unit/meaning depends on the effect type
    /// (e.g. a percentage multiplier) and is interpreted by the system that
    /// consumes it, not by this struct.
    /// </summary>
    [Serializable]
    public struct FurnitureEffect
    {
        public FurnitureEffectType type;
        public float magnitude;
    }
}
