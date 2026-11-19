using System;
using System.Drawing;
using System.Windows.Forms;

namespace PurpleConnector;

// Always-on-top visual dashboard. Controls are deliberately presentation-only.
public sealed class PurpleOverlayForm : Form
{
    private readonly Label connection = new();
    private readonly Label gameInfo = new();
    private readonly Label stats = new();
    private readonly Action disconnect;
    private bool dragging;
    private Point dragOffset;

    public PurpleOverlayForm(Action onDisconnect)
    {
        disconnect = onDisconnect;
        Text = "Purple";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(376, 520);
        MinimumSize = new Size(376, 520);
        BackColor = Color.FromArgb(20, 18, 27);
        ForeColor = Color.FromArgb(238, 234, 247);
        Font = new Font("Segoe UI", 9F);
        DoubleBuffered = true;

        var titlebar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(28, 23, 37) };
        var brand = new Label { Text = "PURPLE", ForeColor = Color.FromArgb(187, 151, 255), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(16, 13) };
        var version = new Label { Text = "LIVE PANEL", ForeColor = Color.FromArgb(139, 128, 157), Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true, Location = new Point(284, 16) };
        titlebar.Controls.AddRange(new Control[] { brand, version });
        foreach (Control c in new Control[] { titlebar, brand, version })
        {
            c.MouseDown += StartDrag;
            c.MouseMove += MoveDrag;
            c.MouseUp += EndDrag;
        }

        var heading = new Label { Text = "Purple Dashboard", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(17, 62) };
        connection.Text = "●  CONNECTED";
        connection.ForeColor = Color.LightGreen;
        connection.AutoSize = true;
        connection.Location = new Point(18, 91);
        gameInfo.Text = "Waiting for game information…";
        gameInfo.ForeColor = Color.FromArgb(165, 155, 180);
        gameInfo.Location = new Point(18, 115);
        gameInfo.Size = new Size(340, 22);
        stats.Text = "FPS  —     PING  —     SCORE  —";
        stats.ForeColor = Color.FromArgb(190, 177, 211);
        stats.Location = new Point(18, 140);
        stats.Size = new Size(340, 22);

        var separator = new Panel { Location = new Point(17, 168), Size = new Size(342, 1), BackColor = Color.FromArgb(53, 44, 66) };
        var section = new Label { Text = "VISUAL CONTROLS", ForeColor = Color.FromArgb(145, 130, 166), Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true, Location = new Point(18, 181) };

        var controls = new FlowLayoutPanel { Location = new Point(18, 205), Size = new Size(340, 244), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Margin = Padding.Empty };
        controls.Controls.Add(MakeToggle("State", true));
        controls.Controls.Add(MakeSlider("Min Damage", 1, 100, 20, " HP"));
        controls.Controls.Add(MakeMode());
        controls.Controls.Add(MakeToggle("Multi-point", true));
        controls.Controls.Add(MakeSlider("Budget", 4, 128, 24, ""));
        controls.Controls.Add(MakeSlider("Every", 1, 10, 2, " fr."));
        controls.Controls.Add(MakeToggle("Status", true));
        controls.Controls.Add(MakeToggle("Damage", true));

        var footer = new Label { Text = "DISPLAY ONLY  •  NO GAME ACTIONS", ForeColor = Color.FromArgb(132, 121, 151), AutoSize = true, Location = new Point(18, 487) };
        var disconnectButton = new Button { Text = "DISCONNECT", Location = new Point(236, 478), Size = new Size(123, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(104, 67, 166), ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
        disconnectButton.FlatAppearance.BorderSize = 0;
        disconnectButton.Click += (_, _) => disconnect();

        Controls.AddRange(new Control[] { titlebar, heading, connection, gameInfo, stats, separator, section, controls, footer, disconnectButton });
    }

    public void UpdateTelemetry(Telemetry t)
    {
        connection.Text = "●  CONNECTED";
        connection.ForeColor = Color.LightGreen;
        gameInfo.Text = $"{t.Game}  ·  {t.Version}  ·  {t.Scene}";
        stats.Text = $"FPS  {t.Fps:0}     PING  {t.PingMs} ms     SCORE  {t.Score}";
    }

    public void SetStale()
    {
        connection.Text = "●  WAITING FOR GAME";
        connection.ForeColor = Color.Goldenrod;
    }

    private static Control MakeToggle(string label, bool enabled)
    {
        var row = new Panel { Width = 332, Height = 26, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2) };
        var text = new Label { Text = label, ForeColor = Color.FromArgb(222, 216, 232), AutoSize = true, Location = new Point(0, 5) };
        var toggle = new CheckBox { Appearance = Appearance.Button, Text = enabled ? "ON" : "OFF", Checked = enabled, TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = enabled ? Color.FromArgb(105, 67, 173) : Color.FromArgb(55, 50, 65), ForeColor = Color.White, Size = new Size(49, 22), Location = new Point(281, 1), Cursor = Cursors.Hand };
        toggle.FlatAppearance.BorderSize = 0;
        toggle.CheckedChanged += (_, _) => { toggle.Text = toggle.Checked ? "ON" : "OFF"; toggle.BackColor = toggle.Checked ? Color.FromArgb(105, 67, 173) : Color.FromArgb(55, 50, 65); };
        row.Controls.AddRange(new Control[] { text, toggle });
        return row;
    }

    private static Control MakeSlider(string label, int min, int max, int value, string suffix)
    {
        var row = new Panel { Width = 332, Height = 29, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2) };
        var text = new Label { Text = label, ForeColor = Color.FromArgb(222, 216, 232), AutoSize = true, Location = new Point(0, 7) };
        var slider = new TrackBar { Minimum = min, Maximum = max, Value = value, TickStyle = TickStyle.None, Width = 158, Height = 28, Location = new Point(132, 0), Cursor = Cursors.Hand };
        var val = new Label { Text = value + suffix, ForeColor = Color.FromArgb(176, 163, 198), AutoSize = true, Location = new Point(293, 7) };
        slider.ValueChanged += (_, _) => val.Text = slider.Value + suffix;
        row.Controls.AddRange(new Control[] { text, slider, val });
        return row;
    }

    private static Control MakeMode()
    {
        var row = new Panel { Width = 332, Height = 29, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 2) };
        var text = new Label { Text = "Mode", ForeColor = Color.FromArgb(222, 216, 232), AutoSize = true, Location = new Point(0, 7) };
        var mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 184, Location = new Point(145, 2), BackColor = Color.FromArgb(33, 27, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        mode.Items.AddRange(new object[] { "Any", "Head Priority", "Head Only" });
        mode.SelectedIndex = 1;
        row.Controls.AddRange(new Control[] { text, mode });
        return row;
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        dragging = true;
        dragOffset = new Point(e.X, e.Y);
    }
    private void MoveDrag(object? sender, MouseEventArgs e)
    {
        if (!dragging) return;
        var screen = Cursor.Position;
        Location = new Point(screen.X - dragOffset.X, screen.Y - dragOffset.Y);
    }
    private void EndDrag(object? sender, MouseEventArgs e) => dragging = false;
}
