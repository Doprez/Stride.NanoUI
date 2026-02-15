using NanoUI;
using NanoUI.Common;
using NanoUI.Components;
using NanoUI.Components.Docking;
using NanoUI.Layouts;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// A demo that creates an IDE-like layout with panels docked to all four sides
/// of the window. Each panel shows interactive controls to demonstrate real usage.
///
/// Layout:
///   ┌──────────┬───────────────────────┬───────────┐
///   │          │                       │           │
///   │  Left    │      Center           │   Right   │
///   │  Panel   │      Workspace        │   Panel   │
///   │          │                       │           │
///   ├──────────┴───────────────────────┴───────────┤
///   │                Bottom Panel                  │
///   └──────────────────────────────────────────────┘
/// </summary>
public class SideDockingDemo : DemoBase
{
    // Shared state so panels can interact with each other
    UILabel? _statusLabel;
    UIProgressbar? _taskProgress;
    int _logLineCount;

    public SideDockingDemo(UIScreen screen) : base(screen)
    {
        CreateLayout();
    }

    void CreateLayout()
    {
        if (_screen == null)
            return;

        var container = new DockContainer(_screen, Orientation.Vertical);
        container.Size = _screen.Size;

        // Root splits vertically: top area | bottom panel
        if (container.FirstNode != null && container.SecondNode != null)
        {
            SetupBottomPanel(container.SecondNode);

            // Top area splits horizontally: (left + center) | right
            container.FirstNode.CreateSubNodes(Orientation.Horizontal);

            if (container.FirstNode.SecondNode != null)
                SetupRightPanel(container.FirstNode.SecondNode);

            if (container.FirstNode.FirstNode != null)
            {
                // Left + center splits horizontally: left | center
                container.FirstNode.FirstNode.CreateSubNodes(Orientation.Horizontal);

                if (container.FirstNode.FirstNode.FirstNode != null)
                    SetupLeftPanel(container.FirstNode.FirstNode.FirstNode);

                if (container.FirstNode.FirstNode.SecondNode != null)
                    SetupCenterPanel(container.FirstNode.FirstNode.SecondNode);
            }
        }
    }

    // ───────────────────── Left Panel ─────────────────────

    void SetupLeftPanel(DockNode node)
    {
        if (Screen == null) return;

        node.Title = "Explorer";
        node.BackgroundFocused = node.BackgroundUnfocused =
            new SolidBrush(new Color(45, 45, 48, 230));

        // ── Explorer tab ──
        var explorer = new DockWindow(Screen);
        explorer.Title = "Explorer";
        explorer.TabCaption = "Explorer";
        explorer.ChildrenLayout = new GroupLayout();

        new UILabel(explorer, "Project Files");

        var fileList = new UIListBox<string>(explorer);
        fileList.AddItem("Program.cs", "Program.cs");
        fileList.AddItem("NanoUISystem.cs", "NanoUISystem.cs");
        fileList.AddItem("SideDockingDemo.cs", "SideDockingDemo.cs");
        fileList.AddItem("NanoInputMapping.cs", "NanoInputMapping.cs");
        fileList.AddItem("NanoUIShader.sdsl", "NanoUIShader.sdsl");
        fileList.SelectedChanged += file =>
        {
            AppendStatus($"Opened: {file}");
        };

        new UILabel(explorer, "Search Files");
        var searchField = new UITextField(explorer);
        searchField.PlaceholderText = "Type to filter...";

        AttachTab(node, explorer);

        // ── Toolbox tab ──
        var toolbox = new DockWindow(Screen);
        toolbox.Title = "Toolbox";
        toolbox.TabCaption = "Toolbox";
        toolbox.ChildrenLayout = new GroupLayout();

        new UILabel(toolbox, "Drag controls to canvas");

        string[] controls = ["Button", "Label", "TextField", "Slider", "CheckBox", "ComboBox", "ProgressBar"];
        foreach (var name in controls)
        {
            var btn = new UIButton(toolbox, name);
            btn.Clicked += () => AppendStatus($"Toolbox: selected {name}");
        }

        AttachTab(node, toolbox);
    }

    // ───────────────────── Right Panel ─────────────────────

