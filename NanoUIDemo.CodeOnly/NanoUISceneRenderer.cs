using NanoUI;
using NanoUI.Common;
using NanoUI.Nvg;
using NanoUI.Rendering;
using NanoUI.Rendering.Data;
using NanoUIDemos;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Texture2D = Stride.Graphics.Texture;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// A <see cref="SceneRendererBase"/> that owns all NanoUI GPU resources and
/// renders every <see cref="NanoUIComponent"/> found in the scene.
/// This mirrors Stride.UI's <c>UIRenderFeature</c>: the companion
/// <see cref="NanoUISystem"/> handles only input / update (like <c>UISystem</c>).
/// </summary>
public class NanoUISceneRenderer : SceneRendererBase, INvgRenderer
{
    const int INITIAL_VERTEX_BUFFER_SIZE = 128;
    const int INITIAL_INDEX_BUFFER_SIZE = 128;

    // GPU resources
    private VertexDeclaration _nanoVertLayout = null!;
    private VertexBufferBinding _vertexBinding;
    private IndexBufferBinding _indexBinding;
    private EffectInstance _nanoShader = null!;
    private readonly Dictionary<DrawCommandType, PipelineState> _pipelines = new();

    // Texture management (INvgRenderer)
    private readonly Dictionary<int, Texture2D> _textures = [];
    private readonly Dictionary<int, TextureFormat> _textureSourceFormats = [];
    private int _textureCounter = 0;

    // Shared NanoUI context
    private NvgContext _nanoContext = null!;
    private NanoUISystem _nanoUISystem = null!;

    // Transient – set only while DrawCore is executing
    private CommandList? _activeCommandList;
    private GraphicsContext? _activeGraphicsContext;
    private NanoUIComponent? _activeComponent;

    // Camera matrices – extracted once per frame for world-space rendering
    private Matrix _cameraView;
    private Matrix _cameraViewProjection;
    private bool _hasCameraMatrices;

    private Logger _log => GlobalLogger.GetLogger(nameof(NanoUISceneRenderer));

    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Get or create the companion game system (like UIRenderFeature → UISystem)
        _nanoUISystem = Services.GetOrCreate<NanoUISystem>();

        CreateDeviceObjects();

        _nanoContext = new NvgContext(this);
        _nanoUISystem.NanoContext = _nanoContext;
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        _activeCommandList = drawContext.CommandList;
        _activeGraphicsContext = drawContext.GraphicsContext;

        // --- Obtain camera matrices for world-space rendering ---
        _hasCameraMatrices = false;
        var renderView = context.RenderView;
        if (renderView == null && context.RenderSystem.Views.Count > 0)
            renderView = context.RenderSystem.Views[0];

        if (renderView != null)
        {
            _cameraView = renderView.View;
            _cameraViewProjection = renderView.ViewProjection;
            _hasCameraMatrices = true;

            // Share with NanoUISystem so it can do input hit-testing
            _nanoUISystem.CameraView = _cameraView;
            _nanoUISystem.CameraViewProjection = _cameraViewProjection;
            _nanoUISystem.HasCameraMatrices = true;
        }

