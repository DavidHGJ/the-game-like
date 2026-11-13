using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Conversão célula↔mundo do grid 3D do escritório (plano XZ, Y para cima).
    /// A sala é centrada na origem: a célula (0,0) fica no canto -X,-Z (o canto
    /// "ao fundo", onde ficam as paredes), e as células crescem em +X,+Z na
    /// direção da câmera. É o único ponto de matemática do grid — editor e
    /// runtime consomem só este helper.
    /// </summary>
    public static class OfficeGrid
    {
        /// <summary>Centro de mundo de uma única célula (footprint 1×1).</summary>
        public static Vector3 CellCenter(int cellX, int cellY, int cols, int rows, float cellSize)
        {
            return FootprintCenter(cellX, cellY, 1, 1, cols, rows, cellSize);
        }

        /// <summary>
        /// Centro de mundo de um footprint cujo canto mínimo é (cellX,cellY) e que
        /// ocupa (fx × fy) células. Y = 0 (os prefabs têm pivô na base).
        /// </summary>
        public static Vector3 FootprintCenter(int cellX, int cellY, int fx, int fy, int cols, int rows, float cellSize)
        {
            float x = (cellX + fx * 0.5f) * cellSize - cols * cellSize * 0.5f;
            float z = (cellY + fy * 0.5f) * cellSize - rows * cellSize * 0.5f;
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Tamanho ortográfico da câmera do jogo para enquadrar uma sala: 6 para salas até o
        /// tamanho da garagem (8×7) e proporcionalmente maior para salas maiores, de modo que o
        /// escritório sempre caiba inteiro na tela.
        /// </summary>
        public static float GameCameraSize(int cols, int rows) => 6f * Mathf.Max(1f, (cols + rows) / 15f);

        /// <summary>
        /// Footprint efetivo após rotação em passos de 90°: passos ímpares trocam
        /// largura por profundidade.
        /// </summary>
        public static Vector2Int RotatedFootprint(Vector2Int footprint, int rotationSteps)
        {
            return (rotationSteps % 2 == 0) ? footprint : new Vector2Int(footprint.y, footprint.x);
        }
    }
}
