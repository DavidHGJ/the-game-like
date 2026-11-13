using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.State;
using GameLike.State.Meta;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using GameLike.Systems.Research;
using GameLike.Systems.Staff;
using UnityEngine;

namespace GameLike.Systems.Market
{
    /// <summary>
    /// Regras puras de M&amp;A (GDD 12.2 / 12.3): preço de aquisição e probabilidade de o rival
    /// aceitar a proposta. Determinístico — coberto por testes.
    /// </summary>
    public static class AcquisitionCalculator
    {
        /// <summary>Preço = valuation do rival + bônus por IPs + bônus por funcionários (GDD 12.2).</summary>
        public static long BasePrice(RivalState rival)
        {
            if (rival == null) return 0;
            return rival.valuation + rival.gamesReleased * 15_000L + rival.employeeCount * 10_000L;
        }

        /// <summary>
        /// Chance (0..1) de o rival aceitar uma oferta de <paramref name="offerMultiple"/> vezes o
        /// valuation. Curva do GDD 12.3: 1x≈5%, 1,5x≈25%, 2x≈55%, 3x≈85%. Crise financeira e
        /// networking reduzem ~20% o múltiplo necessário; rivais menores aceitam por menos;
        /// histórico hostil penaliza.
        /// </summary>
        public static float AcceptChance(float offerMultiple, bool rivalInCrisis, CompanyPhase rivalPhase,
            float networkingEase01, float hostility01)
        {
            float needed = 1f;
            if (rivalInCrisis) needed *= 0.8f;
            needed *= 1f - Mathf.Clamp(networkingEase01, 0f, 0.2f);
            needed *= rivalPhase switch
            {
                CompanyPhase.Garage => 0.8f,
                CompanyPhase.Indie => 0.9f,
                CompanyPhase.Office => 1f,
                _ => 1.1f
            };
            float effective = offerMultiple / Mathf.Max(0.1f, needed);
            float chance = CurveAt(effective);
            chance *= 1f - 0.5f * Mathf.Clamp01(hostility01);
            return Mathf.Clamp(chance, 0f, 0.95f);
        }

        private static float CurveAt(float m)
        {
            if (m <= 1f) return Mathf.Max(0f, 0.05f * m);
            if (m <= 1.5f) return Mathf.Lerp(0.05f, 0.25f, (m - 1f) / 0.5f);
            if (m <= 2f) return Mathf.Lerp(0.25f, 0.55f, (m - 1.5f) / 0.5f);
            if (m <= 3f) return Mathf.Lerp(0.55f, 0.85f, m - 2f);
            return Mathf.Min(0.95f, 0.85f + (m - 3f) * 0.05f);
        }
    }

    /// <summary>
    /// Rivais da timeline (GDD 8.3 / 12): crescem, lançam jogos (saturando gêneros), fazem
    /// propostas para comprar o jogador e podem ser comprados. Empresas de runs anteriores
    /// do jogador reaparecem aqui como rivais (GDD 1.2).
    /// </summary>
    public sealed class RivalManager : MonoBehaviour
    {
        private static readonly string[] NpcNames = { "Garagem 42", "Lua Nova Games", "MegaByte Corp" };
        private static readonly long[] NpcValuations = { 70_000, 320_000, 2_800_000 };
        private static readonly CompanyPhase[] NpcPhases = { CompanyPhase.Garage, CompanyPhase.Indie, CompanyPhase.Office };

        private static readonly string[] GameWordsA =
            { "Reino", "Turbo", "Lenda", "Super", "Mega", "Crônicas", "Sombras", "Planeta", "Ultra", "Missão" };
        private static readonly string[] GameWordsB =
            { "de Aço", "Kart", "Perdida", "Pixel", "do Abismo", "Galáctico", "Final", "Zero", "dos Heróis", "Selvagem" };

        [SerializeField] private float gameReleaseChancePerWeek = 0.035f;
        [SerializeField] private float offerChancePerWeek = 0.04f;
        [SerializeField] private int weeksBetweenOffers = 26;
        [SerializeField] private int failedOfferCooldownWeeks = 12;
        [SerializeField] private float genreSaturationHit = 0.07f;

        private RunState _run;
        private EconomyManager _economy;
        private ResearchManager _research;
        private StaffManager _staff;
        private IReadOnlyList<string> _genreKeys;
        private System.Random _rng;
        private int _weeksSinceOffer;
        private readonly List<EmployeeInstance> _absorbPool = new List<EmployeeInstance>();

        public IReadOnlyList<RivalState> Rivals => _run?.rivals;
        public AcquisitionOffer PendingOffer => _run != null && _run.pendingOffer != null && _run.pendingOffer.active ? _run.pendingOffer : null;

