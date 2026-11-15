using System.Collections.Generic;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Como a pessoa distribui o esforço entre os itens de cada fase.</summary>
    public enum DevAllocation
    {
        /// <summary>Segue o perfil ideal do gênero (jogador que acerta a mistura).</summary>
        Ideal,
        /// <summary>Divide igual entre os itens (jogador que não pensa na mistura).</summary>
        Even,
        /// <summary>Inverte o perfil do gênero (jogador que erra a mistura).</summary>
        Inverted
    }

    /// <summary>Combinação gênero × tema do jogo (GDD 18.7), que vira o componente de 20% da nota.</summary>
    public enum DevCombo
    {
        /// <summary>Tema que combina muito com o gênero (+++).</summary>
        Good,
        /// <summary>Tema neutro.</summary>
        Neutral,
        /// <summary>Tema que não combina (---).</summary>
        Bad
    }

    /// <summary>Engine da empresa no cenário (adendo 23.18).</summary>
    public enum DevEngine
    {
        None,
        /// <summary>Módulos iniciais (renderizador 2D e áudio básicos).</summary>
        Starter,
        /// <summary>Amenização máxima em todos os blocos que têm módulo.</summary>
        Full
    }

    /// <summary>Quando e onde o fundador faz treinamento entre um jogo e outro.</summary>
    public enum DevTraining
    {
        None,
        /// <summary>Um treino por jogo na área mais fraca do fundador.</summary>
        WeakestEachGame,
        /// <summary>Um treino por jogo na área cuja fase entregou o menor volume no último jogo.</summary>
        WorstPhaseEachGame
    }

    /// <summary>Um membro contratado num cenário: especialidade e senioridade (a aptidão sai das regras do jogo).</summary>
    public readonly struct DevHire
    {
        public readonly Specialty Specialty;
        public readonly Seniority Seniority;

        public DevHire(Specialty specialty, Seniority seniority)
        {
            Specialty = specialty;
            Seniority = seniority;
        }
    }

    /// <summary>Cenário de desenvolvimento: fundador, equipe, tamanho e estratégia, repetido por vários jogos.</summary>
    public sealed class DevScenario
    {
        public string name;
        /// <summary>Aptidão fixa do fundador (5 áreas). Nulo = sorteia como no jogo, uma por repetição.</summary>
        public float[] founderAptitude;
        public List<DevHire> hires = new List<DevHire>();
        public ProjectSize size = ProjectSize.Small;
        public DevAllocation allocation = DevAllocation.Ideal;
        public DevTraining training = DevTraining.None;
        public bool crunch;
        public int games = 10;
        /// <summary>Módulos de engine pesquisados (amenizam os blocos ligados a eles).</summary>
        public DevEngine engine = DevEngine.Starter;
        /// <summary>Combinação gênero × tema (componente de 20% da nota).</summary>
        public DevCombo combo = DevCombo.Neutral;
        /// <summary>Compatibilidade gênero × tema exata (−3..+3); quando definida, vale no lugar de <see cref="combo"/>.</summary>
        public int? themeWeight;
        /// <summary>
        /// Erro da divisão por fase (0 = perfeita … 1 = tudo no bloco menos importante). Quando definido, vale no
        /// lugar de <see cref="allocation"/>: a divisão da fase é o ideal misturado com a pior divisão pelo erro.
        /// </summary>
        public float[] phaseError;
    }

    /// <summary>Resultado de um jogo do cenário.</summary>
    public sealed class DevGameResult
    {
        public int game;
        public int weeks;
        /// <summary>Aptidão média do fundador nas 5 áreas, ANTES do jogo.</summary>
        public float founderAptitude;
        /// <summary>Menor aptidão do fundador antes do jogo.</summary>
        public float founderWeakest;
        public float points;
        public float expectedPoints;
        /// <summary>Acerto médio só da divisão nos blocos (0..1) — a decisão, sem pontos nem engine.</summary>
        public float mix;
        /// <summary>Volume de cada fase, entregue ÷ esperado (1 = encheu os tetos; 2 = uma dobra).</summary>
        public float[] phaseVolume = new float[ParameterCatalog.PhaseCount];
        /// <summary>Acerto médio das fases (0..1) com pontos e engine.</summary>
        public float phasesAccuracy;
        /// <summary>Acerto do tema (0..1).</summary>
        public float themeAccuracy;
        /// <summary>Desconto por bugs (0..1).</summary>
        public float bugPenalty;
        /// <summary>Nota estimada do jogo (adendo 23.18, sem o sorteio da crítica, já com o desconto dos bugs esperados).</summary>
        public float score;
        public float bugs;
        public long trainingCost;
    }

    /// <summary>
    /// Simulador do desenvolvimento por aptidão (adendo 23.16), sem rodar o jogo: produz jogos seguidos
    /// semana a semana com as mesmas regras do <see cref="ProjectManager"/> (via
    /// <see cref="DevelopmentRules"/>), aplica prática e treino, e estima a nota de cada jogo.
    /// Sem dinheiro, mercado ou vendas — só "quanto a equipe produz e como isso vira nota".
    /// </summary>
    public static class DevelopmentSimulator
    {
        /// <summary>Roda o cenário uma vez (aptidão sorteada pela semente quando o cenário não fixa uma).</summary>
        public static List<DevGameResult> Run(DevScenario scenario, GenreSO genre, int seed)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var rng = new System.Random(seed);
            float[] founder = scenario.founderAptitude != null
                ? AptitudeCalculator.Copy(scenario.founderAptitude)
                : AptitudeCalculator.RollFounder(rng);

            var staff = new List<EmployeeInstance>();
            foreach (var hire in scenario.hires)
            {
                var e = new EmployeeInstance(System.Guid.NewGuid().ToString("N"), "Sim", hire.Specialty, hire.Seniority, PersonalityTrait.None, 0)
                {
                    mood01 = 1f, burnout01 = 0f, hired = true,
                    aptitude = AptitudeCalculator.ForEmployee(hire.Specialty, hire.Seniority, rng)
                };
                staff.Add(e);
            }

            var results = new List<DevGameResult>();
            int teamSize = 1 + staff.Count;
            int targetWeeks = loop != null ? loop.WeeksFor(scenario.size) : 8;
            float required = ProductionCalculator.RequiredPoints(scenario.size, loop);
            int worstArea = -1;

            for (int g = 1; g <= scenario.games; g++)
            {
                var r = new DevGameResult { game = g, expectedPoints = required };
                long trained = Train(scenario.training, founder, worstArea, loop);
                r.trainingCost = trained;
                r.founderAptitude = Average(founder);
                r.founderWeakest = Min(founder);

                var points = new float[ParameterCatalog.ParameterCount];
                var chosen = new float[ParameterCatalog.ParameterCount];
                float bugs = 0f;
                var workers = new List<(float[] apt, float prod)> { (founder, 1f) };
                foreach (var e in staff) workers.Add((e.aptitude, StaffCalculator.ProductivityFactor(e)));

                for (int phase = 0; phase < ParameterCatalog.PhaseCount; phase++)
                {
                    var parameters = ParameterCatalog.AvailableFor((ProductionPhase)phase, scenario.size);
                    var shares = scenario.phaseError != null
                        ? SharesWithError(genre, scenario.size, (ProductionPhase)phase, scenario.phaseError[phase])
                        : Shares(genre, scenario.size, (ProductionPhase)phase, scenario.allocation);
                    var allocation = new float[ParameterCatalog.ParameterCount];
                    for (int i = 0; i < parameters.Count; i++) allocation[(int)parameters[i]] = chosen[(int)parameters[i]] = shares[i];
                    var areaShares = QualityCalculator.AreaSharesFromParameters(allocation);
                    var techAptitudes = new List<float[]>();
                    foreach (var worker in workers) techAptitudes.Add(worker.apt);
                    float teamQuality = DevelopmentRules.TechQuality01(techAptitudes);
                    float stability = phase == (int)ProductionPhase.Technology ? allocation[(int)GameParameter.StabilityTesting] : 0f;

                    int weeks = DevelopmentRules.PhaseWeeks(targetWeeks, phase, scenario.crunch, loop);
                    r.weeks += weeks;
                    var byArea = new float[ProjectState.AreaCount];
                    for (int w = 0; w < weeks; w++)
                    {
                        float produced = 0f;
                        foreach (var worker in workers)
                        {
                            System.Array.Clear(byArea, 0, byArea.Length);
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                float pts = DevelopmentRules.WeeklyPoints(worker.apt, parameters[i], shares[i], worker.prod, teamSize, scenario.crunch, loop);
                                points[(int)parameters[i]] += pts;
                                produced += pts;
                                byArea[QualityCalculator.AreaIndexOf(parameters[i])] += pts;
                            }
                            for (int a = 0; a < byArea.Length; a++) AptitudeCalculator.Practice(worker.apt, a, byArea[a], loop);
                        }
                        bugs += DevelopmentRules.ExpectedBugs(produced, teamQuality, areaShares, stability, scenario.crunch, loop);
                    }
                }

                float total = 0f;
                foreach (float v in points) total += v;
                r.points = total;
                r.bugs = bugs;
                float worstVolume = float.MaxValue;
                for (int phase = 0; phase < ParameterCatalog.PhaseCount; phase++)
                {
                    r.phaseVolume[phase] = QualityCalculator.PhaseVolume(scenario.size, (ProductionPhase)phase, points, required);
                    if (r.phaseVolume[phase] < worstVolume) { worstVolume = r.phaseVolume[phase]; worstArea = phase; }
                }

                int themeWeight = scenario.themeWeight ?? ComboWeight(scenario.combo);
                float themeAccuracy = loop != null ? loop.ThemeAccuracy(themeWeight) : 0.5f;
                var breakdown = ScoreCalculator.Compute(genre, scenario.size, themeAccuracy, chosen, points, required,
                    EngineLevels(scenario.engine), bugs, loop);
                r.mix = breakdown.divisionAverage;
                r.phasesAccuracy = breakdown.phasesAverage;
                r.themeAccuracy = breakdown.theme;
                r.bugPenalty = breakdown.bugPenalty;
                r.score = breakdown.score;
                results.Add(r);
            }
            return results;
        }

        /// <summary>Roda o cenário várias vezes (sementes e gêneros diferentes) e devolve a média por jogo.</summary>
        public static List<DevGameResult> RunAverage(DevScenario scenario, IReadOnlyList<GenreSO> genres, int repetitions)
        {
            var sums = new List<DevGameResult>();
            int runs = 0;
            int genreCount = genres != null && genres.Count > 0 ? genres.Count : 1;
            for (int rep = 0; rep < repetitions; rep++)
                for (int g = 0; g < genreCount; g++)
                {
                    var genre = genres != null && genres.Count > 0 ? genres[g] : null;
                    var one = Run(scenario, genre, 1000 + rep);
                    for (int i = 0; i < one.Count; i++)
                    {
                        if (sums.Count <= i) sums.Add(new DevGameResult { game = one[i].game, expectedPoints = one[i].expectedPoints });
                        Add(sums[i], one[i]);
                    }
                    runs++;
                }
            foreach (var s in sums) Divide(s, runs);
            return sums;
        }

        /// <summary>
        /// Divisão da fase com um erro de 0 (perfeita, = pesos do gênero) a 1 (péssima: tudo no bloco que o
        /// gênero menos quer). Fase de bloco único continua com 100% nele (não há escolha).
        /// </summary>
        public static float[] SharesWithError(GenreSO genre, ProjectSize size, ProductionPhase phase, float error01)
        {
            var ideal = QualityCalculator.IdealPhaseShares(genre, size, phase);
            if (ideal.Length <= 1) return ideal;
            int least = 0;
            for (int i = 1; i < ideal.Length; i++) if (ideal[i] < ideal[least]) least = i;
            float t = Mathf.Clamp01(error01);
            var result = new float[ideal.Length];
            for (int i = 0; i < ideal.Length; i++) result[i] = Mathf.Lerp(ideal[i], i == least ? 1f : 0f, t);
            return result;
        }

        /// <summary>Frações (soma 1) dos itens da fase pela estratégia do cenário.</summary>
        public static float[] Shares(GenreSO genre, ProjectSize size, ProductionPhase phase, DevAllocation allocation)
        {
            var ideal = QualityCalculator.IdealPhaseShares(genre, size, phase);
            if (allocation == DevAllocation.Ideal || ideal.Length == 0) return ideal;
            var result = new float[ideal.Length];
            if (allocation == DevAllocation.Even)
            {
                for (int i = 0; i < result.Length; i++) result[i] = 1f / result.Length;
                return result;
            }
            float max = 0f, sum = 0f;
            foreach (float v in ideal) max = Mathf.Max(max, v);
            for (int i = 0; i < result.Length; i++) { result[i] = Mathf.Max(0.01f, max - ideal[i]); sum += result[i]; }
            for (int i = 0; i < result.Length; i++) result[i] /= sum;
            return result;
        }

        private static long Train(DevTraining training, float[] founder, int worstPhase, GameLoopBalanceSO loop)
        {
            if (training == DevTraining.None) return 0;
            int area = -1;
            if (training == DevTraining.WeakestEachGame || worstPhase < 0)
            {
                float min = float.MaxValue;
                for (int a = 0; a < founder.Length; a++) if (founder[a] < min) { min = founder[a]; area = a; }
            }
            else area = PhaseArea(worstPhase);
            if (area < 0 || founder[area] >= AptitudeCalculator.Max) return 0;
            long cost = AptitudeCalculator.TrainingCost(founder[area], loop);
            AptitudeCalculator.Train(founder, area, loop);
            return cost;
        }

        /// <summary>Compatibilidade gênero × tema (−3..+3) da combinação do cenário.</summary>
        public static int ComboWeight(DevCombo combo) => combo switch
        {
            DevCombo.Good => 3,
            DevCombo.Bad => -3,
            _ => 0
        };

        /// <summary>Níveis de engine por bloco para o cenário: inicial = renderizador e áudio básicos; completa = máximo em todo bloco com módulo.</summary>
        public static float[] EngineLevels(DevEngine engine)
        {
            var levels = new float[ParameterCatalog.ParameterCount];
            if (engine == DevEngine.None) return levels;
            string[] groups = engine == DevEngine.Starter
                ? new[] { "Renderização", "Áudio" }
                : new[] { "Renderização", "Áudio", "Animação", "Física", "IA", "Rede" };
            int level = engine == DevEngine.Starter ? 1 : 4;
            foreach (string group in groups)
                foreach (var parameter in GameLike.Systems.Research.ResearchCatalog.ParametersHelpedBy(group))
                    levels[(int)parameter] = level;
            return levels;
        }

        /// <summary>Área que domina cada fase (Pré-produção = Design, Arte, Áudio, Tecnologia, Conteúdo).</summary>
        public static int PhaseArea(int phase) => phase switch
        {
            0 => (int)DevArea.Design,
            1 => (int)DevArea.Art,
            2 => (int)DevArea.Audio,
            3 => (int)DevArea.Tech,
            _ => (int)DevArea.Content
        };

        private static float Average(float[] v)
        {
            float s = 0f;
            foreach (float x in v) s += x;
            return v.Length > 0 ? s / v.Length : 0f;
        }

        private static float Min(float[] v)
        {
            float m = float.MaxValue;
            foreach (float x in v) m = Mathf.Min(m, x);
            return m;
        }

        private static void Add(DevGameResult sum, DevGameResult r)
        {
            sum.weeks += r.weeks;
            sum.founderAptitude += r.founderAptitude;
            sum.founderWeakest += r.founderWeakest;
            sum.points += r.points;
            for (int i = 0; i < sum.phaseVolume.Length; i++) sum.phaseVolume[i] += r.phaseVolume[i];
            sum.phasesAccuracy += r.phasesAccuracy;
            sum.themeAccuracy += r.themeAccuracy;
            sum.bugPenalty += r.bugPenalty;
            sum.mix += r.mix;
            sum.score += r.score;
            sum.bugs += r.bugs;
            sum.trainingCost += r.trainingCost;
        }

        private static void Divide(DevGameResult sum, int n)
        {
            if (n <= 0) return;
            sum.weeks = Mathf.RoundToInt(sum.weeks / (float)n);
            sum.founderAptitude /= n;
            sum.founderWeakest /= n;
            sum.points /= n;
            for (int i = 0; i < sum.phaseVolume.Length; i++) sum.phaseVolume[i] /= n;
            sum.phasesAccuracy /= n;
            sum.themeAccuracy /= n;
            sum.bugPenalty /= n;
            sum.mix /= n;
            sum.score /= n;
            sum.bugs /= n;
            sum.trainingCost /= n;
        }
    }
}
