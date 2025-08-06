using NanoUI;
using NanoUI.Common;
using NanoUI.Nvg;
using NanoUI.Rendering;
using NanoUI.Rendering.Data;
//using NanoUIDemos;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using Texture2D = Stride.Graphics.Texture;

namespace NanoUIDemo;

public class NanoUISystem : GameSystemBase, INvgRenderer, IService
{

    const int INITIAL_VERTEX_BUFFER_SIZE = 128;
    const int INITIAL_INDEX_BUFFER_SIZE = 128;

    // mapping texture ids & textures
    readonly Dictionary<int, Texture2D> _textures = [];
    int counter = 0;

    private GraphicsContext _graphicsContext;
    private GraphicsDeviceManager _deviceManager;
    private EffectSystem _effectSystem;
    private CommandList _commandList;

    private PipelineState _nanoPipeline;
    private VertexDeclaration _nanoVertLayout;
    VertexBufferBinding _vertexBinding;
    IndexBufferBinding _indexBinding;
    private EffectInstance _nanoShader;

    private Logger _log => GlobalLogger.GetLogger(nameof(NanoUISystem));

    // DemoTypes:
    // Docking, Drawing, SDFText, SvgShapes, TextShapes, UIBasic, UIExtended, UIExtended2,
    // UIExperimental, UILayouts
    //static DemoType _demoType = DemoType.UIBasic;
    //static DemoBase _demo;

    bool ree = false;

    public NanoUISystem([NotNull] IServiceRegistry registry) : base(registry)
    {
    }

    protected override void LoadContent()
    {
        _effectSystem = Services.GetService<EffectSystem>();
        _deviceManager = Services.GetService<IGraphicsDeviceManager>() as GraphicsDeviceManager;
        _graphicsContext = Services.GetService<IGame>().GraphicsContext;

        Game.Window.ClientSizeChanged += Window_ClientSizeChanged;

        Enabled = true; // Force Update functions to be run
        Visible = true; // Force Draw related functions to be run
        UpdateOrder = 1; // Update should occur after Stride's InputManager

        NvgContext nanoContext = new(this);

        //_demo = DemoFactory.CreateDemo(nanoContext, _demoType, new Vector2(Game.Window.PreferredWindowedSize.X, Game.Window.PreferredWindowedSize.Y));

        // vbos etc
        CreateDeviceObjects();
    }

