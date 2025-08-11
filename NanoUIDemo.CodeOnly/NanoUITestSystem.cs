using Hexa.NET.ImGui;
using Stride.CommunityToolkit.ImGui;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NanoUIDemo.CodeOnly;
internal class NanoUITestSystem : GameSystemBase, IService
{

    public readonly ImGuiContextPtr ImGuiContext;

    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            //CreateFontTexture();
        }
    }
    private float _scale = 1;

    const int INITIAL_VERTEX_BUFFER_SIZE = 128;
    const int INITIAL_INDEX_BUFFER_SIZE = 128;

    private ImGuiIOPtr _io;
    private ImGuiPlatformIOPtr _platform;

    // dependencies
    readonly InputManager input;
    readonly GraphicsDevice device;
    readonly GraphicsDeviceManager deviceManager;
    readonly GraphicsContext context;
    readonly EffectSystem effectSystem;
    readonly DebugTextSystem debug;
    CommandList commandList;

    // device objects
    PipelineState imPipeline;
    VertexDeclaration imVertLayout;
    VertexBufferBinding vertexBinding;
    IndexBufferBinding indexBinding;
    EffectInstance imShader;
    Texture fontTexture;

    private Dictionary<Keys, ImGuiKey> _keys = [];

    private bool _isFirstFrame = true;

    public NanoUITestSystem([NotNull] IServiceRegistry registry, [NotNull] GraphicsDeviceManager graphicsDeviceManager, InputManager inputManager = null) : base(registry)
    {
        input = inputManager ?? Services.GetService<InputManager>();
        Debug.Assert(input != null, "ImGuiSystem: InputManager must be available!");

        deviceManager = graphicsDeviceManager;
        Debug.Assert(deviceManager != null, "ImGuiSystem: GraphicsDeviceManager must be available!");

        device = deviceManager.GraphicsDevice;
        Debug.Assert(device != null, "ImGuiSystem: GraphicsDevice must be available!");

        context = Services.GetService<GraphicsContext>();
        Debug.Assert(context != null, "ImGuiSystem: GraphicsContext must be available!");

        effectSystem = Services.GetService<EffectSystem>();
        Debug.Assert(effectSystem != null, "ImGuiSystem: EffectSystem must be available!");

        //ImGuiContext = CreateContext();
        //SetCurrentContext(ImGuiContext);
        //
        //_io = GetIO();
        //_platform = GetPlatformIO();

        // SETTO
        //SetupInput();

        // vbos etc
        CreateDeviceObjects();

        // font stuff
        //CreateFontTexture();

        Enabled = true; // Force Update functions to be run
        Visible = true; // Force Draw related functions to be run
        UpdateOrder = 1; // Update should occur after Stride's InputManager

        // Include this new instance into our services and systems so that stride fires our functions automatically
        Services.AddService(this);
        Game.GameSystems.Add(this);
    }

    public static IService NewInstance(IServiceRegistry services)
    {
        var game = services.GetService<IGame>() as Game;

        return new NanoUITestSystem(services, game.GraphicsDeviceManager);
    }

    protected override void Destroy()
    {
        //DestroyContext(ImGuiContext);
        base.Destroy();
    }

    [FixedAddressValueType]
    static SetClipboardDelegate setClipboardFn;

    [FixedAddressValueType]
    static GetClipboardDelegate getClipboardFn;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SetClipboardDelegate(IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr GetClipboardDelegate();

    void CreateDeviceObjects()
    {
        // set up a commandlist
        commandList = context.CommandList;

        // compile de shader
        imShader = new EffectInstance(effectSystem.LoadEffect("NanoUIShader").WaitForResult());
        imShader.UpdateEffect(device);

        var layout = new VertexDeclaration(
            VertexElement.Position<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>(),
            VertexElement.Color(PixelFormat.R8G8B8A8_UNorm)
        );

        imVertLayout = layout;

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
            InputElements = imVertLayout.CreateInputElements(),
            DepthStencilState = DepthStencilStates.Default,

            EffectBytecode = imShader.Effect.Bytecode,
            RootSignature = imShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        // finally set up the pipeline
        var pipelineState = PipelineState.New(device, ref pipeline);
        imPipeline = pipelineState;

        var is32Bits = false;
        var indexBuffer = Stride.Graphics.Buffer.Index.New(device, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
        var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        indexBinding = indexBufferBinding;

        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(device, INITIAL_VERTEX_BUFFER_SIZE * imVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic);
        var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
        vertexBinding = vertexBufferBinding;
    }

    public override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.Elapsed.TotalSeconds;
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            deltaTime = 1 / 60f;
        }
        var surfaceSize = Game.Window.ClientBounds;
        _io.DisplaySize = new System.Numerics.Vector2(surfaceSize.Width, surfaceSize.Height);
        _io.DeltaTime = deltaTime;

        if (input.HasMouse == false || input.IsMousePositionLocked == false)
        {
            var mousePos = input.AbsoluteMousePosition;
            _io.MousePos = new System.Numerics.Vector2(mousePos.X, mousePos.Y);

            if (_io.WantTextInput)
            {
                input.TextInput.EnabledTextInput();
            }
            else
            {
                input.TextInput.DisableTextInput();
            }

            // handle input events
            foreach (InputEvent ev in input.Events)
            {
                switch (ev)
                {
                    case TextInputEvent tev:
                        if (tev.Text == "\t") continue;
                        _io.AddInputCharactersUTF8(tev.Text);
                        break;
                    case KeyEvent kev:
                        if (_keys.TryGetValue(kev.Key, out var imGuiKey))
                            _io.AddKeyEvent(imGuiKey, input.IsKeyDown(kev.Key));
                        break;
                    case MouseWheelEvent mw:
                        _io.MouseWheel += mw.WheelDelta;
                        break;
                }
            }

            var mouseDown = _io.MouseDown;
            mouseDown[0] = input.IsMouseButtonDown(MouseButton.Left);
            mouseDown[1] = input.IsMouseButtonDown(MouseButton.Right);
            mouseDown[2] = input.IsMouseButtonDown(MouseButton.Middle);

            _io.KeyAlt = input.IsKeyDown(Keys.LeftAlt) || input.IsKeyDown(Keys.RightAlt);
            _io.KeyShift = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
            _io.KeyCtrl = input.IsKeyDown(Keys.LeftCtrl) || input.IsKeyDown(Keys.RightCtrl);
            _io.KeySuper = input.IsKeyDown(Keys.LeftWin) || input.IsKeyDown(Keys.RightWin);
        }
        Hexa.NET.ImGui.ImGui.NewFrame();
    }

    public override void EndDraw()
    {
        //Hexa.NET.ImGui.ImGui.Render();
        RenderDrawLists(Hexa.NET.ImGui.ImGui.GetDrawData());
        //ImGuiExtension.ClearTextures();
    }

    void CheckBuffers(ImDrawDataPtr drawData)
    {
        uint totalVBOSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBOSize > vertexBinding.Buffer.SizeInBytes)
        {
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(device, (int)(totalVBOSize * 1.5f));
            vertexBinding = new VertexBufferBinding(vertexBuffer, imVertLayout, 0);
        }

        uint totalIBOSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if (totalIBOSize > indexBinding.Buffer.SizeInBytes)
        {
            var is32Bits = false;
            var indexBuffer = Stride.Graphics.Buffer.Index.New(device, (int)(totalIBOSize * 1.5f));
            indexBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        }
    }

    //unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    //{
    //    // copy de dators
    //    int vtxOffsetBytes = 0;
    //    int idxOffsetBytes = 0;
    //
    //    for (int n = 0; n < drawData.CmdListsCount; n++)
    //    {
    //        ImDrawListPtr cmdList = drawData.CmdLists[n];
    //        vertexBinding.Buffer.SetData(commandList, new DataPointer(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()), vtxOffsetBytes);
    //        indexBinding.Buffer.SetData(commandList, new DataPointer(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size * sizeof(ushort)), idxOffsetBytes);
    //        vtxOffsetBytes += cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
    //        idxOffsetBytes += cmdList.IdxBuffer.Size * sizeof(ushort);
    //    }
    //}

    void RenderDrawLists(ImDrawDataPtr drawData)
    {
        // view proj
        var surfaceSize = Game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoRH(surfaceSize.Width, -surfaceSize.Height, -1, 1);

        CheckBuffers(drawData); // potentially resize buffers first if needed
        //UpdateBuffers(drawData); // updeet em now

        // set pipeline stuff
        var is32Bits = false;
        commandList.SetPipelineState(imPipeline);
        commandList.SetVertexBuffer(0, vertexBinding.Buffer, 0, Unsafe.SizeOf<ImDrawVert>());
        commandList.SetIndexBuffer(indexBinding.Buffer, 0, is32Bits);
        imShader.Parameters.Set(ImGuiShaderKeys.tex, fontTexture);

        int vtxOffset = 0;
        int idxOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                ImDrawCmd cmd = cmdList.CmdBuffer[i];

                // Bind the appropriate texture based on cmd.TextureId
                if (cmd.TextureId != IntPtr.Zero)
                {
                    // Convert the IntPtr to the correct texture resource
                    //if (ImGuiExtension.TryGetTexture(cmd.TextureId.Handle, out var texture))
                    //{
                    //    imShader.Parameters.Set(ImGuiShaderKeys.tex, texture);
                    //}
                }
                else
                {
                    // If no specific texture, use the default font texture
                    imShader.Parameters.Set(ImGuiShaderKeys.tex, fontTexture);
                }

                // Set the scissor rectangle for clipping
                commandList.SetScissorRectangle(
                    new Rectangle(
                        (int)cmd.ClipRect.X,
                        (int)cmd.ClipRect.Y,
                        (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                        (int)(cmd.ClipRect.W - cmd.ClipRect.Y)
                    )
                );

                // Set the projection matrix and apply shader
                imShader.Parameters.Set(ImGuiShaderKeys.proj, ref projMatrix);
                imShader.Apply(context);

                // Draw the indexed vertices
                commandList.DrawIndexed((int)cmd.ElemCount, idxOffset, vtxOffset);

                idxOffset += (int)cmd.ElemCount;

            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
    }
}
