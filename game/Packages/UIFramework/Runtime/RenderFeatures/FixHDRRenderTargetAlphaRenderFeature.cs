using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FixHDRRenderTargetAlphaRenderFeature : ComponentBasedRenderFeature
{
    class FixHDRRenderTargetAlphaRecreatePass : ScriptableRenderPass
    {
        protected static RenderTargetHandle[] _CandidateCameraRenderTargets = new RenderTargetHandle[3];
        public ScriptableRenderer Renderer;
        static FixHDRRenderTargetAlphaRecreatePass()
        {
            _CandidateCameraRenderTargets[0].Init("_CameraColorTexture");
            _CandidateCameraRenderTargets[1].Init("_CameraColorAttachmentA");
            _CandidateCameraRenderTargets[2].Init("_CameraColorAttachmentB");
        }

        protected string _ProfilerTag = "FixHDRRenderTargetAlphaRecreatePass";
        protected RenderTargetHandle _CameraRenderTarget;

        public FixHDRRenderTargetAlphaRecreatePass()
        {
            renderPassEvent = RenderPassEvent.BeforeRendering;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool found = false;
            if (Renderer != null)
            {
                if (_CameraRenderTarget.Identifier() == Renderer.cameraColorTarget)
                {
                    found = true;
                }
                if (!found)
                {
                    for (int i = 0; i < _CandidateCameraRenderTargets.Length; ++i)
                    {
                        _CameraRenderTarget = _CandidateCameraRenderTargets[i];
                        if (_CameraRenderTarget.Identifier() == Renderer.cameraColorTarget)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
            if (found)
            {
                renderingData.cameraData.cameraTargetDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                var cmd = CommandBufferPool.Get(_ProfilerTag);
                cmd.ReleaseTemporaryRT(_CameraRenderTarget.id);
                cmd.GetTemporaryRT(_CameraRenderTarget.id, desc, FilterMode.Bilinear);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            else
            {
                Debug.LogError("Cannot fix HDR RenderTarget Alpha. Please check RenderTarget name.");
            }
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isHdrEnabled)
        {
            var curFormat = renderingData.cameraData.cameraTargetDescriptor.colorFormat;
            if (curFormat == RenderTextureFormat.ARGBHalf
                //|| curFormat == RenderTextureFormat.BGRA10101010_XR
                )
            {
                return;
            }
            _RecreatePass.Renderer = renderer;
            renderer.EnqueuePass(_RecreatePass);
        }
    }

    FixHDRRenderTargetAlphaRecreatePass _RecreatePass;
    protected override void Awake()
    {
        base.Awake();
        _RecreatePass = new FixHDRRenderTargetAlphaRecreatePass();
    }
}