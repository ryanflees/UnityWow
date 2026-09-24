// Copyright (c) 2026 CatRabbit. All rights reserved.

using UnityEngine;

namespace CR
{
    /// <summary>Default WASD/Space and mouse bindings for the active input backend.</summary>
    public class BuiltInInputProvider : RuntimeInputProvider
    {
        protected override RuntimeInputState ReadInputOverride(float dt)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var forward = Button(keyboard?.wKey);
            var backward = Button(keyboard?.sKey);
            var left = Button(keyboard?.aKey);
            var right = Button(keyboard?.dKey);
            return new RuntimeInputState
            {
                m_Move = new Vector2((right.m_IsPressed ? 1f : 0f) - (left.m_IsPressed ? 1f : 0f),
                    (forward.m_IsPressed ? 1f : 0f) - (backward.m_IsPressed ? 1f : 0f)),
                m_Look = mouse != null ? mouse.delta.ReadValue() * 0.1f : Vector2.zero,
                m_Zoom = mouse != null ? mouse.scroll.ReadValue().y / 120f * 0.1f : 0f,
                m_PrimaryMouse = Button(mouse?.leftButton),
                m_SecondaryMouse = Button(mouse?.rightButton),
                m_MoveForward = forward,
                m_MoveBackward = backward,
                m_MoveLeft = left,
                m_MoveRight = right,
                m_Jump = Button(keyboard?.spaceKey),
                m_ActionSlot1 = Button(keyboard?.digit1Key),
                m_ActionSlot4 = Button(keyboard?.digit4Key)
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new RuntimeInputState
            {
                m_Move = new Vector2((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                    (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f)),
                m_Look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")),
                m_Zoom = Input.mouseScrollDelta.y * 0.1f,
                m_PrimaryMouse = new RuntimeInputButton(Input.GetMouseButton(0), Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0)),
                m_SecondaryMouse = new RuntimeInputButton(Input.GetMouseButton(1), Input.GetMouseButtonDown(1), Input.GetMouseButtonUp(1)),
                m_MoveForward = Button(KeyCode.W),
                m_MoveBackward = Button(KeyCode.S),
                m_MoveLeft = Button(KeyCode.A),
                m_MoveRight = Button(KeyCode.D),
                m_Jump = Button(KeyCode.Space),
                m_ActionSlot1 = Button(KeyCode.Alpha1),
                m_ActionSlot4 = Button(KeyCode.Alpha4)
            };
#else
            return RuntimeInputState.Empty;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static RuntimeInputButton Button(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            return button == null ? default : new RuntimeInputButton(button.isPressed, button.wasPressedThisFrame, button.wasReleasedThisFrame);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private static RuntimeInputButton Button(KeyCode key)
        {
            return new RuntimeInputButton(Input.GetKey(key), Input.GetKeyDown(key), Input.GetKeyUp(key));
        }
#endif
    }
}

