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

    // --- Panel 1: Billboard (faces the camera), centre ---
    var billboard = DemoNanoUIComponent.Create(DemoType.UIBasic, isFullScreen: false);
    billboard.IsBillboard = true;
    var billboardEntity = new Entity("NanoUI - Billboard")
    {
        billboard
    };
    billboardEntity.Transform.Position = new Vector3(0, 1.5f, 0);
    scene.Entities.Add(billboardEntity);

    // --- Panel 2: Fixed panel, left side, rotated to face +X ---
    var panelLeft = DemoNanoUIComponent.Create(DemoType.UIExtended, isFullScreen: true);
    var leftEntity = new Entity("NanoUI - Left")
    {
        panelLeft
    };
    leftEntity.Transform.Position = new Vector3(-2f, 1.5f, -1f);
    leftEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(45));
    scene.Entities.Add(leftEntity);

    // --- Panel 3: Fixed panel, right side, rotated to face -X ---
    var panelRight = DemoNanoUIComponent.Create(DemoType.UILayouts, isFullScreen: false);
    panelRight.IsBillboard = false;
    var rightEntity = new Entity("NanoUI - Right")
    {
        panelRight
    };
    rightEntity.Transform.Position = new Vector3(2f, 1.5f, -1f);
    rightEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-45));
    scene.Entities.Add(rightEntity);

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
