using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineEx;

using Object = UnityEngine.Object;

public class ModUnityMainBehav : MonoBehaviour
{
    public static ModUnityMainBehav MainBehavInstance;

    // Other module should set these values.
    public static string SceneAfterEntry;
    public event ResManager.ProgressReportDelegate OnLoadingReport = (key, attached, val) => { };
    public UnityEngine.UI.Text TextProgress;
    public UnityEngine.UI.Text TextDesc;
    public UnityEngine.UI.Slider SliderProgress;
    public Func<string, string> FormatMessageFunc;

    private void Awake()
    {
        MainBehavInstance = this;
    }
    private void Start()
    {
        StartCoroutine(ResManager.InitAsync(LoadingReport));
    }
    //private void OnDestroy()
    //{
    //    if (_EntrySceneBg)
    //    {
    //        Destroy(_EntrySceneBg);
    //        _EntrySceneBg = null;
    //    }
    //}

    private Dictionary<string, string> DefaultFormatMessageDict = new Dictionary<string, string>()
    {
        { "Error", "Error happened." },
        { "HaveWorkToDo", "Loading for the first start." },
        { "ProgressStr", "Progress" },
        { "PhaseStr", "Phase" },
    };
    private string FormatMessage(string msg)
    {
        if (FormatMessageFunc != null)
        {
            return FormatMessageFunc(msg);
        }
        string val = LanguageConverter.GetLangValue(msg);
        if (val == null || ReferenceEquals(val, msg))
        {
            if (DefaultFormatMessageDict.TryGetValue(msg, out val))
            {
                return val;
            }
        }
        else
        {
            return val;
        }
        return msg;
    }
    private int TotalPhase;
    private int WorkingPhase;
    private int TotalStep;
    private int WorkingStep;
    private int WorkingStepOnWorkingPhaseStart;
    private void LoadingReport(string key, object attached, double val)
    {
        if (key == "Error")
        {
            if (TextDesc != null)
            {
                TextDesc.gameObject.SetActive(true);
                TextDesc.text = FormatMessage(key);
            }
            if (TextProgress != null && attached is string)
            {
                TextProgress.gameObject.SetActive(true);
                TextProgress.text = FormatMessage(attached as string);
            }
        }
        else if (key == "HaveWorkToDo")
        {
            if (TextDesc != null)
            {
                TextDesc.gameObject.SetActive(true);
                TextDesc.text = FormatMessage(key);
            }
            if (TextProgress != null)
            {
                TextProgress.gameObject.SetActive(true);
            }
            if (SliderProgress != null)
            {
                SliderProgress.maxValue = 1.0f;
                SliderProgress.gameObject.SetActive(true);
            }
        }
        else if (key == "Desc")
        {
            if (TextDesc != null && attached is string)
            {
                TextDesc.text = FormatMessage(attached as string);
            }
        }
        else if (key == "TotalPhase")
        {
            TotalPhase = (int)val;
        }
        else if (key == "TotalStep")
        {
            TotalStep = (int)val;
        }
        else if (key == "WorkingPhase")
        {
            WorkingPhase = (int)val;
            WorkingStepOnWorkingPhaseStart = WorkingStep;
        }
        else if (key == "WorkingStep")
        {
            WorkingStep = (int)val;
            if (SliderProgress != null)
            {
                SliderProgress.value = WorkingStep / (float)TotalStep;
            }
            if (TextProgress != null)
            {
                TextProgress.text = FormatMessage("ProgressStr") + " " + WorkingStep + "/" + TotalStep + ", " + FormatMessage("PhaseStr") + " " + WorkingPhase + "/" + TotalPhase + ".";
            }
        }
        else if (key == "WorkingStepInPhase")
        {
            var WorkingStep = WorkingStepOnWorkingPhaseStart + (int)val;
            if (SliderProgress != null)
            {
                SliderProgress.value = WorkingStep / (float)TotalStep;
            }
            if (TextProgress != null)
            {
                TextProgress.text = FormatMessage("ProgressStr") + " " + WorkingStep + "/" + TotalStep + ", " + FormatMessage("PhaseStr") + " " + WorkingPhase + "/" + TotalPhase + ".";
            }
        }
        else if (key == "WorkingStepAdvance")
        {
            ++WorkingStep;
            if (SliderProgress != null)
            {
                SliderProgress.value = WorkingStep / (float)TotalStep;
            }
            if (TextProgress != null)
            {
                TextProgress.text = FormatMessage("ProgressStr") + " " + WorkingStep + "/" + TotalStep + ", " + FormatMessage("PhaseStr") + " " + WorkingPhase + "/" + TotalPhase + ".";
            }
        }
        else if (key == "Percent")
        {
            if (SliderProgress != null)
            {
                SliderProgress.value = (float)val;
            }
            if (TextProgress != null)
            {
                TextProgress.text = FormatMessage("ProgressStr") + " " + (int)(val * 100) + "%, " + FormatMessage("PhaseStr") + " " + WorkingPhase + "/" + TotalPhase + ".";
            }
        }
        OnLoadingReport(key, attached, val);
    }