        try
        {
            // Collect all NanoUIComponents from every entity in every scene
            var components = CollectComponents();
            if (components.Count == 0)
                return;

            var backBufferSize = GraphicsDevice.Presenter.BackBuffer.Size;
            var screenSize = new System.Numerics.Vector2(backBufferSize.Width, backBufferSize.Height);

            foreach (var uiComponent in components)
            {
                if (!uiComponent.Enabled || uiComponent.Page == null)
                    continue;

                // Determine the pixel-size this view should render at
                var renderSize = uiComponent.IsFullScreen
                    ? screenSize
                    : new System.Numerics.Vector2(uiComponent.Resolution.X, uiComponent.Resolution.Y);

                // Ensure the page content is created (lazy init on first frame)
                if (!uiComponent.Page.EnsureContent(_nanoContext, renderSize))
                    continue;

                var content = uiComponent.Page.Content!;

                // Track which component is being rendered (DoRender reads this)
                _activeComponent = uiComponent;

                // Drive one full NanoUI frame for this view
                _nanoContext.BeginFrame();
                content.Draw(_nanoContext);
                _nanoContext.EndFrame(); // triggers INvgRenderer.Render() → DoRender()
            }
        }
        finally
        {
            _activeCommandList = null;
            _activeGraphicsContext = null;
            _activeComponent = null;
        }
    }

    /// <summary>
    /// Walks all scenes and collects every <see cref="NanoUIComponent"/>.
    /// </summary>
    private List<NanoUIComponent> CollectComponents()
    {
        var result = new List<NanoUIComponent>();

        var sceneSystem = Services.GetService<SceneSystem>();
        if (sceneSystem?.SceneInstance == null)
            return result;

        CollectFromScene(sceneSystem.SceneInstance.RootScene, result);
        return result;
    }

    private static void CollectFromScene(Scene? scene, List<NanoUIComponent> result)
    {
        if (scene == null) return;

        foreach (var entity in scene.Entities)
        {
            var comp = entity.Get<NanoUIComponent>();
            if (comp != null)
                result.Add(comp);
        }

        foreach (var child in scene.Children)
        {
            CollectFromScene(child, result);
        }
    }

    #region GPU Resource Creation

    void CreateDeviceObjects()
    {
        // Compile the shader
        _nanoShader = new EffectInstance(EffectSystem.LoadEffect("NanoUIShader").WaitForResult());
        _nanoShader.UpdateEffect(GraphicsDevice);
        _nanoShader.Parameters.Set(NanoUIShaderKeys.TexSampler, GraphicsDevice.SamplerStates.LinearClamp);

        _nanoVertLayout = new VertexDeclaration(
            VertexElement.Position<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>()
        );

        InitPipelineStates();
        InitNullTexture();

        var is32Bits = false;
        var indexBuffer = Stride.Graphics.Buffer.Index.New(
            GraphicsDevice, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
        _indexBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(
            GraphicsDevice, INITIAL_VERTEX_BUFFER_SIZE * _nanoVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic);
        _vertexBinding = new VertexBufferBinding(vertexBuffer, _nanoVertLayout, 0);
    }

    void InitNullTexture()
    {
        byte[] colorBytes = [255, 255, 255, 255];
        var nullTexture = Texture2D.New2D<byte>(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, colorBytes);
        _textures.Add(-1, nullTexture);
    }

    #endregion

    #region Pipeline States

    void InitPipelineStates()
    {
        _pipelines.Add(DrawCommandType.Triangles, CreateModelPipeline());
        _pipelines.Add(DrawCommandType.FillStencil, CreateFillStencilPipeline());
        _pipelines.Add(DrawCommandType.Fill, CreateFillPipeline());
    }

    PipelineState CreateModelPipeline()
    {
        var desc = new PipelineStateDescription()
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
        return PipelineState.New(GraphicsDevice, ref desc);
    }

    PipelineState CreateFillStencilPipeline()
    {
        var desc = new PipelineStateDescription()
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
        };
        return PipelineState.New(GraphicsDevice, ref desc);
    }

    PipelineState CreateFillPipeline()
    {
        var desc = new PipelineStateDescription()
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
        return PipelineState.New(GraphicsDevice, ref desc);
    }

    #endregion

    #region INvgRenderer – Render

    public void Render()
    {
        DoRender();
    }

    void DoRender()
    {
        var commandList = _activeCommandList!;
        var graphicsContext = _activeGraphicsContext!;

        var backBuffer = GraphicsDevice.Presenter.BackBuffer;
        commandList.SetViewport(new Viewport(0, 0, backBuffer.Width, backBuffer.Height));

        // --- Compute projection matrix ---
        Matrix projMatrix;

        if (_activeComponent == null || _activeComponent.IsFullScreen || !_hasCameraMatrices)
        {
            // Fullscreen: orthographic projection (top-left origin)
            var surfaceSize = backBuffer.Size;
            projMatrix = Matrix.OrthoOffCenterRH(0, surfaceSize.Width, surfaceSize.Height, 0, -1, 1);
        }
        else
        {
            // World-space: pixel coords → panel local → world → clip space
            var pixelToLocal = _activeComponent.GetPixelToLocalMatrix();
            var worldMatrix  = _activeComponent.GetEffectiveWorldMatrix(_cameraView);
            projMatrix = pixelToLocal * worldMatrix * _cameraViewProjection;
        }

        UpdateIndexBuffer(DrawCache.Indexes, commandList);
        UpdateVertexBuffer(DrawCache.Vertices, commandList);

        commandList.SetVertexBuffer(0, _vertexBinding.Buffer, 0, _nanoVertLayout.VertexStride);
        commandList.SetIndexBuffer(_indexBinding.Buffer, 0, false);

        DrawCommandType? previousDrawCommandType = null;
        bool updateTextureRS = true;
        int previousTexture = -1;
        int uniformOffset = -1;

        ReadOnlySpan<FragmentUniform> uniforms = DrawCache.Uniforms;

        foreach (var drawCommand in DrawCache.DrawCommands)
        {
            // Uniforms
            if (uniformOffset != drawCommand.UniformOffset)
            {
                uniformOffset = drawCommand.UniformOffset;
                var u = uniforms[drawCommand.UniformOffset];

                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorMat, (Matrix)u.ScissorMat);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.paintMat, (Matrix)u.PaintMat);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.innerCol, u.InnerCol);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.outerCol, u.OuterCol);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorScale, u.ScissorScale);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.scissorExt, u.ScissorExt);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.extent, u.Extent);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.radius, u.Radius);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.feather, u.Feather);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.actionType, u.ActionType);
                _nanoShader.Parameters.Set(NanoUIShaderKeys.fontSize, u.FontSize);
            }

            // Pipeline
            if (previousDrawCommandType != drawCommand.DrawCommandType)
            {
                previousDrawCommandType = drawCommand.DrawCommandType;
                if (!_pipelines.TryGetValue(drawCommand.DrawCommandType, out var pipeline) || pipeline == null)
                {
                    _pipelines.TryGetValue(DrawCommandType.Triangles, out pipeline);
                }
                commandList.SetPipelineState(pipeline);
                updateTextureRS = true;
            }

            // Texture
            if (updateTextureRS || previousTexture != drawCommand.Texture)
            {
                previousTexture = drawCommand.Texture;
                if (!_textures.TryGetValue(drawCommand.Texture, out var textureResource))
                    textureResource = _textures[-1];
                _nanoShader.Parameters.Set(NanoUIShaderKeys.tex, textureResource);
                updateTextureRS = false;
            }

            // Project + draw
            _nanoShader.Parameters.Set(NanoUIShaderKeys.proj, ref projMatrix);
            _nanoShader.Apply(graphicsContext);
            commandList.DrawIndexed(drawCommand.IndexCount, drawCommand.IndexOffset, drawCommand.VertexOffset);
        }
    }

    void UpdateIndexBuffer(ReadOnlySpan<ushort> indices, CommandList cmd)
    {
        uint totalIBOSize = (uint)(indices.Length * sizeof(ushort));
        if (totalIBOSize > _indexBinding.Buffer.SizeInBytes)
        {
            var indexBuffer = Stride.Graphics.Buffer.Index.New(
                GraphicsDevice, (int)(totalIBOSize * 1.5f), GraphicsResourceUsage.Dynamic);
            _indexBinding = new IndexBufferBinding(indexBuffer, false, 0);
        }
        _indexBinding.Buffer.SetData(cmd, indices);
    }

    void UpdateVertexBuffer(ReadOnlySpan<Vertex> vertices, CommandList cmd)
    {
        uint totalVBOSize = (uint)(vertices.Length * Vertex.SizeInBytes);
        if (totalVBOSize > _vertexBinding.Buffer.SizeInBytes)
        {
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(
                GraphicsDevice, (int)(totalVBOSize * 1.5f), GraphicsResourceUsage.Dynamic);
            _vertexBinding = new VertexBufferBinding(vertexBuffer, _nanoVertLayout, 0);
        }
        _vertexBinding.Buffer.SetData(cmd, vertices);
    }

    #endregion

    #region INvgRenderer – Texture Management

    static PixelFormat MapTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R => PixelFormat.R8_UNorm,
            TextureFormat.RG => PixelFormat.R8G8_UNorm,
            TextureFormat.RGBA => PixelFormat.R8G8B8A8_UNorm,
            _ => PixelFormat.R8G8B8A8_UNorm,
        };
    }

    public int CreateTexture(string path, NanoUI.Common.TextureFlags textureFlags = 0)
    {
        if (!File.Exists(path))
            return -1;

        using (Stream stream = File.OpenRead(path))
        {
            var localTexture = Image.Load(stream);
            ((ContentManager)Content).Save(path, localTexture);
        }

        var textureData = Content.Load<Texture2D>(path);

        _textureCounter++;
        _textures.Add(_textureCounter, textureData);
        return _textureCounter;
    }

    public int CreateTexture(TextureDesc description)
    {
        var texture = Texture2D.New2D(GraphicsDevice,
            (int)description.Width,
            (int)description.Height,
            PixelFormat.R8G8B8A8_UNorm,
            usage: GraphicsResourceUsage.Default);

        _textureCounter++;
        _textures.Add(_textureCounter, texture);
        _textureSourceFormats[_textureCounter] = description.Format;
        return _textureCounter;
    }

    public bool UpdateTexture(int texture, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty || !_textures.TryGetValue(texture, out var tex))
            return false;

        bool isR8Source = _textureSourceFormats.TryGetValue(texture, out var srcFormat)
            && srcFormat == TextureFormat.R;

        byte[] textureData;
        if (isR8Source)
        {
            textureData = new byte[data.Length * 4];
            for (int i = 0; i < data.Length; i++)
            {
                byte v = data[i];
                int j = i * 4;
                textureData[j]     = v;
                textureData[j + 1] = v;
                textureData[j + 2] = v;
                textureData[j + 3] = v;
            }
        }
        else
        {
            textureData = data.ToArray();
        }

        tex.SetData(_activeCommandList!, textureData);
        return true;
    }

    public bool DeleteTexture(int texture)
    {
        if (_textures.TryGetValue(texture, out var tex))
        {
            tex?.Dispose();
            _textures.Remove(texture);
            _textureSourceFormats.Remove(texture);
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
            return false;

        tex?.Dispose();

        var newTexture = Texture2D.New2D(
            GraphicsDevice,
            (int)description.Width,
            (int)description.Height,
            PixelFormat.R8G8B8A8_UNorm,
            usage: GraphicsResourceUsage.Default);

        _textures[texture] = newTexture;
        _textureSourceFormats[texture] = description.Format;
        return true;
    }

    #endregion
}
