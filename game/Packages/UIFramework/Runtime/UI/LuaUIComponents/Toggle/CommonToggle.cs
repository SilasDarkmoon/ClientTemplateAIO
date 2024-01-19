using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngineEx;

namespace UnityEngineEx.UI
{
    public enum ButtonGroupType
    {
        Static,
        Dynamic
    }

    public class CommonToggle : ToggleGroup, ILuaBehavEx
    {
        public ButtonGroupType ButtonGroupType;
        public GameObject ToggleObj;
        public string TogglePrefab;
        public int TogglesCount;
        public List<GameObject> Toggles;
        public bool AllowReselect;
        private bool isTriggerLuaListener = true;
        LuaBehav ILuaBehavEx.Major { get; set; }
        bool ILuaBehavEx.RouteToParents { get { return false; } }
        private int m_cache_tag;

        protected override void Awake()
        {
            base.Awake();

            if (ButtonGroupType == ButtonGroupType.Dynamic)
            {
                Toggles = new List<GameObject>();

                GameObject prefabObj = null;
                if (!string.IsNullOrEmpty(TogglePrefab))
                {
                    prefabObj = ResManager.LoadRes(TogglePrefab) as GameObject;
                }

                for (int i = 0; i < TogglesCount; i++)
                {
                    var obj = GameObject.Instantiate(prefabObj != null ? prefabObj : ToggleObj, transform);

                    if (obj)
                    {
                        if (!obj.activeSelf)
                        {
                            obj.SetActive(true);
                        }

                        Toggles.Add(obj);
                    }
                }

            }
            for (int i = 0; i < Toggles.Count; i++)
            {
                int tag = i + 1;
                GameObject obj = Toggles[i];
                Debug.Assert(obj.GetComponent<UnityEngine.UI.Toggle>(), "Toggle object must has a toggle compoment");
                LuaBehav btnLua = obj.GetComponent<LuaBehav>();
                this.CallLuaFunc("onToggleCreated", btnLua, tag);
                Toggle t = obj.GetComponent<Toggle>();
                t.group = this;
                t.onValueChanged = new Toggle.ToggleEvent();
                t.onValueChanged.AddListener((bool active) =>
                {
                    if (isTriggerLuaListener)
                    {
                        LuaBehav btnLua2 = Toggles[tag - 1].GetComponent<LuaBehav>();
                        if (active)
                        {
                            if (AllowReselect)
                                m_cache_tag = tag;
                            else if (tag == m_cache_tag)
                                return;
                            else
                                m_cache_tag = tag;

                            this.CallLuaFunc("onToggleSelected", btnLua2, tag);
                            this.CallLuaFunc("onTagSwitched", tag);
                        }
                        else
                        {
                            this.CallLuaFunc("onToggleDeselected", btnLua2, tag);
                        }
                    }
                });
            }
        }

        public void SelectDefaultToggle(int tag)
        {
            m_cache_tag = tag;
            isTriggerLuaListener = false;
            for (int i = 0; i < Toggles.Count; i++)
            {
                int toggleTag = i + 1;
                GameObject obj = Toggles[i];
                LuaBehav btnLua = obj.GetComponent<LuaBehav>();
                if (toggleTag == tag)
                {
                    obj.GetComponent<Toggle>().isOn = true;
                    this.CallLuaFunc("onToggleSelected", btnLua, tag);
                    this.CallLuaFunc("onTagSwitched", tag);
                }
                else
                {
                    obj.GetComponent<Toggle>().isOn = false;
                    this.CallLuaFunc("onToggleDeselected", btnLua, tag);
                }
            }
            isTriggerLuaListener = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}