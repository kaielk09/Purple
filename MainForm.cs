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
    private bool overlayShown;

    public MainForm()
    {
        Text = "Purple";
        Size = new Size(500, 470);
        MinimumSize = new Size(500, 470);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(12, 10, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var title = new Label { Text = "PURPLE", ForeColor = Color.FromArgb(174, 132, 255), Font = new Font("Segoe UI", 27, FontStyle.Bold), AutoSize = true, Location = new Point(28, 24) };
        var subtitle = new Label { Text = "GAME TELEMETRY  /  DESKTOP CLIENT", ForeColor = Color.FromArgb(139, 130, 153), AutoSize = true, Location = new Point(33, 73) };
        var line = new Panel { BackColor = Color.FromArgb(65, 46, 91), Location = new Point(30, 105), Size = new Size(420, 1) };
        var label = new Label { Text = "TELEMETRY KEY", ForeColor = Color.Gainsboro, AutoSize = true, Location = new Point(32, 132) };
        gameKey.SetBounds(32, 160, 418, 38);
        gameKey.BackColor = Color.FromArgb(30, 25, 40);
        gameKey.ForeColor = Color.White;
        gameKey.BorderStyle = BorderStyle.FixedSingle;
        gameKey.Text = "MyGame";
        gameKey.Font = new Font("Segoe UI", 11);

        status.Text = "●  DISCONNECTED";
        status.ForeColor = Color.FromArgb(235, 111, 125);
        status.AutoSize = true;
        status.Location = new Point(32, 222);
        status.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        connectButton.Text = "CONNECT";
        connectButton.SetBounds(32, 258, 418, 46);
        connectButton.FlatStyle = FlatStyle.Flat;
        connectButton.BackColor = Color.FromArgb(119, 75, 205);
        connectButton.ForeColor = Color.White;
        connectButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        connectButton.FlatAppearance.BorderSize = 0;
        connectButton.Click += (_, _) => ToggleConnection();

        details.Text = "Enter the key used by your own game's telemetry publisher. Purple displays the floating panel after valid data arrives.";
        details.ForeColor = Color.FromArgb(155, 148, 169);
        details.Location = new Point(33, 326);
        details.Size = new Size(415, 75);

        var foot = new Label { Text = "PURPLE  •  VISUAL TELEMETRY CLIENT", ForeColor = Color.FromArgb(95, 87, 108), AutoSize = true, Location = new Point(32, 420) };
        Controls.AddRange(new Control[] { title, subtitle, line, label, gameKey, status, connectButton, details, foot });

        poll.Interval = 500;
        poll.Tick += (_, _) => ReadSnapshot();
        FormClosing += (_, _) => Disconnect(false);
    }

    private void ToggleConnection()
    {
        if (connected) { Disconnect(true); return; }
        var key = SanitizeKey(gameKey.Text);
        if (key.Length == 0)
        {
            details.Text = "Enter a key containing letters, numbers, '_' or '-'.";
            return;
        }

        try
        {
            map = MemoryMappedFile.OpenExisting($"PurpleTelemetry_{key}", MemoryMappedFileRights.Read);
            connected = true;
            connectButton.Text = "DISCONNECT";
            connectButton.BackColor = Color.FromArgb(72, 52, 91);
            status.Text = "●  CHANNEL OPEN — WAITING FOR DATA";
            status.ForeColor = Color.MediumPurple;
            details.Text = "Channel found. Waiting for a valid, fresh telemetry snapshot…";
            gameKey.Enabled = false;
            poll.Start();
            ReadSnapshot();
        }
        catch (FileNotFoundException)
        {
            status.Text = "●  CHANNEL NOT FOUND";
            status.ForeColor = Color.FromArgb(235, 111, 125);
            details.Text = $"No channel named PurpleTelemetry_{key}. Start the publisher inside your own game first.";
        }
        catch (Exception ex)
        {
            status.Text = "●  CONNECTION ERROR";
            status.ForeColor = Color.FromArgb(235, 111, 125);
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
            if (length <= 0) { SetWaiting(); return; }
            var telemetry = Telemetry.Parse(Encoding.UTF8.GetString(bytes, 0, length));
            if (telemetry is null || !telemetry.IsFresh()) { SetWaiting(); return; }

            if (!overlayShown)
            {
                overlay = new PurpleOverlayForm(() => Disconnect(true));
                overlay.Show();
                overlayShown = true;
                Hide();
            }
            overlay?.UpdateTelemetry(telemetry);
        }
        catch (Exception ex)
        {
            details.Text = "Telemetry read failed: " + ex.Message;
            status.Text = "●  READ ERROR";
            status.ForeColor = Color.FromArgb(235, 111, 125);
        }
    }

    private void SetWaiting()
    {
        if (!overlayShown)
        {
            status.Text = "●  CONNECTED — WAITING FOR FRESH DATA";
            status.ForeColor = Color.MediumPurple;
        }
        else
        {
            overlay?.SetStale();
        }
    }

    private void Disconnect(bool restoreWindow)
    {
        poll.Stop();
        connected = false;
        map?.Dispose();
        map = null;
        if (overlay is not null)
        {
            overlay.Close();
            overlay.Dispose();
            overlay = null;
        }
        overlayShown = false;
        connectButton.Text = "CONNECT";
        connectButton.BackColor = Color.FromArgb(119, 75, 205);
        status.Text = "●  DISCONNECTED";
        status.ForeColor = Color.FromArgb(235, 111, 125);
        details.Text = "Connection closed.";
        gameKey.Enabled = true;
        if (restoreWindow && !IsDisposed) Show();
    }

    private static string SanitizeKey(string text) =>
        new string((text ?? "").Trim().Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').Take(64).ToArray());
}
