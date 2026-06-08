using System;
using System.Collections.Generic;
using System.Globalization;
using ru.tlpteam.tb.Physics;

namespace ru.tlpteam.tb.Core
{
    public struct Vector2f
    {
        public float X;
        public float Y;

        public Vector2f(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public interface ISprite
    {
        Vector2f Position { get; set; }
        Vector2f Scale { get; set; }
        Vector2f Origin { get; set; }
        Vector2f Size { get; }
    }

    public abstract class TestboxedScriptForObject
    {
        public Vector2f Position;
        public float Rotation = 0f;
        public Vector2f Scale = new Vector2f(1f, 1f);
        public int Depth = 0;
        public RenderLayer Layer = RenderLayer.World;
        public bool Visible = true;
        public SpriteOriginMode SpriteOriginMode = SpriteOriginMode.TopLeft;
        public Vector2f SpriteCustomOrigin = new Vector2f(0f, 0f);
        public bool IsStatic = false;
        public BoxCollider? Collider;
        public ISprite? Sprite;
        public Dictionary<string, object> Args = new Dictionary<string, object>();

        public virtual void Start() { }
        public virtual void Update(float deltaTime) { }
        public virtual int GetDepth() => Depth;
        public virtual void OnCollisionEnter(TestboxedScriptForObject other) { }
        public virtual void OnCollisionStay(TestboxedScriptForObject other) { }
        public virtual void OnCollisionExit(TestboxedScriptForObject other) { }

        protected bool TryGetArg(string key, out object? value)
        {
            return Args.TryGetValue(key, out value);
        }

        protected string GetStringArg(string key, string defaultValue = "")
        {
            if (!Args.TryGetValue(key, out var raw) || raw == null)
                return defaultValue;

            return raw.ToString() ?? defaultValue;
        }

        protected float GetFloatArg(string key, float defaultValue = 0f)
        {
            if (!Args.TryGetValue(key, out var raw) || raw == null)
                return defaultValue;

            if (raw is float f) return f;
            if (raw is double d) return (float)d;
            if (raw is decimal m) return (float)m;
            if (raw is int i) return i;
            if (raw is long l) return l;

            if (raw is IConvertible convertible)
            {
                try
                {
                    return Convert.ToSingle(convertible, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (float.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedInvariant))
                return parsedInvariant;

            if (float.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.CurrentCulture, out float parsedCurrent))
                return parsedCurrent;

            return defaultValue;
        }

        protected bool GetBoolArg(string key, bool defaultValue = false)
        {
            if (!Args.TryGetValue(key, out var raw) || raw == null)
                return defaultValue;

            if (raw is bool b) return b;

            if (raw is IConvertible convertible)
            {
                try
                {
                    return Convert.ToBoolean(convertible, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (bool.TryParse(raw.ToString(), out bool parsed))
                return parsed;

            return defaultValue;
        }

        protected Vector2f GetVector2Arg(string xKey, string yKey, Vector2f defaultValue)
        {
            return new Vector2f(
                GetFloatArg(xKey, defaultValue.X),
                GetFloatArg(yKey, defaultValue.Y));
        }
    }
}
