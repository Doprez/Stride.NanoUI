using NanoUI;
using NanoUI.Nvg;
using NanoUIDemos;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

// NanoUI input aliases
using UIKey = NanoUI.Common.Key;
using UIKeyModifiers = NanoUI.Common.KeyModifiers;
using UIPointerButton = NanoUI.Common.PointerButton;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// Thin game-system service for NanoUI - handles input forwarding and per-frame
/// updates for every <see cref="NanoUIComponent"/> in the scene.
/// Mirrors the role of Stride.UI's <c>UISystem</c>.
/// All GPU resources and rendering live in <see cref="NanoUISceneRenderer"/>.
/// </summary>
public partial class NanoUISystem : GameSystemBase, IService
{
    private Logger _log => GlobalLogger.GetLogger(nameof(NanoUISystem));

    // Input tracking
    private InputManager _input = null!;
    private System.Numerics.Vector2 _previousMousePos;

    /// <summary>Set by <see cref="NanoUISceneRenderer"/> after it creates the NvgContext.</summary>
    internal NvgContext? NanoContext { get; set; }

    // Camera matrices - written by NanoUISceneRenderer each frame so we
    // can do world-space hit-testing one frame later.
    internal Matrix CameraView { get; set; }
    internal Matrix CameraViewProjection { get; set; }
    internal bool HasCameraMatrices { get; set; }

    public NanoUISystem([NotNull] IServiceRegistry registry) : base(registry)
    {
    }

    protected override void LoadContent()
    {
        _input = Services.GetService<InputManager>()!;

        Game.Window.ClientSizeChanged += Window_ClientSizeChanged;

        Enabled = true;  // Update() will be called
        Visible = false; // No Draw() - rendering is handled by the compositor via NanoUISceneRenderer
    }

    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        if (NanoContext == null) return;

        var backBufferSize = GraphicsDevice.Presenter.BackBuffer.Size;
        var newSize = new System.Numerics.Vector2(backBufferSize.Width, backBufferSize.Height);

