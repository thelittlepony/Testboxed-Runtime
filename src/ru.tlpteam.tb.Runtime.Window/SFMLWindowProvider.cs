using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using ru.tlpteam.Input;
using ru.tlpteam.tb.Core;
using ru.tlpteam.tb.Runtime.Rendering;
using ru.tlpteam.tb.Runtime.Rendering.Backends;

namespace ru.tlpteam.tb.Runtime.Window
{
    /// <summary>
    /// SFML-based implementation of IWindowProvider.
    /// Window/input concerns live here; render concerns are delegated to the render backend.
    /// </summary>
    public class SFMLWindowProvider : IWindowProvider
    {
        private readonly RenderWindow _window;
        private readonly SFMLRenderBackend _renderBackend;
        private readonly Clock _clock = new();
        private static readonly Dictionary<TlpKey, Keyboard.Key> _keyMap = BuildKeyMap();
        private static readonly Dictionary<TlpMouseButton, Mouse.Button> _mouseMap = BuildMouseMap();

        public bool IsOpen => _window.IsOpen;
        public float DeltaTime { get; private set; }
        public uint ViewportWidth => _renderBackend.ViewportWidth;
        public uint ViewportHeight => _renderBackend.ViewportHeight;

        public SFMLWindowProvider(
            uint width,
            uint height,
            string title,
            float renderScale = 1f,
            uint? viewportWidth = null,
            uint? viewportHeight = null)
        {
            _window = new RenderWindow(new VideoMode(width, height), title);
            _window.Closed += (s, e) => _window.Close();
            _window.Resized += (_, e) => OnWindowResized(e.Width, e.Height);

            CenterWindowOnPrimaryScreen(width, height);
            _window.SetVisible(true);
            _window.RequestFocus();

            _renderBackend = new SFMLRenderBackend(
                _window,
                width,
                height,
                renderScale,
                viewportWidth,
                viewportHeight);
        }

        public bool IsKeyPressed(TlpKey key)
        {
            if (!_keyMap.TryGetValue(key, out var mapped))
                return false;

            return Keyboard.IsKeyPressed(mapped);
        }

        public bool IsMouseButtonPressed(TlpMouseButton button)
        {
            if (!_mouseMap.TryGetValue(button, out var mapped))
                return false;

            return Mouse.IsButtonPressed(mapped);
        }

        public void SetTitle(string title) => _window.SetTitle(title);
        public void SetFramerateLimit(uint limit) => _window.SetFramerateLimit(limit);
        public void Close() => _window.Close();
        public void DispatchEvents() => _window.DispatchEvents();

        public void Clear(TlpColor clearColor) => _renderBackend.Clear(clearColor);
        public void Display() => _renderBackend.Display();
        public void Draw(ISprite sprite) => _renderBackend.Draw(sprite);
        public void DrawDebugText(string text, float x, float y, TlpColor color, uint characterSize = 12) =>
            _renderBackend.DrawDebugText(text, x, y, color, characterSize);
        public ISprite CreateSpriteFromTexture(string texturePath) => _renderBackend.CreateSpriteFromTexture(texturePath);
        public ISprite CreateSolidSprite(uint width, uint height, TlpColor color) => _renderBackend.CreateSolidSprite(width, height, color);

        // DeltaTime must be updated once per frame.
        public void UpdateDeltaTime()
        {
            DeltaTime = _clock.Restart().AsSeconds();
        }

        public int GetMousePositionX()
        {
            var pos = Mouse.GetPosition(_window);
            if (_renderBackend.ViewportWidth == 0) return pos.X;

            float normalized = _window.Size.X > 0 ? pos.X / (float)_window.Size.X : 0f;
            float mapped = normalized * _renderBackend.ViewportWidth;
            return (int)System.Math.Clamp(System.Math.Round(mapped), 0d, _renderBackend.ViewportWidth - 1d);
        }

        public int GetMousePositionY()
        {
            var pos = Mouse.GetPosition(_window);
            if (_renderBackend.ViewportHeight == 0) return pos.Y;

            float normalized = _window.Size.Y > 0 ? pos.Y / (float)_window.Size.Y : 0f;
            float mapped = normalized * _renderBackend.ViewportHeight;
            return (int)System.Math.Clamp(System.Math.Round(mapped), 0d, _renderBackend.ViewportHeight - 1d);
        }

        private static Dictionary<TlpKey, Keyboard.Key> BuildKeyMap()
        {
            var map = new Dictionary<TlpKey, Keyboard.Key>();

            foreach (TlpKey key in Enum.GetValues(typeof(TlpKey)))
            {
                if (key == TlpKey.Unknown) continue;

                string sfmlName = key switch
                {
                    TlpKey.SemiColon => "Semicolon",
                    TlpKey.BackSlash => "Backslash",
                    TlpKey.Dash => "Hyphen",
                    TlpKey.Return => "Enter",
                    TlpKey.BackSpace => "Backspace",
                    _ => key.ToString()
                };

                if (Enum.TryParse(sfmlName, ignoreCase: false, out Keyboard.Key parsed))
                {
                    map[key] = parsed;
                }
            }

            return map;
        }

        private static Dictionary<TlpMouseButton, Mouse.Button> BuildMouseMap()
        {
            return new Dictionary<TlpMouseButton, Mouse.Button>
            {
                [TlpMouseButton.Left] = Mouse.Button.Left,
                [TlpMouseButton.Right] = Mouse.Button.Right,
                [TlpMouseButton.Middle] = Mouse.Button.Middle,
                [TlpMouseButton.XButton1] = Mouse.Button.XButton1,
                [TlpMouseButton.XButton2] = Mouse.Button.XButton2
            };
        }

        private void OnWindowResized(uint width, uint height)
        {
            _renderBackend.Resize(width, height);
        }

        private void CenterWindowOnPrimaryScreen(uint width, uint height)
        {
            var desktop = VideoMode.DesktopMode;

            int clampedWidth = (int)System.Math.Min(width, desktop.Width);
            int clampedHeight = (int)System.Math.Min(height, desktop.Height);

            int x = System.Math.Max(0, ((int)desktop.Width - clampedWidth) / 2);
            int y = System.Math.Max(0, ((int)desktop.Height - clampedHeight) / 2);

            _window.Position = new Vector2i(x, y);
        }
    }
}
