using UnityEngine;

public class SafeAreaFullScreenRect : MonoBehaviour
{
    public void RefreshOnStart(SafeAreaRect safeAreaRect)
    {
        if (safeAreaRect != null)
        {
            var safeRectTransform = safeAreaRect.GetComponent<RectTransform>();
            var rectTransform = GetComponent<RectTransform>();
            var xpercent = (1 - safeRectTransform.anchorMin.x * 2);
            if (xpercent != 0)
            {
                var x = (safeRectTransform.rect.width / xpercent - safeRectTransform.rect.width) / 2;
                rectTransform.offsetMin = new Vector2(-x - 1f, 0);
                rectTransform.offsetMax = new Vector2(x + 1f, 0);
            }
        }
    }
}
