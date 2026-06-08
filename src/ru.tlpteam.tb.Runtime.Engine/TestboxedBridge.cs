using System;
using System.Collections.Generic;
using ru.tlpteam.tb.Core;
using ru.tlpteam.tb.Runtime.Window;

namespace ru.tlpteam.tb.Runtime.Engine
{
    /// <summary>
    /// Global bridge for access from user scripts.
    /// </summary>
    public static class TestboxedBridge
    {
        public static IWindowProvider? WindowProvider;
        public static string? ProjectRoot;
        public static float DeltaTime;

        // Render pipeline camera state (world-space center + zoom).
        public static TestboxedCameraController? ActiveCamera;
        public static Vector2f CameraPosition = new Vector2f(0f, 0f);
        public static float CameraZoom = 1f;

        public static Action<string, float, float>? OnSpawnRequest;
        public static Action<TestboxedScriptForObject>? OnDestroyRequest;
        public static Func<string, List<TestboxedScriptForObject>>? OnFindObjectsRequest;

        public static void Spawn(string type, float x, float y) => OnSpawnRequest?.Invoke(type, x, y);
        public static void Destroy(TestboxedScriptForObject obj) => OnDestroyRequest?.Invoke(obj);
        public static List<TestboxedScriptForObject> FindObjects(string type) =>
            OnFindObjectsRequest?.Invoke(type) ?? new List<TestboxedScriptForObject>();
    }
}
