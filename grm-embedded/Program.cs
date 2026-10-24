using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using ZXing;
using ZXing.Common;

namespace Decod.Grm.Shell;

internal static class Program
{
    private const string MutexName = @"Local\DECOD_GRM_SHELL_V22";
    private const string PipeName = "DECOD_GRM_SHELL_PIPE_V22";
    internal const string BaseUrl = "http://127.0.0.1:38521";

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string targetUrl = args.FirstOrDefault(x => x.StartsWith("--url=", StringComparison.OrdinalIgnoreCase))?.Substring(6) ?? BaseUrl;
        bool background = args.Any(x => string.Equals(x, "--background", StringComparison.OrdinalIgnoreCase));

        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            SendToExisting(targetUrl);
            return;
        }

        using var form = new MainForm(targetUrl, background, PipeName);
        Application.Run(form);
    }

    private static void SendToExisting(string url)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(1200);
            using var sw = new StreamWriter(pipe) { AutoFlush = true };
            sw.WriteLine(url);
        }
        catch { }
    }
}

internal sealed class MainForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly NotifyIcon _tray = new();
    private readonly string _initialUrl;
    private readonly bool _startHidden;
    private readonly string _pipeName;
    private Process? _core;
    private bool _forceExit;
    private bool _ready;

    public MainForm(string initialUrl, bool startHidden, string pipeName)
    {
        _initialUrl = NormalizeLocalUrl(initialUrl);
        _startHidden = startHidden;
        _pipeName = pipeName;

        Text = "G-Rm";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 720);
        BackColor = Color.White;
        Controls.Add(_web);

        ConfigureTray();
        Load += async (_, _) => await InitializeAsync();
        Shown += (_, _) => { if (_startHidden) HideToTray(); };
        FormClosing += OnFormClosing;
        _ = Task.Run(PipeLoopAsync);
    }

    private void ConfigureTray()
    {
        _tray.Text = "G-Rm";
        _tray.Icon = SystemIcons.Application;
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => ShowAndNavigate(Program.BaseUrl);

        var menu = new ContextMenuStrip();
        menu.Items.Add("G-Rm Aç", null, (_, _) => ShowAndNavigate(Program.BaseUrl));
        menu.Items.Add("Ziyaret Hatırlatmaları", null, (_, _) => ShowAndNavigate(Program.BaseUrl + "/#reminders"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıkış", null, async (_, _) => await ExitCompletelyAsync());
        _tray.ContextMenuStrip = menu;
    }

    private async Task InitializeAsync()
    {
        try
        {
            await EnsureCoreAsync();
            var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "G-Rm", "WebView2");
            Directory.CreateDirectory(userData);
            var env = await CoreWebView2Environment.CreateAsync(null, userData);
            await _web.EnsureCoreWebView2Async(env);

            var core = _web.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsBuiltInErrorPageEnabled = true;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;

            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += OnNewWindowRequested;
            core.WebMessageReceived += OnWebMessageReceived;
            core.DownloadStarting += OnDownloadStarting;
            core.ProcessFailed += (_, e) => BeginInvoke((Action)(() => MessageBox.Show(this, "G-Rm görüntü motoru beklenmedik şekilde kapandı.\n\n" + e.ProcessFailedKind, "G-Rm", MessageBoxButtons.OK, MessageBoxIcon.Warning)));

            _ready = true;
            _web.Source = new Uri(_initialUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "G-Rm açılamadı.\n\n" + ex.Message + "\n\nKurulum dosyasını yeniden çalıştırmanız yeterlidir; verileriniz silinmez.",
                "G-Rm", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EnsureCoreAsync()
    {
        if (await IsCoreReadyAsync()) return;

        string corePath = Path.Combine(AppContext.BaseDirectory, "G-Rm-Core.exe");
        if (!File.Exists(corePath)) throw new FileNotFoundException("G-Rm-Core.exe bulunamadı.", corePath);

        _core = Process.Start(new ProcessStartInfo
        {
            FileName = corePath,
            Arguments = "--no-browser",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(250);
            if (await IsCoreReadyAsync()) return;
            if (_core?.HasExited == true) break;
        }
        throw new InvalidOperationException("G-Rm veri motoru başlatılamadı.");
    }

    private async Task<bool> IsCoreReadyAsync()
    {
        try
        {
            using var response = await _http.GetAsync(Program.BaseUrl + "/api/status");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (IsAllowedLocalUrl(e.Uri)) return;
        e.Cancel = true;
        OpenExternal(e.Uri);
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (IsAllowedLocalUrl(e.Uri))
            _web.Source = new Uri(e.Uri);
        else
            OpenExternal(e.Uri);
    }

    private static bool IsAllowedLocalUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp && uri.Host == "127.0.0.1" && uri.Port == 38521;
    }

    private static string NormalizeLocalUrl(string? url)
        => IsAllowedLocalUrl(url) ? url! : Program.BaseUrl;

    private static void OpenExternal(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeMailto) return;
        try { Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true }); } catch { }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string? raw = null;
        try { raw = e.TryGetWebMessageAsString(); } catch { }
        if (string.IsNullOrWhiteSpace(raw)) return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var type) || type.GetString() != "mobilePairing") return;
            string code = root.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
            string user = root.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "";
            string company = root.TryGetProperty("company", out var co) ? co.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(code)) return;
            ShowPairingQr(code, user, company);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Mobil eşleştirme QR kodu oluşturulamadı.\n\n" + ex.Message, "G-Rm", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowPairingQr(string code, string user, string company)
    {
        using var dlg = new Form
        {
            Text = "Mobil Cihaz Eşleştir",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(470, 610),
            BackColor = Color.White
        };

        var title = new Label
        {
            Text = "G-Rm Mobil Eşleştirme",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Color.FromArgb(11, 43, 73),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(20, 20, 430, 45)
        };
        dlg.Controls.Add(title);

        var subtitle = new Label
        {
            Text = $"{user}\n{company}",
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(25, 68, 420, 52)
        };
        dlg.Controls.Add(subtitle);

        using var qr = CreateQrBitmap(code, 330, 330);
        var picture = new PictureBox
        {
            Image = new Bitmap(qr),
            SizeMode = PictureBoxSizeMode.Zoom,
            Bounds = new Rectangle(70, 130, 330, 330),
            BackColor = Color.White
        };
        dlg.Controls.Add(picture);

        var info = new Label
        {
            Text = "Telefonda G-Rm'i açın ve bu QR kodu okutun.\nKod yaklaşık 2 dakika geçerlidir. Eşleşme tamamlandıktan sonra telefon yeniden QR istemez.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(30, 470, 410, 75)
        };
        dlg.Controls.Add(info);

        var close = new Button
        {
            Text = "Kapat",
            BackColor = Color.FromArgb(8, 120, 209),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Bounds = new Rectangle(170, 555, 130, 38)
        };
        close.FlatAppearance.BorderSize = 0;
        close.Click += (_, _) => dlg.Close();
        dlg.Controls.Add(close);

        dlg.ShowDialog(this);
    }

    private static Bitmap CreateQrBitmap(string text, int width, int height)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = width, Height = height, Margin = 1 }
        };
        var pixelData = writer.Write(text);
        var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        try { Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length); }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        try
        {
            string suggested = Path.GetFileName(e.ResultFilePath);
            using var dlg = new SaveFileDialog
            {
                FileName = string.IsNullOrWhiteSpace(suggested) ? "G-Rm_Dosya" : suggested,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                e.ResultFilePath = dlg.FileName;
            else
                e.Cancel = true;
        }
        catch { }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_forceExit) return;
        if (e.CloseReason == CloseReason.WindowsShutDown) { _forceExit = true; return; }

        if (await ShouldRunInTrayAsync())
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _forceExit = true;
        await ShutdownCoreAsync();
    }

    private async Task<bool> ShouldRunInTrayAsync()
    {
        try
        {
            using var res = await _http.GetAsync(Program.BaseUrl + "/api/bootstrap");
            if (!res.IsSuccessStatusCode) return false;
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("state").GetProperty("settings").TryGetProperty("minimizeToTray", out var t) && t.GetBoolean();
        }
        catch { return false; }
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void ShowAndNavigate(string url)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => ShowAndNavigate(url))); return; }
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Maximized;
        Activate();
        BringToFront();
        if (_ready && IsAllowedLocalUrl(url)) _web.Source = new Uri(url);
    }

    private async Task ExitCompletelyAsync()
    {
        _forceExit = true;
        await ShutdownCoreAsync();
        _tray.Visible = false;
        Close();
    }

    private async Task ShutdownCoreAsync()
    {
        try { await _http.PostAsync(Program.BaseUrl + "/api/shutdown", new StringContent("")); } catch { }
        await Task.Delay(150);
        try { if (_core is { HasExited: false }) _core.Kill(true); } catch { }
    }

    private async Task PipeLoopAsync()
    {
        while (!_forceExit)
        {
            try
            {
                using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();
                using var sr = new StreamReader(server);
                string? url = await sr.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(url)) BeginInvoke((Action)(() => ShowAndNavigate(NormalizeLocalUrl(url))));
            }
            catch { await Task.Delay(300); }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _http.Dispose();
            _web.Dispose();
        }
        base.Dispose(disposing);
    }
}