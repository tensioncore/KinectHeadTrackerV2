// Program.cs (FULL FILE)
using System;
using System.Windows.Forms;

namespace KinectHeadtracker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