    void CreateDeviceObjects()
    {
        // set up a commandlist
        _commandList = _graphicsContext.CommandList;

        // compile de shader
        _nanoShader = new EffectInstance(_effectSystem.LoadEffect("NanoUIShader").WaitForResult());
        _nanoShader.UpdateEffect(GraphicsDevice);

        var layout = new VertexDeclaration(
            VertexElement.Position<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>(1)
        );

        _nanoVertLayout = layout;

        // de pipeline desc
        var pipeline = new PipelineStateDescription()
        {
            BlendState = BlendStates.NonPremultiplied,

            RasterizerState = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultisampleAntiAliasLine = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            },

            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = _nanoVertLayout.CreateInputElements(),
            DepthStencilState = DepthStencilStates.Default,

            EffectBytecode = _nanoShader.Effect.Bytecode,
            RootSignature = _nanoShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        // finally set up the pipeline
        //var pipelineState = PipelineState.New(GraphicsDevice, ref pipeline);
        //_nanoPipeline = pipelineState;

        var is32Bits = false;
        var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
        var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        _indexBinding = indexBufferBinding;

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, INITIAL_VERTEX_BUFFER_SIZE * _nanoVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic);
        var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
        _vertexBinding = vertexBufferBinding;
    }

    private void Window_ClientSizeChanged(object sender, EventArgs e)
    {
        // TODO: handle window resize, if needed
    }

    public override void Update(GameTime gameTime)
    {
        //_demo.Update((float)gameTime.Elapsed.TotalSeconds);
    }

    public override void Draw(GameTime gameTime)
    {
        NvgContext.Instance.BeginFrame();
        //_demo.Draw(NvgContext.Instance);
        NvgContext.Instance.EndFrame();
    }

    protected override void Destroy()
    {
        //_demo?.Dispose();
        NvgContext.Instance?.Dispose();
    }

    #region INvgRenderer

    public void Render()
    {
        DoRender();
    }

    void DoRender()
    {
        // view proj
        var surfaceSize = Game.Window.ClientBounds;
        var projMatrix = Stride.Core.Mathematics.Matrix.OrthoRH(surfaceSize.Width, -surfaceSize.Height, -1, 1);

        // previous params
        DrawCommandType? previousDrawCommandType = null;
        bool updateTextureRS = true;
        int previousTexture = -1; // if below 0, gets null rs
        int uniformOffset = -1;

        // get uniforms once
        ReadOnlySpan<FragmentUniform> uniforms = DrawCache.Uniforms;

        // loop draw commands
        foreach (var drawCommand in DrawCache.DrawCommands)
        {
            // uniform buffer
            if (uniformOffset != drawCommand.UniformOffset)
            {
                uniformOffset = drawCommand.UniformOffset;

                // update uniform buffer
                //_commandList.UpdateBuffer(_fragmentUniformBuffer, 0, uniforms[drawCommand.UniformOffset]);
            }

            // pipeline
            if (previousDrawCommandType != drawCommand.DrawCommandType)
            {
                previousDrawCommandType = drawCommand.DrawCommandType;

                // must set new pipeline & uniform rs
                //_commandList.SetPipeline(GetPipeline(drawCommand.DrawCommandType));
                //_commandList.SetGraphicsResourceSet(0, _uniformBufferRS);

                updateTextureRS = true;
            }
            else if (previousTexture != drawCommand.Texture)
            {
                // texture changed
                updateTextureRS = true;
            }

            // texture resourceset
            if (updateTextureRS)
            {
                previousTexture = drawCommand.Texture;

                if(_textures.TryGetValue(drawCommand.Texture, out var textureResource))
                _nanoShader.Parameters.Set(NanoUIShaderKeys.tex, textureResource);

                updateTextureRS = false;
            }

            // Set the projection matrix and apply shader
            _nanoShader.Parameters.Set(NanoUIShaderKeys.proj, ref projMatrix);
            _nanoShader.Apply(_graphicsContext);

            // draw
            _commandList.DrawIndexed(drawCommand.IndexCount, drawCommand.IndexOffset, drawCommand.VertexOffset);
        }
    }

    public int CreateTexture(string path, NanoUI.Common.TextureFlags textureFlags = 0)
    {
        if(File.Exists(path))
        {
            // Save the file from the disk in Stride's asset manager
            using (Stream stream = File.OpenRead(path))
            {
                var localTexture = Image.Load(stream);
                ((ContentManager)Content).Save(path, localTexture);
            }
        }

        var textureData = Content.Load<Texture>(path);

        int texture = CreateTexture(new TextureDesc((uint)textureData.Width, (uint)textureData.Height));

        UpdateTexture(texture, textureData.GetData<byte>(_graphicsContext.CommandList));

        return texture;
    }

    public int CreateTexture(TextureDesc description)
    {
        var texture = Texture2D.New2D(GraphicsDevice, 
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
            //UpdateTextureBytes(tex, data, Vector2.Zero, new Vector2(tex.Width, tex.Height));

            return true;
        }

        return false;
    }

    public bool DeleteTexture(int texture)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            tex?.Dispose();

            // try delete texture resourceset also
            _log.Warning("Attempted to delete Texture. Make sure Stride cleans this up.");

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

        var newTexture = Texture2D.New2D(
            GraphicsDevice,
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
            _log.Warning("UpdateTextureRegion is not implemented yet.");

            return true;
        }

        return false;
    }
    #endregion

    public static IService NewInstance(IServiceRegistry services)
    {
        return new NanoUISystem(services);
    }
}
