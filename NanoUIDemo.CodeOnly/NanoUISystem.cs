using NanoUI;
using NanoUI.Common;
using NanoUI.Nvg;
using NanoUI.Rendering;
using NanoUI.Rendering.Data;
using NanoUIDemos;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.CompilerServices;
using Texture2D = Stride.Graphics.Texture;

namespace NanoUIDemo.CodeOnly;

public partial class NanoUISystem : GameSystemBase, INvgRenderer, IService
{

    const int INITIAL_VERTEX_BUFFER_SIZE = 128;
    const int INITIAL_INDEX_BUFFER_SIZE = 128;

    // mapping texture ids & textures
    readonly Dictionary<int, Texture2D> _textures = [];
    private int counter = 0;

    private GraphicsContext _graphicsContext;
    private GraphicsDeviceManager _deviceManager;
    private EffectSystem _effectSystem;
    private CommandList _commandList;

    private VertexDeclaration _nanoVertLayout;
    private VertexBufferBinding _vertexBinding;
    private IndexBufferBinding _indexBinding;
    private EffectInstance _nanoShader;
    private Dictionary<DrawCommandType, PipelineState> _pipelines = new();
    private NvgContext _nanoContext;

    private Logger _log => GlobalLogger.GetLogger(nameof(NanoUISystem));

    // DemoTypes:
    // Docking, Drawing, SDFText, SvgShapes, TextShapes, UIBasic, UIExtended, UIExtended2,
    // UIExperimental, UILayouts
    static DemoType _demoType = DemoType.UIBasic;
    static DemoBase _demo;

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

        _nanoContext = new(this);

        _demo = DemoFactory.CreateDemo(_nanoContext, _demoType, new Vector2(Game.Window.PreferredWindowedSize.X, Game.Window.PreferredWindowedSize.Y));

