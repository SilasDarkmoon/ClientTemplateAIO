using ModNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngineEx
{
    public class PlaybackConnection : MonoBehaviour, IReqConnection
    {
        private BaseReqHandler _InnerHandler = new BaseReqHandler();
        public void RegHandler(Request.Handler handler)
        {
            _InnerHandler.RegHandler(handler);
        }
        public void RegHandler(uint type, Request.Handler handler)
        {
            _InnerHandler.RegHandler(type, handler);
        }
        public void RegHandler<T>(Request.Handler<T> handler)
        {
            _InnerHandler.RegHandler(handler);
        }
        public void RegHandler(Request.Handler handler, int order)
        {
            _InnerHandler.RegHandler(handler, order);
        }
        public void RegHandler(uint type, Request.Handler handler, int order)
        {
            _InnerHandler.RegHandler(type, handler, order);
        }
        public void RegHandler<T>(Request.Handler<T> handler, int order)
        {
            _InnerHandler.RegHandler(handler, order);
        }
        public void RemoveHandler(Request.Handler handler)
        {
            _InnerHandler.RemoveHandler(handler);
        }
        public void RemoveHandler(uint type, Request.Handler handler)
        {
            _InnerHandler.RemoveHandler(type, handler);
        }
        public void RemoveHandler<T>(Request.Handler<T> handler)
        {
            _InnerHandler.RemoveHandler(handler);
        }

        public event Action OnNotifyConnected = () => { };

        #region Unity Message Methods
        public List<RecordedMessage> Record;
        private void Update()
        {
            if (Record != null)
            {
                int toindex = -1;
                for (int i = 0; i < Record.Count; ++i)
                {
                    var minfo = Record[i];
                    if (IsTimeToProcess(minfo))
                    {
                        toindex = i;
                        if (minfo.IsConnectNotify)
                        {
                            OnNotifyConnected();
                        }
                        else
                        {
                            _InnerHandler.HandleRequest(this, minfo.Type, minfo.Raw, minfo.Seq);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                if (toindex >= 0)
                {
                    Record.RemoveRange(0, toindex + 1);
                }
            }
        }

        public static bool IsTimeToProcess(RecordedMessage minfo)
        {
            var starttime = EventSystemRecorder.ProgressStartTime;
            var pindex = EventSystemRecorder.ProgressIndex;
            if (starttime == null || pindex == null)
            {
                return false;
            }
            if (minfo.ProgressIndex < (int)pindex)
            {
                return true;
            }
            if (minfo.ProgressIndex > (int)pindex)
            {
                return false;
            }
            var timestamp = EventSystemRecorder.RealtimeInCurrentFrame - (float)starttime;
            if (minfo.Time <= timestamp)
            {
                return true;
            }
            return false;
        }
        #endregion

        public class RecordedMessage
        {
            public int ProgressIndex;
            public float Time;

            public bool IsConnectNotify;

            public uint Type;
            public object Raw;
            public uint Seq;
        }
        public static List<RecordedMessage> LoadFile(string path)
        {
            if (!PlatDependant.IsFileExist(path))
            {
                return null;
            }
            using (var sr = PlatDependant.OpenReadText(path))
            {
                if (sr == null)
                {
                    return null;
                }
                var data = new List<RecordedMessage>();
                string endtoken = null;
                string line = null;
                StringBuilder sbframe = new StringBuilder();
                RecordedMessage pending = null;
                DataSplitter.ReceiveBlockDelegate OnReceive = (buffer, size, type, flags, seq, sseq, exflags) =>
                {
                    var message = _Serializer.Formatter.Read(type, buffer, 0, size, exflags);
                    pending.Type = type;
                    pending.Seq = seq;
                    pending.Raw = message;
                    data.Add(pending);
                    pending = null;
                };
                _Serializer.Splitter.OnReceiveBlock += OnReceive;
                try
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line == "-- StartOfNet --")
                        {
                            endtoken = "-- EndOfNet --";
                            sbframe.Clear();
                        }
                        else if (line == "-- StartOfNetCon --")
                        {
                            endtoken = "-- EndOfNetCon --";
                            sbframe.Clear();
                        }
                        else if (line == endtoken)
                        {
                            if (sbframe.Length > 0)
                            {
                                try
                                {
                                    var wrapper = JsonUtility.FromJson<EncodedDataWithProgress>(sbframe.ToString());
                                    if (endtoken == "-- EndOfNetCon --")
                                    {
                                        data.Add(new RecordedMessage()
                                        {
                                            ProgressIndex = wrapper.ProgressIndex,
                                            Time = wrapper.Time,
                                            IsConnectNotify = true,
                                        });
                                    }
                                    else
                                    {
                                        pending = new RecordedMessage()
                                        {
                                            ProgressIndex = wrapper.ProgressIndex,
                                            Time = wrapper.Time,
                                        };
                                        var raw = Convert.FromBase64String(wrapper.Encoded);
                                        var stream = new ArrayBufferStream(raw, 0, raw.Length);
                                        _Serializer.Splitter.Attach(stream);
                                        _Serializer.Splitter.TryReadBlock();
                                    }
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                    break;
                                }
                            }
                            endtoken = null;
                        }
                        else
                        {
                            if (endtoken != null)
                            {
                                sbframe.AppendLine(line);
                            }
                        }
                    }
                }
                finally
                {
                    _Serializer.Splitter.OnReceiveBlock -= OnReceive;
                }

                return data;
            }
        }
        public static List<RecordedMessage> LoadFile()
        {
            var filename = Path.Combine(ThreadSafeValues.LogPath, "rec/record.json");
            return LoadFile(filename);
        }

        #region Dummy
        public ModNet.Request Send(object reqobj, int timeout)
        { 
            return null;
        }
        public int Timeout { get { return 0; } set { } }
        public object HandleRequest(IReqClient from, uint type, object reqobj, uint seq)
        {
            return null;
        }
        public void SendRawResponse(IReqClient to, object response, uint seq_pingback) { }
        public event Action<IReqClient> OnPrepareConnection;
        public SerializationConfig SerializationConfig { get { return CarbonMessageUtils.ConnectionConfig.SConfig; } }
        private static Serializer _Serializer = new Serializer()
        {
            Splitter = new CarbonSplitter(),
            Composer = new CarbonComposer(),
            Formatter = new CarbonFormatter(),
        };
        public static Serializer SharedSerializer { get { return _Serializer; } }
        public Serializer Serializer { get { return _Serializer; } set { } }
        public event Action OnUpdate;
        public event Action OnClose;
        public void Start() { }
        public bool IsStarted { get { return true; } }
        public bool IsAlive { get { return true; } }
        public void Dispose() { }
        public bool IsConnected { get { return true; } }
        public event Action<IServerConnectionLifetime> OnConnected;
        #endregion

        public static PlaybackConnection Start(string file)
        {
            List<RecordedMessage> record;
            if (string.IsNullOrEmpty(file))
            {
                record = LoadFile();
            }
            else
            {
                record = LoadFile(file);
            }
            if (record == null || record.Count == 0)
            {
                return null;
            }

            GameObject go;
            var es = EventSystem.current;
            if (es)
            {
                go = es.gameObject;
            }
            else
            {
                go = new GameObject("PlaybackConnection");
            }
            var con = go.GetComponent<PlaybackConnection>();
            if (!con)
            {
                con = go.AddComponent<PlaybackConnection>();
            }
            con.Record = record;
            return con;
        }
    }
}