        /// <summary>Funcionários do último rival comprado, aguardando o jogador escolher quem fica (GDD 12.4).</summary>
        public IReadOnlyList<EmployeeInstance> AbsorbPool => _absorbPool;

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        public void Bind(RunState run, EconomyManager economy, MetaProgressionState meta, ResearchManager research,
            StaffManager staff, IReadOnlyList<string> genreKeys)
        {
            _run = run;
            _economy = economy;
            _research = research;
            _staff = staff;
            _genreKeys = genreKeys;
            _rng = new System.Random(run != null ? run.runNumber * 15485863 + 5 : 5);
            _weeksSinceOffer = 0;
            _absorbPool.Clear();
            if (_run != null && _run.rivals.Count == 0) Seed(meta);
        }

        private void Seed(MetaProgressionState meta)
        {
            for (int i = 0; i < NpcNames.Length; i++)
            {
                _run.rivals.Add(new RivalState
                {
                    name = NpcNames[i], valuation = NpcValuations[i], phase = NpcPhases[i],
                    relation01 = 0.5f, employeeCount = 1 + i * 2, iconIndex = i
                });
            }
            if (meta?.pastCompanies == null) return;
            int from = Mathf.Max(0, meta.pastCompanies.Count - 3);
            for (int i = from; i < meta.pastCompanies.Count; i++)
            {
                var past = meta.pastCompanies[i];
                _run.rivals.Add(new RivalState
                {
                    name = past.name,
                    valuation = past.bankrupt ? 40_000 : System.Math.Max(60_000, (long)(past.finalValuation * 0.6)),
                    phase = past.bankrupt ? CompanyPhase.Garage : past.phase,
                    relation01 = 0.6f, employeeCount = 1 + (int)past.phase * 2,
                    fromPastRun = true, pastRunNumber = past.runNumber, iconIndex = 3 + i
                });
            }
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.ended) return;
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            _weeksSinceOffer++;

            for (int i = 0; i < _run.rivals.Count; i++)
            {
                var r = _run.rivals[i];
                if (r.acquired) continue;
                if (r.offerCooldownWeeks > 0) r.offerCooldownWeeks--;

                float growth = Mathf.Lerp(-0.006f, 0.014f, (float)_rng.NextDouble());
                r.valuation = System.Math.Max(20_000, (long)(r.valuation * (1.0 + growth)));
                r.inCrisis = growth < -0.004f && _rng.NextDouble() < 0.5 || r.inCrisis && _rng.NextDouble() < 0.9;
                if (economy != null) r.phase = PhaseFor(r.valuation, economy);
                r.employeeCount = 1 + (int)r.phase * 2;

                if (_rng.NextDouble() < gameReleaseChancePerWeek) ReleaseGame(r);
            }

            TryMakeOffer();
        }

        private static CompanyPhase PhaseFor(long valuation, EconomyBalanceSO economy)
        {
            if (valuation >= economy.OfficeToStudioThreshold) return CompanyPhase.Studio;
            if (valuation >= economy.IndieToOfficeThreshold) return CompanyPhase.Office;
            if (valuation >= economy.GarageToIndieThreshold) return CompanyPhase.Indie;
            return CompanyPhase.Garage;
        }

        private void ReleaseGame(RivalState r)
        {
            r.lastGameName = GameWordsA[_rng.Next(GameWordsA.Length)] + " " + GameWordsB[_rng.Next(GameWordsB.Length)];
            r.lastGameScore = Mathf.Round(Mathf.Lerp(4f, 9.5f, (float)_rng.NextDouble()) * 10f) / 10f;
            r.gamesReleased++;
            r.valuation = (long)(r.valuation * (1.0 + (r.lastGameScore - 6f) * 0.02));
            if (_genreKeys != null && _genreKeys.Count > 0 && _run.market != null)
            {
                string key = _genreKeys[_rng.Next(_genreKeys.Count)];
                _run.market.SetGenreTrend(key, _run.market.GetGenreTrend(key) - genreSaturationHit);
            }
            EventBus.Publish(new RivalGameReleasedEvent(r.name, r.lastGameName, r.lastGameScore));
        }

        private void TryMakeOffer()
        {
            if (_run.pendingOffer.active || _weeksSinceOffer < weeksBetweenOffers) return;
            var company = _run.company;
            if (company.valuation < 120_000) return;
            float chance = company.phase == CompanyPhase.Garage ? offerChancePerWeek * 0.4f : offerChancePerWeek;
            if (_rng.NextDouble() > chance) return;

            RivalState buyer = null;
            for (int i = 0; i < _run.rivals.Count; i++)
            {
                var r = _run.rivals[i];
                if (r.acquired || r.valuation < company.valuation * 1.5) continue;
                if (buyer == null || r.valuation > buyer.valuation) buyer = r;
            }
            if (buyer == null) return;

            float multiple = Mathf.Lerp(1.2f, 2.2f, (float)_rng.NextDouble()) * Mathf.Lerp(0.85f, 1.1f, buyer.relation01);
            long price = System.Math.Min((long)(company.valuation * multiple), (long)(buyer.valuation * 0.8));
            if (price <= company.valuation) return;

            _run.pendingOffer = new AcquisitionOffer
            {
                active = true, rivalId = buyer.id, rivalName = buyer.name, price = price,
                multiple = price / (float)System.Math.Max(1, company.valuation)
            };
            _weeksSinceOffer = 0;
            EventBus.Publish(new AcquisitionOfferReceivedEvent(buyer.name, price));
        }

