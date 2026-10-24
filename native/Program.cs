using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace DecodGrmNative {
    static class Program {
        [STAThread]
        static void Main() {
            bool created;
            using (var mutex = new Mutex(true, @"Global\DECOD_G_RM_NATIVE_22", out created)) {
                if (!created) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try {
                    EnsureCore();
                    using (var api = new ApiClient("http://127.0.0.1:38521")) {
                        var form = new MainForm(api);
                        Application.Run(form);
                    }
                } catch (Exception ex) {
                    MessageBox.Show("G-Rm başlatılamadı.\r\n\r\n" + ex.Message, "G-Rm", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        static void EnsureCore() {
            if (Ping()) return;
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var core = Path.Combine(dir, "G-Rm-Core.exe");
            if (!File.Exists(core)) throw new FileNotFoundException("G-Rm-Core.exe bulunamadı.", core);
            var psi = new ProcessStartInfo(core, "--background --no-browser") {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = dir
            };
            Process.Start(psi);
            for (int i=0; i<60; i++) {
                Thread.Sleep(150);
                if (Ping()) return;
            }
            throw new Exception("G-Rm veri motoru başlatılamadı.");
        }

        static bool Ping() {
            try {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:38521/api/status");
                req.Timeout = 400;
                req.ReadWriteTimeout = 400;
                using (var resp = (HttpWebResponse)req.GetResponse()) return (int)resp.StatusCode < 500;
            } catch { return false; }
        }
    }
}
