using System;
using System.Drawing;
using System.Windows.Forms;

namespace Piano
{
    public partial class SettingsForm : Form
    {
        private readonly AppState _state;

        private ComboBox _cmb;
        private TrackBar _trb;
        private Label _vol;

        public SettingsForm(AppState state)
        {
            _state = state;
            InitializeComponent();
            BuildUi();
            LoadState();
        }

        private void BuildUi()
        {
            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 290);
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
                Text = "Настройки",
                AutoSize = true,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(14, 18)
            };

            header.Controls.Add(title);

            var lblSet = new Label
            {
                Text = "Sound Set",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 205, 220),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(18, 84)
            };

            _cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(18, 106),
                Size = new Size(220, 24)
            };

            _cmb.Items.AddRange(new object[] { "Piano", "Organ", "Synth" });
            _cmb.SelectedIndexChanged += (_, __) =>
            {
                _state.SoundSet = (SoundSet)_cmb.SelectedIndex;
            };

            var lblVol = new Label
            {
                Text = "Volume",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 205, 220),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(18, 150)
            };

            _vol = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 205, 220),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(85, 150)
            };

            _trb = new TrackBar
            {
                Location = new Point(18, 170),
                Size = new Size(410, 45),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10
            };

            _trb.ValueChanged += (_, __) =>
            {
                _state.Volume = _trb.Value;
                _vol.Text = _trb.Value.ToString();
            };

            var btnClose = new Button
            {
                Text = "Close",
                Size = new Size(120, 36),
                Location = new Point(308, 232),
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
            Controls.Add(lblSet);
            Controls.Add(_cmb);
            Controls.Add(lblVol);
            Controls.Add(_vol);
            Controls.Add(_trb);
            Controls.Add(btnClose);
        }

        private void LoadState()
        {
            _cmb.SelectedIndex = (int)_state.SoundSet;
            _trb.Value = _state.Volume;
            _vol.Text = _state.Volume.ToString();
        }
    }
}
