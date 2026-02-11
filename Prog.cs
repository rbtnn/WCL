using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TypedPathsSync
{
  public class SyncApplicationContext : ApplicationContext
  {
    private NotifyIcon trayIcon;
    private FileSystemWatcher watcher;
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths";

    private string targetFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "paths.txt");

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

    public SyncApplicationContext()
    {
      if (!File.Exists(targetFilePath))
      {
        File.WriteAllText(targetFilePath, "C:\\\nC:\\Windows");
      }

      trayIcon = new NotifyIcon()
      {
        Icon = SystemIcons.Application,
        Visible = true,
        Text = "TypedPaths Sync (C# 5.0)",
        ContextMenuStrip = CreateContextMenu()
      };

      watcher = new FileSystemWatcher();
      watcher.Path = Path.GetDirectoryName(targetFilePath);
      watcher.Filter = Path.GetFileName(targetFilePath);
      watcher.NotifyFilter = NotifyFilters.LastWrite;
      watcher.Changed += OnFileChanged;
      watcher.EnableRaisingEvents = true;

      SyncRegistry();
    }

    private ContextMenuStrip CreateContextMenu()
    {
      var menu = new ContextMenuStrip();
      menu.Items.Add("設定ファイルを開く", null, (s, e) => System.Diagnostics.Process.Start("notepad.exe", targetFilePath));
      menu.Items.Add("今すぐ同期", null, (s, e) => SyncRegistry());
      menu.Items.Add("-");
      menu.Items.Add("終了", null, Exit);
      return menu;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
      System.Threading.Thread.Sleep(500);
      SyncRegistry();
    }

    private void SyncRegistry()
    {
      try
      {
        string[] paths = File.ReadAllLines(targetFilePath)
          .Where(line => !string.IsNullOrWhiteSpace(line))
          .ToArray();

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
          if (key == null) return;

          foreach (string name in key.GetValueNames())
          {
            key.DeleteValue(name);
          }

          for (int i = 0; i < paths.Length; i++)
          {
            string valueName = string.Format("url{0}", i + 1);
            key.SetValue(valueName, paths[i]);
          }
        }

        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
      }
      catch (Exception ex)
      {
        MessageBox.Show(string.Format("同期エラー: {0}", ex.Message));
      }
    }

    void Exit(object sender, EventArgs e)
    {
      watcher.EnableRaisingEvents = false;
      watcher.Dispose();
      trayIcon.Visible = false;
      Application.Exit();
    }
  }

  static class Program
  {
    [STAThread]
    static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new SyncApplicationContext());
    }
  }
}
