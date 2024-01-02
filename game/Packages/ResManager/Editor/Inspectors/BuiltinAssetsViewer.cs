using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    public class BuiltinAssetsViewer : EditorWindow
    {
        [MenuItem("Tools/Builtin Assets Viewer", priority = 100110)]
        static void Init()
        {
            GetWindow(typeof(BuiltinAssetsViewer));
        }

        private void Awake()
        {
            titleContent = new GUIContent("Builtin Assets");
        }

        private int _SelectedGroup = 0;
        private Vector2 _ScrollOffset = Vector2.zero;
        private int _SelectedItem = 0;
        private GUIContent[] _ItemContents;
        private Object[] _ShowingAssets;
        private Texture2D _Preview;
        void OnGUI()
        {
            int oldSelGroup = _SelectedGroup;
            _SelectedGroup = GUILayout.SelectionGrid(_SelectedGroup, new[] { new GUIContent("Builtin"), new GUIContent("Builtin Extra"), new GUIContent("Editor Builtin") }, 3);
            bool shouldRefreshPreview = false;
            if (oldSelGroup != _SelectedGroup || _ShowingAssets == null)
            {
                _ScrollOffset = Vector2.zero;
                _SelectedItem = 0;
                _ItemContents = null;
                _ShowingAssets = null;

                switch (_SelectedGroup)
                {
                    case 0:
                        goto default;
                    case 1:
                        _ShowingAssets = AssetDatabase.LoadAllAssetsAtPath("Resources/unity_builtin_extra");
                        break;
                    case 2:
                        _ShowingAssets = AssetDatabase.LoadAllAssetsAtPath("Library/unity editor resources");
                        break;
                    default:
                        _ShowingAssets = AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources");
                        break;
                }

                if (_ShowingAssets != null)
                {
                    _ItemContents = new GUIContent[_ShowingAssets.Length];
                    for (int i = 0; i < _ShowingAssets.Length; ++i)
                    {
                        var asset = _ShowingAssets[i];
                        _ItemContents[i] = new GUIContent(asset.name + " (" + asset.GetType() + ")");
                    }
                }
                shouldRefreshPreview = true;
            }
            if (_ItemContents != null && _ItemContents.Length > 0)
            {
                var oldSelItem = _SelectedItem;
                _ScrollOffset = EditorGUILayout.BeginScrollView(_ScrollOffset);
                _SelectedItem = GUILayout.SelectionGrid(_SelectedItem, _ItemContents, 1);
                EditorGUILayout.EndScrollView();
                if (oldSelItem != _SelectedItem)
                {
                    shouldRefreshPreview = true;
                }
            }

            if (shouldRefreshPreview)
            {
                _Preview = null;
                if (_ShowingAssets != null && _ShowingAssets.Length > _SelectedItem && _SelectedItem >= 0)
                {
                    var asset = _ShowingAssets[_SelectedItem];
                    if (asset is Texture2D)
                    {
                        _Preview = asset as Texture2D;
                    }
                    else if (asset is Sprite)
                    {
                        _Preview = ((Sprite)asset).texture;
                    }
                    else
                    {
                        _Preview = AssetPreview.GetAssetPreview(asset);
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent(_Preview), GUILayout.Height(100));
            EditorGUILayout.BeginVertical();
            if (_ShowingAssets != null && _ShowingAssets.Length > _SelectedItem && _SelectedItem >= 0)
            {
                var asset = _ShowingAssets[_SelectedItem];
                GUI.enabled = false;
                if (GUILayout.Button(new GUIContent("Open")))
                {
                    EditorGUIUtility.ShowObjectPicker<Object>(asset, false, asset.name, 0);
                }
                if (_SelectedGroup == 2)
                {
                    GUI.enabled = true;
                }
                if (GUILayout.Button(new GUIContent("Path")))
                {
                    if (_SelectedGroup == 2)
                    {
                        Debug.Log("UnityEditor.EditorGUIUtility.Load(\"" + asset.name + "\")"); // TODO: for shaders, there is '/' in the name, we should get the last part as the asset name.
                    }
                    // TODO: for runtime builtin assets, there is no clean way to load from a path. we'd better apply it to an object's serializable property and be staticlly loaded together with the container object.
                }
                GUI.enabled = true;

                Event evt = Event.current;
                if (evt != null)
                {
                    if (evt.type == EventType.MouseDrag)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.StartDrag("Drag from BuiltinAssetsViewer");
                        DragAndDrop.objectReferences = new[] { asset };
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}