using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    [CustomPropertyDrawer(typeof(DataDictionary))]
    public class DataDictionaryInspector : PropertyDrawer
    {
        public static IDictionary ClipBoard;

        protected bool _GUI_Collapsed = false;
        protected bool _GUI_Dirty = false;
        protected GUIStyle _GUI_BoldFoldout;
        protected GUIStyle _GUI_BoldTextField;

        private List<KeyValuePair<string, object>> rawindices = new List<KeyValuePair<string, object>>();
        private List<object> specifiedTypes = new List<object>();
        private HashSet<string> dirtyKeys = new HashSet<string>();
        private int newindex1 = -1;
        private int newindex2 = -1;

        private DataDictionary Target = null;
        private Dictionary<string, object> oldDict = null;
        private IDictionary<string, object> originDict = null;

        public DataDictionaryInspector()
        {
            _GUI_BoldFoldout = new GUIStyle(EditorStyles.foldout);
            _GUI_BoldFoldout.fontStyle |= FontStyle.Bold;
            _GUI_BoldTextField = new GUIStyle(EditorStyles.textField);
            _GUI_BoldTextField.fontStyle |= FontStyle.Bold;
        }

        private void SyncDataFromTarget()
        {
            rawindices.Clear();
            if (Target == null)
            {
                oldDict = new Dictionary<string, object>();
            }
            else
            {
                oldDict = new Dictionary<string, object>(Target);
            }
            rawindices.AddRange(oldDict);

            rawindices.Sort((kvp1, kvp2) =>
            {
                return string.Compare(kvp1.Key, kvp2.Key);
            });

            newindex1 = rawindices.Count;
            rawindices.Add(new KeyValuePair<string, object>("", ""));
            newindex2 = rawindices.Count;
            rawindices.Add(new KeyValuePair<string, object>("", null));

            specifiedTypes.Clear();
            specifiedTypes.AddRange(new object[rawindices.Count]);
            for (int i = 0; i < specifiedTypes.Count; ++i)
            {
                var val = rawindices[i].Value;
                if (val is string)
                {
                    int type = 0;
                    DataDictionary.GuessExValType(val, out type);
                    if (type != 3)
                    {
                        specifiedTypes[i] = typeof(string);
                    }
                }
            }
        }
        private void SyncDataFromTarget(SerializedProperty property)
        {
            try
            {
                var parent = property.serializedObject.targetObject;
                Target = fieldInfo.GetValue(parent) as DataDictionary;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            SyncDataFromTarget();
            if (property.isInstantiatedPrefab)
            {
                originDict = null;
                try
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(property.serializedObject.targetObject);
                    if (prefab)
                    {
                        var prefabdict = fieldInfo.GetValue(prefab) as DataDictionary;
                        if (prefabdict != null)
                        {
                            originDict = prefabdict;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (originDict == null)
                {
                    originDict = new Dictionary<string, object>();
                }
                CheckDirty();
            }
            else
            {

                originDict = new Dictionary<string, object>(Target);
                _GUI_Dirty = false;
                dirtyKeys.Clear();
            }
        }

        public void PasteData(IDictionary source)
        {
            if (source != null)
            {
                var gsrc = source as IDictionary<string, object>;
                if (gsrc != null)
                {
                    foreach (var kvp in gsrc)
                    {
                        Target[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    try
                    {
                        foreach (var entry in source)
                        {
                            if (entry is DictionaryEntry)
                            {
                                var dentry = (DictionaryEntry)entry;
                                var key = dentry.Key as string;
                                if (key != null)
                                {
                                    Target[key] = dentry.Value;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private bool IsEditorOld(SerializedProperty property)
        {
            if (oldDict == null)
            {
                return true;
            }
            DataDictionary target = null;
            try
            {
                target = fieldInfo.GetValue(property.serializedObject.targetObject) as DataDictionary;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (Target != target)
            {
                return true;
            }
            if (!DataDictionary.EqualDict(target, oldDict))
            {
                return true;
            }
            if (property.isInstantiatedPrefab)
            {
                DataDictionary oDict = null;
                try
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(property.serializedObject.targetObject);
                    if (prefab)
                    {
                        var prefabdict = fieldInfo.GetValue(prefab) as DataDictionary;
                        if (prefabdict != null)
                        {
                            oDict = prefabdict;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (oDict == null)
                {
                    if (originDict.Count > 0)
                    {
                        return true;
                    }
                }
                else
                {
                    if (!DataDictionary.EqualDict(oDict, originDict))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsEditorOld(property))
            {
                SyncDataFromTarget(property);
            }

            if (_GUI_Collapsed)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight * (rawindices.Count + 1) + EditorGUIUtility.standardVerticalSpacing * rawindices.Count;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsEditorOld(property))
            {
                SyncDataFromTarget(property);
            }
            //if (property.isInstantiatedPrefab)
            //{
            //    CheckDirty();
            //}
            //else
            //{
            //    _GUI_Dirty = false;
            //}
            CheckDirty();

            //if (_GUI_Dirty)
            //{
            //    property.serializedObject.Update();
            //    EditorUtility.SetDirty(property.serializedObject.targetObject);
            //    property.serializedObject.ApplyModifiedProperties();
            //}

            // the set-to-null btn
            GUIContent nullbtntxt = new GUIContent("X", "Clear");
            var nullbtnsize = GUI.skin.button.CalcSize(nullbtntxt);
            Rect nullbtnrect = new Rect();
            nullbtnrect.xMin = position.xMax - nullbtnsize.x;
            nullbtnrect.xMax = position.xMax;
            nullbtnrect.yMin = position.yMin;
            nullbtnrect.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(nullbtnrect, nullbtntxt))
            {
                if (EditorUtility.DisplayDialog("Confirm Clear", "Do you really want to clear all data in this dictionary?", "Yes", "No!"))
                {
                    var objs = property.serializedObject.targetObjects;
                    for (int i = 0; i < objs.Length; ++i)
                    {
                        var data = fieldInfo.GetValue(objs[i]) as DataDictionary;
                        if (data != null)
                        {
                            data.Clear();
                        }
                    }
                    property.serializedObject.Update();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    property.serializedObject.ApplyModifiedProperties();
                    return;
                }
            }

            // the reload btn
            GUIContent reloadbtntxt = new GUIContent("↕", "Sort");
            var reloadbtnsize = GUI.skin.button.CalcSize(reloadbtntxt);
            Rect reloadbtnrect = new Rect();
            reloadbtnrect.xMin = position.xMax - reloadbtnsize.x - nullbtnsize.x - EditorGUIUtility.standardVerticalSpacing;
            reloadbtnrect.width = reloadbtnsize.x;
            reloadbtnrect.yMin = position.yMin;
            reloadbtnrect.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(reloadbtnrect, reloadbtntxt))
            {
                SyncDataFromTarget(property);
                return;
            }

            // save btn
            var textureSave = EditorGUIUtility.Load("Save@2x") as Texture2D;
            GUIContent savebtntxt = new GUIContent("^S", "Save");
            var savebtnsize = nullbtnsize;
            GUIStyle styleSave = new GUIStyle(GUI.skin.button);
            if (textureSave)
            {
                savebtntxt = new GUIContent(textureSave, "Save");
                styleSave.padding = new RectOffset(1, 1, 1, 1);
            }
            else
            {
                savebtnsize = GUI.skin.button.CalcSize(savebtntxt);
            }
            Rect savebtnrect = new Rect();
            savebtnrect.xMin = position.xMax - savebtnsize.x - reloadbtnsize.x - nullbtnsize.x - EditorGUIUtility.standardVerticalSpacing;
            savebtnrect.width = savebtnsize.x;
            savebtnrect.yMin = position.yMin;
            savebtnrect.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(savebtnrect, savebtntxt, styleSave))
            {
                property.serializedObject.Update();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            // the copy btn
            GUIContent copybtntxt = new GUIContent("^C", "Copy");
            var copybtnsize = GUI.skin.button.CalcSize(copybtntxt);
            Rect copybtnrect = new Rect();
            copybtnrect.xMin = position.xMax - copybtnsize.x - savebtnsize.x - reloadbtnsize.x - nullbtnsize.x - EditorGUIUtility.standardVerticalSpacing;
            copybtnrect.width = copybtnsize.x;
            copybtnrect.yMin = position.yMin;
            copybtnrect.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(copybtnrect, copybtntxt))
            {
                ClipBoard = Target;
                return;
            }

            // the paste btn
            GUIContent pastebtntxt = new GUIContent("^V", "Paste");
            var pastebtnsize = GUI.skin.button.CalcSize(pastebtntxt);
            Rect pastebtnrect = new Rect();
            pastebtnrect.xMin = position.xMax - pastebtnsize.x - copybtnsize.x - savebtnsize.x - reloadbtnsize.x - nullbtnsize.x - EditorGUIUtility.standardVerticalSpacing;
            pastebtnrect.width = pastebtnsize.x;
            pastebtnrect.yMin = position.yMin;
            pastebtnrect.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(pastebtnrect, pastebtntxt))
            {
                PasteData(ClipBoard);
                return;
            }

            // the field name
            Rect fieldnamerect = new Rect();
            fieldnamerect.xMin = position.xMin;
            fieldnamerect.xMax = position.xMin + EditorGUIUtility.labelWidth;
            fieldnamerect.yMin = position.yMin;
            fieldnamerect.height = EditorGUIUtility.singleLineHeight;
            _GUI_Collapsed = !EditorGUI.Foldout(fieldnamerect, !_GUI_Collapsed, label, true, _GUI_Dirty ? _GUI_BoldFoldout : EditorStyles.foldout);
            if (_GUI_Collapsed) return;

            // each field
            var listCopy = new List<KeyValuePair<string, object>>(rawindices);
            var copyindex1 = newindex1;
            var copyindex2 = newindex2;
            var deletedcount = 0;
            float indentSize = EditorGUI.IndentedRect(position).xMin;
            ++EditorGUI.indentLevel;
            indentSize = EditorGUI.IndentedRect(position).xMin - indentSize;
            --EditorGUI.indentLevel;
            var typebtncontent = new GUIContent(EditorGUIUtility.FindTexture("icon dropdown"));
            var typebtnsize = GUI.skin.label.CalcSize(typebtncontent);
            
            for (int i = 0; i < listCopy.Count; ++i)
            {
                float spacepixel = EditorGUIUtility.standardVerticalSpacing;
                Rect keyrect = new Rect();
                keyrect.xMin = position.xMin + indentSize;
                keyrect.xMax = position.xMin + EditorGUIUtility.labelWidth - spacepixel;
                keyrect.yMin = position.yMin + (i + 1) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                keyrect.yMax = keyrect.yMin + EditorGUIUtility.singleLineHeight;
                Rect valrect = new Rect(keyrect);
                valrect.xMin = position.xMin + EditorGUIUtility.labelWidth;
                valrect.xMax = position.xMax - typebtnsize.x - spacepixel;
                Rect typebtnrect = new Rect(keyrect);
                typebtnrect.xMin = position.xMax - typebtnsize.x;
                typebtnrect.xMax = position.xMax;
                if (typebtnsize.y < EditorGUIUtility.singleLineHeight)
                {
                    typebtnrect.yMin = typebtnrect.yMin + (EditorGUIUtility.singleLineHeight - typebtnsize.y) / 2;
                    typebtnrect.height = typebtnsize.y;
                }

                var key = listCopy[i].Key;
                var val = listCopy[i].Value;
                bool keydirty = false, valdirty = false;
                if (i == copyindex2 || i == copyindex1)
                {
                    keydirty = true;
                    valdirty = true;
                }
                else
                {
                    if (_GUI_Dirty)
                    {
                        if (dirtyKeys.Contains(key))
                        {
                            if (originDict.ContainsKey(key))
                            {
                                valdirty = true;
                            }
                            else
                            {
                                keydirty = true;
                            }
                        }
                    }
                }

                if ((val is Object && ((Object)val != null)) || i == copyindex2)
                {
                    EditorGUI.BeginChangeCheck();
                    var newKey = EditorGUI.TextField(keyrect, key, keydirty ? _GUI_BoldTextField : EditorStyles.textField);
                    if (valdirty)
                        EditorStyles.objectField.fontStyle |= FontStyle.Bold;
                    var newVal = EditorGUI.ObjectField(valrect, (Object)val, typeof(Object), true);
                    if (valdirty)
                        EditorStyles.objectField.fontStyle &= ~FontStyle.Bold;

                    // Edit The Val
                    var rindex = i - deletedcount;
                    if (EditorGUI.EndChangeCheck())
                    {
                        deletedcount += EditField(rindex, newKey, newVal);
                    }
                    if (i == copyindex2 && (val is Object && ((Object)val == null) || val == null))
                    {
                        GUI.enabled = false;
                    }
                    // Type sel
                    if (GUI.Button(typebtnrect, typebtncontent, EditorStyles.label))
                    {
                        ShowTypeSelWinFor(rindex);
                    }
                    GUI.enabled = true;

                }
                else if (val is Object || (val == null && specifiedTypes[i - deletedcount] as Type == typeof(Object))) // ((Object)val) == null
                {
                    EditorGUI.BeginChangeCheck();
                    var newKey = EditorGUI.TextField(keyrect, key, keydirty ? _GUI_BoldTextField : EditorStyles.textField);
                    if (valdirty)
                        EditorStyles.objectField.fontStyle |= FontStyle.Bold;
                    var newVal = EditorGUI.ObjectField(valrect, null, typeof(Object), true);
                    if (valdirty)
                        EditorStyles.objectField.fontStyle &= ~FontStyle.Bold;

                    // Edit The Val
                    var rindex = i - deletedcount;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newVal != null)
                        {
                            deletedcount += EditField(rindex, key, newVal);
                        }
                        else if (string.IsNullOrEmpty(newKey))
                        {
                            deletedcount += EditField(rindex, null, null);
                        }
                        else if (newKey != key)
                        {
                            deletedcount += EditField(rindex, newKey, val);
                        }
                    }
                    if (GUI.Button(typebtnrect, typebtncontent, EditorStyles.label))
                    {
                        ShowTypeSelWinFor(rindex);
                    }
                }
                else if (val is System.IConvertible || val == null)
                {
                    EditorGUI.BeginChangeCheck();
                    var oldVal = val == null ? "" : val.ToString();
                    var newKey = EditorGUI.TextField(keyrect, key, keydirty || valdirty && oldVal == "" ? _GUI_BoldTextField : EditorStyles.textField);
                    var newVal = EditorGUI.TextField(valrect, oldVal, valdirty ? _GUI_BoldTextField : EditorStyles.textField);

                    // Edit The Val
                    var rindex = i - deletedcount;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newKey != key || newVal != oldVal)
                        {
                            deletedcount += EditField(rindex, newKey, newVal);
                        }
                    }
                    if (GUI.Button(typebtnrect, typebtncontent, EditorStyles.label))
                    {
                        ShowTypeSelWinFor(rindex);
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    var oldVal = val == null ? "" : val.ToString();
                    var newKey = EditorGUI.TextField(keyrect, key, keydirty || valdirty && oldVal == "" ? _GUI_BoldTextField : EditorStyles.textField);
                    var newVal = EditorGUI.TextField(valrect, oldVal, valdirty ? _GUI_BoldTextField : EditorStyles.textField);
                    object realnew = val;
                    if (string.IsNullOrEmpty(newVal))
                    {
                        realnew = null;
                    }
                    // Edit The Val
                    var rindex = i - deletedcount;
                    if (EditorGUI.EndChangeCheck())
                    {
                        deletedcount += EditField(rindex, newKey, realnew);
                    }
                    if (GUI.Button(typebtnrect, typebtncontent, EditorStyles.label))
                    {
                        ShowTypeSelWinFor(rindex);
                    }
                }
            }
        }

        public static bool EqualValWithPrefab(object prefab, object instance)
        {
            if (instance is Object)
            {
                //instance = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance as Object);
                // 在prefab mode模式下
                var obj = instance as Object;
                if (obj == null)
                {
                    return object.ReferenceEquals(prefab, instance);
                }
                object ret = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
                if (ret == null)
                {
                    return object.ReferenceEquals(prefab, instance);
                }
            }

            if ((prefab == null || (prefab is Object && ((Object)prefab) == null)) && (instance == null || (instance is Object && (((Object)instance) == null))))
            {
                return object.ReferenceEquals(prefab, instance);
            }
            else
            {
                return object.Equals(prefab, instance);
            }
        }
        public static bool EqualDictWithPrefab(IDictionary<string, object> prefab, IDictionary<string, object> instance)
        {
            if (prefab == null || instance == null)
            {
                return prefab == instance;
            }
            if (prefab.Count != instance.Count)
            {
                return false;
            }
            foreach (var kvp in prefab)
            {
                if (!instance.ContainsKey(kvp.Key))
                {
                    return false;
                }
                if (!EqualValWithPrefab(kvp.Value, instance[kvp.Key]))
                {
                    return false;
                }
            }
            return true;
        }

        private void CheckDirty()
        {
            _GUI_Dirty = !EqualDictWithPrefab(originDict, oldDict);
            dirtyKeys.Clear();
            foreach (var kvp in oldDict)
            {
                var key = kvp.Key;
                if (!originDict.ContainsKey(key) || !EqualValWithPrefab(originDict[key], kvp.Value))
                {
                    dirtyKeys.Add(key);
                }
            }
        }

        private int EditField(int index, string key, object val)
        {
            if (index == newindex1 || index == newindex2)
            {
                if (!string.IsNullOrEmpty(key) && !(val == null || (val is Object && (Object)val == null) || (val is string && string.IsNullOrEmpty((string)val))))
                {
                    if (!oldDict.ContainsKey(key)) // we should not add a duplicated key.
                    {
                        if (index == newindex1)
                        {
                            newindex1 = rawindices.Count;
                            rawindices.Add(new KeyValuePair<string, object>("", ""));
                            specifiedTypes.Add(null);
                        }
                        else
                        {
                            newindex2 = rawindices.Count;
                            rawindices.Add(new KeyValuePair<string, object>("", null));
                            specifiedTypes.Add(null);
                        }

                        oldDict[key] = val;
                        if (Target != null)
                        {
                            Target[key] = val;
                        }
                        //CheckDirty();
                    }
                }
            }
            else if (string.IsNullOrEmpty(key) && (val == null || (val is Object && (Object)val == null) || (val is string && string.IsNullOrEmpty((string)val))))
            {
                var oldkey = rawindices[index].Key;
                int keycnt = 0;
                for (int i = 0; i < rawindices.Count; ++i)
                {
                    if (i != newindex1 && i != newindex2)
                    {
                        if (rawindices[i].Key == oldkey)
                        {
                            ++keycnt;
                        }
                    }
                }

                if (index < newindex1)
                {
                    --newindex1;
                }
                if (index < newindex2)
                {
                    --newindex2;
                }
                rawindices.RemoveAt(index);
                specifiedTypes.RemoveAt(index);

                if (keycnt == 1)
                {
                    oldDict.Remove(oldkey);
                    if (Target != null)
                    {
                        Target.Remove(oldkey);
                    }
                    //CheckDirty();
                }
                return 1;
            }
            else
            {
                key = key ?? "";
                var real = val;
                if (val != null)
                {
                    if (specifiedTypes[index] as Type == typeof(string))
                    {
                        real = real.ToString();
                    }
                    else if (val is Object)
                    {
                        if (specifiedTypes[index] as Type == typeof(Object))
                        {
                            specifiedTypes[index] = null;
                        }
                    }
                    else
                    {
                        int vtype;
                        real = DataDictionary.GuessExValType(real, out vtype);
                    }
                }
                var oldkey = rawindices[index].Key;
                var oldval = rawindices[index].Value;
                if (oldval is Object && val == null)
                {
                    specifiedTypes[index] = typeof(Object);
                }
                for (int i = 0; i < rawindices.Count; ++i)
                {
                    if (i != newindex1 && i != newindex2 && i != index)
                    {
                        if (rawindices[i].Key == key)
                        {
                            rawindices[i] = new KeyValuePair<string, object>(key, val);
                            specifiedTypes[i] = specifiedTypes[index];
                        }
                    }
                }

                bool changed = !DataDictionary.EqualVal(oldval, real) || oldkey != key;
                if (changed)
                {
                    if (key != "")
                    {
                        oldDict[key] = real;
                    }
                    if (oldkey != key)
                    {
                        oldDict.Remove(oldkey);
                    }
                    if (Target != null)
                    {
                        if (key != "")
                        {
                            Target[key] = real;
                        }
                        if (oldkey != key)
                        {
                            Target.Remove(oldkey);
                        }
                    }
                    //CheckDirty();
                }
            }
            rawindices[index] = new KeyValuePair<string, object>(key, val);
            return 0;
        }

        private void ShowTypeSelWinFor(int index)
        {
            if (index >= 0 && index < rawindices.Count)
            {
                var kvp = rawindices[index];
                var val = kvp.Value;
                if ((val is Object || index == newindex2) && val != null || val == null && specifiedTypes[index] as Type == typeof(Object))
                {
                    GenericMenu menu = new GenericMenu();

                    if (rawindices != null && index >= 0 && index < rawindices.Count)
                    {
                        if (val is GameObject || val is Component)
                        {
                            System.Action<Object> addMenuItem = (obj) =>
                            {
                                var type = obj.GetType();
                                menu.AddItem(new GUIContent(type.Name), object.Equals(val, obj), (selcomp) =>
                                {
                                    if (!object.Equals(val, selcomp))
                                    {
                                        EditField(index, kvp.Key, selcomp);
                                    }
                                }, obj);
                            };

                            var go = val is GameObject ? val as GameObject : ((Component)val).gameObject;
                            addMenuItem(go);

                            HashSet<Type> compTypes = new HashSet<Type>();
                            foreach (var comp in go.GetComponents<Component>())
                            {
                                var comptype = comp.GetType();
                                var attrs = comptype.GetCustomAttributes(typeof(DataDictionaryComponentTypeAttribute), true);
                                if (attrs != null && attrs.Length > 0)
                                {
                                    var attr = attrs[0] as DataDictionaryComponentTypeAttribute;
                                    if (attr != null)
                                    {
                                        if (attr.Type == DataDictionaryComponentTypeAttribute.DataDictionaryComponentType.Main)
                                        {
                                            addMenuItem(comp);
                                            compTypes.Add(comptype);
                                        }
                                        else if (attr.Type == DataDictionaryComponentTypeAttribute.DataDictionaryComponentType.Sub)
                                        {
                                            compTypes.Add(comptype);
                                        }
                                    }
                                }
                            }

                            var transcomp = go.GetComponent<Transform>();
                            if (transcomp != null)
                            {
                                compTypes.Add(transcomp.GetType());
                                addMenuItem(transcomp);
                            }

                            bool sepAdded = false;
                            foreach (var comp in go.GetComponents<Component>())
                            {
                                var comptype = comp.GetType();
                                if (!compTypes.Contains(comptype))
                                {
                                    if (!sepAdded)
                                    {
                                        menu.AddSeparator("");
                                        sepAdded = true;
                                    }
                                    addMenuItem(comp);
                                }
                            }

                            if (index != newindex2)
                            {
                                menu.AddSeparator("");
                            }
                        }
                        if (index != newindex2)
                        {
                            menu.AddItem(new GUIContent("Plain"), false, () =>
                            {
                                string path = null;
                                if (val is Object && ((Object)val) != null)
                                {
                                    try
                                    {
                                        path = AssetDatabase.GetAssetPath(val as Object);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogException(e);
                                    }
                                }
                                if (!string.IsNullOrEmpty(path))
                                {
                                    path = ResInfoEditor.GetAssetNormPath(path);
                                }
                                specifiedTypes[index] = null;
                                EditField(index, kvp.Key, path);
                            });
                        }
                    }

                    menu.ShowAsContext();
                }
                else if (!(val is Object && ((Object)val) == null) && !(index == newindex2 && val == null))
                {
                    GenericMenu menu = new GenericMenu();
                    if (rawindices != null && index >= 0 && index < rawindices.Count)
                    {
                        var KeepStringType = specifiedTypes[index] as Type == typeof(string);

                        System.Action<string> addMenuItem = (name) =>
                        {
                            string valstr = val == null ? "null" : val.ToString();
                            if (val is string && (string.IsNullOrEmpty(valstr) || name != "Auto"))
                            {
                                valstr = "\"" + valstr + "\"";
                            }

                            var title = name + " (" + valstr + ")";
                            menu.AddItem(new GUIContent(title), KeepStringType != (name == "Auto"), (x) =>
                            {
                                var oldType = specifiedTypes[index];
                                object newType = null;
                                if (name != "Auto")
                                {
                                    newType = typeof(string);
                                }
                                specifiedTypes[index] = newType;
                                if (oldType != newType)
                                {
                                    EditField(index, kvp.Key, val);
                                }
                            }, name);
                        };

                        addMenuItem("Auto");
                        addMenuItem("String");
                        if (index != newindex1)
                        {
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Object"), false, () =>
                            {
                                specifiedTypes[index] = typeof(Object);
                                object oldval;
                                oldDict.TryGetValue(kvp.Key, out oldval);
                                Object newval = null;
                                if (oldval is string)
                                {
                                    var path = oldval as string;
                                    {
                                        var real = ResInfoEditor.FindDistributeAsset(path);
                                        if (real != null)
                                        {
                                            try
                                            {
                                                var obj = AssetDatabase.LoadMainAssetAtPath(real);
                                                if (obj != null)
                                                {
                                                    newval = obj;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.LogException(e);
                                            }
                                        }
                                    }
                                    if (newval == null && !path.Contains('/'))
                                    {
                                        path = path.Replace('.', '/');
                                        path = "ModSpt/" + path + ".lua";
                                        var real = ResManager.EditorResLoader.CheckDistributePath(path);
                                        if (real != null)
                                        {
                                            try
                                            {
                                                var obj = AssetDatabase.LoadMainAssetAtPath(real);
                                                if (obj != null)
                                                {
                                                    newval = obj;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.LogException(e);
                                            }
                                        }
                                    }
                                }
                                EditField(index, kvp.Key, newval);
                            });
                        }
                        {
                            object oldval;
                            if (index == newindex1)
                            {
                                oldval = val;
                            }
                            else
                            {
                                oldDict.TryGetValue(kvp.Key, out oldval);
                            }
                            if (oldval is string)
                            {
                                var strval = oldval as string;
                                menu.AddSeparator("");
                                menu.AddItem(new GUIContent("Find Asset"), false, () =>
                                {
                                    var real = ResInfoEditor.FindDistributeAsset(strval);
                                    if (real != null)
                                    {
                                        try
                                        {
                                            var obj = AssetDatabase.LoadMainAssetAtPath(real);
                                            if (obj != null)
                                            {
                                                EditorGUIUtility.PingObject(obj);
                                                return;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogException(e);
                                        }
                                    }
                                });
                                menu.AddItem(new GUIContent("Find Script"), false, () =>
                                {
                                    var path = strval.Replace('.', '/');
                                    path = "ModSpt/" + path + ".lua";
                                    var real = ResManager.EditorResLoader.CheckDistributePath(path);
                                    if (real != null)
                                    {
                                        try
                                        {
                                            var obj = AssetDatabase.LoadMainAssetAtPath(real);
                                            if (obj != null)
                                            {
                                                EditorGUIUtility.PingObject(obj);
                                                return;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.LogException(e);
                                        }
                                    }
                                });
                            }

                        }
                    }
                    menu.ShowAsContext();
                }
                else if ((val is Object && ((Object)val) == null) || (val == null && specifiedTypes[index] as Type == typeof(Object)))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Auto"), true, () => { });
                    menu.AddItem(new GUIContent("Plain"), false, () =>
                    {
                        specifiedTypes[index] = null;
                        EditField(index, kvp.Key, null);
                    });
                    menu.ShowAsContext();
                }
            }
        }
    }

    public class InspectorBase<T> : Editor where T : Object
    {
        protected T Target { get { return (T)target; } }
    }
}
