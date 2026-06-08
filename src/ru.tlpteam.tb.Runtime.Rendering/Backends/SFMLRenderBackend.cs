using System;
using System.Collections.Generic;
using System.IO;
using SFML.Graphics;
using ru.tlpteam.Debug;
using ru.tlpteam.tb.Core;
using ru.tlpteam.tb.Runtime.Window;

namespace ru.tlpteam.tb.Runtime.Rendering.Backends
{
    /// <summary>
    /// SFML rendering backend that owns textures, debug font and optional offscreen scaling.
    /// </summary>
    public sealed class SFMLRenderBackend : IRenderBackend
    {
        private readonly RenderWindow _window;
        private readonly RenderTexture? _offscreen;
        private readonly Sprite? _offscreenSprite;
        private readonly Dictionary<string, Texture> _textureCache = new();
        private readonly Font? _debugFont;
        private bool _debugFontWarningShown;
        private readonly float _renderScale;
        private readonly uint _viewportWidth;
        private readonly uint _viewportHeight;

        public uint ViewportWidth => _viewportWidth;
        public uint ViewportHeight => _viewportHeight;

        public SFMLRenderBackend(
            RenderWindow window,
            uint windowWidth,
            uint windowHeight,
            float renderScale = 1f,
            uint? viewportWidth = null,
            uint? viewportHeight = null)
        {
            _window = window;
            _debugFont = LoadDebugFont();
            _renderScale = System.Math.Clamp(renderScale, 1f, 4f);
            _viewportWidth = viewportWidth ?? windowWidth;
            _viewportHeight = viewportHeight ?? windowHeight;

            bool needsOffscreen = _renderScale > 1.001f || _viewportWidth != windowWidth || _viewportHeight != windowHeight;
            if (needsOffscreen)
            {
                uint offscreenWidth = (uint)System.Math.Max(1d, System.Math.Round(_viewportWidth * _renderScale));
                uint offscreenHeight = (uint)System.Math.Max(1d, System.Math.Round(_viewportHeight * _renderScale));
                _offscreen = new RenderTexture(offscreenWidth, offscreenHeight);
                _offscreen.SetView(new View(new FloatRect(0f, 0f, _viewportWidth, _viewportHeight)));

                _offscreen.Texture.Smooth = _renderScale > 1.001f;
                _offscreenSprite = new Sprite(_offscreen.Texture)
                {
                    Scale = new SFML.System.Vector2f(
                        windowWidth / (float)offscreenWidth,
                        windowHeight / (float)offscreenHeight)
                };

                TlpLogging.Info($"Internal render scale: {_renderScale:0.00}x ({offscreenWidth}x{offscreenHeight}), viewport {_viewportWidth}x{_viewportHeight}, window {windowWidth}x{windowHeight}");
            }
        }

        public void Clear(TlpColor clearColor)
        {
            var color = new Color(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
            if (_offscreen != null)
            {
                _offscreen.Clear(color);
                return;
            }

            _window.Clear(color);
        }

        public void Display()
        {
            if (_offscreen == null || _offscreenSprite == null)
            {
                _window.Display();
                return;
            }

            _offscreen.Display();
            _window.Clear(Color.Black);
            _window.Draw(_offscreenSprite);
            _window.Display();
        }

        public void Draw(ISprite sprite)
        {
            if (sprite is not SfmlSpriteAdapter sfmlSprite)
                return;

            if (_offscreen != null)
            {
                _offscreen.Draw(sfmlSprite.NativeSprite);
                return;
            }

            _window.Draw(sfmlSprite.NativeSprite);
        }

        public void DrawDebugText(string text, float x, float y, TlpColor color, uint characterSize = 12)
        {
            if (_debugFont == null)
            {
                if (!_debugFontWarningShown)
                {
                    _debugFontWarningShown = true;
                    TlpLogging.Warning("Debug font not found. Text overlay disabled.");
                }
                return;
            }

            var drawable = new Text(text, _debugFont, characterSize)
            {
                Position = new SFML.System.Vector2f(x, y),
                FillColor = new Color(color.R, color.G, color.B, color.A)
            };

            if (_offscreen != null)
            {
                _offscreen.Draw(drawable);
                return;
            }

            _window.Draw(drawable);
        }

        public ISprite CreateSpriteFromTexture(string texturePath)
        {
            if (!_textureCache.ContainsKey(texturePath))
            {
                _textureCache[texturePath] = new Texture(texturePath);
            }

            return new SfmlSpriteAdapter(_textureCache[texturePath]);
        }

        public ISprite CreateSolidSprite(uint width, uint height, TlpColor color)
        {
            var image = new Image(width, height, new Color(color.R, color.G, color.B, color.A));
            return new SfmlSpriteAdapter(new Texture(image));
        }

        public void Resize(uint width, uint height)
        {
            if (_offscreen != null && _offscreenSprite != null)
            {
                var offscreenSize = _offscreen.Texture.Size;
                _offscreenSprite.Scale = new SFML.System.Vector2f(
                    width / (float)offscreenSize.X,
                    height / (float)offscreenSize.Y);
                return;
            }

            _window.SetView(new View(new FloatRect(0f, 0f, width, height)));
        }

        private Font? LoadDebugFont()
        {
            string windowsFontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            string[] candidates =
            {
                Path.Combine(windowsFontsDir, "consola.ttf"),
                Path.Combine(windowsFontsDir, "segoeui.ttf"),
                Path.Combine(windowsFontsDir, "arial.ttf")
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                return new Font(path);
            }

            return null;
        }
    }
}
