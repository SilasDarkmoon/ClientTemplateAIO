﻿#if UNITY_IOS
#define HTTP_REQ_DONOT_ABORT
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngineEx;

namespace ModNet
{
    public class HttpRequestLegacy : HttpRequestBase
    {
        protected System.Net.HttpWebRequest _InnerReq;
        protected object _CloseLock = new object();
        protected bool _Closed = false;
#if HTTP_REQ_DONOT_ABORT
        protected List<IDisposable> _CloseList = new List<IDisposable>();
#endif

        static HttpRequestLegacy()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        public HttpRequestLegacy(string url, HttpRequestData headers, HttpRequestData data, string dest)
            : base(url, headers, data, dest)
        {
        }
        public HttpRequestLegacy(string url, HttpRequestData data, string dest)
            : this(url, null, data, dest)
        {
        }
        public HttpRequestLegacy(string url, string dest)
            : this(url, null, null, dest)
        {
        }
        public HttpRequestLegacy(string url, HttpRequestData data)
            : this(url, null, data, null)
        {
        }
        public HttpRequestLegacy(string url)
            : this(url, null, null, null)
        {
        }
        
        public override void StartRequest()
        {
            if (_Status == HttpRequestStatus.NotStarted)
            {
                _Status = HttpRequestStatus.Running;
#if NETFX_CORE
                var task = new System.Threading.Tasks.Task(RequestWork, null);
                task.Start();
#else
                System.Threading.ThreadPool.QueueUserWorkItem(RequestWork);
#endif
#if HTTP_REQ_DONOT_ABORT
    			if (_Timeout > 0)
    			{
    				System.Threading.ThreadPool.QueueUserWorkItem(state =>
    				{
    					System.Threading.Thread.Sleep(_Timeout);
    					StopRequest();
    				});
    			}
#endif
            }
        }

        public override void StopRequest()
        {
            lock (_CloseLock)
            {
                if (!_Closed)
                {
                    _Closed = true;
                    if (_InnerReq != null)
                    {
                        var req = _InnerReq;
                        _InnerReq = null;
#if HTTP_REQ_DONOT_ABORT
                        foreach(var todispose in _CloseList)
                        {
                            if (todispose != null)
                            {
                                todispose.Dispose();
                            }
                        }
#else
                        req.Abort();
#endif
                        if (_Error == null)
                        {
                            _Error = "timedout";
                        }
                        _Status = HttpRequestStatus.Finished;
                        if (_OnDone != null)
                        {
                            var ondone = _OnDone;
                            _OnDone = null;
                            ondone();
                        }
                    }
                }
            }
        }

