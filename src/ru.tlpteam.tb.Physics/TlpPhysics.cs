using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ru.tlpteam.tb.Core;
using ru.tlpteam.Debug;

namespace ru.tlpteam.tb.Physics
{
    public enum ColliderType
    {
        Box
    }

    public sealed class BoxCollider
    {
        public ColliderType Type { get; set; } = ColliderType.Box;
        public Vector2f Offset { get; set; } = new Vector2f(0f, 0f);
        public Vector2f Size { get; set; } = new Vector2f(0f, 0f);
        public bool IsTrigger { get; set; } = false;
        public bool Enabled { get; set; } = true;
    }

    internal readonly struct FloatRect
    {
        public readonly float Left;
        public readonly float Top;
        public readonly float Width;
        public readonly float Height;

        public float Right => Left + Width;
        public float Bottom => Top + Height;

        public FloatRect(float left, float top, float width, float height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }

    public static class TlpPhysics
    {
        internal static Func<IReadOnlyList<TestboxedScriptForObject>>? OnWorldObjectsRequest;
        private static readonly HashSet<CollisionPair> _lastFramePairs = new();
        public static int DebugLastCollisionPairs { get; private set; }
        public static int DebugLastCollisionChecks { get; private set; }

        public static List<TestboxedScriptForObject> Overlap(TestboxedScriptForObject self, string? type = null)
        {
            var result = new List<TestboxedScriptForObject>();
            var world = OnWorldObjectsRequest?.Invoke();
            if (world == null || self.Collider == null) return result;

            foreach (var other in world)
            {
                if (ReferenceEquals(other, self)) continue;
                if (!TypeMatches(other, type)) continue;
                if (IsCollidingAt(self, self.Position, other))
                    result.Add(other);
            }

            return result;
        }

        public static bool PlaceMeeting(TestboxedScriptForObject self, float x, float y, string? type = null)
        {
            var world = OnWorldObjectsRequest?.Invoke();
            if (world == null || self.Collider == null) return false;

            var testPosition = new Vector2f(x, y);
            foreach (var other in world)
            {
                if (ReferenceEquals(other, self)) continue;
                if (!TypeMatches(other, type)) continue;
                if (IsCollidingAt(self, testPosition, other))
                    return true;
            }

            return false;
        }

        internal static void ResolveWorldCollisions(
            IReadOnlyList<TestboxedScriptForObject> activeObjects,
            IReadOnlyDictionary<TestboxedScriptForObject, Vector2f> previousPositions)
        {
            DebugLastCollisionChecks = 0;

            foreach (var obj in activeObjects)
            {
                if (obj.Collider == null || !obj.Collider.Enabled || obj.IsStatic)
                    continue;

                var start = previousPositions.TryGetValue(obj, out var prevPos) ? prevPos : obj.Position;
                var desired = obj.Position;
                obj.Position = MoveWithSubsteps(obj, start, desired, activeObjects);
            }
        }

        private static Vector2f MoveWithSubsteps(
            TestboxedScriptForObject self,
            Vector2f start,
            Vector2f desired,
            IReadOnlyList<TestboxedScriptForObject> world)
        {
            float dx = desired.X - start.X;
            float dy = desired.Y - start.Y;

            int steps = (int)System.Math.Ceiling(System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)));
            if (steps <= 0) return start;

            float stepX = dx / steps;
            float stepY = dy / steps;

            var current = new Vector2f(start.X, start.Y);

            for (int i = 0; i < steps; i++)
            {
                var next = new Vector2f(current.X + stepX, current.Y + stepY);
                if (!WouldCollideWithSolid(self, next, world))
                {
                    current = next;
                    continue;
                }

                // Slide along free axis if diagonal move was blocked.
                var nextXOnly = new Vector2f(current.X + stepX, current.Y);
                if (!WouldCollideWithSolid(self, nextXOnly, world))
                {
                    current = nextXOnly;
                }

                var nextYOnly = new Vector2f(current.X, current.Y + stepY);
                if (!WouldCollideWithSolid(self, nextYOnly, world))
                {
                    current = nextYOnly;
                }
            }

            return current;
        }

        private static bool WouldCollideWithSolid(
            TestboxedScriptForObject self,
            Vector2f selfPosition,
            IReadOnlyList<TestboxedScriptForObject> world)
        {
            foreach (var other in world)
            {
                if (ReferenceEquals(other, self)) continue;
                if (other.Collider == null || !other.Collider.Enabled) continue;
                if (self.Collider!.IsTrigger || other.Collider.IsTrigger) continue;

                if (IsCollidingAt(self, selfPosition, other))
                    return true;
            }

            return false;
        }

