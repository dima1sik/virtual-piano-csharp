using System;
using System.Windows.Forms;

namespace Piano
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var state = new AppState();
            SoundPackGenerator.EnsureGenerated();
            Application.Run(new MainForm(state));

        }
    }
}
