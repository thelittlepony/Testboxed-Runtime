using ru.tlpteam.Debug;
using ru.tlpteam.tb.Core;
using ru.tlpteam.tb.Runtime.Engine;
using ru.tlpteam.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ru.tlpteam.tb.Runtime.Window;
using ru.tlpteam.tb.UI;

namespace ru.tlpteam.tb.Runtime.Rendering
{
    public static class RenderEngine
    {
        private static readonly Dictionary<string, ISprite> _debugSpriteCache = new();
        private static readonly Queue<float> _ramWsHistory = new();
        private static readonly Queue<float> _ramGcHistory = new();
        private const int MemoryGraphCapacity = 160;
        private static bool _debugUiInitialized;
        private static Panel? _debugPanel;
        private static Text? _debugTitleText;
        private static Text? _debugPerfText;
        private static Text? _debugViewportText;
        private static Text? _debugObjectsText;
        private static Text? _debugCollidersText;
        private static Text? _debugSceneText;
        private static Text? _debugInputText;
        private static Text? _debugCameraText;
        private static Text? _debugRamText;
        private static Text? _debugControlsText;
        private static Text? _debugWsLegend;
        private static Text? _debugGcLegend;
        private static Sparkline? _debugRamGraph;

        /// <summary>
        /// Main render pass with camera transform, layered rendering and optional debug overlay.
        /// </summary>
        public static void _Render(
            IRenderBackend renderBackend,
            List<TestboxedScriptForObject> activeObjects,
            bool drawPhysicsDebug = false,
            bool drawDebugPanel = false,
            float fps = 0f,
            int collisionPairs = 0,
            int collisionChecks = 0,
            bool fpsLockEnabled = true)
        {
            if (renderBackend == null) return;

            renderBackend.Clear(new TlpColor(0,0,0));

            float zoom = System.Math.Max(0.05f, TestboxedBridge.CameraZoom);
            float halfW = renderBackend.ViewportWidth * 0.5f;
            float halfH = renderBackend.ViewportHeight * 0.5f;
            var cam = TestboxedBridge.CameraPosition;

            // Layer first, then depth ordering inside each layer.
            var sortedObjects = activeObjects
                .Where(o => o.Visible)
                .OrderBy(o => o.Layer)
                .ThenByDescending(o => o.GetDepth());

            foreach (var obj in sortedObjects)
            {
                if (obj.Sprite == null) continue;

                bool uiLayer = obj.Layer == RenderLayer.UI;
                obj.Sprite.Origin = ResolveSpriteOrigin(obj);
                obj.Sprite.Position = uiLayer
                    ? obj.Position
                    : WorldToScreen(obj.Position, cam, zoom, halfW, halfH);
                obj.Sprite.Scale = uiLayer
                    ? obj.Scale
                    : new Vector2f(obj.Scale.X * zoom, obj.Scale.Y * zoom);

                renderBackend.Draw(obj.Sprite);
            }

            if (drawPhysicsDebug)
                DrawPhysicsDebug(renderBackend, activeObjects, cam, zoom, halfW, halfH);

            EnsureDebugUiInitialized();
            if (drawDebugPanel)
            {
                DrawDebugPanel(renderBackend, activeObjects, fps, cam, zoom, collisionPairs, collisionChecks, fpsLockEnabled);
            }
            else if (_debugPanel != null)
            {
                _debugPanel.Visible = false;
            }

            TlpUI.Render(renderBackend);

            renderBackend.Display();
        }

