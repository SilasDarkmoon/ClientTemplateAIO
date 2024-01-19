using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngineEx
{
    [Serializable]
    public struct RecordedTouch : IEquatable<RecordedTouch>
    {
        public int FingerId;
        public Vector2 Position;
        public Vector2 RawPosition;
        public Vector2 PositionDelta;
        public float TimeDelta;
        public int TapCount;
        public int Phase; // TouchPhase
        public int Type; // TouchType
        public float Pressure;
        public float MaximumPossiblePressure;
        public float Radius;
        public float RadiusVariance;
        public float AltitudeAngle;
        public float AzimuthAngle;

        public static implicit operator RecordedTouch(Touch t)
        {
            return new RecordedTouch()
            {
                FingerId = t.fingerId,
                Position = t.position,
                RawPosition = t.rawPosition,
                PositionDelta = t.deltaPosition,
                TimeDelta = t.deltaTime,
                TapCount = t.tapCount,
                Phase = (int)t.phase,
                Type = (int)t.type,
                Pressure = t.pressure,
                MaximumPossiblePressure = t.maximumPossiblePressure,
                Radius = t.radius,
                RadiusVariance = t.radiusVariance,
                AltitudeAngle = t.altitudeAngle,
                AzimuthAngle = t.azimuthAngle,
            };
        }
        public static implicit operator Touch(RecordedTouch t)
        {
            return new Touch()
            {
                fingerId = t.FingerId,
                position = t.Position,
                rawPosition = t.RawPosition,
                deltaPosition = t.PositionDelta,
                deltaTime = t.TimeDelta,
                tapCount = t.TapCount,
                phase = (TouchPhase)t.Phase,
                type = (TouchType)t.Type,
                pressure = t.Pressure,
                maximumPossiblePressure = t.MaximumPossiblePressure,
                radius = t.Radius,
                radiusVariance = t.RadiusVariance,
                altitudeAngle = t.AltitudeAngle,
                azimuthAngle = t.AzimuthAngle,
            };
        }

        public bool Equals(RecordedTouch other)
        {
            return this.FingerId == other.FingerId
                && this.Position == other.Position
                && this.RawPosition == other.RawPosition
                && this.PositionDelta == other.PositionDelta
                && this.TimeDelta == other.TimeDelta
                && this.TapCount == other.TapCount
                && this.Phase == other.Phase
                && this.Type == other.Type
                && this.Pressure == other.Pressure
                && this.MaximumPossiblePressure == other.MaximumPossiblePressure
                && this.Radius == other.Radius
                && this.RadiusVariance == other.RadiusVariance
                && this.AltitudeAngle == other.AltitudeAngle
                && this.AzimuthAngle == other.AzimuthAngle;
        }
    }

    [Serializable]
    public enum RecordedInputFrameType
    {
        Unknown = 0,
        CompositionString,
        GetImeCompositionMode,
        SetImeCompositionMode,
        GetCompositionCursorPos,
        SetCompositionCursorPos,
        MousePresent,
        GetMouseButtonDown,
        GetMouseButtonUp,
        GetMouseButton,
        MousePosition,
        MouseScrollDelta,
        TouchSupported,
        TouchCount,
        GetTouch,
        GetAxisRaw,
        GetButtonDown,
    }

    [Serializable]
    public class RecordedInputFrame
    {
        public int ProgressIndex;
        public float Time;

        public RecordedInputFrameType FrameType;

        public string StringValue;
        public int IntValue;
        public Vector2 V2Value;
        public bool BoolValue;
        public float FloatValue;
        public RecordedTouch TouchValue;
    }

    public class RecordedInputData : ScriptableObject
    {
        public List<RecordedInputFrame> Frames;
    }

    public abstract class InputTimer
    {
        protected RecordedInputData _Data;
        public RecordedInputData Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        protected int? _ProgressIndex;
        protected float? _StartTime;
        protected int _UnityFrameIndex = -1;
        protected float _RealtimeInCurrentFrame;
        public float RealtimeInCurrentFrame
        {
            get
            {
                var frameindex = Time.frameCount;
                if (frameindex != _UnityFrameIndex)
                {
                    _UnityFrameIndex = frameindex;
                    _RealtimeInCurrentFrame = Time.realtimeSinceStartup;
                }
                return _RealtimeInCurrentFrame;
            }
        }

        public void StartNextProgress()
        {
            if (_ProgressIndex == null)
            {
                _ProgressIndex = 0;
            }
            else
            {
                ++_ProgressIndex;
            }
            _StartTime = RealtimeInCurrentFrame;
        }
        public void StartProgress(int progressIndex)
        {
            _ProgressIndex = progressIndex;
            _StartTime = RealtimeInCurrentFrame;
        }
        public void StartProgress(int progressIndex, float startTime)
        {
            _ProgressIndex = progressIndex;
            _StartTime = startTime;
        }
        public void StopProgress()
        {
            _ProgressIndex = null;
            _StartTime = null;
        }
        public int? GetProgressIndex()
        {
            return _ProgressIndex;
        }
        public float? GetProgressStartTime()
        {
            return _StartTime;
        }
    }

    public class InputRecorder : InputTimer
    {
        public InputRecorder()
        {
            _Data = ScriptableObject.CreateInstance<RecordedInputData>();
            _Data.Frames = new List<RecordedInputFrame>();
        }
        public InputRecorder(RecordedInputData data)
        {
            if (data == null)
            {
                _Data = ScriptableObject.CreateInstance<RecordedInputData>();
                _Data.Frames = new List<RecordedInputFrame>();
            }
            else
            {
                _Data = data;
                if (_Data.Frames == null)
                {
                    _Data.Frames = new List<RecordedInputFrame>();
                }
            }
        }

        protected void AddFrameRaw(RecordedInputFrame frame)
        {
            if (_Data.Frames == null)
            {
                _Data.Frames = new List<RecordedInputFrame>();
            }
            _Data.Frames.Add(frame);
        }
        protected void AddFrame(RecordedInputFrame frame)
        {
            if (_ProgressIndex != null)
            {
                frame.ProgressIndex = (int)_ProgressIndex;
            }
            if (_StartTime != null)
            {
                frame.Time = RealtimeInCurrentFrame - (float)_StartTime;
            }
            AddFrameRaw(frame);
        }
        public void Record(RecordedInputFrameType type, string value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                StringValue = value,
            });
        }
        public void Record<T>(RecordedInputFrameType type, T value) where T : struct, Enum
        {
            var intval = (int)EnumUtils.ConvertFromEnum(value);
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                IntValue = intval,
            });
        }
        public void Record(RecordedInputFrameType type, Vector2 value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                V2Value = value,
            });
        }
        public void Record(RecordedInputFrameType type, bool value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                BoolValue = value,
            });
        }
        public void Record(RecordedInputFrameType type, int index, bool value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                IntValue = index,
                BoolValue = value,
            });
        }
        public void Record(RecordedInputFrameType type, int value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                IntValue = value,
            });
        }
        public void Record(RecordedInputFrameType type, int index, Touch value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                IntValue = index,
                TouchValue = value,
            });
        }
        public void Record(RecordedInputFrameType type, string index, float value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                StringValue = index,
                FloatValue = value,
            });
        }
        public void Record(RecordedInputFrameType type, string index, bool value)
        {
            AddFrame(new RecordedInputFrame()
            {
                FrameType = type,
                StringValue = index,
                BoolValue = value,
            });
        }
    }

    public class RecordedInputCache
    {
        public static bool TrySet<T>(ref T field, T value) where T : IEquatable<T>
        {
            if (!field.Equals(value))
            {
                field = value;
                return true;
            }
            return false;
        }
        public static bool TrySetEnum<T>(ref T field, T value) where T : struct, Enum
        {
            if (EnumUtils.ConvertFromEnum(field) != EnumUtils.ConvertFromEnum(value))
            {
                field = value;
                return true;
            }
            return false;
        }
        public static bool TrySetObject<T>(ref T field, T value) where T : class
        {
            if (!ReferenceEquals(field, value))
            {
                field = value;
                return true;
            }
            return false;
        }

        public static TV TryGet<TK, TV>(IDictionary<TK, TV> dict, TK key)
        {
            if (dict == null)
            {
                return default(TV);
            }
            TV value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            return default(TV);
        }
        public static bool TrySet<TK, TV, TDict>(ref TDict dict, TK key, TV value) where TV : IEquatable<TV> where TDict : IDictionary<TK, TV>, new()
        {
            if (dict == null)
            {
                dict = new TDict();
                dict[key] = value;
                return true;
            }
            if (!dict.ContainsKey(key) || !dict[key].Equals(value))
            {
                dict[key] = value;
                return true;
            }
            return false;
        }

        protected string _CompositionString;
        public string GetCompositionString()
        {
            return _CompositionString;
        }
        public bool SetCompositionString(string value)
        {
            return TrySet(ref _CompositionString, value);
        }

        protected IMECompositionMode _ImeCompositionMode;
        public IMECompositionMode GetIMECompositionMode()
        {
            return _ImeCompositionMode;
        }
        public bool SetIMECompositionMode(IMECompositionMode value)
        {
            return TrySetEnum(ref _ImeCompositionMode, value);
        }

        protected Vector2 _CompositionCursorPos;
        public Vector2 GetCompositionCursorPos()
        {
            return _CompositionCursorPos;
        }
        public bool SetCompositionCursorPos(Vector2 value)
        {
            return TrySet(ref _CompositionCursorPos, value);
        }

        protected bool _MousePresent;
        public bool GetMousePresent()
        {
            return _MousePresent;
        }
        public bool SetMousePresent(bool value)
        {
            return TrySet(ref _MousePresent, value);
        }

        protected Dictionary<int, bool> _MouseButtonDownDict;
        public bool GetMouseButtonDown(int button)
        {
            return TryGet(_MouseButtonDownDict, button);
        }
        public bool SetMouseButtonDown(int button, bool value)
        {
            return TrySet(ref _MouseButtonDownDict, button, value);
        }

        protected Dictionary<int, bool> _MouseButtonUpDict;
        public bool GetMouseButtonUp(int button)
        {
            return TryGet(_MouseButtonUpDict, button);
        }
        public bool SetMouseButtonUp(int button, bool value)
        {
            return TrySet(ref _MouseButtonUpDict, button, value);
        }

        protected Dictionary<int, bool> _MouseButtonDict;
        public bool GetMouseButton(int button)
        {
            return TryGet(_MouseButtonDict, button);
        }
        public bool SetMouseButton(int button, bool value)
        {
            return TrySet(ref _MouseButtonDict, button, value);
        }

        protected Vector2 _MousePosition;
        public Vector2 GetMousePosition()
        {
            return _MousePosition;
        }
        public bool SetMousePosition(Vector2 value)
        {
            return TrySet(ref _MousePosition, value);
        }

        protected Vector2 _MouseScrollDelta;
        public Vector2 GetMouseScrollDelta()
        {
            return _MouseScrollDelta;
        }
        public bool SetMouseScrollDelta(Vector2 value)
        {
            return TrySet(ref _MouseScrollDelta, value);
        }

        protected bool _TouchSupported;
        public bool GetTouchSupported()
        {
            return _TouchSupported;
        }
        public bool SetTouchSupported(bool value)
        {
            return TrySet(ref _TouchSupported, value);
        }

        protected int _TouchCount;
        public int GetTouchCount()
        {
            return _TouchCount;
        }
        public bool SetTouchCount(int value)
        {
            return TrySet(ref _TouchCount, value);
        }

        protected Dictionary<int, RecordedTouch> _TouchDict;
        public Touch GetTouch(int index)
        {
            return TryGet(_TouchDict, index);
        }
        public bool SetTouch(int index, Touch touch)
        {
            return TrySet(ref _TouchDict, index, (RecordedTouch)touch);
        }

        protected Dictionary<string, float> _AxisRawDict;
        public float GetAxisRaw(string axisName)
        {
            return TryGet(_AxisRawDict, axisName);
        }
        public bool SetAxisRaw(string axisName, float value)
        {
            return TrySet(ref _AxisRawDict, axisName, value);
        }

        protected Dictionary<string, bool> _ButtonDownDict;
        public bool GetButtonDown(string buttonName)
        {
            return TryGet(_ButtonDownDict, buttonName);
        }
        public bool SetButtonDown(string buttonName, bool value)
        {
            return TrySet(ref _ButtonDownDict, buttonName, value);
        }
    }

    public class InputRecordPlayer : InputTimer
    {
        public InputRecordPlayer() { }
        public InputRecordPlayer(RecordedInputData data)
        {
            _Data = data;
        }

        private RecordedInputCache _Cache = new RecordedInputCache();
        public RecordedInputCache Cache { get { return _Cache; } }

        public bool IsTimeToProcess(RecordedInputFrame frame)
        {
            if (_StartTime == null || _ProgressIndex == null)
            {
                return false;
            }
            if (frame.ProgressIndex < (int)_ProgressIndex)
            {
                return true;
            }
            if (frame.ProgressIndex > (int)_ProgressIndex)
            {
                return false;
            }
            var timestamp = RealtimeInCurrentFrame - (float)_StartTime;
            if (frame.Time <= timestamp)
            {
                return true;
            }
            return false;
        }
        protected void ProcessRaw(RecordedInputFrame frame)
        {
            switch (frame.FrameType)
            {
                case RecordedInputFrameType.CompositionString:
                    ProcessCompositionString(frame.StringValue);
                    break;
                case RecordedInputFrameType.GetImeCompositionMode:
                    ProcessGetImeCompositionMode(frame.IntValue);
                    break;
                case RecordedInputFrameType.SetImeCompositionMode:
                    ProcessSetImeCompositionMode(frame.IntValue);
                    break;
                case RecordedInputFrameType.GetCompositionCursorPos:
                    ProcessGetCompositionCursorPos(frame.V2Value);
                    break;
                case RecordedInputFrameType.SetCompositionCursorPos:
                    ProcessSetCompositionCursorPos(frame.V2Value);
                    break;
                case RecordedInputFrameType.MousePresent:
                    ProcessMousePresent(frame.BoolValue);
                    break;
                case RecordedInputFrameType.GetMouseButtonDown:
                    ProcessGetMouseButtonDown(frame.IntValue, frame.BoolValue);
                    break;
                case RecordedInputFrameType.GetMouseButtonUp:
                    ProcessGetMouseButtonUp(frame.IntValue, frame.BoolValue);
                    break;
                case RecordedInputFrameType.GetMouseButton:
                    ProcessGetMouseButton(frame.IntValue, frame.BoolValue);
                    break;
                case RecordedInputFrameType.MousePosition:
                    ProcessMousePosition(frame.V2Value);
                    break;
                case RecordedInputFrameType.MouseScrollDelta:
                    ProcessMouseScrollDelta(frame.V2Value);
                    break;
                case RecordedInputFrameType.TouchSupported:
                    ProcessTouchSupported(frame.BoolValue);
                    break;
                case RecordedInputFrameType.TouchCount:
                    ProcessTouchCount(frame.IntValue);
                    break;
                case RecordedInputFrameType.GetTouch:
                    ProcessGetTouch(frame.IntValue, frame.TouchValue);
                    break;
                case RecordedInputFrameType.GetAxisRaw:
                    ProcessGetAxisRaw(frame.StringValue, frame.FloatValue);
                    break;
                case RecordedInputFrameType.GetButtonDown:
                    ProcessGetButtonDown(frame.StringValue, frame.BoolValue);
                    break;
                default:
                    break;
            }
        }
        protected void ProcessCompositionString(string value)
        {
            _Cache.SetCompositionString(value);
        }
        protected void ProcessGetImeCompositionMode(int value)
        {
            _Cache.SetIMECompositionMode((IMECompositionMode)value);
            Input.imeCompositionMode = (IMECompositionMode)value;
        }
        protected void ProcessSetImeCompositionMode(int value)
        {
            _Cache.SetIMECompositionMode((IMECompositionMode)value);
            Input.imeCompositionMode = (IMECompositionMode)value;
        }
        protected void ProcessGetCompositionCursorPos(Vector2 value)
        {
            _Cache.SetCompositionCursorPos(value);
            Input.compositionCursorPos = value;
        }
        protected void ProcessSetCompositionCursorPos(Vector2 value)
        {
            _Cache.SetCompositionCursorPos(value);
            Input.compositionCursorPos = value;
        }
        protected void ProcessMousePresent(bool value)
        {
            _Cache.SetMousePresent(value);
        }
        protected void ProcessGetMouseButtonDown(int button, bool value)
        {
            _Cache.SetMouseButtonDown(button, value);
        }
        protected void ProcessGetMouseButtonUp(int button, bool value)
        {
            _Cache.SetMouseButtonUp(button, value);
        }
        protected void ProcessGetMouseButton(int button, bool value)
        {
            _Cache.SetMouseButton(button, value);
        }
        protected void ProcessMousePosition(Vector2 value)
        {
            _Cache.SetMousePosition(value);
        }
        protected void ProcessMouseScrollDelta(Vector2 value)
        {
            _Cache.SetMouseScrollDelta(value);
        }
        protected void ProcessTouchSupported(bool value)
        {
            _Cache.SetTouchSupported(value);
        }
        protected void ProcessTouchCount(int value)
        {
            _Cache.SetTouchCount(value);
        }
        protected void ProcessGetTouch(int index, RecordedTouch touch)
        {
            _Cache.SetTouch(index, touch);
        }
        protected void ProcessGetAxisRaw(string axisName, float value)
        {
            _Cache.SetAxisRaw(axisName, value);
        }
        protected void ProcessGetButtonDown(string buttonName, bool value)
        {
            _Cache.SetButtonDown(buttonName, value);
        }

        public bool TryProcess(RecordedInputFrame frame)
        {
            if (IsTimeToProcess(frame))
            {
                ProcessRaw(frame);
                return true;
            }
            return false;
        }

        public void Update()
        {
            var data = _Data;
            if (data != null && data.Frames != null)
            {
                int toindex = -1;
                for (int i = 0; i < data.Frames.Count; ++i)
                {
                    var frame = data.Frames[i];
                    if (TryProcess(frame))
                    {
                        toindex = i;
                    }
                    else
                    {
                        break;
                    }
                }
                if (toindex >= 0)
                {
                    data.Frames.RemoveRange(0, toindex + 1);
                }
            }
        }
    }

    public class EncodedDataWithProgress
    {
        public int ProgressIndex;
        public float Time;

        public string Tag;
        public string Encoded;
    }
    public class RawDataWithProgress
    {
        public int ProgressIndex;
        public float Time;

        public string Tag;
        public object Raw;
    }

#if UNITY_INCLUDE_TESTS
    #region TESTS
    public static class RecordedInputDataTest
    {
        public class ByteArrayWrapper
        {
            public byte[] Data;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Test/Input Recorder/Serialize", priority = 300010)]
        public static void TestSerialize()
        {
            var inst = ScriptableObject.CreateInstance<RecordedInputData>();
            inst.Frames = new List<RecordedInputFrame>();
            inst.Frames.Add(new RecordedInputFrame());

            var jsonstr = JsonUtility.ToJson(inst);
            Debug.Log(jsonstr);

            var barr = new ByteArrayWrapper()
            {
                Data = new byte[] { 1, 2, 3 },
            };
            jsonstr = JsonUtility.ToJson(barr);
            Debug.Log(jsonstr);
        }
#endif
    }
    #endregion
#endif
}