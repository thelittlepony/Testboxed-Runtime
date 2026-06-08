# Engine API Signatures

This file mirrors the current public runtime API.

## ru.tlpteam.Debug

### TlpLogging
```csharp
public static class TlpLogging
{
    public static void Info(object message, string member = "", string file = "");
    public static void Warning(object message, string member = "", string file = "");
    public static void Error(object message, string member = "", string file = "");
}
```

## ru.tlpteam.tb.Audio

### TlpAudio
```csharp
public static class TlpAudio
{
    public static void SetMasterVolume(float volume);
    public static void SetSfxVolume(float volume);
    public static void SetMusicVolume(float volume);

    public static void PlaySfx(string source, float volume = 100f, bool loop = false);
    public static void PlayMusic(string source, bool loop = true, float volume = 100f, string channel = "BGM");

    public static void StopMusic(string channel = "BGM");
    public static void PauseMusic(string channel = "BGM");
    public static void ResumeMusic(string channel = "BGM");

    public static void StopAllMusic();
    public static void StopAllSfx();
    public static void StopAll();
    public static void Update();
}
```

## ru.tlpteam.tb.Core

### Enums
```csharp
public enum RenderLayer
{
    Background = 0,
    World = 100,
    UI = 200
}

public enum SpriteOriginMode
{
    TopLeft = 0,
    Center = 1,
    Custom = 2
}
```

### TestboxedMapTypeNames
```csharp
public static class TestboxedMapTypeNames
{
    public const string TestboxedLikeMap = "ru.tlpteam.tb.Types.TestboxedLikeMap";
}
```

### Vector2f
```csharp
public struct Vector2f
{
    public float X;
    public float Y;

    public Vector2f(float x, float y);
}
```

### ISprite
```csharp
public interface ISprite
{
    Vector2f Position { get; set; }
    Vector2f Scale { get; set; }
    Vector2f Origin { get; set; }
    Vector2f Size { get; }
}
```

### TlpColor
```csharp
public readonly struct TlpColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public TlpColor(byte r, byte g, byte b, byte a = 255);
}
```

### ObjectInstance
```csharp
public sealed class ObjectInstance
{
    public Vector2f Position { get; set; }
    public float Rotation { get; set; }
    public Vector2f Scale { get; set; }
    public int Depth { get; set; }
    public object? BaseObject { get; set; }
}
```

### MapSettings
```csharp
public sealed class MapSettings
{
    public BackgroundSettings Background { get; set; }
    public Vector2f Scale { get; set; }

    public sealed class BackgroundSettings
    {
        public string FillWith { get; set; }
        public string Value { get; set; }
    }
}
```

### TestboxedScriptForObject
```csharp
public abstract class TestboxedScriptForObject
{
    public Vector2f Position;
    public float Rotation;
    public Vector2f Scale;
    public int Depth;
    public RenderLayer Layer;
    public bool Visible;
    public SpriteOriginMode SpriteOriginMode;
    public Vector2f SpriteCustomOrigin;
    public bool IsStatic;
    public BoxCollider? Collider;
    public ISprite? Sprite;
    public Dictionary<string, object> Args;

    public abstract void Start();
    public abstract void Update(float deltaTime);
    public virtual int GetDepth();
    public virtual void OnCollisionEnter(TestboxedScriptForObject other);
    public virtual void OnCollisionStay(TestboxedScriptForObject other);
    public virtual void OnCollisionExit(TestboxedScriptForObject other);

    protected bool TryGetArg(string key, out object? value);
    protected string GetStringArg(string key, string defaultValue = "");
    protected float GetFloatArg(string key, float defaultValue = 0f);
    protected bool GetBoolArg(string key, bool defaultValue = false);
    protected Vector2f GetVector2Arg(string xKey, string yKey, Vector2f defaultValue);
}
```

