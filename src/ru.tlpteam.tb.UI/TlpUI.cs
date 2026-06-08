using System;
using System.Collections.Generic;
using System.Linq;
using ru.tlpteam.Input;
using ru.tlpteam.tb.Core;
using ru.tlpteam.tb.Runtime.Rendering;

namespace ru.tlpteam.tb.UI
{
    public enum Anchor
    {
        TopLeft = 0,
        TopCenter = 1,
        TopRight = 2,
        MiddleLeft = 3,
        Center = 4,
        MiddleRight = 5,
        BottomLeft = 6,
        BottomCenter = 7,
        BottomRight = 8
    }

    public abstract class UiElement
    {
        public string Id { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public bool Interactable { get; set; } = false;
        public int ZIndex { get; set; } = 0;
        public Anchor Anchor { get; set; } = Anchor.TopLeft;
        public Vector2f Position { get; set; } = new Vector2f(0f, 0f);
        public Vector2f Size { get; set; } = new Vector2f(0f, 0f);

        public event Action<UiElement>? PointerEnter;
        public event Action<UiElement>? PointerExit;
        public event Action<UiElement>? PointerDown;
        public event Action<UiElement>? PointerUp;
        public event Action<UiElement>? Click;

        internal UiElement? Parent { get; set; }

        public virtual IReadOnlyList<UiElement> Children => Array.Empty<UiElement>();

        internal virtual bool HitTest(Vector2f localPoint)
        {
            return localPoint.X >= 0f && localPoint.Y >= 0f &&
                   localPoint.X <= Size.X && localPoint.Y <= Size.Y;
        }

        internal abstract void Draw(IRenderBackend renderBackend, Vector2f absolutePosition);

        internal void RaisePointerEnter() => PointerEnter?.Invoke(this);
        internal void RaisePointerExit() => PointerExit?.Invoke(this);
        internal void RaisePointerDown() => PointerDown?.Invoke(this);
        internal void RaisePointerUp() => PointerUp?.Invoke(this);
        internal void RaiseClick() => Click?.Invoke(this);
    }

    public sealed class Panel : UiElement
    {
        public TlpColor BackgroundColor { get; set; } = new TlpColor(0, 0, 0, 0);
        private readonly List<UiElement> _children = new();

        public override IReadOnlyList<UiElement> Children => _children;

        public void Add(UiElement child)
        {
            if (child == null) return;
            child.Parent = this;
            _children.Add(child);
        }

        public bool Remove(UiElement child)
        {
            if (!_children.Remove(child)) return false;
            child.Parent = null;
            return true;
        }

        public void ClearChildren()
        {
            foreach (var c in _children) c.Parent = null;
            _children.Clear();
        }

        internal override void Draw(IRenderBackend renderBackend, Vector2f absolutePosition)
        {
            uint width = (uint)System.Math.Max(1f, System.Math.Abs(Size.X));
            uint height = (uint)System.Math.Max(1f, System.Math.Abs(Size.Y));
            var sprite = UiSpriteCache.GetSolidSprite(renderBackend, width, height, BackgroundColor);
            sprite.Position = absolutePosition;
            sprite.Scale = new Vector2f(1f, 1f);
            sprite.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(sprite);
        }
    }

    public sealed class Text : UiElement
    {
        public string Value { get; set; } = string.Empty;
        public TlpColor Color { get; set; } = new TlpColor(255, 255, 255, 255);
        public uint CharacterSize { get; set; } = 12;

        internal override bool HitTest(Vector2f localPoint)
        {
            if (Size.X <= 0f || Size.Y <= 0f) return false;
            return base.HitTest(localPoint);
        }

        internal override void Draw(IRenderBackend renderBackend, Vector2f absolutePosition)
        {
            renderBackend.DrawDebugText(Value, absolutePosition.X, absolutePosition.Y, Color, CharacterSize);
        }
    }

    public sealed class Sparkline : UiElement
    {
        private float[] _primaryValues = Array.Empty<float>();
        private float[] _secondaryValues = Array.Empty<float>();

        public TlpColor BackgroundColor { get; set; } = new TlpColor(22, 22, 22, 210);
        public TlpColor PrimaryColor { get; set; } = new TlpColor(255, 188, 107, 255);
        public TlpColor SecondaryColor { get; set; } = new TlpColor(124, 214, 255, 255);

        public void SetSeries(IReadOnlyList<float> primaryValues, IReadOnlyList<float>? secondaryValues = null)
        {
            _primaryValues = primaryValues?.ToArray() ?? Array.Empty<float>();
            _secondaryValues = secondaryValues?.ToArray() ?? Array.Empty<float>();
        }

