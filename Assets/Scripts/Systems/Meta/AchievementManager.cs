using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.State;
using GameLike.State.Meta;
using UnityEngine;

namespace GameLike.Systems.Meta
{
    /// <summary>
    /// Conquistas (GDD 20.5): observa os fatos do jogo e desbloqueia os marcos, guardando-os
    /// na meta-progressão. Sem recompensa interna por enquanto. A integração com Steam entra
    /// depois — aqui só nasce o fato <see cref="AchievementUnlockedEvent"/>.
    /// </summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        private static readonly Dictionary<string, (string title, string description)> Defs =
            new Dictionary<string, (string, string)>
            {
                { "first_game", ("Primeira empresa", "Lance seu primeiro jogo") },
                { "phase_indie", ("Saindo da Garagem", "Alcance a fase Indie pela primeira vez") },
                { "phase_office", ("Profissional", "Alcance a fase Escritório pela primeira vez") },
                { "phase_studio", ("Grande Estúdio", "Alcance a fase Estúdio pela primeira vez") },
                { "first_sale", ("Primeira Venda", "Venda sua empresa pela primeira vez") },
                { "sale_1m", ("Negócio Lucrativo", "Venda uma empresa por mais de 1 milhão") },
                { "sale_10m", ("Magnata", "Venda uma empresa por mais de 10 milhões") },
                { "sale_50m", ("Lendário", "Venda uma empresa por mais de 50 milhões") },
                { "score_9", ("Crítico de Plantão", "Lance um jogo com nota 9 ou mais") },
                { "franchise_5", ("Franquia de Ouro", "Construa uma franquia com 5 jogos") },
                { "genres_5", ("Diversificado", "Lance jogos em 5 gêneros diferentes em uma run") },
                { "ipo", ("IPO", "Abra o capital da empresa") },
                { "acquisition", ("Aquisição", "Adquira um rival pela primeira vez") },
                { "platforms_5", ("Colecionador de Plataformas", "Lance jogos em 5 plataformas diferentes") },
                { "no_crisis", ("Sem Crise", "Complete uma run sem nenhuma crise grave") },
                { "runs_10", ("Veterano", "Complete 10 runs") },
                { "runs_50", ("Lenda Viva", "Complete 50 runs") },
                { "second_run", ("Máquina do Tempo", "Inicie sua segunda run") }
            };

        private RunState _run;
        private MetaProgressionState _meta;

        private void OnEnable()
        {
            EventBus.Subscribe<GameLaunchedEvent>(OnLaunched);
            EventBus.Subscribe<PhaseChangedEvent>(OnPhase);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Subscribe<RivalAcquiredEvent>(OnRivalAcquired);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLaunchedEvent>(OnLaunched);
            EventBus.Unsubscribe<PhaseChangedEvent>(OnPhase);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Unsubscribe<RivalAcquiredEvent>(OnRivalAcquired);
        }

        public void Bind(RunState run, MetaProgressionState meta)
        {
            _run = run;
            _meta = meta;
            if (_run != null && _run.runNumber >= 2) Unlock("second_run");
        }

        /// <summary>Título e descrição de uma conquista (para a UI listar).</summary>
        public static bool TryGetInfo(string id, out string title, out string description)
        {
            if (Defs.TryGetValue(id, out var d)) { title = d.title; description = d.description; return true; }
            title = description = null;
            return false;
        }

        public static IEnumerable<string> AllIds => Defs.Keys;

        private void Unlock(string id)
        {
            if (_meta == null || !Defs.TryGetValue(id, out var d)) return;
            _meta.EnsureCollections();
            if (_meta.achievements.Contains(id)) return;
            _meta.achievements.Add(id);
            EventBus.Publish(new AchievementUnlockedEvent(id, d.title, d.description));
        }

        private void OnLaunched(GameLaunchedEvent e)
        {
            if (_run == null) return;
            Unlock("first_game");
            if (e.Score >= 9f) Unlock("score_9");
            if (_run.company.genresReleased.Count >= 5) Unlock("genres_5");

            var platforms = new HashSet<string>();
            var franchises = new Dictionary<string, int>();
            for (int i = 0; i < _run.releasedProjects.Count; i++)
            {
                var p = _run.releasedProjects[i];
                if (!string.IsNullOrEmpty(p.consoleId)) platforms.Add(p.consoleId);
                if (string.IsNullOrEmpty(p.franchiseId)) continue;
                franchises.TryGetValue(p.franchiseId, out int n);
                franchises[p.franchiseId] = n + 1;
                if (n + 1 >= 5) Unlock("franchise_5");
            }
            if (platforms.Count >= 5) Unlock("platforms_5");
        }

        private void OnPhase(PhaseChangedEvent e)
        {
            if (e.To == CompanyPhase.Indie) Unlock("phase_indie");
            else if (e.To == CompanyPhase.Office) Unlock("phase_office");
            else if (e.To == CompanyPhase.Studio) Unlock("phase_studio");
        }

        private void OnRunEnded(RunEndedEvent e)
        {
            if (_meta == null) return;
            if (e.Reason != RunEndReason.Bankruptcy)
            {
                Unlock("first_sale");
                if (e.FinalValue > 1_000_000) Unlock("sale_1m");
                if (e.FinalValue > 10_000_000) Unlock("sale_10m");
                if (e.FinalValue > 50_000_000) Unlock("sale_50m");
                if (e.Reason == RunEndReason.IPO) Unlock("ipo");
                if (_run != null && !_run.hadMajorCrisis) Unlock("no_crisis");
            }
            if (_meta.runsCompleted >= 10) Unlock("runs_10");
            if (_meta.runsCompleted >= 50) Unlock("runs_50");
        }

        private void OnRivalAcquired(RivalAcquiredEvent e) => Unlock("acquisition");
    }
}
