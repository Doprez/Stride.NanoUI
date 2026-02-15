using NanoUI;
using NanoUI.Common;
using NanoUI.Components;
using NanoUI.Layouts;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// A retro-terminal aesthetic with amber/green phosphor text on a dark CRT-style
/// background. Mimics classic mainframe terminals and system consoles.
/// </summary>
public class RetroTerminalDemo : DemoBase
{
    // Theme palette — classic phosphor CRT colors
    static readonly Color BgCrt       = new(8, 8, 6, 235);
    static readonly Color BgPanel     = new(14, 16, 10, 220);
    static readonly Color PhosGreen   = new(0, 255, 65, 255);
    static readonly Color PhosAmber   = new(255, 176, 0, 255);
    static readonly Color DimGreen    = new(0, 140, 35, 180);
    static readonly Color DimAmber    = new(160, 110, 0, 180);
    static readonly Color BrightWhite = new(220, 255, 220, 255);
    static readonly Color ErrorRed    = new(255, 60, 60, 255);
    static readonly Color HeaderGreen = new(0, 200, 50, 255);
    static readonly Color InputBg     = new(5, 20, 5, 200);

    private UILabel? _outputLabel;
    private UIProgressbar? _cpuBar;
    private UIProgressbar? _memBar;
    private UIProgressbar? _diskBar;
    private float _elapsed;
    private int _lineCount;

    public RetroTerminalDemo(UIScreen screen) : base(screen)
    {
        BuildUI();
    }

    void BuildUI()
    {
        if (Screen == null) return;

        // Style the screen itself as the CRT panel
        Screen.BackgroundFocused = Screen.BackgroundUnfocused =
            new SolidBrush(BgCrt);
        Screen.ChildrenLayout = new GroupLayout { Spacing = new Vector2(5) };
        Screen.TextColor = PhosGreen;
        Screen.FontSize = 13;

        // ── HEADER ──
        var banner = new UILabel(Screen,
            "========================================\n" +
            "  RETRO-SYS MAINFRAME  |  NODE 04\n" +
            "  Uptime: 847d 12h 33m | Users: 3\n" +
            "========================================");
        banner.TextColor = PhosAmber;
        banner.FontSize = 12;

        // ── SYSTEM RESOURCES ──
        new UILabel(Screen, "--- SYSTEM RESOURCES ---") { TextColor = HeaderGreen, FontSize = 14 };

        new UILabel(Screen, "CPU LOAD") { TextColor = DimGreen, FontSize = 11 };
        _cpuBar = new UIProgressbar(Screen);
        _cpuBar.Value = 0.37f;
        _cpuBar.BackgroundFocused = _cpuBar.BackgroundUnfocused =
            new LinearGradient(DimGreen, PhosGreen, new CornerRadius(2), true);

        new UILabel(Screen, "MEMORY") { TextColor = DimAmber, FontSize = 11 };
        _memBar = new UIProgressbar(Screen);
        _memBar.Value = 0.62f;
        _memBar.BackgroundFocused = _memBar.BackgroundUnfocused =
            new LinearGradient(DimAmber, PhosAmber, new CornerRadius(2), true);

        new UILabel(Screen, "DISK I/O") { TextColor = DimGreen, FontSize = 11 };
        _diskBar = new UIProgressbar(Screen);
        _diskBar.Value = 0.21f;
        _diskBar.BackgroundFocused = _diskBar.BackgroundUnfocused =
            new LinearGradient(DimGreen, PhosGreen, new CornerRadius(2), true);

        // ── COMMAND BUTTONS ──
        new UILabel(Screen, "--- COMMANDS ---") { TextColor = HeaderGreen, FontSize = 14 };

        var psBtn = new UIButton(Screen, "ps -aux");
        psBtn.BackgroundFocused = psBtn.BackgroundUnfocused =
            new SolidBrush(InputBg);
        psBtn.TextColor = PhosGreen;
        psBtn.Clicked += () => AppendOutput("PID  USER   %CPU  COMMAND\n 01  root    2.1  /sbin/init\n 42  admin   0.8  mainframe-core\n 99  guest   0.1  bash");

        var dfBtn = new UIButton(Screen, "df -h");
        dfBtn.BackgroundFocused = dfBtn.BackgroundUnfocused =
            new SolidBrush(InputBg);
        dfBtn.TextColor = PhosAmber;
        dfBtn.Clicked += () => AppendOutput("Filesystem   Size  Used  Avail  Use%\n/dev/sda1    120G   78G    42G   65%\n/dev/sdb1    500G  210G   290G   42%");

        var pingBtn = new UIButton(Screen, "ping nexus");
        pingBtn.BackgroundFocused = pingBtn.BackgroundUnfocused =
            new SolidBrush(InputBg);
        pingBtn.TextColor = PhosGreen;
        pingBtn.Clicked += () => AppendOutput("PING nexus.local: 64 bytes, ttl=64, time=0.42ms\nPING nexus.local: 64 bytes, ttl=64, time=0.38ms\n--- 2 packets, 0% loss ---");

        var clearBtn = new UIButton(Screen, "clear");
        clearBtn.BackgroundFocused = clearBtn.BackgroundUnfocused =
            new SolidBrush(new Color(60, 0, 0, 200));
        clearBtn.TextColor = ErrorRed;
        clearBtn.Clicked += () =>
        {
            if (_outputLabel != null)
            {
                _outputLabel.Caption = "Screen cleared.\n>";
                _lineCount = 1;
            }
        };

        // ── OPTIONS ──
        new UILabel(Screen, "--- SETTINGS ---") { TextColor = DimAmber, FontSize = 13 };
        new UICheckBox(Screen, "Echo mode", true) { TextColor = PhosGreen };
        new UICheckBox(Screen, "Verbose logging", false) { TextColor = PhosAmber };
        new UICheckBox(Screen, "Auto-scroll", true) { TextColor = PhosGreen };

        // ── PROCESS PRIORITY ──
        new UILabel(Screen, "Process Priority") { TextColor = DimGreen, FontSize = 12 };
        var prioSlider = new UISlider(Screen);
        prioSlider.Value = 0.5f;

        // ── TERMINAL OUTPUT ──
        new UILabel(Screen, "--- OUTPUT ---") { TextColor = HeaderGreen, FontSize = 14 };
        _outputLabel = new UILabel(Screen,
            "> System ready\n" +
            "> Type a command or click a button\n" +
            "> MOTD: \"The best code is no code at all.\"");
        _outputLabel.TextColor = PhosGreen;
        _outputLabel.FontSize = 11;
        _outputLabel.WrapText = true;
        _lineCount = 3;
    }

    void AppendOutput(string text)
    {
        if (_outputLabel == null) return;
        var current = _outputLabel.Caption ?? "";
        var lines = current.Split('\n');
        if (lines.Length > 15)
            current = string.Join('\n', lines[^15..]);
        _outputLabel.Caption = $"{current}\n> {text}";
        _lineCount += text.Split('\n').Length + 1;
    }

    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        _elapsed += deltaSeconds;

        // Simulate fluctuating resource usage
        if (_cpuBar != null)
            _cpuBar.Value = 0.35f + 0.15f * MathF.Sin(_elapsed * 1.8f);
        if (_memBar != null)
            _memBar.Value = 0.60f + 0.05f * MathF.Sin(_elapsed * 0.4f + 1f);
        if (_diskBar != null)
            _diskBar.Value = 0.18f + 0.12f * MathF.Abs(MathF.Sin(_elapsed * 2.5f));
    }
}