        internal override void Draw(IRenderBackend renderBackend, Vector2f absolutePosition)
        {
            uint width = (uint)System.Math.Max(1f, System.Math.Abs(Size.X));
            uint height = (uint)System.Math.Max(1f, System.Math.Abs(Size.Y));

            var bg = UiSpriteCache.GetSolidSprite(renderBackend, width, height, BackgroundColor);
            bg.Position = absolutePosition;
            bg.Scale = new Vector2f(1f, 1f);
            bg.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(bg);

            int count = System.Math.Min(_primaryValues.Length, _secondaryValues.Length > 0 ? _secondaryValues.Length : _primaryValues.Length);
            if (count < 2) return;

            float min = _primaryValues.Min();
            float max = _primaryValues.Max();
            if (_secondaryValues.Length > 0)
            {
                min = System.Math.Min(min, _secondaryValues.Min());
                max = System.Math.Max(max, _secondaryValues.Max());
            }

            float range = System.Math.Max(1f, max - min);
            uint innerW = width > 2 ? width - 2 : 1;
            uint innerH = height > 2 ? height - 2 : 1;

            DrawSeries(renderBackend, absolutePosition, _primaryValues, count, min, range, innerW, innerH, PrimaryColor);
            if (_secondaryValues.Length > 1)
                DrawSeries(renderBackend, absolutePosition, _secondaryValues, count, min, range, innerW, innerH, SecondaryColor);
        }

        private static void DrawSeries(
            IRenderBackend renderBackend,
            Vector2f absolutePosition,
            float[] values,
            int count,
            float min,
            float range,
            uint innerW,
            uint innerH,
            TlpColor color)
        {
            float prevX = absolutePosition.X + 1f;
            float prevY = absolutePosition.Y + 1f + innerH - 1f - ((values[0] - min) / range) * (innerH - 1f);

            for (int i = 1; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float x = absolutePosition.X + 1f + t * (innerW - 1f);
                float y = absolutePosition.Y + 1f + innerH - 1f - ((values[i] - min) / range) * (innerH - 1f);

                DrawLine(renderBackend, prevX, prevY, x, y, color);
                prevX = x;
                prevY = y;
            }
        }

        private static void DrawLine(IRenderBackend renderBackend, float x0, float y0, float x1, float y1, TlpColor color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            int steps = (int)System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
            if (steps <= 0)
            {
                DrawPixel(renderBackend, x0, y0, color);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawPixel(renderBackend, x0 + dx * t, y0 + dy * t, color);
            }
        }

        private static void DrawPixel(IRenderBackend renderBackend, float x, float y, TlpColor color)
        {
            var pixel = UiSpriteCache.GetSolidSprite(renderBackend, 1, 1, color);
            pixel.Position = new Vector2f((float)System.Math.Round(x), (float)System.Math.Round(y));
            pixel.Scale = new Vector2f(1f, 1f);
            pixel.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(pixel);
        }
    }

    public sealed class UiCanvas
    {
        private readonly List<UiElement> _roots = new();
        private UiElement? _hoveredElement;
        private UiElement? _pressedElement;

        private readonly struct LayoutNode
        {
            public readonly UiElement Element;
            public readonly Vector2f AbsolutePosition;
            public readonly Vector2f Size;
            public readonly int SortKey;

            public LayoutNode(UiElement element, Vector2f absolutePosition, Vector2f size, int sortKey)
            {
                Element = element;
                AbsolutePosition = absolutePosition;
                Size = size;
                SortKey = sortKey;
            }
        }

        public IReadOnlyList<UiElement> Roots => _roots;

        public void Add(UiElement element)
        {
            if (element == null) return;
            element.Parent = null;
            _roots.Add(element);
        }

        public bool Remove(UiElement element)
        {
            return _roots.Remove(element);
        }

        public void Clear()
        {
            _roots.Clear();
            _hoveredElement = null;
            _pressedElement = null;
        }

        public void Render(IRenderBackend renderBackend)
        {
            if (_roots.Count == 0) return;

            var layout = BuildLayout(renderBackend);
            RouteInput(layout);

            foreach (var node in layout.OrderBy(n => n.SortKey))
            {
                if (!node.Element.Visible) continue;
                node.Element.Draw(renderBackend, node.AbsolutePosition);
            }
        }

        private List<LayoutNode> BuildLayout(IRenderBackend renderBackend)
        {
            var nodes = new List<LayoutNode>(_roots.Count * 8);
            int traversalIndex = 0;
            var viewportSize = new Vector2f(renderBackend.ViewportWidth, renderBackend.ViewportHeight);

            foreach (var root in _roots)
            {
                CollectLayoutRecursive(
                    root,
                    parentPosition: new Vector2f(0f, 0f),
                    parentSize: viewportSize,
                    depth: 0,
                    ref traversalIndex,
                    nodes);
            }

            return nodes;
        }

