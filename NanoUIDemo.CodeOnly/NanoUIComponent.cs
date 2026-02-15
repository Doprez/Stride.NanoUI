using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;

namespace NanoUIDemo.CodeOnly;

/// <summary>
/// Entity component for NanoUI views, analogous to Stride.UI's <c>UIComponent</c>.
/// Attach to an entity to render a NanoUI page.
/// <para>
///   <b>Fullscreen mode</b> (<see cref="IsFullScreen"/> = <c>true</c>, default):
///   The UI covers the entire screen as an overlay, ignoring the entity's transform.
/// </para>
/// <para>
///   <b>World-space mode</b> (<see cref="IsFullScreen"/> = <c>false</c>):
///   The UI is rendered as a panel positioned at the entity's transform in the 3D scene.
///   Use <see cref="Size"/> to control the world-unit dimensions and
///   <see cref="IsBillboard"/> to have it auto-face the camera.
/// </para>
/// </summary>
[DataContract("NanoUIComponent")]
[ComponentCategory("UI")]
public class NanoUIComponent : ActivableEntityComponent
{
    /// <summary>
    /// The page containing the NanoUI content to render.
    /// </summary>
    [DataMember(10)]
    public NanoUIPage? Page { get; set; }

    /// <summary>
    /// When <c>true</c> (default) the UI is rendered as a fullscreen overlay.
    /// When <c>false</c> the UI is placed in the 3D scene at the entity's transform.
    /// </summary>
    [DataMember(20)]
    [DefaultValue(true)]
    public bool IsFullScreen { get; set; } = true;

    /// <summary>
    /// Virtual resolution of the NanoUI surface in pixels.
    /// Defaults to <c>(1280, 720)</c>.
    /// </summary>
    [DataMember(30)]
    public Vector2 Resolution { get; set; } = new(1280, 720);

    /// <summary>
    /// Size of the UI panel in world units. Only used when <see cref="IsFullScreen"/> is <c>false</c>.
    /// Defaults to <c>(1.28, 0.72)</c> matching a 1000:1 pixel-to-world ratio.
    /// </summary>
    [DataMember(35)]
    public Vector2 Size { get; set; } = new(1.28f, 0.72f);

    /// <summary>
    /// When <c>true</c> (default) and not fullscreen, the UI panel automatically
    /// rotates to face the camera (billboard behaviour).
    /// </summary>
    [DataMember(50)]
    [DefaultValue(true)]
    public bool IsBillboard { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, any UI content that falls outside the panel's bounds
    /// (defined by <see cref="Resolution"/> / <see cref="Size"/>) is clipped
    /// using a GPU scissor rectangle. Only meaningful for world-space panels
    /// (<see cref="IsFullScreen"/> = <c>false</c>).
    /// Defaults to <c>false</c>.
    /// </summary>
    [DataMember(55)]
    [DefaultValue(false)]
    public bool ClipToBounds { get; set; } = false;

    // ──────────────────────────────────────────────
    //  Helpers shared by NanoUISceneRenderer + NanoUISystem
    // ──────────────────────────────────────────────

    /// <summary>
    /// Returns the world matrix to use for rendering / hit-testing.
    /// For billboard components the rotation is replaced so the panel faces the camera.
    /// </summary>
    internal Matrix GetEffectiveWorldMatrix(Matrix cameraView)
    {
        var world = Entity.Transform.WorldMatrix;
        return (!IsFullScreen && IsBillboard)
            ? BuildBillboardMatrix(world.TranslationVector, cameraView)
            : world;
    }

    /// <summary>
    /// Builds a world matrix that faces the camera while keeping the entity's position.
    /// </summary>
    private static Matrix BuildBillboardMatrix(Vector3 entityPos, Matrix cameraView)
    {
        Matrix.Invert(ref cameraView, out var cameraWorld);
        var cameraPos = cameraWorld.TranslationVector;

        var toCamera = cameraPos - entityPos;
        if (toCamera.LengthSquared() < 1e-6f)
            toCamera = Vector3.UnitZ;
        else
            toCamera = Vector3.Normalize(toCamera);

        var worldUp = Vector3.UnitY;
        var right = Vector3.Cross(worldUp, toCamera);
        if (right.LengthSquared() < 1e-6f)
            right = Vector3.UnitX;
        else
            right = Vector3.Normalize(right);
        var up = Vector3.Cross(toCamera, right);

        return new Matrix(
            right.X,     right.Y,     right.Z,     0,
            up.X,        up.Y,        up.Z,        0,
            toCamera.X,  toCamera.Y,  toCamera.Z,  0,
            entityPos.X, entityPos.Y, entityPos.Z,  1);
    }

    /// <summary>
    /// Maps NanoVG pixel coordinates (0..resX, 0..resY) to panel-local 3D space.
    /// The resulting matrix centres the quad and flips Y so +Y is up.
    /// </summary>
    internal Matrix GetPixelToLocalMatrix()
    {
        return Matrix.Scaling(Size.X / Resolution.X, -Size.Y / Resolution.Y, 1f)
             * Matrix.Translation(-Size.X / 2f, Size.Y / 2f, 0f);
    }

    /// <summary>
    /// Performs a ray-plane intersection to convert a screen-space mouse position
    /// (normalised 0..1, origin top-left) into NanoVG pixel coordinates on this panel.
    /// Returns <c>false</c> if the ray misses the panel.
    /// </summary>
    internal bool TryScreenToPanel(
        Vector2 normScreenPos,
        Matrix cameraViewProjection,
        Matrix cameraView,
        out System.Numerics.Vector2 panelPixel)
    {
        panelPixel = default;

        // NDC: X from -1 (left) to +1 (right), Y from +1 (top) to -1 (bottom)
        float ndcX = normScreenPos.X * 2f - 1f;
        float ndcY = -(normScreenPos.Y * 2f - 1f);

        // Unproject near / far points to world space
        Matrix.Invert(ref cameraViewProjection, out var invVP);
        var nearPt = new Vector3(ndcX, ndcY, 0f);
        var farPt  = new Vector3(ndcX, ndcY, 1f);
        Vector3.TransformCoordinate(ref nearPt, ref invVP, out var worldNear);
        Vector3.TransformCoordinate(ref farPt,  ref invVP, out var worldFar);

        var rayDir = worldFar - worldNear;
        if (rayDir.LengthSquared() < 1e-12f) return false;
        rayDir = Vector3.Normalize(rayDir);

        // Panel plane
        var worldMatrix = GetEffectiveWorldMatrix(cameraView);
        var planeNormal = new Vector3(worldMatrix.M31, worldMatrix.M32, worldMatrix.M33);
        var planePoint  = worldMatrix.TranslationVector;

        float denom = Vector3.Dot(planeNormal, rayDir);
        if (MathF.Abs(denom) < 1e-6f) return false;

        float t = Vector3.Dot(planePoint - worldNear, planeNormal) / denom;
        if (t < 0) return false;

        var hitWorld = worldNear + rayDir * t;

        // To local space
        Matrix.Invert(ref worldMatrix, out var invWorld);
        Vector3.TransformCoordinate(ref hitWorld, ref invWorld, out var localHit);

        // Local → NanoVG pixel (inverse of GetPixelToLocalMatrix)
        float px = (localHit.X + Size.X / 2f) / Size.X * Resolution.X;
        float py = (-localHit.Y + Size.Y / 2f) / Size.Y * Resolution.Y;

        if (px < 0 || px > Resolution.X || py < 0 || py > Resolution.Y)
            return false;

        panelPixel = new System.Numerics.Vector2(px, py);
        return true;
    }
}
