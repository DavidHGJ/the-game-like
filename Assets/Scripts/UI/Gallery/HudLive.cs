using System;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Retrato do jogo que o <see cref="AnimatedHud"/> desenha quando está ligado à run real.
    /// Quem preenche é a camada de jogo (um binder lê o estado a cada frame); o HUD só exibe.
    /// Sem um <c>HudLive</c>, o HUD roda sua simulação de vitrine (galeria).
    /// </summary>
    public sealed class HudLive
    {
        public const int ParameterCount = 19;

        public string companyLine = "";
        public long cash;
        public long valuation;
        public bool valuationRising = true;
        public int week = 1, year = 1;
        public float weekProgress01;
        public bool paused;
        public int speedIndex;
        public int team = 1;
        public int bugs;
        public int research;

        public bool hasProject;
        public string title = "";
        public string subtitle = "";
        public string progressCaption = "";
        public string progressLabel = "";
        public int phaseIndex;
        public float phaseProgress01;
        /// <summary>Verdadeiro quando o projeto pede uma ação do jogador (alocar fase / lançar).</summary>
        public bool needsAttention;
        public float hype100;
        /// <summary>Pontos entregues por parâmetro (ordem do GDD 5.2).</summary>
        public readonly float[] paramPoints = new float[ParameterCount];
        /// <summary>Parâmetros que existem no escopo do projeto atual.</summary>
        public readonly bool[] paramVisible = new bool[ParameterCount];
        /// <summary>Segundos reais que uma semana leva agora (ritmo do enchimento dos círculos).</summary>
        public float secondsPerWeek = 5f;
        /// <summary>Unidades vendidas por semana do jogo em destaque (uma barra por semana).</summary>
        public readonly System.Collections.Generic.List<float> salesBars = new System.Collections.Generic.List<float>();
        public string salesTitle = "";
        public string salesCaption = "";
        /// <summary>Jogo lançado esperando as críticas (as vendas começam depois delas).</summary>
        public bool salesAwaitingReview;

        /// <summary>Texto do selo de alerta sobre a barra de comando (vazio = sem alerta).</summary>
        public string alert = "";
    }

    /// <summary>Intenções que o HUD dispara (cliques). A camada de jogo assina o que precisar.</summary>
    public sealed class HudActions
    {
        public Action Pause, Play, Fast, NewProject, Project, Research, Money, Menu;
        /// <summary>Abre um painel de gestão: 0 Financeiro, 1 Projetos, 2 RH, 3 Mercado, 4 P&amp;D, 5 Relações, 6 Fundador.</summary>
        public Action<int> Panel;
    }
}
