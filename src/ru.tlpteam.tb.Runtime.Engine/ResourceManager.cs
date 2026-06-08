using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json.Linq;
using ru.tlpteam.tb.Core;
using ru.tlpteam.Debug;
using ru.tlpteam.tb.Physics;
using ru.tlpteam.tb.Runtime.Window;

namespace ru.tlpteam.tb.Runtime.Engine
{
    public class ResourceManager
    {
        private const string SpriteFitScaleXArg = "__tb_sprite_fit_scale_x";
        private const string SpriteFitScaleYArg = "__tb_sprite_fit_scale_y";
        private readonly string _projectRoot;
        private readonly Assembly _scriptsAssembly;
        private readonly IWindowProvider _windowProvider;

        public ResourceManager(string projectRoot, Assembly scriptsAssembly, IWindowProvider windowProvider)
        {
            _projectRoot = projectRoot;
            _scriptsAssembly = scriptsAssembly;
            _windowProvider = windowProvider;
        }

        public JObject LoadConfig()
        {
            string path = Path.Combine(_projectRoot, "tlpruntimeconfig.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Config not found!");
            return JObject.Parse(File.ReadAllText(path));
        }

        public List<TestboxedScriptForObject> LoadScene(string sceneName)
        {
            var objects = new List<TestboxedScriptForObject>();
            string scenePath = Path.Combine(_projectRoot, "Scenes", $"{sceneName}.json");

            if (!File.Exists(scenePath))
            {
                TlpLogging.Error($"Scene file not found: {scenePath}");
                return objects;
            }

            var sceneData = JObject.Parse(File.ReadAllText(scenePath));
            ValidateSceneType(sceneData, sceneName);
            var instances = sceneData["Instances"] as JArray;
            if (instances == null) return objects;

            foreach (var inst in instances)
            {
                string type = ResolveObjectType(inst);
                if (string.IsNullOrWhiteSpace(type))
                {
                    TlpLogging.Warning("Scene instance skipped: missing BaseObject/Type.");
                    continue;
                }

                var position = ParsePosition(inst);
                float rotation = ReadFloat(inst["Rotation"], 0f);
                var scale = ParseScale(inst);
                int depth = ReadInt(inst["Depth"], 0);

                var obj = CreateObject(type, position.X, position.Y);
                if (obj == null) continue;

                obj.Rotation = rotation;
                // Keep object base scale (e.g. SpriteSize fitting) and apply scene scale as multiplier.
                obj.Scale = new Vector2f(obj.Scale.X * scale.X, obj.Scale.Y * scale.Y);
                obj.Depth = depth;
                objects.Add(obj);
                //TlpLogging.Info(obj);
            }

            return objects;
        }

        private void ValidateSceneType(JObject sceneData, string sceneName)
        {
            string? sceneType = sceneData["Type"]?.ToString();
            if (string.IsNullOrWhiteSpace(sceneType)) return;

            if (sceneType == TestboxedMapTypeNames.TestboxedLikeMap || sceneType == "TestboxedLikeMap")
                return;

            TlpLogging.Warning($"Scene '{sceneName}' has unknown Type: {sceneType}");
        }

        private string ResolveObjectType(JToken inst)
        {
            string typeFromBaseObject = ResolveBaseObject(inst["BaseObject"]);
            if (!string.IsNullOrWhiteSpace(typeFromBaseObject)) return typeFromBaseObject;

            // Legacy scene compatibility.
            return inst["Type"]?.ToString() ?? string.Empty;
        }

        private string ResolveBaseObject(JToken? baseObjectToken)
        {
            if (baseObjectToken == null || baseObjectToken.Type == JTokenType.Null)
                return string.Empty;

            if (baseObjectToken.Type == JTokenType.String)
                return baseObjectToken.ToString();

            if (baseObjectToken.Type == JTokenType.Object)
            {
                var obj = (JObject)baseObjectToken;
                return obj["Type"]?.ToString()
                    ?? obj["Name"]?.ToString()
                    ?? obj["Id"]?.ToString()
                    ?? string.Empty;
            }

            return string.Empty;
        }

