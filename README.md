<p align="center">
  <img src="./ReadmeAssets/TestboxedLogo.png" alt="tb. Logo" width="200">
</p>

# Testboxed Runtime

> Testboxed is a sort of standard for 2D games that specifies what the APIs and file formats should be, and how everything fits together. I developed it myself, as neither GameMaker nor Unity suited my needs.

> Even it's mostly vibe-coded (made with help of AI), it's working stable, and I developed the engine's architecture myself.

## Features
- Built-in debug mode overlay (F3)
- Dynamic C# script compilation (like Unity)
- 2D rendering (SFML.Net, but custom backend can be added)
- Physics (AABB box colliders) - `ru.tlpteam.tb.Physics`
- Audio API - `ru.tlpteam.tb.Audio`
- Input API - `ru.tlpteam.Input`, and i know it's still called ru.tlpteam.TlpInput in folders, don't ask why
- Built-in UI API - `ru.tlpteam.tb.UI`

## How to run
```
dotnet run -- <ABSOLUTE_OR_RELATIVE_PROJECT_PATH> --initial-scene SceneName
```

You can look on example/demo projects for this engine [here](https://github.com/thelittlepony/Testboxed-Examples).

## Screenshots
<p align="center">
  <img src="./ReadmeAssets/Demo_Kris.png" alt="Demo with Kris dancing, and debug overlay" width="700">
</p>