    void SetupRightPanel(DockNode node)
    {
        if (Screen == null) return;

        node.Title = "Properties";
        node.BackgroundFocused = node.BackgroundUnfocused =
            new SolidBrush(new Color(45, 45, 48, 230));

        // ── Properties tab ──
        var properties = new DockWindow(Screen);
        properties.Title = "Properties";
        properties.TabCaption = "Properties";
        properties.ChildrenLayout = new GroupLayout();

        new UILabel(properties, "Widget Inspector");

        new UILabel(properties, "Name");
        var nameField = new UITextField(properties, "myButton");
        nameField.TextChanged += t => AppendStatus($"Name changed: {t}");

        new UILabel(properties, "Text");
        var textField = new UITextField(properties, "Click Me");

        new UILabel(properties, "Width");
        var widthSlider = new UISlider(properties);
        widthSlider.Value = 0.5f;
        widthSlider.ValueChanged += v => AppendStatus($"Width: {v:P0}");

        new UILabel(properties, "Height");
        var heightSlider = new UISlider(properties);
        heightSlider.Value = 0.3f;

        new UILabel(properties, "Opacity");
        var opacitySlider = new UISlider(properties);
        opacitySlider.Value = 1.0f;

        new UILabel(properties, "Options");
        new UICheckBox(properties, "Visible", true);
        new UICheckBox(properties, "Enabled", true);
        new UICheckBox(properties, "Focusable", false);

        AttachTab(node, properties);

        // ── Theme tab ──
        var theme = new DockWindow(Screen);
        theme.Title = "Theme";
        theme.TabCaption = "Theme";
        theme.ChildrenLayout = new GroupLayout();

        new UILabel(theme, "Appearance");

        new UILabel(theme, "Color Scheme");
        var colorCombo = new UIComboBox<string>(theme);
        colorCombo.AddItem("Dark", "dark");
        colorCombo.AddItem("Light", "light");
        colorCombo.AddItem("Blue", "blue");
        colorCombo.AddItem("High Contrast", "highcontrast");
        colorCombo.SelectedChanged += scheme => AppendStatus($"Theme: {scheme}");

        new UILabel(theme, "Font Size");
        var fontSlider = new UISlider(theme);
        fontSlider.Value = 0.4f;

        new UILabel(theme, "UI Scale");
        var scaleSlider = new UISlider(theme);
        scaleSlider.Value = 0.5f;

        new UICheckBox(theme, "Animations", true);
        new UICheckBox(theme, "Rounded Corners", true);
        new UICheckBox(theme, "Show Tooltips", true);

        AttachTab(node, theme);
    }

    // ───────────────────── Bottom Panel ─────────────────────

    void SetupBottomPanel(DockNode node)
    {
        if (Screen == null) return;

        node.Title = "Output";
        node.BackgroundFocused = node.BackgroundUnfocused =
            new SolidBrush(new Color(40, 40, 42, 230));

        // ── Output / Log tab ──
        var output = new DockWindow(Screen);
        output.Title = "Output";
        output.TabCaption = "Output";
        output.ChildrenLayout = new GroupLayout();

        _statusLabel = new UILabel(output, "");
        _statusLabel.WrapText = true;
        AppendStatus("[ready] Docking demo loaded");
        AppendStatus("[ready] All panels docked to window edges");
        AppendStatus("[ready] Interact with controls to see log updates");

        AttachTab(node, output);

        // ── Tasks tab ──
        var tasks = new DockWindow(Screen);
        tasks.Title = "Tasks";
        tasks.TabCaption = "Tasks";
        tasks.ChildrenLayout = new GroupLayout();

        new UILabel(tasks, "Background Tasks");

        _taskProgress = new UIProgressbar(tasks);
        _taskProgress.Value = 0f;

        var progressSlider = new UISlider(tasks);
        progressSlider.Value = 0f;
        progressSlider.ValueChanged += v =>
        {
            if (_taskProgress != null)
                _taskProgress.Value = v;
        };

        new UILabel(tasks, "Task Controls");
        var startBtn = new UIButton(tasks, "Start Task");
        startBtn.Clicked += () =>
        {
            if (_taskProgress != null) _taskProgress.Value = 0.0f;
            AppendStatus("Task started");
        };
        var completeBtn = new UIButton(tasks, "Complete Task");
        completeBtn.Clicked += () =>
        {
            if (_taskProgress != null) _taskProgress.Value = 1.0f;
            AppendStatus("Task completed!");
        };

        AttachTab(node, tasks);

        // ── Console tab ──
        var console = new DockWindow(Screen);
        console.Title = "Console";
        console.TabCaption = "Console";
        console.ChildrenLayout = new GroupLayout();

        var consoleOutput = new UILabel(console, "> NanoUI Docking Demo v1.0\n> Type a command below");
        consoleOutput.WrapText = true;

        var consoleInput = new UITextField(console);
        consoleInput.PlaceholderText = "Enter command...";

        var runBtn = new UIButton(console, "Run");
        runBtn.Clicked += () =>
        {
            var cmd = consoleInput.Text;
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                consoleOutput.Caption += $"\n> {cmd}\n  OK";
                consoleInput.ResetText("");
                AppendStatus($"Console: {cmd}");
            }
        };

