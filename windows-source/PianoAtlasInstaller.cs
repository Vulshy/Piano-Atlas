using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PianoAtlasRelease
{
    internal static class PianoAtlasInstaller
    {
        private const string AppName = "Piano Atlas";
        private const string AppVersion = "1.0.0";
        private const string Publisher = "Vulshy";
        private const string PayloadResourceName = "PianoAtlas.Payload.zip";

        [STAThread]
        private static int Main(string[] args)
        {
            bool silent = HasArg(args, "/silent");

            try
            {
                string installDir = InstallDirectory();
                string tempZip = Path.Combine(Path.GetTempPath(), "PianoAtlasPayload-" + Guid.NewGuid().ToString("N") + ".zip");

                Directory.CreateDirectory(installDir);
                ExtractPayloadToFile(tempZip);
                ExtractZipSafe(tempZip, installDir);
                TryDelete(tempZip);

                string runExe = Path.Combine(installDir, "run.exe");
                string uninstallExe = Path.Combine(installDir, "uninstaller.exe");
                string iconPath = Path.Combine(installDir, "app", "PianoAtlas.ico");

                bool skipShellRegistration = Environment.GetEnvironmentVariable("PIANO_ATLAS_SKIP_SHELL_REGISTRATION") == "1";
                if (!skipShellRegistration)
                {
                    CreateShortcut(DesktopShortcutPath(), runExe, installDir, iconPath, "Piano Atlas");
                    CreateShortcut(StartMenuShortcutPath(), runExe, installDir, iconPath, "Piano Atlas");
                    WriteUninstallEntry(installDir, runExe, uninstallExe);
                }

                if (!silent)
                {
                    MessageBox.Show(
                        "Piano Atlas has been installed.\n\nA Desktop shortcut and Start Menu shortcut were created.",
                        AppName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return 0;
            }
            catch (Exception ex)
            {
                if (silent)
                {
                    TryWriteLog("Install failed: " + ex);
                }
                else
                {
                    MessageBox.Show("Piano Atlas could not be installed.\n\n" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

        private static string DesktopShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");
        }

        private static string StartMenuShortcutPath()
        {
            string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            Directory.CreateDirectory(programs);
            return Path.Combine(programs, AppName + ".lnk");
        }

        private static void ExtractPayloadToFile(string tempZip)
        {
            Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName);
            if (payload == null) throw new InvalidOperationException("Installer payload was not found.");

            using (payload)
            using (FileStream output = File.Create(tempZip))
            {
                payload.CopyTo(output);
            }
        }

        private static void ExtractZipSafe(string zipPath, string destination)
        {
            string destinationFull = Path.GetFullPath(destination);

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string target = Path.GetFullPath(Path.Combine(destinationFull, entry.FullName));
                    if (!target.StartsWith(destinationFull, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Installer payload contains an unsafe file path.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    entry.ExtractToFile(target, true);
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                Type shortcutType = shortcut.GetType();

                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                if (File.Exists(iconPath))
                {
                    shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
                }
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);

                Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
            catch
            {
                // Shortcuts are helpful, but installation should still succeed if Windows blocks shortcut creation.
            }
        }

        private static void WriteUninstallEntry(string installDir, string runExe, string uninstallExe)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PianoAtlas"))
            {
                key.SetValue("DisplayName", AppName);
                key.SetValue("DisplayVersion", AppVersion);
                key.SetValue("Publisher", Publisher);
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", runExe);
                key.SetValue("UninstallString", "\"" + uninstallExe + "\"");
                key.SetValue("QuietUninstallString", "\"" + uninstallExe + "\" /silent");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", EstimateSizeKb(installDir), RegistryValueKind.DWord);
            }
        }

        private static int EstimateSizeKb(string directory)
        {
            long bytes = 0;
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                try { bytes += new FileInfo(file).Length; } catch { }
            }
            return Math.Max(1, (int)(bytes / 1024));
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private static void TryWriteLog(string message)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "PianoAtlas-install.log");
                File.WriteAllText(logPath, message);
            }
            catch { }
        }
    }
}
