namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// As quatro direções de orientação isométrica de uma peça de mobília
    /// (LEVEL_BUILDER.md §9.4). Vive na camada Definição porque
    /// <see cref="FurnitureDefinitionSO"/> declara quais direções uma peça
    /// permite — a camada Runtime (IsoGrid, FurnitureInstance) consome este
    /// enum, nunca o contrário.
    ///
    /// Mapeamento para a tela na projeção "Isometric Z as Y" (eixos da
    /// LEVEL_BUILDER.md §5), partindo da câmera padrão:
    /// <list type="bullet">
    /// <item><b>SouthEast</b> — frente da peça aponta para baixo-direita (+X de célula).</item>
    /// <item><b>SouthWest</b> — frente aponta para baixo-esquerda (-Y de célula).</item>
    /// <item><b>NorthWest</b> — frente aponta para cima-esquerda (-X de célula).</item>
    /// <item><b>NorthEast</b> — frente aponta para cima-direita (+Y de célula).</item>
    /// </list>
    /// A ordem do enum é a sequência de rotação horária (tecla R durante a
    /// colocação cicla nesta ordem entre as direções permitidas).
    /// </summary>
    public enum IsoDirection
    {
        SouthEast,
        SouthWest,
        NorthWest,
        NorthEast
    }
}
