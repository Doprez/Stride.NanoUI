using NanoUI.Nvg;
using NanoUIDemos;
using System.Numerics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// Container for NanoUI content, analogous to Stride.UI's <c>UIPage</c>.
/// Wraps a <see cref="DemoBase"/> which serves as the root of a NanoUI view
/// (it may contain a <c>UIScreen</c> widget tree, or do raw NanoVG drawing).
/// </summary>
public class NanoUIPage : IDisposable
{
    /// <summary>
    /// The NanoUI content that will be drawn and receive input.
    /// </summary>
    public DemoBase? Content { get; set; }

    /// <summary>
    /// Factory function used to create the content lazily on first render.
    /// Receives the <see cref="NvgContext"/> and the initial pixel size.
    /// Return the <see cref="DemoBase"/> to display.
    /// </summary>
    public Func<NvgContext, Vector2, DemoBase>? ContentFactory { get; set; }

    /// <summary>
    /// Creates the content using <see cref="ContentFactory"/> if not already set.
    /// Called internally by the renderer on the first frame.
    /// </summary>
    internal bool EnsureContent(NvgContext ctx, Vector2 size)
    {
        if (Content != null)
            return true;

        if (ContentFactory != null)
        {
            Content = ContentFactory(ctx, size);
            return Content != null;
        }

        return false;
    }

    public void Dispose()
    {
        Content?.Dispose();
        Content = null;
    }
}
