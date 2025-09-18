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
  private RTHandle _denoisedTarget;
  private RTHandle _temporalTarget;
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

  private RTHandle RequireDenoisedTarget(Camera camera)
  {
    if (_denoisedTarget != null && (_denoisedTarget.rt.width == camera.pixelWidth && _denoisedTarget.rt.height == camera.pixelHeight))
      return _denoisedTarget;

    if (_denoisedTarget != null) RTHandles.Release(_denoisedTarget);
    _denoisedTarget = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
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
      name: $"DenoisedTarget_{camera.name}"
    );
    return _denoisedTarget;
  }

  private RTHandle RequireTemporalTarget(Camera camera)
  {
    if (_temporalTarget != null && (_temporalTarget.rt.width == camera.pixelWidth && _temporalTarget.rt.height == camera.pixelHeight))
      return _temporalTarget;

    if (_temporalTarget != null) RTHandles.Release(_temporalTarget);
    _temporalTarget = RTHandles.Alloc(
      width: camera.pixelWidth,
      height: camera.pixelHeight,
      slices: 1,
      depthBufferBits: DepthBits.None,
      colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
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
      name: $"TemporalTarget_{camera.name}"
    );
    return _temporalTarget;
  }

  /// <summary>
  /// constructor.
  /// </summary>
  /// <param name="asset">the tutorial asset.</param>
  private readonly CornellBoxAsset _assetRef;
  public CornellBox(RayTracingTutorialAsset asset) : base(asset)
  {
    _assetRef = asset as CornellBoxAsset;
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
    var denoisedTarget = RequireDenoisedTarget(camera);
    var temporalTarget = RequireTemporalTarget(camera);

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
      }

      // Spatial Denoise (High-pass bilateral)
      using (new ProfilingSample(cmd, "DenoiseBilateral"))
      {
        var cs = _assetRef != null ? _assetRef.denoiseBilateral : null;
        int kernel = cs.FindKernel("CSMain");
        cs.SetVector("_TexelSize", new Vector2(1.0f / outputTarget.rt.width, 1.0f / outputTarget.rt.height));
        cs.SetFloat("_SigmaSpatial", 1.0f);
        cs.SetFloat("_SigmaColor", 0.2f);
        cs.SetFloat("_SigmaNormal", 0.1f);
        cs.SetFloat("_HighpassStrength", 0.5f);
        cs.SetInt("_ScaleSigmaWithStride", 1); // 保持不同步长下空间核形状相似

        uint tx = 8, ty = 8, tz = 1;
        cs.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);

        // 多趟：固定 5x5 核，步长 stride = 2^i，ping-pong 写入
        RTHandle src = outputTarget;
        RTHandle dst = denoisedTarget;
        int passes = 3; // i = 0,1,2 -> stride: 1,2,4
        for (int i = 0; i < passes; i++)
        {
          int stride = 1 << i;
          cs.SetInt("_Stride", stride);

          cs.SetTexture(kernel, "_InputColor", src);
          cs.SetTexture(kernel, "_Normal", normalTarget);
          cs.SetTexture(kernel, "_BaseColor", baseColorTarget);
          cs.SetTexture(kernel, "_DenoisedColor", dst);

          cmd.DispatchCompute(cs, kernel,
            Mathf.CeilToInt(outputTarget.rt.width / (float)tx),
            Mathf.CeilToInt(outputTarget.rt.height / (float)ty), 1);

          // 交换 src/dst
          var tmp = src; src = dst; dst = tmp;
        }

        // 确保后续时间重投影读取 denoisedTarget
        denoisedTarget = src;
      }

      // Temporal Reprojection & Blend
      using (new ProfilingSample(cmd, "TemporalReproject"))
      {
        var cs = _assetRef != null ? _assetRef.temporalReproject : null;
        int kernel = cs.FindKernel("CSMain");
        cs.SetTexture(kernel, "_CurrentColor", denoisedTarget);
        cs.SetTexture(kernel, "_Normal", normalTarget);
        cs.SetTexture(kernel, "_BaseColor", baseColorTarget);
        cs.SetTexture(kernel, "_HistoryColor", historyTarget);
        cs.SetTexture(kernel, "_HistoryNormal", historyNormal);
        cs.SetTexture(kernel, "_HistoryBaseColor", historyBaseColor);
        cs.SetTexture(kernel, "_HistoryCount", historyCount);
        cs.SetTexture(kernel, "_OutColor", temporalTarget);
        cs.SetTexture(kernel, "_OutCount", countTarget);
        cs.SetVector("_TexelSize", new Vector2(1.0f / outputTarget.rt.width, 1.0f / outputTarget.rt.height));
        cs.SetFloat("_NormalCosThr", 0.90f);
        cs.SetFloat("_BaseColorDiffThr", 0.10f);
        cs.SetFloat("_CountNMax", 256.0f);
        uint tx = 8, ty = 8, tz = 1;
        cs.GetKernelThreadGroupSizes(kernel, out tx, out ty, out tz);
        cmd.DispatchCompute(cs, kernel, Mathf.CeilToInt(outputTarget.rt.width / (float)tx), Mathf.CeilToInt(outputTarget.rt.height / (float)ty), 1);
      }

      using (new ProfilingSample(cmd, "FinalBlit"))
      {
        // present temporal result
        cmd.Blit(temporalTarget, BuiltinRenderTextureType.CameraTarget, Vector2.one, Vector2.zero);
        // copy current temporal/color to history for next frame
        cmd.Blit(temporalTarget, historyTarget);
        // copy current normal/count/baseColor to history
        cmd.Blit(normalTarget, historyNormal);
        cmd.Blit(countTarget, historyCount);
        cmd.Blit(baseColorTarget, historyBaseColor);
        if (camera.cameraType == CameraType.Game)
          _frameIndex++;
      }

      context.ExecuteCommandBuffer(cmd);
    }
    finally
    {
      CommandBufferPool.Release(cmd);
    }
  }
}
