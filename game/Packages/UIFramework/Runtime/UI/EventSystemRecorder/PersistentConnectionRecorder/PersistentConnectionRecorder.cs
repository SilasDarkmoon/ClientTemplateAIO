using ModNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityEngineEx
{
    public class PersistentConnectionRecorder : ISafeReqClientAttachment
    {
        private IReqConnection _Parent;
        private CarbonFormatter _Formatter;
        private CarbonComposer _Composer;

        public void Attach(IReqConnection safeclient)
        {
            AttachRaw(safeclient);
            if (_IsRecording)
            {
                RecordSaver.EnqueueRecordOfProgress("NetCon", "");
            }
        }
        public void AttachRaw(IReqConnection safeclient)
        {
            _Parent = safeclient;
            _Formatter = new CarbonFormatter();
            _Composer = new CarbonComposer();
            _Parent.RegHandler(HandleRequest_Record);
        }

        [EventOrder(int.MinValue)]
        public object HandleRequest_Record(IReqClient from, uint type, object reqobj, uint seq)
        {
            if (_IsRecording)
            {
                if (!(reqobj is CarbonMessage))
                {
                    var exFlags = _Formatter.GetExFlags(reqobj);
                    // write obj
                    var stream = _Formatter.Write(reqobj);
                    if (stream != null)
                    {
                        // compose block
                        _Composer.PrepareBlock(stream, type, 0, seq, 0, exFlags);
                        // record
                        byte[] rdata = new byte[stream.Count];
                        stream.CopyTo(rdata, 0);
                        string tag = reqobj.GetType().Name;
                        if (reqobj is PredefinedMessages.Control)
                        {
                            var ctrl = (PredefinedMessages.Control)reqobj;
                            tag = "Ctrl" + ctrl.Code;
                        }
                        RecordSaver.EnqueueRecordOfProgress("Net", tag, rdata);
                    }
                }
            }
            return null;
        }

        private static ConnectionFactory.ClientAttachmentCreator _AttachmentCreator = new ConnectionFactory.ClientAttachmentCreator("Recorder", client => new PersistentConnectionRecorder());

        private static bool _IsRecording = false;
        public static void StartRecord()
        {
            _IsRecording = true;

            var conconfig = CarbonMessageUtils.ConnectionConfig;
            var attach = conconfig.ClientAttachmentCreators;
            if (!attach.Contains(_AttachmentCreator))
            {
                var newattach = new ConnectionFactory.ClientAttachmentCreator[attach.Count + 1];
                attach.CopyTo(newattach, 0);
                newattach[attach.Count] = _AttachmentCreator;
                conconfig.ClientAttachmentCreators = newattach;

                var recorder = new PersistentConnectionRecorder();
                var client = CarbonMessageUtils.CarbonPushConnection;
                client.SetAttachment("Recorder", recorder);
                recorder.AttachRaw(client);
            }
        }
        public static void StopRecord()
        {
            _IsRecording = false;
        }
    }
}