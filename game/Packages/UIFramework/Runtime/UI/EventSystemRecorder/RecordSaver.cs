using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityEngineEx
{
    public class RecordSaver : IDisposable
    {
        private class SavingData
        {
            public string Type;
            public object Data;
        }
        private ConcurrentQueue<SavingData> FramesToSave = new ConcurrentQueue<SavingData>();
        private AutoResetEvent HaveFramesToSave = new AutoResetEvent(false);
        private int IsDoingSaveWork = 0;
        private void SaveWork(string filename)
        {
            try
            {
                while (true)
                {
                    if (HaveFramesToSave.WaitOne(500))
                    {
                        var frames = FramesToSave;
                        if (frames == null)
                        {
                            return;
                        }
                        using (var sw = PlatDependant.OpenAppendText(filename))
                        {
                            SavingData saving;
                            while (frames.TryDequeue(out saving))
                            {
                                sw.Write("-- StartOf");
                                sw.Write(saving.Type);
                                sw.WriteLine(" --");
                                var data = saving.Data;
                                if (data is string)
                                {
                                    var dstr = (string)data;
                                    sw.WriteLine(dstr);
                                }
                                else if (data is byte[])
                                {
                                    var dbuffer = (byte[])data;
                                    sw.WriteLine(Convert.ToBase64String(dbuffer));
                                }
                                else if (data is RawDataWithProgress)
                                {
                                    var rawwithprog = (RawDataWithProgress)data;
                                    var realdata = rawwithprog.Raw;
                                    string str = realdata as string;
                                    if (str == null)
                                    {
                                        if (realdata is byte[])
                                        {
                                            var dbuffer = (byte[])realdata;
                                            str = Convert.ToBase64String(dbuffer);
                                        }
                                        else
                                        {
                                            str = JsonUtility.ToJson(realdata);
                                        }
                                    }
                                    var enc = new EncodedDataWithProgress()
                                    {
                                        ProgressIndex = rawwithprog.ProgressIndex,
                                        Time = rawwithprog.Time,
                                        Tag = rawwithprog.Tag,
                                        Encoded = str,
                                    };
                                    sw.WriteLine(JsonUtility.ToJson(enc));
                                }
                                else
                                {
                                    sw.WriteLine(JsonUtility.ToJson(data));
                                }
                                sw.Write("-- EndOf");
                                sw.Write(saving.Type);
                                sw.WriteLine(" --");
                            }
                        }
                    }
                    if (FramesToSave == null)
                    {
                        return;
                    }
                }
            }
            finally
            {
                HaveFramesToSave.Dispose();
                Interlocked.Exchange(ref IsDoingSaveWork, 0);
            }
        }
        private bool TryStartSaveWork(string filename)
        {
            if (Interlocked.Exchange(ref IsDoingSaveWork, 1) == 0)
            {
                PlatDependant.RunBackgroundLongTime(prog => SaveWork(filename));
                return true;
            }
            else
            {
                return false;
            }
        }

        public void EnqueueSavingData(string type, object data)
        {
            try
            {
                FramesToSave.Enqueue(new SavingData()
                {
                    Type = type,
                    Data = data,
                });
                HaveFramesToSave.Set();
            }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
        }

        private string _FileName;
        public string FileName { get { return _FileName; } }

        public RecordSaver(string filename, bool additive)
        {
            var old = Interlocked.Exchange(ref _Instance, this);
            if (old != null)
            {
                old.Dispose();
            }

            filename = filename ?? "rec/record.json";
            _FileName = filename = Path.Combine(ThreadSafeValues.LogPath, filename);
            if (!additive)
            {
                PlatDependant.DeleteFile(filename);
            }
            TryStartSaveWork(filename);
        }
        public RecordSaver(string filename) : this(filename, true) { }
        public RecordSaver() : this(null) { }

        private int Disposed = 0;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref Disposed, 1) == 0)
            {
                Volatile.Write(ref FramesToSave, null);
                HaveFramesToSave.Set();
            }
            Interlocked.CompareExchange(ref _Instance, null, this);
        }

        private static RecordSaver _Instance;
        public static RecordSaver Instance { get { return _Instance; } }

        public static void Start(string filename)
        {
            if (_Instance == null)
            {
                new RecordSaver(filename);
            }
        }
        public static void Start()
        {
            Start(null);
        }
        public static void Stop()
        {
            RecordSaver inst;
            if ((inst = _Instance) != null)
            {
                inst.Dispose();
            }
        }
        public static void StartNew(string filename)
        {
            new RecordSaver(filename, false);
        }
        public static void StartNew()
        {
            StartNew(null);
        }
        public static void EnqueueRecord(string type, object data)
        {
            var inst = _Instance;
            if (inst != null)
            {
                inst.EnqueueSavingData(type, data);
            }
        }
        public static void EnqueueRecordOfProgress(string type, object data)
        {
            EnqueueRecordOfProgress(type, null, data);
        }
        public static void EnqueueRecordOfProgress(string type, string tag, object data)
        {
            var inst = _Instance;
            if (inst != null)
            {
                int pindex = EventSystemRecorder.ProgressIndex ?? 0;
                float ptime = EventSystemRecorder.RealtimeInCurrentFrame - (EventSystemRecorder.ProgressStartTime ?? 0);
                //string str = data as string;
                //if (str == null)
                //{
                //    if (data is byte[])
                //    {
                //        var dbuffer = (byte[])data;
                //        str = Convert.ToBase64String(dbuffer);
                //    }
                //    else
                //    {
                //        str = JsonUtility.ToJson(data);
                //    }
                //}
                //inst.EnqueueSavingData(type, new EncodedDataWithProgress()
                //{
                //    ProgressIndex = pindex,
                //    Time = ptime,
                //    Tag = tag,
                //    Encoded = str,
                //});
                inst.EnqueueSavingData(type, new RawDataWithProgress()
                {
                    ProgressIndex = pindex,
                    Time = ptime,
                    Tag = tag,
                    Raw = data,
                });
            }
        }
    }
}
