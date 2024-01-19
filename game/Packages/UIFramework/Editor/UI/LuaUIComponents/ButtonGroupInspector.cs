using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorEx;
using UnityEngineEx.UI;

[CustomEditor(typeof(CommonToggle))]
[CanEditMultipleObjects]
public class ButtonGroupInspector : InspectorBase<CommonToggle>
{
    private SerializedObject obj;
    private SerializedProperty toggleObj;
    private SerializedProperty togglesCount;
    private SerializedProperty toggles;
    private SerializedProperty buttonGroupType;
    private SerializedProperty allowSwitchOff;
    // 重选已选中项触发选中事件
    private SerializedProperty allowReselect;

    void OnEnable()
    {
        obj = new SerializedObject(target);
        toggleObj = obj.FindProperty("ToggleObj");
        togglesCount = obj.FindProperty("TogglesCount");
        toggles = obj.FindProperty("Toggles");
        buttonGroupType = obj.FindProperty("ButtonGroupType");
        allowSwitchOff = obj.FindProperty("m_AllowSwitchOff");
        allowReselect = obj.FindProperty("AllowReselect");
    }

    public override void OnInspectorGUI()
    {
        obj.Update();
        EditorGUILayout.PropertyField(buttonGroupType);
        if (Target.ButtonGroupType == ButtonGroupType.Dynamic)
        {
            EditorGUILayout.PropertyField(toggleObj);
            EditorGUILayout.PropertyField(togglesCount);
        }
        else
        {
            EditorGUILayout.PropertyField(toggles, true);
        }
        EditorGUILayout.PropertyField(allowSwitchOff);
        EditorGUILayout.PropertyField(allowReselect);
        obj.ApplyModifiedProperties();
    }
}