        private void CollectLayoutRecursive(
            UiElement element,
            Vector2f parentPosition,
            Vector2f parentSize,
            int depth,
            ref int traversalIndex,
            List<LayoutNode> nodes)
        {
            if (!element.Visible) return;

            var anchoredPosition = ResolveAnchoredPosition(parentPosition, parentSize, element.Size, element.Anchor);
            var absolutePosition = new Vector2f(
                anchoredPosition.X + element.Position.X,
                anchoredPosition.Y + element.Position.Y);

            int sortKey = depth * 100000 + element.ZIndex * 1000 + traversalIndex++;
            nodes.Add(new LayoutNode(element, absolutePosition, element.Size, sortKey));

            foreach (var child in element.Children)
            {
                if (child == null) continue;
                CollectLayoutRecursive(
                    child,
                    absolutePosition,
                    element.Size,
                    depth + 1,
                    ref traversalIndex,
                    nodes);
            }
        }

        private static Vector2f ResolveAnchoredPosition(Vector2f parentPosition, Vector2f parentSize, Vector2f elementSize, Anchor anchor)
        {
            float x = parentPosition.X;
            float y = parentPosition.Y;

            switch (anchor)
            {
                case Anchor.TopLeft:
                    break;
                case Anchor.TopCenter:
                    x += parentSize.X * 0.5f - elementSize.X * 0.5f;
                    break;
                case Anchor.TopRight:
                    x += parentSize.X - elementSize.X;
                    break;
                case Anchor.MiddleLeft:
                    y += parentSize.Y * 0.5f - elementSize.Y * 0.5f;
                    break;
                case Anchor.Center:
                    x += parentSize.X * 0.5f - elementSize.X * 0.5f;
                    y += parentSize.Y * 0.5f - elementSize.Y * 0.5f;
                    break;
                case Anchor.MiddleRight:
                    x += parentSize.X - elementSize.X;
                    y += parentSize.Y * 0.5f - elementSize.Y * 0.5f;
                    break;
                case Anchor.BottomLeft:
                    y += parentSize.Y - elementSize.Y;
                    break;
                case Anchor.BottomCenter:
                    x += parentSize.X * 0.5f - elementSize.X * 0.5f;
                    y += parentSize.Y - elementSize.Y;
                    break;
                case Anchor.BottomRight:
                    x += parentSize.X - elementSize.X;
                    y += parentSize.Y - elementSize.Y;
                    break;
            }

            return new Vector2f(x, y);
        }

        private void RouteInput(List<LayoutNode> layout)
        {
            var mousePosition = new Vector2f(TlpInput.Mouse.GetX(), TlpInput.Mouse.GetY());
            UiElement? hit = null;

            foreach (var node in layout.OrderByDescending(n => n.SortKey))
            {
                if (!node.Element.Visible || !node.Element.Enabled || !node.Element.Interactable) continue;

                var localPoint = new Vector2f(
                    mousePosition.X - node.AbsolutePosition.X,
                    mousePosition.Y - node.AbsolutePosition.Y);

                if (node.Element.HitTest(localPoint))
                {
                    hit = node.Element;
                    break;
                }
            }

            if (!ReferenceEquals(hit, _hoveredElement))
            {
                _hoveredElement?.RaisePointerExit();
                _hoveredElement = hit;
                _hoveredElement?.RaisePointerEnter();
            }

            if (TlpInput.Mouse.GetButtonDown(TlpMouseButton.Left) && _hoveredElement != null)
            {
                _pressedElement = _hoveredElement;
                _pressedElement.RaisePointerDown();
            }

            if (TlpInput.Mouse.GetButtonUp(TlpMouseButton.Left))
            {
                if (_pressedElement != null)
                {
                    _pressedElement.RaisePointerUp();
                    if (ReferenceEquals(_pressedElement, _hoveredElement))
                        _pressedElement.RaiseClick();
                }
                _pressedElement = null;
            }
        }
    }

    public static class TlpUI
    {
        private static readonly UiCanvas _canvas = new();

        public static UiCanvas Canvas => _canvas;

        public static void Render(IRenderBackend renderBackend)
        {
            _canvas.Render(renderBackend);
        }
    }

    internal static class UiSpriteCache
    {
        private static readonly Dictionary<string, ISprite> _cache = new();

        public static ISprite GetSolidSprite(IRenderBackend renderBackend, uint width, uint height, TlpColor color)
        {
            string key = $"{width}x{height}:{color.R},{color.G},{color.B},{color.A}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var created = renderBackend.CreateSolidSprite(width, height, color);
            _cache[key] = created;
            return created;
        }
    }
}