        private static void DrawPhysicsDebug(
            IRenderBackend renderBackend,
            List<TestboxedScriptForObject> activeObjects,
            Vector2f cam,
            float zoom,
            float halfW,
            float halfH)
        {
            foreach (var obj in activeObjects)
            {
                DrawObjectOrigin(renderBackend, obj, cam, zoom, halfW, halfH);
                DrawObjectDebugLabel(renderBackend, obj, obj.Layer == RenderLayer.UI ? obj.Position : WorldToScreen(obj.Position, cam, zoom, halfW, halfH));

                var collider = obj.Collider;
                if (collider == null || !collider.Enabled) continue;

                float effectiveColliderWidth = System.Math.Abs(collider.Size.X * obj.Scale.X);
                float effectiveColliderHeight = System.Math.Abs(collider.Size.Y * obj.Scale.Y);
                float effectiveOffsetX = collider.Offset.X * obj.Scale.X;
                float effectiveOffsetY = collider.Offset.Y * obj.Scale.Y;

                uint width = (uint)System.Math.Max(1f, effectiveColliderWidth * zoom);
                uint height = (uint)System.Math.Max(1f, effectiveColliderHeight * zoom);
                var worldLeftTop = new Vector2f(obj.Position.X + effectiveOffsetX, obj.Position.Y + effectiveOffsetY);
                var leftTop = WorldToScreen(worldLeftTop, cam, zoom, halfW, halfH);

                var fillColor = collider.IsTrigger
                    ? new TlpColor(255, 165, 0, 120)
                    : obj.IsStatic
                        ? new TlpColor(46, 204, 113, 120)
                        : new TlpColor(231, 76, 60, 120);

                var sprite = GetDebugSprite(renderBackend, width, height, fillColor);
                sprite.Position = leftTop;
                sprite.Scale = new Vector2f(1f, 1f);
                sprite.Origin = new Vector2f(0f, 0f);
                renderBackend.Draw(sprite);
            }

            DrawCameraCenter(renderBackend, halfW, halfH);
        }

        private static void DrawObjectOrigin(
            IRenderBackend renderBackend,
            TestboxedScriptForObject obj,
            Vector2f cam,
            float zoom,
            float halfW,
            float halfH)
        {
            var originColor = obj.Layer == RenderLayer.UI ? new TlpColor(200, 200, 255, 180) : new TlpColor(255, 255, 0, 180);
            var worldOrScreenPos = obj.Layer == RenderLayer.UI
                ? obj.Position
                : WorldToScreen(obj.Position, cam, zoom, halfW, halfH);

            var marker = GetDebugSprite(renderBackend, 4, 4, originColor);
            marker.Position = new Vector2f(worldOrScreenPos.X - 2f, worldOrScreenPos.Y - 2f);
            marker.Scale = new Vector2f(1f, 1f);
            marker.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(marker);
        }

        private static void DrawCameraCenter(IRenderBackend renderBackend, float halfW, float halfH)
        {
            var h = GetDebugSprite(renderBackend, 16, 2, new TlpColor(255, 255, 255, 180));
            h.Position = new Vector2f(halfW - 8f, halfH - 1f);
            h.Scale = new Vector2f(1f, 1f);
            h.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(h);

            var v = GetDebugSprite(renderBackend, 2, 16, new TlpColor(255, 255, 255, 180));
            v.Position = new Vector2f(halfW - 1f, halfH - 8f);
            v.Scale = new Vector2f(1f, 1f);
            v.Origin = new Vector2f(0f, 0f);
            renderBackend.Draw(v);
        }

        private static Vector2f WorldToScreen(Vector2f world, Vector2f cam, float zoom, float halfW, float halfH)
        {
            return new Vector2f(
                (world.X - cam.X) * zoom + halfW,
                (world.Y - cam.Y) * zoom + halfH);
        }

        private static ISprite GetDebugSprite(IRenderBackend renderBackend, uint width, uint height, TlpColor color)
        {
            string key = $"{width}x{height}:{color.R},{color.G},{color.B},{color.A}";
            if (_debugSpriteCache.TryGetValue(key, out var cached))
                return cached;

            var created = renderBackend.CreateSolidSprite(width, height, color);
            _debugSpriteCache[key] = created;
            return created;
        }

