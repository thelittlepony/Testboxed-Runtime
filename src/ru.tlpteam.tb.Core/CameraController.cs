using System;
using System.Linq;
using ru.tlpteam.tb.Math;
using ru.tlpteam.tb.Runtime.Engine;

namespace ru.tlpteam.tb.Core
{
    public class TestboxedCameraController : TestboxedScriptForObject
    {
        private const string ArgActive = "Active";
        private const string ArgFollowTargetType = "FollowTargetType";
        private const string ArgFollowTarget = "FollowTarget";
        private const string ArgSnapToTargetOnStart = "SnapToTargetOnStart";
        private const string ArgZoom = "Zoom";
        private const string ArgMinZoom = "MinZoom";
        private const string ArgMaxZoom = "MaxZoom";
        private const string ArgFollowLerp = "FollowLerp";
        private const string ArgFollowOffsetX = "FollowOffsetX";
        private const string ArgFollowOffsetY = "FollowOffsetY";
        private const string ArgClampToBounds = "ClampToBounds";
        private const string ArgBoundsMinX = "BoundsMinX";
        private const string ArgBoundsMinY = "BoundsMinY";
        private const string ArgBoundsMaxX = "BoundsMaxX";
        private const string ArgBoundsMaxY = "BoundsMaxY";

        private TestboxedScriptForObject? _followTarget;
        private bool _snapApplied;

        public bool Active = true;
        public bool FollowTarget = true;
        public bool SnapToTargetOnStart = true;
        public string FollowTargetType = string.Empty;
        public Vector2f FollowOffset = new Vector2f(0f, 0f);
        public float Zoom = 1f;
        public float MinZoom = 0.25f;
        public float MaxZoom = 4f;
        public float FollowLerp = 10f;
        public bool ClampToBounds = false;
        public Vector2f BoundsMin = new Vector2f(0f, 0f);
        public Vector2f BoundsMax = new Vector2f(0f, 0f);

        public override void Start()
        {
            LoadArgs();
            ResolveFollowTarget(force: true);
            ApplyTargetSnap();
            ClampZoom();
            ClampPositionToBounds();
        }

        public override void Update(float deltaTime)
        {
            if (!Active)
                return;

            LoadArgs();
            ClampZoom();

            if (FollowTarget)
            {
                ResolveFollowTarget(force: false);
                if (_followTarget != null)
                {
                    var targetPosition = new Vector2f(
                        _followTarget.Position.X + FollowOffset.X,
                        _followTarget.Position.Y + FollowOffset.Y);

                    if (!_snapApplied && SnapToTargetOnStart)
                    {
                        Position = targetPosition;
                        _snapApplied = true;
                    }
                    else
                    {
                        float lerpT = FollowLerp <= 0f
                            ? 1f
                            : 1f - (float)System.Math.Exp(-FollowLerp * System.Math.Max(0f, deltaTime));

                        Position = TlpMath.Lerp(Position, targetPosition, lerpT);
                    }
                }
            }

            ClampPositionToBounds();
        }

        public void SnapTo(Vector2f position, float zoom = 1f)
        {
            Position = position;
            Zoom = zoom;
            _snapApplied = true;
            ClampZoom();
            ClampPositionToBounds();
        }

        public void FocusOn(TestboxedScriptForObject target, bool snap = true)
        {
            _followTarget = target;
            FollowTarget = true;
            FollowTargetType = target.GetType().Name;
            if (snap)
            {
                Position = target.Position;
                _snapApplied = true;
            }
        }

        public void StopFollowing()
        {
            FollowTarget = false;
            _followTarget = null;
        }

        private void LoadArgs()
        {
            if (TryGetArg(ArgActive, out _))
                Active = GetBoolArg(ArgActive, Active);

            if (TryGetArg(ArgFollowTarget, out _))
                FollowTarget = GetBoolArg(ArgFollowTarget, FollowTarget);

            if (TryGetArg(ArgFollowTargetType, out _))
                FollowTargetType = GetStringArg(ArgFollowTargetType, FollowTargetType);

            if (TryGetArg(ArgSnapToTargetOnStart, out _))
                SnapToTargetOnStart = GetBoolArg(ArgSnapToTargetOnStart, SnapToTargetOnStart);

            if (TryGetArg(ArgZoom, out _))
                Zoom = GetFloatArg(ArgZoom, Zoom);

            if (TryGetArg(ArgMinZoom, out _))
                MinZoom = GetFloatArg(ArgMinZoom, MinZoom);

            if (TryGetArg(ArgMaxZoom, out _))
                MaxZoom = GetFloatArg(ArgMaxZoom, MaxZoom);

            if (TryGetArg(ArgFollowLerp, out _))
                FollowLerp = GetFloatArg(ArgFollowLerp, FollowLerp);

            if (TryGetArg(ArgFollowOffsetX, out _))
                FollowOffset = GetVector2Arg(ArgFollowOffsetX, ArgFollowOffsetY, FollowOffset);

            if (TryGetArg(ArgClampToBounds, out _))
                ClampToBounds = GetBoolArg(ArgClampToBounds, ClampToBounds);

            if (TryGetArg(ArgBoundsMinX, out _))
                BoundsMin = GetVector2Arg(ArgBoundsMinX, ArgBoundsMinY, BoundsMin);

            if (TryGetArg(ArgBoundsMaxX, out _))
                BoundsMax = GetVector2Arg(ArgBoundsMaxX, ArgBoundsMaxY, BoundsMax);
        }

        private void ResolveFollowTarget(bool force)
        {
            if (!force && _followTarget != null && string.IsNullOrWhiteSpace(FollowTargetType))
                return;

            if (string.IsNullOrWhiteSpace(FollowTargetType))
            {
                _followTarget = null;
                return;
            }

            _followTarget = TestboxedBridge.FindObjects(FollowTargetType).FirstOrDefault();
        }

        private void ApplyTargetSnap()
        {
            if (!SnapToTargetOnStart || _followTarget == null)
                return;

            Position = new Vector2f(
                _followTarget.Position.X + FollowOffset.X,
                _followTarget.Position.Y + FollowOffset.Y);
            _snapApplied = true;
        }

        private void ClampZoom()
        {
            if (MinZoom > MaxZoom)
                (MinZoom, MaxZoom) = (MaxZoom, MinZoom);

            Zoom = TlpMath.Clamp(Zoom, System.Math.Max(0.05f, MinZoom), System.Math.Max(0.05f, MaxZoom));
        }

        private void ClampPositionToBounds()
        {
            if (!ClampToBounds)
                return;

            var window = TestboxedBridge.WindowProvider;
            if (window == null)
                return;

            float halfVisibleWidth = window.ViewportWidth * 0.5f / System.Math.Max(0.05f, Zoom);
            float halfVisibleHeight = window.ViewportHeight * 0.5f / System.Math.Max(0.05f, Zoom);

            float minX = BoundsMin.X + halfVisibleWidth;
            float maxX = BoundsMax.X - halfVisibleWidth;
            float minY = BoundsMin.Y + halfVisibleHeight;
            float maxY = BoundsMax.Y - halfVisibleHeight;

            if (minX > maxX)
                Position.X = (BoundsMin.X + BoundsMax.X) * 0.5f;
            else
                Position.X = TlpMath.Clamp(Position.X, minX, maxX);

            if (minY > maxY)
                Position.Y = (BoundsMin.Y + BoundsMax.Y) * 0.5f;
            else
                Position.Y = TlpMath.Clamp(Position.Y, minY, maxY);
        }
    }
}
