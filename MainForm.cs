using System;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurpleConnector;

public sealed class MainForm : Form
{
    private readonly TextBox gameKey = new();
    private readonly Label status = new();
    private readonly Label details = new();
    private readonly Button connectButton = new();
    private readonly System.Windows.Forms.Timer poll = new();
    private MemoryMappedFile? map;
    private PurpleOverlayForm? overlay;
    private bool connected;
    private bool hasShownOverlay;

    public MainForm()
    {
        Text = "Purple";
        Size = new Size(470, 450);
        MinimumSize = new Size(470, 450);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(12, 10, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        var title = new Label { Text = "PURPLE", ForeColor = Color.FromArgb(160, 100, 255), Font = new Font("Segoe UI", 24, FontStyle.Bold), AutoSize = true, Location = new Point(26, 22) };
        var subtitle = new Label { Text = "GAME TELEMETRY CONNECTOR", ForeColor = Color.Gray, AutoSize = true, Location = new Point(30, 66) };
        var label = new Label { Text = "GAME KEY", ForeColor = Color.Gainsboro, AutoSize = true, Location = new Point(30, 112) };
        gameKey.SetBounds(30, 138, 390, 34);
        gameKey.BackColor = Color.FromArgb(32, 25, 43);
        gameKey.ForeColor = Color.White;
        gameKey.BorderStyle = BorderStyle.FixedSingle;
        gameKey.Text = "MyGame";

        status.Text = "● DISCONNECTED";
        status.ForeColor = Color.IndianRed;
        status.AutoSize = true;
        status.Location = new Point(30, 190);
        status.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        connectButton.Text = "CONNECT";
        connectButton.SetBounds(30, 224, 390, 42);
        connectButton.FlatStyle = FlatStyle.Flat;
        connectButton.BackColor = Color.FromArgb(132, 76, 220);
        connectButton.ForeColor = Color.White;
        connectButton.FlatAppearance.BorderSize = 0;
        connectButton.Click += (_, _) => ToggleConnection();

        details.Text = "Enter the telemetry key published by your own game. The Purple panel appears after valid telemetry arrives.";
        details.ForeColor = Color.FromArgb(155, 148, 169);
        details.Location = new Point(30, 286);
        details.Size = new Size(390, 130);

        Controls.AddRange(new Control[] { title, subtitle, label, gameKey, status, connectButton, details });
        poll.Interval = 800;
        poll.Tick += (_, _) => ReadSnapshot();
        FormClosing += (_, _) => Disconnect(false);
    }

    private void ToggleConnection()
    {
        if (connected) { Disconnect(true); return; }
        var key = SanitizeKey(gameKey.Text);
        if (key.Length == 0)
        {
            details.Text = "Please enter a valid telemetry key.";
            return;
        }

        try
        {
            map = MemoryMappedFile.OpenExisting($"PurpleTelemetry_{key}", MemoryMappedFileRights.Read);
            connected = true;
            hasShownOverlay = false;
            connectButton.Text = "DISCONNECT";
            status.Text = "● CONNECTED — WAITING FOR DATA";
            status.ForeColor = Color.MediumPurple;
            details.Text = "Channel opened. The floating Purple panel will appear when a valid telemetry snapshot is received.";
            poll.Start();
            ReadSnapshot();
        }
        catch (FileNotFoundException)
        {
            status.Text = "● CHANNEL NOT FOUND";
            status.ForeColor = Color.IndianRed;
            details.Text = $"No channel named PurpleTelemetry_{key} exists. Start your game's telemetry publisher first.";
        }
        catch (Exception ex)
        {
            status.Text = "● CONNECTION ERROR";
            status.ForeColor = Color.IndianRed;
            details.Text = ex.Message;
            map?.Dispose();
            map = null;
        }
    }

    private void ReadSnapshot()
    {
        if (!connected || map is null) return;
        try
        {
            using var view = map.CreateViewAccessor(0, 4096, MemoryMappedFileAccess.Read);
            byte[] bytes = new byte[4096];
            view.ReadArray(0, bytes, 0, bytes.Length);
            int length = Array.IndexOf(bytes, (byte)0);
            if (length < 0) length = bytes.Length;
            string json = Encoding.UTF8.GetString(bytes, 0, length).Trim();
            var telemetry = Telemetry.Parse(json);
            if (telemetry is null || string.IsNullOrWhiteSpace(telemetry.Game))
            {
                status.Text = "● CONNECTED — WAITING FOR DATA";
                status.ForeColor = Color.MediumPurple;
                return;
            }

            if (!hasShownOverlay)
            {
                overlay = new PurpleOverlayForm(() => Disconnect(true));
                overlay.Show();
                hasShownOverlay = true;
                Hide();
            }
            overlay?.UpdateTelemetry(telemetry);
        }
        catch (Exception ex)
        {
            status.Text = "● READ ERROR";
            status.ForeColor = Color.IndianRed;
            details.Text = ex.Message;
        }
    }

    private void Disconnect(bool restoreWindow)
    {
        poll.Stop();
        connected = false;
        map?.Dispose();
        map = null;
        overlay?.Close();
        overlay = null;
        hasShownOverlay = false;
        connectButton.Text = "CONNECT";
        status.Text = "● DISCONNECTED";
        status.ForeColor = Color.IndianRed;
        details.Text = "Connection closed.";
        if (restoreWindow && !IsDisposed) Show();
    }

    private static string SanitizeKey(string text)
    {
        var chars = text.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray();
        return new string(chars);
    }
}
