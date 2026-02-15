using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

public class LauncherForm : Form {
    private TextBox searchBox;
    private FlowLayoutPanel listPanel;
    private LauncherModel model;

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.Run(new LauncherForm());
    }

    public LauncherForm() {
        model = new LauncherModel();
        this.Text = "Windows Command Launcher";
        this.Size = new Size(650, 800);
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.StartPosition = FormStartPosition.CenterScreen;
        if (File.Exists("app.ico")) this.Icon = new Icon("app.ico");
        InitUI();
        InitAutoReload();
        this.Resize += (s, e) => RefreshList();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        if (keyData == (Keys.Control | Keys.L)) {
            searchBox.Focus();
            searchBox.SelectAll();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void InitUI() {
        MenuStrip ms = new MenuStrip();
        ms.BackColor = Color.FromArgb(45, 45, 45);
        ms.ForeColor = Color.White;
        ToolStripMenuItem fileMenu = new ToolStripMenuItem("メニュー(&M)");
        ToolStripMenuItem editXmlItem = new ToolStripMenuItem("XMLファイルを編集...(&E)");
        editXmlItem.Click += (s, e) => { try { Process.Start("notepad.exe", "commands.xml"); } catch { } };
        ToolStripMenuItem exitItem = new ToolStripMenuItem("終了(&X)");
        exitItem.Click += (s, e) => Application.Exit();
        fileMenu.DropDownItems.Add(editXmlItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(exitItem);
        ms.Items.Add(fileMenu);

        searchBox = new TextBox {
            Dock = DockStyle.Top, BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White, Font = new Font("Segoe UI", 12), BorderStyle = BorderStyle.FixedSingle
        };
        searchBox.TextChanged += (s, e) => RefreshList();
        searchBox.KeyDown += HandleEmacsKeys;

        Panel listFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        listPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 25, 25),
            FlowDirection = FlowDirection.TopDown, WrapContents = false, BorderStyle = BorderStyle.FixedSingle
        };

        listFrame.Controls.Add(listPanel);
        this.Controls.Add(listFrame);
        this.Controls.Add(searchBox);
        this.Controls.Add(ms);
        
        this.MainMenuStrip = ms;
        RefreshList();
    }

    private void RefreshList() {
        listPanel.Controls.Clear();
        var items = model.GetFilteredList(searchBox.Text);
        int maxItems = Math.Max(1, listPanel.Height / 65);

        foreach (var item in items.Take(maxItems)) {
            var row = LauncherItemRow.Create(
                item, 
                model.Favorites.Contains(item.Name),
                listPanel.Width,
                () => { model.ToggleFavorite(item.Name); RefreshList(); },
                () => { ExecuteCommand(item.Command); }
            );
            listPanel.Controls.Add(row);
        }
    }

    private void ExecuteCommand(string cmd) {
        try {
            string fn = "", ag = "", c = cmd.Trim();
            // 引数付きコマンドの解析（引用符対応）
            if (c.StartsWith("\"")) {
                int n = c.IndexOf("\"", 1);
                if (n > 0) { fn = c.Substring(1, n - 1); ag = c.Substring(n + 1).Trim(); }
                else fn = c;
            } else {
                int sp = c.IndexOf(" ");
                if (sp > 0) { fn = c.Substring(0, sp); ag = c.Substring(sp + 1).Trim(); }
                else fn = c;
            }
            Process.Start(new ProcessStartInfo(fn, ag) { UseShellExecute = true });
        } catch {
            // 解析に失敗した場合は cmd.exe 経由で試行
            try { Process.Start("cmd.exe", "/c " + cmd); } catch { }
        }
        searchBox.Focus();
    }

    private void HandleEmacsKeys(object sender, KeyEventArgs e) {
        if (!e.Control) return;
        var tb = (TextBox)sender; e.SuppressKeyPress = true;
        
        // 現在のカーソル位置を一旦保持
        int cp = tb.SelectionStart;

        switch (e.KeyCode) {
            case Keys.A: tb.SelectionStart = 0; break;
            case Keys.E: tb.SelectionStart = tb.Text.Length; break;
            case Keys.F: if (cp < tb.Text.Length) tb.SelectionStart = cp + 1; break;
            case Keys.B: if (cp > 0) tb.SelectionStart = cp - 1; break;
            case Keys.H: // Backspace (Ctrl-H)
                if (cp > 0) {
                    tb.Text = tb.Text.Remove(cp - 1, 1);
                    tb.SelectionStart = cp - 1; // 削った位置にカーソルを戻す
                } break;
            case Keys.K: // カーソル以降を削除 (Ctrl-K)
                tb.Text = tb.Text.Substring(0, cp);
                tb.SelectionStart = cp; // 【重要】ここを明示することで先頭に戻らなくなります
                break;
            case Keys.U: // カーソル以前を削除 (Ctrl-U)
                string remaining = tb.Text.Substring(cp);
                tb.Text = remaining;
                tb.SelectionStart = 0; // 行頭削除なので 0 で正解
                break;
            default: e.SuppressKeyPress = false; break;
        }
    }

    private void InitAutoReload() {
        Timer t = new Timer { Interval = 2000 };
        t.Tick += (s, e) => { if (model.IsXmlChanged()) { model.LoadCommands(); RefreshList(); } };
        t.Start();
    }
}

