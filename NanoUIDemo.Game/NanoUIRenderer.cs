using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System;

namespace NanoUIDemo;
public class NanoUIRenderer : SceneRendererBase
{
    private Effect customEffect;
    private SpriteBatch spriteBatch;
    private EffectInstance customEffectInstance;
    private SamplerState samplerState;

    public Texture Background;
    public Texture Logo;

    protected override void InitializeCore()
    {
        var game = Services.GetSafeServiceAs<IGame>();

        customEffect = EffectSystem.LoadEffect("NanoUIShader").WaitForResult();
        customEffectInstance = new EffectInstance(customEffect);

        spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = new Vector3(1) };

        var nanoRenderer = new NanoUIStrideContextRenderer(game, GraphicsDevice, Content, spriteBatch, customEffectInstance);
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        // Clear
        drawContext.CommandList.Clear(drawContext.CommandList.RenderTarget, Color.Green);
        drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

        //customEffectInstance.Parameters.Set(EffectKeys.Phase, -3 * (float)context.Time.Total.TotalSeconds);
        //
        //spriteBatch.Begin(drawContext.GraphicsContext, blendState: BlendStates.NonPremultiplied, depthStencilState: DepthStencilStates.None, effect: customEffectInstance);
        //
        //// Draw background
        //var target = drawContext.CommandList.RenderTarget;
        //var imageBufferMinRatio = Math.Min(Background.ViewWidth / (float)target.ViewWidth, Background.ViewHeight / (float)target.ViewHeight);
        //var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
        //var source = new RectangleF((Background.ViewWidth - sourceSize.X) / 2, (Background.ViewHeight - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);
        //spriteBatch.Draw(Background, new RectangleF(0, 0, 1, 1), source, Color.White, 0, Vector2.Zero);
        //
        //
        //spriteBatch.Draw(Logo, new RectangleF(0, 0, 1, 1), Color.White);
        //spriteBatch.End();
    }
}
