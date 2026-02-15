using NanoUI;
using NanoUI.Common;
using NanoUI.Components;
using NanoUI.Layouts;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// A nature / environmental dashboard with earthy green and brown tones,
/// organic shapes, and environmental sensor readouts.
/// </summary>
public class NatureDashboardDemo : DemoBase
{
    // Theme palette
    static readonly Color BgForest    = new(22, 36, 18, 220);
    static readonly Color BgPanel     = new(32, 48, 28, 210);
    static readonly Color LeafGreen   = new(76, 175, 80, 255);
    static readonly Color MossGreen   = new(46, 125, 50, 255);
    static readonly Color EarthBrown  = new(121, 85, 72, 255);
    static readonly Color WarmAmber   = new(255, 193, 7, 255);
    static readonly Color SkyBlue     = new(100, 181, 246, 255);
    static readonly Color TextCream   = new(245, 240, 225, 255);
    static readonly Color TextMuted   = new(180, 200, 170, 200);
    static readonly Color SunGold     = new(255, 215, 0, 255);
    static readonly Color WaterBlue   = new(33, 150, 243, 200);

    private UIProgressbar? _humidityBar;
    private UIProgressbar? _soilBar;
    private UIProgressbar? _airBar;
    private UILabel? _tempLabel;
    private UILabel? _weatherLog;
    private float _elapsed;

    public NatureDashboardDemo(UIScreen screen) : base(screen)
    {
        BuildUI();
    }

    void BuildUI()
    {
        if (Screen == null) return;

        // Style the screen itself as the panel background
        Screen.BackgroundFocused = Screen.BackgroundUnfocused =
            new LinearGradient(BgForest, BgPanel, new CornerRadius(8), false);
        Screen.ChildrenLayout = new GroupLayout { Spacing = new Vector2(6) };
        Screen.TextColor = LeafGreen;
        Screen.FontSize = 14;

        // ── TITLE ──
        var title = new UILabel(Screen, "Eco Monitor Station");
        title.TextColor = SunGold;
        title.FontSize = 20;

        _tempLabel = new UILabel(Screen, "Temperature: 22.4 C  |  Wind: 12 km/h NW");
        _tempLabel.TextColor = TextCream;
        _tempLabel.FontSize = 13;

        // ── ENVIRONMENTAL SENSORS ──
        new UILabel(Screen, "Environmental Sensors") { TextColor = LeafGreen, FontSize = 16 };

        // Humidity
        new UILabel(Screen, "Humidity") { TextColor = SkyBlue, FontSize = 12 };
        _humidityBar = new UIProgressbar(Screen);
        _humidityBar.Value = 0.68f;
        _humidityBar.BackgroundFocused = _humidityBar.BackgroundUnfocused =
            new LinearGradient(WaterBlue, SkyBlue, new CornerRadius(5), true);

        // Soil moisture
        new UILabel(Screen, "Soil Moisture") { TextColor = EarthBrown, FontSize = 12 };
        _soilBar = new UIProgressbar(Screen);
        _soilBar.Value = 0.55f;
        _soilBar.BackgroundFocused = _soilBar.BackgroundUnfocused =
            new LinearGradient(EarthBrown, new Color(160, 120, 90, 220), new CornerRadius(5), true);

        // Air quality
        new UILabel(Screen, "Air Quality Index") { TextColor = MossGreen, FontSize = 12 };
        _airBar = new UIProgressbar(Screen);
        _airBar.Value = 0.91f;
        _airBar.BackgroundFocused = _airBar.BackgroundUnfocused =
            new LinearGradient(MossGreen, LeafGreen, new CornerRadius(5), true);

        // ── CONTROLS ──
        new UILabel(Screen, "Irrigation Controls") { TextColor = WarmAmber, FontSize = 16 };

        var waterBtn = new UIButton(Screen, "Activate Sprinklers");
        waterBtn.BackgroundFocused = waterBtn.BackgroundUnfocused =
            new LinearGradient(WaterBlue, SkyBlue, new CornerRadius(6), false);
        waterBtn.TextColor = TextCream;
        waterBtn.Clicked += () => AppendLog("Sprinklers activated - Zone A");

        var harvestBtn = new UIButton(Screen, "Schedule Harvest");
        harvestBtn.BackgroundFocused = harvestBtn.BackgroundUnfocused =
            new LinearGradient(EarthBrown, WarmAmber, new CornerRadius(6), false);
        harvestBtn.TextColor = TextCream;
        harvestBtn.Clicked += () => AppendLog("Harvest scheduled for dawn");

        // Light level slider
        new UILabel(Screen, "Grow-Light Intensity") { TextColor = SunGold, FontSize = 13 };
        var lightSlider = new UISlider(Screen);
        lightSlider.Value = 0.7f;

        // ── OPTIONS ──
        new UILabel(Screen, "Automation") { TextColor = TextMuted, FontSize = 14 };
        new UICheckBox(Screen, "Auto-irrigate", true) { TextColor = LeafGreen };
        new UICheckBox(Screen, "Frost protection", true) { TextColor = SkyBlue };
        new UICheckBox(Screen, "Night lights", false) { TextColor = SunGold };

        // ── Species selector ──
        new UILabel(Screen, "Crop Monitor") { TextColor = LeafGreen, FontSize = 14 };
        var cropCombo = new UIComboBox<string>(Screen);
        cropCombo.AddItem("Tomatoes - Bed A", "tomato");
        cropCombo.AddItem("Basil - Bed B", "basil");
        cropCombo.AddItem("Sunflowers - Bed C", "sunflower");
        cropCombo.AddItem("Strawberries - Bed D", "strawberry");
        cropCombo.SelectedChanged += crop => AppendLog($"Monitoring: {crop}");

        // ── LOG ──
        new UILabel(Screen, "Activity Log") { TextColor = TextMuted, FontSize = 14 };
        _weatherLog = new UILabel(Screen, "Station online\nSensors calibrated\nAll systems green");
        _weatherLog.TextColor = LeafGreen;
        _weatherLog.FontSize = 11;
        _weatherLog.WrapText = true;
    }

    void AppendLog(string msg)
    {
        if (_weatherLog == null) return;
        var lines = (_weatherLog.Caption ?? "").Split('\n');
        if (lines.Length > 8)
            _weatherLog.Caption = string.Join('\n', lines[^8..]);
        var time = DateTime.Now.ToString("HH:mm");
        _weatherLog.Caption += $"\n[{time}] {msg}";
    }

    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        _elapsed += deltaSeconds;

        // Gently fluctuate sensor readings
        if (_humidityBar != null)
            _humidityBar.Value = 0.65f + 0.06f * MathF.Sin(_elapsed * 0.5f);
        if (_soilBar != null)
            _soilBar.Value = 0.52f + 0.04f * MathF.Sin(_elapsed * 0.3f + 1f);
        if (_airBar != null)
            _airBar.Value = 0.88f + 0.06f * MathF.Sin(_elapsed * 0.4f + 2f);

        // Update temperature readout
        if (_tempLabel != null)
        {
            float temp = 22.0f + 1.5f * MathF.Sin(_elapsed * 0.15f);
            float wind = 10f + 5f * MathF.Sin(_elapsed * 0.2f + 0.5f);
            _tempLabel.Caption = $"Temperature: {temp:F1} C  |  Wind: {wind:F0} km/h NW";
        }
    }
}
