using NanoUI;
using NanoUI.Common;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// Example component that creates a NanoUI demo and wraps it in a
/// <see cref="NanoUIComponent"/> + <see cref="NanoUIPage"/>.
/// <para>Attach this to an entity via <see cref="DemoNanoUIComponent.Create"/>.</para>
/// </summary>
public static class DemoNanoUIComponent
{
    /// <summary>
    /// Creates a <see cref="NanoUIComponent"/> pre-configured with a demo page.
    /// </summary>
    /// <param name="demoType">Which built-in NanoUI demo to load.</param>
    /// <param name="isFullScreen">
    ///   <c>true</c> → fullscreen overlay (screen-space);
    ///   <c>false</c> → placed in the 3D scene at the entity's transform.
    /// </param>
    /// <param name="resolution">Virtual resolution (default 1280×720).</param>
    public static NanoUIComponent Create(
        DemoType demoType = DemoType.UIBasic,
        bool isFullScreen = true,
        Vector2? resolution = null)
    {
        var res = resolution ?? new Vector2(1280, 720);

        var page = new NanoUIPage
        {
            ContentFactory = (ctx, size) =>
            {
                var demo = DemoFactory.CreateDemo(ctx, demoType, size);

                // Make the screen transparent so the 3D scene shows through
                if (demo?.Screen != null)
                {
                    demo.Screen.BackgroundUnfocused = new SolidBrush(NanoUI.Common.Color.Transparent);
                    demo.Screen.BackgroundFocused = new SolidBrush(NanoUI.Common.Color.Transparent);
                    demo.Screen.BackgroundPushed = new SolidBrush(NanoUI.Common.Color.Transparent);
                }

                return demo!;
            }
        };

        return new NanoUIComponent
        {
            Page = page,
            IsFullScreen = isFullScreen,
            Resolution = res,
        };
    }
}