        private Vector2f ParsePosition(JToken inst)
        {
            var positionObj = inst["Position"] as JObject;
            if (positionObj != null)
            {
                float x = ReadFloat(positionObj["X"], 0f);
                float y = ReadFloat(positionObj["Y"], 0f);
                return new Vector2f(x, y);
            }

            // Legacy "Pos" support: [x, y] or "x y".
            var posArray = inst["Pos"] as JArray;
            if (posArray != null)
            {
                float x = ReadFloat(posArray.Count > 0 ? posArray[0] : null, 0f);
                float y = ReadFloat(posArray.Count > 1 ? posArray[1] : null, 0f);
                return new Vector2f(x, y);
            }

            string? posRaw = inst["Pos"]?.ToString();
            if (!string.IsNullOrWhiteSpace(posRaw))
            {
                var parts = posRaw.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    float x = TryParseFloat(parts[0], 0f);
                    float y = TryParseFloat(parts[1], 0f);
                    return new Vector2f(x, y);
                }
            }

            return new Vector2f(0f, 0f);
        }

        private Vector2f ParseScale(JToken inst)
        {
            var scaleObj = inst["Scale"] as JObject;
            if (scaleObj != null)
            {
                float x = ReadFloat(scaleObj["X"], 1f);
                float y = ReadFloat(scaleObj["Y"], 1f);
                return new Vector2f(x, y);
            }

            return new Vector2f(1f, 1f);
        }

        private float ReadFloat(JToken? token, float defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return token.Value<float>();

            return TryParseFloat(token.ToString(), defaultValue);
        }

