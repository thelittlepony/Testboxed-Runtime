using System;
using System.Collections.Generic;
using ru.tlpteam.tb.Runtime.Window;
using ru.tlpteam.tb.Runtime.Engine;
using ru.tlpteam.tb.Core;

namespace ru.tlpteam.Input
{
    /// <summary>
    /// Keyboard keys independent from SFML APIs.
    /// </summary>
    public enum TlpKey
    {
        Unknown = -1, A = 0, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
        Escape, LControl, LShift, LAlt, LSystem, RControl, RShift, RAlt, RSystem,
        Menu, LBracket, RBracket, SemiColon, Comma, Period, Quote, Slash, BackSlash,
        Tilde, Equal, Dash, Space, Return, BackSpace, Tab, PageUp, PageDown, End, Home,
        Insert, Delete, Add, Subtract, Multiply, Divide, Left, Right, Up, Down,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, Pause
    }

    public enum TlpMouseButton
    {
        Left, Right, Middle, XButton1, XButton2
    }

    public static class TlpInput
    {
        private static readonly HashSet<TlpKey> _currentKeys = new();
        private static readonly HashSet<TlpKey> _previousKeys = new();

        private static readonly HashSet<TlpMouseButton> _currentButtons = new();
        private static readonly HashSet<TlpMouseButton> _previousButtons = new();

        /// <summary>
        /// Refreshes keyboard and mouse state; call once per frame.
        /// </summary>
        public static void UpdateStates()
        {
            _previousKeys.Clear();
            foreach (var key in _currentKeys) _previousKeys.Add(key);

            _previousButtons.Clear();
            foreach (var btn in _currentButtons) _previousButtons.Add(btn);

            var window = TestboxedBridge.WindowProvider;
            if (window != null)
            {
                _currentKeys.Clear();
                foreach (TlpKey k in Enum.GetValues(typeof(TlpKey)))
                {
                    if (k == TlpKey.Unknown) continue;
                    if (window.IsKeyPressed(k))
                        _currentKeys.Add(k);
                }

                _currentButtons.Clear();
                foreach (TlpMouseButton b in Enum.GetValues(typeof(TlpMouseButton)))
                {
                    if (window.IsMouseButtonPressed(b))
                        _currentButtons.Add(b);
                }
            }
        }

        public static class Keyboard
        {
            /// <summary>
            /// Returns true while key is held.
            /// </summary>
            public static bool GetKey(TlpKey key) => _currentKeys.Contains(key);

            /// <summary>
            /// Returns true only on the frame when key becomes pressed.
            /// </summary>
            public static bool GetKeyDown(TlpKey key) => _currentKeys.Contains(key) && !_previousKeys.Contains(key);

            /// <summary>
            /// Returns true only on the frame when key is released.
            /// </summary>
            public static bool GetKeyUp(TlpKey key) => !_currentKeys.Contains(key) && _previousKeys.Contains(key);
        }

        public static class Mouse
        {
            /// <summary>
            /// Returns mouse position relative to the game window.
            /// </summary>
            public static Vector2f GetPosition()
            {
                var window = TestboxedBridge.WindowProvider;
                if (window != null)
                {
                    return new Vector2f(GetX(), GetY());
                }

                return new Vector2f(0, 0);
            }

            public static float GetX()
            {
                var win = TestboxedBridge.WindowProvider;
                return win?.GetMousePositionX() ?? 0;
            }

            public static float GetY()
            {
                var win = TestboxedBridge.WindowProvider;
                return win?.GetMousePositionY() ?? 0;
            }

            public static bool GetButton(TlpMouseButton button) => _currentButtons.Contains(button);
            public static bool GetButtonDown(TlpMouseButton button) =>
                _currentButtons.Contains(button) && !_previousButtons.Contains(button);
            public static bool GetButtonUp(TlpMouseButton button) =>
                !_currentButtons.Contains(button) && _previousButtons.Contains(button);
        }
    }
}
