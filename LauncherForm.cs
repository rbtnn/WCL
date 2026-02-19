using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.IO;

public class LauncherForm : Form {
    private TextBox searchBox;
    private FlowLayoutPanel listPanel;
    private LauncherModel model;
    private List<CommandItem> currentFilteredItems = new List<CommandItem>();
    private int selectedIndex = 0; // 現在選択されている項目のインデックス

    // 選択状態の色定義
    private Color ColorSelected = Color.FromArgb(50, 50, 50);
    private Color ColorDefault = Color.FromArgb(25, 25, 25);

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.Run(new LauncherForm());
    }

    public LauncherForm() {
        model = new LauncherModel();
        this.Text = "Windows Command Launcher";
        this.Size = new Size(1000, 600);
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.StartPosition = FormStartPosition.CenterScreen;
        if (File.Exists("app.ico")) this.Icon = new Icon("app.ico");

        this.KeyPreview = true;
        InitUI();
        InitAutoReload();
        this.Resize += (s, e) => RefreshList();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        // Ctrl+L: 検索ボックスへ
        if (keyData == (Keys.Control | Keys.L)) {
            searchBox.Focus();
            searchBox.SelectAll();
            return true;
        }

        // Ctrl+N: 下へ移動
        if (keyData == (Keys.Control | Keys.N)) {
            MoveSelection(1);
            return true;
        }

        // Ctrl+P: 上へ移動
        if (keyData == (Keys.Control | Keys.P)) {
            MoveSelection(-1);
            return true;
        }

        // Ctrl+Enter: 選択中の項目を実行
        if (keyData == (Keys.Control | Keys.Enter)) {
            RunSelected();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MoveSelection(int delta) {
        if (listPanel.Controls.Count == 0) return;

        selectedIndex += delta;
        if (selectedIndex < 0) selectedIndex = listPanel.Controls.Count - 1;
        if (selectedIndex >= listPanel.Controls.Count) selectedIndex = 0;

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals() {
        for (int i = 0; i < listPanel.Controls.Count; i++) {
            listPanel.Controls[i].BackColor = (i == selectedIndex) ? ColorSelected : ColorDefault;
            // 備考（TextBox）の背景色も合わせる
            foreach (Control child in listPanel.Controls[i].Controls) {
                if (child is TextBox && child.Name == "txtRem") {
                    child.BackColor = (i == selectedIndex) ? ColorSelected : ColorDefault;
                }
            }
        }
    }

    private void RunSelected() {
        if (selectedIndex >= 0 && selectedIndex < currentFilteredItems.Count) {
            // 視覚的なフィードバックのためにボタンをクリックしたことにする、
            // もしくは直接実行してボタンの色を変える
            var rowPanel = listPanel.Controls[selectedIndex] as Panel;
            foreach (Control c in rowPanel.Controls) {
                if (c is Button) {
                    ((Button)c).PerformClick();
                    return;
                }
            }
        }
    }

    private void InitUI() {
        MenuStrip ms = new MenuStrip { BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White };
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

        Panel listFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        listPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill, BackColor = ColorDefault,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, BorderStyle = BorderStyle.None
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
        currentFilteredItems = model.GetFilteredList(searchBox.Text);
        int maxItems = Math.Max(1, listPanel.Height / 32);

        var displayItems = currentFilteredItems.Take(maxItems).ToList();
        foreach (var item in displayItems) {
            var row = LauncherItemRow.Create(
                item, 
                model.Favorites.Contains(item.Name),
                listPanel.Width,
                () => { model.ToggleFavorite(item.Name); RefreshList(); },
                () => { ExecuteCommand(item.Command); }
            );
            listPanel.Controls.Add(row);
        }

        // 検索結果が変わったら選択を一番上に戻す
        selectedIndex = 0;
        UpdateSelectionVisuals();
    }

    private void ExecuteCommand(string cmd) {
        try {
            string fn = "", ag = "", c = cmd.Trim();
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
            try { Process.Start("cmd.exe", "/c " + cmd); } catch { }
        }
        searchBox.Focus();
    }

    private void HandleEmacsKeys(object sender, KeyEventArgs e) {
        if (!e.Control) return;
        var tb = (TextBox)sender; e.SuppressKeyPress = true;
        int cp = tb.SelectionStart;
        switch (e.KeyCode) {
            case Keys.A: tb.SelectionStart = 0; break;
            case Keys.E: tb.SelectionStart = tb.Text.Length; break;
            case Keys.F: if (cp < tb.Text.Length) tb.SelectionStart = cp + 1; break;
            case Keys.B: if (cp > 0) tb.SelectionStart = cp - 1; break;
            case Keys.H: if (cp > 0) { tb.Text = tb.Text.Remove(cp - 1, 1); tb.SelectionStart = cp - 1; } break;
            case Keys.K: tb.Text = tb.Text.Substring(0, cp); tb.SelectionStart = cp; break;
            case Keys.U: tb.Text = tb.Text.Substring(cp); tb.SelectionStart = 0; break;
            default: e.SuppressKeyPress = false; break;
        }
    }

    private void InitAutoReload() {
        Timer t = new Timer { Interval = 2000 };
        t.Tick += (s, e) => { if (model.IsXmlChanged()) { model.LoadCommands(); RefreshList(); } };
        t.Start();
    }
}
