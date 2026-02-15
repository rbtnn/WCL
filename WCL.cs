using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml.Linq;

public class CommandItem {
    public string Name { get; set; }
    public string Command { get; set; }
    public string Remarks { get; set; }
}

public class LauncherForm : Form {
    private TextBox searchBox;
    private FlowLayoutPanel listPanel;
    private List<CommandItem> allItems = new List<CommandItem>();
    private HashSet<string> favorites = new HashSet<string>();
    private string favPath = "favorites.txt";
    private string xmlPath = "commands.xml";
    private DateTime lastXmlWriteTime; // XMLの最終更新日時を保持

    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new LauncherForm());
    }

    public LauncherForm() {
        this.Text = "Windows Command Launcher";
        this.Size = new Size(650, 800);
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.DoubleBuffered = true; 
        this.StartPosition = FormStartPosition.CenterScreen;

        // 初回ロード
        lastXmlWriteTime = File.Exists(xmlPath) ? File.GetLastWriteTime(xmlPath) : DateTime.MinValue;
        LoadFavorites();
        LoadCommands();
        InitUI();
        InitAutoReload(); // 自動更新タイマーの開始

        this.Resize += (s, e) => RefreshList();
    }

    private void InitUI() {
        searchBox = new TextBox {
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(15)
        };
        searchBox.KeyDown += HandleEmacsKeys;
        searchBox.TextChanged += (s, e) => RefreshList();

        listPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false 
        };

        this.Controls.Add(listPanel);
        this.Controls.Add(searchBox);
        RefreshList();
    }

    // 自動更新の監視タイマー
    private void InitAutoReload() {
        Timer reloadTimer = new Timer();
        reloadTimer.Interval = 2000; // 2秒ごとにチェック
        reloadTimer.Tick += (s, e) => {
            if (File.Exists(xmlPath)) {
                DateTime currentWriteTime = File.GetLastWriteTime(xmlPath);
                if (currentWriteTime != lastXmlWriteTime) {
                    lastXmlWriteTime = currentWriteTime;
                    LoadCommands();
                    RefreshList();
                }
            }
        };
        reloadTimer.Start();
    }

    private void HandleEmacsKeys(object sender, KeyEventArgs e) {
        if (!e.Control) return;
        var tb = (TextBox)sender;
        e.SuppressKeyPress = true;

        switch (e.KeyCode) {
            case Keys.A: tb.SelectionStart = 0; break;
            case Keys.E: tb.SelectionStart = tb.Text.Length; break;
            case Keys.F: if (tb.SelectionStart < tb.Text.Length) tb.SelectionStart++; break;
            case Keys.B: if (tb.SelectionStart > 0) tb.SelectionStart--; break;
            case Keys.H: 
                if (tb.SelectionStart > 0) {
                    int pos = tb.SelectionStart;
                    tb.Text = tb.Text.Remove(pos - 1, 1);
                    tb.SelectionStart = pos - 1;
                }
                break;
            case Keys.K: tb.Text = tb.Text.Substring(0, tb.SelectionStart); break;
            case Keys.U: 
                string remaining = tb.Text.Substring(tb.SelectionStart);
                tb.Text = remaining;
                tb.SelectionStart = 0;
                break;
            default: e.SuppressKeyPress = false; break;
        }
    }

    private void RefreshList() {
        listPanel.Controls.Clear();
        string q = searchBox.Text.ToLower();

        var filtered = allItems
            .Where(i => i.Name.ToLower().Contains(q) || 
                       (i.Remarks != null && i.Remarks.ToLower().Contains(q)) ||
                       (i.Command != null && i.Command.ToLower().Contains(q)))
            .OrderByDescending(i => favorites.Contains(i.Name))
            .ToList();

        int itemHeight = 80;
        int maxItems = Math.Max(1, listPanel.Height / itemHeight);
        
        foreach (var item in filtered.Take(maxItems)) {
            listPanel.Controls.Add(CreateItemRow(item));
        }
    }

    private Panel CreateItemRow(CommandItem item) {
        Panel p = new Panel { Size = new Size(listPanel.Width - 30, 75), Margin = new Padding(5, 5, 5, 0) };
        bool isFav = favorites.Contains(item.Name);

        Label lblFav = new Label {
            Text = isFav ? "★" : "☆",
            ForeColor = isFav ? Color.Gold : Color.DimGray,
            Location = new Point(5, 20),
            Size = new Size(30, 30),
            Font = new Font("MS UI Gothic", 16),
            Cursor = Cursors.Hand
        };
        lblFav.Click += (s, e) => {
            if (favorites.Contains(item.Name)) favorites.Remove(item.Name);
            else favorites.Add(item.Name);
            SaveFavorites();
            RefreshList();
        };

        Button btnRun = new Button {
            Text = "RUN",
            Size = new Size(60, 48),
            Location = new Point(p.Width - 65, 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(26, 115, 232),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        Label lblName = new Label {
            Text = item.Name,
            ForeColor = Color.FromArgb(144, 202, 249),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(40, 5),
            Size = new Size(btnRun.Left - 250, 20),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        
        Label lblRemarks = new Label {
            Text = !string.IsNullOrEmpty(item.Remarks) ? string.Format("({0})", item.Remarks) : "",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(btnRun.Left - 205, 7), 
            Size = new Size(200, 20),
            TextAlign = ContentAlignment.TopRight,
            AutoEllipsis = true
        };

        Label lblCmd = new Label {
            Text = item.Command,
            ForeColor = Color.FromArgb(176, 190, 197),
            Font = new Font("Consolas", 8),
            Location = new Point(40, 32),
            Size = new Size(p.Width - 120, 22),
            BackColor = Color.FromArgb(38, 50, 56),
            Padding = new Padding(5, 3, 5, 3),
            AutoEllipsis = true
        };

        btnRun.FlatAppearance.BorderSize = 0;
        btnRun.Click += (s, e) => {
            btnRun.BackColor = Color.FromArgb(67, 160, 71);
            btnRun.Text = "OK";
            try {
                string fileName = ""; string arguments = ""; string cmdStr = item.Command.Trim();
                if (cmdStr.StartsWith("\"")) {
                    int next = cmdStr.IndexOf("\"", 1);
                    if (next > 0) {
                        fileName = cmdStr.Substring(1, next - 1);
                        arguments = cmdStr.Substring(next + 1).Trim();
                    } else { fileName = cmdStr; }
                } else {
                    int space = cmdStr.IndexOf(" ");
                    if (space > 0) {
                        fileName = cmdStr.Substring(0, space);
                        arguments = cmdStr.Substring(space + 1).Trim();
                    } else { fileName = cmdStr; }
                }
                ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
                psi.UseShellExecute = true;
                Process.Start(psi);
            } catch {
                try {
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c " + item.Command) {
                        CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                    });
                } catch { }
            }
            Timer timer = new Timer { Interval = 800 };
            timer.Tick += (ts, te) => {
                btnRun.BackColor = Color.FromArgb(26, 115, 232);
                btnRun.Text = "RUN";
                timer.Stop();
            };
            timer.Start();
            searchBox.Focus();
        };

        p.Controls.Add(lblFav);
        p.Controls.Add(lblName);
        p.Controls.Add(lblRemarks);
        p.Controls.Add(lblCmd);
        p.Controls.Add(btnRun);
        return p;
    }

    private void LoadCommands() {
        if (!File.Exists(xmlPath)) return;
        try {
            XDocument doc = XDocument.Load(xmlPath);
            allItems = doc.Descendants("Item").Select(x => new CommandItem {
                Name = (string)x.Element("Name") ?? "No Name",
                Command = (string)x.Element("Command") ?? "",
                Remarks = (string)x.Element("Remarks") ?? ""
            }).ToList();
        } catch (Exception ex) {
            MessageBox.Show("XML Load Error: " + ex.Message);
        }
    }

    private void LoadFavorites() {
        if (File.Exists(favPath)) favorites = new HashSet<string>(File.ReadAllLines(favPath));
    }

    private void SaveFavorites() {
        File.WriteAllLines(favPath, favorites.ToArray());
    }
}
