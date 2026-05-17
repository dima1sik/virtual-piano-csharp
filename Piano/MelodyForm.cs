using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Piano
{
    public partial class MelodyForm : Form
    {
        private readonly AppState _state;

        private ListBox _lst;
        private Label _info;

        public MelodyForm(AppState state)
        {
            _state = state;
            InitializeComponent();
            BuildUi();
            RefreshList();
            _state.Changed += StateChanged;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _state.Changed -= StateChanged;
            base.OnFormClosed(e);
        }

        private void StateChanged()
        {
            if (IsDisposed) return;
            BeginInvoke((Action)RefreshList);
        }

        private void BuildUi()
        {
            Text = "Melody";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(18, 20, 26);

            Controls.Clear();

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(24, 26, 34)
            };

            var title = new Label
            {
                Text = "Мелодия",
                AutoSize = true,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(14, 18)
            };

            header.Controls.Add(title);

            _info = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 185, 200),
                Font = new Font("Segoe UI", 9),
                Location = new Point(18, 68)
            };

            _lst = new ListBox
            {
                Location = new Point(18, 90),
                Size = new Size(520, 240),
                BackColor = Color.FromArgb(24, 26, 34),
                ForeColor = Color.FromArgb(220, 225, 235),
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnClear = BlueButton("Clear", 18);
            btnClear.Click += (_, __) => _state.ClearMelody();

            var btnSave = BlueButton("Save", 114);
            btnSave.Click += (_, __) => SaveToFile();

            var btnLoad = BlueButton("Load", 210);
            btnLoad.Click += (_, __) => LoadFromFile();

            var btnDelete = BlueButton("Delete", 306);
            btnDelete.Click += (_, __) => DeleteSelected();

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 36),
                Location = new Point(418, 350),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 34, 46),
                ForeColor = Color.FromArgb(235, 235, 245)
            };

            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(55, 60, 80);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 44, 60);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(32, 38, 54);
            btnClose.Click += (_, __) => Close();

            Controls.Add(header);
            Controls.Add(_info);
            Controls.Add(_lst);
            Controls.Add(btnClear);
            Controls.Add(btnSave);
            Controls.Add(btnLoad);
            Controls.Add(btnDelete);
            Controls.Add(btnClose);
        }

        private Button BlueButton(string text, int x)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(90, 36),
                Location = new Point(x, 350),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 92, 255),
                ForeColor = Color.White
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 110, 255);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 78, 225);
            return b;
        }

        private void RefreshList()
        {
            _lst.Items.Clear();
            foreach (var ev in _state.Melody.OrderBy(x => x.TimeMs))
                _lst.Items.Add($"{ev.TimeMs};{ev.Note}");

            _info.Text = $"Событий: {_state.Melody.Count}";
        }

        private void DeleteSelected()
        {
            int idx = _lst.SelectedIndex;
            if (idx < 0) return;

            string line = _lst.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(line)) return;

            var parts = line.Split(';');
            if (parts.Length < 2) return;

            if (!int.TryParse(parts[0], out int t)) return;
            string note = parts[1].Trim();

            var found = _state.Melody.FirstOrDefault(x => x.TimeMs == t && string.Equals(x.Note, note, StringComparison.OrdinalIgnoreCase));
            if (found == null) return;

            _state.Melody.Remove(found);
            _state.NotifyChanged();
        }

        private void SaveToFile()
        {
            if (_state.Melody.Count == 0)
            {
                MessageBox.Show(this, "Мелодия пустая.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Melody (*.mel)|*.mel|Text (*.txt)|*.txt|All files (*.*)|*.*";
                dlg.FileName = "melody.mel";

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var lines = _state.Melody.OrderBy(x => x.TimeMs).Select(x => $"{x.TimeMs};{x.Note}").ToArray();
                File.WriteAllLines(dlg.FileName, lines);
            }
        }

        private void LoadFromFile()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Melody (*.mel;*.txt)|*.mel;*.txt|All files (*.*)|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var lines = File.ReadAllLines(dlg.FileName);
                var list = new List<NoteEvent>();

                foreach (var raw in lines)
                {
                    var s = (raw ?? "").Trim();
                    if (s.Length == 0) continue;

                    var parts = s.Split(';');
                    if (parts.Length < 2) continue;

                    if (!int.TryParse(parts[0].Trim(), out int t)) continue;
                    var note = parts[1].Trim();
                    if (note.Length == 0) continue;

                    list.Add(new NoteEvent { TimeMs = t, Note = note });
                }

                list = list.OrderBy(x => x.TimeMs).ToList();
                _state.SetMelody(list);
            }
        }
    }
}