### TestboxedCameraController
```csharp
public class TestboxedCameraController : TestboxedScriptForObject
{
    public bool Active;
    public bool FollowTarget;
    public bool SnapToTargetOnStart;
    public string FollowTargetType;
    public Vector2f FollowOffset;
    public float Zoom;
    public float MinZoom;
    public float MaxZoom;
    public float FollowLerp;
    public bool ClampToBounds;
    public Vector2f BoundsMin;
    public Vector2f BoundsMax;

    public override void Start();
    public override void Update(float deltaTime);
    public void SnapTo(Vector2f position, float zoom = 1f);
    public void FocusOn(TestboxedScriptForObject target, bool snap = true);
    public void StopFollowing();
}
```

## ru.tlpteam.tb.Math

### TlpMath
```csharp
public static class TlpMath
{
    public static float Clamp(float value, float min, float max);
    public static float Lerp(float a, float b, float t);
    public static Vector2f Lerp(Vector2f a, Vector2f b, float t);
    public static float InverseLerp(float a, float b, float value);
    public static float Remap(float inMin, float inMax, float outMin, float outMax, float value);
    public static float MoveTowards(float current, float target, float maxDelta);
    public static float SmoothStep(float edge0, float edge1, float x);
}
```

## ru.tlpteam.tb.Physics

### Enums
```csharp
public enum ColliderType
{
    Box
}
```

### BoxCollider
```csharp
public sealed class BoxCollider
{
    public ColliderType Type { get; set; }
    public Vector2f Offset { get; set; }
    public Vector2f Size { get; set; }
    public bool IsTrigger { get; set; }
    public bool Enabled { get; set; }
}
```

### TlpPhysics
```csharp
public static class TlpPhysics
{
    public static int DebugLastCollisionPairs { get; }
    public static int DebugLastCollisionChecks { get; }

    public static List<TestboxedScriptForObject> Overlap(TestboxedScriptForObject self, string? type = null);
    public static bool PlaceMeeting(TestboxedScriptForObject self, float x, float y, string? type = null);
}
```

## ru.tlpteam.tb.Runtime.Engine

### ScriptLoader
```csharp
public class ScriptLoader
{
    public Assembly? CompiledAssembly { get; }
    public void LoadAndCompile(string scriptsPath);
}
```

### ResourceManager
```csharp
public class ResourceManager
{
    public ResourceManager(string projectRoot, Assembly scriptsAssembly, IWindowProvider windowProvider);

    public JObject LoadConfig();
    public List<TestboxedScriptForObject> LoadScene(string sceneName);
    public TestboxedScriptForObject? CreateObject(string type, float x, float y);
}
```

### TestboxedBridge
```csharp
public static class TestboxedBridge
{
    public static IWindowProvider? WindowProvider;
    public static string? ProjectRoot;
    public static float DeltaTime;

    public static TestboxedCameraController? ActiveCamera;
    public static Vector2f CameraPosition;
    public static float CameraZoom;

    public static Action<string, float, float>? OnSpawnRequest;
    public static Action<TestboxedScriptForObject>? OnDestroyRequest;
    public static Func<string, List<TestboxedScriptForObject>>? OnFindObjectsRequest;

    public static void Spawn(string type, float x, float y);
    public static void Destroy(TestboxedScriptForObject obj);
    public static List<TestboxedScriptForObject> FindObjects(string type);
}
```

### TestboxedEngine
```csharp
public class TestboxedEngine
{
    public TestboxedEngine(Assembly compiledScripts, string rootPath, IWindowProvider windowProvider);
    public void Run(string initialSceneName);
    public void LoadScene(string sceneName);
}
```

## ru.tlpteam.tb.Runtime.Rendering

### RenderEngine
```csharp
public static class RenderEngine
{
    public static void _Render(
        IRenderBackend renderBackend,
        List<TestboxedScriptForObject> activeObjects,
        bool drawPhysicsDebug = false,
        bool drawDebugPanel = false,
        float fps = 0f,
        int collisionPairs = 0,
        int collisionChecks = 0,
        bool fpsLockEnabled = true);

    public static void _DrawImage(IRenderBackend renderBackend, string path, float x, float y);
}
```

