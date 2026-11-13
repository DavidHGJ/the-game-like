using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Core;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Senta os bonecos nos assentos do cenário (<see cref="OfficeLayoutSO.Seats"/>): o fundador
    /// no assento 0 e os funcionários nos seguintes, na ordem de contratação. Refaz a ocupação
    /// quando alguém entra ou sai da equipe. O fundador se chama <c>DevCharacter</c> — é a
    /// âncora de onde saem as bolinhas de ponto do HUD.
    /// </summary>
    public sealed class OfficeCrew : MonoBehaviour
    {
        public const string FounderName = "DevCharacter";

        [Tooltip("Leve avanço do boneco na direção que ele encara (para encostar na mesa).")]
        [SerializeField] private float forwardNudge = 0.05f;

        private OfficeLayoutSO _layout;
        private GameController _gc;

        private void OnEnable()
        {
            EventBus.Subscribe<EmployeeHiredEvent>(OnHired);
            EventBus.Subscribe<EmployeeLeftEvent>(OnLeft);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EmployeeHiredEvent>(OnHired);
            EventBus.Unsubscribe<EmployeeLeftEvent>(OnLeft);
        }

        private void OnHired(EmployeeHiredEvent e) => Refresh();
        private void OnLeft(EmployeeLeftEvent e) => Refresh();

        /// <summary>Define o cenário recém-montado e ocupa os assentos.</summary>
        public void Show(OfficeLayoutSO layout)
        {
            _layout = layout;
            Refresh();
        }

        /// <summary>Reocupa os assentos conforme o tamanho atual da equipe.</summary>
        public void Refresh()
        {
            if (_layout == null) return;
            if (_gc == null) _gc = FindFirstObjectByType<GameController>();
            int employees = _gc != null && _gc.Run != null ? _gc.Run.company.employees.Count : 0;
            var founder = _gc != null && _gc.Meta != null ? _gc.Meta.founder : null;
            CharacterLook founderLook = founder != null
                ? new CharacterLook { skin = founder.skin, hair = founder.hair, hairStyle = founder.hairStyle, shirt = founder.shirt, accessory = founder.accessory }
                : CharacterLook.ForIndex(0);
            Populate(_layout, transform, 1 + employees, forwardNudge, founderLook);
        }

        /// <summary>
        /// Instancia <paramref name="people"/> bonecos nos primeiros assentos de um escritório já
        /// montado sob <paramref name="officeRoot"/>. Usado pelo jogo e pelo preview do Map Builder.
        /// Devolve quantos bonecos couberam.
        /// </summary>
        public static int Populate(OfficeLayoutSO layout, Transform officeRoot, int people, float nudge = 0.05f, CharacterLook? founderLook = null)
        {
            if (layout == null || officeRoot == null) return 0;
            var seats = officeRoot.Find(OfficeBuilder.SeatsRootName);
            if (seats == null) return 0;

            for (int s = 0; s < seats.childCount; s++)
            {
                var seat = seats.GetChild(s);
                for (int c = seat.childCount - 1; c >= 0; c--)
                {
                    var old = seat.GetChild(c).gameObject;
                    if (Application.isPlaying) { old.SetActive(false); Destroy(old); } else DestroyImmediate(old);
                }
            }

            int placed = Mathf.Min(Mathf.Max(0, people), Mathf.Min(seats.childCount, layout.Seats.Count));
            for (int i = 0; i < placed; i++)
            {
                var seat = seats.GetChild(i);
                CharacterLook look = i == 0 ? (founderLook ?? CharacterLook.ForIndex(0)) : CharacterLook.ForIndex(i);
                var person = OfficeCharacter.Create(seat, i == 0 ? FounderName : "Employee_" + i, look, layout.Seats[i].activity, i);
                person.transform.localPosition = new Vector3(0f, OfficeBuilder.SeatHeight(layout, layout.Seats[i]), nudge);
            }
            return placed;
        }
    }
}
