using System;
using System.Drawing;
using System.Windows.Forms;

public class LauncherItemRow {
    public static Panel Create(CommandItem item, bool isFav, int width, Action onFavClick, Action onRunClick) {
        // 枠線をなくし、高さを 32px まで圧縮
        Panel p = new Panel { Size = new Size(width - 5, 32), Margin = new Padding(0) };

        // お気に入り星 (位置調整)
        Label lblFav = new Label {
            Text = isFav ? "★" : "☆",
            ForeColor = isFav ? Color.Gold : Color.DimGray,
            Location = new Point(5, 6), Size = new Size(25, 20),
            Font = new Font("MS UI Gothic", 12), Cursor = Cursors.Hand
        };
        lblFav.Click += (s, e) => onFavClick();

        // RUNボタン (さらに高さを抑える)
        Button btnRun = new Button {
            Text = "RUN", Size = new Size(50, 24), Location = new Point(p.Width - 55, 4),
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

        // 名前 (280px)
        Label lblName = new Label {
            Text = item.Name, ForeColor = Color.FromArgb(144, 202, 249),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(35, 7), Size = new Size(280, 18),
            AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
        };

        // 備考 (右寄せ)
        TextBox txtRem = new TextBox {
            Name = "txtRem", // ← ここを追加
            Text = item.Remarks, ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5f), TabStop = false,
            Location = new Point(btnRun.Left - 135, 8), Size = new Size(130, 18),
            TextAlign = HorizontalAlignment.Right, Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        // コマンド (背景色のみ残し、枠線はなし)
        Panel pnlCmd = new Panel {
            BackColor = Color.FromArgb(38, 50, 56), 
            Location = new Point(320, 4),
            Size = new Size(txtRem.Left - 325, 24), 
            Padding = new Padding(6, 3, 6, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        TextBox txtCmd = new TextBox {
            Text = item.Command, ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(38, 50, 56), ForeColor = Color.FromArgb(176, 190, 197),
            Font = new Font("Consolas", 10f), TabStop = false, Dock = DockStyle.Fill,
            Multiline = false
        };
        pnlCmd.Controls.Add(txtCmd);

        p.Controls.Add(lblFav); p.Controls.Add(lblName); p.Controls.Add(txtRem); p.Controls.Add(pnlCmd); p.Controls.Add(btnRun);
        return p;
    }
}