        private volatile static System.Reflection.FieldInfo _hostField;
        private volatile static System.Reflection.FieldInfo _hostFieldLock;
        public void RequestWork(object state)
        {
            try
            {
                var uri = new Uri(_Url);
#if NETFX_CORE
                System.Net.HttpWebRequest req = System.Net.HttpWebRequest.CreateHttp(uri);
#else
                System.Net.HttpWebRequest req = System.Net.HttpWebRequest.Create(uri) as System.Net.HttpWebRequest;
                req.KeepAlive = false;

                try
                {
                    // https://stackoverflow.com/questions/15643223/dns-refresh-timeout-with-mono
                    // Clear out the cached host entry
                    if (_hostField == null || _hostFieldLock == null)
                    {
                        _hostField = typeof(System.Net.ServicePoint).GetField("host", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        _hostFieldLock = typeof(System.Net.ServicePoint).GetField("hostE", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    }
                    var hostLock = _hostFieldLock.GetValue(req.ServicePoint);
                    lock (hostLock)
                        _hostField.SetValue(req.ServicePoint, null);
                }
                catch (Exception re)
                {
                    PlatDependant.LogError(re);
                }
#endif

                try
                {
                    lock (_CloseLock)
                    {
                        if (_Closed)
                        {
#if !HTTP_REQ_DONOT_ABORT
                            req.Abort();
#endif
                            if (_Status != HttpRequestStatus.Finished)
                            {
                                _Error = "cancelled";
                                _Status = HttpRequestStatus.Finished;
                            }
                            return;
                        }
                        _InnerReq = req;
                    }

#if !NETFX_CORE
                    req.Timeout = int.MaxValue;
                    req.ReadWriteTimeout = int.MaxValue;
#if !HTTP_REQ_DONOT_ABORT
                    if (_Timeout > 0)
                    {
                        req.Timeout = _Timeout;
                        req.ReadWriteTimeout = _Timeout;

                    }
#endif
#endif

                    var data = PrepareRequestData();
                    if (_Headers != null)
                    {
                        foreach (var kvp in _Headers.Data)
                        {
                            var key = kvp.Key;
                            var val = (kvp.Value ?? "").ToString();
                            if (key.IndexOfAny(new[] { '\r', '\n', ':', }) >= 0)
                            {
                                continue; // it is dangerous, may be attacking.
                            }
                            if (val.IndexOfAny(new[] { '\r', '\n', }) >= 0)
                            {
                                continue; // it is dangerous, may be attacking.
                            }
                            else
                            {
                                req.Headers[key] = val;
                            }
                        }
                    }
                    if (_RangeEnabled)
                    {
                        long filepos = 0;
                        if (_Dest != null)
                        {
                            using (var stream = PlatDependant.OpenRead(_Dest))
                            {
                                if (stream != null)
                                {
                                    try
                                    {
                                        filepos = stream.Length;
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                }
                            }
                        }
                        if (filepos <= 0)
                        {
                            if (_DestStream != null)
                            {
                                try
                                {
                                    if (_DestStream.CanSeek)
                                    {
                                        filepos = _DestStream.Length;
                                    }
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                            }
                        }
                        if (filepos > 0)
                        {
                            if (filepos > int.MaxValue)
                            {
                                _RangeEnabled = false;
                            }
                            else
                            {
                                req.AddRange((int)filepos);
                            }
                        }
                        else
                        {
                            _RangeEnabled = false;
                        }
                    }
                    if (_Data != null && data != null)
                    {
                        req.Method = "POST";
                        if (_Data.ContentType != null)
                        {
                            req.ContentType = _Data.ContentType;
                        }

#if NETFX_CORE
                        var tstream = req.GetRequestStreamAsync();
                        if (_Timeout > 0)
                        {
                            if (!tstream.Wait(_Timeout))
                            {
                                throw new TimeoutException();
                            }
                        }
                        else
                        {
                            tstream.Wait();
                        }
                        var stream = tstream.Result;
#else
                        req.ContentLength = data.Length;
                        var stream = req.GetRequestStream();
#endif

                        lock (_CloseLock)
                        {
                            if (_Closed)
                            {
#if !HTTP_REQ_DONOT_ABORT
                                req.Abort();
#endif
                                if (_Status != HttpRequestStatus.Finished)
                                {
                                    _Error = "cancelled";
                                    _Status = HttpRequestStatus.Finished;
                                }
                                return;
                            }
                        }
                        if (stream != null)
                        {
#if NETFX_CORE
                            if (_Timeout > 0)
                            {
                                stream.WriteTimeout = _Timeout;
                            }
                            else
                            {
                                stream.WriteTimeout = int.MaxValue;
                            }
#endif

                            try
                            {
                                if (data.RawData != null)
                                {
                                    stream.Write(data.RawData, 0, data.RawData.Length);
                                }
                                else if (data.DataStream != null)
                                {
                                    data.DataStream.CopyTo(stream);
                                }
                                else if (data.FilePath != null)
                                {
                                    using (var src = PlatDependant.OpenRead(data.FilePath))
                                    {
                                        src.CopyTo(stream);
                                    }
                                }
                                stream.Flush();
                            }
                            finally
                            {
                                stream.Dispose();
                            }
                        }
                    }
                    else
                    {
                    }
                    lock (_CloseLock)
                    {
                        if (_Closed)
                        {
#if !HTTP_REQ_DONOT_ABORT
                            req.Abort();
#endif
                            if (_Status != HttpRequestStatus.Finished)
                            {
                                _Error = "cancelled";
                                _Status = HttpRequestStatus.Finished;
                            }
                            return;
                        }
                    }
                    System.Net.WebResponse resp = null;
                    try
                    {
#if NETFX_CORE
                        var tresp = req.GetResponseAsync();
                        if (_Timeout > 0)
                        {
                            if (!tresp.Wait(_Timeout))
                            {
                                throw new TimeoutException();
                            }
                        }
                        else
                        {
                            tresp.Wait();
                        }
                        resp = tresp.Result;
#else
                        resp = req.GetResponse();
#endif
                    }
                    catch (System.Net.WebException we)
                    {
#if NETFX_CORE
                        if (we.Status.ToString() == "Timeout")
#else
                        if (we.Status == System.Net.WebExceptionStatus.Timeout)
#endif
                        {
                            throw;
                        }
                        else
                        {
                            if (we.Response is System.Net.HttpWebResponse && ((System.Net.HttpWebResponse)we.Response).StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable) // 416
                            {
                                throw;
                            }
                            else if (we.Response is System.Net.HttpWebResponse)
                            {
                                resp = we.Response;
                                var code = ((System.Net.HttpWebResponse)we.Response).StatusCode;
                                _Error = "HttpError: " + (int)code + "\n" + we.Message;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    lock (_CloseLock)
                    {
                        if (_Closed)
                        {
#if !HTTP_REQ_DONOT_ABORT
                            req.Abort();
#endif
                            if (_Status != HttpRequestStatus.Finished)
                            {
                                _Error = "cancelled";
                                _Status = HttpRequestStatus.Finished;
                            }
                            return;
                        }
                    }
                    if (resp != null)
                    {
                        try
                        {
                            _Total = (ulong)resp.ContentLength;
                        }
                        catch
                        {
                        }
                        try
                        {
                            _RespHeaders = new HttpRequestData();
                            foreach (var key in resp.Headers.AllKeys)
                            {
                                _RespHeaders.Add(key, resp.Headers[key]);
                            }

                            if (_RangeEnabled)
                            {
                                bool rangeRespFound = false;
                                foreach (var key in resp.Headers.AllKeys)
                                {
                                    if (key.ToLower() == "accept-ranges")
                                    {
                                        if (resp.Headers[key].ToLower() == "bytes")
                                        {
                                            rangeRespFound = true;
                                        }
                                    }
                                    else if (key.ToLower() == "content-range")
                                    {
                                        rangeRespFound = true;
                                        //var headerval = resp.Headers[key].ToLower();
                                        //if (string.IsNullOrEmpty(headerval))
                                        //{
                                        //    rangeRespFound = false;
                                        //}
                                        //else
                                        //{
                                        //    var parts = headerval.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                                        //    if (parts.Length < 1)
                                        //    {
                                        //        rangeRespFound = false;
                                        //    }
                                        //    else if (parts[0] != "bytes")
                                        //    {
                                        //        rangeRespFound = false;
                                        //    }
                                        //    else if (parts.Length > 1)
                                        //    {
                                        //        ulong respstart;
                                        //        if (!ulong.TryParse(parts[1], out respstart))
                                        //        {
                                        //            rangeRespFound = false;
                                        //        }
                                        //        else if (respstart != _DestStartOffset)
                                        //        {
                                        //            rangeRespFound = false;
                                        //        }
                                        //    }
                                        //}
                                        //if (!rangeRespFound)
                                        //{
                                        //    break;
                                        //}
                                    }
                                }
                                if (!rangeRespFound)
                                {
                                    _RangeEnabled = false;
                                }
                            }

                            var stream = resp.GetResponseStream();
                            lock (_CloseLock)
                            {
                                if (_Closed)
                                {
#if !HTTP_REQ_DONOT_ABORT
                                    req.Abort();
#endif
                                    if (_Status != HttpRequestStatus.Finished)
                                    {
                                        _Error = "cancelled";
                                        _Status = HttpRequestStatus.Finished;
                                    }
                                    return;
                                }
                            }
                            if (stream != null)
                            {
#if NETFX_CORE
                                if (_Timeout > 0)
                                {
                                    stream.ReadTimeout = _Timeout;
                                }
                                else
                                {
                                    stream.ReadTimeout = int.MaxValue;
                                }
#endif
                                Stream streamd = null;
                                try
                                {
                                    byte[] buffer = new byte[64 * 1024];
                                    ulong totalcnt = 0;
                                    int readcnt = 0;

                                    bool mem = false;
                                    if (_Dest != null)
                                    {
                                        if (_RangeEnabled)
                                        {
                                            streamd = PlatDependant.OpenReadWrite(_Dest);
                                            streamd.Seek(0, SeekOrigin.End);
                                            totalcnt = (ulong)streamd.Length;
                                        }
                                        else
                                        {
                                            streamd = PlatDependant.OpenWrite(_Dest);
                                        }
#if HTTP_REQ_DONOT_ABORT
                                        if (streamd != null)
                                        {
                                            _CloseList.Add(streamd);
                                        }
#endif
                                    }
                                    if (streamd == null)
                                    {
                                        if (_DestStream != null)
                                        {
                                            if (_RangeEnabled)
                                            {
                                                _DestStream.Seek(0, SeekOrigin.End);
                                                totalcnt = (ulong)_DestStream.Length;
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    _DestStream.SetLength(0);
                                                }
                                                catch (Exception e)
                                                {
                                                    PlatDependant.LogError(e);
                                                }
                                            }
                                            streamd = _DestStream;
                                        }
                                        else
                                        {
                                            mem = true;
                                            streamd = new MemoryStream();
#if HTTP_REQ_DONOT_ABORT
                                            _CloseList.Add(streamd);
#endif
                                        }
                                    }

                                    if (_Total > 0)
                                    {
                                        _Total += totalcnt;
                                    }

                                    do
                                    {
                                        lock (_CloseLock)
                                        {
                                            if (_Closed)
                                            {
#if !HTTP_REQ_DONOT_ABORT
                                                req.Abort();
#endif
                                                if (_Status != HttpRequestStatus.Finished)
                                                {
                                                    _Error = "cancelled";
                                                    _Status = HttpRequestStatus.Finished;
                                                }
                                                return;
                                            }
                                        }
                                        try
                                        {
                                            readcnt = 0;
                                            readcnt = stream.Read(buffer, 0, buffer.Length);
                                            if (readcnt <= 0)
                                            {
                                                stream.ReadByte(); // when it is closed, we need read to raise exception.
                                                break;
                                            }

                                            streamd.Write(buffer, 0, readcnt);
                                            streamd.Flush();
                                        }
                                        catch (TimeoutException te)
                                        {
                                            PlatDependant.LogError(te);
                                            _Error = "timedout";
                                        }
                                        catch (System.Net.WebException we)
                                        {
                                            PlatDependant.LogError(we);
#if NETFX_CORE
                                            if (we.Status.ToString() == "Timeout")
#else
                                            if (we.Status == System.Net.WebExceptionStatus.Timeout)
#endif
                                            {
                                                _Error = "timedout";
                                            }
                                            else
                                            {
                                                _Error = "Request Error (Exception):\n" + we.ToString();
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            PlatDependant.LogError(e);
                                            _Error = "Request Error (Exception):\n" + e.ToString();
                                        }
                                        lock (_CloseLock)
                                        {
                                            if (_Closed)
                                            {
#if !HTTP_REQ_DONOT_ABORT
                                                req.Abort();
#endif
                                                if (_Status != HttpRequestStatus.Finished)
                                                {
                                                    _Error = "cancelled";
                                                    _Status = HttpRequestStatus.Finished;
                                                }
                                                return;
                                            }
                                        }
                                        totalcnt += (ulong)readcnt;
                                        _Length = totalcnt;
                                        //PlatDependant.LogInfo(readcnt);
                                    } while (readcnt > 0);

                                    if (mem)
                                    {
                                        _Resp = ((MemoryStream)streamd).ToArray();
                                    }
                                }
                                finally
                                {
                                    stream.Dispose();
                                    if (streamd != null)
                                    {
                                        if (streamd != _DestStream)
                                        {
                                            streamd.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
#if NETFX_CORE
                            resp.Dispose();
#else
                            resp.Close();
#endif
                        }
                    }
                }
                catch (TimeoutException te)
                {
                    PlatDependant.LogError(te);
                    _Error = "timedout";
                }
                catch (System.Net.WebException we)
                {
                    PlatDependant.LogError(we);
#if NETFX_CORE
                    if (we.Status.ToString() == "Timeout")
#else
                    if (we.Status == System.Net.WebExceptionStatus.Timeout)
#endif
                    {
                        _Error = "timedout";
                    }
                    else
                    {
                        if (we.Response is System.Net.HttpWebResponse && ((System.Net.HttpWebResponse)we.Response).StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable) // 416
                        {
                            //try
                            //{
                            //    _RangeEnabled = false;
                            //    if (_DestStream != null)
                            //    {
                            //        _DestStream.Seek(0, SeekOrigin.Begin);
                            //        _DestStream.SetLength(0);
                            //    }
                            //    RequestWork(state);
                            //}
                            //catch (Exception e) { }

                            // Normally this is a fully downloaded file.
                            _Error = null;
                        }
                        else if (we.Response is System.Net.HttpWebResponse)
                        {
                            var code = ((System.Net.HttpWebResponse)we.Response).StatusCode;
                            _Error = "HttpError: " + (int)code + "\n" + we.Message;
                        }
                        else
                        {
                            _Error = "Request Error (Exception):\n" + we.ToString();
                        }
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                    _Error = "Request Error (Exception):\n" + e.ToString();
                }
                finally
                {
                    if (_Error == null)
                    {
                        lock (_CloseLock)
                        {
                            _Closed = true;
                            _InnerReq = null;
                        }
                    }
                    else
                    {
                        StopRequest();
                    }
                }
            }
            catch (TimeoutException te)
            {
                PlatDependant.LogError(te);
                _Error = "timedout";
            }
            catch (System.Net.WebException we)
            {
                PlatDependant.LogError(we);
#if NETFX_CORE
                if (we.Status.ToString() == "Timeout")
#else
                if (we.Status == System.Net.WebExceptionStatus.Timeout)
#endif
                {
                    _Error = "timedout";
                }
                else
                {
                    _Error = "Request Error (Exception):\n" + we.ToString();
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
                _Error = "Request Error (Exception):\n" + e.ToString();
            }
            finally
            {
                lock (_CloseLock)
                {
                    _Status = HttpRequestStatus.Finished;
                    if (_OnDone != null)
                    {
                        var ondone = _OnDone;
                        _OnDone = null;
                        ondone();
                    }
                }
            }
        }
    }

    internal partial class HttpRequestCreator
    {
        protected static HttpRequestCreator _Creator_Legacy = new HttpRequestCreator("legacy", (url, headers, data, dest) => new HttpRequestLegacy(url, headers, data, dest));
    }
}
