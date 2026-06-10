using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PianoAtlasRelease
{
    internal static class PianoAtlasUninstaller
    {
        private const string AppName = "Piano Atlas";

        [STAThread]
        private static int Main(string[] args)
        {
            bool silent = HasArg(args, "/silent");

            if (!silent)
            {
                DialogResult confirm = MessageBox.Show(
                    "Uninstall Piano Atlas from this computer?",
                    AppName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return 0;
            }

            try
            {
                string installDir = InstallDirectory();

                TryDelete(DesktopShortcutPath());
                TryDelete(StartMenuShortcutPath());
                TryDeleteRegistry();

                bool removeUserData = false;
                if (!silent)
                {
                    DialogResult dataChoice = MessageBox.Show(
                        "Do you also want to remove saved favorites and user data?\n\nChoose No if you may install Piano Atlas again later.",
                        AppName,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    removeUserData = dataChoice == DialogResult.Yes;
                }

                if (removeUserData)
                {
                    TryDeleteDirectory(UserDataDirectory());
                }

                ScheduleInstallFolderRemoval(installDir);

                if (!silent)
                {
                    MessageBox.Show("Piano Atlas has been uninstalled.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Piano Atlas could not be fully uninstalled.\n\n" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static bool HasArg(string[] args, string value)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string InstallDirectory()
        {
            string overrideDir = Environment.GetEnvironmentVariable("PIANO_ATLAS_INSTALL_DIR");
            if (!string.IsNullOrEmpty(overrideDir)) return overrideDir;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                AppName);
        }

        private static string UserDataDirectory()
        {
            string overrideDir = Environment.GetEnvironmentVariable("PIANO_ATLAS_USERDATA_DIR");
            if (!string.IsNullOrEmpty(overrideDir)) return overrideDir;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);
        }

        private static string DesktopShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");
        }

        private static string StartMenuShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", AppName + ".lnk");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch { }
        }

        private static void TryDeleteRegistry()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PianoAtlas", false);
            }
            catch { }
        }

        private static void ScheduleInstallFolderRemoval(string installDir)
        {
            if (!Directory.Exists(installDir)) return;

            string command = "/C timeout /T 2 /NOBREAK >NUL & rmdir /S /Q \"" + installDir + "\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = command,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
    }
}
