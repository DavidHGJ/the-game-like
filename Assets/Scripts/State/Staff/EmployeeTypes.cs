using GameLike.State;

namespace GameLike.State.Staff
{
    /// <summary>
    /// Especialidade de um funcionário. O GDD 7.1 lista Programação e Design como
    /// iniciais, com arquitetura extensível. No vertical slice as especialidades são
    /// mapeadas às 5 <see cref="DevArea"/> para que a composição da equipe influencie
    /// a entrega por área (o "nível e especialização da equipe" da fórmula de
    /// qualidade, GDD 19.7, tornado concreto). <see cref="Generalist"/> é o fundador.
    /// </summary>
    public enum Specialty
    {
        Design = 0,
        Art = 1,
        Audio = 2,
        Programming = 3,
        Content = 4,
        Generalist = 5
    }

    /// <summary>Senioridade de um funcionário (GDD 19.2) — define salário e multiplicador de produção.</summary>
    public enum Seniority
    {
        Junior = 0,
        Mid = 1,
        Senior = 2,
        Star = 3
    }

    /// <summary>
    /// Traços de personalidade (GDD 7.4). Cada traço aplica um pequeno modificador
    /// a produção, burnout ou humor. Extensível — no futuro vira <c>TraitSO</c>.
    /// </summary>
    public enum PersonalityTrait
    {
        None = 0,
        Workaholic,
        Lazy,
        Perfectionist,
        Egocentric,
        Mentor,
        Loyal,
        Ambitious,
        Creative,
        BurnoutProne
    }

    /// <summary>Mapeia uma <see cref="Specialty"/> para a <see cref="DevArea"/> correspondente (ou -1 para generalista).</summary>
    public static class SpecialtyMap
    {
        public static int ToAreaIndex(Specialty specialty) => specialty switch
        {
            Specialty.Design => (int)DevArea.Design,
            Specialty.Art => (int)DevArea.Art,
            Specialty.Audio => (int)DevArea.Audio,
            Specialty.Programming => (int)DevArea.Tech,
            Specialty.Content => (int)DevArea.Content,
            _ => -1
        };
    }
}
