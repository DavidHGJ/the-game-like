using System;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Staff
{
    /// <summary>
    /// Gera candidatos para o mercado de talentos (GDD 7.2). Puro e determinístico
    /// dado um <see cref="System.Random"/>, para o pool ser reproduzível e testável.
    /// Nomes caricatos combinam com o tom do jogo; salário vem de um resolvedor
    /// externo (lido do balanceamento pelo manager), mantendo o gerador desacoplado.
    /// </summary>
    public static class CandidateGenerator
    {
        private static readonly string[] FirstNames =
        {
            "Alex", "Bruna", "Caio", "Dani", "Edu", "Fê", "Gui", "Helô", "Ike", "Juca",
            "Kiko", "Lari", "Max", "Nina", "Osk", "Pati", "Quim", "Rafa", "Sol", "Teo",
            "Ulisses", "Val", "Wes", "Xana", "Yuri", "Zé"
        };

        private static readonly string[] LastNames =
        {
            "Pixeletti", "Buglione", "Shaderman", "Crunchfield", "Debugski", "Megabyte",
            "Framedrop", "Lootbox", "Respawn", "Patchnote", "Overclock", "Hotfix",
            "Polygon", "Checkpoint", "Rollback", "Netcode"
        };

        private static readonly PersonalityTrait[] Traits = (PersonalityTrait[])Enum.GetValues(typeof(PersonalityTrait));
        private static readonly Specialty[] HireableSpecialties =
        {
            Specialty.Design, Specialty.Art, Specialty.Audio, Specialty.Programming, Specialty.Content
        };

        /// <summary>Cria um candidato aleatório. <paramref name="qualityBias"/> 0..1 puxa a senioridade para cima (headhunting/Networking).</summary>
        public static EmployeeInstance Create(System.Random rng, Func<Seniority, long> salaryFor, float qualityBias = 0f)
        {
            var specialty = HireableSpecialties[rng.Next(HireableSpecialties.Length)];
            var seniority = RollSeniority(rng, qualityBias);
            var trait = rng.NextDouble() < 0.55 ? Traits[rng.Next(Traits.Length)] : PersonalityTrait.None;

            string name = $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";
            long salary = salaryFor != null ? salaryFor(seniority) : 2000;

            var e = new EmployeeInstance(Guid.NewGuid().ToString("N"), name, specialty, seniority, trait, salary)
            {
                mood01 = 0.75f + (float)rng.NextDouble() * 0.2f,
                burnout01 = 0f,
                xp = seniority switch
                {
                    Seniority.Mid => 100,
                    Seniority.Senior => 300,
                    Seniority.Star => 600,
                    _ => 0
                }
            };
            return e;
        }

        private static Seniority RollSeniority(System.Random rng, float qualityBias)
        {
            double roll = rng.NextDouble() + Mathf.Clamp01(qualityBias) * 0.4;
            if (roll > 1.25) return Seniority.Star;
            if (roll > 0.95) return Seniority.Senior;
            if (roll > 0.55) return Seniority.Mid;
            return Seniority.Junior;
        }
    }
}
