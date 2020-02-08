using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows;

namespace MinecraftAutoLogin
{
    public partial class CustomMessageBox : Form
    {
        static CustomMessageBox cmb;
        public static int secondsToWait = 10;
        static System.Windows.Forms.Timer countdown;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        public static void Show(string text, string title, int milliseconds)
        {
            cmb = new CustomMessageBox();
            cmb.Location = new Point(500, 100);
            cmb.StartPosition = FormStartPosition.Manual;
            cmb.Text = title;
            cmb.FormBorderStyle = FormBorderStyle.None;

            cmb.lblMessage.Text = text;
            cmb.WindowState = FormWindowState.Maximized;
            
            secondsToWait = milliseconds / 1000;

            countdown = new System.Windows.Forms.Timer();
            countdown.Tick += new EventHandler(timer_tick);
            countdown.Interval = 1000;
            countdown.Start();
            cmb.label1.Text = $"\r\n{secondsToWait} seconds remaining.".ToUpper();
            cmb.TopMost = true;
            cmb.ShowDialog();
        }

        private static void timer_tick(object sender, EventArgs e)
        {
            secondsToWait--;
            if (secondsToWait == 0) countdown.Stop();
            cmb.label1.Text = $"\r\n{secondsToWait} seconds remaining.".ToUpper();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            cmb.TopMost = true;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private void CustomMessageBox_Load(object sender, EventArgs e)
        {

        }
    }
}
