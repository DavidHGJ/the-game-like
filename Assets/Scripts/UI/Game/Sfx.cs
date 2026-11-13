using System.Collections.Generic;
using UnityEngine;

namespace GameLike.UI.Game
{
    /// <summary>Efeitos sonoros provisórios do jogo.</summary>
    public enum SfxId
    {
        Hover, Click, Open, Close, Point, Coin, Spend, Success, Fail, Alert, Launch, Review, Fanfare, GameOver, Unlock, Week
    }

    /// <summary>
    /// Som PROVISÓRIO: todos os efeitos são sintetizados em runtime (ondas simples), sem
    /// nenhum arquivo de áudio — só para o jogo dar retorno sonoro até a trilha/SFX
    /// definitivos existirem (GDD 16.3). Volume em <c>PlayerPrefs</c> ("sfxVolume").
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const string VolumeKey = "sfxVolume";

        private static Sfx _instance;
        private readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        private AudioSource _source;
        private float _lastHover, _lastPoint;

        /// <summary>Volume dos efeitos (0..1), persistido.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.6f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); }
        }

        /// <summary>Garante o player na cena (e um AudioListener, se a cena não tiver).</summary>
        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("[Sfx]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Sfx>();
        }

        public static void Play(SfxId id, float pitch = 1f)
        {
            if (_instance == null) Ensure();
            _instance.PlayInternal(id, pitch);
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            BuildClips();
        }

        private void PlayInternal(SfxId id, float pitch)
        {
            float now = Time.unscaledTime;
            if (id == SfxId.Hover) { if (now - _lastHover < 0.05f) return; _lastHover = now; }
            if (id == SfxId.Point) { if (now - _lastPoint < 0.07f) return; _lastPoint = now; }
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            if (!_clips.TryGetValue(id, out var clip)) return;
            _source.pitch = pitch;
            _source.PlayOneShot(clip, Volume);
        }

        // ---------------- síntese ----------------

        private void BuildClips()
        {
            _clips[SfxId.Hover] = Make("hover", Note(1400f, 1400f, 0.025f, 0.12f, 0));
            _clips[SfxId.Click] = Make("click", Note(700f, 420f, 0.06f, 0.35f, 1));
            _clips[SfxId.Open] = Make("open", Note(320f, 760f, 0.14f, 0.3f, 0));
            _clips[SfxId.Close] = Make("close", Note(700f, 300f, 0.12f, 0.28f, 0));
            _clips[SfxId.Point] = Make("point", Note(880f, 1320f, 0.07f, 0.28f, 0));
            _clips[SfxId.Coin] = Make("coin", Join(Note(988f, 988f, 0.06f, 0.3f, 1), Note(1319f, 1319f, 0.16f, 0.3f, 1)));
            _clips[SfxId.Spend] = Make("spend", Note(420f, 260f, 0.12f, 0.3f, 2));
            _clips[SfxId.Success] = Make("success", Join(Note(523f, 523f, 0.08f, 0.3f, 0), Note(659f, 659f, 0.08f, 0.3f, 0), Note(784f, 784f, 0.2f, 0.3f, 0)));
            _clips[SfxId.Fail] = Make("fail", Join(Note(300f, 240f, 0.12f, 0.3f, 2), Note(220f, 160f, 0.22f, 0.3f, 2)));
            _clips[SfxId.Alert] = Make("alert", Join(Note(880f, 880f, 0.09f, 0.3f, 1), Silence(0.04f), Note(880f, 880f, 0.14f, 0.3f, 1)));
            _clips[SfxId.Launch] = Make("launch", Join(Note(200f, 1200f, 0.5f, 0.3f, 2), Note(1047f, 1047f, 0.25f, 0.3f, 0)));
            _clips[SfxId.Review] = Make("review", Join(Note(660f, 660f, 0.05f, 0.3f, 1), Note(990f, 990f, 0.14f, 0.3f, 1)));
            _clips[SfxId.Fanfare] = Make("fanfare", Join(Note(523f, 523f, 0.11f, 0.3f, 1), Note(659f, 659f, 0.11f, 0.3f, 1),
                Note(784f, 784f, 0.11f, 0.3f, 1), Note(1047f, 1047f, 0.3f, 0.32f, 1), Note(784f, 784f, 0.1f, 0.3f, 1), Note(1047f, 1047f, 0.5f, 0.32f, 1)));
            _clips[SfxId.GameOver] = Make("gameover", Join(Note(392f, 392f, 0.2f, 0.3f, 2), Note(330f, 330f, 0.2f, 0.3f, 2),
                Note(262f, 262f, 0.25f, 0.3f, 2), Note(196f, 150f, 0.6f, 0.3f, 2)));
            _clips[SfxId.Unlock] = Make("unlock", Join(Note(784f, 784f, 0.07f, 0.3f, 0), Note(1175f, 1175f, 0.07f, 0.3f, 0), Note(1568f, 1568f, 0.22f, 0.3f, 0)));
            _clips[SfxId.Week] = Make("week", Note(520f, 520f, 0.03f, 0.08f, 0));
        }

        private static AudioClip Make(string name, float[] samples)
        {
            var clip = AudioClip.Create("sfx_" + name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Uma nota com glissando e envelope. wave: 0 seno, 1 quadrada suave, 2 serra.</summary>
        private static float[] Note(float fromHz, float toHz, float seconds, float volume, int wave)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                phase += hz / SampleRate;
                float x = phase - Mathf.Floor(phase);
                float s = wave == 0 ? Mathf.Sin(x * Mathf.PI * 2f)
                    : wave == 1 ? Mathf.Clamp(Mathf.Sin(x * Mathf.PI * 2f) * 3f, -1f, 1f) * 0.7f
                    : (x * 2f - 1f) * 0.7f;
                float attack = Mathf.Clamp01(i / (0.004f * SampleRate));
                float release = Mathf.Pow(1f - t, 1.6f);
                data[i] = s * volume * attack * release;
            }
            return data;
        }

        private static float[] Silence(float seconds) => new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];

        private static float[] Join(params float[][] parts)
        {
            int total = 0;
            foreach (var p in parts) total += p.Length;
            var data = new float[total];
            int at = 0;
            foreach (var p in parts) { System.Array.Copy(p, 0, data, at, p.Length); at += p.Length; }
            return data;
        }
    }
}
