using Stride.Core.Diagnostics;
using Stride.Engine;

namespace NanoUIDemo;
public class CustomGame : Game
{

    protected override void BeginRun()
    {
        var gamedateTime = Services.GetOrCreate<NanoUISystem>();
        GameSystems.Add(gamedateTime);

        Window.AllowUserResizing = true;

        Logger.MinimumLevelEnabled = LogMessageType.Debug;
    }
}
