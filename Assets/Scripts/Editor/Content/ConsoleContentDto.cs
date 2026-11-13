using System;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Forma de dados que um agente produz para um Console/Plataforma (GDD seção 15).
    /// Só existe para ser desserializado por <see cref="ConsoleContentImporter"/>.
    /// O campo <c>category</c> aceita o nome do enum <c>PlatformCategory</c>
    /// (PC/Console/Handheld/Mobile/VR). Popularidade em 0..1; licença em dinheiro bruto.
    /// </summary>
    [Serializable]
    public sealed class ConsoleContentDto
    {
        public string id;
        public string key;
        public string displayNamePtBr;
        public string descriptionPtBr;
        public string manufacturer;
        public string category;
        public int releaseYear;
        public long licenseCost;
        public float initialPopularity;
        public float peakPopularity;
        public int lifespanYears;
    }
}
