using GameLike.Core.Events;
using UnityEngine;

namespace GameLike.Systems.Time
{
    /// <summary>
    /// Drives the game clock. Reference implementation of the "manager" pattern:
    /// owns a domain (time), holds coherent state (current week/year, speed),
    /// and announces facts via the EventBus instead of calling other systems.
    ///
    /// Real-time pausable model (GDD section 17). Speed and pause are controlled
    /// by the player; every elapsed in-game week publishes a WeekTickEvent that
    /// the rest of the game reacts to.
    /// </summary>
    public sealed class TimeManager : MonoBehaviour
    {
        [Header("Tempo")]
        [Tooltip("Segundos reais por semana de jogo na velocidade 1x.")]
        [SerializeField] private float secondsPerWeekAtBaseSpeed = 5f;

        [Tooltip("Multiplicadores de velocidade selecionáveis pelo jogador.")]
        [SerializeField] private float[] speedMultipliers = { 1f, 2f, 4f };

        private int _currentWeek = 1;
        private int _currentYear = 1;
        private int _speedIndex;
        private bool _paused;
        private float _accumulator;

        public int CurrentWeek => _currentWeek;
        public int CurrentYear => _currentYear;
        public bool IsPaused => _paused;
        public float CurrentSpeed => speedMultipliers[_speedIndex];

        private const int WeeksPerYear = 52;

        private void Update()
        {
            if (_paused) return;

            _accumulator += UnityEngine.Time.deltaTime * CurrentSpeed;

            while (_accumulator >= secondsPerWeekAtBaseSpeed)
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

            // Announce the fact. TimeManager does not know or care who reacts.
            EventBus.Publish(new WeekTickEvent(_currentWeek, _currentYear));
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void TogglePause() => _paused = !_paused;

        public void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % speedMultipliers.Length;
        }

        public void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= speedMultipliers.Length) return;
            _speedIndex = index;
        }
    }
}
