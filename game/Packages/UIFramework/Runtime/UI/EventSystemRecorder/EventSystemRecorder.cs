using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngineEx
{
    public static class EventSystemRecorder
    {
        public static RecordedInputData Data;

        public static void NewRecord()
        {
            Data = ScriptableObject.CreateInstance<RecordedInputData>();
        }

        public static void LoadRecord()
        {
            Data = InputRecordSaver.LoadFile();
        }

        public static void Stop()
        {
            _CurrentTimer = null;
            var es = EventSystem.current;
            if (es)
            {
                if (es.currentInputModule)
                {
                    es.currentInputModule.inputOverride = null;
                }
                else
                {
                    var module = es.GetComponent<BaseInputModule>();
                    if (module)
                    {
                        module.inputOverride = null;
                    }
                }

                var saver = es.GetComponent<InputRecordSaver>();
                if (saver)
                {
                    GameObject.Destroy(saver);
                }

                RecordSaver.Stop();
            }
        }

        private static InputTimer _CurrentTimer;
        public static void StartRecord()
        {
            var es = EventSystem.current;
            if (es)
            {
                RecordSaver.StartNew();

                var saver = es.GetComponent<InputRecordSaver>();
                if (!saver)
                {
                    saver = es.gameObject.AddComponent<InputRecordSaver>();
                }


                var recorder = es.GetComponent<RecorderInput>();
                if (!recorder)
                {
                    recorder = es.gameObject.AddComponent<RecorderInput>();
                }
                recorder.Data = Data;
                recorder.Recorder.StopProgress();
                recorder.Recorder.StartNextProgress();
                _CurrentTimer = recorder.Recorder;

                es.currentInputModule.inputOverride = recorder;
            }
        }

        public static void StartPlayback()
        {
            var es = EventSystem.current;
            if (es)
            {
                var player = es.GetComponent<PlaybackInput>();
                if (!player)
                {
                    player = es.gameObject.AddComponent<PlaybackInput>();
                }
                player.Data = Data;
                player.Player.StopProgress();
                player.Player.StartNextProgress();
                _CurrentTimer = player.Player;

                es.currentInputModule.inputOverride = player;
            }
        }

        public static void StartNextProgress()
        {
            if (_CurrentTimer != null)
            {
                _CurrentTimer.StartNextProgress();
            }
        }

        public static float RealtimeInCurrentFrame
        {
            get
            {
                if (_CurrentTimer == null)
                {
                    return Time.realtimeSinceStartup;
                }
                else
                {
                    return _CurrentTimer.RealtimeInCurrentFrame;
                }
            }
        }

        public static int? ProgressIndex
        {
            get
            {
                if (_CurrentTimer == null)
                {
                    return null;
                }
                else
                {
                    return _CurrentTimer.GetProgressIndex();
                }
            }
        }

        public static float? ProgressStartTime
        {
            get
            {
                if (_CurrentTimer == null)
                {
                    return null;
                }
                else
                {
                    return _CurrentTimer.GetProgressStartTime();
                }
            }
        }
    }
}