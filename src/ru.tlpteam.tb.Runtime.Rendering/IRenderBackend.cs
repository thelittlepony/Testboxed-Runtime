using ru.tlpteam.tb.Core;
using ru.tlpteam.Input;

namespace ru.tlpteam.tb.Runtime.Rendering
{
    public interface IRenderBackend
    {
        uint ViewportWidth { get; }
        uint ViewportHeight { get; }

        void Clear(TlpColor clearColor);
        void Display();

        void Draw(ISprite sprite);
        void DrawDebugText(string text, float x, float y, TlpColor color, uint characterSize = 12);
        ISprite CreateSpriteFromTexture(string texturePath);
        ISprite CreateSolidSprite(uint width, uint height, TlpColor color);
    }
}
