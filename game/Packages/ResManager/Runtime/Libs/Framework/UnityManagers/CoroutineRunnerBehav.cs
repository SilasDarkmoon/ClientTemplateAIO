using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunnerBehav : MonoBehaviour
{
    private void Update()
    {
        UnityEngineEx.CoroutineRunner.DisposeDeadCoroutines();
    }

    private void OnDestroy()
    {
        UnityEngineEx.CoroutineRunner.DisposeAllCoroutinesOnDestroyRunner(this);
    }
}
