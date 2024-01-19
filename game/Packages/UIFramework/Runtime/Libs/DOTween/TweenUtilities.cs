using DG.Tweening;
using UnityEngine;

public static class TweenUtiliti
{
    public static void PlayAnim(RectTransform target, Vector2 endValue, float duration, float scaleEndValue, TweenCallback action)
    {
        DOTween.To(() => target.anchoredPosition, x => target.anchoredPosition = x, endValue, duration)
                .SetOptions(false).SetTarget(target).SetEase<Tweener>(Ease.Linear);
        Vector3 endValueV3 = new Vector3(scaleEndValue, scaleEndValue, scaleEndValue);
        DOTween.To(() => target.localScale, x => target.localScale = x, endValueV3, duration).SetTarget(target)
                .SetEase<Tweener>(Ease.Linear).OnComplete<Tweener>(action);
    }

    public static void KillTween(Component target, bool complete = false)
    {
        DOTween.Kill(target, complete);
    }
}
