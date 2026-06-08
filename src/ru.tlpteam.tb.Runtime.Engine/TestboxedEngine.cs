using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using ru.tlpteam.tb.Core;
using ru.tlpteam.Debug;
using ru.tlpteam.Input;
using ru.tlpteam.tb.Runtime.Rendering;
using ru.tlpteam.tb.Runtime.Window;
using ru.tlpteam.tb.Scenes;
using ru.tlpteam.tb.Physics;
using ru.tlpteam.tb.Audio;

namespace ru.tlpteam.tb.Runtime.Engine
{
    public class TestboxedEngine
    {
        private IWindowProvider? _window; // Используем интерфейс вместо конкретной реализации
        private readonly ResourceManager _resources;

        private readonly List<TestboxedScriptForObject> _activeObjects = new();
        private readonly List<TestboxedScriptForObject> _toDestroy = new();
        private readonly List<TestboxedScriptForObject> _toSpawn = new();
        private readonly Dictionary<TestboxedScriptForObject, Vector2f> _previousPositions = new();
        private TestboxedCameraController? _activeCamera;
        private string? _pendingSceneLoad;
        private bool _drawPhysicsDebug = false;
        private bool _drawDebugPanel = false;
        private bool _fpsLockEnabled = true;
        private uint _fpsLockLimit = 60;
        private const float DebugCameraSpeed = 300f;
        private const float DebugZoomSpeed = 1.5f;
        private const float MaxSimulationDeltaTime = 1f / 30f;

        public TestboxedEngine(Assembly compiledScripts, string rootPath, IWindowProvider windowProvider)
        {
            _resources = new ResourceManager(rootPath, compiledScripts, windowProvider);
            TestboxedBridge.ProjectRoot = rootPath;

            TestboxedBridge.OnSpawnRequest = (type, x, y) =>
            {
                var obj = _resources.CreateObject(type, x, y);
                if (obj != null) _toSpawn.Add(obj);
            };

            TestboxedBridge.OnDestroyRequest = (obj) => _toDestroy.Add(obj);

            TestboxedBridge.OnFindObjectsRequest = (type) =>
                _activeObjects.Where(o => o.GetType().Name == type).ToList();
            
            ru.tlpteam.tb.Scenes.SceneController.OnSceneChangeRequest = sceneName => _pendingSceneLoad = sceneName;

            _window = windowProvider;
            TestboxedBridge.WindowProvider = _window;
            TlpPhysics.OnWorldObjectsRequest = () => _activeObjects;
        }

        public void Run(string InitialSceneName)
        {
            if (_window == null)
            {
                TlpLogging.Error("Window provider is not initialized");
                return;
            }

            try
            {
                TlpLogging.Info("Hello from TestboxedEngine.");

                var config = _resources.LoadConfig();
                string title = config["Name"]?.ToString() ?? "ru.tlpteam.tb";
                _window.SetTitle(title);
                ApplyFramerateLimit();
                TestboxedBridge.CameraPosition = new Vector2f(_window.ViewportWidth * 0.5f, _window.ViewportHeight * 0.5f);
                TestboxedBridge.CameraZoom = 1f;

                TlpLogging.Info($"Loading initial scene: {InitialSceneName}");
                var sceneObjects = _resources.LoadScene(InitialSceneName);
                _activeObjects.AddRange(sceneObjects);
                RefreshActiveCamera();
                SyncCameraStateToBridge();

                // ГЛАВНЫЙ ЦИКЛ
                while (_window.IsOpen)
                {
                    _window.UpdateDeltaTime(); // Обновляем delta time
                    float simulationDeltaTime = System.Math.Min(_window.DeltaTime, MaxSimulationDeltaTime);
                    TestboxedBridge.DeltaTime = simulationDeltaTime;

                    TlpInput.UpdateStates();
                    _window.DispatchEvents();
                    HandleHotkeys(simulationDeltaTime);
                    TlpAudio.Update();

                    UpdateLogic(simulationDeltaTime);
                    HandleDeferredRequests();
                    Render();
                }
            }
            catch (Exception ex)
            {
                TlpLogging.Error(ex.ToString());
            }
            finally
            {
                TlpAudio.StopAll();
                TlpLogging.Info("Engine shut down.");
            }
        }

        public void LoadScene(string sceneName)
        {
            // Уничтожаем объекты текущей сцены
            foreach (var obj in _activeObjects)
                _toDestroy.Add(obj);

            HandleDeferredRequests();

            // Загружаем новую сцену
            var sceneObjects = _resources.LoadScene(sceneName);
            _activeObjects.AddRange(sceneObjects);
            RefreshActiveCamera();
            SyncCameraStateToBridge();
        }

        private void UpdateLogic(float deltaTime)
        {
            _previousPositions.Clear();
            foreach (var obj in _activeObjects)
            {
                _previousPositions[obj] = obj.Position;
            }

            foreach (var obj in _activeObjects)
            {
                obj.Update(deltaTime);
            }

            TlpPhysics.ResolveWorldCollisions(_activeObjects, _previousPositions);
            TlpPhysics.ProcessCollisionEvents(_activeObjects);
        }

