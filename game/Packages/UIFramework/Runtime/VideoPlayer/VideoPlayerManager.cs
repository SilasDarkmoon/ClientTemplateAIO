using System;
//using CoreGame.UI.Views;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;
//using CoreGame.CameraControllers;
using UnityEngine.EventSystems;
using UnityEngineEx;
/// <summary>
///海鸣不骑猪  修改
///2018.5.21 修改
///主要是增加低配置机器判断 在低配置机器上面将会禁止组件执行
/// </summary>
public class VideoPlayerManager : MonoBehaviour, IPointerClickHandler
{
    public enum InitType
    {
        Awake,
        Manually,
    }

    public string videoPath;
    public event Action<VideoPlayer> PrepareCompletedDelegate;
    public event Action<VideoPlayer, string> ErrorReceivedDelegate;
    public Action<VideoPlayer> LoopEndDelegate;
    public Camera renderCamera;

    [SerializeField]
    private VideoPlayer videoPlayer;
    [SerializeField]
    private RawImage targetRawImage;
    [SerializeField]
    private InitType IsInitType = InitType.Awake;
    //[SerializeField]
    //private CoreGame.QualityLevel MinimumAdaptation = CoreGame.QualityLevel.Middle;
    private RenderTexture _targetRenderTexture;
    public RenderTexture targetRenderTexture { get { return _targetRenderTexture; } }
    private const int MaxW = 1920;
    private const int MaxH = 1080;
    private bool _isInit = false;
    private bool closeClick = false;
    private static VideoPlayerManager instance;
    public static VideoPlayerManager GetInstance()
    {
        return instance;
    }

