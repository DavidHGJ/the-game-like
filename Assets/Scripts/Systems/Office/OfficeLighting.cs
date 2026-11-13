using UnityEngine;
using UnityEngine.Rendering;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Configura uma iluminação "diorama" agradável para o escritório iso (estilo
    /// tycoon): luz ambiente plana (nada de sombras pretas), uma luz direcional
    /// principal quente com sombra suave e uma luz de preenchimento fria sem
    /// sombra. Reutilizada pelo jogo e pelo preview do criador de fases para o
    /// visual ficar consistente e não "chapado/sem luz".
    /// </summary>
    public static class OfficeLighting
    {
        /// <summary>Aplica a iluminação à cena atual (cria as luzes se faltarem).</summary>
        public static void Apply() => Apply(new Color(0.62f, 0.60f, 0.56f));

        /// <summary>
        /// Como <see cref="Apply()"/>, com a luz ambiente em três tons: céu frio por cima, tom
        /// neutro nos lados e, por baixo, o rebote da cor do piso — é o que faz a base dos
        /// móveis "pegar" a cor do chão em vez de ficar cinza.
        /// </summary>
        public static void Apply(Color floorColor)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.68f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.50f, 0.48f);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.40f, 0.38f, 0.36f), floorColor, 0.65f) * 0.62f;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;

            var main = FindOrCreate("Sun (Main)", out bool createdMain);
            main.type = LightType.Directional;
            main.color = new Color(1f, 0.96f, 0.89f);
            main.intensity = 1.3f;
            main.shadows = LightShadows.Soft;
            main.shadowStrength = 0.72f;
            main.shadowBias = 0.02f;
            main.shadowNormalBias = 0.25f;
            main.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            var fill = FindOrCreateNamed("Fill Light");
            fill.type = LightType.Directional;
            fill.color = new Color(0.78f, 0.84f, 1f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(-18f, 150f, 0f);
        }

        private static Light FindOrCreate(string _, out bool created)
        {
            // Reaproveita a primeira direcional existente como principal.
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional && l.name != "Fill Light")
                {
                    created = false;
                    return l;
                }
            }
            created = true;
            var go = new GameObject("Sun (Main)");
            return go.AddComponent<Light>();
        }

        private static Light FindOrCreateNamed(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                var l = existing.GetComponent<Light>();
                if (l != null) return l;
            }
            var go = new GameObject(name);
            return go.AddComponent<Light>();
        }
    }
}
