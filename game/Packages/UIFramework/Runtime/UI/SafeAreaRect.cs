using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaRect : MonoBehaviour
{
    public enum SimulateOption
    {
        None,
        IphoneXLandscape,
        IphoneXPortrait,
        OPPOR15,
    }

    public SimulateOption Simulate = SimulateOption.None;

    [NonSerialized]
    private RectTransform _rect;
    protected RectTransform rectTransform
    {
        get
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            return _rect;
        }
    }
    
    private float screenWidth, screenHeight, notchWidth, notchHeight;

    public Rect GetSafeArea()
    {
        float x = 0, y = 0, w = 1, h = 1;

        if (Simulate != SimulateOption.None || SafeAreaConfig.HasConfig())
        {
            var orientation = Screen.orientation;
            var safeAreaParam = SafeAreaConfig.HasConfig() ? SafeAreaConfig.GetConfig() : SafeAreaConfig.GetSimulateConfig(Simulate);

            if (Simulate == SimulateOption.IphoneXLandscape)
            {
                orientation = ScreenOrientation.LandscapeLeft;
            }
            
            if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                y = safeAreaParam.NotchSize / safeAreaParam.ScreenWidth;
                h = (safeAreaParam.ScreenWidth - safeAreaParam.NotchSize * 2) / safeAreaParam.ScreenWidth;
            }
            else
            {
                x = safeAreaParam.NotchSize / safeAreaParam.ScreenWidth;
                w = (safeAreaParam.ScreenWidth - safeAreaParam.NotchSize * 2) / safeAreaParam.ScreenWidth;
            }
        }

        return new Rect(x, y, w, h);
    }

    void Start()
    {
#if UNITY_EDITOR
        if (UnityEngine.PlayerPrefs.GetInt("___Editor_AdapteriPhoneXOpen") == 1)
        {
            Simulate = SimulateOption.IphoneXLandscape;
        }
#endif
        ApplySafeArea(GetSafeArea());
        ApplyFullArea();
    }

    void ApplySafeArea(Rect area)
    {
        var anchorMin = area.position;
        var anchorMax = area.position + area.size;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }

    void ApplyFullArea()
    {
        var fullRects = GetComponentsInChildren<SafeAreaFullScreenRect>(true);
        if (fullRects != null)
        {
            for (int i = 0; i < fullRects.Length; i++)
            {
                fullRects[i].RefreshOnStart(this);
            }
        }
    }
}