    [SerializeField]
    private bool ProhibitedAndroidSimulator = true;///禁止模拟器上面播放视频
    #region 对外接口
    private bool CheckPlayVideo()
    {
        return false;// (int)QualityManager.GetLevel() > (int)MinimumAdaptation || ProhibitedAndroidSimulator && PlatDependant.IsRunAndroidSimulator();
    }
    /// <summary>
    /// init 方法
    /// </summary>
    public void Init()
    {
        if (_isInit) return;
        //视频组件在模拟器上面不进行播放
        if (CheckPlayVideo())
        {
//            if (GLog.IsLogInfoEnabled) GLog.LogInfo("VideoPlayerManager 最低不是最低处理机型 [" + QualityManager.GetLevel() + " ] [ " + MinimumAdaptation + " ]");
            if (targetRawImage != null && targetRawImage.gameObject != null) targetRawImage.enabled = false;
            _isInit = false;
            return;
        }

        if (targetRawImage == null)
        {
            PlatDependant.LogError("VideoPlayerManager 中的渲染组件targetRawImage 不存在 ");
            _isInit = false;
            return;
        }

        if (_targetRenderTexture == null)
        {
            var h =  Math.Min(Screen.height, MaxH);
            int w =  Mathf.FloorToInt(h * (MaxW * 1.0f / MaxH));
            _targetRenderTexture = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            //_targetRenderTexture.useMipMap = false;
            _targetRenderTexture.wrapMode = TextureWrapMode.Clamp;
            _targetRenderTexture.filterMode = FilterMode.Point;
            //_targetRenderTexture.useMipMap = false;
        }
        else
        {
            RenderTexture.ReleaseTemporary(_targetRenderTexture);
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = true;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = true;
            videoPlayer.playbackSpeed = 1;
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _targetRenderTexture;
        _targetRenderTexture.name = "VideoPlayerManager";

        videoPlayer.prepareCompleted += PrepareCompleted;
        videoPlayer.errorReceived += ErrorReceived;
        videoPlayer.loopPointReached += LoopEnd;
        if (videoPath != "")
        {
            videoPlayer.url = Application.streamingAssetsPath + "/" + videoPath;
        }
        targetRawImage.texture = _targetRenderTexture;
        _isInit = true;
    }
    public void SetVideoPath(string path)
    {
        videoPlayer.url = Application.streamingAssetsPath + "/" + path;
    }

    public void SetVideoClip(VideoClip clip)
    {
        videoPlayer.clip = clip;
    }

    public void SetCloseClickTrigger(bool close)
    {
        closeClick = close;
    }

    /// <summary>
    /// 播放
    /// </summary>
    /// <returns></returns>
    public bool Play()
    {
        if (!_isInit || videoPlayer == null)
        {
            if (LoopEndDelegate != null) LoopEndDelegate(null);
            return false;
        }

        targetRawImage.raycastTarget = true;
        videoPlayer.Play();
        renderCamera.enabled = true;
        GetComponent<EmptyGraphic>().enabled = true;

        return true;
    }

    public bool Replay()
    {
        if (videoPlayer != null)
        {
            videoPlayer.time = 0f;
        }

        return Play();
    }
    public void Pause(bool pause)
    {
        if (videoPlayer != null)
        {
            if (pause)
            {
                videoPlayer.Pause();
            }
            else
            {
                if (videoPlayer.isPaused)
                {
                    videoPlayer.Play();
                }
            }
        }
    }

    /// <summary>
    /// Prepare
    /// </summary>
    /// <returns></returns>
    public bool Prepare()
    {
        if (!_isInit || videoPlayer == null)
        {
            if (PrepareCompletedDelegate != null) PrepareCompletedDelegate(null);
            return false;
        }
        videoPlayer.Prepare();
        return true;
    }

    public bool StopVideoPlayer()
    {
        if (!_isInit || videoPlayer == null) return false;
        return true;
    }
    #endregion

    #region 私有接口
    /// <summary>
    /// Awake
    /// </summary>
    private void Awake()
    {
        instance = this;

        //QualityManager.SetLevel("high");//TODO

        //视频组件在模拟器上面不进行播放
        if (CheckPlayVideo())
        {
            //if (GLog.IsLogInfoEnabled) GLog.LogInfo("VideoPlayerManager Awake 最低不是最低处理机型 [" + QualityManager.GetLevel() + " ] [ " + MinimumAdaptation + " ]");
            if (targetRawImage != null && targetRawImage.gameObject != null) targetRawImage.gameObject.SetActive(false);
            _isInit = false;
            return;
        }
        if (IsInitType == InitType.Awake) Init();
    }

    /// <summary>
    /// 销毁相关组件
    /// </summary>
    private void OnDestroy()
    {
        if (targetRawImage != null) targetRawImage.texture = null;
        if (videoPlayer != null) videoPlayer.targetTexture = null;
        if (_targetRenderTexture != null) RenderTexture.ReleaseTemporary(_targetRenderTexture);
        _targetRenderTexture = null;
        videoPlayer = null;
        targetRawImage = null;
        PrepareCompletedDelegate = null;
        ErrorReceivedDelegate = null;
        LoopEndDelegate = null;
        _isInit = false;
        instance = null;
    }

    private void PrepareCompleted(VideoPlayer vp)
    {
        if (PrepareCompletedDelegate != null) PrepareCompletedDelegate(vp);
    }

    private void ErrorReceived(VideoPlayer vp, string message)
    {
        if (ErrorReceivedDelegate != null) ErrorReceivedDelegate(vp, message);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if(!closeClick) 
            LoopEnd(videoPlayer);
    }

    private void LoopEnd(VideoPlayer vp)
    {
       
        //if (renderCamera.enabled)
        //{
        //    renderCamera.enabled = false;
        //    GetComponent<EmptyGraphic>().enabled = false;
        //    videoPlayer.Pause();
        //    videoPlayer.time = 0f;
        //    Time.timeScale = 1f;
        //    DOTween.timeScale = 1f;
        //    targetRawImage.raycastTarget = false;
        //    if (LoopEndDelegate != null)
        //    {
        //        LoopEndDelegate(vp);
        //    }
        //}
        GetComponent<EmptyGraphic>().enabled = false;
        videoPlayer.Pause();
        videoPlayer.time = 0f;
        Time.timeScale = 1f;
        DOTween.timeScale = 1f;
        targetRawImage.raycastTarget = false;
        if (LoopEndDelegate != null)
        {
            LoopEndDelegate(vp);
        }
    }
    #endregion
}
