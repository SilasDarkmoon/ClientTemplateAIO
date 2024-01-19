﻿//#define LOG_TCPCLIENT_SEND_DATA
#if UNITY_IOS && !UNITY_EDITOR
#define SOCKET_SEND_EXPLICIT_ORDER
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngineEx;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.Collections.Concurrent;
#else
using System.Collections.Concurrent;
#endif
#else
using System.Collections.Concurrent;
#endif

using PlatDependant = UnityEngineEx.PlatDependant;
using TaskProgress = UnityEngineEx.TaskProgress;

namespace ModNet
{
    public class TCPClient : ICustomSendConnection, IPositiveConnection, IDisposable
    {
        private string _Url;
        protected ReceiveHandler _OnReceive;
        //protected SendCompleteHandler _OnSendComplete;
        protected CommonHandler _OnClose;
        protected SendHandler _OnSend;
        protected UpdateHandler _OnUpdate;
        protected bool _PositiveMode;

        protected TCPClient() { }
        public TCPClient(string url)
        {
            _Url = url;
        }

        public string Url
        {
            get { return _Url; }
            set
            {
                if (value != _Url)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change url when connection started");
                    }
                    else
                    {
                        _Url = value;
                    }
                }
            }
        }
        /// <summary>
        /// This will be called in connection thread.
        /// </summary>
        public ReceiveHandler OnReceive
        {
            get { return _OnReceive; }
            set
            {
                if (value != _OnReceive)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change OnReceive when connection started");
                    }
                    else
                    {
                        _OnReceive = value;
                    }
                }
            }
        }
        ///// <summary>
        ///// This will be called in undetermined thread.
        ///// </summary>
        //public SendCompleteHandler OnSendComplete
        //{
        //    get { return _OnSendComplete; }
        //    set
        //    {
        //        if (value != _OnSendComplete)
        //        {
        //            if (IsConnectionAlive)
        //            {
        //                PlatDependant.LogError("Cannot change OnSendComplete when connection started");
        //            }
        //            else
        //            {
        //                _OnSendComplete = value;
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// This will be called in connection thread.
        /// </summary>
        public CommonHandler OnClose
        {
            get { return _OnClose; }
            set
            {
                if (value != _OnClose)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change PreDispose when connection started");
                    }
                    else
                    {
                        _OnClose = value;
                    }
                }
            }
        }
        /// <summary>
        /// This will be called in connection thread.
        /// </summary>
        public SendHandler OnSend
        {
            get { return _OnSend; }
            set
            {
                if (value != _OnSend)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change OnSend when connection started");
                    }
                    else
                    {
                        _OnSend = value;
                    }
                }
            }
        }
        /// <summary>
        /// This will be called in connection thread.
        /// </summary>
        public UpdateHandler OnUpdate
        {
            get { return _OnUpdate; }
            set
            {
                if (value != _OnUpdate)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change OnUpdate when connection started");
                    }
                    else
                    {
                        _OnUpdate = value;
                    }
                }
            }
        }
        public bool PositiveMode
        {
            get { return _PositiveMode; }
            set
            {
                if (value != _PositiveMode)
                {
                    if (IsStarted)
                    {
                        PlatDependant.LogError("Cannot change PositiveMode when connection started");
                    }
                    else
                    {
                        _PositiveMode = value;
                    }
                }
            }
        }

        protected volatile bool _ConnectWorkStarted;
        protected volatile bool _ConnectWorkFinished;
        protected Socket _Socket;
        public EndPoint RemoteEndPoint
        {
            get
            {
                if (_Socket != null)
                {
                    return _Socket.RemoteEndPoint;
                }
                return null;
            }
        }

        public bool IsAlive
        {
            get { return !_ConnectWorkFinished; }
        }
        public bool IsStarted
        {
            get { return _ConnectWorkStarted || _ConnectWorkFinished; }
        }
        protected IEnumerator _ConnectWork;
        public void Start()
        {
            if (!IsStarted)
            {
                _ConnectWorkStarted = true;
                if (_PositiveMode)
                {
                    _ConnectWork = ConnectWork();
                }
                else
                {
                    PlatDependant.RunBackgroundLongTime(prog =>
                    {
                        var work = ConnectWork();
                        while (work.MoveNext()) ;
                    });
                }
            }
        }
        public void Step()
        {
            if (_PositiveMode)
            {
                if (_ConnectWork != null)
                {
                    if (!_ConnectWork.MoveNext())
                    {
                        _ConnectWork = null;
                    }
                }
            }
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        protected ConcurrentQueueGrowOnly<MessageInfo> _PendingSendMessages = new ConcurrentQueueGrowOnly<MessageInfo>();
        protected ConcurrentQueueGrowOnly<ValueList<PooledBufferSpan>> _PendingRecvMessages = new ConcurrentQueueGrowOnly<ValueList<PooledBufferSpan>>();
#else
        protected ConcurrentQueue<MessageInfo> _PendingSendMessages = new ConcurrentQueue<MessageInfo>();
        protected ConcurrentQueue<ValueList<PooledBufferSpan>> _PendingRecvMessages = new ConcurrentQueue<ValueList<PooledBufferSpan>>();
#endif
        protected AutoResetEvent _HaveDataToSend = new AutoResetEvent(false);
        /// <summary>
        /// Schedule sending the data. Handle OnSendComplete to recyle the data buffer.
        /// </summary>
        /// <param name="data">data to be sent.</param>
        /// <returns>false means the data is dropped because to many messages is pending to be sent.</returns>
        public virtual bool TrySend(MessageInfo minfo)
        {
            if (Thread.CurrentThread.ManagedThreadId == _ConnectionThreadID || _PositiveMode)
            {
                DoSendWork(minfo);
            }
            else
            {
                _PendingSendMessages.Enqueue(minfo);
                _HaveDataToSend.Set();
                //StartConnect();
            }
            return true;
        }
        public void Send(IPooledBuffer data, int cnt)
        {
            TrySend(new MessageInfo(data, cnt));
        }
        public void Send(ValueList<PooledBufferSpan> data)
        {
            TrySend(new MessageInfo(data));
        }
        public void Send(object raw, SendSerializer serializer)
        {
            TrySend(new MessageInfo(raw, serializer));
        }
        public void Send(byte[] data, int cnt)
        {
            Send(new UnpooledBuffer(data), cnt);
        }
        public void Send(byte[] data)
        {
            Send(data, data.Length);
        }

#if LOG_TCPCLIENT_SEND_DATA
        private string _LogFilePath;
        public string LogFilePath
        {
            get
            {
                if (_LogFilePath == null)
                {
                    _LogFilePath = System.IO.Path.Combine(ThreadSafeValues.LogPath, $"rec/tcpdata{DateTime.Now:ddHHmmss}.bin");
                }
                return _LogFilePath;
            }
        }
#endif

#if SOCKET_SEND_EXPLICIT_ORDER
        protected AutoResetEvent _AsyncSendWaitHandle = new AutoResetEvent(true);
#else
        protected Semaphore _AsyncSendWaitHandle = new Semaphore(4, 4);
#endif
        /// <summary>
        /// This should be called in connection thread. Real send data to server. The sending will NOT be done immediately, and we should NOT reuse data before onComplete.
        /// </summary>
        /// <param name="data">data to send.</param>
        /// <param name="cnt">data count in bytes.</param>
        /// <param name="onComplete">this will be called in some other thread.</param>
        public void SendRaw(IPooledBuffer data, int cnt, Action<bool> onComplete)
        {
            data.AddRef();
            if (_Socket != null)
            {
#if LOG_TCPCLIENT_SEND_DATA
                using (var file_stream = PlatDependant.OpenAppend(LogFilePath))
                {
                    file_stream.Write(data.Buffer, 0, cnt);
                }
#endif

#if SOCKET_SEND_USE_BLOCKING_INSTEAD_OF_ASYNC
                try
                {
                    _Socket.Send(data.Buffer, 0, cnt, SocketFlags.None);
                    if (onComplete != null)
                    {
                        onComplete(true);
                    }
                    data.Release();
                    return;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#else
                UDPClient.SendAsyncInfo info = null;
                try
                {
                    _AsyncSendWaitHandle.WaitOne();
                    info = UDPClient.GetSendAsyncInfoFromPool();
                    info.AsyncSendWaitHandle = _AsyncSendWaitHandle;
                    info.Data = data;
                    info.Socket = _Socket;
                    info.OnComplete = onComplete;
                    info.IsBinded = true;
                    _Socket.BeginSend(data.Buffer, 0, cnt, SocketFlags.None, info.OnAsyncCallback, null);
                    return;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
#if SOCKET_SEND_EXPLICIT_ORDER
                    _AsyncSendWaitHandle.Set();
#else
                    _AsyncSendWaitHandle.Release();
#endif
                    UDPClient.ReturnSendAsyncInfoToPool(info);
                }
#endif
            }
            if (onComplete != null)
            {
                onComplete(false);
            }
            data.Release();
        }
        public void SendRaw(IPooledBuffer data, int cnt)
        {
            SendRaw(data, cnt, null);
        }
        public void SendRaw(IPooledBuffer data)
        {
            SendRaw(data, data.Buffer.Length);
        }
        //public void SendRaw(byte[] data, int cnt, Action onComplete)
        //{
        //    SendRaw(data, cnt, onComplete == null ? null : (Action<bool>)(success => onComplete()));
        //}
        public void SendRaw(byte[] data, int cnt, Action<bool> onComplete)
        {
            SendRaw(new UnpooledBuffer(data), cnt, onComplete);
        }
        public void SendRaw(byte[] data, int cnt)
        {
            SendRaw(data, cnt, null);
        }
        public void SendRaw(byte[] data)
        {
            SendRaw(data, data.Length);
        }

        protected byte[] _ReceiveBuffer = new byte[CONST.MTU];
        protected void EndReceive(IAsyncResult ar)
        {
            try
            {
                var receivecnt = _Socket.EndReceive(ar);
                if (receivecnt > 0)
                {
                    var bytesRemaining = _Socket.Available;
                    if (bytesRemaining > 0)
                    {
                        if (bytesRemaining > CONST.MTU - 1)
                        {
                            bytesRemaining = CONST.MTU - 1;
                        }
                        receivecnt += _Socket.Receive(_ReceiveBuffer, 1, bytesRemaining, SocketFlags.None);
                    }
                    _PendingRecvMessages.Enqueue(BufferPool.GetPooledBufferList(_ReceiveBuffer, 0, receivecnt));

                    if (!_ConnectWorkFinished)
                    {
                        BeginReceive();
                    }
                }
                else
                {
                    if (_ConnectWorkStarted)
                    {
                        _ConnectWorkFinished = true;
                    }
                }
            }
            catch (Exception e)
            {
                if (IsAlive)
                {
                    _ConnectWorkFinished = true;
                    PlatDependant.LogError(e);
                }
            }
            _HaveDataToSend.Set();
        }
        protected AsyncCallback EndReceiveFunc;
        protected void BeginReceive()
        {
            try
            {
                var cb = EndReceiveFunc = EndReceiveFunc ?? EndReceive;
                _Socket.BeginReceive(_ReceiveBuffer, 0, 1, SocketFlags.None, cb, null);
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
        }

        protected virtual void PrepareSocket()
        {
            if (_Url != null)
            {
                Uri uri = new Uri(_Url);
                var addresses = Dns.GetHostAddresses(uri.DnsSafeHost);
                if (addresses != null && addresses.Length > 0)
                {
                    var address = addresses[0];
                    _Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _Socket.Connect(address, uri.Port);
                }
            }
        }

        protected int _ConnectionThreadID;
        protected void DoSendWork(MessageInfo minfo)
        {
            ValueList<PooledBufferSpan> messages;
            if (minfo.Serializer != null)
            {
                messages = minfo.Serializer(minfo.Raw);
            }
            else
            {
                messages = minfo.Buffers;
            }
            for (int i = 0; i < messages.Count; ++i)
            {
                var message = messages[i];
                DoSendWork(message);
            }
        }
        protected void DoSendWork(PooledBufferSpan message)
        {
            var cnt = message.Length;
            if (_OnSend != null && _OnSend(message, cnt))
            {
                //if (_OnSendComplete != null)
                //{
                //    _OnSendComplete(true);
                //}
            }
            else
            {
                SendRaw(message, cnt
                    //, success =>
                    //{
                    //    if (_OnSendComplete != null)
                    //    {
                    //        _OnSendComplete(message, success);
                    //    }
                    //}
                    );
            }
            message.Release();
        }
        protected virtual IEnumerator ConnectWork()
        {
            try
            {
                _ConnectionThreadID = Thread.CurrentThread.ManagedThreadId;
                try
                {
                    PrepareSocket();
                }
                catch (ThreadAbortException)
                {
                    if (!_PositiveMode)
                    {
                        Thread.ResetAbort();
                    }
                    yield break;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                    yield break;
                }
                if (_Socket != null)
                {
                    BeginReceive();
                    while (!_ConnectWorkFinished)
                    {
                        int waitinterval;
                        try
                        {
                            ValueList<PooledBufferSpan> recvmessages;
                            while (_PendingRecvMessages.TryDequeue(out recvmessages))
                            {
                                for (int i = 0; i < recvmessages.Count; ++i)
                                {
                                    var message = recvmessages[i];
                                    if (_OnReceive != null)
                                    {
                                        _OnReceive(message.Buffer, message.Length, _Socket.RemoteEndPoint);
                                    }
                                    message.Release();
                                }
                            }

                            MessageInfo minfo;
                            while (_PendingSendMessages.TryDequeue(out minfo))
                            {
                                DoSendWork(minfo);
                            }

                            waitinterval = int.MinValue;
                            if (_OnUpdate != null)
                            {
                                waitinterval = _OnUpdate(this);
                            }
                            if (waitinterval < 0)
                            {
                                waitinterval = CONST.MAX_WAIT_MILLISECONDS;
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            if (!_PositiveMode)
                            {
                                Thread.ResetAbort();
                            }
                            yield break;
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                            yield break;
                        }
                        if (_HaveDataToSend.WaitOne(0))
                        {
                            continue;
                        }
                        if (_PositiveMode)
                        {
                            yield return null;
                        }
                        else
                        {
                            _HaveDataToSend.WaitOne(waitinterval);
                        }
                    }
                    _Socket.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                _ConnectWorkFinished = true;
                //_ConnectWorkRunning = false;
                //_ConnectWorkCanceled = false;
                if (_OnClose != null)
                {
                    _OnClose(this);
                }
                if (_Socket != null)
                {
                    _Socket.Close();
                    _Socket = null;
                }
                // set handlers to null.
                _OnReceive = null;
                _OnSend = null;
                _OnUpdate = null;
                //_OnSendComplete = null;
                _OnClose = null;
            }
        }

        public void Dispose()
        {
            Dispose(false);
        }
        public void Dispose(bool inFinalizer)
        {
            if (_ConnectWorkStarted)
            {
                _ConnectWorkFinished = true;
                if (_PositiveMode)
                {
                    var disposable = _ConnectWork as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                    _ConnectWork = null;
                }
                else
                {
                    _HaveDataToSend.Set();
                }
            }
            if (!inFinalizer)
            {
                GC.SuppressFinalize(this);
            }
        }
        ~TCPClient()
        {
            Dispose(true);
        }
    }
}
