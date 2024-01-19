using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngineEx
{
    public class RecorderInput : BaseInput
    {
        protected RecordedInputCache _Cache;
        protected InputRecorder _Recorder;
        public InputRecorder Recorder { get { return _Recorder; } }
        protected RecordedInputData _Data;
        public RecordedInputData Data
        {
            get { return _Data; }
            set
            {
                _Data = value;
                if (_Recorder != null)
                {
                    _Recorder.Data = value;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _Cache = new RecordedInputCache();
            _Recorder = new InputRecorder(_Data);
        }

        public override string compositionString
        {
            get
            {
                var value = base.compositionString;
                if (_Cache.SetCompositionString(value))
                {
                    _Recorder.Record(RecordedInputFrameType.CompositionString, value);
                }
                return value;
            }
        }

        public override IMECompositionMode imeCompositionMode
        {
            get
            {
                var value = base.imeCompositionMode;
                if (_Cache.SetIMECompositionMode(value))
                {
                    _Recorder.Record(RecordedInputFrameType.GetImeCompositionMode, value);
                }
                return value;
            }
            set
            {
                base.imeCompositionMode = value;
                if (_Cache.SetIMECompositionMode(value))
                {
                    _Recorder.Record(RecordedInputFrameType.SetImeCompositionMode, value);
                }
            }
        }

        public override Vector2 compositionCursorPos
        {
            get
            {
                var value = base.compositionCursorPos;
                if (_Cache.SetCompositionCursorPos(value))
                {
                    _Recorder.Record(RecordedInputFrameType.GetCompositionCursorPos, value);
                }
                return value;
            }
            set
            {
                base.compositionCursorPos = value;
                if (_Cache.SetCompositionCursorPos(value))
                {
                    _Recorder.Record(RecordedInputFrameType.SetCompositionCursorPos, value);
                }
            }
        }

        public override bool mousePresent
        {
            get
            {
                var value = base.mousePresent;
                if (_Cache.SetMousePresent(value))
                {
                    _Recorder.Record(RecordedInputFrameType.MousePresent, value);
                }
                return value;
            }
        }

        public override bool GetMouseButtonDown(int button)
        {
            var value = base.GetMouseButtonDown(button);
            if (_Cache.SetMouseButtonDown(button, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetMouseButtonDown, button, value);
            }
            return value;
        }

        public override bool GetMouseButtonUp(int button)
        {
            var value = base.GetMouseButtonUp(button);
            if (_Cache.SetMouseButtonUp(button, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetMouseButtonUp, button, value);
            }
            return value;
        }

        public override bool GetMouseButton(int button)
        {
            var value = base.GetMouseButton(button);
            if (_Cache.SetMouseButton(button, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetMouseButton, button, value);
            }
            return value;
        }

        public override Vector2 mousePosition
        {
            get
            {
                var value = base.mousePosition;
                if (_Cache.SetMousePosition(value))
                {
                    _Recorder.Record(RecordedInputFrameType.MousePosition, value);
                }
                return value;
            }
        }

        public override Vector2 mouseScrollDelta
        {
            get
            {
                var value = base.mouseScrollDelta;
                if (_Cache.SetMouseScrollDelta(value))
                {
                    _Recorder.Record(RecordedInputFrameType.MouseScrollDelta, value);
                }
                return value;
            }
        }

        public override bool touchSupported
        {
            get
            {
                var value = base.touchSupported;
                if (_Cache.SetTouchSupported(value))
                {
                    _Recorder.Record(RecordedInputFrameType.TouchSupported, value);
                }
                return value;
            }
        }

        public override int touchCount
        {
            get
            {
                var value = base.touchCount;
                if (_Cache.SetTouchCount(value))
                {
                    _Recorder.Record(RecordedInputFrameType.TouchCount, value);
                }
                return value;
            }
        }

        public override Touch GetTouch(int index)
        {
            var value = base.GetTouch(index);
            if (_Cache.SetTouch(index, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetTouch, index, value);
            }
            return value;
        }

        public override float GetAxisRaw(string axisName)
        {
            var value = base.GetAxisRaw(axisName);
            if (_Cache.SetAxisRaw(axisName, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetAxisRaw, axisName, value);
            }
            return value;
        }

        public override bool GetButtonDown(string buttonName)
        {
            var value = base.GetButtonDown(buttonName);
            if (_Cache.SetButtonDown(buttonName, value))
            {
                _Recorder.Record(RecordedInputFrameType.GetButtonDown, buttonName, value);
            }
            return value;
        }
    }
}