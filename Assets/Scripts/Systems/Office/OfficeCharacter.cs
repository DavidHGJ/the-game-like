using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>Aparência de um boneco do escritório (índices nas paletas de <see cref="OfficeCharacter"/>).</summary>
    public struct CharacterLook
    {
        public int skin, hair, hairStyle, shirt, accessory;

        /// <summary>Aparência variada e estável para o n-ésimo funcionário.</summary>
        public static CharacterLook ForIndex(int index)
        {
            int n = Mathf.Abs(index * 7919 + 13);
            return new CharacterLook { skin = n % 4, hair = (n / 3) % 5, hairStyle = (n / 5) % 4, shirt = (n / 7) % 6, accessory = (n / 11) % 3 };
        }
    }

    /// <summary>
    /// Boneco sentado do escritório: corpo arredondado montado por código (cabeça, tronco,
    /// braços e pernas articulados, cabelo e acessório) e animado por procedimento — digita,
    /// usa o mouse, para para pensar, olha em volta, se espreguiça, cochila. A atividade padrão
    /// vem do assento (<see cref="SeatActivity"/>); de tempos em tempos ele faz uma pausa
    /// diferente e volta. A origem do boneco é o quadril, apoiado no assento; a frente é +Z.
    /// </summary>
    public sealed class OfficeCharacter : MonoBehaviour
    {
        // Mesmas paletas do retrato 2D do fundador (FounderArt), para o boneco 3D bater com ele.
        private static readonly Color[] Skins =
        {
            new Color32(244, 204, 170, 255), new Color32(222, 170, 130, 255), new Color32(176, 122, 88, 255), new Color32(120, 80, 58, 255)
        };
        private static readonly Color[] Hairs =
        {
            new Color32(60, 44, 36, 255), new Color32(150, 92, 48, 255), new Color32(228, 190, 96, 255), new Color32(200, 80, 70, 255), new Color32(120, 120, 130, 255)
        };
        private static readonly Color[] Shirts =
        {
            new Color32(42, 157, 143, 255), new Color32(47, 116, 201, 255), new Color32(214, 86, 143, 255),
            new Color32(224, 145, 74, 255), new Color32(139, 109, 214, 255), new Color32(76, 158, 86, 255)
        };
        private static readonly Color Pants = new Color32(58, 66, 86, 255);
        private static readonly Color Shoes = new Color32(40, 38, 40, 255);
        private static readonly Color Dark = new Color32(28, 28, 32, 255);

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        /// <summary>Ângulos (graus) das articulações numa pose.</summary>
        private struct Pose
        {
            public float lean, headPitch, headYaw, headTilt;
            public float shoulderL, spreadL, elbowL, shoulderR, spreadR, elbowR;

            public static Pose Lerp(Pose a, Pose b, float t) => new Pose
            {
                lean = Mathf.Lerp(a.lean, b.lean, t), headPitch = Mathf.Lerp(a.headPitch, b.headPitch, t),
                headYaw = Mathf.Lerp(a.headYaw, b.headYaw, t), headTilt = Mathf.Lerp(a.headTilt, b.headTilt, t),
                shoulderL = Mathf.Lerp(a.shoulderL, b.shoulderL, t), spreadL = Mathf.Lerp(a.spreadL, b.spreadL, t),
                elbowL = Mathf.Lerp(a.elbowL, b.elbowL, t), shoulderR = Mathf.Lerp(a.shoulderR, b.shoulderR, t),
                spreadR = Mathf.Lerp(a.spreadR, b.spreadR, t), elbowR = Mathf.Lerp(a.elbowR, b.elbowR, t)
            };
        }

        private enum Act { Typing, Mouse, Thinking, LookAround, Stretch, Relaxed, Sleeping }

        private static readonly Pose TypingPose = new Pose { lean = 6f, headPitch = 9f, shoulderL = -20f, spreadL = 7f, elbowL = -78f, shoulderR = -20f, spreadR = 7f, elbowR = -78f };
        private static readonly Pose MousePose = new Pose { lean = 5f, headPitch = 7f, shoulderL = -20f, spreadL = 7f, elbowL = -78f, shoulderR = -26f, spreadR = -14f, elbowR = -70f };
        private static readonly Pose ThinkingPose = new Pose { lean = -5f, headPitch = -7f, headTilt = 9f, shoulderL = -12f, spreadL = 4f, elbowL = -62f, shoulderR = -38f, spreadR = 16f, elbowR = -128f };
        private static readonly Pose LookPose = new Pose { lean = -3f, headPitch = -2f, shoulderL = -10f, spreadL = 4f, elbowL = -58f, shoulderR = -10f, spreadR = 4f, elbowR = -58f };
        private static readonly Pose StretchPose = new Pose { lean = -12f, headPitch = -16f, shoulderL = -168f, spreadL = -14f, elbowL = -22f, shoulderR = -168f, spreadR = -14f, elbowR = -22f };
        private static readonly Pose RelaxedPose = new Pose { lean = -13f, headPitch = -3f, shoulderL = -8f, spreadL = 5f, elbowL = -52f, shoulderR = -8f, spreadR = 5f, elbowR = -52f };
        private static readonly Pose SleepingPose = new Pose { lean = 13f, headPitch = 38f, headTilt = 8f, shoulderL = -10f, spreadL = 5f, elbowL = -56f, shoulderR = -10f, spreadR = 5f, elbowR = -56f };

        private Transform _spine, _neck, _shoulderL, _shoulderR, _elbowL, _elbowR;
        private SeatActivity _activity;
        private Act _act, _previousAct;
        private Pose _from;
        private float _blend = 1f, _actTime, _actDuration, _clock, _seed;

        /// <summary>Cria um boneco sob <paramref name="parent"/> (origem no quadril).</summary>
        public static OfficeCharacter Create(Transform parent, string name, CharacterLook look, SeatActivity activity, int seed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var character = go.AddComponent<OfficeCharacter>();
            character.Build(look);
            character._activity = activity;
            character._seed = seed * 3.71f + 0.37f;
            character._clock = character._seed;
            character.Begin(character.DefaultAct(), true);
            character.Apply(Current(character._act), 0f);
            return character;
        }

        // ------------------------------------------------------------------ corpo

        private void Build(CharacterLook look)
        {
            Color skin = Skins[Mathf.Abs(look.skin) % Skins.Length];
            Color hair = Hairs[Mathf.Abs(look.hair) % Hairs.Length];
            Color shirt = Shirts[Mathf.Abs(look.shirt) % Shirts.Length];

            Part(PrimitiveType.Sphere, "Pelvis", transform, new Vector3(0f, 0.07f, 0f), new Vector3(0.32f, 0.20f, 0.26f), Pants);

            _spine = Joint("Spine", transform, new Vector3(0f, 0.08f, 0f));
            Part(PrimitiveType.Capsule, "Torso", _spine, new Vector3(0f, 0.20f, 0f), new Vector3(0.31f, 0.225f, 0.23f), shirt);

            _neck = Joint("Neck", _spine, new Vector3(0f, 0.40f, 0f));
            Part(PrimitiveType.Capsule, "NeckSkin", _neck, new Vector3(0f, 0.02f, 0f), new Vector3(0.10f, 0.05f, 0.10f), skin);
            Part(PrimitiveType.Sphere, "Head", _neck, new Vector3(0f, 0.19f, 0.01f), new Vector3(0.30f, 0.31f, 0.30f), skin);
            Part(PrimitiveType.Sphere, "EyeL", _neck, new Vector3(-0.055f, 0.20f, 0.146f), Vector3.one * 0.034f, Dark);
            Part(PrimitiveType.Sphere, "EyeR", _neck, new Vector3(0.055f, 0.20f, 0.146f), Vector3.one * 0.034f, Dark);
            BuildHair(look.hairStyle, hair);
            BuildAccessory(look.accessory);

            _shoulderL = Joint("ShoulderL", _spine, new Vector3(-0.185f, 0.35f, 0f));
            _shoulderR = Joint("ShoulderR", _spine, new Vector3(0.185f, 0.35f, 0f));
            _elbowL = Arm(_shoulderL, shirt, skin);
            _elbowR = Arm(_shoulderR, shirt, skin);

            Leg(-0.095f);
            Leg(0.095f);
        }

        private Transform Arm(Transform shoulder, Color sleeve, Color skin)
        {
            Part(PrimitiveType.Sphere, "ShoulderCap", shoulder, Vector3.zero, Vector3.one * 0.12f, sleeve);
            Part(PrimitiveType.Capsule, "UpperArm", shoulder, new Vector3(0f, -0.12f, 0f), new Vector3(0.105f, 0.13f, 0.105f), sleeve);
            var elbow = Joint("Elbow", shoulder, new Vector3(0f, -0.24f, 0f));
            Part(PrimitiveType.Capsule, "Forearm", elbow, new Vector3(0f, -0.11f, 0f), new Vector3(0.088f, 0.12f, 0.088f), skin);
            Part(PrimitiveType.Sphere, "Hand", elbow, new Vector3(0f, -0.235f, 0f), new Vector3(0.095f, 0.10f, 0.095f), skin);
            return elbow;
        }

        private void Leg(float side)
        {
            var hip = Joint("Hip", transform, new Vector3(side, 0.04f, 0.04f));
            hip.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.18f, 0f), new Vector3(0.15f, 0.20f, 0.15f), Pants);
            var knee = Joint("Knee", hip, new Vector3(0f, -0.37f, 0f));
            knee.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.18f, 0f), new Vector3(0.12f, 0.19f, 0.12f), Pants);
            Part(PrimitiveType.Sphere, "Shoe", knee, new Vector3(0f, -0.385f, 0.05f), new Vector3(0.12f, 0.09f, 0.23f), Shoes);
        }

        private void BuildHair(int style, Color hair)
        {
            Part(PrimitiveType.Sphere, "Hair", _neck, new Vector3(0f, 0.262f, -0.04f), new Vector3(0.315f, 0.215f, 0.31f), hair);
            switch (Mathf.Abs(style) % 4)
            {
                case 1: // topete
                    Part(PrimitiveType.Sphere, "HairTuft", _neck, new Vector3(0.02f, 0.33f, 0.07f), new Vector3(0.20f, 0.11f, 0.17f), hair);
                    break;
                case 2: // comprido
                    Part(PrimitiveType.Sphere, "HairBack", _neck, new Vector3(0f, 0.10f, -0.085f), new Vector3(0.31f, 0.34f, 0.20f), hair);
                    break;
                case 3: // coque
                    Part(PrimitiveType.Sphere, "HairBun", _neck, new Vector3(0f, 0.385f, -0.05f), Vector3.one * 0.13f, hair);
                    break;
            }
        }

        private void BuildAccessory(int accessory)
        {
            switch (Mathf.Abs(accessory) % 3)
            {
                case 1: // óculos
                    Part(PrimitiveType.Cube, "Glasses", _neck, new Vector3(0f, 0.20f, 0.155f), new Vector3(0.22f, 0.045f, 0.012f), Dark);
                    break;
                case 2: // fone de ouvido
                    Part(PrimitiveType.Cube, "HeadphoneBand", _neck, new Vector3(0f, 0.355f, 0f), new Vector3(0.33f, 0.03f, 0.05f), Dark);
                    Part(PrimitiveType.Sphere, "HeadphoneL", _neck, new Vector3(-0.158f, 0.20f, 0f), new Vector3(0.06f, 0.13f, 0.13f), Dark);
                    Part(PrimitiveType.Sphere, "HeadphoneR", _neck, new Vector3(0.158f, 0.20f, 0f), new Vector3(0.06f, 0.13f, 0.13f), Dark);
                    break;
            }
        }

        private static Transform Joint(string name, Transform parent, Vector3 position)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        private static void Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(color);
        }

        private static Material MaterialFor(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null) return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { hideFlags = HideFlags.DontSave };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.28f);
            Materials[color] = material;
            return material;
        }

        // ------------------------------------------------------------------ animação

        private Act DefaultAct()
        {
            switch (_activity)
            {
                case SeatActivity.Thinking: return Act.Thinking;
                case SeatActivity.Relaxed: return Act.Relaxed;
                case SeatActivity.Sleeping: return Act.Sleeping;
                default: return Act.Typing;
            }
        }

        private static Pose Current(Act act)
        {
            switch (act)
            {
                case Act.Mouse: return MousePose;
                case Act.Thinking: return ThinkingPose;
                case Act.LookAround: return LookPose;
                case Act.Stretch: return StretchPose;
                case Act.Relaxed: return RelaxedPose;
                case Act.Sleeping: return SleepingPose;
                default: return TypingPose;
            }
        }

        private void Begin(Act act, bool instant)
        {
            _from = instant ? Current(act) : Pose.Lerp(_from, Current(_act), Smooth(_blend));
            _previousAct = _act;
            _act = act;
            _blend = instant ? 1f : 0f;
            _actTime = 0f;
            bool isDefault = act == DefaultAct();
            _actDuration = isDefault ? Random.Range(6f, 13f) : act == Act.Mouse ? Random.Range(2.5f, 5f) : Random.Range(2.8f, 4.6f);
        }

        /// <summary>Escolhe a próxima ação: volta ao padrão do assento ou faz uma pausa.</summary>
        private Act Next()
        {
            Act home = DefaultAct();
            if (_act != home) return home;
            float roll = Random.value;
            switch (_activity)
            {
                case SeatActivity.Sleeping: return Act.Sleeping;
                case SeatActivity.Relaxed: return roll < 0.6f ? Act.LookAround : roll < 0.8f ? Act.Stretch : Act.Thinking;
                case SeatActivity.Thinking: return roll < 0.55f ? Act.Typing : roll < 0.85f ? Act.LookAround : Act.Stretch;
                default: return roll < 0.45f ? Act.Mouse : roll < 0.75f ? Act.Thinking : roll < 0.9f ? Act.LookAround : Act.Stretch;
            }
        }

        private void Update()
        {
            float dt = UnityEngine.Time.unscaledDeltaTime;
            _clock += dt;
            _actTime += dt;
            _blend = Mathf.Min(1f, _blend + dt / 0.55f);
            if (_actTime >= _actDuration) Begin(Next(), false);
            Apply(Pose.Lerp(_from, Current(_act), Smooth(_blend)), Smooth(_blend));
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private void Apply(Pose pose, float weight)
        {
            float t = _clock;
            float breathe = Mathf.Sin(t * (_act == Act.Sleeping ? 0.9f : 1.7f)) * (_act == Act.Sleeping ? 2.2f : 0.9f);

            // movimento próprio de cada ação, somado à pose (entra junto com a transição)
            float typeL = 0f, typeR = 0f, headYaw = 0f, headPitch = 0f, spreadR = 0f, shoulderR = 0f;
            switch (_act)
            {
                case Act.Typing:
                    // rajadas de digitação: as mãos alternam rápido, com pausas curtas
                    float burst = Mathf.Clamp01(Mathf.Sin(t * 0.9f + _seed) * 2f + 0.8f);
                    typeL = Mathf.Sin(t * 19f + _seed) * 5.5f * burst;
                    typeR = Mathf.Sin(t * 23f + _seed * 2f + 1.3f) * 5.5f * burst;
                    headPitch = Mathf.Sin(t * 0.7f + _seed) * 2.5f;
                    headYaw = Mathf.Sin(t * 0.45f + _seed) * 4f;
                    break;
                case Act.Mouse:
                    spreadR = Mathf.Sin(t * 2.3f + _seed) * 4f;
                    shoulderR = Mathf.Cos(t * 1.9f + _seed) * 3f;
                    typeR = Mathf.Max(0f, Mathf.Sin(t * 5f)) * 2f;
                    headYaw = 6f + Mathf.Sin(t * 0.6f) * 3f;
                    break;
                case Act.Thinking:
                    headYaw = Mathf.Sin(t * 0.5f + _seed) * 9f;
                    headPitch = Mathf.Sin(t * 0.33f) * 3f;
                    typeR = Mathf.Max(0f, Mathf.Sin(t * 3.1f)) * 3f; // dedo batendo no queixo
                    break;
                case Act.LookAround:
                case Act.Relaxed:
                    headYaw = Mathf.Sin(t * 0.55f + _seed) * 34f;
                    headPitch = Mathf.Sin(t * 0.4f + 1f) * 5f;
                    break;
                case Act.Stretch:
                    typeL = Mathf.Sin(t * 2.2f) * 4f;
                    typeR = Mathf.Sin(t * 2.2f + 0.6f) * 4f;
                    break;
            }

            _spine.localRotation = Quaternion.Euler(pose.lean + breathe, 0f, 0f);
            _neck.localRotation = Quaternion.Euler(pose.headPitch + headPitch * weight - breathe * 0.6f, pose.headYaw + headYaw * weight, pose.headTilt);
            _shoulderL.localRotation = Quaternion.Euler(pose.shoulderL, 0f, pose.spreadL);
            _shoulderR.localRotation = Quaternion.Euler(pose.shoulderR + shoulderR * weight, 0f, -(pose.spreadR + spreadR * weight));
            _elbowL.localRotation = Quaternion.Euler(pose.elbowL + typeL * weight, 0f, 0f);
            _elbowR.localRotation = Quaternion.Euler(pose.elbowR + typeR * weight, 0f, 0f);
        }
    }
}