    public static string EntrySceneBgPath { get { return "Entry/EntrySceneBg.prefab"; } }
    private static GameObject _EntrySceneBg;
    public static void LoadEntrySceneBg()
    {
        if (MainBehavInstance)
        {
            var inititems = ResManager.GetInitItems(ResManager.LifetimeOrders.ResLoader, ResManager.LifetimeOrders.PostResLoader);
            for (int i = 0; i < inititems.Length; ++i)
            {
                inititems[i].Init();
            }
            var bg = ResManager.LoadResDeep(EntrySceneBgPath) as GameObject;
            if (bg)
            {
                _EntrySceneBg = Instantiate(bg);
            }
        }
    }
    public static void UnloadEntrySceneBg()
    {
        if (MainBehavInstance)
        {
            if (_EntrySceneBg)
            {
                Destroy(_EntrySceneBg);
                _EntrySceneBg = null;
            }
            var inititems = ResManager.GetInitItems(ResManager.LifetimeOrders.ResLoader, ResManager.LifetimeOrders.PostResLoader);
            for (int i = inititems.Length - 1; i >= 0; --i)
            {
                inititems[i].Cleanup();
            }
        }
    }
    public static void EntrySceneDone()
    {
        if (SceneAfterEntry != null)
        {
            ResManager.LoadScene(SceneAfterEntry);
        }
    }

    public class WaitForReadyToNextScene : ResManager.ILifetime, ResManager.IInitAsync
    {
        public static bool Done = true;

        public int Order { get { return ResManager.LifetimeOrders.EntrySceneDone - 5; } }
        public void Prepare()
        {
        }
        public void Init()
        {
        }
        public void Cleanup()
        {
        }
        public IEnumerator InitAsync()
        {
            while (!Done)
            {
                yield return null;
            }
        }

        private WaitForReadyToNextScene() { }
        public static readonly WaitForReadyToNextScene Instance = new WaitForReadyToNextScene();
    }

#if UNITY_EDITOR
    private class WaitForReadyToStart : ResManager.ILifetime, ResManager.IInitAsync, ResManager.IInitProgressReporter
    {
        public int Order { get { return ResManager.LifetimeOrders.EditorPrepare; } }

        public event ResManager.ProgressReportDelegate ReportProgress = (key, attached, val) => { };

        public void Cleanup()
        {
        }

        public int CountWorkStep()
        {
            return 1;
        }

        public string GetPhaseDesc()
        {
            return "EditorWaitForReadyToStart";
        }

        public void Init()
        {
        }

        public IEnumerator InitAsync()
        {
            while (!EditorToClientUtils.Ready)
            {
                yield return null;
            }
        }

        public void Prepare()
        {
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnUnityStart()
    {
#if UNITY_EDITOR
        ResManager.AddInitItem(new WaitForReadyToStart());
#endif
        if (MainBehavInstance != null)
        {
            ResManager.AddInitItem(ResManager.LifetimeOrders.PostResLoader + 5, LoadEntrySceneBg);
            ResManager.AddInitItem(WaitForReadyToNextScene.Instance);
            ResManager.AddInitItem(ResManager.LifetimeOrders.EntrySceneDone, EntrySceneDone);
        }
    }
}