        private void HandleDeferredRequests()
        {
            if (_toDestroy.Count > 0)
            {
                foreach (var obj in _toDestroy)
                {
                    _activeObjects.Remove(obj);
                }
                _toDestroy.Clear();
            }

            if (_toSpawn.Count > 0)
            {
                _activeObjects.AddRange(_toSpawn);
                _toSpawn.Clear();
            }

            if (_pendingSceneLoad != null)
            {
                string pendingScene = _pendingSceneLoad;
                _pendingSceneLoad = null;
                LoadScene(pendingScene);
            }

            RefreshActiveCamera();
            SyncCameraStateToBridge();
        }

        private void Render()
        {
            SyncCameraStateToBridge();
            float fps = _window!.DeltaTime > 0.0001f ? 1f / _window.DeltaTime : 0f;
            RenderEngine._Render(
                _window,
                _activeObjects,
                _drawPhysicsDebug,
                _drawDebugPanel,
                fps,
                TlpPhysics.DebugLastCollisionPairs,
                TlpPhysics.DebugLastCollisionChecks,
                _fpsLockEnabled);
        }

        private void HandleHotkeys(float deltaTime)
        {
            var activeCamera = GetActiveCamera();

            if (TlpInput.Keyboard.GetKeyDown(TlpKey.F3))
            {
                bool enabled = !_drawPhysicsDebug;
                _drawPhysicsDebug = enabled;
                _drawDebugPanel = enabled;
                TlpLogging.Info($"Debug mode: {(enabled ? "ON" : "OFF")} (colliders + panel + camera controls)");
            }

            if (TlpInput.Keyboard.GetKeyDown(TlpKey.F8))
            {
                _fpsLockEnabled = !_fpsLockEnabled;
                ApplyFramerateLimit();
                TlpLogging.Info(_fpsLockEnabled
                    ? $"FPS lock enabled ({_fpsLockLimit} FPS)."
                    : "FPS lock disabled.");
            }

            bool debugModeEnabled = _drawPhysicsDebug;
            if (!debugModeEnabled) return;

            if (TlpInput.Keyboard.GetKeyDown(TlpKey.F5))
            {
                if (activeCamera != null)
                {
                    activeCamera.SnapTo(new Vector2f(_window!.ViewportWidth * 0.5f, _window.ViewportHeight * 0.5f), 1f);
                }
                else
                {
                    TestboxedBridge.CameraPosition = new Vector2f(_window!.ViewportWidth * 0.5f, _window.ViewportHeight * 0.5f);
                    TestboxedBridge.CameraZoom = 1f;
                }
                TlpLogging.Info("Camera reset to viewport center (zoom=1.0).");
            }

            float zoomDelta = 0f;
            if (TlpInput.Keyboard.GetKey(TlpKey.F6)) zoomDelta += DebugZoomSpeed * deltaTime;
            if (TlpInput.Keyboard.GetKey(TlpKey.F7)) zoomDelta -= DebugZoomSpeed * deltaTime;
            if (System.Math.Abs(zoomDelta) > 0.00001f)
            {
                if (activeCamera != null)
                {
                    activeCamera.Zoom = System.Math.Clamp(activeCamera.Zoom + zoomDelta, activeCamera.MinZoom, activeCamera.MaxZoom);
                }
                else
                {
                    TestboxedBridge.CameraZoom = System.Math.Clamp(TestboxedBridge.CameraZoom + zoomDelta, 0.1f, 8f);
                }
            }

            float zoom = activeCamera != null ? activeCamera.Zoom : TestboxedBridge.CameraZoom;
            float step = DebugCameraSpeed * deltaTime / System.Math.Max(0.01f, zoom);

            if (activeCamera != null)
            {
                var cam = activeCamera.Position;

                if (TlpInput.Keyboard.GetKey(TlpKey.Left)) cam.X -= step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Right)) cam.X += step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Up)) cam.Y -= step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Down)) cam.Y += step;

                activeCamera.Position = cam;
            }
            else
            {
                var cam = TestboxedBridge.CameraPosition;

                if (TlpInput.Keyboard.GetKey(TlpKey.Left)) cam.X -= step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Right)) cam.X += step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Up)) cam.Y -= step;
                if (TlpInput.Keyboard.GetKey(TlpKey.Down)) cam.Y += step;

                TestboxedBridge.CameraPosition = cam;
            }

            SyncCameraStateToBridge();
        }

        private void RefreshActiveCamera()
        {
            _activeCamera = _activeObjects
                .OfType<TestboxedCameraController>()
                .Where(c => c.Active)
                .OrderByDescending(c => c.GetDepth())
                .FirstOrDefault();
        }

        private TestboxedCameraController? GetActiveCamera()
        {
            if (_activeCamera != null && _activeObjects.Contains(_activeCamera) && _activeCamera.Active)
                return _activeCamera;

            RefreshActiveCamera();
            return _activeCamera;
        }

        private void SyncCameraStateToBridge()
        {
            var camera = GetActiveCamera();
            TestboxedBridge.ActiveCamera = camera;

            if (camera == null)
                return;

            camera.Zoom = System.Math.Clamp(camera.Zoom, camera.MinZoom, camera.MaxZoom);
            TestboxedBridge.CameraPosition = camera.Position;
            TestboxedBridge.CameraZoom = camera.Zoom;
        }

        private void ApplyFramerateLimit()
        {
            _window?.SetFramerateLimit(_fpsLockEnabled ? _fpsLockLimit : 0);
        }
    }
}
