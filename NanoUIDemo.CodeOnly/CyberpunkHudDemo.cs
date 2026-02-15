using NanoUI;
using NanoUI.Common;
using NanoUI.Components;
using NanoUI.Layouts;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// A cyberpunk / sci-fi themed HUD panel with neon accents,
/// dark translucent backgrounds, and glowing cyan/magenta highlights.
/// </summary>
public class CyberpunkHudDemo : DemoBase
{
    // Theme palette
    static readonly Color BgDark       = new(10, 12, 18, 220);
    static readonly Color BgPanel      = new(18, 22, 35, 200);
    static readonly Color NeonCyan     = new(0, 230, 255, 255);
    static readonly Color NeonMagenta  = new(255, 0, 180, 255);
    static readonly Color NeonGreen    = new(0, 255, 120, 255);
    static readonly Color DimCyan      = new(0, 120, 140, 180);
    static readonly Color TextBright   = new(210, 230, 255, 255);
    static readonly Color TextDim      = new(120, 150, 180, 200);
    static readonly Color WarningAmber = new(255, 180, 0, 255);

    private UIProgressbar? _shieldBar;
    private UIProgressbar? _powerBar;
    private UIProgressbar? _signalBar;
    private UILabel? _statusLog;
    private float _elapsed;

    public CyberpunkHudDemo(UIScreen screen) : base(screen)
    {
        BuildUI();
    }

    void BuildUI()
    {
        if (Screen == null) return;

        // Style the screen itself as the panel background
        Screen.BackgroundFocused = Screen.BackgroundUnfocused =
            new LinearGradient(BgDark, BgPanel, new CornerRadius(4), false);
        Screen.ChildrenLayout = new GroupLayout { Spacing = new Vector2(6) };
        Screen.TextColor = NeonCyan;
        Screen.FontSize = 14;

        // ── TITLE ──
        var title = new UILabel(Screen, "// NEXUS HUD v4.2");
        title.TextColor = NeonCyan;
        title.FontSize = 20;

        // ── SYSTEM STATUS ──
        var sysHeader = new UILabel(Screen, "[ SYSTEM STATUS ]");
        sysHeader.TextColor = NeonCyan;
        sysHeader.FontSize = 16;

        // Shield bar
        new UILabel(Screen, "SHIELD INTEGRITY") { TextColor = TextDim, FontSize = 12 };
        _shieldBar = new UIProgressbar(Screen);
        _shieldBar.Value = 0.82f;
        _shieldBar.BackgroundFocused = _shieldBar.BackgroundUnfocused =
            new LinearGradient(DimCyan, new Color(0, 180, 220, 200), new CornerRadius(3), true);

        // Power bar
        new UILabel(Screen, "REACTOR OUTPUT") { TextColor = TextDim, FontSize = 12 };
        _powerBar = new UIProgressbar(Screen);
        _powerBar.Value = 0.65f;
        _powerBar.BackgroundFocused = _powerBar.BackgroundUnfocused =
            new LinearGradient(new Color(60, 0, 80, 200), new Color(200, 0, 140, 200), new CornerRadius(3), true);

        // Signal strength
        new UILabel(Screen, "COMM SIGNAL") { TextColor = TextDim, FontSize = 12 };
        _signalBar = new UIProgressbar(Screen);
        _signalBar.Value = 0.94f;
        _signalBar.BackgroundFocused = _signalBar.BackgroundUnfocused =
            new LinearGradient(new Color(0, 60, 40, 200), NeonGreen, new CornerRadius(3), true);

        // ── TACTICAL CONTROLS ──
        new UILabel(Screen, "[ TACTICAL ]") { TextColor = NeonMagenta, FontSize = 16 };

        var scanBtn = new UIButton(Screen, "SCAN SECTOR");
        scanBtn.BackgroundFocused = scanBtn.BackgroundUnfocused =
            new LinearGradient(new Color(0, 80, 100, 200), DimCyan, new CornerRadius(4), false);
        scanBtn.TextColor = TextBright;
        scanBtn.Clicked += () => AppendLog(">> Scanning sector 7G...");

        var lockBtn = new UIButton(Screen, "WEAPONS LOCK");
        lockBtn.BackgroundFocused = lockBtn.BackgroundUnfocused =
            new LinearGradient(new Color(100, 0, 60, 200), NeonMagenta, new CornerRadius(4), false);
        lockBtn.TextColor = TextBright;
        lockBtn.Clicked += () => AppendLog(">> Target acquired");

        var cloakBtn = new UIButton(Screen, "TOGGLE CLOAK");
        cloakBtn.Flags = ButtonFlags.ToggleButton;
        cloakBtn.BackgroundFocused = cloakBtn.BackgroundUnfocused =
            new LinearGradient(new Color(10, 40, 10, 200), new Color(0, 120, 60, 200), new CornerRadius(4), false);
        cloakBtn.TextColor = NeonGreen;
        cloakBtn.StateChanged += (_, on) => AppendLog(on ? ">> Cloak ENGAGED" : ">> Cloak DISENGAGED");

        // ── FREQUENCY ──
        new UILabel(Screen, "[ FREQUENCY BAND ]") { TextColor = WarningAmber, FontSize = 14 };
        var freqSlider = new UISlider(Screen);
        freqSlider.Value = 0.42f;

        // ── SUBSYSTEMS ──
        new UILabel(Screen, "[ SUBSYSTEMS ]") { TextColor = TextDim, FontSize = 14 };
        new UICheckBox(Screen, "Auto-pilot", true) { TextColor = NeonCyan };
        new UICheckBox(Screen, "ECM Jammer", false) { TextColor = NeonCyan };
        new UICheckBox(Screen, "Hull Repair", true) { TextColor = NeonGreen };

        // ── STATUS LOG ──
        new UILabel(Screen, "[ COMMS LOG ]") { TextColor = NeonCyan, FontSize = 14 };
        _statusLog = new UILabel(Screen, "> System online\n> All subsystems nominal");
        _statusLog.TextColor = NeonGreen;
        _statusLog.FontSize = 11;
        _statusLog.WrapText = true;
    }

    void AppendLog(string msg)
    {
        if (_statusLog == null) return;
        var lines = (_statusLog.Caption ?? "").Split('\n');
        if (lines.Length > 8)
            _statusLog.Caption = string.Join('\n', lines[^8..]);
        _statusLog.Caption += $"\n{msg}";
    }

    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        _elapsed += deltaSeconds;

        // Animate bars with subtle pulse
        if (_shieldBar != null)
            _shieldBar.Value = 0.80f + 0.05f * MathF.Sin(_elapsed * 1.2f);
        if (_signalBar != null)
            _signalBar.Value = 0.90f + 0.08f * MathF.Sin(_elapsed * 0.7f);
    }
}
