using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

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

        InitUI();
        InitAutoReload();
        this.Resize += (s, e) => RefreshList();
    }

    private void InitUI() {
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
        switch (e.KeyCode) {
            case Keys.A: tb.SelectionStart = 0; break;
            case Keys.E: tb.SelectionStart = tb.Text.Length; break;
            case Keys.H: if (tb.SelectionStart > 0) { int p = tb.SelectionStart; tb.Text = tb.Text.Remove(p - 1, 1); tb.SelectionStart = p - 1; } break;
            case Keys.K: tb.Text = tb.Text.Substring(0, tb.SelectionStart); break;
            default: e.SuppressKeyPress = false; break;
        }
    }

    private void InitAutoReload() {
        Timer t = new Timer { Interval = 2000 };
        t.Tick += (s, e) => { if (model.IsXmlChanged()) { model.LoadCommands(); RefreshList(); } };
        t.Start();
    }
}