        // vbos etc
        CreateDeviceObjects();
    }

    void CreateDeviceObjects()
    {
        // set up a commandlist
        _commandList = _graphicsContext.CommandList;

        // compile the shader
        _nanoShader = new EffectInstance(_effectSystem.LoadEffect("NanoUIShader").WaitForResult());
        _nanoShader.UpdateEffect(GraphicsDevice);
        // UI sampler: clamp + linear
        _nanoShader.Parameters.Set(NanoUIShaderKeys.TexSampler, GraphicsDevice.SamplerStates.LinearClamp);

        _nanoVertLayout = new VertexDeclaration(
            VertexElement.Position<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>()
        );

        InitPipelineSates();
        InitNullTexture();

        var is32Bits = false;
        var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
        var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        _indexBinding = indexBufferBinding;

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, INITIAL_VERTEX_BUFFER_SIZE * _nanoVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic);
        var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, _nanoVertLayout, 0);
        _vertexBinding = vertexBufferBinding;
    }

    void InitNullTexture()
    {
        var color = Color4.White;
        byte[] colorBytes = new byte[]
        {
            (byte)(color.R * 255),
            (byte)(color.G * 255),
            (byte)(color.B * 255),
            (byte)(color.A * 255)
        };
        // create a null texture (for default texture)
        var nullTexture = Texture2D.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, usage: GraphicsResourceUsage.Default, 
            textureData: colorBytes);
        _textures.Add(-1, nullTexture);
    }

    #region PipelineStates

    void InitPipelineSates()
    {
        _pipelines.Add(DrawCommandType.Triangles, CreateModelPipeline());
        _pipelines.Add(DrawCommandType.FillStencil, CreateFillStencilPipeline());
        _pipelines.Add(DrawCommandType.Fill, CreateFillPipeline());
    }

    PipelineState CreateModelPipeline()
    {
        var modelPipeline = new PipelineStateDescription()
        {
            BlendState = BlendStates.AlphaBlend,

            RasterizerState = new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontFaceCounterClockwise = true,
                DepthClipEnable = false,
                ScissorTestEnable = false,
            },

            DepthStencilState = DepthStencilStates.None,

            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = _nanoVertLayout.CreateInputElements(),

            EffectBytecode = _nanoShader.Effect.Bytecode,
            RootSignature = _nanoShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        return PipelineState.New(GraphicsDevice, ref modelPipeline);
    }

    PipelineState CreateFillStencilPipeline()
    {
        var fillStencilPipeline = new PipelineStateDescription()
        {
            BlendState = BlendStates.ColorDisabled,

            RasterizerState = new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontFaceCounterClockwise = true,
                ScissorTestEnable = false,
                DepthClipEnable = false,
            },

            DepthStencilState = new DepthStencilStateDescription
            {
                DepthBufferEnable = false,
                StencilEnable = true,
                StencilMask = 0xff,
                StencilWriteMask = 0xff,
                
                FrontFace = new DepthStencilStencilOpDescription
                {
                    StencilFunction = CompareFunction.Always,
                    StencilFail = StencilOperation.Keep,
                    StencilDepthBufferFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Increment,
                },

                BackFace = new DepthStencilStencilOpDescription
                {
                    StencilFunction = CompareFunction.Always,
                    StencilFail = StencilOperation.Keep,
                    StencilDepthBufferFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Decrement,
                },
            },

            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = _nanoVertLayout.CreateInputElements(),

            EffectBytecode = _nanoShader.Effect.Bytecode,
            RootSignature = _nanoShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        return PipelineState.New(GraphicsDevice, ref fillStencilPipeline);
    }

    PipelineState CreateFillPipeline()
    {
        var fillPipeline = new PipelineStateDescription()
        {
            BlendState = BlendStates.AlphaBlend,

            RasterizerState = new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontFaceCounterClockwise = true,
                ScissorTestEnable = false,
                DepthClipEnable = false,
            },

            DepthStencilState = new DepthStencilStateDescription
            {
                DepthBufferEnable = false,
                StencilEnable = true,
                StencilMask = 0xff,
                StencilWriteMask = 0xff,

                FrontFace = new DepthStencilStencilOpDescription
                {
                    StencilFunction = CompareFunction.NotEqual,
                    StencilFail = StencilOperation.Zero,
                    StencilDepthBufferFail = StencilOperation.Zero,
                    StencilPass = StencilOperation.Zero,
                },

                BackFace = new DepthStencilStencilOpDescription
                {
                    StencilFunction = CompareFunction.NotEqual,
                    StencilFail = StencilOperation.Zero,
                    StencilDepthBufferFail = StencilOperation.Zero,
                    StencilPass = StencilOperation.Zero,
                },
            },

            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = _nanoVertLayout.CreateInputElements(),

            EffectBytecode = _nanoShader.Effect.Bytecode,
            RootSignature = _nanoShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        return PipelineState.New(GraphicsDevice, ref fillPipeline);
    }

    #endregion

    private void Window_ClientSizeChanged(object sender, EventArgs e)
    {
        // TODO: handle window resize, when needed
    }

    public override void Update(GameTime gameTime)
    {
        _demo.Update((float)gameTime.Elapsed.TotalSeconds);
    }

    public override void Draw(GameTime gameTime)
    {
        _nanoContext.BeginFrame();
        _demo.Draw(_nanoContext);
        _nanoContext.EndFrame();
    }

    protected override void Destroy()
    {
        _demo?.Dispose();
        _nanoContext?.Dispose();
    }

    #region INvgRenderer

    public void Render()
    {
        DoRender();
    }

    void DoRender()
    {
        // view proj (top-left origin)
        var surfaceSize = Game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoOffCenterRH(0, surfaceSize.Width, surfaceSize.Height, 0, -1, 1);

        // Do not clear color: let Stride scene show through. Only clear stencil (we use it)
        if (_commandList.DepthStencilBuffer != null)
        {
            //_commandList.Clear(_commandList.DepthStencilBuffer, DepthStencilClearOptions.Stencil);
        }

        UpdateIndexBuffer(DrawCache.Indexes);
        UpdateVertexBuffer(DrawCache.Vertices);

        _commandList.SetVertexBuffer(0, _vertexBinding.Buffer, 0, _nanoVertLayout.VertexStride);
        _commandList.SetIndexBuffer(_indexBinding.Buffer, 0, false);

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
                var newUniform = uniforms[drawCommand.UniformOffset];

                // set uniform data from NanoUI for the cbuffer object
                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorMat, (Matrix)newUniform.ScissorMat);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.paintMat, (Matrix)newUniform.PaintMat);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.innerCol, (Vector4)newUniform.InnerCol);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.outerCol, (Vector4)newUniform.OuterCol);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorScale, (Vector2)newUniform.ScissorScale);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorExt, (Vector2)newUniform.ScissorExt);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.extent, (Vector2)newUniform.Extent);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.radius, newUniform.Radius);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.feather, newUniform.Feather);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.actionType, newUniform.ActionType);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.fontSize, newUniform.FontSize);
            }

            // pipeline
            if (previousDrawCommandType != drawCommand.DrawCommandType)
            {
                previousDrawCommandType = drawCommand.DrawCommandType;

                // must set new pipeline
                _pipelines.TryGetValue(drawCommand.DrawCommandType, out var pipeline);
                _commandList.SetPipelineState(pipeline);

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

                Texture2D textureResource;
                if (!_textures.TryGetValue(drawCommand.Texture, out textureResource))
                    textureResource = _textures[-1]; // fallback to white

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

    void UpdateIndexBuffer(ReadOnlySpan<ushort> indices)
    {
        uint totalIBOSize = (uint)(indices.Length * sizeof(ushort));
        if (totalIBOSize > _indexBinding.Buffer.SizeInBytes)
        {
            var is32Bits = false;
            var indexBuffer = Stride.Graphics.Buffer.Index.New(GraphicsDevice, (int)(totalIBOSize * 1.5f));
            _indexBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        }

        _indexBinding.Buffer.SetData(_commandList, indices);
    }

    void UpdateVertexBuffer(ReadOnlySpan<Vertex> vertices)
    {
        uint totalVBOSize = (uint)(vertices.Length * Vertex.SizeInBytes);
        if (totalVBOSize > _vertexBinding.Buffer.SizeInBytes)
        {
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(GraphicsDevice, (int)(totalVBOSize * 1.5f));
            _vertexBinding = new VertexBufferBinding(vertexBuffer, _nanoVertLayout, 0);
        }

        _vertexBinding.Buffer.SetData(_commandList, vertices);
    }

    static PixelFormat MapTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R => PixelFormat.R8_UNorm,
            // Stride might not expose packed RG/RGB formats; map to RGBA for safety
            TextureFormat.RG => PixelFormat.R8G8B8A8_UNorm,
            TextureFormat.RGB => PixelFormat.R8G8B8A8_UNorm,
            TextureFormat.RGBA => PixelFormat.R8G8B8A8_UNorm,
            _ => PixelFormat.R8G8B8A8_UNorm,
        };
    }

    public int CreateTexture(string path, NanoUI.Common.TextureFlags textureFlags = 0)
    {
        Texture2D textureData = null;

        // Try load from content first (asset might already exist)
        try
        {
            textureData = Content.Load<Texture2D>(path);
        }
        catch
        {
            // If not in content, try to import from file system
            if (File.Exists(path))
            {
                using (Stream stream = File.OpenRead(path))
                {
                    var localTexture = Image.Load(stream);
                    ((ContentManager)Content).Save(path, localTexture);
                }
                textureData = Content.Load<Texture2D>(path);
            }
            else
            {
                return -1; // invalid texture id
            }
        }

        int texture = CreateTexture(new TextureDesc((uint)textureData.Width, (uint)textureData.Height));

        UpdateTexture(texture, textureData.GetData<byte>(_graphicsContext.CommandList));

        return texture;
    }

    public int CreateTexture(TextureDesc description)
    {
        var pixelFormat = MapTextureFormat(description.Format);

        var texture = Texture2D.New2D(GraphicsDevice, 
            (int)description.Width,
            (int)description.Height, 
            pixelFormat, 
            usage: GraphicsResourceUsage.Default);

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
            tex.SetData(_graphicsContext.CommandList, data.ToArray());
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

        var pixelFormat = MapTextureFormat(description.Format);
        var newTexture = Texture2D.New2D(
            GraphicsDevice,
            (int)description.Width,
            (int)description.Height,
            pixelFormat,
            usage: GraphicsResourceUsage.Default);

        _textures[texture] = newTexture;

        return true;
    }

    public bool UpdateTextureRegion(int texture, System.Numerics.Vector4 regionRect, ReadOnlySpan<byte> allData)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            // Update full texture; regionRect ignored as suggested
            tex.SetData(_graphicsContext.CommandList, allData.ToArray());
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
