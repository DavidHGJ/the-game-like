namespace GameLike.Systems.Core
{
    /// <summary>
    /// Transporta a intenção do menu principal para a cena de jogo (GDD 20.1).
    /// O jogo abre no menu; o jogador escolhe "Novo jogo" ou "Continuar" e só então
    /// a cena de gameplay carrega. O <see cref="GameController"/> lê este estado no
    /// <c>Start</c> para decidir se inicia uma run nova ou carrega um save, em vez de
    /// auto-iniciar uma run (o que fazia o jogo "abrir direto no jogo").
    ///
    /// É estático e sobrevive à troca de cena (não é destruído com a cena do menu).
    /// </summary>
    public static class GameBoot
    {
        public enum BootMode
        {
            None,
            NewRun,
            Continue
        }

        /// <summary>Intenção corrente. <see cref="BootMode.None"/> quando já foi consumida.</summary>
        public static BootMode Mode { get; private set; }

        /// <summary>Nome da empresa escolhido no menu (para <see cref="BootMode.NewRun"/>).</summary>
        public static string CompanyName { get; private set; }

        /// <summary>Slot de save a carregar (para <see cref="BootMode.Continue"/>).</summary>
        public static int Slot { get; private set; }

        /// <summary>O menu pede uma run nova com o nome de empresa dado.</summary>
        public static void RequestNewRun(string companyName)
        {
            Mode = BootMode.NewRun;
            CompanyName = companyName;
            Slot = 0;
        }

        /// <summary>O menu pede para continuar o save do slot dado.</summary>
        public static void RequestContinue(int slot)
        {
            Mode = BootMode.Continue;
            CompanyName = null;
            Slot = slot;
        }

        /// <summary>Limpa a intenção depois de consumida pelo <see cref="GameController"/>.</summary>
        public static void Clear()
        {
            Mode = BootMode.None;
            CompanyName = null;
            Slot = 0;
        }
    }
}
