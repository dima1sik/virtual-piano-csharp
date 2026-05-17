using System;
using System.Collections.Generic;

namespace Piano
{
    public enum SoundSet
    {
        Piano = 0,
        Organ = 1,
        Synth = 2
    }

    public sealed class AppState
    {
        public event Action Changed;

        private SoundSet _soundSet = SoundSet.Piano;
        private int _volume = 80;

        public SoundSet SoundSet
        {
            get => _soundSet;
            set
            {
                if (_soundSet == value) return;
                _soundSet = value;
                Changed?.Invoke();
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                int v = value;
                if (v < 0) v = 0;
                if (v > 100) v = 100;
                if (_volume == v) return;
                _volume = v;
                Changed?.Invoke();
            }
        }

        public List<NoteEvent> Melody { get; } = new List<NoteEvent>();

        public void ClearMelody()
        {
            Melody.Clear();
            Changed?.Invoke();
        }

        public void AddEvent(NoteEvent ev)
        {
            Melody.Add(ev);
            Changed?.Invoke();
        }

        public void SetMelody(List<NoteEvent> list)
        {
            Melody.Clear();
            Melody.AddRange(list);
            Changed?.Invoke();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
