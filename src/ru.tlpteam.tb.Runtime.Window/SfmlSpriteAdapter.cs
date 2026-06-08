using SFML.Graphics;
using ru.tlpteam.tb.Core;

namespace ru.tlpteam.tb.Runtime.Rendering.Backends
{
    public sealed class SfmlSpriteAdapter : ISprite
    {
        public Sprite NativeSprite { get; }

        public ru.tlpteam.tb.Core.Vector2f Position
        {
            get
            {
                var pos = NativeSprite.Position;
                return new ru.tlpteam.tb.Core.Vector2f(pos.X, pos.Y);
            }
            set => NativeSprite.Position = new SFML.System.Vector2f(value.X, value.Y);
        }

        public ru.tlpteam.tb.Core.Vector2f Scale
        {
            get
            {
                var scale = NativeSprite.Scale;
                return new ru.tlpteam.tb.Core.Vector2f(scale.X, scale.Y);
            }
            set => NativeSprite.Scale = new SFML.System.Vector2f(value.X, value.Y);
        }

        public ru.tlpteam.tb.Core.Vector2f Origin
        {
            get
            {
                var origin = NativeSprite.Origin;
                return new ru.tlpteam.tb.Core.Vector2f(origin.X, origin.Y);
            }
            set => NativeSprite.Origin = new SFML.System.Vector2f(value.X, value.Y);
        }

        public ru.tlpteam.tb.Core.Vector2f Size
        {
            get
            {
                var bounds = NativeSprite.GetLocalBounds();
                return new ru.tlpteam.tb.Core.Vector2f(bounds.Width, bounds.Height);
            }
        }

        public SfmlSpriteAdapter(Texture texture)
        {
            NativeSprite = new Sprite(texture);
        }
    }
}
