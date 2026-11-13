using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Ações de input do Map Builder (Input System). Tudo é uma Action mapeada — nenhum
    /// atalho é lido direto do teclado — então rebind e suporte a controle ficam possíveis.
    /// </summary>
    internal sealed class MapBuilderInput : IDisposable
    {
        private readonly InputActionMap _map = new InputActionMap("MapBuilder");

        public InputAction Point { get; }
        public InputAction Primary { get; }
        public InputAction Secondary { get; }
        public InputAction PanDrag { get; }
        public InputAction Zoom { get; }
        public InputAction Pan { get; }
        public InputAction Control { get; }
        public InputAction Rotate { get; }
        public InputAction Remove { get; }
        public InputAction Cancel { get; }
        public InputAction Undo { get; }
        public InputAction Redo { get; }
        public InputAction Save { get; }
        public InputAction Duplicate { get; }
        public InputAction Guides { get; }
        public InputAction ViewLeft { get; }
        public InputAction ViewRight { get; }
        public InputAction ViewHome { get; }

        public MapBuilderInput()
        {
            Point = _map.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            Primary = _map.AddAction("Primary", InputActionType.Button, "<Mouse>/leftButton");
            Secondary = _map.AddAction("Secondary", InputActionType.Button, "<Mouse>/rightButton");
            PanDrag = _map.AddAction("PanDrag", InputActionType.Button, "<Mouse>/middleButton");
            Zoom = _map.AddAction("Zoom", InputActionType.PassThrough, "<Mouse>/scroll/y");
            Control = _map.AddAction("Control", InputActionType.Button, "<Keyboard>/ctrl");

            Pan = _map.AddAction("Pan", InputActionType.Value);
            Pan.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Pan.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            Rotate = _map.AddAction("Rotate", InputActionType.Button, "<Keyboard>/r");
            Remove = _map.AddAction("Remove", InputActionType.Button, "<Keyboard>/delete");
            Remove.AddBinding("<Keyboard>/backspace");
            Cancel = _map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            Guides = _map.AddAction("Guides", InputActionType.Button, "<Keyboard>/g");
            ViewLeft = _map.AddAction("ViewLeft", InputActionType.Button, "<Keyboard>/q");
            ViewRight = _map.AddAction("ViewRight", InputActionType.Button, "<Keyboard>/e");
            ViewHome = _map.AddAction("ViewHome", InputActionType.Button, "<Keyboard>/home");

            Undo = WithControl("Undo", "<Keyboard>/z");
            Redo = WithControl("Redo", "<Keyboard>/y");
            Save = WithControl("Save", "<Keyboard>/s");
            Duplicate = WithControl("Duplicate", "<Keyboard>/d");

            _map.Enable();
        }

        private InputAction WithControl(string name, string key)
        {
            var action = _map.AddAction(name, InputActionType.Button);
            action.AddCompositeBinding("OneModifier").With("Modifier", "<Keyboard>/ctrl").With("Binding", key);
            return action;
        }

        /// <summary>Posição do ponteiro em pixels de tela (origem embaixo à esquerda).</summary>
        public Vector2 PointerPosition => Point.ReadValue<Vector2>();

        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
