using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// the cornell box.
/// </summary>
public class CornellBox : RayTracingTutorial
{
  /// <summary>
  /// the focus camera shader parameters
  /// </summary>
  private static class FocusCameraShaderParams
  {
    public static readonly int _FocusCameraLeftBottomCorner = Shader.PropertyToID("_FocusCameraLeftBottomCorner");
    public static readonly int _FocusCameraRight = Shader.PropertyToID("_FocusCameraRight");
    public static readonly int _FocusCameraUp = Shader.PropertyToID("_FocusCameraUp");
    public static readonly int _FocusCameraSize = Shader.PropertyToID("_FocusCameraSize");
    public static readonly int _FocusCameraHalfAperture = Shader.PropertyToID("_FocusCameraHalfAperture");
  }

  private static class HistoryShaderParams
  {
    public static readonly int _HistoryColor = Shader.PropertyToID("_HistoryColor");
    public static readonly int _HistoryNormal = Shader.PropertyToID("_HistoryNormal");
    public static readonly int _HistoryCount = Shader.PropertyToID("_HistoryCount");
    public static readonly int _HistoryBaseColor = Shader.PropertyToID("_HistoryBaseColor");
  }

  private static class NormalShaderParams
  {
    public static readonly int _NormalTarget = Shader.PropertyToID("_NormalTarget");
  }

  private static class CountShaderParams
  {
    public static readonly int _CountTarget = Shader.PropertyToID("_CountTarget");
  }

  private static class BaseColorShaderParams
  {
    public static readonly int _BaseColorTarget = Shader.PropertyToID("_BaseColorTarget");
  }

  private readonly int _PRNGStatesShaderId = Shader.PropertyToID("_PRNGStates");

  /// <summary>
  /// the frame index.
  /// </summary>
  private int _frameIndex = 0;

  private readonly int _frameIndexShaderId = Shader.PropertyToID("_FrameIndex");

  /// <summary>
  /// history targets per camera.
  /// </summary>
  private readonly System.Collections.Generic.Dictionary<int, RTHandle> _historyTargets = new System.Collections.Generic.Dictionary<int, RTHandle>();
  private readonly System.Collections.Generic.Dictionary<int, RTHandle> _historyNormals = new System.Collections.Generic.Dictionary<int, RTHandle>();
  private readonly System.Collections.Generic.Dictionary<int, RTHandle> _historyCounts = new System.Collections.Generic.Dictionary<int, RTHandle>();
  private readonly System.Collections.Generic.Dictionary<int, RTHandle> _historyBaseColors = new System.Collections.Generic.Dictionary<int, RTHandle>();