        private static bool IsCollidingAt(
            TestboxedScriptForObject self,
            Vector2f selfPosition,
            TestboxedScriptForObject other)
        {
            DebugLastCollisionChecks++;

            if (self.Collider == null || !self.Collider.Enabled) return false;
            if (other.Collider == null || !other.Collider.Enabled) return false;
            if (self.Collider.Type != ColliderType.Box || other.Collider.Type != ColliderType.Box)
                return false;

            var a = BuildRect(self.Collider, selfPosition, self.Scale);
            var b = BuildRect(other.Collider, other.Position, other.Scale);

            return Intersects(a, b);
        }

        private static FloatRect BuildRect(BoxCollider collider, Vector2f position, Vector2f scale)
        {
            float scaleX = System.Math.Abs(scale.X) > 0.0001f ? scale.X : 1f;
            float scaleY = System.Math.Abs(scale.Y) > 0.0001f ? scale.Y : 1f;

            var width = System.Math.Abs(collider.Size.X * scaleX);
            var height = System.Math.Abs(collider.Size.Y * scaleY);
            return new FloatRect(
                position.X + collider.Offset.X * scaleX,
                position.Y + collider.Offset.Y * scaleY,
                width,
                height);
        }

        private static bool Intersects(FloatRect a, FloatRect b)
        {
            return a.Left < b.Right &&
                   a.Right > b.Left &&
                   a.Top < b.Bottom &&
                   a.Bottom > b.Top;
        }

        private static bool TypeMatches(TestboxedScriptForObject obj, string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return true;
            return string.Equals(obj.GetType().Name, type, StringComparison.Ordinal);
        }

        internal static void ProcessCollisionEvents(IReadOnlyList<TestboxedScriptForObject> activeObjects)
        {
            var currentFramePairs = new HashSet<CollisionPair>();

            for (int i = 0; i < activeObjects.Count; i++)
            {
                var a = activeObjects[i];
                if (a.Collider == null || !a.Collider.Enabled) continue;

                for (int j = i + 1; j < activeObjects.Count; j++)
                {
                    var b = activeObjects[j];
                    if (b.Collider == null || !b.Collider.Enabled) continue;
                    if (!IsCollidingAt(a, a.Position, b)) continue;

                    var pair = new CollisionPair(a, b);
                    currentFramePairs.Add(pair);

                    if (_lastFramePairs.Contains(pair))
                    {
                        SafeCall(() =>
                        {
                            a.OnCollisionStay(b);
                            b.OnCollisionStay(a);
                        }, "OnCollisionStay");
                    }
                    else
                    {
                        SafeCall(() =>
                        {
                            a.OnCollisionEnter(b);
                            b.OnCollisionEnter(a);
                        }, "OnCollisionEnter");
                    }
                }
            }

            foreach (var previousPair in _lastFramePairs)
            {
                if (currentFramePairs.Contains(previousPair)) continue;

                SafeCall(() =>
                {
                    previousPair.A.OnCollisionExit(previousPair.B);
                    previousPair.B.OnCollisionExit(previousPair.A);
                }, "OnCollisionExit");
            }

            _lastFramePairs.Clear();
            foreach (var pair in currentFramePairs) _lastFramePairs.Add(pair);

            DebugLastCollisionPairs = _lastFramePairs.Count;
        }

        private static void SafeCall(Action action, string eventName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                TlpLogging.Error($"Collision event '{eventName}' failed: {ex.Message}");
            }
        }
    }

    internal readonly struct CollisionPair : IEquatable<CollisionPair>
    {
        public readonly TestboxedScriptForObject A;
        public readonly TestboxedScriptForObject B;

        public CollisionPair(TestboxedScriptForObject first, TestboxedScriptForObject second)
        {
            if (RuntimeHelpers.GetHashCode(first) <= RuntimeHelpers.GetHashCode(second))
            {
                A = first;
                B = second;
            }
            else
            {
                A = second;
                B = first;
            }
        }

        public bool Equals(CollisionPair other) => ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B);
        public override bool Equals(object? obj) => obj is CollisionPair other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (RuntimeHelpers.GetHashCode(A) * 397) ^ RuntimeHelpers.GetHashCode(B);
            }
        }
    }
}