        foreach (var comp in CollectComponents())
        {
            if (!comp.Enabled || comp.Page?.Content == null) continue;

            if (comp.IsFullScreen)
            {
                comp.Page.Content.ScreenResize(newSize, NanoContext);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        var components = CollectComponents();
        if (components.Count == 0) return;

        float dt = (float)gameTime.Elapsed.TotalSeconds;

        // Route input to every enabled component
        foreach (var comp in components)
        {
            if (!comp.Enabled || comp.Page?.Content == null) continue;

            if (comp.IsFullScreen)
            {
                ProcessInputFullscreen(comp.Page.Content);
            }
            else if (HasCameraMatrices)
            {
                ProcessInputWorldSpace(comp);
            }
        }

        // Update all components
        foreach (var comp in components)
        {
            if (!comp.Enabled || comp.Page?.Content == null) continue;
            comp.Page.Content.Update(dt);
        }
    }

    void ProcessInputFullscreen(DemoBase content)
    {
        if (_input.Mouse == null)
            return;

        var mouse = _input.Mouse;
        // Use back buffer size for coordinate conversion so mouse coords match the
        // rendering projection (ClientBounds can differ due to DPI scaling).
        var surfSize = GraphicsDevice.Presenter.BackBuffer.Size;
        var mousePos = new System.Numerics.Vector2(mouse.Position.X * surfSize.Width,
                                                    mouse.Position.Y * surfSize.Height);

        // Mouse move
        var delta = mousePos - _previousMousePos;
        if (delta.X != 0 || delta.Y != 0)
        {
            content.OnPointerMove(mousePos, delta);
        }
        _previousMousePos = mousePos;

        // Mouse buttons
        foreach (var btn in mouse.PressedButtons)
        {
            content.OnPointerUpDown(mousePos, NanoInputMapping.MapMouseButtons(btn), true);
        }
        foreach (var btn in mouse.ReleasedButtons)
        {
            content.OnPointerUpDown(mousePos, NanoInputMapping.MapMouseButtons(btn), false);
        }

        // Mouse scroll
        float wheelDelta = _input.MouseWheelDelta;
        if (wheelDelta != 0)
        {
            content.OnPointerScroll(mousePos, new System.Numerics.Vector2(0, wheelDelta));
        }

        ProcessKeyboardInput(content);
    }

    /// <summary>
    /// Processes input for a world-space (non-fullscreen) component by ray-casting
    /// the mouse position onto the panel plane.
    /// </summary>
    void ProcessInputWorldSpace(NanoUIComponent comp)
    {
        if (_input.Mouse == null || comp.Page?.Content == null)
            return;

        var content = comp.Page.Content;
        var mouse = _input.Mouse;

        // Ray-cast to find panel-space mouse position
        bool onPanel = comp.TryScreenToPanel(
            mouse.Position, CameraViewProjection, CameraView,
            out var panelMousePos);

        if (onPanel)
        {
            var delta = panelMousePos - _previousMousePos;
            if (delta.X != 0 || delta.Y != 0)
                content.OnPointerMove(panelMousePos, delta);
            _previousMousePos = panelMousePos;

            foreach (var btn in mouse.PressedButtons)
                content.OnPointerUpDown(panelMousePos, NanoInputMapping.MapMouseButtons(btn), true);
            foreach (var btn in mouse.ReleasedButtons)
                content.OnPointerUpDown(panelMousePos, NanoInputMapping.MapMouseButtons(btn), false);

            float wheelDelta = _input.MouseWheelDelta;
            if (wheelDelta != 0)
                content.OnPointerScroll(panelMousePos, new System.Numerics.Vector2(0, wheelDelta));
        }

        // Keyboard always forwarded regardless of mouse position
        ProcessKeyboardInput(content);
    }

    void ProcessKeyboardInput(DemoBase content)
    {
        if (_input.Keyboard == null) return;

        var keyboard = _input.Keyboard;

        foreach (var key in keyboard.PressedKeys)
        {
            if (NanoInputMapping.TryMapKey(key, true, out var uiKey, out _))
            {
                content.OnKeyUpDown(uiKey, true, NanoInputMapping.KeyModifiers);
            }
        }

        foreach (var key in keyboard.ReleasedKeys)
        {
            if (NanoInputMapping.TryMapKey(key, false, out var uiKey, out _))
            {
                content.OnKeyUpDown(uiKey, false, NanoInputMapping.KeyModifiers);
            }
        }

        foreach (var ev in _input.Events)
        {
            if (ev is TextInputEvent textEvent)
            {
                foreach (char c in textEvent.Text)
                {
                    if (c >= 32)
                        content.OnKeyChar(c);
                }
            }
        }
    }

    /// <summary>
    /// Walks all scenes and collects every <see cref="NanoUIComponent"/>.
    /// </summary>
    private List<NanoUIComponent> CollectComponents()
    {
        var result = new List<NanoUIComponent>();

        var sceneSystem = Services.GetService<SceneSystem>();
        if (sceneSystem?.SceneInstance == null)
            return result;

        CollectFromScene(sceneSystem.SceneInstance.RootScene, result);
        return result;
    }

    private static void CollectFromScene(Scene? scene, List<NanoUIComponent> result)
    {
        if (scene == null) return;

        foreach (var entity in scene.Entities)
        {
            var comp = entity.Get<NanoUIComponent>();
            if (comp != null)
                result.Add(comp);
        }

        foreach (var child in scene.Children)
        {
            CollectFromScene(child, result);
        }
    }

    protected override void Destroy()
    {
        foreach (var comp in CollectComponents())
        {
            comp.Page?.Dispose();
        }
        NanoContext?.Dispose();
    }

    public static IService NewInstance(IServiceRegistry services)
    {
        return new NanoUISystem(services);
    }
}
