using NanoUIDemo.CodeOnly;
using NanoUIDemos;
using Stride.Core.Mathematics;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;

// Set working directory to executable location so relative asset paths resolve correctly
var appDir = AppContext.BaseDirectory;
Directory.SetCurrentDirectory(appDir);

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    game.AddSkybox();

    game.Window.AllowUserResizing = true;

    // Register NanoUI as a game system (handles input & updates, like Stride.UI's UISystem)
    var nanoUI = game.Services.GetOrCreate<NanoUISystem>();
    game.GameSystems.Add(nanoUI);

    // Add NanoUI to the graphics compositor as a separate render layer
    // (mirrors how Stride.UI's UIRenderFeature is wired into the compositor)
    var compositor = game.SceneSystem.GraphicsCompositor;
    compositor.AddSceneRenderer(new NanoUISceneRenderer());

    // --- Panel 1: Cyberpunk HUD — Billboard (faces the camera), centre ---
    var cyberpunkPanel = DemoNanoUIComponent.CreateCustom(
        screen => new CyberpunkHudDemo(screen),
        isFullScreen: false);
    cyberpunkPanel.IsBillboard = false;
    cyberpunkPanel.ClipToBounds = true;
    var cyberpunkEntity = new Entity("NanoUI - Cyberpunk HUD")
    {
        cyberpunkPanel
    };
    cyberpunkEntity.Transform.Position = new Vector3(0, 1.5f, 0);
    scene.Entities.Add(cyberpunkEntity);

    // --- Panel 2: Nature Dashboard — Fixed panel, left side, angled toward viewer ---
    var naturePanel = DemoNanoUIComponent.CreateCustom(
        screen => new NatureDashboardDemo(screen),
        isFullScreen: false);
    naturePanel.IsBillboard = false;
    var natureEntity = new Entity("NanoUI - Nature Dashboard")
    {
        naturePanel
    };
    natureEntity.Transform.Position = new Vector3(-2.2f, 1.5f, -1f);
    natureEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(45));
    scene.Entities.Add(natureEntity);

    // --- Panel 3: Retro Terminal — Fixed panel, right side, angled toward viewer ---
    var retroPanel = DemoNanoUIComponent.CreateCustom(
        screen => new RetroTerminalDemo(screen),
        isFullScreen: false);
    retroPanel.IsBillboard = false;
    retroPanel.ClipToBounds = true; // show off the clipping feature with some intentionally oversized content
    var retroEntity = new Entity("NanoUI - Retro Terminal")
    {
        retroPanel
    };
    retroEntity.Transform.Position = new Vector3(2.2f, 1.5f, -1f);
    retroEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-45));
    scene.Entities.Add(retroEntity);

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