        private static Vector2f ResolveSpriteOrigin(TestboxedScriptForObject obj)
        {
            if (obj.Sprite == null) return new Vector2f(0f, 0f);

            var size = obj.Sprite.Size;
            return obj.SpriteOriginMode switch
            {
                SpriteOriginMode.Center => new Vector2f(size.X * 0.5f, size.Y * 0.5f),
                SpriteOriginMode.Custom => obj.SpriteCustomOrigin,
                _ => new Vector2f(0f, 0f),
            };
        }

        private static void DrawObjectDebugLabel(IRenderBackend renderBackend, TestboxedScriptForObject obj, Vector2f screenLeftTop)
        {
            string colliderText = obj.Collider == null
                ? "no-col"
                : $"{System.Math.Abs(obj.Collider.Size.X * obj.Scale.X):0.#}x{System.Math.Abs(obj.Collider.Size.Y * obj.Scale.Y):0.#}";

            string label = $"{obj.GetType().Name} L={obj.Layer} D={obj.GetDepth()} P=({obj.Position.X:0.#},{obj.Position.Y:0.#}) C={colliderText}";
            renderBackend.DrawDebugText(label, screenLeftTop.X + 4f, screenLeftTop.Y - 14f, new TlpColor(255, 255, 255, 220), 11);
        }

        private static void DrawDebugPanel(
            IRenderBackend renderBackend,
            List<TestboxedScriptForObject> activeObjects,
            float fps,
            Vector2f cameraPosition,
            float zoom,
            int collisionPairs,
            int collisionChecks,
            bool fpsLockEnabled)
        {
            int objectCount = activeObjects.Count;
            int visibleCount = activeObjects.Count(o => o.Visible);
            int uiCount = activeObjects.Count(o => o.Layer == RenderLayer.UI);
            int worldCount = activeObjects.Count(o => o.Layer == RenderLayer.World);
            int backgroundCount = activeObjects.Count(o => o.Layer == RenderLayer.Background);
            int colliderEnabledCount = activeObjects.Count(o => o.Collider != null && o.Collider.Enabled);
            int triggerCount = activeObjects.Count(o => o.Collider != null && o.Collider.Enabled && o.Collider.IsTrigger);
            int staticCount = activeObjects.Count(o => o.IsStatic);
            int hiddenCount = activeObjects.Count(o => !o.Visible);
            var camera = TestboxedBridge.ActiveCamera;

            float frameMs = fps > 0.001f ? 1000f / fps : 0f;
            int mouseX = TestboxedBridge.WindowProvider?.GetMousePositionX() ?? 0;
            int mouseY = TestboxedBridge.WindowProvider?.GetMousePositionY() ?? 0;
            string cameraText = TestboxedBridge.ActiveCamera == null
                ? "Cam none"
                : $"Cam {TestboxedBridge.ActiveCamera.GetType().Name} ({TestboxedBridge.ActiveCamera.Position.X:0.#},{TestboxedBridge.ActiveCamera.Position.Y:0.#}) Z {TestboxedBridge.ActiveCamera.Zoom:0.00}";
            string inputText = BuildInputSummary(mouseX, mouseY);
            string sceneText = $"Scene objs:{objectCount} vis:{visibleCount} hid:{hiddenCount} bg:{backgroundCount} world:{worldCount} ui:{uiCount}";
            string cameraSummaryText = BuildCameraSummary(camera, cameraPosition, zoom);
            float ramWorkingSetMb = Process.GetCurrentProcess().WorkingSet64 / (1024f * 1024f);
            float ramManagedMb = GC.GetTotalMemory(false) / (1024f * 1024f);
            PushMemorySample(ramWorkingSetMb, ramManagedMb);

            if (_debugPanel == null ||
                _debugTitleText == null ||
                _debugPerfText == null ||
                _debugViewportText == null ||
                _debugObjectsText == null ||
                _debugCollidersText == null ||
                _debugSceneText == null ||
                _debugInputText == null ||
                _debugCameraText == null ||
                _debugRamText == null ||
                _debugControlsText == null ||
                _debugWsLegend == null ||
                _debugGcLegend == null ||
                _debugRamGraph == null)
                return;

            _debugPanel.Visible = true;
            _debugTitleText.Value = "DEBUG (F3)";
            _debugPerfText.Value = $"FPS {fps:0.0} | {frameMs:0.00}ms | DT {TestboxedBridge.DeltaTime:0.0000}s | Lock {(fpsLockEnabled ? "ON" : "OFF")}";
            _debugViewportText.Value = $"VP {renderBackend.ViewportWidth}x{renderBackend.ViewportHeight} | Mouse {mouseX},{mouseY} | Zoom {zoom:0.00} | {cameraText}";
            _debugSceneText.Value = sceneText;
            _debugObjectsText.Value = $"Obj total:{objectCount} hidden:{hiddenCount}";
            _debugCollidersText.Value = $"Col E:{colliderEnabledCount} Tr:{triggerCount} St:{staticCount} P:{collisionPairs} C:{collisionChecks}";
            _debugInputText.Value = inputText;
            _debugCameraText.Value = cameraSummaryText;
            _debugRamText.Value = $"RAM WS:{ramWorkingSetMb:0.0}MB | GC:{ramManagedMb:0.0}MB";
            _debugControlsText.Value = $"F5 reset | F6/F7 zoom | F8 fps lock {(fpsLockEnabled ? "off" : "on")} | Arrows pan";
            _debugWsLegend.Value = "WS";
            _debugGcLegend.Value = "GC";
            _debugRamGraph.SetSeries(_ramWsHistory.ToArray(), _ramGcHistory.ToArray());
        }

