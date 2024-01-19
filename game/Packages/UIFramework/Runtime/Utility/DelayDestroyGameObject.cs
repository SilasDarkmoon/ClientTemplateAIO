using System.Collections;
using UnityEngine;

/// <summary>
/// ÑÓ³ÙÉ¾³ý
/// </summary>
public class DelayDestroyGameObject : MonoBehaviour
{
    [SerializeField]
    private float _duration = 0f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayDestroy(_duration));
    }

    private IEnumerator DelayDestroy(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        Destroy(this.gameObject);
    }

    private void OnDisable()
    {
        Destroy(this.gameObject);
    }
}