## ru.tlpteam.tb.Runtime.Window

### IRenderBackend
```csharp
public interface IRenderBackend
{
    uint ViewportWidth { get; }
    uint ViewportHeight { get; }

    void Clear(TlpColor clearColor);
    void Display();

    void Draw(ISprite sprite);
    void DrawDebugText(string text, float x, float y, TlpColor color, uint characterSize = 12);
    ISprite CreateSpriteFromTexture(string texturePath);
    ISprite CreateSolidSprite(uint width, uint height, TlpColor color);
}
```

### IWindowProvider
```csharp
public interface IWindowProvider : IRenderBackend
{
    bool IsOpen { get; }
    float DeltaTime { get; }

    void SetTitle(string title);
    void SetFramerateLimit(uint limit);
    void Close();

    void DispatchEvents();
    void UpdateDeltaTime();

    int GetMousePositionX();
    int GetMousePositionY();

    bool IsKeyPressed(TlpKey key);
    bool IsMouseButtonPressed(TlpMouseButton button);
}
```

### SfmlSpriteAdapter
```csharp
public sealed class SfmlSpriteAdapter : ISprite
{
    public Sprite NativeSprite { get; }

    public Vector2f Position { get; set; }
    public Vector2f Scale { get; set; }
    public Vector2f Origin { get; set; }
    public Vector2f Size { get; }

    public SfmlSpriteAdapter(Texture texture);
}
```

### SFMLRenderBackend
```csharp
public sealed class SFMLRenderBackend : IRenderBackend
{
    public uint ViewportWidth { get; }
    public uint ViewportHeight { get; }

    public SFMLRenderBackend(
        RenderWindow window,
        uint windowWidth,
        uint windowHeight,
        float renderScale = 1f,
        uint? viewportWidth = null,
        uint? viewportHeight = null);

    public void Resize(uint width, uint height);
    public void Clear(TlpColor clearColor);
    public void Display();
    public void Draw(ISprite sprite);
    public void DrawDebugText(string text, float x, float y, TlpColor color, uint characterSize = 12);
    public ISprite CreateSpriteFromTexture(string texturePath);
    public ISprite CreateSolidSprite(uint width, uint height, TlpColor color);
}
```

### SFMLWindowProvider
```csharp
public class SFMLWindowProvider : IWindowProvider
{
    public bool IsOpen { get; }
    public float DeltaTime { get; }
    public uint ViewportWidth { get; }
    public uint ViewportHeight { get; }

    public SFMLWindowProvider(
        uint width,
        uint height,
        string title,
        float renderScale = 1f,
        uint? viewportWidth = null,
        uint? viewportHeight = null);

    public bool IsKeyPressed(TlpKey key);
    public bool IsMouseButtonPressed(TlpMouseButton button);

    public void SetTitle(string title);
    public void SetFramerateLimit(uint limit);
    public void Close();
    public void DispatchEvents();
    public void UpdateDeltaTime();

    public int GetMousePositionX();
    public int GetMousePositionY();
}
```

## ru.tlpteam.tb.Scenes

### SceneController
```csharp
public static class SceneController
{
    public static Action<string>? OnSceneChangeRequest;
    public static void LoadScene(string sceneName);
}
```

## ru.tlpteam.Input

### Enums
```csharp
public enum TlpKey
{
    Unknown = -1,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
    Escape, LControl, LShift, LAlt, LSystem, RControl, RShift, RAlt, RSystem,
    Menu, LBracket, RBracket, SemiColon, Comma, Period, Quote, Slash, BackSlash,
    Tilde, Equal, Dash, Space, Return, BackSpace, Tab, PageUp, PageDown, End, Home,
    Insert, Delete, Add, Subtract, Multiply, Divide, Left, Right, Up, Down,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15, Pause
}

public enum TlpMouseButton
{
    Left, Right, Middle, XButton1, XButton2
}
```