        private static void PushMemorySample(float wsMb, float gcMb)
        {
            _ramWsHistory.Enqueue(wsMb);
            _ramGcHistory.Enqueue(gcMb);

            while (_ramWsHistory.Count > MemoryGraphCapacity) _ramWsHistory.Dequeue();
            while (_ramGcHistory.Count > MemoryGraphCapacity) _ramGcHistory.Dequeue();
        }

        private static void EnsureDebugUiInitialized()
        {
            if (_debugUiInitialized) return;

            _debugPanel = new Panel
            {
                Id = "debug-panel",
                Anchor = Anchor.TopLeft,
                Position = new Vector2f(8f, 8f),
                Size = new Vector2f(468f, 156f),
                BackgroundColor = new TlpColor(0, 0, 0, 180),
                ZIndex = 10000,
                Visible = false
            };

            _debugTitleText = CreateDebugText("debug-title", new Vector2f(6f, 4f), 12, new TlpColor(120, 255, 120, 240));
            _debugPerfText = CreateDebugText("debug-perf", new Vector2f(6f, 19f), 11, new TlpColor(255, 255, 255, 230));
            _debugViewportText = CreateDebugText("debug-vp", new Vector2f(6f, 33f), 11, new TlpColor(255, 255, 255, 230));
            _debugSceneText = CreateDebugText("debug-scene", new Vector2f(6f, 47f), 11, new TlpColor(255, 255, 255, 230));
            _debugObjectsText = CreateDebugText("debug-obj", new Vector2f(6f, 61f), 11, new TlpColor(255, 255, 255, 230));
            _debugCollidersText = CreateDebugText("debug-col", new Vector2f(6f, 75f), 11, new TlpColor(255, 255, 255, 230));
            _debugInputText = CreateDebugText("debug-input", new Vector2f(6f, 89f), 11, new TlpColor(200, 220, 255, 230));
            _debugCameraText = CreateDebugText("debug-cam", new Vector2f(6f, 103f), 11, new TlpColor(255, 225, 170, 230));
            _debugRamText = CreateDebugText("debug-ram", new Vector2f(6f, 117f), 11, new TlpColor(255, 225, 170, 230));
            _debugControlsText = CreateDebugText("debug-ctl", new Vector2f(6f, 131f), 10, new TlpColor(200, 220, 255, 230));

            _debugRamGraph = new Sparkline
            {
                Id = "debug-ram-graph",
                Anchor = Anchor.TopLeft,
                Position = new Vector2f(268f, 104f),
                Size = new Vector2f(192f, 40f),
                ZIndex = 2
            };

            _debugWsLegend = CreateDebugText("debug-ws", new Vector2f(272f, 106f), 9, new TlpColor(255, 188, 107, 240));
            _debugGcLegend = CreateDebugText("debug-gc", new Vector2f(296f, 106f), 9, new TlpColor(124, 214, 255, 240));

            _debugPanel.Add(_debugTitleText);
            _debugPanel.Add(_debugPerfText);
            _debugPanel.Add(_debugViewportText);
            _debugPanel.Add(_debugSceneText);
            _debugPanel.Add(_debugObjectsText);
            _debugPanel.Add(_debugCollidersText);
            _debugPanel.Add(_debugInputText);
            _debugPanel.Add(_debugCameraText);
            _debugPanel.Add(_debugRamText);
            _debugPanel.Add(_debugControlsText);
            _debugPanel.Add(_debugRamGraph);
            _debugPanel.Add(_debugWsLegend);
            _debugPanel.Add(_debugGcLegend);

            TlpUI.Canvas.Add(_debugPanel);
            _debugUiInitialized = true;
        }

