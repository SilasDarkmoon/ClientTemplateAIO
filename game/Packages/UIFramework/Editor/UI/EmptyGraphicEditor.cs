using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(EmptyGraphic), false)]
    [CanEditMultipleObjects]
    public class EmptyGraphicEditor : GraphicEditor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            EditorGUILayout.PropertyField(this.m_Script);
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
