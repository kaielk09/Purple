using System;
using System.Drawing;
using System.Windows.Forms;

namespace PurpleConnector;

// Floating visual-only controls. They do not read game entities or trigger game actions.
public sealed class PurpleOverlayForm : Form
{
    private readonly Label connection = new();
    private readonly Label gameInfo = new();
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
        Size = new Size(360, 420);
        BackColor = Color.FromArgb(21, 19, 27);
        ForeColor = Color.FromArgb(238, 234, 247);
        Font = new Font("Segoe UI", 9F);

        var titlebar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(25, 21, 32) };
        var brand = new Label { Text = "PURPLE", ForeColor = Color.FromArgb(179, 146, 255), Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Location = new Point(14, 11) };
        var version = new Label { Text = "CONNECTED", ForeColor = Color.FromArgb(151, 139, 168), Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(258, 15) };
        titlebar.Controls.AddRange(new Control[] { brand, version });
        titlebar.MouseDown += StartDrag;
        titlebar.MouseMove += MoveDrag;
        titlebar.MouseUp += EndDrag;
        brand.MouseDown += StartDrag;
        brand.MouseMove += MoveDrag;
        brand.MouseUp += EndDrag;

        var heading = new Label { Text = "Purple AutoWall", Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoSize = true, Location = new Point(16, 57) };
        connection.Text = "● CONNECTED";
        connection.ForeColor = Color.LightGreen;
        connection.AutoSize = true;
        connection.Location = new Point(16, 83);
        gameInfo.Text = "Waiting for game information…";
        gameInfo.ForeColor = Color.FromArgb(155, 148, 169);
        gameInfo.Location = new Point(16, 105);
        gameInfo.Size = new Size(328, 38);

        var controls = new FlowLayoutPanel { Location = new Point(16, 148), Size = new Size(328, 214), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        controls.Controls.Add(MakeToggle("State", true));
        controls.Controls.Add(MakeSlider("Min Damage", 1, 100, 20, " HP"));
        controls.Controls.Add(MakeMode());
        controls.Controls.Add(MakeToggle("Multi-point", true));
        controls.Controls.Add(MakeSlider("Budget", 4, 128, 24, ""));
        controls.Controls.Add(MakeSlider("Every", 1, 10, 2, " fr."));
        controls.Controls.Add(MakeToggle("Status", true));
        controls.Controls.Add(MakeToggle("Damage", true));

        var footer = new Label { Text = "VISUAL CONTROLS ONLY", ForeColor = Color.FromArgb(139, 128, 157), AutoSize = true, Location = new Point(16, 376) };
        var disconnectButton = new Button { Text = "DISCONNECT", Location = new Point(213, 369), Size = new Size(131, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(105, 63, 170), ForeColor = Color.White };
        disconnectButton.FlatAppearance.BorderSize = 0;
        disconnectButton.Click += (_, _) => disconnect();

        Controls.AddRange(new Control[] { titlebar, heading, connection, gameInfo, controls, footer, disconnectButton });
    }

    public void UpdateTelemetry(Telemetry t)
    {
        gameInfo.Text = $"{t.Game}  ·  {t.Version}  ·  {t.Scene}";
    }

    private static Control MakeToggle(string label, bool enabled)
    {
        var row = new Panel { Width = 318, Height = 27, BackColor = Color.Transparent };
        var text = new Label { Text = label, ForeColor = Color.FromArgb(216, 211, 226), AutoSize = true, Location = new Point(0, 5) };
        var toggle = new CheckBox { Appearance = Appearance.Button, Text = enabled ? "ON" : "OFF", Checked = enabled, TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = enabled ? Color.FromArgb(105, 67, 173) : Color.FromArgb(55, 50, 65), ForeColor = Color.White, Size = new Size(48, 23), Location = new Point(267, 1) };
        toggle.CheckedChanged += (_, _) => { toggle.Text = toggle.Checked ? "ON" : "OFF"; toggle.BackColor = toggle.Checked ? Color.FromArgb(105, 67, 173) : Color.FromArgb(55, 50, 65); };
        row.Controls.AddRange(new Control[] { text, toggle });
        return row;
    }

    private static Control MakeSlider(string label, int min, int max, int value, string suffix)
    {
        var row = new Panel { Width = 318, Height = 29, BackColor = Color.Transparent };
        var text = new Label { Text = label, ForeColor = Color.FromArgb(216, 211, 226), AutoSize = true, Location = new Point(0, 7) };
        var slider = new TrackBar { Minimum = min, Maximum = max, Value = value, TickStyle = TickStyle.None, Width = 145, Height = 28, Location = new Point(145, 0) };
        var val = new Label { Text = value + suffix, ForeColor = Color.FromArgb(155, 148, 169), AutoSize = true, Location = new Point(290, 7) };
        slider.ValueChanged += (_, _) => val.Text = slider.Value + suffix;
        row.Controls.AddRange(new Control[] { text, slider, val });
        return row;
    }

    private static Control MakeMode()
    {
        var row = new Panel { Width = 318, Height = 29, BackColor = Color.Transparent };
        var text = new Label { Text = "Mode", ForeColor = Color.FromArgb(216, 211, 226), AutoSize = true, Location = new Point(0, 7) };
        var mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 172, Location = new Point(145, 2), BackColor = Color.FromArgb(33, 27, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        mode.Items.AddRange(new object[] { "Any", "Head Priority", "Head Only" });
        mode.SelectedIndex = 1;
        row.Controls.AddRange(new Control[] { text, mode });
        return row;
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        dragging = true;
        dragOffset = e.Location;
    }
    private void MoveDrag(object? sender, MouseEventArgs e)
    {
        if (!dragging) return;
        var screen = PointToScreen(e.Location);
        Location = new Point(screen.X - dragOffset.X, screen.Y - dragOffset.Y);
    }
    private void EndDrag(object? sender, MouseEventArgs e) => dragging = false;
}