        AttachTab(node, console);
    }

    // ───────────────────── Center Panel ─────────────────────

    void SetupCenterPanel(DockNode node)
    {
        if (Screen == null) return;

        node.Title = "Editor";
        node.BackgroundFocused = node.BackgroundUnfocused =
            new SolidBrush(new Color(30, 30, 30, 200));

        // ── Welcome tab ──
        var welcome = new DockWindow(Screen);
        welcome.Title = "Welcome";
        welcome.TabCaption = "Welcome";
        welcome.ChildrenLayout = new GroupLayout();

        var title = new UILabel(welcome, "NanoUI Docking Demo");
        title.FontSize = 24;

        new UILabel(welcome, "This demo shows an IDE-style layout with panels\ndocked to all four sides of the window.");

        new UILabel(welcome, "Docked Panels");
        new UILabel(welcome, "  Left   - Explorer (file list) and Toolbox");
        new UILabel(welcome, "  Right  - Properties and Theme settings");
        new UILabel(welcome, "  Bottom - Output log, Tasks, and Console");
        new UILabel(welcome, "  Center - This workspace area");

        new UILabel(welcome, "Tips");
        new UILabel(welcome, "  - Drag splitters between panels to resize");
        new UILabel(welcome, "  - Drag tab headers to rearrange or undock");
        new UILabel(welcome, "  - Interact with controls, results appear\n    in the Output panel");

        AttachTab(node, welcome);

        // ── Controls Demo tab ──
        var controls = new DockWindow(Screen);
        controls.Title = "Controls";
        controls.TabCaption = "Controls";
        controls.ChildrenLayout = new GroupLayout();

        new UILabel(controls, "Interactive Controls Showcase");

        // Buttons section
        new UILabel(controls, "Buttons");
        var normalBtn = new UIButton(controls, "Normal Button");
        normalBtn.Clicked += () => AppendStatus("Normal button clicked");

        var toggleBtn = new UIButton(controls, "Toggle Button");
        toggleBtn.Flags = ButtonFlags.ToggleButton;
        toggleBtn.StateChanged += (btn, pushed) =>
            AppendStatus($"Toggle: {(pushed ? "ON" : "OFF")}");

        // Text input
        new UILabel(controls, "Text Input");
        var inputField = new UITextField(controls, "Edit this text");
        inputField.TextChanged += t => AppendStatus($"Text: {t}");

        // Slider
        new UILabel(controls, "Value Slider");
        var slider = new UISlider(controls);
        slider.Value = 0.5f;

        // Checkboxes
        new UILabel(controls, "Options");
        new UICheckBox(controls, "Option A", true);
        new UICheckBox(controls, "Option B", false);
        new UICheckBox(controls, "Option C", true);

        // Combo box
        new UILabel(controls, "Selection");
        var combo = new UIComboBox<int>(controls);
        combo.AddItem("First Item", 1);
        combo.AddItem("Second Item", 2);
        combo.AddItem("Third Item", 3);
        combo.SelectedChanged += idx => AppendStatus($"Selected item: {idx}");

        // Progress
        new UILabel(controls, "Progress");
        var progress = new UIProgressbar(controls);
        progress.Value = 0.65f;

        AttachTab(node, controls);

        // ── Form Demo tab ──
        var form = new DockWindow(Screen);
        form.Title = "Form";
        form.TabCaption = "Form";
        form.ChildrenLayout = new GroupLayout();

        new UILabel(form, "Sample Data Entry Form");

        new UILabel(form, "First Name");
        new UITextField(form, "John");

        new UILabel(form, "Last Name");
        new UITextField(form, "Doe");

        new UILabel(form, "Email");
        var emailField = new UITextField(form);
        emailField.PlaceholderText = "user@example.com";

        new UILabel(form, "Role");
        var roleCombo = new UIComboBox<string>(form);
        roleCombo.AddItem("Developer", "dev");
        roleCombo.AddItem("Designer", "design");
        roleCombo.AddItem("Manager", "mgr");
        roleCombo.AddItem("QA Tester", "qa");

        new UILabel(form, "Experience (years)");
        var expSlider = new UISlider(form);
        expSlider.Value = 0.3f;

        new UICheckBox(form, "Subscribe to newsletter", false);
        new UICheckBox(form, "Accept terms & conditions", false);

        var submitBtn = new UIButton(form, "Submit");
        submitBtn.Clicked += () => AppendStatus("Form submitted!");

        var resetBtn = new UIButton(form, "Reset");
        resetBtn.Clicked += () => AppendStatus("Form reset");

        AttachTab(node, form);
    }

    // ───────────────────── Helpers ─────────────────────

    /// <summary>
    /// Attaches a DockWindow to a DockNode as a non-closable tab.
    /// </summary>
    static void AttachTab(DockNode node, DockWindow window)
    {
        if (node.TryAttach(window, out UITabItem? tab))
        {
            if (tab != null) tab.Closable = false;
        }
    }

    /// <summary>
    /// Appends a timestamped line to the Output panel's status label.
    /// </summary>
    void AppendStatus(string message)
    {
        _logLineCount++;
        if (_statusLabel != null)
        {
            // Keep the log from growing unbounded — trim to last ~20 lines
            var current = _statusLabel.Caption ?? "";
            var lines = current.Split('\n');
            if (lines.Length > 20)
            {
                current = string.Join('\n', lines[^20..]);
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _statusLabel.Caption = $"{current}\n[{timestamp}] {message}";
        }
    }
}
