namespace ru.tlpteam.tb.Core
{
    public static class TestboxedMapTypeNames
    {
        public const string TestboxedLikeMap = "ru.tlpteam.tb.Types.TestboxedLikeMap";
    }

    public sealed class ObjectInstance
    {
        public Vector2f Position { get; set; } = new Vector2f(0f, 0f);
        public float Rotation { get; set; } = 0f;
        public Vector2f Scale { get; set; } = new Vector2f(1f, 1f);
        public int Depth { get; set; } = 0;
        public object? BaseObject { get; set; }
    }

    public sealed class MapSettings
    {
        public BackgroundSettings Background { get; set; } = new BackgroundSettings();
        public Vector2f Scale { get; set; } = new Vector2f(1f, 1f);

        public sealed class BackgroundSettings
        {
            public string FillWith { get; set; } = "ColorHex";
            public string Value { get; set; } = "#000000";
        }
    }
}
