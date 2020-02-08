using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using Gma.System.MouseKeyHook;

using Gma.System.MouseKeyHook.Implementation;
using System.Security.Principal;
using System.Security.Permissions;

namespace MinecraftAutoLogin
{


    class Program
    {
       #region Native Methods & Variables
        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, uint Msg, long wParam, long lParam);

        [DllImportAttribute("User32.dll")]
        private static extern int FindWindow(String ClassName, String WindowName);

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool SetActiveWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int WS_SHOWNORMAL = 1;
        #endregion

        static string _configpath = AppDomain.CurrentDomain.BaseDirectory + "MinecraftAutoLogin.config";
        static string _launchpath = "MinecraftLauncher.exe.lnk";
        static string _username = "";
        static string _password = "";

        private static IKeyboardMouseEvents m_Events;

        static System.Threading.Timer _t;

        static void Main(string[] args)
        {
            // Hide console window
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            // Close all existing Minecraft Process
            foreach (Process p in System.Diagnostics.Process.GetProcessesByName("MinecraftLauncher"))
            {
                try
                {
                    EndProcess(p.ProcessName);
                }
                catch { }
            }

            // Display dialog on a separate process
            Task.Factory.StartNew(() =>
            {
                SubscribeGlobal();
                int wait = waitBeforeEnteringCredentials + waitAfterTab + waitAfterUsernameEntry + waitAfterTab + numberofpasswordretries * (waitAfterPasswordEntry + waitAfterEnter) + 3 * waitAfterTab;
                CustomMessageBox.Show("Logging in to Minecraft. \n \nPlease wait.", "Minecraft", wait);
            });

            // Get credentials from credentials.txt and hide the file.
            GetConfig();

            // Start timer that checks if the Minecraft window is in the front
            _t = new System.Threading.Timer(TimerCallback, null, 0, 50);

            // Launch MinecraftLauncher.exe in current directory.
            LaunchMinecraft();

            // Start process to enter credentials on Separate thread
            Thread t = new Thread(EnterCredentials);
            t.Start();
        }

        private static void timerClose(object state)
        {
            //Environment.Exit(0);
        }

        private static void SubscribeGlobal()
        {
            Unsubscribe();
            Subscribe(Hook.GlobalEvents());
        }

        private static void Subscribe(IKeyboardMouseEvents events)
        {
            m_Events = events;
            m_Events.KeyDown += OnKeyDown;

            m_Events.MouseDownExt += HookManager_Supress;
        }

        private static void Unsubscribe()
        {
            // I only care about clicks and keys
            if (m_Events == null) return;
            m_Events.KeyDown -= OnKeyDown;

            m_Events.MouseDownExt -= HookManager_Supress;

            m_Events.Dispose();
            m_Events = null;
        }

