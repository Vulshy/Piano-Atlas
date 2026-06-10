using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PianoAtlasRelease
{
    public static class PianoAtlasComponent
    {
        public static int Run(IntPtr args, int sizeBytes)
        {
            try
            {
                string commandLine = sizeBytes > 0 ? (Marshal.PtrToStringUni(args, Math.Max(0, (sizeBytes / 2) - 1)) ?? string.Empty) : string.Empty;
                string[] parsedArgs = new[]
                {
                    commandLine.IndexOf("/self-test", StringComparison.OrdinalIgnoreCase) >= 0 ? "/self-test" : null,
                    commandLine.IndexOf("/load-test", StringComparison.OrdinalIgnoreCase) >= 0 ? "/load-test" : null
                }.Where(arg => arg != null).ToArray();

                return PianoAtlasLauncher.Run(parsedArgs);
            }
            catch (Exception ex)
            {
                TryWriteStartupLog(ex);
                return 99;
            }
        }

        private static void TryWriteStartupLog(Exception ex)
        {
            try
            {
                string baseDir = Environment.GetEnvironmentVariable("PIANO_ATLAS_BASE_DIR");
                if (string.IsNullOrWhiteSpace(baseDir)) baseDir = AppDomain.CurrentDomain.BaseDirectory;
                File.WriteAllText(Path.Combine(baseDir, "PianoAtlas-startup-error.txt"), ex.ToString());
            }
            catch { }
        }
    }

    internal static class PianoAtlasLauncher
    {
        private const string AppName = "Piano Atlas";

        [STAThread]
        private static int Main(string[] args)
        {
            return Run(args);
        }

        internal static int Run(string[] args)
        {
            string baseDir = BaseDirectory();
            string appPath = FindAppFile(baseDir);
            bool selfTest = HasArg(args, "/self-test");
            bool loadTest = HasArg(args, "/load-test");

            if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
            {
                if (!selfTest) ShowMessage("The Piano Atlas app file was not found.\n\nExpected location:\n" + Path.Combine(baseDir, "app", "PianoAtlas.html"));
                return 2;
            }

            string appDataDir = UserDataDirectory();
            Directory.CreateDirectory(appDataDir);

            if (selfTest)
            {
                return Directory.Exists(appDataDir) ? 0 : 4;
            }

            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                PianoAtlasForm form = new PianoAtlasForm(appPath, appDataDir, FindIcon(baseDir), loadTest);
                Application.Run(form);
                return loadTest ? form.ExitCode : 0;
            }
            catch (Exception ex)
            {
                ShowMessage("Piano Atlas could not be started.\n\n" + ex.Message);
                return 5;
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

        private static string BaseDirectory()
        {
            string baseDir = Environment.GetEnvironmentVariable("PIANO_ATLAS_BASE_DIR");
            if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
            {
                return Path.GetFullPath(baseDir);
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string UserDataDirectory()
        {
            string appDataDir = Environment.GetEnvironmentVariable("PIANO_ATLAS_USERDATA_DIR");
            if (!string.IsNullOrEmpty(appDataDir)) return appDataDir;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName,
                "UserData");
        }

        private static string FindAppFile(string baseDir)
        {
            string[] candidates =
            {
                Path.Combine(baseDir, "app", "PianoAtlas.html"),
                Path.Combine(baseDir, "PianoAtlas.html"),
                Path.Combine(baseDir, "app", "piano-scales-chords.html")
            };

            string match = candidates.FirstOrDefault(File.Exists);
            return string.IsNullOrEmpty(match) ? null : Path.GetFullPath(match);
        }

        private static string FindIcon(string baseDir)
        {
            string[] candidates =
            {
                Path.Combine(baseDir, "app", "PianoAtlas.ico"),
                Path.Combine(baseDir, "PianoAtlas.ico")
            };

            string match = candidates.FirstOrDefault(File.Exists);
            return string.IsNullOrEmpty(match) ? null : Path.GetFullPath(match);
        }

        private static void ShowMessage(string message)
        {
            MessageBox.Show(message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    internal sealed class PianoAtlasForm : Form
    {
        private readonly string appPath;
        private readonly string appDataDir;
        private readonly bool loadTest;
        private readonly WebView2 webView;
        private bool nativeFullscreen;
        private Rectangle previousBounds;
        private FormBorderStyle previousBorderStyle;
        private FormWindowState previousWindowState;

        public int ExitCode { get; private set; }

        public PianoAtlasForm(string appPath, string appDataDir, string iconPath, bool loadTest)
        {
            this.appPath = appPath;
            this.appDataDir = appDataDir;
            this.loadTest = loadTest;
            ExitCode = 0;

            Text = "Piano Atlas";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1024, 680);
            BackColor = Color.FromArgb(17, 17, 17);
            previousBounds = Bounds;
            previousBorderStyle = FormBorderStyle;
            previousWindowState = WindowState;

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(17, 17, 17),
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = appDataDir
                }
            };

            Controls.Add(webView);
            Shown += async (sender, args) => await InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.WebMessageReceived += (sender, args) =>
                {
                    if (IsToggleFullscreenMessage(args.WebMessageAsJson))
                    {
                        ToggleNativeFullscreen();
                    }
                };
                webView.CoreWebView2.DocumentTitleChanged += (sender, args) =>
                {
                    string title = webView.CoreWebView2.DocumentTitle;
                    Text = string.IsNullOrWhiteSpace(title) ? "Piano Atlas" : title;
                    if (loadTest && !string.IsNullOrWhiteSpace(title))
                    {
                        ExitCode = 0;
                        BeginInvoke(new Action(Close));
                    }
                };
                webView.CoreWebView2.NavigationCompleted += (sender, args) =>
                {
                    if (!loadTest) return;
                    ExitCode = args.IsSuccess ? 0 : 7;
                    BeginInvoke(new Action(Close));
                };

                string appUrl = new UriBuilder(Uri.UriSchemeFile, string.Empty)
                {
                    Path = Path.GetFullPath(appPath)
                }.Uri.AbsoluteUri;
                webView.Source = new Uri(appUrl, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                if (loadTest)
                {
                    TryWriteLoadTestLog(ex);
                    ExitCode = 6;
                    BeginInvoke(new Action(Close));
                    return;
                }

                MessageBox.Show(
                    "Piano Atlas could not load the app window.\n\n" +
                    "Microsoft Edge WebView2 Runtime is required and is normally included with Windows 10 and Windows 11.\n\n" +
                    ex.Message,
                    "Piano Atlas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool IsToggleFullscreenMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (!document.RootElement.TryGetProperty("type", out JsonElement type)) return false;
                    return string.Equals(type.GetString(), "toggle-fullscreen", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return json.IndexOf("toggle-fullscreen", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void ToggleNativeFullscreen()
        {
            if (!nativeFullscreen)
            {
                previousBounds = Bounds;
                previousBorderStyle = FormBorderStyle;
                previousWindowState = WindowState;

                nativeFullscreen = true;
                SuspendLayout();
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromHandle(Handle).Bounds;
                ResumeLayout(true);
            }
            else
            {
                nativeFullscreen = false;
                SuspendLayout();
                FormBorderStyle = previousBorderStyle;
                WindowState = FormWindowState.Normal;
                Bounds = previousBounds;
                WindowState = previousWindowState;
                ResumeLayout(true);
            }

            PostFullscreenState();
        }

        private void PostFullscreenState()
        {
            try
            {
                string json = nativeFullscreen
                    ? "{\"type\":\"fullscreen-state\",\"isFullscreen\":true}"
                    : "{\"type\":\"fullscreen-state\",\"isFullscreen\":false}";
                webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch { }
        }

        private static void TryWriteLoadTestLog(Exception ex)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PianoAtlas-load-test-error.txt"),
                    ex.ToString());
            }
            catch { }
        }
    }
}
