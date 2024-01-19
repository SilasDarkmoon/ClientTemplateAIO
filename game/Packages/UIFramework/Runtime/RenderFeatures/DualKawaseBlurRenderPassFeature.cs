using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class DualKawaseBlurRenderPassFeature : ComponentBasedRenderFeature
{
    class DualKawaseBlurRenderPass : ScriptableRenderPass
    {
        struct Level
        {
            public int downNameID;
            public RenderTargetIdentifier downRTI;
            public int upNameID;
            public RenderTargetIdentifier upRTI;
        }

        string m_ProfilerTag;

        private RenderTargetIdentifier source { get; set; }

        private Shader shader;

        // [down,up]
        private Level[] m_Pyramid;
        private readonly int BlurOffset = Shader.PropertyToID("_Offset");

        public Material material;

        private int tw;

        private int th;

        private int MainTex = Shader.PropertyToID("_MainTex");

        private static Mesh s_FullscreenTriangle;

        private static Mesh fullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }

        public float BlurRadius;

        public int Iteration;

        public float RTDownScaling;

        public float Rate;

        public DualKawaseBlurRenderPass(string profilerTag)
        {
            m_ProfilerTag = profilerTag;
            shader = Shader.Find("Custom/X-PostProcessing/DualKawaseBlur");
            string shaderName = shader.name;
            material = new Material(shader)
            {
                name = string.Format("PostProcess - {0}", shaderName.Substring(shaderName.LastIndexOf('/') + 1)),
                hideFlags = HideFlags.DontSave
            };
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
            m_Pyramid = new Level[Iteration];
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            tw = (int)(cameraTextureDescriptor.width / RTDownScaling);
            th = (int)(cameraTextureDescriptor.height / RTDownScaling);

            for (int i = 0; i < Iteration; i++)
            {
                int down = Shader.PropertyToID("_BlurMipDown" + i);
                int up = Shader.PropertyToID("_BlurMipUp" + i);
                cmd.GetTemporaryRT(down, tw, th, 0, FilterMode.Bilinear, RenderTextureFormat.RGB565);
                cmd.GetTemporaryRT(up, tw, th, 0, FilterMode.Bilinear, RenderTextureFormat.RGB565);
                RenderTargetIdentifier downRTI = new RenderTargetIdentifier(down);
                RenderTargetIdentifier upRTI = new RenderTargetIdentifier(up);
                m_Pyramid[i] = new Level
                {
                    downNameID = down,
                    downRTI = downRTI,
                    upNameID = up,
                    upRTI = upRTI
                };

                tw = Mathf.Max(tw / 2, 1);
                th = Mathf.Max(th / 2, 1);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            material.SetFloat(BlurOffset, BlurRadius);
            material.SetFloat("_Rate", Rate);
            // Downsample Rate
            RenderTargetIdentifier lastDown = source;
            for (int i = 0; i < Iteration; i++)
            {
                RenderTargetIdentifier mipDown = m_Pyramid[i].downRTI;
                this.BlitFullscreenTriangle(cmd, lastDown, mipDown, material, 0);
                lastDown = mipDown;
            }

            // Upsample
            RenderTargetIdentifier lastUp = lastDown;
            for (int i = Iteration - 2; i >= 0; i--)
            {
                RenderTargetIdentifier mipUp = m_Pyramid[i].upRTI;
                this.BlitFullscreenTriangle(cmd, lastUp, mipUp, material, 1);
                lastUp = mipUp;
            }

            // Render blurred texture in blend pass
            this.BlitFullscreenTriangle(cmd, lastUp, source, material, 1);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Cleanup
            for (int i = 0; i < Iteration; i++)
            {
                cmd.ReleaseTemporaryRT(m_Pyramid[i].downNameID);
                cmd.ReleaseTemporaryRT(m_Pyramid[i].upNameID);
            }
        }
        private void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier des, Material mat, int passIndex)
        {
            cmd.SetGlobalTexture(MainTex, src);
            cmd.SetRenderTarget(des);
            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, mat, 0, passIndex);
        }
    }

    DualKawaseBlurRenderPass m_ScriptablePass;

    [System.Serializable]
    public class DualKawaseBlurSettings
    {
        public delegate void OnRTScalingChangeDelegate(float newVal);
        public delegate void OnIterationChangeDelegate(int newVal);
        public delegate void OnBlurRadiusChangeDelegate(float newVal);
        public delegate void OnRateChangeDelegate(float newVal);
        public event OnIterationChangeDelegate OnIterationChange;
        public event OnRTScalingChangeDelegate OnRTChange;
        public event OnBlurRadiusChangeDelegate OnBlurRadiusChange;
        public event OnRateChangeDelegate OnRateChange;
        public string passTag = "X-DualKawaseBlur";

        [Range(0.0f, 15.0f)]
        [SerializeField]
        private float blurRadius = 5.0f;
        public float BlurRadius
        {
            get { return blurRadius; }
            set
            {
                if (value != blurRadius)
                {
                    blurRadius = value;
                    OnBlurRadiusChange?.Invoke(blurRadius);
                }
            }
        }

        [Range(1.0f, 10.0f)]
        [SerializeField]
        private int iteration = 2;
        public int Iteration
        {
            get { return iteration; }
            set
            {
                if (value != iteration)
                {
                    iteration = value;
                    OnIterationChange?.Invoke(iteration);
                }
            }
        }

        [Range(1, 10)]
        [SerializeField]
        private float rtDownScaling = 2;
        public float RTDownScaling
        {
            get { return rtDownScaling; }
            set
            {
                if (value != rtDownScaling)
                {
                    rtDownScaling = value;
                    OnRTChange?.Invoke(rtDownScaling);
                }
            }
        }
        [Range(0, 1)]
        [SerializeField]
        //新增70%黑色遮罩，UI已经确认效果
        private float _rate = 0.3f;
        public float Rate
        {
            get { return _rate; }
            set 
            {
                if (value != _rate)
                {
                    _rate = value;
                    OnRateChange?.Invoke(_rate);
                }
            }
        }

        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
    }

    public DualKawaseBlurSettings settings = new DualKawaseBlurSettings();
    private float blurRadius;
    private int iteration;
    private float rtDownScaling;
    private float defaultRate;
    private static Tween blurRadiusValueTween;
    private static Tween iterationValueTween;
    private static Tween rtDownScalingValueTween;

    protected override void Awake()
    {
        base.Awake();
        m_ScriptablePass = new DualKawaseBlurRenderPass(settings.passTag);
        m_ScriptablePass.BlurRadius = settings.BlurRadius;
        m_ScriptablePass.Iteration = settings.Iteration;
        m_ScriptablePass.RTDownScaling = settings.RTDownScaling;
        m_ScriptablePass.Rate = settings.Rate;
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = settings.Event;

        blurRadius = settings.BlurRadius;
        iteration = settings.Iteration;
        rtDownScaling = settings.RTDownScaling;
        defaultRate = settings.Rate;
        settings.OnRTChange += RTChange;
        settings.OnIterationChange += IterationChange;
        settings.OnBlurRadiusChange += BlurRadiusChange;
        settings.OnRateChange += RateChange;
    }

    public void ResetValue()
    {
        settings.BlurRadius = blurRadius;
        settings.Iteration = iteration;
        settings.RTDownScaling = rtDownScaling;
        settings.Rate = defaultRate;
    }

    private void BlurRadiusChange(float value)
    {
        m_ScriptablePass.BlurRadius = value;
    }

    private void IterationChange(int value)
    {
        m_ScriptablePass.Iteration = value;
    }

    private void RTChange(float value)
    {
        m_ScriptablePass.RTDownScaling = value;
    }

    private void RateChange(float value)
    {
        m_ScriptablePass.Rate = value;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    public void DoCameraBlur(float blurRadius = 0, int iteration = 1, float rtDownScaling = 1, float time = 0, float rate = 0.3f)
    {
        if (blurRadius != 0)
        {
            blurRadiusValueTween = DOTween.To(() => 0, x => settings.BlurRadius = x, blurRadius, time);
            blurRadiusValueTween.SetEase(Ease.OutSine);
        }
        if (iteration > 1)
        {
            iterationValueTween = DOTween.To(() => 1, x => settings.Iteration = x, iteration, time);
            iterationValueTween.SetEase(Ease.OutSine);
        }
        if (rtDownScaling > 1)
        {
            rtDownScalingValueTween = DOTween.To(() => 1, x => settings.RTDownScaling = x, rtDownScaling, time);
            rtDownScalingValueTween.SetEase(Ease.OutSine);
        }
        if (rate > 0)
        {
            settings.Rate = rate;
        }
    }

    public void StopCameraBlur()
    {
        if (blurRadiusValueTween != null)
        {
            TweenExtensions.Kill(blurRadiusValueTween);
        }
        if (iterationValueTween != null)
        {
            TweenExtensions.Kill(iterationValueTween);
        }
        if (rtDownScalingValueTween != null)
        {
            TweenExtensions.Kill(rtDownScalingValueTween);
        }
        ResetValue();
    }
}