        // Suppress Key Press
        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
                e.SuppressKeyPress = true;
                return;
        }

        // Supress Mouse Click
        private static void HookManager_Supress(object sender, MouseEventExtArgs e)
        {
            e.Handled = true;
        }

        private static void EndProcessWithMessage(string Message)
        {
            MessageBox.Show(Message, "Minecraft Auto Login");
            EndProcess("MinecraftAutoLogin");
            //Environment.Exit(0);
        }

        static void GetConfig()
        {
            System.Threading.Timer close = new System.Threading.Timer(timerClose, null, 3000, 3000);

            string[] lines, tokens;

            try
            {
                if (!File.Exists(_configpath))
                {
                    EndProcessWithMessage("No config file found.");
                }
                else
                {
                    lines = File.ReadAllLines(_configpath);

                    foreach(string line in lines)
                    {
                        if (line.Contains("FILE PATH="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) _launchpath = tokens[1];
                            }
                        }

                        if (line.Contains("USERNAME="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1) _username = tokens[1];
                        }

                        if (line.Contains("PASSWORD="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1) _password = tokens[1];
                        }

                        if (line.Contains("WAIT BEFORE ENTERING CREDENTIALS="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out waitBeforeEnteringCredentials);
                            }
                        }

                        if (line.Contains("WAIT AFTER TAB="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out waitAfterTab);
                            }
                        }

                        if (line.Contains("WAIT AFTER USERNAME ENTRY="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out waitAfterUsernameEntry);
                            }
                        }

                        if (line.Contains("WAIT AFTER PASSWORD ENTRY="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out waitAfterPasswordEntry);
                            }
                        }

                        if (line.Contains("WAIT AFTER ENTER="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out waitAfterEnter);
                            }
                        }

                        if (line.Contains("NUMBER OF PASSWORD RETRIES="))
                        {
                            tokens = line.Split('=');
                            if (tokens.Length > 1)
                            {
                                if (!String.IsNullOrEmpty(tokens[1]) || !String.IsNullOrWhiteSpace(tokens[1])) int.TryParse(tokens[1], out numberofpasswordretries);
                            }
                        }
                    }

                    File.SetAttributes(_configpath, File.GetAttributes(_configpath) | FileAttributes.Hidden);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void EndProcess(string Name)
        {
            foreach (Process p in System.Diagnostics.Process.GetProcessesByName(Name))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch
                { }
            }
        }

        static void TestTimerCallback(Object o)
        {
            int hWnd2 = FindWindow(null, "Untitled - Notepad");
            if (hWnd2 > 0)
            {
                foreach (Process p in System.Diagnostics.Process.GetProcessesByName("notepad"))
                {
                    try
                    {
                        SetForegroundWindow(p.MainWindowHandle);
                        ShowWindowAsync(p.MainWindowHandle, WS_SHOWNORMAL);
                    }
                    catch { }
                }
            }
        }

        static void TimerCallback(Object o)
        {
            int hWnd2 = FindWindow(null, "Minecraft Launcher");
            if (hWnd2 > 0)
            {
                foreach (Process p in System.Diagnostics.Process.GetProcessesByName("MinecraftLauncher"))
                {
                    try
                    {
                        SetForegroundWindow(p.MainWindowHandle);
                        ShowWindowAsync(p.MainWindowHandle, WS_SHOWNORMAL);

                    }
                    catch { }
                }
            }
        }

        static void LaunchMinecraft()
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = _launchpath;
                p.Start();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " (" + _launchpath + ")");
                Console.ReadKey();
                //Environment.Exit(0);
            }
        }

        static async void EnterCredentials2()
        {
            await Task.Delay(4000);
            m_Events.KeyDown -= OnKeyDown;
            SendKeys.SendWait("{TAB}");
            await Task.Delay(400);

            SendKeys.SendWait(_username);
            await Task.Delay(400);

            SendKeys.SendWait("{TAB}");
            await Task.Delay(400);

            SendKeys.SendWait(_password);
            await Task.Delay(400);

            SendKeys.SendWait("{ENTER}");
            await Task.Delay(1000);

            Unsubscribe();
            //Environment.Exit(0);
        }


        static int waitBeforeEnteringCredentials = 5000;
        static int waitAfterTab = 300;
        static int waitAfterUsernameEntry = 300;
        static int waitAfterPasswordEntry = 300;
        static int waitAfterEnter = 500;
        static int waitCustomMessageTimeToWait = 0;
        static int numberofpasswordretries = 5;

        static void EnterCredentials()
        {

            m_Events.KeyDown -= OnKeyDown;

            Thread.Sleep(waitBeforeEnteringCredentials);


            SendKeys.SendWait("{TAB}");
            wait(waitAfterTab);
            SendKeys.SendWait("{TAB}");
            wait(waitAfterTab);
            SendKeys.SendWait("{TAB}");
            wait(waitAfterTab);
            SendKeys.SendWait(_username);
            wait(waitAfterUsernameEntry);
            SendKeys.SendWait("{TAB}");
            wait(waitAfterTab);

            for(int i = 0; i < numberofpasswordretries; i++)
            {
                SendKeys.SendWait(_password);
                wait(waitAfterPasswordEntry);
                SendKeys.SendWait("{ENTER}");
                wait(waitAfterEnter);
            }

            //SendKeys.SendWait("+{TAB}");
            //wait(waitAfterTab);
            //SendKeys.SendWait("{BS}");
            //SendKeys.SendWait("{TAB}");
            //wait(waitAfterTab);
            //SendKeys.SendWait("{BS}");
            //wait(waitAfterTab);
            //SendKeys.SendWait("{ENTER}");
            //SendKeys.SendWait("+{TAB}");
            //SendKeys.SendWait("{ENTER}");

            Unsubscribe();

            //Environment.Exit(0);
        }

        static void wait(int milliseconds)
        {
            Thread.Sleep(milliseconds);
            waitCustomMessageTimeToWait += milliseconds;
        }
    }
}
