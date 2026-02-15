using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

public class LauncherItemRow {
    public static Panel Create(CommandItem item, bool isFav, int width, Action onFavClick, Action onRunClick) {
        Panel p = new Panel { Size = new Size(width - 25, 55), Margin = new Padding(0, 0, 0, 5) };

        // お気に入り星
        Label lblFav = new Label {
            Text = isFav ? "★" : "☆",
            ForeColor = isFav ? Color.Gold : Color.DimGray,
            Location = new Point(5, 18), Size = new Size(25, 25),
            Font = new Font("MS UI Gothic", 14), Cursor = Cursors.Hand
        };
        lblFav.Click += (s, e) => onFavClick();

        // RUNボタン
        Button btnRun = new Button {
            Text = "RUN", Size = new Size(50, 32), Location = new Point(p.Width - 55, 14),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(26, 115, 232),
            ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold)
        };
        btnRun.FlatAppearance.BorderSize = 0;
        btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnRun.Click += (s, e) => {
            btnRun.BackColor = Color.FromArgb(67, 160, 71); btnRun.Text = "OK";
            onRunClick();
            Timer t = new Timer { Interval = 800 };
            t.Tick += (ts, te) => { btnRun.BackColor = Color.FromArgb(26, 115, 232); btnRun.Text = "RUN"; t.Stop(); };
            t.Start();
        };

        // 名前
        Label lblName = new Label {
            Text = item.Name, ForeColor = Color.FromArgb(144, 202, 249),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(40, 5), Size = new Size(btnRun.Left - 220, 18),
            AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
        };

        // 備考 (選択可能TextBox)
        TextBox txtRem = new TextBox {
            Text = item.Remarks, ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f), TabStop = false,
            Location = new Point(btnRun.Left - 180, 6), Size = new Size(170, 18),
            TextAlign = HorizontalAlignment.Right, Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        // コマンド (パディング付きコンテナ)
        Panel pnlCmd = new Panel {
            BackColor = Color.FromArgb(38, 50, 56), Location = new Point(40, 26),
            Size = new Size(p.Width - 110, 28), Padding = new Padding(6, 4, 6, 4)
        };
        TextBox txtCmd = new TextBox {
            Text = item.Command, ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(38, 50, 56), ForeColor = Color.FromArgb(176, 190, 197),
            Font = new Font("Consolas", 11f), TabStop = false, Dock = DockStyle.Fill
        };
        pnlCmd.Controls.Add(txtCmd);

        p.Controls.Add(lblFav); p.Controls.Add(lblName); p.Controls.Add(txtRem); p.Controls.Add(pnlCmd); p.Controls.Add(btnRun);
        return p;
    }
}
