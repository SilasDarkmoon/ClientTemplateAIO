using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public class EditorWorkProgressShowerWaitHandle
    {
        private bool _Done = false;
        public event Action OnDone = () => { };
        public bool Done
        {
            get { return _Done; }
            set
            {
                var old = _Done;
                _Done = value;
                if (value && !old)
                {
                    OnDone();
                }
            }
        }
    }

    public interface IEditorWorkProgressShower
    {
        string Message { get; set; }
        string Title { get; set; }
        IEnumerator Work { get; set; }
        List<IEnumerator> Works { get; }
        void StartWork();
        bool Paused { get; set; }
        event Action OnQuit;
    }

    public class EditorWorkProgressShower : EditorWindow, IEditorWorkProgressShower
    {
        private string _Message = "";
        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }
        public string Title
        {
            get
            {
                if (titleContent == null)
                {
                    return null;
                }
                return titleContent.text;
            }
            set { titleContent = new GUIContent(value); }
        }
        public event Action OnQuit = () => { };
        private List<IEnumerator> _Works = new List<IEnumerator>();
        public IEnumerator Work
        {
            get
            {
                if (_Works.Count > 0)
                {
                    return _Works[0];
                }
                return null;
            }
            set
            {
                _Works.Clear();
                _Works.Add(value);
            }
        }
        public List<IEnumerator> Works
        {
            get { return _Works; }
        }
        private int _CurWork = 0;
        private bool _Initialized = false;
        private bool _Disposed = false;

        public void StartWork()
        {
            if (!_Initialized)
            {
                _Initialized = true;
                EditorApplication.LockReloadAssemblies();
            }
            ShowUtility();
        }

        public bool Paused { get; set; }
        void Update()
        {
            if (!Paused)
            {
                if (_CurWork < _Works.Count)
                {
                    var work = _Works[_CurWork];
                    if (work == null)
                    {
                        ++_CurWork;
                    }
                    else
                    {
                        if (!work.MoveNext())
                        {
                            ++_CurWork;
                        }
                    }
                    if (_CurWork >= _Works.Count)
                    {
                        Message = "Done";
                        OnDestroy();
                    }
                    else
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(Message);
        }

        void OnDestroy()
        {
            if (!_Disposed)
            {
                _Disposed = true;
                if (_Initialized)
                {
                    EditorApplication.UnlockReloadAssemblies();
                }
                OnQuit();
            }
        }
    }

    public class EditorWorkProgressShowerInEditorWindow : IEditorWorkProgressShower
    {
        private EditorWorkProgressShower win;

        public EditorWorkProgressShowerInEditorWindow()
        {
            win = ScriptableObject.CreateInstance<EditorWorkProgressShower>();
            win.OnQuit += () =>
            {
                OnQuit();
            };
        }

        public string Message
        {
            get { return win.Message; }
            set { win.Message = value; }
        }

        public string Title
        {
            get { return win.Title; }
            set { win.Title = value; }
        }

        public System.Collections.IEnumerator Work
        {
            get { return win.Work; }
            set { win.Work = value; }
        }

        public List<System.Collections.IEnumerator> Works
        {
            get { return win.Works; }
        }

        public void StartWork()
        {
            win.StartWork();
        }

        public bool Paused
        {
            get { return win.Paused; }
            set { win.Paused = value; }
        }

        public event System.Action OnQuit = () => { };
    }

    public class EditorWorkProgressShowerInConsole : IEditorWorkProgressShower
    {
        private string _Message = "";
        private string _Title = "";
        private List<System.Collections.IEnumerator> _Works = new List<System.Collections.IEnumerator>();
        public event System.Action OnQuit = () => { };

        private System.Diagnostics.Process _ExProc = null;
        private LinkedList<string> _MessageQueue = new LinkedList<string>();
        private System.Threading.ManualResetEvent _MessageReady = new System.Threading.ManualResetEvent(false);

        private volatile bool _ShouldQuit = false;
        private bool _Quited = false;
        public EditorWorkProgressShowerInConsole()
        {
#if UNITY_EDITOR_WIN
            var pipeName = System.DateTime.Now.ToString("yyMMddHHmmss");
            var pipeout = new System.IO.Pipes.NamedPipeServerStream("ProgressShowerInConsole" + pipeName, System.IO.Pipes.PipeDirection.Out);
            var pipein = new System.IO.Pipes.NamedPipeServerStream("ProgressShowerInConsoleControl" + pipeName, System.IO.Pipes.PipeDirection.In);
            var arout = pipeout.BeginWaitForConnection(null, null);
            var arin = pipein.BeginWaitForConnection(null, null);

            var dir = Application.dataPath + "/../";
            var tooldir = ModEditor.GetPackageOrModRoot(ModEditorUtils.__MOD__) + "/~Tools~/";
            System.Diagnostics.ProcessStartInfo si = new System.Diagnostics.ProcessStartInfo(tooldir + "ProgressShowerInConsole.exe", pipeName);
            si.WorkingDirectory = tooldir;
            _ExProc = System.Diagnostics.Process.Start(si);

            var thd_Write = new System.Threading.Thread(() =>
            {
                try
                {
                    pipeout.EndWaitForConnection(arout);
                    var sw = new System.IO.StreamWriter(pipeout);
                    while (_MessageReady.WaitOne())
                    {
                        _MessageReady.Reset();
                        lock (_MessageQueue)
                        {
                            foreach (var line in _MessageQueue)
                            {
                                sw.WriteLine(line);
                            }
                            sw.Flush();
                            _MessageQueue.Clear();
                        }
                    }
                }
                finally
                {
                    pipeout.Dispose();
                }
            });
            thd_Write.Start();

            var thd_Read = new System.Threading.Thread(() =>
            {
                try
                {
                    pipein.EndWaitForConnection(arin);
                    var sr = new System.IO.StreamReader(pipein);
                    while (!_ExProc.HasExited)
                    {
                        var line = sr.ReadLine();
                        if (line != null)
                        {
                            if (line == "\uEE05Quit")
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _ShouldQuit = true;
                    thd_Write.Abort();
                    _MessageReady.Set();
                    pipein.Dispose();
                }
            });
            thd_Read.Start();
#endif
        }

        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                if (!string.IsNullOrEmpty(value))
                {
                    lock (_MessageQueue)
                    {
                        _MessageQueue.AddLast("\uEE05Message");
                        _MessageQueue.AddLast(value);
                    }
                    _MessageReady.Set();
                }
            }
        }
        public string Title
        {
            get { return _Title; }
            set
            {
                _Title = value;
                if (!string.IsNullOrEmpty(value))
                {
                    lock (_MessageQueue)
                    {
                        _MessageQueue.AddLast("\uEE05Title");
                        _MessageQueue.AddLast(value);
                    }
                    _MessageReady.Set();
                }
            }
        }
        public System.Collections.IEnumerator Work
        {
            get
            {
                if (_Works.Count > 0)
                {
                    return _Works[0];
                }
                return null;
            }
            set
            {
                _Works.Clear();
                _Works.Add(value);
            }
        }
        public List<System.Collections.IEnumerator> Works
        {
            get { return _Works; }
        }

        private bool _Paused = false;
        public bool Paused
        {
            get { return _Paused; }
            set
            {
                var old = _Paused;
                _Paused = value;
                if (!value && old)
                {
                    StartWork();
                }
            }
        }

        public void StartWork()
        {
            while (WorkStep())
            {
                AsyncWorkTimer.Reset();
            }
        }
        private int _CurWork = 0;
        private bool WorkStep()
        {
            if (_ShouldQuit)
            {
                if (!_Quited)
                {
                    _Quited = true;
                    OnQuit();
                }
                return false;
            }
            if (!Paused)
            {
                if (_CurWork < _Works.Count)
                {
                    var work = _Works[_CurWork];
                    if (work == null)
                    {
                        ++_CurWork;
                    }
                    else
                    {
                        if (!work.MoveNext())
                        {
                            ++_CurWork;
                        }
                    }
                    if (_CurWork >= _Works.Count)
                    {
                        Message = "Done";
                        lock (_MessageQueue)
                        {
                            _MessageQueue.AddLast("\uEE05Quit");
                        }
                        _MessageReady.Set();
                        if (!_Quited)
                        {
                            _Quited = true;
                            OnQuit();
                        }
                        return false;
                    }
                    return true;
                }
            }
            if (!_Quited)
            {
                _Quited = true;
                OnQuit();
            }
            return false;
        }
    }

    public struct EditorWorkProgressLogger
    {
        public IEditorWorkProgressShower Shower;

        public void Log(string message)
        {
            Debug.Log(message);
            if (Shower != null)
            {
                Shower.Message = message;
            }
        }
    }
}
