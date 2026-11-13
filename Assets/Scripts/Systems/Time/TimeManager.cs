using GameLike.Core.Events;
using UnityEngine;

namespace GameLike.Systems.Time
{
    /// <summary>
    /// Dono do relógio do jogo (GDD 17): tempo real pausável, com velocidade ajustável.
    /// A cada semana anuncia <see cref="WeekTickEvent"/> — não sabe quem reage.
    ///
    /// Há dois tipos de pausa: a do jogador (botão de pausa) e as travas de tela
    /// (<see cref="AddLock"/>/<see cref="RemoveLock"/>): toda tela de decisão trava o tempo e,
    /// ao fechar, o relógio volta ao estado em que o jogador o deixou (adendo 23.10).
    /// </summary>
    public sealed class TimeManager : MonoBehaviour
    {
        [Header("Tempo")]
        /// <summary>Segundos reais por semana na velocidade 1x — lido do balanceamento a cada uso (GDD 23.13).</summary>
        private static float secondsPerWeekAtBaseSpeed
        {
            get
            {
                var tuning = GameLike.Data.Balance.BalanceProvider.Get<GameLike.Data.Balance.GameTuningBalanceSO>();
                return tuning != null ? tuning.SecondsPerWeek : 5f;
            }
        }

        [Tooltip("Multiplicadores de velocidade selecionáveis pelo jogador.")]
        [SerializeField] private float[] speedMultipliers = { 1f, 2f, 4f };

        private int _currentWeek = 1;
        private int _currentYear = 1;
        private int _speedIndex;
        private bool _paused;
        private int _locks;
        private float _accumulator;

        private const int WeeksPerYear = 52;

        public int CurrentWeek => _currentWeek;
        public int CurrentYear => _currentYear;

        /// <summary>Verdadeiro se o relógio está parado (pausa do jogador ou tela aberta).</summary>
        public bool IsPaused => _paused || _locks > 0;

        /// <summary>Verdadeiro se o JOGADOR pausou (independente de telas abertas).</summary>
        public bool IsUserPaused => _paused;

        public float CurrentSpeed => speedMultipliers[_speedIndex];
        public int SpeedIndex => _speedIndex;
        public int SpeedCount => speedMultipliers.Length;

        /// <summary>Fração (0..1) da semana atual já decorrida — para o relógio do HUD.</summary>
        public float WeekProgress01 => Mathf.Clamp01(_accumulator / secondsPerWeekAtBaseSpeed);

        /// <summary>Segundos reais que uma semana leva na velocidade atual.</summary>
        public float SecondsPerWeek => secondsPerWeekAtBaseSpeed / Mathf.Max(0.01f, CurrentSpeed);

        private void Update()
        {
            if (IsPaused) return;

            _accumulator += UnityEngine.Time.deltaTime * CurrentSpeed;
            while (_accumulator >= secondsPerWeekAtBaseSpeed && !IsPaused)
            {
                _accumulator -= secondsPerWeekAtBaseSpeed;
                AdvanceWeek();
            }
        }

        private void AdvanceWeek()
        {
            _currentWeek++;
            if (_currentWeek > WeeksPerYear)
            {
                _currentWeek = 1;
                _currentYear++;
            }
            EventBus.Publish(new WeekTickEvent(_currentWeek, _currentYear));
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void TogglePause() => _paused = !_paused;

        /// <summary>Trava o relógio enquanto uma tela de decisão está aberta.</summary>
        public void AddLock() => _locks++;

        /// <summary>Libera uma trava de tela.</summary>
        public void RemoveLock() => _locks = Mathf.Max(0, _locks - 1);

        /// <summary>Remove todas as travas (ao trocar de run).</summary>
        public void ClearLocks() => _locks = 0;

        public void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % speedMultipliers.Length;
        }

        public void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= speedMultipliers.Length) return;
            _speedIndex = index;
        }

        /// <summary>Reposiciona o calendário (nova run ou save carregado).</summary>
        public void SetDate(int week, int year)
        {
            _currentWeek = Mathf.Clamp(week, 1, WeeksPerYear);
            _currentYear = Mathf.Max(1, year);
            _accumulator = 0f;
        }
    }
}
