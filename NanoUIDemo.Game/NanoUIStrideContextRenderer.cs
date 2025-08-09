using NanoUI;
using NanoUI.Common;
using NanoUI.Rendering;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.IO;

namespace NanoUIDemo;
public class NanoUIStrideContextRenderer : INvgRenderer
{

    // mapping texture ids & textures
    readonly Dictionary<int, Texture> _textures = [];
    int counter = 0;

    private readonly IGame _game;
    private GraphicsContext _graphicsContext;
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    private ContentManager _content;

    private EffectInstance _nanoShader;

    private Logger _log => GlobalLogger.GetLogger(nameof(NanoUIStrideContextRenderer));

    public NanoUIStrideContextRenderer(IGame game, GraphicsDevice graphicsDevice, ContentManager content, SpriteBatch spriteBatch, EffectInstance effect)
    {
        _game = game;
        _graphicsDevice = graphicsDevice;
        _content = content;
        _spriteBatch = spriteBatch;
        _nanoShader = effect;
    }

    public void Render()
    {
        DoRender();
    }

    void DoRender()
    {
        // view proj
        var surfaceSize = _game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoRH(surfaceSize.Width, -surfaceSize.Height, -1, 1);

        // Set the projection matrix and apply shader
        _nanoShader.Parameters.Set(NanoUIShaderKeys.proj, ref projMatrix);
        _nanoShader.Apply(_graphicsContext);

        _spriteBatch.Begin(_graphicsContext, blendState: BlendStates.NonPremultiplied, depthStencilState: DepthStencilStates.None, effect: _nanoShader);

        // loop draw commands
        foreach (var drawCommand in DrawCache.DrawCommands)
        {
            if (_textures.TryGetValue(drawCommand.Texture, out var textureResource))
                _spriteBatch.Draw(textureResource, new RectangleF(0, 0, 1, 1), Stride.Core.Mathematics.Color.White);
        }

        _spriteBatch.End();
    }

    public int CreateTexture(string path, NanoUI.Common.TextureFlags textureFlags = 0)
    {
        if (File.Exists(path))
        {
            // Save the file from the disk in Stride's asset manager
            using (Stream stream = File.OpenRead(path))
            {
                var localTexture = Image.Load(stream);
                _content.Save(path, localTexture);
            }
        }

        var textureData = _content.Load<Texture>(path);

        int texture = CreateTexture(new TextureDesc((uint)textureData.Width, (uint)textureData.Height));

        UpdateTexture(texture, textureData.GetData<byte>(_graphicsContext.CommandList));

        return texture;
    }

    public int CreateTexture(TextureDesc description)
    {
        var texture = Texture.New2D(_graphicsDevice,
            (int)description.Width,
            (int)description.Height,
            PixelFormat.R8G8B8A8_UNorm,
            usage: GraphicsResourceUsage.Dynamic);

        // note: id is 1-based (not 0-based), since then we can neglect texture (int) value when
        // serializing theme/ui to file & have all properties with texture as default (= 0)
        counter++;

        _textures.Add(counter, texture);

        return counter;
    }

    public bool UpdateTexture(int texture, ReadOnlySpan<byte> data)
    {
        if (!data.IsEmpty && _textures.TryGetValue(texture, out var tex))
        {
            var newTexture = Texture.New2D<byte>(_graphicsDevice,
                tex.Width,
                tex.Height,
                PixelFormat.R8G8B8A8_UNorm,
                usage: GraphicsResourceUsage.Dynamic, textureData: data.ToArray());

            return true;
        }

        return false;
    }

    public bool DeleteTexture(int texture)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            tex?.Dispose();
            _textures.Remove(texture);
            return true;
        }

        return false;
    }

    public bool GetTextureSize(int texture, out System.Numerics.Vector2 size)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            size = new Vector2(tex.Width, tex.Height);
            return true;
        }

        size = Vector2.Zero;
        return false;
    }

    public bool ResizeTexture(int texture, TextureDesc description)
    {
        if (!_textures.TryGetValue(texture, out var tex))
        {
            return false;
        }

        tex?.Dispose();

        var newTexture = Texture.New2D(
            _graphicsDevice,
            (int)description.Width,
            (int)description.Height,
            PixelFormat.R8G8B8A8_UNorm,
            usage: GraphicsResourceUsage.Dynamic);

        _textures[texture] = newTexture;

        return true;
    }

    public bool UpdateTextureRegion(int texture, System.Numerics.Vector4 regionRect, ReadOnlySpan<byte> allData)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            _log.Warning("UpdateTextureRegion is not implemented.");

            return true;
        }

        return false;
    }
}
