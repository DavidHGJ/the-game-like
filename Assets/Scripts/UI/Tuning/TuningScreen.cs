using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using GameLike.Data.Balance;
using GameLike.Data.Base;
using GameLike.Data.Definitions;
using GameLike.Systems.Core;
using GameLike.UI.Gallery;
using GameLike.UI.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Tela in-game de Parâmetros (GDD 23.13) — ferramenta do dev para calibrar o jogo sem
    /// recompilar. Lista todos os assets de balanceamento e o conteúdo (gêneros, temas,
    /// plataformas) e monta, por reflexão, um campo editável para cada valor serializado:
    /// números, liga/desliga, enums, textos, listas e estruturas aninhadas. Um parâmetro novo
    /// num asset aparece aqui sozinho. "Salvar" grava os ajustes; o jogo lê os valores a cada
    /// uso, então a mudança vale na hora.
    /// </summary>
    public sealed class TuningScreen : MonoBehaviour
    {
        private const string PanelResource = "HudPanelSettings";
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

        private sealed class Group
        {
            public string section, title;
            public UnityEngine.Object target;
            public VisualElement button;
        }

        private readonly List<Group> _groups = new List<Group>();
        private readonly HashSet<UnityEngine.Object> _dirty = new HashSet<UnityEngine.Object>();
        private VisualElement _root, _list, _content;
        private Label _status;
        private Group _current;
        private string _search = "";
        private bool _resumeTime;

        /// <summary>true no editor e em development build (o jogador final não vê).</summary>
        public static bool IsAvailable => GameLike.Core.DevTools.Enabled;

        /// <summary>Abre a tela por cima do que estiver na tela (menu ou jogo).</summary>
        public static void Open()
        {
            if (!IsAvailable || FindFirstObjectByType<TuningScreen>() != null) return;
            new GameObject("[Tuning]").AddComponent<TuningScreen>();
        }

        private void Awake()
        {
            var panel = Resources.Load<PanelSettings>(PanelResource);
            if (panel == null) { Destroy(gameObject); return; }

            var gc = FindFirstObjectByType<GameController>();
            if (gc != null && gc.Time != null && !gc.Time.IsPaused)
            {
                gc.Time.Pause();
                _resumeTime = true;
            }

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.sortingOrder = 60;
            _root = document.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Kit.RGB(238, 232, 219);

            CollectGroups();
            BuildLayout();
            if (_groups.Count > 0) Select(_groups[0]);
        }

        private void Close()
        {
            if (_resumeTime)
            {
                var gc = FindFirstObjectByType<GameController>();
                if (gc != null && gc.Time != null) gc.Time.Resume();
            }
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ grupos

        private void CollectGroups()
        {
            // Garante que os parâmetros gerais existam mesmo sem asset criado.
            BalanceProvider.Get<GameTuningBalanceSO>();
            foreach (var asset in BalanceProvider.All())
                _groups.Add(new Group { section = "Balanceamento", title = GroupTitle(asset.GetType().Name.Replace("BalanceSO", "")), target = asset });
            AddDefinitions("Gêneros", Resources.LoadAll<GenreSO>("Genres"));
            AddDefinitions("Temas", Resources.LoadAll<ThemeSO>("Themes"));
            AddDefinitions("Plataformas", Resources.LoadAll<ConsoleSO>("Consoles"));
        }

        private void AddDefinitions<T>(string section, T[] assets) where T : DefinitionAsset
        {
            Array.Sort(assets, (a, b) => string.Compare(a.DisplayLabel, b.DisplayLabel, StringComparison.OrdinalIgnoreCase));
            foreach (var asset in assets)
                _groups.Add(new Group { section = section, title = asset.DisplayLabel, target = asset });
        }

        // ------------------------------------------------------------------ layout

        private void BuildLayout()
        {
            var top = Kit.Card(0, 16);
            top.style.position = Position.Absolute;
            top.style.left = 14; top.style.right = 14; top.style.top = 12;
            top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
            top.Add(Kit.SquareBadge(Kit.IcSliders, Kit.Purple, 38, 11));
            top.Add(Kit.Gap(12));
            var titles = Kit.Col();
            titles.Add(Kit.Text("Parâmetros do jogo", 18, Kit.Ink, true));
            titles.Add(Kit.Text("Os valores valem na hora; Salvar guarda para as próximas sessões.", 11, Kit.Soft));
            top.Add(titles);
            top.Add(Kit.Gap(24));
            var search = Widgets.Input("", 280, 14, 40);
            search.style.height = 36;
            search.textEdition.placeholder = "Buscar parâmetro…";
            search.RegisterValueChangedCallback(e => { _search = e.newValue ?? ""; RebuildContent(); });
            top.Add(search);
            top.Add(Kit.Spacer());
            _status = Kit.Text("", 12, Kit.Soft, true);
            _status.style.marginRight = 14;
            top.Add(_status);
            top.Add(Kit.Button("Salvar", Kit.IcSave, Kit.Teal, Color.white, Save, 38));
            top.Add(Kit.Gap(8));
            top.Add(Kit.Button("Fechar", Kit.IcClose, Kit.Sand, Kit.Ink, Close, 38));
            _root.Add(top);

            var left = Kit.Card(300, 16);
            left.style.position = Position.Absolute;
            left.style.left = 14; left.style.top = 86; left.style.bottom = 14;
            var listScroll = new ScrollView(ScrollViewMode.Vertical);
            listScroll.style.flexGrow = 1;
            _list = listScroll.contentContainer;
            left.Add(listScroll);
            _root.Add(left);

            string section = null;
            foreach (var group in _groups)
            {
                if (group.section != section)
                {
                    section = group.section;
                    var caption = Kit.Caps(section, 11, Kit.Faint);
                    caption.style.marginTop = _list.childCount == 0 ? 0 : 12; caption.style.marginBottom = 6;
                    _list.Add(caption);
                }
                var captured = group;
                var button = Kit.Row();
                Kit.Pad(button, 7, 12); Kit.Radius(button, 10); Kit.Smooth(button);
                button.style.marginBottom = 3; button.style.marginRight = 6;
                var label = Kit.Text(group.title, 13, Kit.Ink, true);
                label.style.flexShrink = 1; label.style.overflow = Overflow.Hidden;
                button.Add(label);
                button.RegisterCallback<ClickEvent>(_ => { Kit.Press(); Select(captured); });
                group.button = button;
                _list.Add(button);
            }

            var right = Kit.Card(0, 16);
            right.style.position = Position.Absolute;
            right.style.left = 326; right.style.right = 14; right.style.top = 86; right.style.bottom = 14;
            var contentScroll = new ScrollView(ScrollViewMode.Vertical);
            contentScroll.style.flexGrow = 1;
            _content = contentScroll.contentContainer;
            right.Add(contentScroll);
            _root.Add(right);
        }

        private void Select(Group group)
        {
            CommitTyping();
            _current = group;
            foreach (var g in _groups)
            {
                bool on = g == group;
                g.button.style.backgroundColor = on ? Kit.Teal : new Color(0, 0, 0, 0);
                g.button.Q<Label>().style.color = on ? Color.white : Kit.Ink;
            }
            RebuildContent();
        }

        private void RebuildContent()
        {
            _content.Clear();
            if (_current == null) return;
            var target = _current.target;

            var head = Kit.Row();
            head.style.marginBottom = 8;
            head.Add(Kit.Text(_current.title, 20, Kit.Ink, true));
            head.Add(Kit.Spacer());
            if (target is BalanceAsset balance)
            {
                if (BalanceProvider.HasOverride(balance)) { head.Add(Kit.Chip("com ajuste salvo", Kit.Teal, true, Kit.IcCheck)); head.Add(Kit.Gap(8)); }
                head.Add(Kit.Button("Restaurar valores originais", Kit.IcUndo, Kit.Sand, Kit.Ink, () =>
                {
                    CommitTyping();
                    BalanceProvider.ResetToDefaults(balance);
                    _dirty.Remove(balance);
                    SetStatus("Valores originais restaurados.");
                    RebuildContent();
                }, 32));
            }
            else if (!Application.isEditor)
            {
                head.Add(Kit.Chip("neste build vale só nesta sessão — para gravar, use pelo editor", Kit.Orange, false, Kit.IcWarn));
            }
            _content.Add(head);

            foreach (var field in SerializedFields(target.GetType()))
            {
                var header = field.GetCustomAttribute<HeaderAttribute>();
                bool matches = Matches(field);
                if (header != null && (_search.Length == 0 || matches))
                {
                    var caption = Kit.Caps(header.header, 12, Kit.Purple);
                    caption.style.marginTop = 16; caption.style.marginBottom = 6;
                    _content.Add(caption);
                }
                if (!matches) continue;
                var captured = field;
                var row = Draw(FieldLabel(field), field.FieldType, field,
                    () => captured.GetValue(target),
                    value => { captured.SetValue(target, value); Touch(target); }, 0);
                if (row != null) _content.Add(row);
            }
        }

        private bool Matches(FieldInfo field)
        {
            if (_search.Length == 0) return true;
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            if (TuningTexts.TryField(field.Name, out string label, out string help)
                && (label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 || help.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return Nicify(field.Name).IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                   || field.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                   || (tooltip != null && tooltip.tooltip.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void Touch(UnityEngine.Object target)
        {
            _dirty.Add(target);
            SetStatus(_dirty.Count + " grupo(s) com mudanças não salvas");
        }

        private void SetStatus(string text)
        {
            if (_status != null) _status.text = text;
        }

        private void Save()
        {
            CommitTyping();
            int saved = 0;
            foreach (var target in _dirty)
            {
                if (target is BalanceAsset balance) BalanceProvider.SaveOverride(balance);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(target);
#endif
                saved++;
            }
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            _dirty.Clear();
            Sfx.Play(SfxId.Success);
            SetStatus(saved == 0 ? "Nada para salvar." : saved + " grupo(s) salvo(s).");
            RebuildContent();
        }

        // ------------------------------------------------------------------ campos por reflexão

        private static IEnumerable<FieldInfo> SerializedFields(Type type)
        {
            var chain = new List<Type>();
            for (var t = type; t != null && t != typeof(ScriptableObject) && t != typeof(object); t = t.BaseType) chain.Insert(0, t);
            foreach (var t in chain)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false)) continue;
                    if (field.IsDefined(typeof(HideInInspector), false)) continue;
                    yield return field;
                }
            }
        }

        /// <summary>Monta o editor de um valor do tipo dado. Devolve null para tipos sem editor.</summary>
        private VisualElement Draw(string label, Type type, FieldInfo info, Func<object> get, Action<object> set, int depth)
        {
            string tooltip = info == null ? null : TuningTexts.TryField(info.Name, out _, out string help) ? help : info.GetCustomAttribute<TooltipAttribute>()?.tooltip;
            var range = info != null ? info.GetCustomAttribute<RangeAttribute>() : null;

            if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double))
                return Line(label, tooltip, NumberEditor(type, range, get, set));
            if (type == typeof(bool))
                return Line(label, tooltip, Widgets.Toggle((bool)get(), v => set(v)));
            if (type == typeof(string))
            {
                var field = Widgets.Input((string)get() ?? "", 320, 13, 80);
                field.style.height = 34;
                field.RegisterCallback<FocusOutEvent>(_ => set(field.value));
                return Line(label, tooltip, field);
            }
            if (type.IsEnum)
            {
                string[] names = Array.ConvertAll(Enum.GetNames(type), n => TuningTexts.EnumValue(type, n));
                Array values = Enum.GetValues(type);
                int index = Mathf.Max(0, Array.IndexOf(values, get()));
                return Line(label, tooltip, Widgets.Select(names, index, 220, i => set(values.GetValue(i))));
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var reference = get() as UnityEngine.Object;
                string name = reference == null ? "—" : reference is DefinitionAsset def ? def.DisplayLabel : reference.name;
                return Line(label, tooltip, Kit.Chip(name, Kit.Soft));
            }
            if (typeof(IList).IsAssignableFrom(type))
                return ListEditor(label, tooltip, type, get, depth);
            if (type == typeof(Color) || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector2Int) || type == typeof(AnimationCurve))
                return null;
            if ((type.IsClass || type.IsValueType) && !type.IsPrimitive && type.IsDefined(typeof(SerializableAttribute), false)
                && type.Namespace != null && type.Namespace.StartsWith("GameLike"))
                return StructEditor(label, type, get, set, depth);
            return null;
        }

        private static VisualElement Line(string label, string tooltip, VisualElement editor)
        {
            var row = Kit.Row();
            row.style.marginBottom = 6; row.style.paddingTop = 4; row.style.paddingBottom = 4;
            row.style.borderBottomWidth = 1; row.style.borderBottomColor = Kit.Track;
            var text = Kit.Col();
            text.style.flexGrow = 1; text.style.flexShrink = 1; text.style.marginRight = 16;
            text.Add(Kit.Wrap(label, 14, Kit.Ink, true));
            if (!string.IsNullOrEmpty(tooltip)) text.Add(Kit.Wrap(tooltip, 11, Kit.Soft));
            row.Add(text);
            editor.style.flexShrink = 0;
            row.Add(editor);
            return row;
        }

        private VisualElement NumberEditor(Type type, RangeAttribute range, Func<object> get, Action<object> set)
        {
            bool whole = type == typeof(int) || type == typeof(long);
            var row = Kit.Row();
            var field = Widgets.Input(Format(get(), whole), 150, 14, 24);
            field.style.height = 34;

            void Commit(double value)
            {
                if (range != null) value = Math.Max(range.min, Math.Min(range.max, value));
                object boxed = TuningNumbers.Box(type, value);
                set(boxed);
                field.SetValueWithoutNotify(Format(boxed, whole));
            }

            double Current() => Convert.ToDouble(get(), CultureInfo.InvariantCulture);

            double Step()
            {
                double magnitude = Math.Abs(Current());
                if (range != null) return Math.Max((range.max - range.min) / 100.0, whole ? 1.0 : 0.001);
                if (whole) return magnitude < 50 ? 1 : Math.Pow(10, Math.Floor(Math.Log10(magnitude)) - 1);
                return magnitude < 0.0001 ? 0.01 : Math.Pow(10, Math.Floor(Math.Log10(magnitude)) - 1);
            }

            field.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (TuningNumbers.TryParse(field.value, whole, out double parsed)) Commit(parsed);
                else field.SetValueWithoutNotify(Format(get(), whole));
            });
            row.Add(Kit.RoundButton(Kit.IcMinus, Kit.Sand, Kit.Ink, 28, () => Commit(Current() - Step())));
            row.Add(Kit.Gap(6));
            row.Add(field);
            row.Add(Kit.Gap(6));
            row.Add(Kit.RoundButton(Kit.IcPlus, Kit.Sand, Kit.Ink, 28, () => Commit(Current() + Step())));
            if (range != null)
            {
                var limits = Kit.Text(range.min.ToString("0.###", Culture) + " a " + range.max.ToString("0.###", Culture), 11, Kit.Faint);
                limits.style.marginLeft = 8; limits.style.width = 78;
                row.Add(limits);
            }
            return row;
        }

        private static string Format(object value, bool whole) =>
            whole ? Convert.ToInt64(value).ToString("N0", Culture) : Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.####", Culture);

        /// <summary>Aceita "1.234,5", "1234.5" e "1234,5".</summary>
        /// <summary>
        /// Confirma o campo de texto que estiver sendo digitado. Clicar em Salvar, trocar de grupo
        /// ou restaurar não tira o foco dele sozinho — sem isso, o último número digitado se perdia.
        /// </summary>
        private void CommitTyping()
        {
            var focused = _root?.panel?.focusController?.focusedElement as VisualElement;
            focused?.Blur();
        }

        private VisualElement ListEditor(string label, string tooltip, Type listType, Func<object> get, int depth)
        {
            var list = get() as IList;
            Type element = listType.IsArray ? listType.GetElementType()
                : listType.IsGenericType ? listType.GetGenericArguments()[0] : typeof(object);
            var box = Box(label + (list != null ? "  (" + list.Count + ")" : ""), tooltip, depth);
            if (list == null || depth > 3) return box;
            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                var row = Draw(ElementLabel(list[i], i), element, null, () => list[index], value => { list[index] = value; Touch(_current.target); }, depth + 1);
                if (row != null) box.Add(row);
            }
            return box;
        }

        private VisualElement StructEditor(string label, Type type, Func<object> get, Action<object> set, int depth)
        {
            var box = Box(label, null, depth);
            if (depth > 3) return box;
            foreach (var field in SerializedFields(type))
            {
                var captured = field;
                var row = Draw(FieldLabel(field), field.FieldType, field,
                    () => { object owner = get(); return owner != null ? captured.GetValue(owner) : null; },
                    value =>
                    {
                        object owner = get();
                        if (owner == null) return;
                        captured.SetValue(owner, value);
                        set(owner); // estruturas são cópias: devolve a alterada para quem a guarda
                    }, depth + 1);
                if (row != null) box.Add(row);
            }
            return box;
        }

        private static VisualElement Box(string title, string tooltip, int depth)
        {
            var box = new VisualElement();
            box.style.marginTop = 6; box.style.marginBottom = 8;
            box.style.paddingLeft = 12; box.style.paddingTop = 6; box.style.paddingBottom = 2;
            box.style.borderLeftWidth = 3; box.style.borderLeftColor = depth == 0 ? Kit.Teal : Kit.Track;
            box.Add(Kit.Text(title, depth == 0 ? 14 : 13, Kit.Ink, true));
            if (!string.IsNullOrEmpty(tooltip)) box.Add(Kit.Wrap(tooltip, 11, Kit.Soft));
            box.Add(Kit.Gap(0, 4));
            return box;
        }

        /// <summary>Rótulo de um item de lista: o enum ou a definição que ele carrega, senão a posição.</summary>
        private static string ElementLabel(object element, int index)
        {
            if (element == null) return "#" + (index + 1);
            if (element is DefinitionAsset definition) return definition.DisplayLabel;
            Type type = element.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)) return "#" + (index + 1);
            foreach (var field in SerializedFields(type))
            {
                object value = field.GetValue(element);
                if (value is DefinitionAsset inner) return inner.DisplayLabel;
                if (value != null && field.FieldType.IsEnum) return TuningTexts.EnumValue(field.FieldType, value.ToString());
                if (value is string text && !string.IsNullOrEmpty(text)) return text;
            }
            return "#" + (index + 1);
        }

        /// <summary>Nome em português do campo; sem tradução, o nome derivado do código.</summary>
        private static string FieldLabel(FieldInfo field) =>
            TuningTexts.TryField(field.Name, out string label, out _) ? label : Nicify(field.Name);

        private static string GroupTitle(string typeKey) => TuningTexts.Group(typeKey) ?? Nicify(typeKey);

        /// <summary>"garageToIndieThreshold" → "Garage to indie threshold".</summary>
        private static string Nicify(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new System.Text.StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_') { sb.Append(' '); continue; }
                bool boundary = i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])))
                                || i > 0 && char.IsDigit(c) && !char.IsDigit(name[i - 1]);
                if (boundary) sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpperInvariant(c) : boundary ? char.ToLowerInvariant(c) : c);
            }
            return sb.ToString();
        }
    }
}