        private static string BuildCameraSummary(TestboxedCameraController? camera, Vector2f cameraPosition, float zoom)
        {
            if (camera == null)
                return $"Camera: legacy pos({cameraPosition.X:0.#},{cameraPosition.Y:0.#}) zoom:{zoom:0.00}";

            string follow = camera.FollowTarget ? $"follow {camera.FollowTargetType}" : "free";
            string bounds = camera.ClampToBounds
                ? $"bounds [{camera.BoundsMin.X:0.#},{camera.BoundsMin.Y:0.#}]..[{camera.BoundsMax.X:0.#},{camera.BoundsMax.Y:0.#}]"
                : "bounds off";
            return $"Camera: {camera.GetType().Name} pos({camera.Position.X:0.#},{camera.Position.Y:0.#}) zoom:{camera.Zoom:0.00} {follow} {bounds}";
        }

        private static string BuildInputSummary(int mouseX, int mouseY)
        {
            bool lmb = TlpInput.Mouse.GetButton(TlpMouseButton.Left);
            bool rmb = TlpInput.Mouse.GetButton(TlpMouseButton.Right);
            bool mmb = TlpInput.Mouse.GetButton(TlpMouseButton.Middle);
            bool up = TlpInput.Keyboard.GetKey(TlpKey.Up);
            bool down = TlpInput.Keyboard.GetKey(TlpKey.Down);
            bool left = TlpInput.Keyboard.GetKey(TlpKey.Left);
            bool right = TlpInput.Keyboard.GetKey(TlpKey.Right);
            return $"Input: mouse({mouseX},{mouseY}) L:{lmb} R:{rmb} M:{mmb} arrows:{(left ? "L" : "-")}{(right ? "R" : "-")}{(up ? "U" : "-")}{(down ? "D" : "-")}";
        }

        private static Text CreateDebugText(string id, Vector2f position, uint size, TlpColor color)
        {
            return new Text
            {
                Id = id,
                Anchor = Anchor.TopLeft,
                Position = position,
                CharacterSize = size,
                Color = color,
                ZIndex = 2
            };
        }

        /// <summary>
        /// Legacy image drawing API placeholder.
        /// </summary>
        public static void _DrawImage(IRenderBackend renderBackend, string path, float x, float y)
        {
            if (renderBackend == null) return;

            TlpLogging.Warning("_DrawImage is not fully implemented with the new abstraction");
        }
    }
}