### TlpInput
```csharp
public static class TlpInput
{
    public static void UpdateStates();

    public static class Keyboard
    {
        public static bool GetKey(TlpKey key);
        public static bool GetKeyDown(TlpKey key);
        public static bool GetKeyUp(TlpKey key);
    }

    public static class Mouse
    {
        public static Vector2f GetPosition();
        public static float GetX();
        public static float GetY();

        public static bool GetButton(TlpMouseButton button);
        public static bool GetButtonDown(TlpMouseButton button);
        public static bool GetButtonUp(TlpMouseButton button);
    }
}
```

## ru.tlpteam.tb.UI

### Anchor
```csharp
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
```

### UiElement
```csharp
public abstract class UiElement
{
    public string Id { get; set; }
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
    public bool Interactable { get; set; }
    public int ZIndex { get; set; }
    public Anchor Anchor { get; set; }
    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }

    public event Action<UiElement>? PointerEnter;
    public event Action<UiElement>? PointerExit;
    public event Action<UiElement>? PointerDown;
    public event Action<UiElement>? PointerUp;
    public event Action<UiElement>? Click;

    public virtual IReadOnlyList<UiElement> Children { get; }
}
```

### Panel
```csharp
public sealed class Panel : UiElement
{
    public TlpColor BackgroundColor { get; set; }
    public override IReadOnlyList<UiElement> Children { get; }

    public void Add(UiElement child);
    public bool Remove(UiElement child);
    public void ClearChildren();
}
```

### Text
```csharp
public sealed class Text : UiElement
{
    public string Value { get; set; }
    public TlpColor Color { get; set; }
    public uint CharacterSize { get; set; }
}
```

### Sparkline
```csharp
public sealed class Sparkline : UiElement
{
    public TlpColor BackgroundColor { get; set; }
    public TlpColor PrimaryColor { get; set; }
    public TlpColor SecondaryColor { get; set; }

    public void SetSeries(IReadOnlyList<float> primaryValues, IReadOnlyList<float>? secondaryValues = null);
}
```

### UiCanvas
```csharp
public sealed class UiCanvas
{
    public IReadOnlyList<UiElement> Roots { get; }

    public void Add(UiElement element);
    public bool Remove(UiElement element);
    public void Clear();
    public void Render(IRenderBackend renderBackend);
}
```

### TlpUI
```csharp
public static class TlpUI
{
    public static UiCanvas Canvas { get; }
    public static void Render(IRenderBackend renderBackend);
}
```

## Camera objects

- Create a scene object whose `BaseClassInScripts` points to a script derived from `TestboxedCameraController`.
- The active camera is tracked through `TestboxedBridge.ActiveCamera` when present.
- The camera supports follow-by-type, smooth movement, optional snap-on-start, zoom limits, and optional world bounds.
- Script arguments recognized by `TestboxedCameraController` include `Active`, `FollowTarget`, `FollowTargetType`, `SnapToTargetOnStart`, `Zoom`, `MinZoom`, `MaxZoom`, `FollowLerp`, `FollowOffsetX`, `FollowOffsetY`, `ClampToBounds`, `BoundsMinX`, `BoundsMinY`, `BoundsMaxX`, and `BoundsMaxY`.

## Runtime config (`tlpruntimeconfig.json`)

```json
{
  "Name": "string",
  "WindowWidth": "uint",
  "WindowHeight": "uint",
  "ViewportWidth": "uint",
  "ViewportHeight": "uint",
  "RenderScale": "float"
}
```

- `Name` sets the OS window title. If omitted, the runtime uses `ru.tlpteam.tb`.
- `WindowWidth` and `WindowHeight` define the real OS window size. If omitted or invalid, they fall back to `640x480`.
- `ViewportWidth` and `ViewportHeight` define the logical game viewport. If omitted, they fall back to the window size.
- `RenderScale` controls the internal offscreen render scale. If omitted or invalid, it falls back to `1.0`, and the runtime clamps it to the `1.0` to `4.0` range.
