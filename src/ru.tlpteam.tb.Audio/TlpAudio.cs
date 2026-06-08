using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFML.Audio;
using ru.tlpteam.Debug;
using ru.tlpteam.tb.Runtime.Engine;

namespace ru.tlpteam.tb.Audio
{
    public static class TlpAudio
    {
        private static readonly Dictionary<string, SoundBuffer> _soundBufferCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Sound> _activeSounds = new();
        private static readonly Dictionary<Sound, float> _soundBaseVolumes = new();
        private static readonly Dictionary<string, Music> _musicChannels = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> _musicBaseVolumes = new(StringComparer.OrdinalIgnoreCase);

        private static float _masterVolume = 100f;
        private static float _sfxVolume = 100f;
        private static float _musicVolume = 100f;

        public static void SetMasterVolume(float volume)
        {
            _masterVolume = Clamp01To100(volume);
            ApplyVolumes();
        }

        public static void SetSfxVolume(float volume)
        {
            _sfxVolume = Clamp01To100(volume);
            ApplyVolumes();
        }

        public static void SetMusicVolume(float volume)
        {
            _musicVolume = Clamp01To100(volume);
            ApplyVolumes();
        }

        public static void PlaySfx(string source, float volume = 100f, bool loop = false)
        {
            string? path = ResolveAudioPath(source, AudioKind.Sfx);
            if (path == null)
            {
                TlpLogging.Warning($"SFX not found: {source}");
                return;
            }

            if (!_soundBufferCache.TryGetValue(path, out var buffer))
            {
                buffer = new SoundBuffer(path);
                _soundBufferCache[path] = buffer;
            }

            var sound = new Sound(buffer)
            {
                Loop = loop,
                Volume = ComposeVolume(volume, _sfxVolume)
            };

            sound.Play();
            _activeSounds.Add(sound);
            _soundBaseVolumes[sound] = Clamp01To100(volume);
        }

        public static void PlayMusic(string source, bool loop = true, float volume = 100f, string channel = "BGM")
        {
            string? path = ResolveAudioPath(source, AudioKind.Music);
            if (path == null)
            {
                TlpLogging.Warning($"Music not found: {source}");
                return;
            }

            StopMusic(channel);

            var music = new Music(path)
            {
                Loop = loop,
                Volume = ComposeVolume(volume, _musicVolume)
            };
            music.Play();

            _musicChannels[channel] = music;
            _musicBaseVolumes[channel] = Clamp01To100(volume);
        }

        public static void StopMusic(string channel = "BGM")
        {
            if (_musicChannels.TryGetValue(channel, out var existing))
            {
                existing.Stop();
                existing.Dispose();
                _musicChannels.Remove(channel);
                _musicBaseVolumes.Remove(channel);
            }
        }

        public static void PauseMusic(string channel = "BGM")
        {
            if (_musicChannels.TryGetValue(channel, out var existing))
                existing.Pause();
        }

        public static void ResumeMusic(string channel = "BGM")
        {
            if (_musicChannels.TryGetValue(channel, out var existing))
                existing.Play();
        }

        public static void StopAllMusic()
        {
            foreach (var music in _musicChannels.Values)
            {
                music.Stop();
                music.Dispose();
            }
            _musicChannels.Clear();
            _musicBaseVolumes.Clear();
        }

        public static void StopAllSfx()
        {
            foreach (var sound in _activeSounds)
            {
                sound.Stop();
                sound.Dispose();
            }
            _activeSounds.Clear();
            _soundBaseVolumes.Clear();
        }

        public static void StopAll()
        {
            StopAllSfx();
            StopAllMusic();
        }

        public static void Update()
        {
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                var s = _activeSounds[i];
                if (s.Status != SoundStatus.Stopped) continue;

                s.Dispose();
                _activeSounds.RemoveAt(i);
                _soundBaseVolumes.Remove(s);
            }
        }

        private static void ApplyVolumes()
        {
            foreach (var sound in _activeSounds)
            {
                float baseVolume = _soundBaseVolumes.TryGetValue(sound, out float vol) ? vol : 100f;
                sound.Volume = ComposeVolume(baseVolume, _sfxVolume);
            }

            foreach (var kv in _musicChannels)
            {
                float baseVolume = _musicBaseVolumes.TryGetValue(kv.Key, out float vol) ? vol : 100f;
                kv.Value.Volume = ComposeVolume(baseVolume, _musicVolume);
            }
        }

        private static float ComposeVolume(float sourceVolume, float groupVolume)
        {
            float src = Clamp01To100(sourceVolume) / 100f;
            float grp = Clamp01To100(groupVolume) / 100f;
            float master = Clamp01To100(_masterVolume) / 100f;
            return src * grp * master * 100f;
        }

        private static float Clamp01To100(float v)
        {
            return System.Math.Max(0f, System.Math.Min(100f, v));
        }

        private static string? ResolveAudioPath(string rawSource, AudioKind kind)
        {
            string source = rawSource?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(source))
                return null;

            bool hasExt = Path.HasExtension(source);
            string[] exts = hasExt ? new[] { "" } : new[] { ".ogg", ".wav", ".mp3", ".flac" };

            var candidates = new List<string>();

            if (Path.IsPathRooted(source))
            {
                candidates.Add(source);
                candidates.AddRange(exts.Where(x => x.Length > 0).Select(ext => source + ext));
                return candidates.FirstOrDefault(File.Exists);
            }

            string? root = TestboxedBridge.ProjectRoot;
            if (string.IsNullOrWhiteSpace(root))
                return null;

            var baseFolders = new List<string>
            {
                root,
                Path.Combine(root, "Sounds"),
                Path.Combine(root, "Audio"),
            };

            if (kind == AudioKind.Music)
            {
                baseFolders.Add(Path.Combine(root, "Sounds", "Music"));
                baseFolders.Add(Path.Combine(root, "Audio", "Music"));
            }

            if (kind == AudioKind.Sfx)
            {
                baseFolders.Add(Path.Combine(root, "Sounds", "Sfx"));
                baseFolders.Add(Path.Combine(root, "Audio", "Sfx"));
            }

            foreach (var folder in baseFolders.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string candidate = Path.Combine(folder, source);
                candidates.Add(candidate);
                foreach (var ext in exts)
                {
                    if (ext.Length == 0) continue;
                    candidates.Add(candidate + ext);
                }
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        private enum AudioKind
        {
            Sfx,
            Music
        }
    }
}
