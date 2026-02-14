using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

class WCL {
  private static string port = "8000";
  private static string jsonPath = "WCL.json";
  private static string htmlPath = "index.html";
  private static string url = "http://localhost:" + port + "/";
  private static HttpListener listener;
  private static NotifyIcon trayIcon;

  [STAThread]
  static void Main() {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    ContextMenu contextMenu = new ContextMenu();
    contextMenu.MenuItems.Add("設定ファイルを開く", (s, e) => Process.Start("notepad.exe", jsonPath));
    contextMenu.MenuItems.Add("Webブラウザを開く", (s, e) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
    contextMenu.MenuItems.Add("-");
    contextMenu.MenuItems.Add("終了", (s, e) => {
        trayIcon.Visible = false;
        Environment.Exit(0);
        });

    trayIcon = new NotifyIcon() {
      Icon = SystemIcons.Application,
      ContextMenu = contextMenu,
      Text = "Windows Command Launcher",
      Visible = true
    };

    Thread serverThread = new Thread(StartServer);
    serverThread.IsBackground = true;
    serverThread.Start();

    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    Application.Run();
  }

  static void StartServer() {
    listener = new HttpListener();
    listener.Prefixes.Add(url);
    try { listener.Start(); } catch { return; }
    while (listener.IsListening) {
      try {
        HttpListenerContext context = listener.GetContext();
        HttpListenerRequest req = context.Request;
        HttpListenerResponse res = context.Response;

        if (req.Url.LocalPath == "/run") {
          string encodedCmd = req.QueryString["cmd"];
          if (!string.IsNullOrEmpty(encodedCmd)) {
            byte[] data = Convert.FromBase64String(encodedCmd);
            string fullCommand = Encoding.UTF8.GetString(data);
            ExecuteDirectly(fullCommand);
          }
          SendResponse(res, "OK", "text/plain");
        }
        else if (req.Url.LocalPath == "/check-reload") {
          SendResponse(res, (req.QueryString["v"] != GetTimestamp() ? "reload" : "ok"), "text/plain");
        }
        else if (req.Url.LocalPath == "/") {
          string json = File.Exists(jsonPath) ? File.ReadAllText(jsonPath, Encoding.UTF8) : "[]";
          string html = GetHtmlTemplate();
          // プレースホルダーを動的データで置換
          string finalHtml = html.Replace("MY_DATA_JSON", json).Replace("MY_TIMESTAMP", GetTimestamp());
          SendResponse(res, finalHtml, "text/html; charset=utf-8");
        }
        else { res.StatusCode = 404; res.Close(); }
      } catch { }
    }
  }

  static void ExecuteDirectly(string fullCommand) {
    try {
      string fileName = "", arguments = "";
      fullCommand = fullCommand.Trim();
      if (fullCommand.StartsWith("\"")) {
        int nextQuote = fullCommand.IndexOf("\"", 1);
        fileName = fullCommand.Substring(1, nextQuote - 1);
        arguments = fullCommand.Substring(nextQuote + 1).Trim();
      } else {
        int firstSpace = fullCommand.IndexOf(" ");
        if (firstSpace > 0) {
          fileName = fullCommand.Substring(0, firstSpace);
          arguments = fullCommand.Substring(firstSpace + 1).Trim();
        } else { fileName = fullCommand; }
      }
      Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
    } catch {
      try { Process.Start(new ProcessStartInfo("cmd.exe", "/c " + fullCommand)); } catch { }
    }
  }

  static string GetHtmlTemplate() {
    if (File.Exists(htmlPath)) {
      return File.ReadAllText(htmlPath, Encoding.UTF8);
    }
    return "<html><body><h1>index.html not found</h1></body></html>";
  }

  static void SendResponse(HttpListenerResponse res, string text, string contentType) {
    byte[] buffer = Encoding.UTF8.GetBytes(text);
    res.ContentType = contentType;
    res.ContentLength64 = buffer.Length;
    res.OutputStream.Write(buffer, 0, buffer.Length);
    res.Close();
  }

  static string GetTimestamp() {
    return File.Exists(jsonPath) ? File.GetLastWriteTime(jsonPath).Ticks.ToString() : "0";
  }
}
