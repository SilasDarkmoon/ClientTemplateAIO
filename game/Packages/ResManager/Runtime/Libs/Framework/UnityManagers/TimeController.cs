using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class TimeController
    {
        private static bool _ControllerRunning;
        private static IEnumerator ControllerWork()
        {
            try
            {
                _ControllerRunning = true;
                float target, current;
                while ((target = TargetTimeScale) != (current = Time.timeScale))
                {
                    var step = target;
                    var diff = Mathf.Abs(target - current);
                    var delta = Time.unscaledDeltaTime * ChangeSpeed;
                    if (delta < diff)
                    {
                        step = current + delta * Mathf.Sign(target - current);
                    }
                    Time.timeScale = Mathf.Max(step, MIN_TIME_SCALE);
                    if (step == target && _TimeScaleHistoryList.Count == 1 && _TimeScaleHistoryList.First.Value.Owner == null)
                    {
                        _TimeScaleHistoryList.RemoveFirst();
                        _TargetTimeScale = -1.0f;
                    }
                    yield return null;
                }
            }
            finally
            {
                _ControllerRunning = false;
            }
        }
        private static void RunController()
        {
            if (!_ControllerRunning)
            {
                CoroutineRunner.StartCoroutine(ControllerWork());
            }
        }

        public const float MIN_TIME_SCALE = 0.000001f;
        private static float _ChangeSpeed;
        public static float ChangeSpeed
        {
            get { return _ChangeSpeed <= 0 ? 10 : _ChangeSpeed; }
            set { _ChangeSpeed = value; }
        }

        private struct TimeScaleRecordInfo
        {
            public bool Additive;
            public float NewTimeScale;
            public object Owner;
        }
        private static readonly Dictionary<object, LinkedListNode<TimeScaleRecordInfo>> _TimeScaleHistoryMap = new Dictionary<object, LinkedListNode<TimeScaleRecordInfo>>();
        private static readonly LinkedList<TimeScaleRecordInfo> _TimeScaleHistoryList = new LinkedList<TimeScaleRecordInfo>();
        private static float _TargetTimeScale = -1.0f;
        private static float TargetTimeScale { get { return _TargetTimeScale >= 0 ? _TargetTimeScale : Time.timeScale; } }
        private static float CalculateTargetTimeScale()
        {
            float target;
            if (_TimeScaleHistoryList.Count > 0)
            {
                target = 1.0f;
                var node = _TimeScaleHistoryList.Last;
                while (node != null)
                {
                    var info = node.Value;
                    target *= info.NewTimeScale;
                    if (!info.Additive)
                    {
                        break;
                    }
                    node = node.Previous;
                }
            }
            else
            {
                target = Time.timeScale;
            }
            return Mathf.Max(target, MIN_TIME_SCALE);
        }
        private static bool IsInAdditiveMode
        {
            get
            {
                return _TimeScaleHistoryList.Count > 0 && _TimeScaleHistoryList.Last.Value.Additive;
            }
        }
        private static void AddLastHistoryTimeScale()
        {
            if (_TimeScaleHistoryList.Count == 0)
            {
                _TimeScaleHistoryList.AddLast(new TimeScaleRecordInfo() { NewTimeScale = Time.timeScale });
                _TargetTimeScale = Time.timeScale;
            }
        }

        public static void ChangeTimeScaleDelayed(float ts, object owner, bool additive)
        {
            owner = owner ?? new object();
            if (_TimeScaleHistoryMap.ContainsKey(owner))
            {
                RestoreTimeScaleDelayed(owner);
            }
            //if (IsInAdditiveMode)
            //{
            //    Time.timeScale = TargetTimeScale;
            //}
            //else
            {
                AddLastHistoryTimeScale();
            }
            _TimeScaleHistoryMap[owner] = _TimeScaleHistoryList.AddLast(new TimeScaleRecordInfo() { NewTimeScale = ts, Owner = owner, Additive = additive });
            _TargetTimeScale = CalculateTargetTimeScale();
            RunController();
        }
        public static void ChangeTimeScaleDelayed(float ts, object owner)
        {
            ChangeTimeScaleDelayed(ts, owner, false);
        }
        public static void ChangeTimeScaleDelayed(float ts, bool additive)
        {
            ChangeTimeScaleDelayed(ts, null, additive);
        }
        public static void ChangeTimeScaleDelayed(float ts)
        {
            ChangeTimeScaleDelayed(ts, null, false);
        }
        public static void RestoreTimeScaleDelayed(object owner)
        {
            if (owner == null)
            {
                RestoreTimeScaleDelayed();
            }
            else
            {
                LinkedListNode<TimeScaleRecordInfo> node;
                if (_TimeScaleHistoryMap.TryGetValue(owner, out node))
                {
                    //if (IsInAdditiveMode)
                    //{
                    //    Time.timeScale = TargetTimeScale;
                    //}
                    _TimeScaleHistoryMap.Remove(owner);
                    _TimeScaleHistoryList.Remove(node);
                    _TargetTimeScale = CalculateTargetTimeScale();
                    RunController();
                }
            }
        }
        public static void RestoreTimeScaleDelayed()
        {
            if (_TimeScaleHistoryList.Count > 0)
            {
                //if (IsInAdditiveMode)
                //{
                //    Time.timeScale = TargetTimeScale;
                //}
                var node = _TimeScaleHistoryList.Last;
                var owner = node.Value.Owner;
                if (owner != null)
                {
                    _TimeScaleHistoryMap.Remove(owner);
                }
                _TimeScaleHistoryList.RemoveLast();
                _TargetTimeScale = CalculateTargetTimeScale();
                RunController();
            }
        }
        public static void ChangeTimeScaleImmediate(float ts, object owner, bool additive)
        {
            ChangeTimeScaleDelayed(ts, owner, additive);
            Time.timeScale = TargetTimeScale;
        }
        public static void ChangeTimeScaleImmediate(float ts, object owner)
        {
            ChangeTimeScaleImmediate(ts, owner, false);
        }
        public static void ChangeTimeScaleImmediate(float ts, bool additive)
        {
            ChangeTimeScaleImmediate(ts, null, additive);
        }
        public static void ChangeTimeScaleImmediate(float ts)
        {
            ChangeTimeScaleImmediate(ts, null, false);
        }
        public static void RestoreTimeScaleImmediate(object owner)
        {
            RestoreTimeScaleDelayed(owner);
            Time.timeScale = TargetTimeScale;
            if (_TimeScaleHistoryList.Count == 1 && _TimeScaleHistoryList.First.Value.Owner == null)
            {
                _TimeScaleHistoryList.RemoveFirst();
                _TargetTimeScale = -1.0f;
            }
        }
        public static void RestoreTimeScaleImmediate()
        {
            RestoreTimeScaleDelayed();
            Time.timeScale = TargetTimeScale;
            if (_TimeScaleHistoryList.Count == 1 && _TimeScaleHistoryList.First.Value.Owner == null)
            {
                _TimeScaleHistoryList.RemoveFirst();
                _TargetTimeScale = -1.0f;
            }
        }
        public static void RestoreAllTimeScaleImmediate()
        {
            _TimeScaleHistoryMap.Clear();
            if (_TimeScaleHistoryList.Count > 0)
            {
                var ts = _TimeScaleHistoryList.First.Value.NewTimeScale;
                Time.timeScale = Mathf.Max(ts, MIN_TIME_SCALE);
                _TimeScaleHistoryList.Clear();
                _TargetTimeScale = -1.0f;
            }
        }
    }
}