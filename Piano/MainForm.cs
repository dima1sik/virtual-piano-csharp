using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Piano
{
    public partial class MainForm : Form
    {
        private readonly AppState _state;
        private readonly PianoEngine _engine;

        private Label _noteLabel;
        private Button _btnRecord;
        private Button _btnStop;
        private Button _btnPlay;

        private bool _recording;
        private bool _playing;

        private readonly Stopwatch _sw = new Stopwatch();
        private CancellationTokenSource _cts;

        private readonly Dictionary<string, Button> _keys = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

        public MainForm(AppState state)
        {
            _state = state;
            _engine = new PianoEngine(_state);

            InitializeComponent();
            BuildUi();

            _state.Changed += () =>
            {
                if (!IsDisposed) BeginInvoke((Action)UpdateButtons);
            };
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopAll();
            base.OnFormClosing(e);
        }

        private void BuildUi()
        {
            Text = "Piano";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(980, 560);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 20, 26);

            Controls.Clear();

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(24, 26, 34)
            };

            var title = new Label
            {
                Text = "Piano",
                AutoSize = true,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(14, 18)
            };

            var btnSettings = TopButton("Settings");
            btnSettings.Location = new Point(140, 18);
            btnSettings.Click += (_, __) =>
            {
                using (var f = new SettingsForm(_state))
                    f.ShowDialog(this);
            };

            var btnMelody = TopButton("Melody");
            btnMelody.Location = new Point(250, 18);
            btnMelody.Click += (_, __) =>
            {
                using (var f = new MelodyForm(_state))
                    f.ShowDialog(this);
            };

            _noteLabel = new Label
            {
                Text = "",
                Size = new Size(260, 30),
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(top.Width - 16 - 260, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            top.SizeChanged += (_, __) =>
            {
                _noteLabel.Location = new Point(top.Width - 16 - _noteLabel.Width, 18);
            };

            top.Controls.Add(title);
            top.Controls.Add(btnSettings);
            top.Controls.Add(btnMelody);
            top.Controls.Add(_noteLabel);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(24, 26, 34)
            };

            var lblControl = new Label
            {
                Text = "Control",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 205, 220),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(16, 14)
            };

            _btnRecord = ActionButton("Record");
            _btnRecord.Location = new Point(16, 48);
            _btnRecord.Click += (_, __) => StartRecord();

            _btnStop = ActionButton("Stop");
            _btnStop.Location = new Point(148, 48);
            _btnStop.Click += (_, __) => StopAll();

            _btnPlay = ActionButton("Play");
            _btnPlay.Location = new Point(280, 48);
            _btnPlay.Click += async (_, __) => await StartPlay();

            bottom.Controls.Add(lblControl);
            bottom.Controls.Add(_btnRecord);
            bottom.Controls.Add(_btnStop);
            bottom.Controls.Add(_btnPlay);

            var center = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(18, 20, 26)
            };

            var lblKeyboard = new Label
            {
                Text = "Keyboard",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 205, 220),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(16, 10)
            };

            var area = new Panel
            {
                Location = new Point(16, 36),
                Size = new Size(930, 360),
                BackColor = Color.FromArgb(28, 30, 40)
            };

            area.Paint += (_, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(60, 65, 85), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, area.Width - 1, area.Height - 1);
            };

            center.Controls.Add(lblKeyboard);
            center.Controls.Add(area);

            Controls.Add(center);
            Controls.Add(bottom);
            Controls.Add(top);

            BuildKeyboard(area);
            UpdateButtons();
        }

        private void BuildKeyboard(Panel host)
        {
            host.Controls.Clear();
            _keys.Clear();

            int startX = 50;
            int startY = 70;
            int whiteW = 100;
            int whiteH = 240;
            int blackW = 62;
            int blackH = 150;

            string[] white = { "C", "D", "E", "F", "G", "A", "B" };

            for (int i = 0; i < white.Length; i++)
            {
                string note = white[i] + "4";
                var b = new Button
                {
                    Tag = note,
                    Size = new Size(whiteW, whiteH),
                    Location = new Point(startX + i * whiteW, startY),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(244, 245, 248),
                    TabStop = false
                };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);

                b.MouseDown += (_, __) =>
                {
                    SetPressed(b, false, true);
                    NotePressed(note);
                };
                b.MouseUp += (_, __) => SetPressed(b, false, false);
                b.MouseLeave += (_, __) => SetPressed(b, false, false);

                host.Controls.Add(b);
                _keys[note] = b;
            }

            (string Note, int Left)[] black =
            {
                ("C#4", 0),
                ("D#4", 1),
                ("F#4", 3),
                ("G#4", 4),
                ("A#4", 5)
            };

            for (int i = 0; i < black.Length; i++)
            {
                int x = startX + (black[i].Left + 1) * whiteW - (blackW / 2);
                string note = black[i].Note;

                var b = new Button
                {
                    Tag = note,
                    Size = new Size(blackW, blackH),
                    Location = new Point(x, startY),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(30, 31, 38),
                    TabStop = false
                };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(15, 16, 20);

                b.MouseDown += (_, __) =>
                {
                    SetPressed(b, true, true);
                    NotePressed(note);
                };
                b.MouseUp += (_, __) => SetPressed(b, true, false);
                b.MouseLeave += (_, __) => SetPressed(b, true, false);

                host.Controls.Add(b);
                b.BringToFront();
                _keys[note] = b;
            }
        }

        private void NotePressed(string note)
        {
            _noteLabel.Text = note;
            _engine.PlayNote(note);

            if (_recording)
            {
                _state.AddEvent(new NoteEvent
                {
                    TimeMs = (int)_sw.ElapsedMilliseconds,
                    Note = note
                });
            }
        }

        private void StartRecord()
        {
            if (_playing) return;

            _state.ClearMelody();
            _sw.Reset();
            _sw.Start();
            _recording = true;
            _noteLabel.Text = "REC";
            UpdateButtons();
        }

        private async Task StartPlay()
        {
            if (_recording) return;
            if (_playing) return;
            if (_state.Melody.Count == 0) return;

            _playing = true;
            _cts = new CancellationTokenSource();
            UpdateButtons();

            try
            {
                var token = _cts.Token;
                var list = _state.Melody.OrderBy(x => x.TimeMs).ToList();

                int prev = 0;
                foreach (var ev in list)
                {
                    token.ThrowIfCancellationRequested();

                    int delay = ev.TimeMs - prev;
                    if (delay < 0) delay = 0;

                    await Task.Delay(delay, token);

                    _noteLabel.Text = ev.Note;
                    Flash(ev.Note, 120);
                    _engine.PlayNote(ev.Note);

                    prev = ev.TimeMs;
                }

                _noteLabel.Text = "";
            }
            catch { }
            finally
            {
                _playing = false;
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }
                UpdateButtons();
            }
        }

        private void StopAll()
        {
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
            }

            if (_recording)
            {
                _recording = false;
                try { _sw.Stop(); } catch { }
            }

            _engine.StopAll();
            _noteLabel.Text = "";
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool has = _state.Melody.Count > 0;

            _btnRecord.Enabled = !_playing && !_recording;
            _btnPlay.Enabled = !_playing && !_recording && has;
            _btnStop.Enabled = _playing || _recording;
        }

        private void Flash(string note, int ms)
        {
            if (!_keys.TryGetValue(note, out var b)) return;

            bool isBlack = note.Contains("#");
            SetPressed(b, isBlack, true);

            _ = Task.Run(async () =>
            {
                await Task.Delay(ms);
                if (b.IsDisposed) return;

                try
                {
                    b.BeginInvoke((Action)(() =>
                    {
                        if (!b.IsDisposed)
                            SetPressed(b, isBlack, false);
                    }));
                }
                catch { }
            });
        }

        private void SetPressed(Button b, bool isBlack, bool pressed)
        {
            if (isBlack)
                b.BackColor = pressed ? Color.FromArgb(24, 25, 32) : Color.FromArgb(30, 31, 38);
            else
                b.BackColor = pressed ? Color.FromArgb(214, 224, 238) : Color.FromArgb(244, 245, 248);
        }

        private Button TopButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(96, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(30, 34, 46),
                ForeColor = Color.FromArgb(235, 235, 245)
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(55, 60, 80);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 44, 60);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(32, 38, 54);
            return b;
        }

        private Button ActionButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(120, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 92, 255),
                ForeColor = Color.White
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 110, 255);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 78, 225);
            return b;
        }
    }
}
