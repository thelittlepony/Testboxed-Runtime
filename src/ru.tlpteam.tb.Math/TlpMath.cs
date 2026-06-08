using ru.tlpteam.tb.Core;

namespace ru.tlpteam.tb.Math
{
    public static class TlpMath
    {
        public static float Clamp(float value, float min, float max)
        {
            if (min > max) (min, max) = (max, min);
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp(t, 0f, 1f);
        }

        public static Vector2f Lerp(Vector2f a, Vector2f b, float t)
        {
            t = Clamp(t, 0f, 1f);
            return new Vector2f(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t);
        }

        public static float InverseLerp(float a, float b, float value)
        {
            if (System.Math.Abs(b - a) < 0.000001f) return 0f;
            return Clamp((value - a) / (b - a), 0f, 1f);
        }

        public static float Remap(float inMin, float inMax, float outMin, float outMax, float value)
        {
            float t = InverseLerp(inMin, inMax, value);
            return Lerp(outMin, outMax, t);
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (maxDelta < 0f) maxDelta = -maxDelta;
            if (System.Math.Abs(target - current) <= maxDelta) return target;
            return current + System.Math.Sign(target - current) * maxDelta;
        }

        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = InverseLerp(edge0, edge1, x);
            return t * t * (3f - 2f * t);
        }
    }
}