        /// <summary>Recusa a oferta pendente: o rival fica mais hostil (GDD 12.1).</summary>
        public void DeclineOffer()
        {
            if (_run == null || !_run.pendingOffer.active) return;
            var r = Find(_run.pendingOffer.rivalId);
            if (r != null) r.relation01 = Mathf.Clamp01(r.relation01 - 0.2f);
            _run.pendingOffer = new AcquisitionOffer();
        }

        public RivalState Find(string id)
        {
            if (_run == null) return null;
            for (int i = 0; i < _run.rivals.Count; i++)
                if (_run.rivals[i].id == id) return _run.rivals[i];
            return null;
        }

        // ------------------------------------------------------------------ comprar um rival

        /// <summary>M&amp;A exige a pesquisa e fase Escritório (rivais pequenos) ou Estúdio (grandes) — GDD 12.1.</summary>
        public bool CanTarget(RivalState rival)
        {
            if (_run == null || rival == null || rival.acquired || rival.offerCooldownWeeks > 0) return false;
            if (_research != null && !_research.IsUnlocked(ResearchCatalog.FeatureAcquisition)) return false;
            if ((int)_run.company.phase < (int)CompanyPhase.Office) return false;
            return _run.company.phase == CompanyPhase.Studio || (int)rival.phase <= (int)CompanyPhase.Indie;
        }

        public long PriceFor(RivalState rival, float multiple) =>
            (long)(AcquisitionCalculator.BasePrice(rival) * Mathf.Max(1f, multiple));

        public float AcceptChance(RivalState rival, float multiple) =>
            rival == null ? 0f : AcquisitionCalculator.AcceptChance(multiple, rival.inCrisis, rival.phase,
                FounderPerks.AcquisitionEase, 1f - Mathf.Clamp01(rival.relation01 * 2f));

        /// <summary>
        /// Envia a proposta. Se o rival aceitar: paga, absorve os IPs (entram no valuation) e
        /// abre o grupo de funcionários para o jogador escolher (GDD 12.4). Se recusar: cooldown.
        /// </summary>
        public bool TryAcquire(RivalState rival, float multiple)
        {
            if (!CanTarget(rival)) return false;
            long price = PriceFor(rival, multiple);
            if (_run.company.cash < price) return false;

            if (_rng.NextDouble() > AcceptChance(rival, multiple))
            {
                rival.offerCooldownWeeks = failedOfferCooldownWeeks;
                rival.relation01 = Mathf.Clamp01(rival.relation01 - 0.05f);
                return false;
            }

            _economy?.ApplyDelta(-price, $"Aquisição de {rival.name}");
            rival.acquired = true;
            _run.company.acquiredIpValue += (long)(rival.valuation * 0.5) + rival.gamesReleased * 15_000L;
            _run.company.acquiredGames += rival.gamesReleased;

            _absorbPool.Clear();
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            for (int i = 0; i < rival.employeeCount; i++)
                _absorbPool.Add(CandidateGenerator.Create(_rng, s => SalaryFor(s, economy), 0.2f));
            EventBus.Publish(new RivalAcquiredEvent(rival.name, price));
            return true;
        }

        /// <summary>Taxa de demissão de um funcionário absorvido que não vai ficar (GDD 12.4).</summary>
        public long DismissalFee(EmployeeInstance e) => e != null ? e.weeklySalary * 2 : 0;

        /// <summary>Conclui a integração: contrata os escolhidos (até o limite da fase) e paga as taxas dos demais.</summary>
        public void FinishAbsorption(IList<EmployeeInstance> keep)
        {
            if (_run == null) return;
            for (int i = 0; i < _absorbPool.Count; i++)
            {
                var e = _absorbPool[i];
                bool kept = keep != null && keep.Contains(e) && _staff != null && _staff.HireDirect(e);
                if (!kept) _economy?.ApplyDelta(-DismissalFee(e), $"Demissão de {e.name}");
            }
            _absorbPool.Clear();
        }

        private static long SalaryFor(Seniority s, EconomyBalanceSO economy)
        {
            if (economy == null)
                return s switch { Seniority.Junior => 2000, Seniority.Mid => 3500, Seniority.Senior => 6000, _ => 10000 };
            return s switch
            {
                Seniority.Junior => economy.JuniorWeeklySalary,
                Seniority.Mid => economy.MidWeeklySalary,
                Seniority.Senior => economy.SeniorWeeklySalary,
                _ => economy.StarWeeklySalary
            };
        }
    }
}