        private float TryParseFloat(string raw, float defaultValue)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedInvariant))
                return parsedInvariant;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsedCurrent))
                return parsedCurrent;

            return defaultValue;
        }

        private int ReadInt(JToken? token, int defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            if (token.Type == JTokenType.Integer)
                return token.Value<int>();

            if (int.TryParse(token.ToString(), out int parsed))
                return parsed;

            return defaultValue;
        }

        public TestboxedScriptForObject? CreateObject(string type, float x, float y)
        {
            string objPath = Path.Combine(_projectRoot, "Objects", $"{type}.json");
            if (!File.Exists(objPath))
            {
                TlpLogging.Warning($"Object definition not found: {type}");
                return null;
            }

            var objData = JObject.Parse(File.ReadAllText(objPath));
            string className = objData["BaseClassInScripts"]?.ToString() ?? "";

            // Resolve script type from the compiled scripts assembly.
            Type? t = _scriptsAssembly.GetType($"ru.tlpteam.tb.CustomScripts.{className}") ??
                      _scriptsAssembly.GetTypes().FirstOrDefault(x => x.Name == className);

            if (t == null)
            {
                TlpLogging.Error($"Class {className} not found in scripts!");
                return null;
            }

            var script = (TestboxedScriptForObject?)Activator.CreateInstance(t);
            if (script == null) return null;

            // Attach texture sprite or placeholder sprite.
            SetupSprite(script, objData);
            SetupPhysics(script, objData);
            SetupRenderProperties(script, objData);
            SetupArgs(script, objData["Args"]);

            script.Position = new Vector2f(x, y);
            script.Start();
            return script;
        }

        private void SetupSprite(TestboxedScriptForObject script, JObject objData)
        {
            string textureValue = objData["Texture"]?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(textureValue))
            {
                if (TryParseHexColor(textureValue, out var color))
                {
                    var size = ResolveSolidSpriteSize(objData);
                    script.Sprite = _windowProvider.CreateSolidSprite(size.width, size.height, color);
                    return;
                }

                foreach (var path in BuildTextureCandidates(textureValue))
                {
                    if (!File.Exists(path)) continue;

                    script.Sprite = _windowProvider.CreateSpriteFromTexture(path);
                    ApplyConfiguredSpriteSizeScale(script, objData);
                    return;
                }

                TlpLogging.Warning($"Texture not found: {textureValue}");
            }

            script.Sprite = CreatePlaceholderSprite();
        }

        private ISprite CreatePlaceholderSprite()
        {
            return _windowProvider.CreateSolidSprite(32, 32, new TlpColor(255, 0, 255));
        }

        private IEnumerable<string> BuildTextureCandidates(string rawTexture)
        {
            string tex = rawTexture.Trim();
            bool hasExt = Path.HasExtension(tex);

            var candidates = new List<string>();

            if (Path.IsPathRooted(tex))
            {
                candidates.Add(tex);
                if (!hasExt) candidates.Add($"{tex}.png");
                return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
            }

            string projectRelative = Path.Combine(_projectRoot, tex);
            candidates.Add(projectRelative);
            if (!hasExt) candidates.Add($"{projectRelative}.png");

            string texturesRelative = Path.Combine(_projectRoot, "Textures", tex);
            candidates.Add(texturesRelative);
            if (!hasExt) candidates.Add($"{texturesRelative}.png");

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private (uint width, uint height) ResolveSolidSpriteSize(JObject objData)
        {
            var spriteSizeArray = objData["SpriteSize"] as JArray;
            if (spriteSizeArray != null)
            {
                uint w = ToPixelSize(ReadFloat(spriteSizeArray.Count > 0 ? spriteSizeArray[0] : null, 32f), 32);
                uint h = ToPixelSize(ReadFloat(spriteSizeArray.Count > 1 ? spriteSizeArray[1] : null, 32f), 32);
                return (w, h);
            }

            var spriteSizeObj = objData["SpriteSize"] as JObject;
            if (spriteSizeObj != null)
            {
                uint w = ToPixelSize(ReadFloat(spriteSizeObj["X"], 32f), 32);
                uint h = ToPixelSize(ReadFloat(spriteSizeObj["Y"], 32f), 32);
                return (w, h);
            }

            var colliderData = objData["Collider"] as JObject;
            var colliderSize = colliderData?["Size"] as JArray;
            if (colliderSize != null)
            {
                uint w = ToPixelSize(ReadFloat(colliderSize.Count > 0 ? colliderSize[0] : null, 32f), 32);
                uint h = ToPixelSize(ReadFloat(colliderSize.Count > 1 ? colliderSize[1] : null, 32f), 32);
                return (w, h);
            }

            return (32, 32);
        }

        private uint ToPixelSize(float raw, uint fallback)
        {
            if (raw <= 0f) return fallback;
            return (uint)System.Math.Max(1f, raw);
        }

        private void ApplyConfiguredSpriteSizeScale(TestboxedScriptForObject script, JObject objData)
        {
            if (script.Sprite == null) return;

            var target = ResolveConfiguredSpriteSize(objData);
            if (target == null)
            {
                SetSpriteFitScale(script, 1f, 1f);
                return;
            }

            var source = script.Sprite.Size;
            if (source.X <= 0.001f || source.Y <= 0.001f)
            {
                SetSpriteFitScale(script, 1f, 1f);
                return;
            }

            float fitX = target.Value.X / source.X;
            float fitY = target.Value.Y / source.Y;
            script.Scale = new Vector2f(fitX, fitY);
            SetSpriteFitScale(script, fitX, fitY);
        }

        private Vector2f? ResolveConfiguredSpriteSize(JObject objData)
        {
            var spriteSizeArray = objData["SpriteSize"] as JArray;
            if (spriteSizeArray != null)
            {
                float w = ReadFloat(spriteSizeArray.Count > 0 ? spriteSizeArray[0] : null, 0f);
                float h = ReadFloat(spriteSizeArray.Count > 1 ? spriteSizeArray[1] : null, 0f);
                if (w > 0f && h > 0f) return new Vector2f(w, h);
            }

            var spriteSizeObj = objData["SpriteSize"] as JObject;
            if (spriteSizeObj != null)
            {
                float w = ReadFloat(spriteSizeObj["X"], 0f);
                float h = ReadFloat(spriteSizeObj["Y"], 0f);
                if (w > 0f && h > 0f) return new Vector2f(w, h);
            }

            return null;
        }

        private void SetSpriteFitScale(TestboxedScriptForObject script, float x, float y)
        {
            script.Args[SpriteFitScaleXArg] = x;
            script.Args[SpriteFitScaleYArg] = y;
        }

        private Vector2f GetSpriteFitScale(TestboxedScriptForObject script)
        {
            float x = 1f;
            float y = 1f;

            if (script.Args.TryGetValue(SpriteFitScaleXArg, out var xRaw) && xRaw is float xf)
                x = xf;

            if (script.Args.TryGetValue(SpriteFitScaleYArg, out var yRaw) && yRaw is float yf)
                y = yf;

            return new Vector2f(
                System.Math.Abs(x) > 0.0001f ? x : 1f,
                System.Math.Abs(y) > 0.0001f ? y : 1f);
        }

        private bool TryParseHexColor(string raw, out TlpColor color)
        {
            color = new TlpColor(255, 0, 255);
            string value = raw.Trim();
            if (!value.StartsWith("#", StringComparison.Ordinal))
                return false;

            string hex = value.Substring(1);
            if (hex.Length == 3)
            {
                if (!TryParseHexByte($"{hex[0]}{hex[0]}", out byte r)) return false;
                if (!TryParseHexByte($"{hex[1]}{hex[1]}", out byte g)) return false;
                if (!TryParseHexByte($"{hex[2]}{hex[2]}", out byte b)) return false;
                color = new TlpColor(r, g, b, 255);
                return true;
            }

            if (hex.Length == 4)
            {
                if (!TryParseHexByte($"{hex[0]}{hex[0]}", out byte r)) return false;
                if (!TryParseHexByte($"{hex[1]}{hex[1]}", out byte g)) return false;
                if (!TryParseHexByte($"{hex[2]}{hex[2]}", out byte b)) return false;
                if (!TryParseHexByte($"{hex[3]}{hex[3]}", out byte a)) return false;
                color = new TlpColor(r, g, b, a);
                return true;
            }

            if (hex.Length == 6)
            {
                if (!TryParseHexByte(hex.Substring(0, 2), out byte r)) return false;
                if (!TryParseHexByte(hex.Substring(2, 2), out byte g)) return false;
                if (!TryParseHexByte(hex.Substring(4, 2), out byte b)) return false;
                color = new TlpColor(r, g, b, 255);
                return true;
            }

            if (hex.Length == 8)
            {
                if (!TryParseHexByte(hex.Substring(0, 2), out byte r)) return false;
                if (!TryParseHexByte(hex.Substring(2, 2), out byte g)) return false;
                if (!TryParseHexByte(hex.Substring(4, 2), out byte b)) return false;
                if (!TryParseHexByte(hex.Substring(6, 2), out byte a)) return false;
                color = new TlpColor(r, g, b, a);
                return true;
            }

            return false;
        }

        private bool TryParseHexByte(string hex, out byte value)
        {
            return byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        private void SetupPhysics(TestboxedScriptForObject script, JObject objData)
        {
            script.IsStatic = ReadBool(objData["IsStatic"], false) || ReadBool(objData["Static"], false);

            var colliderData = objData["Collider"] as JObject;
            if (colliderData == null)
            {
                script.Collider = null;
                return;
            }

            string rawType = colliderData["Type"]?.ToString() ?? "Box";
            if (!string.Equals(rawType, "Box", StringComparison.OrdinalIgnoreCase))
            {
                TlpLogging.Warning($"Unsupported collider type '{rawType}'. Falling back to Box.");
            }

            var sizeArray = colliderData["Size"] as JArray;
            float width = ReadFloat(sizeArray != null && sizeArray.Count > 0 ? sizeArray[0] : null, 0f);
            float height = ReadFloat(sizeArray != null && sizeArray.Count > 1 ? sizeArray[1] : null, 0f);

            var offsetArray = colliderData["Offset"] as JArray;
            float offsetX = ReadFloat(offsetArray != null && offsetArray.Count > 0 ? offsetArray[0] : null, 0f);
            float offsetY = ReadFloat(offsetArray != null && offsetArray.Count > 1 ? offsetArray[1] : null, 0f);

            // Collider dimensions are authored in world units. If sprite was auto-fitted by SpriteSize,
            // neutralize that internal fit so collider is affected only by scene/object scale.
            var spriteFit = GetSpriteFitScale(script);
            width /= spriteFit.X;
            height /= spriteFit.Y;
            offsetX /= spriteFit.X;
            offsetY /= spriteFit.Y;

            script.Collider = new BoxCollider
            {
                Type = ColliderType.Box,
                Size = new Vector2f(width, height),
                Offset = new Vector2f(offsetX, offsetY),
                IsTrigger = ReadBool(colliderData["IsTrigger"], false),
                Enabled = !ReadBool(colliderData["Disabled"], false)
            };
        }

        private bool ReadBool(JToken? token, bool defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>();

            if (bool.TryParse(token.ToString(), out bool parsed))
                return parsed;

            return defaultValue;
        }

        private void SetupRenderProperties(TestboxedScriptForObject script, JObject objData)
        {
            script.Visible = ReadBool(objData["Visible"], true);

            var renderLayerToken = objData["RenderLayer"];
            if (renderLayerToken != null && renderLayerToken.Type != JTokenType.Null)
            {
                if (renderLayerToken.Type == JTokenType.Integer)
                {
                    script.Layer = (RenderLayer)renderLayerToken.Value<int>();
                }
                else
                {
                    var raw = renderLayerToken.ToString();
                    if (Enum.TryParse<RenderLayer>(raw, ignoreCase: true, out var parsed))
                    {
                        script.Layer = parsed;
                    }
                    else
                    {
                        TlpLogging.Warning($"Unknown RenderLayer '{raw}'. Fallback to World.");
                        script.Layer = RenderLayer.World;
                    }
                }
            }

            string originRaw = objData["SpriteOrigin"]?.ToString() ?? "TopLeft";
            if (Enum.TryParse<SpriteOriginMode>(originRaw, ignoreCase: true, out var originMode))
            {
                script.SpriteOriginMode = originMode;
            }
            else
            {
                TlpLogging.Warning($"Unknown SpriteOrigin '{originRaw}'. Fallback to TopLeft.");
                script.SpriteOriginMode = SpriteOriginMode.TopLeft;
            }

            script.SpriteCustomOrigin = ParseVector2(objData["SpriteOriginCustom"], new Vector2f(0f, 0f));
        }

        private void SetupArgs(TestboxedScriptForObject script, JToken? argsToken)
        {
            if (argsToken is not JObject argsObject)
                return;

            foreach (var property in argsObject.Properties())
            {
                var value = ConvertJsonValue(property.Value);
                if (value != null)
                    script.Args[property.Name] = value;
            }
        }

        private object? ConvertJsonValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => token.Value<double>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.String => token.Value<string>(),
                JTokenType.Null => null,
                JTokenType.Array => token.Select(ConvertJsonValue).ToList(),
                JTokenType.Object => token.Children<JProperty>()
                    .ToDictionary(p => p.Name, p => ConvertJsonValue(p.Value)),
                _ => token.ToString(),
            };
        }

        private Vector2f ParseVector2(JToken? token, Vector2f defaultValue)
        {
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            if (token is JObject obj)
            {
                float x = ReadFloat(obj["X"], defaultValue.X);
                float y = ReadFloat(obj["Y"], defaultValue.Y);
                return new Vector2f(x, y);
            }

            if (token is JArray arr)
            {
                float x = ReadFloat(arr.Count > 0 ? arr[0] : null, defaultValue.X);
                float y = ReadFloat(arr.Count > 1 ? arr[1] : null, defaultValue.Y);
                return new Vector2f(x, y);
            }

            return defaultValue;
        }
    }
}
