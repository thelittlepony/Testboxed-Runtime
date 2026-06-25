using ru.tlpteam.tb.Core;
using ru.tlpteam.Input;
using ru.tlpteam.tb.Runtime.Rendering;
using System.Diagnostics;

namespace ru.tlpteam.tb.Runtime.Window
{
    public class DummySprite : ISprite
    {
        public Vector2f Position { get; set; }

        public Vector2f Scale { get; set; } = new(1f, 1f);

        public Vector2f Origin { get; set; }

        public Vector2f Size { get; }

        public DummySprite(uint width = 0, uint height = 0)
        {
            Size = new Vector2f(width, height);
        }
    }

    public class DummyWindowProvider : IWindowProvider
    {
        private readonly Stopwatch _clock = new();
        public bool IsOpen { get; private set; } = true;

        public float DeltaTime { get; private set; }

        public uint ViewportWidth { get; }
        public uint ViewportHeight { get; }

        public DummyWindowProvider(
            uint viewportWidth = 800,
            uint viewportHeight = 600)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;

            _clock.Start();
        }


        // Window

        public void SetTitle(string title)
        {
        }

        public void SetFramerateLimit(uint limit)
        {
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void DispatchEvents()
        {
        }

        public void UpdateDeltaTime()
        {
            DeltaTime = (float)_clock.Elapsed.TotalSeconds;
            _clock.Restart();
        }


        // Input

        public bool IsKeyPressed(TlpKey key)
        {
            return false;
        }

        public bool IsMouseButtonPressed(TlpMouseButton button)
        {
            return false;
        }

        public int GetMousePositionX()
        {
            return 0;
        }

        public int GetMousePositionY()
        {
            return 0;
        }


        // Rendering

        public void Clear(TlpColor clearColor)
        {
        }

        public void Display()
        {
        }

        public void Draw(ISprite sprite)
        {
        }

        public void DrawDebugText(
            string text,
            float x,
            float y,
            TlpColor color,
            uint characterSize = 12)
        {
        }


        public ISprite CreateSpriteFromTexture(string texturePath)
        {
            return new DummySprite();
        }

        public ISprite CreateSolidSprite(
            uint width,
            uint height,
            TlpColor color)
        {
            return new DummySprite();
        }
    }
}