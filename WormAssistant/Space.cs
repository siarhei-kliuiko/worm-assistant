using System;
using System.Windows.Forms;
using System.Drawing;

namespace WormAssistant
{
    static class Space
    {
        public const float Gravity = 1.3f;
        private static Worm worm;
        private static Timer time = new Timer { Interval = 16 };
        private static System.Threading.Mutex mut; 
        public static int TimeSpeed => time.Interval;
        public static Rectangle Surface => new Rectangle(0, Screen.PrimaryScreen.WorkingArea.Height, Screen.PrimaryScreen.WorkingArea.Width, 1);

        [STAThread]
        static void Main()
        {
            mut = new System.Threading.Mutex(true, "WormMutex", out bool firstRun);
            if (firstRun)
            {
                Application.EnableVisualStyles();
                worm = new Worm();
                time.Tick += Time_Tick;
                time.Start();
                Application.Run(worm);
            }
        }

        static void Time_Tick(object sender, EventArgs e)
        {
            worm.PerformAction();
        }
    }
}
