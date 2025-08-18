using NanoUIDemo.CodeOnly;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.ImGui;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    game.AddSkybox();

    var gamedateTime = game.Services.GetOrCreate<NanoUISystem>();
    game.GameSystems.Add(gamedateTime);

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
