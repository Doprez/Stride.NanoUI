using NanoUIDemo.CodeOnly;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
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

    var ui = game.Services.GetOrCreate<NanoUISystem>();
    game.GameSystems.Add(ui);

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
