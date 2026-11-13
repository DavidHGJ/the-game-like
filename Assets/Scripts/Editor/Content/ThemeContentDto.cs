using System;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Forma de dados que um agente produz para um Tema (GDD 18.6/18.7). Só existe
    /// para ser desserializado por <see cref="ThemeContentImporter"/>. O campo
    /// <c>category</c> aceita o nome do enum <c>ThemeCategory</c>; cada compat usa
    /// o slug do gênero (nome do asset) e o símbolo de peso da escala do GDD.
    /// </summary>
    [Serializable]
    public sealed class ThemeContentDto
    {
        public string id;
        public string key;
        public string displayNamePtBr;
        public string descriptionPtBr;
        public string category;
        public GenreCompatDto[] compatibilities;
    }

    [Serializable]
    public sealed class GenreCompatDto
    {
        public string genre;  // slug do gênero (ex: action_rpg)
        public string weight; // "---".."+++"
    }
}
