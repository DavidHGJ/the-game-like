using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Base class for every balance configuration ScriptableObject
    /// (EconomyBalanceSO, ResearchBalanceSO, EngineBalanceSO, ...).
    ///
    /// Unlike <see cref="GameLike.Data.Base.DefinitionAsset"/>, balance assets
    /// do not need a stable Id — there is exactly one instance of each balance
    /// type in the project (e.g. one EconomyBalanceSO), referenced by type
    /// through <see cref="BalanceProvider"/>, not by content key.
    ///
    /// Fields on subclasses are read directly by managers every time they are
    /// needed (never copied into a local variable at startup), so editing a
    /// value in the Inspector takes effect immediately, including mid-Play
    /// Mode — this is what gives hot-reload-like iteration without an actual
    /// hot-reload mechanism.
    /// </summary>
    public abstract class BalanceAsset : ScriptableObject
    {
    }
}
