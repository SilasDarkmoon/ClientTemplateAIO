using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace UnityEngineEx
{
    public static class LanguageConverter
    {
        public class LangFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type format)
            {
                if (format == typeof(ICustomFormatter))
                    return this;
                return null;
            }

            private static readonly char[] _WordSplitChars = new[] { '/' };

            public string Format(string format, object arg, IFormatProvider provider)
            {
                if (format == null)
                {
                    if (arg == null)
                        return "";
                    if (arg is IFormattable)
                        return ((IFormattable)arg).ToString(format, provider);
                    return arg.ToString();
                }
                else
                {
                    if (format.StartsWith("`cnt`"))
                    {
                        // Examples:
                        // string.Format(Instance, "Look! {1} {0:`cnt`is/are} {0:`cnt`a} {0:`cnt`child/children}", 1, "There")
                        // string.Format(Instance, "Look! {1} {0:`cnt`is/are} {0:`cnt`a} {0:`cnt`child/children}", 10, "There")
                        string sub = format.Substring("`cnt`".Length);
                        var words = sub.Split(_WordSplitChars, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 0)
                        {
                            double cnt = 0;
                            try
                            {
                                cnt = Convert.ToDouble(arg);
                                if (Math.Abs(cnt) > 1)
                                {
                                    if (words.Length > 1)
                                    {
                                        return words[1];
                                    }
                                }
                                else
                                {
                                    return words[0];
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        if (arg is IFormattable)
                            return ((IFormattable)arg).ToString("", provider); // invalid format. so we set format to "" and call default formatter.
                        return arg.ToString();
                    }
                    else
                    {
                        if (arg is IFormattable)
                            return ((IFormattable)arg).ToString(format, provider);
                        return arg.ToString();
                    }
                }
            }

            public static readonly LangFormatter Instance = new LangFormatter();
        }

        public static class LanguageConverterConfig
        {
            public static string JSONPATH = "config/language.json";
        }

        private static Dictionary<string, string> _LangDict;
        public static Dictionary<string, string> LangDict
        {
            get
            {
                if (_LangDict == null)
                {
                    Init();
                }
                return _LangDict;
            }
            //private set
            //{
            //    _LangDict = value;
            //}
        }

        public static void Init()
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            ResInitializer.CheckInit();
#endif
            _LangDict = ResManager.TryLoadConfig(LanguageConverterConfig.JSONPATH) ?? new Dictionary<string, string>();
        }

        public static void UpdateDict(Dictionary<string, string> newMap)
        {
            if (newMap != null)
            {
                var dict = LangDict;
                foreach (var kvp in newMap)
                {
                    if (kvp.Value == null)
                    {
                        dict.Remove(kvp.Key);
                    }
                    else
                    {
                        dict[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        public static bool ContainsKey(string key)
        {
            return LangDict.ContainsKey(key);
        }

        public static string GetLangValue(string key, params object[] args)
        {
            string format = null, result = null;
            if (key == null)
            {
                PlatDependant.LogError("Language Converter - cannot convert null key.");
            }
            else
            {
                if (!LangDict.TryGetValue(key, out format))
                {
                    PlatDependant.LogError("Language Converter - cannot find key: " + key);
                }
                else
                {
                    if (format == null)
                    {
                        PlatDependant.LogError("Language Converter - null record for key: " + key);
                    }
                    else
                    {
                        if (args != null && args.Length > 0)
                        {
                            try
                            {
                                result = string.Format(LangFormatter.Instance, format, args);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                                System.Text.StringBuilder sbmess = new System.Text.StringBuilder();
                                sbmess.AppendLine("Language Converter - format failed.");
                                sbmess.Append("key: ");
                                sbmess.AppendLine(key);
                                sbmess.Append("format: ");
                                sbmess.AppendLine(format);
                                sbmess.Append("args: cnt ");
                                sbmess.AppendLine(args.Length.ToString());
                                for (int i = 0; i < args.Length; ++i)
                                {
                                    sbmess.AppendLine((args[i] ?? "null").ToString());
                                }
                                PlatDependant.LogError(sbmess);
                                result = format;
                            }
                        }
                        else
                        {
                            result = format;
                        }
                    }
                }
            }
            if (result != null)
            {
                return result;
            }
            if (format == null)
            {
                if (key == null)
                {
                    return null;
                }
                format = key;
            }
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(LangFormatter.Instance, format, args);
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                    System.Text.StringBuilder sbmess = new System.Text.StringBuilder();
                    sbmess.AppendLine("Language Converter - format failed.");
                    sbmess.Append("key: ");
                    sbmess.AppendLine(key);
                    sbmess.Append("format: ");
                    sbmess.AppendLine(format);
                    sbmess.Append("args: cnt ");
                    sbmess.AppendLine(args.Length.ToString());
                    for (int i = 0; i < args.Length; ++i)
                    {
                        sbmess.AppendLine((args[i] ?? "null").ToString());
                    }
                    PlatDependant.LogError(sbmess);
                    return format;
                }
            }
            else
            {
                return format;
            }
        }
        public static string Translate(string key, params object[] args)
        {
            return GetLangValue(key, args);
        }
        public static void Translate(ref string key, params object[] args)
        {
            key = GetLangValue(key, args);
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static void IterateText(Transform trans)
        {
            UnityEngine.UI.Text[] textArr = trans.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var item in textArr)
            {
                string txt = item.text;
                int posIndex = txt.IndexOf('@');
                if (posIndex >= 0)
                {
                    string langValue = GetLangValue(txt.Substring(posIndex + 1));
                    item.text = txt.Substring(0, posIndex) + (langValue ?? "");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
#if !UNITY_EDITOR
            ResManager.AddInitItem(ResManager.LifetimeOrders.PostResLoader - 5, Init);
#endif
        }
#endif

#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        static LanguageConverter()
        {
            Init();
        }
#endif
    }
}