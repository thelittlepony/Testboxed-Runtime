using ru.tlpteam.Input;
using ru.tlpteam.tb.Runtime.Rendering;

namespace ru.tlpteam.tb.Runtime.Window
{
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
}
