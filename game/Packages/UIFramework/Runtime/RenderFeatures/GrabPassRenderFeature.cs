using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrabPassRenderFeature : ComponentBasedRenderFeature
{
    public enum DownSampling
    {
        None,
        x2,
        x4,
    }

    class GrabRenderPass : ScriptableRenderPass
    {
        public Material BlitMaterial;
        protected string _ProfilerTag = "Grab Pass";
        protected string _TargetName;
        protected RenderTargetIdentifier _Source;
        public RenderTexture TargetRT;
        protected RenderTargetHandle _Target;
        protected DownSampling _DownSampling;
        public bool KeepRenderTargetAfterRendering;

        public GrabRenderPass() : this(null) { }
        public GrabRenderPass(string target)
        {
            _TargetName = target;
            if (string.IsNullOrEmpty(target))
            {
                _TargetName = "_CameraOpaqueTexture";
            }
            else
            {
                _TargetName = target;
            }
            _Target.Init(_TargetName);
        }

        public void Setup(RenderTargetIdentifier source, DownSampling downSampling)
        {
            _Source = source;
            _DownSampling = downSampling;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (TargetRT == null)
            {
                RenderTextureDescriptor descriptor = cameraTextureDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                if (_DownSampling == DownSampling.x2)
                {
                    descriptor.width /= 2;
                    descriptor.height /= 2;
                }
                else if (_DownSampling == DownSampling.x4)
                {
                    descriptor.width /= 4;
                    descriptor.height /= 4;
                }

                cmd.GetTemporaryRT(_Target.id, descriptor, _DownSampling == DownSampling.None ? FilterMode.Point : FilterMode.Bilinear);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_ProfilerTag);
            RenderTargetIdentifier target;
            if (TargetRT == null)
            {
                target = _Target.Identifier();
            }
            else
            {
                target = new RenderTargetIdentifier(TargetRT);
            }

            if (BlitMaterial != null)
            {
                Blit(cmd, _Source, target, BlitMaterial);
            }
            else
            {
                Blit(cmd, _Source, target);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (TargetRT == null && !KeepRenderTargetAfterRendering)
            {
                cmd.ReleaseTemporaryRT(_Target.id);
            }
        }
    }

    GrabRenderPass _Pass;

    [System.Serializable]
    public class GrabSettings
    {
        public Material BlitMaterial = null;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingOpaques;
        public DownSampling DownSampling = DownSampling.None;
        public string TargetName = null;
        public RenderTexture TargetRT = null;
        public bool KeepRenderTargetAfterRendering;
    }

    public GrabSettings Settings = new GrabSettings();

    protected override void Awake()
    {
        base.Awake();

        _Pass = new GrabRenderPass(Settings.TargetName);

        // Configures where the render pass should be injected.
        _Pass.renderPassEvent = Settings.Event;
        _Pass.BlitMaterial = Settings.BlitMaterial;
        _Pass.TargetRT = Settings.TargetRT;
        _Pass.KeepRenderTargetAfterRendering = Settings.KeepRenderTargetAfterRendering;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _Pass.Setup(renderer.cameraColorTarget, Settings.DownSampling);
        renderer.EnqueuePass(_Pass);
    }
}


