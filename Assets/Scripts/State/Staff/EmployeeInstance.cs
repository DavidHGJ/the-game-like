using System;

namespace GameLike.State.Staff
{
    /// <summary>
    /// Estado vivo de um funcionário (camada de Estado, serializável). Guarda os
    /// valores mutáveis de um membro da equipe: especialidade, senioridade, XP,
    /// humor e burnout. É criado pelo mercado de talentos e vive em
    /// <see cref="CompanyState.employees"/>. Referenciado por <see cref="id"/> estável
    /// (GUID) em saves, nunca pelo nome.
    /// </summary>
    [Serializable]
    public sealed class EmployeeInstance
    {
        public string id;
        public string name;
        public Specialty specialty;
        public Seniority seniority;
        public PersonalityTrait trait = PersonalityTrait.None;

        /// <summary>XP acumulado; promove a senioridade ao cruzar thresholds (GDD 7.3).</summary>
        public int xp;
        /// <summary>Humor 0..1 — alto melhora produtividade levemente (GDD 7.5).</summary>
        public float mood01 = 0.8f;
        /// <summary>Burnout 0..1 — alto derruba produtividade e arrisca demissão (GDD 7.5).</summary>
        public float burnout01;
        /// <summary>Salário semanal em $ (GDD 19.2), definido na contratação pela senioridade.</summary>
        public long weeklySalary;

        /// <summary>True enquanto o funcionário está empregado (false = ainda candidato no mercado).</summary>
        public bool hired;

        public EmployeeInstance() { }

        public EmployeeInstance(string id, string name, Specialty specialty, Seniority seniority,
            PersonalityTrait trait, long weeklySalary)
        {
            this.id = id;
            this.name = name;
            this.specialty = specialty;
            this.seniority = seniority;
            this.trait = trait;
            this.weeklySalary = weeklySalary;
        }

        /// <summary>Multiplicador de produção pela senioridade (Junior 1.0 → Star 2.2).</summary>
        public float SeniorityMultiplier => seniority switch
        {
            Seniority.Junior => 1.0f,
            Seniority.Mid => 1.4f,
            Seniority.Senior => 1.8f,
            Seniority.Star => 2.2f,
            _ => 1.0f
        };
    }
}
