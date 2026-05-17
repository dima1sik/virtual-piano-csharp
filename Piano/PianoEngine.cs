using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace Piano
{
    public sealed class PianoEngine
    {
        private readonly AppState _state;
        private readonly Dictionary<string, SoundPlayer> _cache = new Dictionary<string, SoundPlayer>(StringComparer.OrdinalIgnoreCase);

        public PianoEngine(AppState state)
        {
            _state = state;
        }

        public void PlayNote(string note)
        {
            string path = GetPath(note);
            if (!File.Exists(path)) return;

            try
            {
                var p = GetPlayer(path);
                p.Stop();
                p.Play();
            }
            catch { }
        }

        public void StopAll()
        {
            foreach (var p in _cache.Values)
            {
                try { p.Stop(); } catch { }
            }
        }

        private SoundPlayer GetPlayer(string path)
        {
            if (_cache.TryGetValue(path, out var p)) return p;

            p = new SoundPlayer(path);
            try { p.LoadAsync(); } catch { }
            _cache[path] = p;
            return p;
        }

        private string GetPath(string note)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string setName = _state.SoundSet.ToString();
            string file = note.Replace("#", "s") + ".wav";
            return Path.Combine(baseDir, "Sounds", setName, file);
        }
    }
}
