using System;
using System.IO;
using System.Windows.Forms;
using SafeKeyBinder.Native;
using SafeKeyBinder.Services;
using SafeKeyBinder.UI;

namespace SafeKeyBinder
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Win32Native.SetProcessDPIAware();
            }
            catch
            {
                // Non-critical on older environments
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string configPath = Path.Combine(exeDir, "SafeKeyBinder.config.xml");

            ConfigService configService = new ConfigService(configPath);
            WindowService windowService = new WindowService();
            InputSenderService inputSender = new InputSenderService();

            using (KeyboardHookService hookService = new KeyboardHookService(windowService, inputSender))
            using (AutoSequenceService autoService = new AutoSequenceService(windowService, inputSender, hookService))
            {
                Application.Run(new MainForm(configService, windowService, inputSender, hookService, autoService));
            }
        }
    }
}