  private RTHandle RequireHistoryTarget(Camera camera)
  {
    var id = camera.GetInstanceID();
    if (_historyTargets.TryGetValue(id, out var history))
      return history;

    history = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R32G32B32A32_SFloat, // 或 graphicsFormat: ...
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"HistoryTarget_{camera.name}"
    );
        _historyTargets.Add(id, history);
    return history;
  }

  private RTHandle RequireHistoryNormal(Camera camera)
  {
    var id = camera.GetInstanceID();
    if (_historyNormals.TryGetValue(id, out var historyNormal))
      return historyNormal;

    historyNormal = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"HistoryNormal_{camera.name}"
    );
    _historyNormals.Add(id, historyNormal);
    return historyNormal;
  }

  private RTHandle RequireHistoryCount(Camera camera)
  {
    var id = camera.GetInstanceID();
    if (_historyCounts.TryGetValue(id, out var historyCount))
      return historyCount;

    historyCount = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"HistoryCount_{camera.name}"
    );
    _historyCounts.Add(id, historyCount);
    return historyCount;
  }

  private RTHandle RequireHistoryBaseColor(Camera camera)
  {
    var id = camera.GetInstanceID();
    if (_historyBaseColors.TryGetValue(id, out var historyBaseColor))
      return historyBaseColor;

    historyBaseColor = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"HistoryBaseColor_{camera.name}"
    );
    _historyBaseColors.Add(id, historyBaseColor);
    return historyBaseColor;
  }

  private RTHandle _normalTarget;
  private RTHandle _countTarget;
  private RTHandle _baseColorTarget;
  private RTHandle RequireNormalTarget(Camera camera)
  {
    if (_normalTarget != null && (_normalTarget.rt.width == camera.pixelWidth && _normalTarget.rt.height == camera.pixelHeight))
      return _normalTarget;

    if (_normalTarget != null) RTHandles.Release(_normalTarget);
    _normalTarget = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"NormalTarget_{camera.name}"
    );
    return _normalTarget;
  }

  private RTHandle RequireCountTarget(Camera camera)
  {
    if (_countTarget != null && (_countTarget.rt.width == camera.pixelWidth && _countTarget.rt.height == camera.pixelHeight))
      return _countTarget;

    if (_countTarget != null) RTHandles.Release(_countTarget);
    _countTarget = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"CountTarget_{camera.name}"
    );
    return _countTarget;
  }

  private RTHandle RequireBaseColorTarget(Camera camera)
  {
    if (_baseColorTarget != null && (_baseColorTarget.rt.width == camera.pixelWidth && _baseColorTarget.rt.height == camera.pixelHeight))
      return _baseColorTarget;

    if (_baseColorTarget != null) RTHandles.Release(_baseColorTarget);
    _baseColorTarget = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
      filterMode: FilterMode.Point,
      wrapMode: TextureWrapMode.Clamp,
      dimension: TextureDimension.Tex2D,
      enableRandomWrite: true,
      useMipMap: false,
      autoGenerateMips: false,
      isShadowMap: false,
      anisoLevel: 1,
      mipMapBias: 0f,
      msaaSamples: MSAASamples.None,
      bindTextureMS: false,
      useDynamicScale: false,
      memoryless: RenderTextureMemoryless.None,
      name: $"BaseColorTarget_{camera.name}"
    );
    return _baseColorTarget;
  }

  /// <summary>
  /// constructor.
  /// </summary>
  /// <param name="asset">the tutorial asset.</param>
  public CornellBox(RayTracingTutorialAsset asset) : base(asset)
  {
  }

  /// <summary>
  /// render.
  /// </summary>
  /// <param name="context">the render context.</param>
  /// <param name="camera">the camera.</param>
  public override void Render(ScriptableRenderContext context, Camera camera)
  {
    base.Render(context, camera);
    var focusCamera = camera.GetComponent<FocusCamera>();
    if (null == focusCamera)
      return;

    var outputTarget = RequireOutputTarget(camera);
    var outputTargetSize = RequireOutputTargetSize(camera);
    var historyTarget = RequireHistoryTarget(camera);
    var normalTarget = RequireNormalTarget(camera);
    var historyNormal = RequireHistoryNormal(camera);
    var countTarget = RequireCountTarget(camera);
    var historyCount = RequireHistoryCount(camera);
    var baseColorTarget = RequireBaseColorTarget(camera);
    var historyBaseColor = RequireHistoryBaseColor(camera);

    var accelerationStructure = _pipeline.RequestAccelerationStructure();
    var PRNGStates = _pipeline.RequirePRNGStates(camera);

    var cmd = CommandBufferPool.Get(typeof(MotionBlur).Name);
    try
    {
      
      {
        using (new ProfilingSample(cmd, "RayTracing"))
        {
          cmd.SetRayTracingVectorParam(_shader, FocusCameraShaderParams._FocusCameraLeftBottomCorner, focusCamera.leftBottomCorner);
          cmd.SetRayTracingVectorParam(_shader, FocusCameraShaderParams._FocusCameraRight, focusCamera.transform.right);
          cmd.SetRayTracingVectorParam(_shader, FocusCameraShaderParams._FocusCameraUp, focusCamera.transform.up);
          cmd.SetRayTracingVectorParam(_shader, FocusCameraShaderParams._FocusCameraSize, focusCamera.size);
          cmd.SetRayTracingFloatParam(_shader, FocusCameraShaderParams._FocusCameraHalfAperture, focusCamera.aperture * 0.5f);

          cmd.SetRayTracingShaderPass(_shader, "RayTracing");
          cmd.SetRayTracingAccelerationStructure(_shader, _pipeline.accelerationStructureShaderId,
            accelerationStructure);
          cmd.SetRayTracingIntParam(_shader, _frameIndexShaderId, _frameIndex);
          cmd.SetRayTracingBufferParam(_shader, _PRNGStatesShaderId, PRNGStates);
          cmd.SetRayTracingTextureParam(_shader, _outputTargetShaderId, outputTarget);
          cmd.SetRayTracingVectorParam(_shader, _outputTargetSizeShaderId, outputTargetSize);
          cmd.SetRayTracingTextureParam(_shader, HistoryShaderParams._HistoryColor, historyTarget);

          // bind normal & count targets
          cmd.SetRayTracingTextureParam(_shader, NormalShaderParams._NormalTarget, normalTarget);
          cmd.SetRayTracingTextureParam(_shader, HistoryShaderParams._HistoryNormal, historyNormal);
          cmd.SetRayTracingTextureParam(_shader, CountShaderParams._CountTarget, countTarget);
          cmd.SetRayTracingTextureParam(_shader, HistoryShaderParams._HistoryCount, historyCount);

          // bind base color targets
          cmd.SetRayTracingTextureParam(_shader, BaseColorShaderParams._BaseColorTarget, baseColorTarget);
          cmd.SetRayTracingTextureParam(_shader, HistoryShaderParams._HistoryBaseColor, historyBaseColor);

          cmd.DispatchRays(_shader, "CornellBoxGenShader", (uint) outputTarget.rt.width,
            (uint) outputTarget.rt.height, 1, camera);
        }

        context.ExecuteCommandBuffer(cmd);
        if (camera.cameraType == CameraType.Game)
          _frameIndex++;
        cmd.Clear();
      }

      using (new ProfilingSample(cmd, "FinalBlit"))
      {
        cmd.Blit(outputTarget, BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
        // copy current output to history
        cmd.Blit(outputTarget, historyTarget);
        // copy current normal/count to history
        cmd.Blit(normalTarget, historyNormal);
        cmd.Blit(countTarget, historyCount);
        // copy current base color to history
        cmd.Blit(baseColorTarget, historyBaseColor);
      }

      context.ExecuteCommandBuffer(cmd);
    }
    finally
    {
      CommandBufferPool.Release(cmd);
    }
  }
}