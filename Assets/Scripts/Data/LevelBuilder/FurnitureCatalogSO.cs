using System.Collections.Generic;
using UnityEngine;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Índice de toda a mobília disponível (<see cref="FurnitureDef3DSO"/>) e do boneco padrão.
    /// Existe para o runtime conseguir listar as peças e resolver o Id salvo num
    /// <see cref="OfficeMapData"/>. É mantido automaticamente pelo editor
    /// (<c>FurnitureCatalogSync</c>) — nunca edite a lista à mão.
    /// </summary>
    public sealed class FurnitureCatalogSO : ScriptableObject
    {
        /// <summary>Caminho em Resources (sem extensão).</summary>
        public const string ResourcePath = "Office/FurnitureCatalog";

        [SerializeField] private List<FurnitureDef3DSO> furniture = new List<FurnitureDef3DSO>();
        [SerializeField] private GameObject defaultCharacter;

        public IReadOnlyList<FurnitureDef3DSO> Furniture => furniture;

        /// <summary>Boneco sentado usado nos cenários salvos pelo Map Builder.</summary>
        public GameObject DefaultCharacter => defaultCharacter;

        /// <summary>Resolve uma peça pelo Id estável; se não achar, tenta pelo nome do asset.</summary>
        public FurnitureDef3DSO Find(string id, string key)
        {
            FurnitureDef3DSO byKey = null;
            for (int i = 0; i < furniture.Count; i++)
            {
                var def = furniture[i];
                if (def == null) continue;
                if (!string.IsNullOrEmpty(id) && def.Id == id) return def;
                if (byKey == null && !string.IsNullOrEmpty(key) && def.name == key) byKey = def;
            }
            return byKey;
        }

        /// <summary>Substitui o conteúdo do catálogo (ferramenta de sincronização e testes).</summary>
        public void SetContents(IEnumerable<FurnitureDef3DSO> defs, GameObject character)
        {
            furniture = new List<FurnitureDef3DSO>(defs);
            defaultCharacter = character;
        }
    }
}
