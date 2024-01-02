using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DataDictionaryComponentTypeAttribute : Attribute
    {
        public enum DataDictionaryComponentType
        {
            Norm = 0,
            Main = 1,
            Sub = 2,
        }

        public DataDictionaryComponentType Type;

        public DataDictionaryComponentTypeAttribute(DataDictionaryComponentType type)
        {
            Type = type;
        }
    }

    [Serializable]
    public class DataDictionary : ISerializationCallbackReceiver
        , IEnumerable, IEnumerable<KeyValuePair<string, object>>, ICollection, ICollection<KeyValuePair<string, object>>, IDictionary, IDictionary<string, object>
    {
        [SerializeField] private List<string> ExFieldKeys = new List<string>();
        [SerializeField] private List<int> ExFieldTypes = new List<int>();
        [SerializeField] private List<int> ExFieldIndices = new List<int>();
        [SerializeField] private List<bool> ExFieldValsBool = new List<bool>();
        [SerializeField] private List<int> ExFieldValsInt = new List<int>();
        [SerializeField] private List<double> ExFieldValsDouble = new List<double>();
        [SerializeField] private List<string> ExFieldValsString = new List<string>();
        [SerializeField] private List<Object> ExFieldValsObj = new List<Object>();

        public static object GuessExValType(object val, out int vtype)
        {
            if (!(val is string))
            {
                if (val == null || val is UnityEngine.Object)
                {
                    vtype = 4;
                    return val;
                }
                else
                {
                    val = val.ToString();
                }
            }
            var str = val as string;
            int ival;
            if (int.TryParse(str, out ival))
            {
                vtype = 1;
                return ival;
            }
            bool bval;
            if (bool.TryParse(str, out bval))
            {
                vtype = 0;
                return bval;
            }
            double dval;
            if (double.TryParse(str, out dval))
            {
                vtype = 2;
                return dval;
            }
            vtype = 3;
            return val;
        }
        public static object GetExValType(object val, out int vtype)
        {
            if (val == null || val is UnityEngine.Object)
            {
                vtype = 4;
                return val;
            }
            if (val is string)
            {
                vtype = 3;
                return val;
            }
            if (val is bool)
            {
                vtype = 0;
                return val;
            }
            if (val is double)
            {
                vtype = 2;
                return val;
            }
            if (val is int)
            {
                vtype = 1;
                return val;
            }
            if (val.IsObjIConvertible())
            {
                try
                {
                    double d = Convert.ToDouble(val);
                    int i = (int)d;
                    if (((double)i) == d)
                    {
                        vtype = 1;
                        return i;
                    }
                    else
                    {
                        vtype = 2;
                        return d;
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }

            vtype = -1;
            return val;
        }
        public static bool EqualVal(object val1, object val2)
        {
            if ((val1 == null || (val1 is Object && ((Object)val1) == null)) && (val2 == null || (val2 is Object && ((Object)val2) == null)))
            {
                return object.ReferenceEquals(val1, val2);
            }
            else
            {
                return object.Equals(val1, val2);
            }
        }
        public static bool EqualDict(IDictionary<string, object> a, IDictionary<string, object> b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }
            if (a.Count != b.Count)
            {
                return false;
            }
            foreach (var kvp in a)
            {
                if (!b.ContainsKey(kvp.Key))
                {
                    return false;
                }
                if (!EqualVal(kvp.Value, b[kvp.Key]))
                {
                    return false;
                }
            }
            return true;
        }
        public int GetExKeyIndex(string key)
        {
            int index = -1;
            for (int i = 0; i < ExFieldKeys.Count; ++i)
            {
                if (key == ExFieldKeys[i])
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public object ParseExVal(int index)
        {
            if (index >= 0)
            {
                if (index < ExFieldIndices.Count && index < ExFieldTypes.Count)
                {
                    var vindex = ExFieldIndices[index];
                    var vtype = ExFieldTypes[index];
                    if (vindex >= 0)
                    {
                        switch (vtype)
                        {
                            case 0:
                                if (vindex < ExFieldValsBool.Count)
                                {
                                    return ExFieldValsBool[vindex];
                                }
                                break;
                            case 1:
                                if (vindex < ExFieldValsInt.Count)
                                {
                                    return ExFieldValsInt[vindex];
                                }
                                break;
                            case 2:
                                if (vindex < ExFieldValsDouble.Count)
                                {
                                    return ExFieldValsDouble[vindex];
                                }
                                break;
                            case 3:
                                if (vindex < ExFieldValsString.Count)
                                {
                                    return ExFieldValsString[vindex];
                                }
                                break;
                            case 4:
                                if (vindex < ExFieldValsObj.Count)
                                {
                                    return ExFieldValsObj[vindex];
                                }
                                break;
                        }
                    }
                }
            }
            return null;
        }
        public void ExpandExVal()
        {
            _Data.Clear();
            for (int i = 0; i < ExFieldKeys.Count; ++i)
            {
                _Data[ExFieldKeys[i]] = ParseExVal(i);
            }
        }
        private int DeleteExKey(string key)
        {
            int index = GetExKeyIndex(key);
            if (index >= 0)
            {
                ExFieldKeys.RemoveAt(index);
                var type = ExFieldTypes[index];
                ExFieldTypes.RemoveAt(index);
                var vindex = ExFieldIndices[index];
                ExFieldIndices.RemoveAt(index);

                for (int i = 0; i < ExFieldIndices.Count; ++i)
                {
                    if (ExFieldTypes[i] == type && ExFieldIndices[i] > vindex)
                    {
                        --ExFieldIndices[i];
                    }
                }

                switch (type)
                {
                    case 0:
                        if (vindex < ExFieldValsBool.Count)
                        {
                            ExFieldValsBool.RemoveAt(vindex);
                        }
                        break;
                    case 1:
                        if (vindex < ExFieldValsInt.Count)
                        {
                            ExFieldValsInt.RemoveAt(vindex);
                        }
                        break;
                    case 2:
                        if (vindex < ExFieldValsDouble.Count)
                        {
                            ExFieldValsDouble.RemoveAt(vindex);
                        }
                        break;
                    case 3:
                        if (vindex < ExFieldValsString.Count)
                        {
                            ExFieldValsString.RemoveAt(vindex);
                        }
                        break;
                    case 4:
                        if (vindex < ExFieldValsObj.Count)
                        {
                            ExFieldValsObj.RemoveAt(vindex);
                        }
                        break;
                }
            }
            return index;
        }
        private void AddExVal(string key, object val)
        {
            if (!string.IsNullOrEmpty(key) && !(val == null || (val is UnityEngine.Object && (UnityEngine.Object)val == null) || (val is string && string.IsNullOrEmpty((string)val))))
            {
                ExFieldKeys.Add(key);
                int vtype;
                var realval = GetExValType(val, out vtype);
                if (vtype < 0)
                {
                    vtype = 3;
                    realval = realval.ToString();
                }
                ExFieldTypes.Add(vtype);
                switch (vtype)
                {
                    case 0:
                        ExFieldIndices.Add(ExFieldValsBool.Count);
                        ExFieldValsBool.Add((bool)realval);
                        break;
                    case 1:
                        ExFieldIndices.Add(ExFieldValsInt.Count);
                        ExFieldValsInt.Add((int)realval);
                        break;
                    case 2:
                        ExFieldIndices.Add(ExFieldValsDouble.Count);
                        ExFieldValsDouble.Add((double)realval);
                        break;
                    case 3:
                        ExFieldIndices.Add(ExFieldValsString.Count);
                        ExFieldValsString.Add((string)realval);
                        break;
                    case 4:
                        ExFieldIndices.Add(ExFieldValsObj.Count);
                        ExFieldValsObj.Add((UnityEngine.Object)realval);
                        break;
                }
            }
        }
        private void AddExValStr(string key, string val)
        {
            ExFieldKeys.Add(key);
            ExFieldTypes.Add(3);
            ExFieldIndices.Add(ExFieldValsString.Count);
            ExFieldValsString.Add(val);
        }
        private void ChangeExKey(int index, string newKey)
        {
            if (newKey != null && index >= 0 && index < ExFieldKeys.Count)
            {
                var oldKey = ExFieldKeys[index];
                if (oldKey != newKey)
                {
                    int index2 = DeleteExKey(newKey);
                    if (index2 >= 0)
                    {
                        if (index > index2)
                        {
                            --index;
                        }
                    }
                    ExFieldKeys[index] = newKey;
                }
            }
        }
        private void ChangeExVal(string key, object val)
        {
            var index = GetExKeyIndex(key);
            if (index < 0)
            {
                AddExVal(key, val);
            }
            else
            {
                var old = ParseExVal(index);
                if (!EqualVal(old, val))
                {
                    int oldtype = ExFieldTypes[index];
                    int newtype;
                    object realnew = GetExValType(val, out newtype);
                    if (newtype < 0)
                    {
                        newtype = 3;
                        realnew = realnew.ToString();
                    }

                    if (oldtype == newtype)
                    {
                        var vindex = ExFieldIndices[index];
                        switch (newtype)
                        {
                            case 0:
                                ExFieldValsBool[vindex] = (bool)realnew;
                                break;
                            case 1:
                                ExFieldValsInt[vindex] = (int)realnew;
                                break;
                            case 2:
                                ExFieldValsDouble[vindex] = (double)realnew;
                                break;
                            case 3:
                                ExFieldValsString[vindex] = (string)realnew;
                                break;
                            case 4:
                                ExFieldValsObj[vindex] = (UnityEngine.Object)realnew;
                                break;
                        }
                    }
                    else
                    {
                        DeleteExKey(key);
                        AddExVal(key, realnew);
                    }
                }
            }
        }
        public void SyncBackExVal()
        {
            Dictionary<string, object> newdict = new Dictionary<string, object>(_Data);
            ExpandExVal();
            SyncBackExVal(newdict);
        }
        public void SyncBackExVal(Dictionary<string, object> data)
        {
            if (data == null)
            {
                Clear();
            }
            else
            {
                if (!EqualDict(data, _Data))
                {
                    Clear();
                    foreach (var kvp in data)
                    {
                        AddExVal(kvp.Key, kvp.Value);
                    }
                }
            }
        }
        public void SyncWithOther(List<string> keys, List<int> types, List<int> indices, List<bool> valsBool, List<int> valsInt, List<double> valsDouble, List<string> valsString, List<Object> valsObj)
        {
            ExFieldKeys = keys ?? new List<string>();
            ExFieldTypes = types ?? new List<int>();
            ExFieldIndices = indices ?? new List<int>();
            ExFieldValsBool = valsBool ?? new List<bool>();
            ExFieldValsInt = valsInt ?? new List<int>();
            ExFieldValsDouble = valsDouble ?? new List<double>();
            ExFieldValsString = valsString ?? new List<string>();
            ExFieldValsObj = valsObj ?? new List<Object>();

            ExpandExVal();
        }

        public void OnAfterDeserialize()
        {
            ExFieldKeys = ExFieldKeys ?? new List<string>();
            ExFieldTypes = ExFieldTypes ?? new List<int>();
            ExFieldIndices = ExFieldIndices ?? new List<int>();
            ExFieldValsBool = ExFieldValsBool ?? new List<bool>();
            ExFieldValsInt = ExFieldValsInt ?? new List<int>();
            ExFieldValsDouble = ExFieldValsDouble ?? new List<double>();
            ExFieldValsString = ExFieldValsString ?? new List<string>();
            ExFieldValsObj = ExFieldValsObj ?? new List<Object>();

            ExpandExVal();
        }

        public void OnBeforeSerialize()
        {
            // Do nothing. the _Data & ExFields should be sync over member calls.
        }

        private readonly Dictionary<string, object> _Data = new Dictionary<string, object>();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return GetEnumerator();
        }
        public int Count { get { return _Data.Count; } }
        bool ICollection.IsSynchronized { get { return ((ICollection)_Data).IsSynchronized; } }
        object ICollection.SyncRoot { get { return ((ICollection)_Data).SyncRoot; } }
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_Data).CopyTo(array, index);
        }
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly { get { return ((ICollection<KeyValuePair<string, object>>)_Data).IsReadOnly; } }
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)_Data).Add(item);
            ChangeExVal(item.Key, item.Value);
        }
        public void Clear()
        {
            _Data.Clear();
            ExFieldKeys.Clear();
            ExFieldTypes.Clear();
            ExFieldIndices.Clear();
            ExFieldValsBool.Clear();
            ExFieldValsInt.Clear();
            ExFieldValsDouble.Clear();
            ExFieldValsString.Clear();
            ExFieldValsObj.Clear();
        }
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_Data).Contains(item);
        }
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_Data).CopyTo(array, arrayIndex);
        }
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            bool found = ((ICollection<KeyValuePair<string, object>>)_Data).Remove(item);
            DeleteExKey(item.Key);
            return found;
        }
        public Dictionary<string, object>.Enumerator GetEnumerator()
        {
            return _Data.GetEnumerator();
        }
        bool IDictionary.IsFixedSize { get { return ((IDictionary)_Data).IsFixedSize; } }
        bool IDictionary.IsReadOnly { get { return ((IDictionary)_Data).IsReadOnly; } }
        public Dictionary<string, object>.KeyCollection Keys { get { return _Data.Keys; } }
        public Dictionary<string, object>.ValueCollection Values { get { return _Data.Values; } }
        ICollection IDictionary.Keys { get { return Keys; } }
        ICollection IDictionary.Values { get { return Values; } }
        object IDictionary.this[object key]
        {
            get { return this[key as string]; }
            set
            {
                ((IDictionary)_Data)[key] = value;
                ChangeExVal(key as string, value);
            }
        }
        void IDictionary.Add(object key, object value)
        {
            ((IDictionary)_Data).Add(key, value);
            ChangeExVal(key as string, value);
        }
        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)_Data).Contains(key);
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return GetEnumerator();
        }
        void IDictionary.Remove(object key)
        {
            ((IDictionary)_Data).Remove(key);
            DeleteExKey(key as string);
        }
        ICollection<string> IDictionary<string, object>.Keys { get { return Keys; } }
        ICollection<object> IDictionary<string, object>.Values { get { return Values; } }
        public object this[string key]
        {
            get
            {
                if (key == null)
                {
                    return null;
                }
                object rv;
                if (_Data.TryGetValue(key, out rv))
                {
                    return rv;
                }
                return null;
            }
            set
            {
                _Data[key] = value;
                ChangeExVal(key, value);
            }
        }
        public void Add(string key, object value)
        {
            _Data.Add(key, value);
            ChangeExVal(key, value);
        }
        public bool ContainsKey(string key)
        {
            return _Data.ContainsKey(key);
        }
        public bool Remove(string key)
        {
            bool found = _Data.Remove(key);
            DeleteExKey(key);
            return found;
        }
        public bool TryGetValue(string key, out object value)
        {
            return _Data.TryGetValue(key, out value);
        }

        #region Typed Getter and Setter
        // TODO: typed ChangeExVal and Typed AddExVal...
        //public void Set(string key, bool val)
        //{
        //    this[key] = val;
        //}
        //public void Set(string key, int val)
        //{

        //}
        //public void Set(string key, double val)
        //{

        //}
        //public void Set(string key, string val)
        //{

        //}
        //public void Set(string key, Object val)
        //{

        //}
        //public void 
        #endregion
    }
}