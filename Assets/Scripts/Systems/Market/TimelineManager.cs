using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using UnityEngine;

namespace GameLike.Systems.Market
{
    /// <summary>
    /// Linha do tempo da indústria (GDD 15.2): anuncia consoles quando chegam ao mercado e
    /// dispara os eventos globais (crash do ano 11, renascimento, booms), que mexem no
    /// multiplicador global de vendas por um período.
    /// </summary>
    public sealed class TimelineManager : MonoBehaviour
    {
        private const int WeeksPerYear = 52;

        private RunState _run;
        private IReadOnlyList<ConsoleSO> _consoles;

        private void OnEnable() => EventBus.Subscribe<WeekTickEvent>(OnWeekTick);
        private void OnDisable() => EventBus.Unsubscribe<WeekTickEvent>(OnWeekTick);

        public void Bind(RunState run, IReadOnlyList<ConsoleSO> consoles)
        {
            _run = run;
            _consoles = consoles;
        }

        private void OnWeekTick(WeekTickEvent e)
        {
            if (_run == null || _run.ended || _run.market == null) return;
            var market = _run.market;

            if (market.globalEventWeeksLeft > 0 && --market.globalEventWeeksLeft == 0)
                market.globalSalesMultiplier = 1f;

            if (e.Week != 1) return;
            AnnounceConsoles(e.Year);
            TriggerGlobalEvent(e.Year, market);
        }

        private void AnnounceConsoles(int year)
        {
            if (_consoles == null) return;
            for (int i = 0; i < _consoles.Count; i++)
            {
                var c = _consoles[i];
                if (c != null && c.ReleaseYear == year)
                    EventBus.Publish(new ConsoleReleasedEvent(c.DisplayLabel, c.LicenseCost, year));
            }
        }

        private static void TriggerGlobalEvent(int year, MarketState market)
        {
            switch (year)
            {
                case 11:
                    market.globalSalesMultiplier = 0.5f;
                    market.globalEventWeeksLeft = 2 * WeeksPerYear;
                    EventBus.Publish(new GlobalMarketEvent("Crash do mercado de games!",
                        "As vendas globais caem pela metade por 2 anos. Consoles antigos perdem popularidade e jogos ruins são punidos sem dó.", false));
                    break;
                case 13:
                    market.globalSalesMultiplier = 1f;
                    market.globalEventWeeksLeft = 0;
                    EventBus.Publish(new GlobalMarketEvent("O renascimento!",
                        "Um novo console salvou a indústria. A confiança do mercado voltou e as vendas se normalizaram.", true));
                    break;
                case 31:
                    market.globalSalesMultiplier = 1.15f;
                    market.globalEventWeeksLeft = WeeksPerYear;
                    EventBus.Publish(new GlobalMarketEvent("Boom digital!",
                        "A distribuição digital explodiu no PC. Jogos independentes nunca venderam tanto.", true));
                    break;
                case 45:
                    market.globalSalesMultiplier = 1.2f;
                    market.globalEventWeeksLeft = WeeksPerYear;
                    EventBus.Publish(new GlobalMarketEvent("Boom mobile!",
                        "Todo mundo tem um videogame no bolso. O mobile virou o maior mercado individual.", true));
                    break;
            }
        }
    }
}
