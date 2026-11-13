using UnityEngine;

namespace GameLike.Data.Base
{
    /// <summary>
    /// Base class for every definition ScriptableObject (Genre, Theme, Console,
    /// EngineModule, Trait, ...). Provides a stable, auto-generated unique
    /// identifier that survives renaming or moving the asset file.
    ///
    /// Saves must reference content by <see cref="Id"/>, never by asset/file name.
    /// Renaming an asset in the editor never breaks an existing save, because the
    /// Id field is independent of the file name and is generated once.
    ///
    /// The Id is assigned automatically the first time the asset is created
    /// (see DefinitionAssetIdAssigner, editor-only) and is read-only from any
    /// runtime code — only the editor tooling may set it, and only when empty.
    /// </summary>
    public abstract class DefinitionAsset : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string id;

        [SerializeField, Tooltip("Nome amigável pt-BR mostrado ao jogador (ex: \"Action RPG\"). " +
            "Preenchido pelo importer a partir de displayNamePtBr. Enquanto a ponte com o " +
            "Localization Package não existe, a UI usa este campo em vez do slug cru.")]
        private string displayNamePt;

        /// <summary>Stable unique identifier. Never changes after creation.</summary>
        public string Id => id;

        /// <summary>Nome amigável pt-BR (pode estar vazio em assets antigos).</summary>
        public string DisplayNamePt => displayNamePt;

        /// <summary>
        /// Rótulo pronto para a UI: usa <see cref="DisplayNamePt"/> quando preenchido,
        /// senão deriva do nome do asset (slug) deixando-o legível. Nunca mostra o slug cru.
        /// </summary>
        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(displayNamePt) ? Prettify(name) : displayNamePt;

        private static string Prettify(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return slug;
            var parts = slug.Replace('_', ' ').Replace('-', ' ').Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Assigns the Id exactly once. Intended to be called only by editor
        /// tooling right after asset creation. No-ops if an Id is already set,
        /// guaranteeing the value is immutable once assigned.
        /// </summary>
        public void AssignIdIfEmpty(string newId)
        {
            if (!string.IsNullOrEmpty(id)) return;
            id = newId;
        }
    }
}
