using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

namespace UnityEngineEx
{
    public class PlaybackInput : BaseInput
    {
        protected InputRecordPlayer _Player;
        public InputRecordPlayer Player { get { return _Player; } }
        [SerializeField] protected RecordedInputData _Data;
        public RecordedInputData Data
        {
            get { return _Data; }
            set
            {
                _Data = value;
                if (_Player != null)
                {
                    _Player.Data = value;
                }
            }
        }

        protected static Texture2D _IndicatorImage;
        public static Texture2D IndicatorImage
        {
            get
            {
                if (!_IndicatorImage)
                {
                    _IndicatorImage = new Texture2D(1, 1);
                    _IndicatorImage.SetPixel(0, 0, new Color(1f, 0f, 0f, 1f));
                    _IndicatorImage.Apply();
                }
                return _IndicatorImage;
            }
        }

        protected int _IndicatorFrameIndex;
        protected Vector2[] _IndicatorPositions;
        protected bool ShouldShowIndicator()
        {
            return _IndicatorFrameIndex == Time.frameCount;
        }
        protected void ShowIndicator(int index, Vector2 pos)
        {
            if (_IndicatorPositions == null)
            {
                _IndicatorPositions = new Vector2[10];
            }
            if (_IndicatorFrameIndex != Time.frameCount)
            {
                _IndicatorFrameIndex = Time.frameCount;
                for (int i = 0; i < _IndicatorPositions.Length; ++i)
                {
                    _IndicatorPositions[i] = new Vector2(-1, -1);
                }
            }
            if (index >= _IndicatorPositions.Length)
            {
                return;
            }
            else
            {
                _IndicatorPositions[index] = pos;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _Player = new InputRecordPlayer(_Data);
            _IndicatorFrameIndex = -1;
            _IndicatorPositions = new Vector2[10];
        }

        public override string compositionString
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetCompositionString();
            }
        }

        public override IMECompositionMode imeCompositionMode
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetIMECompositionMode();
            }
            set
            {
                _Player.Update();
            }
        }

        public override Vector2 compositionCursorPos
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetCompositionCursorPos();
            }
            set
            {
                _Player.Update();
            }
        }

        public override bool mousePresent
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetMousePresent();
            }
        }

        public override bool GetMouseButtonDown(int button)
        {
            _Player.Update();
            return _Player.Cache.GetMouseButtonDown(button);
        }

        public override bool GetMouseButtonUp(int button)
        {
            _Player.Update();
            return _Player.Cache.GetMouseButtonUp(button);
        }

        public override bool GetMouseButton(int button)
        {
            _Player.Update();
            return _Player.Cache.GetMouseButton(button);
        }

        public override Vector2 mousePosition
        {
            get
            {
                _Player.Update();
                var pos = _Player.Cache.GetMousePosition();
                ShowIndicator(0, pos);
                return pos;
            }
        }

        public override Vector2 mouseScrollDelta
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetMouseScrollDelta();
            }
        }

        public override bool touchSupported
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetTouchSupported();
            }
        }

        public override int touchCount
        {
            get
            {
                _Player.Update();
                return _Player.Cache.GetTouchCount();
            }
        }

        public override Touch GetTouch(int index)
        {
            _Player.Update();
            var touch = _Player.Cache.GetTouch(index);
            ShowIndicator(touch.fingerId, touch.position);
            return touch;
        }

        public override float GetAxisRaw(string axisName)
        {
            _Player.Update();
            return _Player.Cache.GetAxisRaw(axisName);
        }

        public override bool GetButtonDown(string buttonName)
        {
            _Player.Update();
            return _Player.Cache.GetButtonDown(buttonName);
        }

        private void OnGUI()
        {
            if (ShouldShowIndicator())
            {
                if (Event.current.type.Equals(EventType.Repaint))
                {
                    for (int i = 0; i < _IndicatorPositions.Length; ++i)
                    {
                        var pos = _IndicatorPositions[i];
                        if (pos.x >= 0 && pos.y >= 0)
                        {
                            Graphics.DrawTexture(new Rect(pos.x - 15f, Screen.height - pos.y - 15f, 30f, 30f), IndicatorImage);
                        }
                    }
                }
            }
        }
    }
}