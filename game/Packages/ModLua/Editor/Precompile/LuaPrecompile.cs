#if NET_UNITY_4_8 || (!UNITY_ENGINE && !UNITY_5_3_OR_NEWER) || UNITY_2021_1_OR_NEWER
#define RUNTIME_HAS_REF_STRUCT
#endif
#if UNITY_2020_2_OR_NEWER || NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1 || NETSTANDARD2_1_OR_GREATER
#define RUNTIME_HAS_READONLY_REF
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.Collections.Concurrent;
#else
using System.Collections.Concurrent;
#endif

using Types = LuaLib.Types;
using LuaHub = LuaLib.LuaHub;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static class LuaPrecompile
    {
        static LuaPrecompile()
        {
            LuaLib.BaseMethodMeta.OnReflectInvokeMember = LuaPrecompile.OnReflectInvokeMember;
        }

        private static List<string> _WhiteList;
        private static HashSet<string> _BlackList;
        private static Dictionary<string, Type> _TypeList;
        public class MemberInfo
        {
            public string FullStr;
            public string MemberType;
            public string TargetType;
            public string OwnerType;
            public string ReturnType;
            public string BodyStr;
            public string Name;

            public List<string> Comments;
            public HashSet<string> CommentSet; 

            public static readonly MemberInfo DefaultCtor = new MemberInfo() { MemberType = "ctor" };
        }
        private static Dictionary<Type, Dictionary<string, List<MemberInfo>>> _MemberList;
        public static MemberInfo ParseMemberInfo(string line)
        {
            if (line != null && line.StartsWith("member "))
            {
                List<string> comments = null;
                HashSet<string> commentSet = null;
                var commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                {
                    var comment = line.Substring(commentIndex + "//".Length);
                    line = line.Substring(0, commentIndex).TrimEnd();
                    var commentItems = comment.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (commentItems != null && commentItems.Length > 0)
                    {
                        comments = new List<string>();
                        commentSet = new HashSet<string>();
                        for (int i = 0; i < commentItems.Length; ++i)
                        {
                            comments.Add(commentItems[i]);
                            commentSet.Add(commentItems[i].ToLower());
                        }
                    }
                }

                var rest = line.Substring("member ".Length);
                var infos = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var minfo = new MemberInfo()
                {
                    FullStr = rest,
                    MemberType = infos[0],
                    TargetType = infos[1],
                    OwnerType = infos[2],
                    //ReturnType = infos[3],
                    BodyStr = infos.Length > 3 ? infos[3] : "",

                    Comments = comments,
                    CommentSet = commentSet,
                };
                var bindex = minfo.BodyStr.IndexOf('(');
                if (bindex >= 0)
                {
                    minfo.Name = minfo.BodyStr.Substring(0, bindex);
                }
                else
                {
                    minfo.Name = minfo.BodyStr;
                }
                return minfo;
            }
            return null;
        }
        public static void LoadMemberList()
        {
            if (!PlatDependant.IsFileExist("EditorOutput/LuaPrecompile/MemberList.txt"))
            {
                throw new InvalidOperationException("Please Run 'Lua/Parse Engine Member List' First.");
            }

            List<string> whitelist = _WhiteList = new List<string>();
            HashSet<string> blacklist = _BlackList = new HashSet<string>();
            var prelists = ModEditor.FindAssetsInMods("LuaPrecompile/MemberList.txt");
            foreach (var listfile in prelists)
            {
                using (var sr = PlatDependant.OpenReadText(listfile))
                {
                    while (true)
                    {
                        var line = sr.ReadLine();
                        if (line == null)
                            break;
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.StartsWith("--"))
                            {
                                line = TrimComment(line.Substring("--".Length).TrimStart());
                                blacklist.Add(line);
                            }
                            else
                            {
                                whitelist.Add(line);
                            }
                        }
                    }
                }
            }

            List<string> memberlines = new List<string>();
            Dictionary<string, int> lineindices = new Dictionary<string, int>();
            using (var sr = PlatDependant.OpenReadText("EditorOutput/LuaPrecompile/MemberList.txt"))
            {
                while (true)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        break;
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (!blacklist.Contains(TrimComment(line.Trim())))
                        {
                            lineindices[line] = memberlines.Count;
                            memberlines.Add(line);
                        }
                    }
                }
            }

            _TypeList = new Dictionary<string, Type>();
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var precompileAttribute = asm.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                if (precompileAttribute != null && precompileAttribute.Ignore)
                {
                    continue;
                }
                var types = asm.GetTypes(); // It seems that the GetTypes returns nested types.
                foreach (var type in types)
                {
                    precompileAttribute = type.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                    if (precompileAttribute != null && precompileAttribute.Ignore)
                    {
                        continue;
                    }
                    if (type.IsSubclassOf(typeof(Delegate)))
                    {
                        continue;
                    }
                    if (type.IsSubclassOf(typeof(LuaLib.BaseDynamic)))
                    {
                        continue;
                    }
                    if (typeof(LuaLib.ILuaWrapper).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var exsitinghubtype = LuaLib.LuaTypeHub.GetCachedTypeHubType(type);
                    if (exsitinghubtype != null)
                    {
                        if (exsitinghubtype.IsSubclassOf(typeof(LuaLib.LuaTypeHub.TypeHubCommonPrecompiled))
                            && (exsitinghubtype.Name != "TypeHubPrecompiled_" + GetFileNameForType(type.ToString()) || exsitinghubtype.DeclaringType.Name != "LuaHubEx"))
                        {
                            continue; // this is a hub file written by hand. ignore this.
                        }
                    }

                    var line = "type " + ReflectAnalyzer.GetIDString(type);
                    if (lineindices.ContainsKey(line))
                    {
                        _TypeList[type.FullName] = type;
                    }
                }
            }

            _MemberList = new Dictionary<Type, Dictionary<string, List<MemberInfo>>>();
            foreach (var line in memberlines)
            {
                if (line.Contains('*'))
                {
                    // deal with the pointers, this is ignored.
                    continue;
                }
                if (!string.IsNullOrEmpty(line))
                {
                    var minfo = ParseMemberInfo(line);
                    if (minfo != null)
                    {
                        if (_TypeList.ContainsKey(minfo.OwnerType))
                        {
                            var otype = _TypeList[minfo.OwnerType];
                            if (!_MemberList.ContainsKey(otype))
                            {
                                _MemberList[otype] = new Dictionary<string, List<MemberInfo>>();
                            }
                            if (!_MemberList[otype].ContainsKey(minfo.Name))
                            {
                                _MemberList[otype][minfo.Name] = new List<MemberInfo>();
                            }
                            _MemberList[otype][minfo.Name].Add(minfo);
                        }
                    }
                }
            }
        }
        public static Dictionary<string, Type> GetTypeList()
        {
            if (_TypeList == null)
            {
                LoadMemberList();
            }
            return _TypeList;
        }
        public static Dictionary<string, Type> GetFullTypeList()
        {
            if (!PlatDependant.IsFileExist("EditorOutput/LuaPrecompile/MemberList.txt"))
            {
                throw new InvalidOperationException("Please Run 'Lua/Parse Engine Member List' First.");
            }

            List<string> memberlines = new List<string>();
            Dictionary<string, int> lineindices = new Dictionary<string, int>();
            using (var sr = PlatDependant.OpenReadText("EditorOutput/LuaPrecompile/MemberList.txt"))
            {
                while (true)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        break;
                    if (!string.IsNullOrEmpty(line))
                    {
                        {
                            lineindices[line] = memberlines.Count;
                            memberlines.Add(line);
                        }
                    }
                }
            }

            var typelist = new Dictionary<string, Type>();
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var types = asm.GetTypes(); // It seems that the GetTypes returns nested types.
                foreach (var type in types)
                {
                    var line = "type " + ReflectAnalyzer.GetIDString(type);
                    if (lineindices.ContainsKey(line))
                    {
                        typelist[type.FullName] = type;
                    }
                }
            }

            return typelist;
        }
        public static Dictionary<Type, Dictionary<string, List<MemberInfo>>> GetMemberList()
        {
            if (_MemberList == null)
            {
                LoadMemberList();
            }
            return _MemberList;
        }
        public static List<string> ParseWhiteListForPrecompileAttribute()
        {
            List<string> list = new List<string>();
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                bool allType = false;
                var precompileAttribute = asm.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                if (precompileAttribute != null && precompileAttribute.Ignore)
                {
                    continue;
                }
                else if (precompileAttribute != null && !precompileAttribute.Ignore)
                {
                    allType = true;
                }
                var types = asm.GetTypes(); // It seems that the GetTypes returns nested types.
                foreach (var type in types)
                {
                    bool allmember = false;
                    precompileAttribute = type.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                    if (allType)
                    {
                        if (precompileAttribute != null && precompileAttribute.Ignore)
                        {
                            continue;
                        }
                        allmember = true;
                    }
                    else
                    {
                        if (precompileAttribute != null && precompileAttribute.Ignore)
                        {
                            continue;
                        }
                        else if (precompileAttribute != null && !precompileAttribute.Ignore)
                        {
                            allmember = true;
                        }
                    }

                    var typestr = ReflectAnalyzer.GetIDString(type);
                    if (allmember)
                    {
                        list.Add("type " + typestr);
                        list.Add("member * * " + typestr + " *");
                    }
                    else
                    {
                        foreach (var member in type.GetMembers())
                        {
                            precompileAttribute = member.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                            if (precompileAttribute != null && precompileAttribute.Ignore)
                            {
                                continue;
                            }
                            else if (precompileAttribute != null && !precompileAttribute.Ignore)
                            {
                                list.Add("member * * " + typestr + " " + ReflectAnalyzer.GetIDString(member));
                            }
                        }
                    }
                }
            }
            return list;
        }
        public static List<string> ParseFullWhiteList()
        {
            List<string> list = new List<string>();
            HashSet<string> uniqueset = new HashSet<string>();
            if (_WhiteList != null)
            {
                foreach (var line in _WhiteList)
                {
                    if (uniqueset.Add(line))
                    {
                        list.Add(line);
                    }
                }
            }
            foreach (var line in ParseWhiteListForPrecompileAttribute())
            {
                if (uniqueset.Add(line))
                {
                    list.Add(line);
                }
            }
            //if (PlatDependant.IsFileExist("EditorOutput/LuaPrecompile/CachedCommands.txt"))
            //{
            //    try
            //    {
            //        using (var sr = PlatDependant.OpenReadText("EditorOutput/LuaPrecompile/CachedCommands.txt"))
            //        {
            //            while (true)
            //            {
            //                var line = sr.ReadLine();
            //                if (line == null)
            //                    break;

            //                if (!string.IsNullOrEmpty(line))
            //                {
            //                    if (uniqueset.Add(line))
            //                    {
            //                        list.Add(line);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        PlatDependant.LogError(e);
            //    }
            //}
            return list;
        }

        public static void CheckBasePrecompileFiles()
        {
            var manmod = ModEditorUtils.__MOD__;
            var manidir = "Assets/Mods/" + manmod + "/LuaHubSub/";
            var srcdir = System.IO.Path.GetDirectoryName(ModEditorUtils.__FILE__) + "/../../Runtime/Precompile/";

            if (!PlatDependant.IsFileExist(manidir + "LuaPrecompileLoaderEx.cs"))
            {
                PlatDependant.CopyFile(srcdir + ".LuaPrecompileLoaderEx.cs", manidir + "LuaPrecompileLoaderEx.cs");
            }
            if (!PlatDependant.IsFileExist(manidir + "LuaPrecompileLoaderEx.cs.meta"))
            {
                PlatDependant.CopyFile(srcdir + ".LuaPrecompileLoaderEx.cs.meta", manidir + "LuaPrecompileLoaderEx.cs.meta");
            }
            if (!PlatDependant.IsFileExist(manidir + "Resources/LuaPrecompileLoaderEx.asset"))
            {
                PlatDependant.CopyFile(srcdir + ".LuaPrecompileLoaderEx.asset", manidir + "Resources/LuaPrecompileLoaderEx.asset");
            }
            if (!PlatDependant.IsFileExist(manidir + "Resources/LuaPrecompileLoaderEx.asset.meta"))
            {
                PlatDependant.CopyFile(srcdir + ".LuaPrecompileLoaderEx.asset.meta", manidir + "Resources/LuaPrecompileLoaderEx.asset.meta");
            }
        }

        private static volatile int _IsAsyncCompileWorking = 0;
        private static volatile HashSet<string> _AyncPrecompileMembers = null;
        private static volatile Dictionary<string, List<string>> _CachedFiles = null;
        private static ConcurrentQueue<string> _PrecompileCommands = new ConcurrentQueue<string>();
        private static System.Threading.AutoResetEvent _FinishWriting = new System.Threading.AutoResetEvent(false);
        private static System.Threading.AutoResetEvent _PrecompileWorkArrived = new System.Threading.AutoResetEvent(false);

        public static void OnReflectInvokeMember(Type type, string member)
        {
            if (type == null || string.IsNullOrEmpty(member))
            {
                return;
            }
            var command = "member * * " + ReflectAnalyzer.GetIDString(type) + " " + member;
            WritePrecompileFuncForMemberAsync(command);
        }

        public static void SortPrecompileMemberList()
        {
            var cachedPath = "Assets/Mods/" + ModEditorUtils.__MOD__ + "/LuaPrecompile/MemberList.txt";
            var lines = PlatDependant.ReadAllLines(cachedPath);
            Array.Sort(lines);
            HashSet<string> filter = new HashSet<string>();
            using (var sw = PlatDependant.OpenWriteText(cachedPath))
            {
                for (int i = 0; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (filter.Add(line))
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        public static void WritePrecompileFuncForMemberAsync(string memberstr)
        {
            if (SafeInitializerUtils.IsInitializingInUnityCtor) return;
            if (_IsAsyncCompileWorking != 0 || ThreadSafeValues.IsMainThread && Application.isPlaying)
            {
                if (System.Threading.Interlocked.CompareExchange(ref _IsAsyncCompileWorking, 1, 0) == 0)
                {
                    _CachedFiles = new Dictionary<string, List<string>>();

                    PlatDependant.RunBackground(prog =>
                    {
                        HashSet<string> recordedMembers = new HashSet<string>();
                        HashSet<string> compiledMembers = new HashSet<string>();

                        var cachedPath = "Assets/Mods/" + ModEditorUtils.__MOD__ + "/LuaPrecompile/MemberList.txt";
                        //var cachedPath = "EditorOutput/LuaPrecompile/CachedCommands.txt";
                        if (PlatDependant.IsFileExist(cachedPath))
                        {
                            try
                            {
                                using (var sr = PlatDependant.OpenReadText(cachedPath))
                                {
                                    while (true)
                                    {
                                        var line = sr.ReadLine();
                                        if (line == null)
                                            break;

                                        if (!string.IsNullOrEmpty(line))
                                        {
                                            recordedMembers.Add(line);
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }

                        using (var swrecord = PlatDependant.OpenAppendText(cachedPath))
                        {
                            while (_PrecompileWorkArrived.WaitOne())
                            {
                                string command;
                                while (_PrecompileCommands.TryDequeue(out command))
                                {
                                    if (command == "-")
                                    {
                                        break;
                                    }
                                    if (compiledMembers.Add(command))
                                    {
                                        if (recordedMembers.Add(command))
                                        {
                                            try
                                            {
                                                swrecord.WriteLine(command);
                                            }
                                            catch (Exception e)
                                            {
                                                PlatDependant.LogError(e);
                                            }
                                        }
#if ENABLE_LUA_PRECOMPILE_WHILE_PLAYING
                                        WritePrecompileFuncForMember(command, false);
#endif
                                    }
                                }
                                if (command == "-")
                                {
                                    break;
                                }
                            }
                        }
                        SortPrecompileMemberList();

                        var cached = _CachedFiles;
                        _CachedFiles = null;

                        foreach (var kvp in cached)
                        {
                            WriteLines(kvp.Key, kvp.Value);
                        }

                        _AyncPrecompileMembers = recordedMembers;

                        _IsAsyncCompileWorking = 0;
                        _FinishWriting.Set();
                    });

                    Action onPlayModeChanged = null;
                    onPlayModeChanged = () =>
                    {
                        if (!Application.isPlaying)
                        {
                            EditorBridge.OnPlayModeChanged -= onPlayModeChanged;

                            _PrecompileCommands.Enqueue("-");
                            _PrecompileWorkArrived.Set();

                            _FinishWriting.WaitOne();
                            AssetDatabase.Refresh();
                        }
                    };
                    EditorBridge.OnPlayModeChanged += onPlayModeChanged;
                }

                _PrecompileCommands.Enqueue(memberstr);
                _PrecompileWorkArrived.Set();
            }
            else
            {
                var cachedPath = "Assets/Mods/" + ModEditorUtils.__MOD__ + "/LuaPrecompile/MemberList.txt";
                //var cachedPath = "EditorOutput/LuaPrecompile/CachedCommands.txt";
                if (_AyncPrecompileMembers == null)
                {
                    _AyncPrecompileMembers = new HashSet<string>();

                    if (PlatDependant.IsFileExist(cachedPath))
                    {
                        try
                        {
                            using (var sr = PlatDependant.OpenReadText(cachedPath))
                            {
                                while (true)
                                {
                                    var line = sr.ReadLine();
                                    if (line == null)
                                        break;

                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        _AyncPrecompileMembers.Add(line);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                if (_AyncPrecompileMembers.Add(memberstr))
                {
                    try
                    {
                        using (var sw = PlatDependant.OpenAppendText(cachedPath))
                        {
                            sw.WriteLine(memberstr);
                        }
                        SortPrecompileMemberList();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
#if ENABLE_LUA_PRECOMPILE_WHILE_PLAYING
                WritePrecompileFuncForMember(memberstr, false);
                AssetDatabase.Refresh();
#endif
            }
        }

        public static List<string> ReadRawLines(string path)
        {
            if (_CachedFiles != null && _CachedFiles.ContainsKey(path))
            {
                return _CachedFiles[path];
            }
            List<string> data = new List<string>();
            using (var sr = PlatDependant.OpenReadText(path))
            {
                if (sr != null)
                {
                    while (true)
                    {
                        var line = sr.ReadLine();
                        if (line == null)
                            break;
                        data.Add(line.Trim());
                    }
                }
            }
            if (_CachedFiles != null)
            {
                _CachedFiles[path] = data;
            }
            return data;
        }
        public static void WriteLines(string path, List<string> lines)
        {
            if (_CachedFiles != null)
            {
                return;
            }
            System.IO.StreamWriter sw = null;
            while (sw == null)
            {
                using (sw = PlatDependant.OpenWriteText(path))
                {
                    if (sw != null)
                    {
                        int tab = 0;
                        foreach (var line in lines)
                        {
                            if (line == "}")
                                --tab;
                            for (int i = 0; i < tab; ++i)
                            {
                                sw.Write("    ");
                            }
                            sw.WriteLine(line);
                            if (line == "{")
                                ++tab;
                        }
                        sw.Flush();
                    }
                }
            }
        }
        public static List<string> TrimRawLines(List<string> lines)
        {
            for (int i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                var innerlines = line.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (innerlines.Length > 1)
                {
                    lines.RemoveAt(i);
                    lines.InsertRange(i, innerlines);
                    line = innerlines[0];
                }
                lines[i] = line.Trim();
            }
            return lines;
        }
        public static void FindTag(List<string> lines, string tag, out int start, out int end)
        {
            start = -1;
            end = -1;
            string strstart = "#region " + tag;
            string strend = "#endregion // " + tag;
            for (int i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                if (line == strstart)
                {
                    start = i;
                }
                else if (line == strend)
                {
                    end = i;
                    break;
                }
            }
        }
        public static int EncloseBlock(List<string> lines, int start)
        {
            int level = 0;
            for (int i = start; i < lines.Count; ++i)
            {
                var line = lines[i];
                if (line == "{")
                    ++level;
                else if (line == "}")
                    --level;
                if (level == 0)
                    return i;
            }
            return -1;
        }

        public static bool IsNativeType(Type type)
        {
            return LuaHub.IsConvertible(type) && !type.IsEnum() || typeof(Type).IsAssignableFrom(type) || typeof(LuaLib.ILuaTypeHub).IsAssignableFrom(type);
        }
        private static HashSet<Type> _LongerThanDoubleNumberTypes = new HashSet<Type>()
        {
            typeof(long),
            typeof(ulong),
            typeof(decimal),
        };
        public static bool IsLongerThanDoubleNumberType(Type type)
        {
            return _LongerThanDoubleNumberTypes.Contains(type);
        }
        public static bool IsOverride(System.Reflection.MethodBase method)
        {
            var dtype = method.DeclaringType;
            var rtype = method.ReflectedType;
            return dtype == rtype && method.IsVirtual && (method.Attributes & System.Reflection.MethodAttributes.NewSlot) == 0;
        }
        //public static string GetTypeCate(Type type)
        //{
        //    if (type == null)
        //        return null;
        //    if (type.Assembly.FullName.Contains("UnityEditor"))
        //        return "Editor";
        //    if (type.Assembly == typeof(LuaPrecompile).Assembly)
        //        return "Editor";
        //    //if (type.Assembly == typeof(LuaLib.LuaHubClient).Assembly)
        //    //    return "Client";
        //    return "";
        //}
        public static string GetPrecompileFilePath(Type type)
        {
            var filename = GetFileNameForType(type.ToString());

            var manmod = ModEditorUtils.__MOD__;
            var manidir = "Assets/Mods/" + manmod + "/LuaHubSub/";

            return manidir + "LuaHub_" + filename + ".cs";
        }

        public static Dictionary<string, string> OpMap = new Dictionary<string, string>()
        {
            { "op_Addition", "__add" },
            { "op_Multiply", "__mul" },
            { "op_Subtraction", "__sub" },
            { "op_Division", "__div" },
            { "op_Modulus", "__mod" },
            { "op_LessThan", "__lt" },
            { "op_LessThanOrEqual", "__le" },
        };
        public class SpecialTypeInfo
        {
            public Type type;
            public string sname;
            public string convto;
            public string pushfunc;
            public string getfunc;
        }
        public static Dictionary<Type, string> ExplicitToDoubleTypes = new Dictionary<Type, string>()
        {
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
        };
        public static Dictionary<Type, SpecialTypeInfo> SpecialTypes = new Dictionary<Type, SpecialTypeInfo>()
        {
            { typeof(bool), new SpecialTypeInfo(){ type = typeof(bool), sname = "int", pushfunc = "lua_pushboolean", getfunc = "lua_toboolean" } },
            { typeof(byte), new SpecialTypeInfo(){ type = typeof(byte), sname = "unsigned char", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(char), new SpecialTypeInfo(){ type = typeof(char), sname = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(decimal), new SpecialTypeInfo(){ type = typeof(decimal), sname = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(double), new SpecialTypeInfo(){ type = typeof(double), sname = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(float), new SpecialTypeInfo(){ type = typeof(float), sname = "float", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(int), new SpecialTypeInfo(){ type = typeof(int), sname = "int", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(long), new SpecialTypeInfo(){ type = typeof(long), sname = "long long", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(sbyte), new SpecialTypeInfo(){ type = typeof(sbyte), sname = "signed char", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(short), new SpecialTypeInfo(){ type = typeof(short), sname = "short", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(uint), new SpecialTypeInfo(){ type = typeof(uint), sname = "unsigned int", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(ulong), new SpecialTypeInfo(){ type = typeof(ulong), sname = "unsigned long long", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(ushort), new SpecialTypeInfo(){ type = typeof(ushort), sname = "unsigned short", convto = "double", pushfunc = "lua_pushnumber", getfunc = "lua_tonumber" } },
            { typeof(IntPtr), new SpecialTypeInfo(){ type = typeof(IntPtr), sname = "void*", pushfunc = "lua_pushlightuserdata", getfunc = "lua_touserdata" } },
        };

        public static Type GetPrecompileBaseType(Type type)
        {
            if (type == null)
            {
                return null;
            }
            else if (type.IsValueType)
            {
                return null;
            }
            var basetype = type.BaseType;
            if (basetype == null)
            {
                return null;
            }
            else if (basetype == typeof(object))
            {
                return null;
            } 
            else if (basetype.IsGenericType())
            {
                return GetPrecompileBaseType(basetype); // TODO: generic support
            }
            else if (!GetTypeList().ContainsKey(basetype.ToString()))
            {
                return GetPrecompileBaseType(basetype);
            }
            else
            {
                return basetype;
            }
        }
        public static string GetFileNameForType(string typestr)
        {
            var filename = typestr;
            var gindex = filename.IndexOf('`');
            if (gindex >= 0)
            {
                filename = filename.Substring(0, gindex);
            }
            filename = filename.Replace('.', '_');
            filename = filename.Replace('+', '_');
            return filename;
        }
        public static void WritePrecompileFileForType(string typestr)
        {
            WritePrecompileFileForType(typestr, false);
        }
        public static void WritePrecompileFileForType(string typestr, bool overwrite)
        {
            var typelist = GetTypeList();
            Type type;
            typelist.TryGetValue(typestr, out type);
            if (type == null || type == typeof(object))
                return;
            if (type.IsGenericType())
                return; // TODO: generic support.
#if RUNTIME_HAS_REF_STRUCT
            if (type.IsByRefLike)
                return;
#endif

            var filename = GetFileNameForType(typestr);
            var path = GetPrecompileFilePath(type);
            if (PlatDependant.IsFileExist(path) && !overwrite)
                return;

            CheckBasePrecompileFiles();

            var sb_type = new System.Text.StringBuilder();
            sb_type.WriteType(type);
            var basetype = GetPrecompileBaseType(type);
            if (basetype != null)
            {
                WritePrecompileFileForType(basetype.ToString());
            }

            var outtertype = type.DeclaringType;
            if (outtertype != null)
            {
                WritePrecompileFileForType(outtertype.ToString());
            }

            var sbline = new System.Text.StringBuilder();
            var lines = ReadRawLines(path);
            if (lines.Count > 0 && !overwrite)
                return;
            lines.Clear();
            lines.Add("#pragma warning disable CS0162");
            lines.Add("#if !DISABLE_LUA_PRECOMPILE");
            lines.Add("using System;");
            lines.Add("");
            lines.Add("using lua = LuaLib.LuaCoreLib;");
            lines.Add("using lual = LuaLib.LuaAuxLib;");
            lines.Add("using luae = LuaLib.LuaLibEx;");
            lines.Add("");
            lines.Add("namespace LuaLib");
            lines.Add("{");
            lines.Add("public static partial class LuaHubEx");
            lines.Add("{");
            // write class name
            sbline.Remove(0, sbline.Length);
            sbline.Append("public class TypeHubPrecompiled_");
            sbline.Append(filename);
            sbline.Append(" : ");
            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    sbline.Append("LuaLib.LuaTypeHub.TypeHubEnumPrecompiled<");
                    sbline.Append(sb_type);
                    sbline.Append(">");
                }
                else
                {
                    sbline.Append("LuaLib.LuaTypeHub.TypeHubValueTypePrecompiled<");
                    sbline.Append(sb_type);
                    sbline.Append(">");
                    if (IsNativeType(type))
                    {
                        sbline.Append(", ILuaNative");
                    }
                }
            }
            else
            {
                if (basetype != null)
                {
                    sbline.Append("LuaHubEx");
                    sbline.Append(".");
                    sbline.Append("TypeHubPrecompiled_");
                    sbline.Append(GetFileNameForType(basetype.ToString()));
                }
                else
                {
                    sbline.Append("LuaLib.LuaTypeHub.TypeHubCommonPrecompiled");
                }
            }
            lines.Add(sbline.ToString());
            lines.Add("{");
            {
                // write ctor
                sbline.Remove(0, sbline.Length);
                sbline.Append("public TypeHubPrecompiled_");
                sbline.Append(filename);
                sbline.Append("()");
                if (!type.IsValueType)
                {
                    sbline.Append(" : this(typeof(");
                    sbline.Append(sb_type);
                    sbline.Append(")) { }");
                    lines.Add(sbline.ToString());
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public TypeHubPrecompiled_");
                    sbline.Append(filename);
                    sbline.Append("(Type type) : base(type)");
                }
                lines.Add(sbline.ToString());
                lines.Add("{");
                {
                    if (type.IsValueType && !IsNativeType(type) && !type.IsEnum)
                    {
                        lines.Add("#if !DISABLE_LUA_HUB_C");
                        lines.Add("if (LuaHub.LuaHubC.Ready)");
                        lines.Add("{");
                        sbline.Remove(0, sbline.Length);
                        sbline.Append("//LuaHub.LuaHubC.lua_setType");
                        sbline.Append(type.Name);
                        sbline.Append("(r);");
                        sbline.Append("// Notice: generate hubc to improve this.");
                        lines.Add(sbline.ToString());
                        lines.Add("}");
                        lines.Add("#endif");
                    }
                    lines.Add("#region REG_I_FUNC");
                    lines.Add("#endregion // REG_I_FUNC");
                    lines.Add("#region REG_I_PROP");
                    lines.Add("#endregion // REG_I_PROP");
                    lines.Add("#region REG_G_I_FUNC");
                    lines.Add("#endregion // REG_G_I_FUNC");
                    lines.Add("#region REG_I_INDEX");
                    lines.Add("#endregion // REG_I_INDEX");
                }
                lines.Add("}");
                // write ctor END
                // write reg static
                lines.Add("public override void RegPrecompiledStatic()");
                lines.Add("{");
                lines.Add("#region REG_I_CTOR");
                lines.Add("#endregion // REG_I_CTOR");
                lines.Add("#region REG_S_FUNC");
                lines.Add("#endregion // REG_S_FUNC");
                lines.Add("#region REG_S_PROP");
                lines.Add("#endregion // REG_S_PROP");
                lines.Add("#region REG_G_S_FUNC");
                lines.Add("#endregion // REG_G_S_FUNC");
                lines.Add("#region REG_S_OP");
                lines.Add("#endregion // REG_S_OP");
                lines.Add("#region REG_S_CONV");
                lines.Add("#endregion // REG_S_CONV");
                lines.Add("#region REG_G_GTYPES");
                lines.Add("#endregion // REG_G_GTYPES");
                lines.Add("}");
                // write reg static END
                lines.Add("");
                lines.Add("#region DEL_I_CTOR");
                lines.Add("#endregion // DEL_I_CTOR");
                lines.Add("#region DEL_I_FUNC");
                lines.Add("#endregion // DEL_I_FUNC");
                lines.Add("#region DEL_I_PROP");
                lines.Add("#endregion // DEL_I_PROP");
                lines.Add("#region DEL_S_FUNC");
                lines.Add("#endregion // DEL_S_FUNC");
                lines.Add("#region DEL_S_PROP");
                lines.Add("#endregion // DEL_S_PROP");
                lines.Add("#region DEL_G_I_FUNC");
                lines.Add("#endregion // DEL_G_I_FUNC");
                lines.Add("#region DEL_I_INDEX");
                lines.Add("#endregion // DEL_I_INDEX");
                lines.Add("#region DEL_G_S_FUNC");
                lines.Add("#endregion // DEL_G_S_FUNC");
                lines.Add("#region DEL_S_OP");
                lines.Add("#endregion // DEL_S_OP");
                lines.Add("#region DEL_S_CONV");
                lines.Add("#endregion // DEL_S_CONV");
                lines.Add("#region DEL_G_GTYPES");
                lines.Add("#endregion // DEL_G_GTYPES");
                lines.Add("");
                lines.Add("#region FUNC_I_CTOR");
                lines.Add("#endregion // FUNC_I_CTOR");
                lines.Add("#region FUNC_I_FUNC");
                lines.Add("#endregion // FUNC_I_FUNC");
                lines.Add("#region FUNC_I_PROP");
                lines.Add("#endregion // FUNC_I_PROP");
                lines.Add("#region FUNC_S_FUNC");
                lines.Add("#endregion // FUNC_S_FUNC");
                lines.Add("#region FUNC_S_PROP");
                lines.Add("#endregion // FUNC_S_PROP");
                lines.Add("#region FUNC_G_I_FUNC");
                lines.Add("#endregion // FUNC_G_I_FUNC");
                lines.Add("#region FUNC_I_INDEX");
                lines.Add("#endregion // FUNC_I_INDEX");
                lines.Add("#region FUNC_G_S_FUNC");
                lines.Add("#endregion // FUNC_G_S_FUNC");
                lines.Add("#region FUNC_S_OP");
                lines.Add("#endregion // FUNC_S_OP");
                lines.Add("#region FUNC_S_CONV");
                lines.Add("#endregion // FUNC_S_CONV");
                lines.Add("#region FUNC_G_GTYPES");
                lines.Add("#endregion // FUNC_G_GTYPES");
                lines.Add("");
                lines.Add("#region NESTED_TYPE_HUB");
                lines.Add("#endregion // NESTED_TYPE_HUB");
            }
            if (type.IsValueType)
            {
                if (IsNativeType(type))
                {
                    // write override methods
                    lines.Add("");
                    lines.Add("public override IntPtr PushLua(IntPtr l, object val)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("PushLua(l, (");
                    sbline.Append(sb_type);
                    sbline.Append(")val);");
                    lines.Add(sbline.ToString());
                    lines.Add("return IntPtr.Zero;");
                    lines.Add("}");
                    lines.Add("public override void SetData(IntPtr l, int index, object val)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("SetDataRaw(l, index, (");
                    sbline.Append(sb_type);
                    sbline.Append(")val);");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    lines.Add("public override object GetLuaObject(IntPtr l, int index)");
                    lines.Add("{");
                    lines.Add("return GetLuaRaw(l, index);");
                    lines.Add("}");

                    lines.Add("");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override IntPtr PushLua(IntPtr l, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("l.checkstack(3);");
                    lines.Add("l.newtable(); // ud");
                    lines.Add("SetDataRaw(l, -1, val);");
                    lines.Add("l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud #trans");
                    lines.Add("l.pushlightuserdata(r); // ud #trans trans");
                    lines.Add("l.rawset(-3); // ud");
                    lines.Add("");
                    lines.Add("PushToLuaCached(l); // ud type");
                    lines.Add("l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud type #meta");
                    lines.Add("l.rawget(-2); // ud type meta");
                    lines.Add("l.setmetatable(-3); // ud type");
                    lines.Add("l.pop(1); // ud");
                    lines.Add("return IntPtr.Zero;");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override void SetData(IntPtr l, int index, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("SetDataRaw(l, index, val);");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override ");
                    sbline.Append(sb_type);
                    sbline.Append(" GetLua(IntPtr l, int index)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("return GetLuaRaw(l, index);");
                    lines.Add("}");
                    lines.Add("public void Wrap(IntPtr l, int index)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append(sb_type);
                    sbline.Append(" val;");
                    lines.Add(sbline.ToString());
                    lines.Add("l.GetLua(index, out val);");
                    lines.Add("PushLua(l, val);");
                    lines.Add("}");
                    lines.Add("public void Unwrap(IntPtr l, int index)");
                    lines.Add("{");
                    lines.Add("var val = GetLuaRaw(l, index);");
                    if (IsLongerThanDoubleNumberType(type))
                    {
                        lines.Add("l.pushnumber((double)val);");
                    }
                    else
                    {
                        lines.Add("l.PushLua(val);");
                    }
                    lines.Add("}");
                    string luatype;
                    if (!LuaPrecompileWriter.nativeTypeMap.TryGetValue(type, out luatype))
                    {
                        luatype = "LUA_TTABLE";
                    }
                    sbline.Clear();
                    sbline.Append("public int LuaType { get { return lua.");
                    sbline.Append(luatype);
                    sbline.Append("; } }");
                    lines.Add(sbline.ToString());

                    lines.Add("");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static void SetDataRaw(IntPtr l, int index, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("l.checkstack(3);");
                    lines.Add("l.pushvalue(index); // otab");
                    lines.Add("l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar");
                    lines.Add("l.PushLua(val); // otab #tar val");
                    lines.Add("l.rawset(-3); // otab");
                    lines.Add("l.pop(1);");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static ");
                    sbline.Append(sb_type);
                    sbline.Append(" GetLuaRaw(IntPtr l, int index)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append(sb_type);
                    sbline.Append(" rv;");
                    lines.Add(sbline.ToString());
                    lines.Add("l.checkstack(2);");
                    lines.Add("l.pushvalue(index); // otab");
                    lines.Add("l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar");
                    lines.Add("l.rawget(-2); // otab val");
                    lines.Add("l.GetLua(-1, out rv);");
                    lines.Add("l.pop(2); // X");
                    lines.Add("return rv;");
                    lines.Add("}");
                    // write override methods END
                }
                else if (type.IsEnum)
                {
                    // write override methods
                    lines.Add("");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override ");
                    sbline.Append(sb_type);
                    sbline.Append(" ConvertFromNum(double val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("return (");
                    sbline.Append(sb_type);
                    sbline.Append(")val;");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override double ConvertToNum(");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("return (double)val;");
                    lines.Add("}");
                }
                else
                {
                    lines.Add("");
                    lines.Add("#region VALUE_TYPE_PLAIN_FIELDS");
                    lines.Add("#endregion // VALUE_TYPE_PLAIN_FIELDS");
                    //// have any plain fields?
                    //if (_ValueTypePlainFields != null && _ValueTypePlainFields.ContainsKey(typestr))
                    //{
                    //    var plainfields = _ValueTypePlainFields[typestr];
                    //    lines.Add("");
                    //    for (int i = 0; i < plainfields.Count; ++i)
                    //    {
                    //        var field = plainfields[i];
                    //        sbline.Clear();
                    //        sbline.Append("public static readonly LuaString LS_");
                    //        sbline.Append(field.ToUpper());
                    //        sbline.Append(" = new LuaString(\"");
                    //        sbline.Append(field);
                    //        sbline.Append("\");");
                    //        lines.Add(sbline.ToString());
                    //    }
                    //}

                    // write override methods
                    lines.Add("");
                    lines.Add("public override IntPtr PushLua(IntPtr l, object val)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("PushLua(l, (");
                    sbline.Append(sb_type);
                    sbline.Append(")val);");
                    lines.Add(sbline.ToString());
                    lines.Add("return IntPtr.Zero;");
                    lines.Add("}");
                    lines.Add("public override void SetData(IntPtr l, int index, object val)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("SetDataRaw(l, index, (");
                    sbline.Append(sb_type);
                    sbline.Append(")val);");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    lines.Add("public override object GetLuaObject(IntPtr l, int index)");
                    lines.Add("{");
                    lines.Add("return GetLuaRaw(l, index);");
                    lines.Add("}");

                    lines.Add("");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override IntPtr PushLua(IntPtr l, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("return base.PushLua(l, (object)val); // Notice: generate hubc to improve this.");
                    lines.Add("#if !DISABLE_LUA_HUB_C");
                    lines.Add("if (LuaHub.LuaHubC.Ready)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("//LuaHub.LuaHubC.lua_push");
                    sbline.Append(type.Name);
                    sbline.Append("(l, fields);");
                    sbline.Append("// Notice: generate hubc to improve this.");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    lines.Add("else");
                    lines.Add("#endif");
                    lines.Add("{");
                    lines.Add("l.checkstack(3);");
                    lines.Add("l.newtable(); // ud");
                    lines.Add("SetDataRaw(l, -1, val);");
                    lines.Add("PushToLuaCached(l); // ud type");
                    lines.Add("l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud type #meta");
                    lines.Add("l.rawget(-2); // ud type meta");
                    lines.Add("l.setmetatable(-3); // ud type");
                    lines.Add("l.pop(1); // ud");
                    lines.Add("}");
                    lines.Add("return IntPtr.Zero;");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override void SetData(IntPtr l, int index, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("SetDataRaw(l, index, val);");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public override ");
                    sbline.Append(sb_type);
                    sbline.Append(" GetLua(IntPtr l, int index)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("return GetLuaRaw(l, index);");
                    lines.Add("}");

                    lines.Add("");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static void SetDataRaw(IntPtr l, int index, ");
                    sbline.Append(sb_type);
                    sbline.Append(" val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("LuaCommonMeta.LuaTransCommon.Instance.SetData(l, index, val); // Notice: generate hubc to improve this.");
                    lines.Add("return; // Notice: generate hubc to improve this.");
                    lines.Add("#if !DISABLE_LUA_HUB_C");
                    lines.Add("if (LuaHub.LuaHubC.Ready)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("//LuaHub.LuaHubC.lua_set");
                    sbline.Append(type.Name);
                    sbline.Append("(l, index, fields);");
                    sbline.Append("// Notice: generate hubc to improve this.");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    lines.Add("else");
                    lines.Add("#endif");
                    lines.Add("{");
                    lines.Add("// Notice: generate hubc to improve this.");
                    lines.Add("}");
                    lines.Add("}");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static ");
                    sbline.Append(sb_type);
                    sbline.Append(" GetLuaRaw(IntPtr l, int index)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("// Notice: generate hubc to improve this.");
                    lines.Add("var rawobj = LuaCommonMeta.LuaTransCommon.Instance.GetLua(l, index);");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("if (rawobj is ");
                    sbline.Append(sb_type);
                    sbline.Append(")");
                    lines.Add(sbline.ToString());
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("return (");
                    sbline.Append(sb_type);
                    sbline.Append(")rawobj;");
                    lines.Add(sbline.ToString());
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("return default(");
                    sbline.Append(sb_type);
                    sbline.Append(");");
                    lines.Add(sbline.ToString());
                    lines.Add("// Notice: generate hubc to improve this.");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append(sb_type);
                    sbline.Append(" rv = new ");
                    sbline.Append(sb_type);
                    sbline.Append("();");
                    lines.Add(sbline.ToString());
                    lines.Add("#if !DISABLE_LUA_HUB_C");
                    lines.Add("if (LuaHub.LuaHubC.Ready)");
                    lines.Add("{");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("//LuaHub.LuaHubC.lua_get");
                    sbline.Append(type.Name);
                    sbline.Append("(l, index, out fields);");
                    sbline.Append("// Notice: generate hubc to improve this.");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                    lines.Add("else");
                    lines.Add("#endif");
                    lines.Add("{");
                    lines.Add("// Notice: generate hubc to improve this.");
                    lines.Add("}");
                    lines.Add("return rv;");
                    lines.Add("}");
                    // write override methods END
                }
            }
            else if (type == typeof(Type))
            {
                lines.Add("");
                lines.Add("public override IntPtr PushLua(IntPtr l, object val)");
                lines.Add("{");
                lines.Add("var type = val as Type;");
                lines.Add("if (type == null)");
                lines.Add("{");
                lines.Add("l.pushnil();");
                lines.Add("}");
                lines.Add("else");
                lines.Add("{");
                lines.Add("l.PushLuaType(type);");
                lines.Add("}");
                lines.Add("return IntPtr.Zero;");
                lines.Add("}");
            }
            lines.Add("}");
            // write class name END
            // write instance
#if LUA_PRECOMPILE_INSTANT_TYPE_HUB_CREATION
            if (outtertype == null)
            {
                sbline.Remove(0, sbline.Length);
                sbline.Append("private static TypeHubPrecompiled_");
                sbline.Append(filename);
                sbline.Append(" ___tp_");
                sbline.Append(filename);
                sbline.Append(" = new TypeHubPrecompiled_");
                sbline.Append(filename);
                sbline.Append("();");
                lines.Add(sbline.ToString());
            }
            else
            {
                var outterpath = GetPrecompileFilePath(outtertype);
                var outterlines = ReadRawLines(outterpath);
                int istart, iend;
                FindTag(outterlines, "NESTED_TYPE_HUB", out istart, out iend);
                if (iend > istart && istart >= 0)
                {
                    var sb_iline = new System.Text.StringBuilder();
                    sb_iline.Append("public static TypeHubPrecompiled_");
                    sb_iline.Append(filename);
                    sb_iline.Append(" ___tp_");
                    sb_iline.Append(filename);
                    sb_iline.Append(" = new TypeHubPrecompiled_");
                    sb_iline.Append(filename);
                    sb_iline.Append("();");

                    outterlines.Insert(iend, sb_iline.ToString());
                    WriteLines(outterpath, outterlines);
                }
            }
#else
            //if (outtertype == null)
            {
                sbline.Remove(0, sbline.Length);
                sbline.Append("private static LuaLib.LuaTypeHub.TypeHubCreator<TypeHubPrecompiled_");
                sbline.Append(filename);
                sbline.Append("> ___tp_");
                sbline.Append(filename);
                sbline.Append(" = new LuaLib.LuaTypeHub.TypeHubCreator<TypeHubPrecompiled_");
                sbline.Append(filename);
                sbline.Append(">(typeof(");
                sbline.Append(sb_type);
                sbline.Append("));");
                lines.Add(sbline.ToString());
            }
            //else
            //{
            //    var outterpath = GetPrecompileFilePath(outtertype);
            //    var outterlines = ReadRawLines(outterpath);
            //    int istart, iend;
            //    FindTag(outterlines, "NESTED_TYPE_HUB", out istart, out iend);
            //    if (iend > istart && istart >= 0)
            //    {
            //        var sb_iline = new System.Text.StringBuilder();
            //        sb_iline.Append("public static LuaLib.LuaTypeHub.TypeHubCreator<TypeHubPrecompiled_");
            //        sb_iline.Append(filename);
            //        sb_iline.Append("> ___tp_");
            //        sb_iline.Append(filename);
            //        sb_iline.Append(" = new LuaLib.LuaTypeHub.TypeHubCreator<TypeHubPrecompiled_");
            //        sb_iline.Append(filename);
            //        sb_iline.Append(">(typeof(");
            //        sb_iline.Append(sb_type);
            //        sb_iline.Append("));");

            //        outterlines.Insert(iend, sb_iline.ToString());
            //        WriteLines(outterpath, outterlines);
            //    }
            //}
#endif
            // write instance END
            if (!IsNativeType(type) && !(type.IsAbstract && type.IsSealed))
            {
                // write get set
                string instance = "___tp_" + filename + ".TypeHubSub";
#if LUA_PRECOMPILE_INSTANT_TYPE_HUB_CREATION
                if (outtertype != null)
                {
                    instance = "TypeHubPrecompiled_" + GetFileNameForType(outtertype.ToString()) + "." + instance;
                }
#else
                //if (outtertype != null)
                //{
                //    instance = "TypeHubPrecompiled_" + GetFileNameForType(outtertype.ToString()) + "." + instance;
                //}
#endif
                sbline.Remove(0, sbline.Length);
                sbline.Append("public static void PushLua(this IntPtr l, ");
                sbline.Append(sb_type);
                sbline.Append(" val)");
                lines.Add(sbline.ToString());
                lines.Add("{");
                if (!type.IsValueType && !type.IsSealed)
                {
                    lines.Add("if (object.ReferenceEquals(val, null))");
                    lines.Add("{");
                    lines.Add("l.pushnil();");
                    lines.Add("}");
                    lines.Add("else");
                    lines.Add("{");
                    lines.Add("l.PushLuaObject(val);");
                    lines.Add("}");
                }
                else
                {
                    if (!type.IsValueType)
                    {
                        lines.Add("if (object.ReferenceEquals(val, null))");
                        lines.Add("{");
                        lines.Add("l.pushnil();");
                        lines.Add("}");
                        lines.Add("else");
                        lines.Add("{");
                    }
                    sbline.Remove(0, sbline.Length);
                    sbline.Append(instance);
                    if (type.IsEnum)
                    {
                        sbline.Append(".LuaHubNative");
                    }
                    sbline.Append(".PushLua");
                    if (!type.IsValueType)
                    {
                        sbline.Append("Object");
                    }
                    sbline.Append("(l, val);");
                    if (!type.IsValueType)
                    {
                        lines.Add("}");
                    }
                    lines.Add(sbline.ToString());
                }
                lines.Add("}");
                sbline.Remove(0, sbline.Length);
                sbline.Append("public static void GetLua(this IntPtr l, int index, out ");
                sbline.Append(sb_type);
                sbline.Append(" val)");
                lines.Add(sbline.ToString());
                lines.Add("{");
                sbline.Remove(0, sbline.Length);
                sbline.Append("val = ");
                if (type.IsValueType)
                {
                    sbline.Append(instance);
                    sbline.Append(".GetLuaChecked(l, index);");
                }
                else
                {
                    sbline.Append("LuaHub.GetLuaTableObjectChecked(l, index) as ");
                    sbline.Append(sb_type);
                    sbline.Append(";");
                }
                lines.Add(sbline.ToString());
                lines.Add("}");
                // write get set END
                // write get set nullable
                if (type.IsValueType)
                {
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static void PushLua(this IntPtr l, ");
                    sbline.Append(sb_type);
                    sbline.Append("? val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("if (val == null)");
                    lines.Add("l.pushnil();");
                    lines.Add("else");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append(instance);
                    if (type.IsEnum)
                    {
                        sbline.Append(".LuaHubNative");
                    }
                    sbline.Append(".PushLua");
                    sbline.Append("(l, val.Value);");
                    lines.Add(sbline.ToString());
                    lines.Add("}");

                    sbline.Remove(0, sbline.Length);
                    sbline.Append("public static void GetLua(this IntPtr l, int index, out ");
                    sbline.Append(sb_type);
                    sbline.Append("? val)");
                    lines.Add(sbline.ToString());
                    lines.Add("{");
                    lines.Add("if (l.isnoneornil(index))");
                    lines.Add("val = null;");
                    lines.Add("else");
                    sbline.Remove(0, sbline.Length);
                    sbline.Append("val = ");
                    sbline.Append(instance);
                    sbline.Append(".GetLuaChecked(l, index);");
                    lines.Add(sbline.ToString());
                    lines.Add("}");
                }
                // write get set nullable END
            }
            lines.Add("}");
            lines.Add("}");
            lines.Add("#endif");
            lines.Add("#pragma warning restore CS0162");

            WriteLines(path, lines);
        }

        public static void WritePrecompileFuncForMember(string memberName, string typeName, bool overwrite)
        {
            var typelist = GetTypeList();
            if (!typelist.ContainsKey(typeName))
                return;
            WritePrecompileFileForType(typeName);
            var type = typelist[typeName];
            if (type.IsGenericType())
                return; // TODO: generic support.
#if RUNTIME_HAS_REF_STRUCT
            if (type.IsByRefLike)
                return;
#endif
            List<MemberInfo> members = null;
            var memberlist = GetMemberList();
            if (memberlist.ContainsKey(type))
            {
                if (memberlist[type].ContainsKey(memberName))
                {
                    members = memberlist[type][memberName];
                }
            }
            if (memberName == ".ctor" && type.IsValueType)
            {
                if (members == null)
                {
                    members = new List<MemberInfo>();
                }
                members.Add(MemberInfo.DefaultCtor);
            }
            if (members == null || members.Count <= 0)
                return;

            Dictionary<MemberInfo, System.Reflection.MemberInfo> realMembers = new Dictionary<MemberInfo, System.Reflection.MemberInfo>();
            var allmembers = type.GetMembers();
            foreach (var member in allmembers)
            {
                var mstr = ReflectAnalyzer.GetIDString(member);
                //if (member is PropertyInfo && memberName == "get_" + member.Name || memberName == "set_" + member.Name)
                //{
                //    return;
                //}
                //if (member is EventInfo && memberName == "add_" + member.Name || memberName == "remove_" + member.Name)
                //{
                //    return;
                //}

#if RUNTIME_HAS_REF_STRUCT
                if (member is MethodBase)
                {
                    var mbase = member as MethodBase;
                    bool hasInvalidParam = false;
                    foreach (var ptype in mbase.GetParameters())
                    {
                        if (ptype.ParameterType.IsByRefLike)
                        {
                            hasInvalidParam = true;
                            break;
                        }
                    }
                    if (hasInvalidParam)
                    {
                        continue;
                    }
                }
#endif

                var precompileAttribute = member.GetCustomAttribute<LuaLib.LuaPrecompileAttribute>();
                if (precompileAttribute != null && precompileAttribute.Ignore)
                {
                    continue;
                }
                if (member is System.Reflection.MethodInfo)
                {
                    var minfo = member as System.Reflection.MethodInfo;
                    if (minfo.ContainsGenericParameters)
                        continue;
                }
                foreach (var info in members)
                {
                    if (mstr == info.BodyStr)
                    {
                        if (realMembers.ContainsKey(info))
                        {
                            // public new virtual void Func5() { }
                            // public new int F1;
                            var oldmember = realMembers[info];
                            if (oldmember.DeclaringType.IsAssignableFrom(member.DeclaringType))
                            {
                                realMembers[info] = member;
                            }
                            else
                            {
                                //realMembers[info] = oldmember;
                            }
                        }
                        else
                        {
                            realMembers[info] = member;
                        }
                    }
                }
            }
            if (memberName == ".ctor" && type.IsValueType)
            {
                realMembers[MemberInfo.DefaultCtor] = typeof(LuaPrecompileWriter.ConstructorWrapperOfType<>).MakeGenericType(type).GetConstructors()[0];
            }
            members.Clear();
            members.AddRange(realMembers.Keys);
            if (members.Count <= 0)
                return;

            var filename = GetFileNameForType(type.ToString());
            var path = GetPrecompileFilePath(type);
            var lines = ReadRawLines(path);
            var basetype = GetPrecompileBaseType(type);

            var member0 = members[0];
            var membertype = member0.MemberType;
            switch (membertype)
            {
                case "func":
                    {
                        if (LuaPrecompileWriter.convertOps.Contains(memberName))
                        {
                            foreach (var member in members)
                            {
                                var realmember = realMembers[member] as System.Reflection.MethodInfo;
                                var fromtype = realmember.GetParameters()[0].ParameterType;
                                var totype = realmember.ReturnType;
                                System.Text.StringBuilder sb_from = new System.Text.StringBuilder();
                                sb_from.WriteType(fromtype);
                                System.Text.StringBuilder sb_to = new System.Text.StringBuilder();
                                sb_to.WriteType(totype);
                                if (totype == type)
                                {
                                    WritePrecompileFileForType(fromtype.ToString());
                                    var fromname = GetFileNameForType(fromtype.ToString());
                                    var frompath = GetPrecompileFilePath(fromtype);
                                    var fromlines = ReadRawLines(frompath);
                                    var fromclasspre = "private class TypeHubPrecompiled_" + fromname + " :";
                                    for (int i = 0; i < fromlines.Count; ++i)
                                    {
                                        var line = fromlines[i];
                                        if (line.StartsWith(fromclasspre))
                                        {
                                            if (!line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Contains("ILuaConvert"))
                                            {
                                                fromlines[i] = line + ", ILuaConvert";
                                                WriteLines(frompath, fromlines);
                                            }
                                            break;
                                        }
                                    }
                                    // remove old
                                    int convtags, convtage, sblock, eblock;
                                    FindTag(fromlines, "REG_S_CONV", out convtags, out convtage);
                                    sblock = -1; eblock = -1;
                                    var blockpre = "_ConvertFuncs[typeof(" + sb_to + ")] =";
                                    for (int i = convtags + 1; i < convtage; ++i)
                                    {
                                        var line = fromlines[i];
                                        if (line.StartsWith(blockpre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        if (!overwrite) return;
                                        fromlines.RemoveAt(sblock);
                                    }
                                    FindTag(fromlines, "FUNC_S_CONV", out convtags, out convtage);
                                    sblock = -1; eblock = -1;
                                    blockpre = "private static int ___convm_" + filename + "(IntPtr l, int index)";
                                    for (int i = convtags + 1; i < convtage; ++i)
                                    {
                                        var line = fromlines[i];
                                        if (line.StartsWith(blockpre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        eblock = EncloseBlock(fromlines, sblock + 1);
                                        fromlines.RemoveRange(sblock, eblock - sblock + 1);
                                    }
                                    // write new
                                    FindTag(fromlines, "REG_S_CONV", out convtags, out convtage);
                                    fromlines.Insert(convtage, "_ConvertFuncs[typeof(" + sb_to + ")] = ___convm_" + filename + ";");
                                    FindTag(fromlines, "FUNC_S_CONV", out convtags, out convtage);
                                    fromlines.Insert(convtage++, "private static int ___convm_" + filename + "(IntPtr l, int index)");
                                    fromlines.Insert(convtage++, "{");
                                    fromlines.Insert(convtage++, "try");
                                    fromlines.Insert(convtage++, "{");
                                    fromlines.Insert(convtage++, sb_from + " p0;");
                                    fromlines.Insert(convtage++, "l.GetLua(index, out p0);");
                                    fromlines.Insert(convtage++, "l.PushLuaExplicit<" + sb_to + ">((" + sb_to + ")p0);");
                                    fromlines.Insert(convtage++, "return 1;");
                                    fromlines.Insert(convtage++, "}");
                                    fromlines.Insert(convtage++, "catch (Exception exception)");
                                    fromlines.Insert(convtage++, "{");
                                    fromlines.Insert(convtage++, "l.LogError(exception);");
                                    fromlines.Insert(convtage++, "return 0;");
                                    fromlines.Insert(convtage++, "}");
                                    fromlines.Insert(convtage++, "}");
                                    WriteLines(frompath, fromlines);
                                }
                                else // fromtype == type
                                {
                                    var toname = GetFileNameForType(totype.ToString());
                                    var fromclasspre = "private class TypeHubPrecompiled_" + filename + " :";
                                    for (int i = 0; i < lines.Count; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(fromclasspre))
                                        {
                                            if (!line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Contains("ILuaConvert"))
                                            {
                                                lines[i] = line + ", ILuaConvert";
                                                WriteLines(path, lines);
                                            }
                                            break;
                                        }
                                    }
                                    // remove old
                                    int convtags, convtage, sblock, eblock;
                                    FindTag(lines, "REG_S_CONV", out convtags, out convtage);
                                    sblock = -1; eblock = -1;
                                    var blockpre = "_ConvertFuncs[typeof(" + sb_to + ")] =";
                                    for (int i = convtags + 1; i < convtage; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(blockpre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        if (!overwrite) return;
                                        lines.RemoveAt(sblock);
                                    }
                                    FindTag(lines, "FUNC_S_CONV", out convtags, out convtage);
                                    sblock = -1; eblock = -1;
                                    blockpre = "private static int ___convm_" + toname + "(IntPtr l, int index)";
                                    for (int i = convtags + 1; i < convtage; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(blockpre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        eblock = EncloseBlock(lines, sblock + 1);
                                        lines.RemoveRange(sblock, eblock - sblock + 1);
                                    }
                                    // write new
                                    FindTag(lines, "REG_S_CONV", out convtags, out convtage);
                                    lines.Insert(convtage, "_ConvertFuncs[typeof(" + sb_to + ")] = ___convm_" + toname + ";");
                                    FindTag(lines, "FUNC_S_CONV", out convtags, out convtage);
                                    lines.Insert(convtage++, "private static int ___convm_" + toname + "(IntPtr l, int index)");
                                    lines.Insert(convtage++, "{");
                                    lines.Insert(convtage++, "try");
                                    lines.Insert(convtage++, "{");
                                    lines.Insert(convtage++, sb_from + " p0;");
                                    lines.Insert(convtage++, "l.GetLua(index, out p0);");
                                    lines.Insert(convtage++, "l.PushLuaExplicit<" + sb_to + ">((" + sb_to + ")p0);");
                                    lines.Insert(convtage++, "return 1;");
                                    lines.Insert(convtage++, "}");
                                    lines.Insert(convtage++, "catch (Exception exception)");
                                    lines.Insert(convtage++, "{");
                                    lines.Insert(convtage++, "l.LogError(exception);");
                                    lines.Insert(convtage++, "return 0;");
                                    lines.Insert(convtage++, "}");
                                    lines.Insert(convtage++, "}");
                                    WriteLines(path, lines);
                                }
                            }
                        }
                        else
                        {
                            List<MemberInfo> smethods = new List<MemberInfo>();
                            List<MemberInfo> imethods = new List<MemberInfo>();
                            foreach (var method in members)
                            {
                                if (method.TargetType == "static")
                                {
                                    smethods.Add(method);
                                }
                                else
                                {
                                    imethods.Add(method);
                                }
                            }

                            // static
                            if (smethods.Count > 0)
                            {
                                do
                                {
                                    // 1) remove old lines
                                    int sreg, ereg, sdel, edel, sfunc, efunc;
                                    FindTag(lines, "REG_S_FUNC", out sreg, out ereg);
                                    string pre = "_StaticMethods[\"" + memberName + "\"]";
                                    int sblock = -1, eblock = -1;
                                    for (int i = sreg + 1; i < ereg; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        if (!overwrite) break;
                                        lines.RemoveAt(sblock);
                                    }

                                    FindTag(lines, "DEL_S_FUNC", out sdel, out edel);
                                    pre = "private static readonly lua.CFunction ___sfm_" + memberName + " =";
                                    sblock = -1; eblock = -1;
                                    for (int i = sdel + 1; i < edel; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        lines.RemoveAt(sblock);
                                    }

                                    FindTag(lines, "FUNC_S_FUNC", out sfunc, out efunc);
                                    pre = "private static int ___smm_" + memberName + "(IntPtr l)";
                                    sblock = -1; eblock = -1;
                                    for (int i = sfunc + 1; i < efunc; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        eblock = EncloseBlock(lines, sblock + 1);
                                        lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                                    }

                                    // 2) insert new lines
                                    FindTag(lines, "REG_S_FUNC", out sreg, out ereg);
                                    lines.Insert(ereg, "_StaticMethods[\"" + memberName + "\"] = new LuaMetaCallWithPrecompiled() { _Method = _StaticMethods[\"" + memberName + "\"]._Method, _Precompiled = ___sfm_" + memberName + " };");

                                    FindTag(lines, "DEL_S_FUNC", out sdel, out edel);
                                    lines.Insert(edel, "private static readonly lua.CFunction ___sfm_" + memberName + " = new lua.CFunction(___smm_" + memberName + ");");

                                    FindTag(lines, "FUNC_S_FUNC", out sfunc, out efunc);
                                    lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                    lines.Insert(efunc++, "private static int ___smm_" + memberName + "(IntPtr l)");
                                    lines.Insert(efunc++, "{");
                                    var context = new LuaPrecompileWriter.WriteMethodBodyContext();
                                    context.Methods = smethods.Select(info => (MethodBase)realMembers[info]).ToArray();
                                    LuaPrecompileWriter.WriteMethodBody(context);
                                    lines.Insert(efunc++, context.sb.ToString());
                                    lines.Insert(efunc++, "}");

                                    if (OpMap.ContainsKey(memberName))
                                    {
                                        // this is an operator
                                        var opmetaname = OpMap[memberName];
                                        FindTag(lines, "REG_S_OP", out sreg, out ereg);
                                        pre = "_Ops[\"" + opmetaname + "\"] =";
                                        sblock = -1; eblock = -1;
                                        for (int i = sreg + 1; i < ereg; ++i)
                                        {
                                            var line = lines[i];
                                            if (line.StartsWith(pre))
                                            {
                                                sblock = i;
                                                break;
                                            }
                                        }
                                        if (sblock >= 0)
                                        {
                                            lines.RemoveAt(sblock);
                                        }

                                        FindTag(lines, "DEL_S_OP", out sdel, out edel);
                                        pre = "private static readonly lua.CFunction ___opf" + opmetaname + " =";
                                        sblock = -1; eblock = -1;
                                        for (int i = sdel + 1; i < edel; ++i)
                                        {
                                            var line = lines[i];
                                            if (line.StartsWith(pre))
                                            {
                                                sblock = i;
                                                break;
                                            }
                                        }
                                        if (sblock >= 0)
                                        {
                                            lines.RemoveAt(sblock);
                                        }

                                        FindTag(lines, "FUNC_S_OP", out sfunc, out efunc);
                                        pre = "private static int ___opm" + opmetaname + "(IntPtr l)";
                                        sblock = -1; eblock = -1;
                                        for (int i = sfunc + 1; i < efunc; ++i)
                                        {
                                            var line = lines[i];
                                            if (line.StartsWith(pre))
                                            {
                                                sblock = i;
                                                break;
                                            }
                                        }
                                        if (sblock >= 0)
                                        {
                                            eblock = EncloseBlock(lines, sblock + 1);
                                            lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                                        }

                                        FindTag(lines, "REG_S_OP", out sreg, out ereg);
                                        lines.Insert(ereg, "_Ops[\"" + opmetaname + "\"] = new LuaMetaCallWithPrecompiled() { _Method = _Ops[\"" + opmetaname + "\"]._Method, _Precompiled = ___opf" + opmetaname + " };");

                                        FindTag(lines, "DEL_S_OP", out sdel, out edel);
                                        lines.Insert(edel, "private static readonly lua.CFunction ___opf" + opmetaname + " = new lua.CFunction(___opm" + opmetaname + ");");

                                        FindTag(lines, "FUNC_S_OP", out sfunc, out efunc);
                                        lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                        lines.Insert(efunc++, "private static int ___opm" + opmetaname + "(IntPtr l)");
                                        lines.Insert(efunc++, "{");
                                        lines.Insert(efunc++, "var rv = ___smm_" + memberName + "(l);");
                                        lines.Insert(efunc++, "if (rv == 0)");
                                        lines.Insert(efunc++, "{");
                                        lines.Insert(efunc++, "l.pushnil();");
                                        lines.Insert(efunc++, "l.pushboolean(true);");
                                        lines.Insert(efunc++, "return 2;");
                                        lines.Insert(efunc++, "}");
                                        lines.Insert(efunc++, "return rv;");
                                        lines.Insert(efunc++, "}");
                                    }

                                    WriteLines(path, TrimRawLines(lines));
                                } while (false);
                            }
                            // instance
                            if (imethods.Count > 0)
                            {
                                if (!type.IsValueType)
                                {
                                    List<MemberInfo> inheritedMembers = new List<MemberInfo>(imethods.Count);
                                    foreach (var imethod in imethods)
                                    {
                                        var method = realMembers[imethod] as MethodInfo;
                                        var dtype = method.DeclaringType;
                                        var rtype = method.ReflectedType;
                                        if (dtype != rtype)
                                        {
                                            inheritedMembers.Add(imethod);
                                        }
                                        else if (IsOverride(method))
                                        {
                                            inheritedMembers.Add(imethod);
                                        }
                                    }
                                    if (inheritedMembers.Count == imethods.Count)
                                    {
                                        int stag, etag;
                                        FindTag(lines, "REG_I_FUNC", out stag, out etag);
                                        if (stag > 0)
                                        {
                                            etag = -1;
                                            for (; stag < lines.Count; ++stag)
                                            {
                                                if (lines[stag] == "}")
                                                {
                                                    etag = stag;
                                                    break;
                                                }
                                            }
                                            if (etag > 0)
                                            {
                                                for (stag = etag; stag > 0; --stag)
                                                {
                                                    if (lines[stag] == "{")
                                                    {
                                                        break;
                                                    }
                                                }
                                                if (stag > 0)
                                                {
                                                    for (; stag < etag; ++stag)
                                                    {
                                                        if (lines[stag] == "#if UNITY_EDITOR")
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    if (stag < etag)
                                                    {
                                                        for (; etag > stag; --etag)
                                                        {
                                                            if (lines[etag] == "#endif")
                                                            {
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    if (stag >= etag)
                                                    {
                                                        lines.Insert(etag, "#endif");
                                                        lines.Insert(etag, "#if UNITY_EDITOR");
                                                        stag = etag++;
                                                    }
                                                    var dline = "_InstanceMethods_DirectFromBase.Add(\"" + memberName + "\");";
                                                    bool dfound = false;
                                                    for (int i = stag + 1; i < etag; ++i)
                                                    {
                                                        if (lines[i] == dline)
                                                        {
                                                            dfound = true;
                                                            break;
                                                        }
                                                    }
                                                    if (!dfound)
                                                    {
                                                        lines.Insert(etag, dline);
                                                        WriteLines(path, TrimRawLines(lines));
                                                    }
                                                }
                                            }
                                        }

                                        if (basetype != null)
                                        {
                                            WritePrecompileFuncForMember(memberName, basetype.ToString(), false);
                                        }
                                        return;
                                    }
                                }
                                do
                                {
                                    // 1) remove old lines
                                    int sreg, ereg, sdel, edel, sfunc, efunc;
                                    FindTag(lines, "REG_I_FUNC", out sreg, out ereg);
                                    string pre = "_InstanceMethods[\"" + memberName + "\"]";
                                    int sblock = -1, eblock = -1;
                                    for (int i = sreg + 1; i < ereg; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        if (!overwrite) break;
                                        lines.RemoveAt(sblock);
                                    }

                                    FindTag(lines, "DEL_I_FUNC", out sdel, out edel);
                                    pre = "private static readonly lua.CFunction ___fm_" + memberName + " =";
                                    sblock = -1; eblock = -1;
                                    for (int i = sdel + 1; i < edel; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        lines.RemoveAt(sblock);
                                    }

                                    FindTag(lines, "FUNC_I_FUNC", out sfunc, out efunc);
                                    pre = "private static int ___mm_" + memberName + "(IntPtr l)";
                                    sblock = -1; eblock = -1;
                                    for (int i = sfunc + 1; i < efunc; ++i)
                                    {
                                        var line = lines[i];
                                        if (line.StartsWith(pre))
                                        {
                                            sblock = i;
                                            break;
                                        }
                                    }
                                    if (sblock >= 0)
                                    {
                                        eblock = EncloseBlock(lines, sblock + 1);
                                        lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                                    }

                                    // 2) insert new lines
                                    FindTag(lines, "REG_I_FUNC", out sreg, out ereg);
                                    lines.Insert(ereg, "_InstanceMethods[\"" + memberName + "\"] = new LuaMetaCallWithPrecompiled() { _Method = _InstanceMethods[\"" + memberName + "\"]._Method, _Precompiled = ___fm_" + memberName + " };");

                                    FindTag(lines, "DEL_I_FUNC", out sdel, out edel);
                                    lines.Insert(edel, "private static readonly lua.CFunction ___fm_" + memberName + " = new lua.CFunction(___mm_" + memberName + ");");

                                    FindTag(lines, "FUNC_I_FUNC", out sfunc, out efunc);
                                    lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                    lines.Insert(efunc++, "private static int ___mm_" + memberName + "(IntPtr l)");
                                    lines.Insert(efunc++, "{");
                                    var context = new LuaPrecompileWriter.WriteMethodBodyContext();
                                    context.Methods = imethods.Select(info => (MethodBase)realMembers[info]).ToArray();
                                    LuaPrecompileWriter.WriteMethodBody(context);
                                    lines.Insert(efunc++, context.sb.ToString());
                                    lines.Insert(efunc++, "}");
                                    WriteLines(path, TrimRawLines(lines));
                                } while (false);
                            }
                        }
                    }
                    break;
                case "ctor":
                    {
                        // 1) remove old lines
                        int sreg, ereg, sdel, edel, sfunc, efunc;
                        FindTag(lines, "REG_I_CTOR", out sreg, out ereg);
                        string pre = "_Ctor._Precompiled = ___fm_ctor;";
                        int sblock = -1, eblock = -1;
                        for (int i = sreg + 1; i < ereg; ++i)
                        {
                            var line = lines[i];
                            if (line.StartsWith(pre))
                            {
                                sblock = i;
                                break;
                            }
                        }
                        if (sblock >= 0)
                        {
                            if (!overwrite) return;
                            lines.RemoveAt(sblock);
                        }

                        FindTag(lines, "DEL_I_CTOR", out sdel, out edel);
                        pre = "private static readonly lua.CFunction ___fm_ctor = new lua.CFunction(___mm_ctor);";
                        sblock = -1; eblock = -1;
                        for (int i = sdel + 1; i < edel; ++i)
                        {
                            var line = lines[i];
                            if (line.StartsWith(pre))
                            {
                                sblock = i;
                                break;
                            }
                        }
                        if (sblock >= 0)
                        {
                            lines.RemoveAt(sblock);
                        }

                        FindTag(lines, "FUNC_I_CTOR", out sfunc, out efunc);
                        pre = "private static int ___mm_ctor(IntPtr l)";
                        sblock = -1; eblock = -1;
                        for (int i = sfunc + 1; i < efunc; ++i)
                        {
                            var line = lines[i];
                            if (line.StartsWith(pre))
                            {
                                sblock = i;
                                break;
                            }
                        }
                        if (sblock >= 0)
                        {
                            eblock = EncloseBlock(lines, sblock + 1);
                            lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                        }

                        // 2) insert new lines
                        FindTag(lines, "REG_I_CTOR", out sreg, out ereg);
                        lines.Insert(ereg, "_Ctor._Precompiled = ___fm_ctor;");

                        FindTag(lines, "DEL_I_CTOR", out sdel, out edel);
                        lines.Insert(edel, "private static readonly lua.CFunction ___fm_ctor = new lua.CFunction(___mm_ctor);");

                        FindTag(lines, "FUNC_I_CTOR", out sfunc, out efunc);
                        lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                        lines.Insert(efunc++, "private static int ___mm_ctor(IntPtr l)");
                        lines.Insert(efunc++, "{");
                        var context = new LuaPrecompileWriter.WriteMethodBodyContext();
                        context.Methods = realMembers.Select(kvp => (MethodBase)kvp.Value).ToArray();
                        LuaPrecompileWriter.WriteMethodBody(context);
                        lines.Insert(efunc++, context.sb.ToString());
                        lines.Insert(efunc++, "}");
                        WriteLines(path, TrimRawLines(lines));
                    }
                    break;
                case "field":
                case "prop":
                    {
                        var realmember = realMembers[member0];
                        Type retype;
                        bool hasget, hasset;
                        if (membertype == "field")
                        {
                            var minfo = realmember as FieldInfo;
                            if (!type.IsValueType)
                            {
                                var dtype = minfo.DeclaringType;
                                var rtype = minfo.ReflectedType;
                                if (dtype != rtype)
                                {
                                    if (basetype != null)
                                    {
                                        WritePrecompileFuncForMember(memberName, basetype.ToString(), false);
                                        return;
                                    }
                                }
                            }
                            retype = minfo.FieldType;
                            hasget = true;
                            hasset = !minfo.IsInitOnly && !minfo.IsLiteral;
                        }
                        else //if (membertype == "prop")
                        {
                            var minfo = realmember as PropertyInfo;
                            if (!type.IsValueType)
                            {
                                var dtype = minfo.DeclaringType;
                                var rtype = minfo.ReflectedType;
                                if (dtype != rtype || minfo.GetGetMethod() != null && IsOverride(minfo.GetGetMethod()) || minfo.GetSetMethod() != null && IsOverride(minfo.GetSetMethod()))
                                {
                                    if (basetype != null)
                                    {
                                        WritePrecompileFuncForMember(memberName, basetype.ToString(), false);
                                        return;
                                    }
                                }
                            }
                            if (minfo.GetIndexParameters().Length > 0)
                            {
                                // is it the indexer?
                                var attrs = type.GetCustomAttributes(typeof(DefaultMemberAttribute), false);
                                if (attrs != null && attrs.Length > 0)
                                {
                                    var attr = attrs[0] as DefaultMemberAttribute;
                                    if (attr != null)
                                    {
                                        if (attr.MemberName == minfo.Name)
                                        {
                                            // this is the indexer.
                                            {
                                                List<MethodBase> getters = new List<MethodBase>();
                                                List<MethodBase> setters = new List<MethodBase>();
                                                foreach (var kvprm in realMembers)
                                                {
                                                    if (kvprm.Key.TargetType == "instance" && kvprm.Value is PropertyInfo)
                                                    {
                                                        var pinfo = kvprm.Value as PropertyInfo;
                                                        var getm = pinfo.GetGetMethod();
                                                        if (getm != null && getm.IsPublic)
                                                        {
                                                            getters.Add(getm);
                                                        }
                                                        var setm = pinfo.GetSetMethod();
                                                        if (setm != null && setm.IsPublic)
                                                        {
                                                            setters.Add(setm);
                                                        }
                                                    }
                                                }
                                                {
                                                    // remove old
                                                    int sreg, ereg, sdel, edel, sfunc, efunc;
                                                    FindTag(lines, "REG_I_INDEX", out sreg, out ereg);
                                                    string pre = "_IndexAccessor[\"get\"]";
                                                    int sblock = -1, eblock = -1;
                                                    for (int i = sreg + 1; i < ereg; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        if (!overwrite) return;
                                                        lines.RemoveAt(sblock);
                                                    }
                                                    FindTag(lines, "REG_I_INDEX", out sreg, out ereg);
                                                    pre = "_IndexAccessor[\"set\"]";
                                                    sblock = -1; eblock = -1;
                                                    for (int i = sreg + 1; i < ereg; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        if (!overwrite) return;
                                                        lines.RemoveAt(sblock);
                                                    }

                                                    FindTag(lines, "DEL_I_INDEX", out sdel, out edel);
                                                    pre = "private static readonly lua.CFunction ___gfi_Index =";
                                                    sblock = -1; eblock = -1;
                                                    for (int i = sdel + 1; i < edel; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        lines.RemoveAt(sblock);
                                                    }
                                                    FindTag(lines, "DEL_I_INDEX", out sdel, out edel);
                                                    pre = "private static readonly lua.CFunction ___sfi_Index =";
                                                    sblock = -1; eblock = -1;
                                                    for (int i = sdel + 1; i < edel; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        lines.RemoveAt(sblock);
                                                    }

                                                    FindTag(lines, "FUNC_I_INDEX", out sfunc, out efunc);
                                                    pre = "private static int ___gmi_Index(IntPtr l)";
                                                    sblock = -1; eblock = -1;
                                                    for (int i = sfunc + 1; i < efunc; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        eblock = EncloseBlock(lines, sblock + 1);
                                                        lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                                                    }
                                                    FindTag(lines, "FUNC_I_INDEX", out sfunc, out efunc);
                                                    pre = "private static int ___smi_Index(IntPtr l)";
                                                    sblock = -1; eblock = -1;
                                                    for (int i = sfunc + 1; i < efunc; ++i)
                                                    {
                                                        var line = lines[i];
                                                        if (line.StartsWith(pre))
                                                        {
                                                            sblock = i;
                                                            break;
                                                        }
                                                    }
                                                    if (sblock >= 0)
                                                    {
                                                        eblock = EncloseBlock(lines, sblock + 1);
                                                        lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                                                    }

                                                    // write new lines
                                                    FindTag(lines, "REG_I_INDEX", out sreg, out ereg);
                                                    if (getters.Count > 0)
                                                        lines.Insert(ereg++, "_IndexAccessor[\"get\"] = new LuaMetaCallWithPrecompiled() { _Precompiled = ___gfi_Index };");
                                                    if (setters.Count > 0)
                                                        lines.Insert(ereg++, "_IndexAccessor[\"set\"] = new LuaMetaCallWithPrecompiled() { _Precompiled = ___sfi_Index };");

                                                    FindTag(lines, "DEL_I_INDEX", out sdel, out edel);
                                                    if (getters.Count > 0)
                                                        lines.Insert(edel++, "private static readonly lua.CFunction ___gfi_Index = new lua.CFunction(___gmi_Index);");
                                                    if (setters.Count > 0)
                                                        lines.Insert(edel++, "private static readonly lua.CFunction ___sfi_Index = new lua.CFunction(___smi_Index);");

                                                    FindTag(lines, "FUNC_I_INDEX", out sfunc, out efunc);
                                                    if (getters.Count > 0)
                                                    {
                                                        lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                                        lines.Insert(efunc++, "private static int ___gmi_Index(IntPtr l)");
                                                        lines.Insert(efunc++, "{");
                                                        var context = new LuaPrecompileWriter.WriteMethodBodyContext();
                                                        context.Methods = getters;
                                                        LuaPrecompileWriter.WriteMethodBody(context);
                                                        lines.Insert(efunc++, context.sb.ToString());
                                                        lines.Insert(efunc++, "}");
                                                    }
                                                    if (setters.Count > 0)
                                                    {
                                                        lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                                        lines.Insert(efunc++, "private static int ___smi_Index(IntPtr l)");
                                                        lines.Insert(efunc++, "{");
                                                        var context = new LuaPrecompileWriter.WriteMethodBodyContext();
                                                        context.Methods = setters;
                                                        LuaPrecompileWriter.WriteMethodBody(context);
                                                        lines.Insert(efunc++, context.sb.ToString());
                                                        lines.Insert(efunc++, "}");
                                                    }
                                                }
                                                WriteLines(path, TrimRawLines(lines));
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                            retype = minfo.PropertyType;
                            hasget = minfo.GetGetMethod() != null && minfo.GetGetMethod().IsPublic;
                            hasset = minfo.GetSetMethod() != null && minfo.GetSetMethod().IsPublic;
                        }
                        {
                            string tagreg, tagdel, tagfunc;
                            string getdict, setdict, prename;
                            if (member0.TargetType == "static")
                            {
                                tagreg = "REG_S_PROP";
                                tagdel = "DEL_S_PROP";
                                tagfunc = "FUNC_S_PROP";
                                getdict = "_StaticFieldsIndex";
                                setdict = "_StaticFieldsNewIndex";
                                prename = "s";
                            }
                            else
                            {
                                tagreg = "REG_I_PROP";
                                tagdel = "DEL_I_PROP";
                                tagfunc = "FUNC_I_PROP";
                                getdict = "_InstanceFieldsIndex";
                                setdict = "_InstanceFieldsNewIndex";
                                prename = "";
                            }
                            // 1) remove old lines
                            int sreg, ereg, sdel, edel, sfunc, efunc;
                            FindTag(lines, tagreg, out sreg, out ereg);
                            string pre = getdict + "[\"" + memberName + "\"]";
                            int sblock = -1, eblock = -1;
                            for (int i = sreg + 1; i < ereg; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                if (!overwrite) return;
                                lines.RemoveAt(sblock);
                            }
                            FindTag(lines, tagreg, out sreg, out ereg);
                            pre = setdict + "[\"" + memberName + "\"]";
                            sblock = -1; eblock = -1;
                            for (int i = sreg + 1; i < ereg; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                if (!overwrite) return;
                                lines.RemoveAt(sblock);
                            }

                            FindTag(lines, tagdel, out sdel, out edel);
                            pre = "private static readonly lua.CFunction ___" + prename + "gf_" + memberName + " =";
                            sblock = -1; eblock = -1;
                            for (int i = sdel + 1; i < edel; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                lines.RemoveAt(sblock);
                            }
                            FindTag(lines, tagdel, out sdel, out edel);
                            pre = "private static readonly lua.CFunction ___" + prename + "sf_" + memberName + " =";
                            sblock = -1; eblock = -1;
                            for (int i = sdel + 1; i < edel; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                lines.RemoveAt(sblock);
                            }

                            FindTag(lines, tagfunc, out sfunc, out efunc);
                            pre = "private static int ___" + prename + "gm_" + memberName + "(IntPtr l)";
                            sblock = -1; eblock = -1;
                            for (int i = sfunc + 1; i < efunc; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                eblock = EncloseBlock(lines, sblock + 1);
                                lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                            }
                            FindTag(lines, tagfunc, out sfunc, out efunc);
                            pre = "private static int ___" + prename + "sm_" + memberName + "(IntPtr l)";
                            sblock = -1; eblock = -1;
                            for (int i = sfunc + 1; i < efunc; ++i)
                            {
                                var line = lines[i];
                                if (line.StartsWith(pre))
                                {
                                    sblock = i;
                                    break;
                                }
                            }
                            if (sblock >= 0)
                            {
                                eblock = EncloseBlock(lines, sblock + 1);
                                lines.RemoveRange(sblock - 1, eblock - sblock + 2);
                            }

                            // 2) insert new lines
                            FindTag(lines, tagreg, out sreg, out ereg);
                            if (hasget)
                                lines.Insert(ereg++, getdict + "[\"" + memberName + "\"] = new LuaMetaCallWithPrecompiled() { _Method = " + getdict + "[\"" + memberName + "\"]._Method, _Precompiled = ___" + prename + "gf_" + memberName + " };");
                            if (hasset)
                                lines.Insert(ereg++, setdict + "[\"" + memberName + "\"] = new LuaMetaCallWithPrecompiled() { _Method = " + setdict + "[\"" + memberName + "\"]._Method, _Precompiled = ___" + prename + "sf_" + memberName + " };");

                            FindTag(lines, tagdel, out sdel, out edel);
                            if (hasget)
                                lines.Insert(edel++, "private static readonly lua.CFunction ___" + prename + "gf_" + memberName + " = new lua.CFunction(___" + prename + "gm_" + memberName + ");");
                            if (hasset)
                                lines.Insert(edel++, "private static readonly lua.CFunction ___" + prename + "sf_" + memberName + " = new lua.CFunction(___" + prename + "sm_" + memberName + ");");

                            FindTag(lines, tagfunc, out sfunc, out efunc);
                            if (hasget)
                            {
                                lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                lines.Insert(efunc++, "private static int ___" + prename + "gm_" + memberName + "(IntPtr l)");
                                lines.Insert(efunc++, "{");
                                lines.Insert(efunc++, "try");
                                lines.Insert(efunc++, "{");
                                if (member0.TargetType == "static")
                                {
                                    var sb_type = new System.Text.StringBuilder();
                                    sb_type.WriteType(type);
                                    lines.Insert(efunc++, "var rv = " + sb_type + "." + memberName + ";");
                                }
                                else
                                {
                                    var sb_type = new System.Text.StringBuilder();
                                    sb_type.WriteType(type);
                                    lines.Insert(efunc++, sb_type + " tar;");
                                    lines.Insert(efunc++, "l.GetLua(1, out tar);");
                                    lines.Insert(efunc++, "var rv = tar." + memberName + ";");
                                }
                                lines.Insert(efunc++, "l.PushLua(rv);");
                                lines.Insert(efunc++, "return 1;");
                                lines.Insert(efunc++, "}");
                                lines.Insert(efunc++, "catch (Exception exception)");
                                lines.Insert(efunc++, "{");
                                lines.Insert(efunc++, "l.LogError(exception);");
                                lines.Insert(efunc++, "return 0;");
                                lines.Insert(efunc++, "}");
                                lines.Insert(efunc++, "}");
                            }
                            if (hasset)
                            {
                                lines.Insert(efunc++, "[AOT.MonoPInvokeCallback(typeof(lua.CFunction))]");
                                lines.Insert(efunc++, "private static int ___" + prename + "sm_" + memberName + "(IntPtr l)");
                                lines.Insert(efunc++, "{");
                                lines.Insert(efunc++, "try");
                                lines.Insert(efunc++, "{");
                                if (member0.TargetType == "static")
                                {
                                    var sb_type = new System.Text.StringBuilder();
                                    sb_type.WriteType(type);
                                    var sb_rtype = new System.Text.StringBuilder();
                                    sb_rtype.WriteType(retype);
                                    lines.Insert(efunc++, sb_rtype + " val;");
                                    lines.Insert(efunc++, "l.GetLua(1, out val);");
                                    lines.Insert(efunc++, sb_type + "." + memberName + " = val;");
                                }
                                else
                                {
                                    var sb_type = new System.Text.StringBuilder();
                                    sb_type.WriteType(type);
                                    var sb_rtype = new System.Text.StringBuilder();
                                    sb_rtype.WriteType(retype);
                                    lines.Insert(efunc++, sb_type + " tar;");
                                    lines.Insert(efunc++, "l.GetLua(1, out tar);");
                                    lines.Insert(efunc++, sb_rtype + " val;");
                                    lines.Insert(efunc++, "l.GetLua(2, out val);");
                                    lines.Insert(efunc++, "tar." + memberName + " = val;");
                                    if (type.IsValueType)
                                    {
                                        lines.Insert(efunc++, "SetDataRaw(l, 1, tar);");
                                    }
                                }
                                lines.Insert(efunc++, "return 0;");
                                lines.Insert(efunc++, "}");
                                lines.Insert(efunc++, "catch (Exception exception)");
                                lines.Insert(efunc++, "{");
                                lines.Insert(efunc++, "l.LogError(exception);");
                                lines.Insert(efunc++, "return 0;");
                                lines.Insert(efunc++, "}");
                                lines.Insert(efunc++, "}");
                            }
                        }
                        WriteLines(path, TrimRawLines(lines));
                    }
                    break;
                default:
                    break;
            }
        }

        public static void WritePrecompileFuncForMember(string memberstr, bool overwrite)
        {
            var minfo = ParseMemberInfo(memberstr);
            if (minfo == null)
                return;
            if (minfo.Name.Contains('`') || minfo.Name.Contains('<') || minfo.Name.Contains('['))
                return; // this is generic

            string memberName, typeName;
            memberName = minfo.Name;
            typeName = minfo.OwnerType;

            if (memberName == "*")
            {
                var typelist = GetTypeList();
                if (!typelist.ContainsKey(typeName))
                    return;
                WritePrecompileFileForType(typeName, overwrite);
                var type = typelist[typeName];
                List<string> members = new List<string>();
                HashSet<string> memberSet = new HashSet<string>();
                var memberlist = GetMemberList();
                if (memberlist.ContainsKey(type))
                {
                    foreach (var member in memberlist[type].Keys)
                    {
                        if (memberSet.Add(member))
                        {
                            members.Add(member);
                        }
                    }
                }
                if (type.IsValueType)
                {
                    if (memberSet.Add(".ctor"))
                    {
                        members.Add(".ctor");
                    }
                }
                if (members.Count <= 0)
                    return;

                foreach (var member in members)
                {
                    WritePrecompileFuncForMember(member, typeName, overwrite);
                }
            }
            else
            {
                WritePrecompileFuncForMember(memberName, typeName, overwrite);

                if (minfo.MemberType == "field" || minfo.MemberType == "prop")
                {
                    if (minfo.CommentSet.Contains("plain"))
                    {
                        WritePlainFieldForValueType(typeName, minfo.Name);
                    }
                }
            }
        }

        public static void WritePlainFieldForValueType(string typestr, string field)
        {
            var typelist = GetTypeList();
            Type type;
            typelist.TryGetValue(typestr, out type);
            if (type == null || type == typeof(object))
                return;

            var path = GetPrecompileFilePath(type);
            var lines = ReadRawLines(path);
            int istart, iend;
            FindTag(lines, "VALUE_TYPE_PLAIN_FIELDS", out istart, out iend);
            if (iend > istart && istart >= 0)
            {
                for (int i = istart + 1; i < iend; ++i)
                {
                    var line = lines[i];
                    if (line.StartsWith("public static readonly LuaString LS_"))
                    {
                        var i1 = line.IndexOf("new LuaString(\"");
                        var i2 = line.LastIndexOf('"');
                        var existingfield = line.Substring(i1 + "new LuaString(\"".Length, i2 - i1 - "new LuaString(\"".Length);
                        if (existingfield == field)
                        {
                            return;
                        }
                    }
                }
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("public static readonly LuaString LS_");
            sb.Append(field.ToUpper());
            sb.Append(" = new LuaString(\"");
            sb.Append(field);
            sb.Append("\");");
            lines.Insert(iend, sb.ToString());
            WriteLines(path, lines);
        }

        private class ValueTypeKeyFieldInfo
        {
            public string field;
            public string luastring;
            public string fullfieldname;
            public Type type;
            public ValueTypeKeyFieldInfo parent;
            public List<ValueTypeKeyFieldInfo> subs;
        }
        private static List<ValueTypeKeyFieldInfo> ParseKeyFieldInfos(Type type)
        {
            return ParseKeyFieldInfos(type, null);
        }
        private static List<ValueTypeKeyFieldInfo> ParseKeyFieldInfos(Type type, ValueTypeKeyFieldInfo parent)
        {
            var path = GetPrecompileFilePath(type);
            var lines = ReadRawLines(path);

            int istart, iend;
            FindTag(lines, "VALUE_TYPE_PLAIN_FIELDS", out istart, out iend);
            if (iend <= istart || istart < 0)
            {
                istart = 0;
                iend = lines.Count;
            }

            List<ValueTypeKeyFieldInfo> rv = new List<ValueTypeKeyFieldInfo>();
            for (int i = istart; i < iend; ++i)
            {
                var line = lines[i];
                if (line.StartsWith("public static readonly LuaString LS_"))
                {
                    var i1 = line.IndexOf("new LuaString(\"");
                    var i2 = line.LastIndexOf('"');
                    var field = line.Substring(i1 + "new LuaString(\"".Length, i2 - i1 - "new LuaString(\"".Length);
                    var info = new ValueTypeKeyFieldInfo()
                    {
                        field = field,
                        luastring = "LS_" + field.ToUpper(),
                        fullfieldname = field,
                        parent = parent,
                    };
                    if (parent != null)
                    {
                        info.fullfieldname = parent.fullfieldname + "_" + field;
                    }
                    Type fieldtype = null;
                    var member = type.GetMember(field);
                    if (member != null && member.Length > 0)
                    {
                        var member0 = member[0];
                        if (member0 is FieldInfo)
                        {
                            fieldtype = ((FieldInfo)member0).FieldType;
                        }
                        else if (member0 is PropertyInfo)
                        {
                            fieldtype = ((PropertyInfo)member0).PropertyType;
                        }
                    }
                    if (fieldtype != null && fieldtype.IsValueType)
                    {
                        info.type = fieldtype;
                        if (IsNativeType(fieldtype) || fieldtype.IsEnum())
                        {
                            rv.Add(info);
                        }
                        else
                        {
                            var sub = ParseKeyFieldInfos(fieldtype, info);
                            if (sub != null && sub.Count > 0)
                            {
                                info.subs = sub;
                                rv.Add(info);
                            }
                        }
                    }
                }
            }
            return rv;
        }

        public static void WritePrecompileFuncForHubC(string typestr)
        {
            var typelist = GetTypeList();
            Type type;
            typelist.TryGetValue(typestr, out type);
            if (type == null || type == typeof(object) || !type.IsValueType)
                return;

            WritePrecompileFileForType(typestr);

            var sb_type = new System.Text.StringBuilder();
            sb_type.WriteType(type);
            var filename = GetFileNameForType(typestr);
            var path = GetPrecompileFilePath(type);

            var lines = ReadRawLines(path);
            var thisfolder = System.IO.Path.GetDirectoryName(ModEditorUtils.__FILE__);
            var csfilepath = thisfolder + "/../../Runtime/LuaHubC.cs";
            var cfilepath = thisfolder + "/../../~PluginSrc~/ModLuaNative/src/ModLuaNative.cpp";
            var cslines = ReadRawLines(csfilepath);
            var clines = ReadRawLines(cfilepath);

            var fieldinfos = ParseKeyFieldInfos(type);
            List<ValueTypeKeyFieldInfo> expanded = new List<ValueTypeKeyFieldInfo>(fieldinfos);
            for (int i = 0; i < expanded.Count; ++i)
            {
                var field = expanded[i];
                if (field.subs != null)
                {
                    expanded.AddRange(field.subs);
                }
            }
            if (expanded.Any(info => (info.subs == null || info.subs.Count <= 0) && !IsNativeType(info.type) && !info.type.IsEnum()))
            {
                Debug.LogError("Can not generate c file for " + sb_type);
                return;
            }

            bool verdirty = false;
            int csverline = -1;
            int cverline = -1;
            int csver = -1;
            int cver = -1;
            for (int i = 0; i < cslines.Count; ++i)
            {
                var line = cslines[i];
                if (line.StartsWith("public const int LIB_VER = "))
                {
                    csverline = i;
                    var sub = line.Substring("public const int LIB_VER = ".Length);
                    int lineendindex = sub.IndexOf(";");
                    if (lineendindex > 0)
                    {
                        sub = sub.Substring(0, lineendindex).TrimEnd();
                        int.TryParse(sub, out csver);
                    }
                    break;
                }
            }
            if (csverline < 0 || csver <= 0)
            {
                Debug.LogError("Version of LuaHubC.cs is not correct.");
                return;
            }
            for (int i = 0; i < clines.Count; ++i)
            {
                var line = clines[i];
                if (line.StartsWith("EXPORT_API int lua_checkVer(int ver)"))
                {
                    if (clines.Count > i + 1)
                    {
                        if (clines[i + 1] == "{")
                        {
                            var blockend = EncloseBlock(clines, i + 1);
                            if (blockend > i + 1)
                            {
                                for (int j = i + 2; j < blockend; ++j)
                                {
                                    line = clines[j];
                                    if (line.StartsWith("if (ver != "))
                                    {
                                        cverline = j;
                                        var sub = line.Substring("if (ver != ".Length);
                                        int lineendindex = sub.IndexOf(")");
                                        if (lineendindex > 0)
                                        {
                                            sub = sub.Substring(0, lineendindex).TrimEnd();
                                            int.TryParse(sub, out cver);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (cverline < 0 || cver <= 0)
            {
                Debug.LogError("Version of ModLuaNative.cpp is not correct.");
                return;
            }
            if (csver != cver)
            {
                Debug.LogError("Version of LuaHubC.cs and ModLuaNative.cpp do not equal.");
                return;
            }

            string cspretype = "public static extern void lua_setType" + type.Name;
            string csprepush = "public static extern void lua_push" + type.Name;
            string cspreset = "public static extern void lua_set" + type.Name;
            string cspreget = "public static extern void lua_get" + type.Name;
            string[] cspres = new[] { cspretype, csprepush, cspreset, cspreget };
            string[] csprelines = new string[cspres.Length];
            for (int i = 0; i < cslines.Count; ++i)
            {
                var line = cslines[i];
                for (int j = 0; j < cspres.Length; ++j)
                {
                    var cspre = cspres[j];
                    if (line.StartsWith(cspre))
                    {
                        csprelines[j] = line;
                        cslines.RemoveRange(i - 1, 2);
                        --i;
                        break;
                    }
                }
            }

            string cpretyped = "static void* type" + type.Name;
            string cpretype = "EXPORT_API void lua_setType" + type.Name;
            string cprepush = "EXPORT_API void lua_push" + type.Name;
            string cpreset = "EXPORT_API void lua_set" + type.Name;
            string cpreget = "EXPORT_API void lua_get" + type.Name;
            string[] cpres = new[] { cpretype, cprepush, cpreset, cpreget };
            string cpretypedline = null;
            string[] cprelines = new string[cpres.Length];
            for (int i = 0; i < clines.Count; ++i)
            {
                var line = clines[i];
                if (line.StartsWith(cpretyped))
                {
                    cpretypedline = line;
                    clines.RemoveAt(i--);
                }
                for (int j = 0; j < cpres.Length; ++j)
                {
                    var cpre = cpres[j];
                    if (line.StartsWith(cpre))
                    {
                        cprelines[j] = line;
                        var endpos = EncloseBlock(clines, i + 1);
                        clines.RemoveRange(i, endpos - i + 1);
                        --i;
                        break;
                    }
                }
            }

            // write hubfile
            {
                // write setType
                {
                    var ctordef = "public TypeHubPrecompiled_" + filename + "()";
                    for (int ictor = 0; ictor < lines.Count; ++ictor)
                    {
                        var line = lines[ictor];
                        if (line.StartsWith(ctordef))
                        {
                            var endctor = EncloseBlock(lines, ictor + 1);
                            for (int iblock = ictor + 2; iblock < endctor; ++iblock)
                            {
                                line = lines[iblock];
                                if (line.StartsWith("if (LuaHub.LuaHubC.Ready)"))
                                {
                                    ++iblock;
                                    var endblock = EncloseBlock(lines, iblock);
                                    lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                    ++iblock;
                                    lines.Insert(iblock++, "LuaHub.LuaHubC.lua_setType" + type.Name + "(r);");
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                // write pushLua
                {
                    // notice, the c# part do not need change.
                    var funcdef = "public override IntPtr PushLua(IntPtr l, " + sb_type + " val)";
                    for (int ifunc = 0; ifunc < lines.Count; ++ifunc)
                    {
                        var line = lines[ifunc];
                        if (line.StartsWith(funcdef))
                        {
                            var endfunc = EncloseBlock(lines, ifunc + 1);
                            bool macrofound = false;
                            for (int iblock = ifunc + 2; iblock < endfunc; ++iblock)
                            {
                                line = lines[iblock];
                                if (!macrofound)
                                {
                                    if (line.StartsWith("#if !DISABLE_LUA_HUB_C"))
                                    {
                                        macrofound = true;
                                    }
                                    else
                                    {
                                        lines.RemoveAt(iblock--);
                                        --endfunc;
                                    }
                                    continue;
                                }
                                if (line.StartsWith("if (LuaHub.LuaHubC.Ready)"))
                                {
                                    ++iblock;
                                    var endblock = EncloseBlock(lines, iblock);
                                    lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                    ++iblock;

                                    for (int i = 0; i < expanded.Count; ++i)
                                    {
                                        var field = expanded[i];
                                        System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                        if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                        {
                                            sbline.Append("double ");
                                        }
                                        else
                                        {
                                            sbline.Append("var ");
                                        }
                                        sbline.Append(field.fullfieldname);
                                        sbline.Append(" = ");
                                        if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                        {
                                            sbline.Append("(double)");
                                        }
                                        if (field.parent == null)
                                        {
                                            sbline.Append("val");
                                        }
                                        else
                                        {
                                            sbline.Append(field.parent.fullfieldname);
                                        }
                                        sbline.Append(".");
                                        sbline.Append(field.field);
                                        sbline.Append(";");
                                        lines.Insert(iblock++, sbline.ToString());
                                    }
                                    var sbcall = new System.Text.StringBuilder();
                                    sbcall.Append("LuaHub.LuaHubC.lua_push");
                                    sbcall.Append(type.Name);
                                    sbcall.Append("(l");
                                    for (int i = 0; i < expanded.Count; ++i)
                                    {
                                        var field = expanded[i];
                                        if (field.subs == null || field.subs.Count <= 0)
                                        {
                                            sbcall.Append(", ");
                                            sbcall.Append(field.fullfieldname);
                                        }
                                    }
                                    sbcall.Append(");");
                                    lines.Insert(iblock++, sbcall.ToString());
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                // write setData
                {
                    int cspartstart = -1;
                    int cspartend = -1;
                    // c part
                    {
                        var funcdef = "public static void SetDataRaw(IntPtr l, int index, " + sb_type + " val)";
                        for (int ifunc = 0; ifunc < lines.Count; ++ifunc)
                        {
                            var line = lines[ifunc];
                            if (line.StartsWith(funcdef))
                            {
                                var endfunc = EncloseBlock(lines, ifunc + 1);
                                bool macrofound = false;
                                for (int iblock = ifunc + 2; iblock < endfunc; ++iblock)
                                {
                                    line = lines[iblock];
                                    if (!macrofound)
                                    {
                                        if (line.StartsWith("#if !DISABLE_LUA_HUB_C"))
                                        {
                                            macrofound = true;
                                        }
                                        else
                                        {
                                            lines.RemoveAt(iblock--);
                                            --endfunc;
                                        }
                                        continue;
                                    }
                                    if (line.StartsWith("if (LuaHub.LuaHubC.Ready)"))
                                    {
                                        var oldcnt = lines.Count;
                                        ++iblock;
                                        var endblock = EncloseBlock(lines, iblock);
                                        lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                        ++iblock;

                                        for (int i = 0; i < expanded.Count; ++i)
                                        {
                                            var field = expanded[i];
                                            System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                            if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                            {
                                                sbline.Append("double ");
                                            }
                                            else
                                            {
                                                sbline.Append("var ");
                                            }
                                            sbline.Append(field.fullfieldname);
                                            sbline.Append(" = ");
                                            if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                            {
                                                sbline.Append("(double)");
                                            }
                                            if (field.parent == null)
                                            {
                                                sbline.Append("val");
                                            }
                                            else
                                            {
                                                sbline.Append(field.parent.fullfieldname);
                                            }
                                            sbline.Append(".");
                                            sbline.Append(field.field);
                                            sbline.Append(";");
                                            lines.Insert(iblock++, sbline.ToString());
                                        }
                                        var sbcall = new System.Text.StringBuilder();
                                        sbcall.Append("LuaHub.LuaHubC.lua_set");
                                        sbcall.Append(type.Name);
                                        sbcall.Append("(l, index");
                                        for (int i = 0; i < expanded.Count; ++i)
                                        {
                                            var field = expanded[i];
                                            if (field.subs == null || field.subs.Count <= 0)
                                            {
                                                sbcall.Append(", ");
                                                sbcall.Append(field.fullfieldname);
                                            }
                                        }
                                        sbcall.Append(");");
                                        lines.Insert(iblock++, sbcall.ToString());
                                        cspartstart = iblock;
                                        endfunc += lines.Count - oldcnt;
                                        cspartend = endfunc;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    // c# part
                    {
                        for (int iblock = cspartstart + 1; iblock < cspartend; ++iblock)
                        {
                            var line = lines[iblock];
                            if (line.StartsWith("#endif"))
                            {
                                ++iblock;
                                var endblock = EncloseBlock(lines, iblock);
                                lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                ++iblock;

                                lines.Insert(iblock++, "l.checkstack(3);");
                                lines.Insert(iblock++, "l.pushvalue(index);");
                                for (int i = 0; i < fieldinfos.Count; ++i)
                                {
                                    var field = fieldinfos[i];
                                    if (field.parent == null)
                                    {
                                        lines.Insert(iblock++, "l.PushString(" + field.luastring + ");");
                                        lines.Insert(iblock++, "l.PushLua(val." + field.field + ");");
                                        lines.Insert(iblock++, "l.rawset(-3);");
                                    }
                                }
                                lines.Insert(iblock++, "l.pop(1);");
                                break;
                            }
                        }
                    }
                }
                // write getLua
                {
                    int cspartstart = -1;
                    int cspartend = -1;
                    // c part
                    {
                        var funcdef = "public static " + sb_type + " GetLuaRaw(IntPtr l, int index)";
                        for (int ifunc = 0; ifunc < lines.Count; ++ifunc)
                        {
                            var line = lines[ifunc];
                            if (line.StartsWith(funcdef))
                            {
                                var endfunc = EncloseBlock(lines, ifunc + 1);
                                bool macrofound = false;
                                for (int iblock = ifunc + 2; iblock < endfunc; ++iblock)
                                {
                                    line = lines[iblock];
                                    if (!macrofound)
                                    {
                                        if (line.StartsWith("#if !DISABLE_LUA_HUB_C"))
                                        {
                                            macrofound = true;
                                            lines.Insert(iblock++, sb_type + " rv = new " + sb_type + "();");
                                        }
                                        else
                                        {
                                            lines.RemoveAt(iblock--);
                                            --endfunc;
                                        }
                                        continue;
                                    }
                                    if (line.StartsWith("if (LuaHub.LuaHubC.Ready)"))
                                    {
                                        var oldcnt = lines.Count;
                                        ++iblock;
                                        var endblock = EncloseBlock(lines, iblock);
                                        lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                        ++iblock;

                                        for (int i = 0; i < expanded.Count; ++i)
                                        {
                                            var field = expanded[i];
                                            System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                            if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                            {
                                                sbline.Append("double ");
                                                sbline.Append(field.fullfieldname);
                                                sbline.Append(" = 0;");
                                            }
                                            else
                                            {
                                                sbline.WriteType(field.type);
                                                sbline.Append(" ");
                                                sbline.Append(field.fullfieldname);
                                                sbline.Append(" = default(");
                                                sbline.WriteType(field.type);
                                                sbline.Append(");");
                                            }
                                            lines.Insert(iblock++, sbline.ToString());
                                        }
                                        var sbcall = new System.Text.StringBuilder();
                                        sbcall.Append("LuaHub.LuaHubC.lua_get");
                                        sbcall.Append(type.Name);
                                        sbcall.Append("(l, index");
                                        for (int i = 0; i < expanded.Count; ++i)
                                        {
                                            var field = expanded[i];
                                            if (field.subs == null || field.subs.Count <= 0)
                                            {
                                                sbcall.Append(", out ");
                                                sbcall.Append(field.fullfieldname);
                                            }
                                        }
                                        sbcall.Append(");");
                                        lines.Insert(iblock++, sbcall.ToString());
                                        for (int i = expanded.Count - 1; i >= 0; --i)
                                        {
                                            var field = expanded[i];
                                            System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                            if (field.parent == null)
                                            {
                                                sbline.Append("rv");
                                            }
                                            else
                                            {
                                                sbline.Append(field.parent.fullfieldname);
                                            }
                                            sbline.Append(".");
                                            sbline.Append(field.field);
                                            sbline.Append(" = ");
                                            if (ExplicitToDoubleTypes.ContainsKey(field.type))
                                            {
                                                sbline.Append("(");
                                                sbline.Append(ExplicitToDoubleTypes[field.type]);
                                                sbline.Append(")");
                                            }
                                            else if (field.type.IsEnum())
                                            {
                                                sbline.Append("(");
                                                sbline.WriteType(field.type);
                                                sbline.Append(")");
                                            }
                                            sbline.Append(field.fullfieldname);
                                            sbline.Append(";");
                                            lines.Insert(iblock++, sbline.ToString());
                                        }
                                        cspartstart = iblock;
                                        endfunc += lines.Count - oldcnt;
                                        cspartend = endfunc;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    // c# part
                    {
                        for (int iblock = cspartstart + 1; iblock < cspartend; ++iblock)
                        {
                            var line = lines[iblock];
                            if (line.StartsWith("#endif"))
                            {
                                ++iblock;
                                var endblock = EncloseBlock(lines, iblock);
                                lines.RemoveRange(iblock + 1, endblock - iblock - 1);
                                ++iblock;

                                for (int i = 0; i < fieldinfos.Count; ++i)
                                {
                                    var field = fieldinfos[i];
                                    if (field.parent == null)
                                    {
                                        System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                        sbline.WriteType(field.type);
                                        sbline.Append(" ");
                                        sbline.Append(field.field);
                                        sbline.Append(";");
                                        lines.Insert(iblock++, sbline.ToString());
                                    }
                                }

                                lines.Insert(iblock++, "l.checkstack(2);");
                                lines.Insert(iblock++, "l.pushvalue(index);");
                                for (int i = 0; i < fieldinfos.Count; ++i)
                                {
                                    var field = fieldinfos[i];
                                    if (field.parent == null)
                                    {
                                        lines.Insert(iblock++, "l.PushString(" + field.luastring + ");");
                                        lines.Insert(iblock++, "l.rawget(-2);");
                                        lines.Insert(iblock++, "l.GetLua(-1, out " + field.field + ");");
                                        lines.Insert(iblock++, "l.pop(1);");
                                    }
                                }
                                lines.Insert(iblock++, "l.pop(1);");

                                for (int i = 0; i < fieldinfos.Count; ++i)
                                {
                                    var field = fieldinfos[i];
                                    if (field.parent == null)
                                    {
                                        System.Text.StringBuilder sbline = new System.Text.StringBuilder();
                                        sbline.Append("rv.");
                                        sbline.Append(field.field);
                                        sbline.Append(" = ");
                                        sbline.Append(field.field);
                                        sbline.Append(";");
                                        lines.Insert(iblock++, sbline.ToString());
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                // write to file
                WriteLines(path, lines);
            }
            // write hubc.cs
            for (int iline = cslines.Count - 1; iline >= 0; --iline)
            {
                var line = cslines[iline];
                if (line.StartsWith("#endif"))
                {
                    cslines.Insert(iline++, "[DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]");
                    cslines.Insert(iline++, "public static extern void lua_setType" + type.Name + "(IntPtr type);");
                    if (csprelines[0] != cslines[iline - 1])
                    {
                        verdirty = true;
                    }
                    {
                        cslines.Insert(iline++, "[DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]");
                        var sbline = new System.Text.StringBuilder();
                        sbline.Append("public static extern void lua_push");
                        sbline.Append(type.Name);
                        sbline.Append("(IntPtr l");
                        for (int i = 0; i < expanded.Count; ++i)
                        {
                            var field = expanded[i];
                            if (field.subs == null || field.subs.Count <= 0)
                            {
                                sbline.Append(", ");
                                if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                {
                                    sbline.Append("double");
                                }
                                else
                                {
                                    sbline.WriteType(field.type);
                                }
                                sbline.Append(" ");
                                sbline.Append(field.fullfieldname);
                            }
                        }
                        sbline.Append(");");
                        cslines.Insert(iline++, sbline.ToString());
                        if (csprelines[1] != cslines[iline - 1])
                        {
                            verdirty = true;
                        }
                    }
                    {
                        cslines.Insert(iline++, "[DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]");
                        var sbline = new System.Text.StringBuilder();
                        sbline.Append("public static extern void lua_set");
                        sbline.Append(type.Name);
                        sbline.Append("(IntPtr l, int index");
                        for (int i = 0; i < expanded.Count; ++i)
                        {
                            var field = expanded[i];
                            if (field.subs == null || field.subs.Count <= 0)
                            {
                                sbline.Append(", ");
                                if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                {
                                    sbline.Append("double");
                                }
                                else
                                {
                                    sbline.WriteType(field.type);
                                }
                                sbline.Append(" ");
                                sbline.Append(field.fullfieldname);
                            }
                        }
                        sbline.Append(");");
                        cslines.Insert(iline++, sbline.ToString());
                        if (csprelines[2] != cslines[iline - 1])
                        {
                            verdirty = true;
                        }
                    }
                    {
                        cslines.Insert(iline++, "[DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]");
                        var sbline = new System.Text.StringBuilder();
                        sbline.Append("public static extern void lua_get");
                        sbline.Append(type.Name);
                        sbline.Append("(IntPtr l, int index");
                        for (int i = 0; i < expanded.Count; ++i)
                        {
                            var field = expanded[i];
                            if (field.subs == null || field.subs.Count <= 0)
                            {
                                sbline.Append(", out ");
                                if (ExplicitToDoubleTypes.ContainsKey(field.type) || field.type.IsEnum())
                                {
                                    sbline.Append("double");
                                }
                                else
                                {
                                    sbline.WriteType(field.type);
                                }
                                sbline.Append(" ");
                                sbline.Append(field.fullfieldname);
                            }
                        }
                        sbline.Append(");");
                        cslines.Insert(iline++, sbline.ToString());
                        if (csprelines[3] != cslines[iline - 1])
                        {
                            verdirty = true;
                        }
                    }
                    break;
                }
            }
            // write c file
            {
                for (int i = clines.Count - 1; i >= 0; --i)
                {
                    var line = clines[i];
                    clines.RemoveAt(i);
                    if (line == "}")
                    {
                        break;
                    }
                    else
                    {
                        clines.RemoveAt(i);
                    }
                }
                clines.Add("");
                // setType
                {
                    clines.Add("static void* type" + type.Name + " = 0;");
                    if (clines[clines.Count - 1] != cpretypedline)
                    {
                        verdirty = true;
                    }
                    clines.Add("EXPORT_API void lua_setType" + type.Name + "(void* type)");
                    if (clines[clines.Count - 1] != cprelines[0])
                    {
                        verdirty = true;
                    }
                    clines.Add("{");
                    clines.Add("type" + type.Name + " = type;");
                    clines.Add("}");
                }
                // push
                {
                    var sbdef = new System.Text.StringBuilder();
                    sbdef.Append("EXPORT_API void lua_push");
                    sbdef.Append(type.Name);
                    sbdef.Append("(lua_State *l");
                    for (int i = 0; i < expanded.Count; ++i)
                    {
                        var field = expanded[i];
                        if (field.subs == null || field.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(field.type))
                            {
                                Debug.LogError("unknown c type for " + field.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[field.type];
                            sbdef.Append(", ");
                            sbdef.Append(stinfo.sname);
                            sbdef.Append(" ");
                            sbdef.Append(field.fullfieldname);
                        }
                    }
                    sbdef.Append(")");
                    clines.Add(sbdef.ToString());
                    if (clines[clines.Count - 1] != cprelines[1])
                    {
                        verdirty = true;
                    }

                    clines.Add("{");
                    clines.Add("lua_checkstack(l, 3);");
                    clines.Add("");
                    clines.Add("lua_newtable(l); // otab");
                    for (int i = 0; i < fieldinfos.Count; ++i)
                    {
                        var directfield = fieldinfos[i];
                        if (directfield.subs == null || directfield.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(directfield.type))
                            {
                                Debug.LogError("unknown c type for " + directfield.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[directfield.type];
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append(stinfo.pushfunc);
                            sbpush.Append("(l, ");
                            if (!string.IsNullOrEmpty(stinfo.convto))
                            {
                                sbpush.Append("(");
                                sbpush.Append(stinfo.convto);
                                sbpush.Append(")");
                            }
                            sbpush.Append(directfield.fullfieldname);
                            sbpush.Append(");");
                            clines.Add(sbpush.ToString());
                            clines.Add("lua_setfield(l, -2, \"" + directfield.field + "\");");
                        }
                        else
                        {
                            List<ValueTypeKeyFieldInfo> children = new List<ValueTypeKeyFieldInfo>() { directfield };
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs != null)
                                {
                                    children.AddRange(child.subs);
                                }
                            }
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append("lua_push");
                            sbpush.Append(directfield.type.Name);
                            sbpush.Append("(l");
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs == null || child.subs.Count <= 0)
                                {
                                    sbpush.Append(", ");
                                    sbpush.Append(child.fullfieldname);
                                }
                            }
                            sbpush.Append(");");
                            clines.Add(sbpush.ToString());
                            clines.Add("lua_setfield(l, -2, \"" + directfield.field + "\");");
                        }
                    }

                    clines.Add("");
                    clines.Add("lua_pushlightuserdata(l, (void*)1003); // otab #cache");
                    clines.Add("lua_gettable(l, LUA_REGISTRYINDEX); // otab cache");
                    clines.Add("if (lua_istable(l, -1))");
                    clines.Add("{");
                    clines.Add("lua_pushlightuserdata(l, type" + type.Name + "); // otab cache #type");
                    clines.Add("lua_gettable(l, -2); // otab cache type");
                    clines.Add("lua_remove(l, -2); // otab type");
                    clines.Add("}");

                    clines.Add("");
                    clines.Add("if (!lua_istable(l, -1))");
                    clines.Add("{");
                    clines.Add("lua_pop(l, 1); // otab");
                    clines.Add("luacs_pushType(l, type" + type.Name + "); // otab type");
                    clines.Add("}");

                    clines.Add("");
                    clines.Add("lua_pushlightuserdata(l, (void*)2101); // otab type #meta");
                    clines.Add("lua_rawget(l, -2); // otab type meta");
                    clines.Add("lua_setmetatable(l, -3); // otab type");
                    clines.Add("lua_pop(l, 1); // otab");
                    clines.Add("}");
                }
                // set
                {
                    var sbdef = new System.Text.StringBuilder();
                    sbdef.Append("EXPORT_API void lua_set");
                    sbdef.Append(type.Name);
                    sbdef.Append("(lua_State *l, int index");
                    for (int i = 0; i < expanded.Count; ++i)
                    {
                        var field = expanded[i];
                        if (field.subs == null || field.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(field.type))
                            {
                                Debug.LogError("unknown c type for " + field.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[field.type];
                            sbdef.Append(", ");
                            sbdef.Append(stinfo.sname);
                            sbdef.Append(" ");
                            sbdef.Append(field.fullfieldname);
                        }
                    }
                    sbdef.Append(")");
                    clines.Add(sbdef.ToString());
                    if (clines[clines.Count - 1] != cprelines[2])
                    {
                        verdirty = true;
                    }

                    clines.Add("{");
                    clines.Add("lua_checkstack(l, 1);");
                    clines.Add("");
                    clines.Add("index = abs_index(l, index);");
                    clines.Add("");
                    for (int i = 0; i < fieldinfos.Count; ++i)
                    {
                        var directfield = fieldinfos[i];
                        if (directfield.subs == null || directfield.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(directfield.type))
                            {
                                Debug.LogError("unknown c type for " + directfield.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[directfield.type];
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append(stinfo.pushfunc);
                            sbpush.Append("(l, ");
                            if (!string.IsNullOrEmpty(stinfo.convto))
                            {
                                sbpush.Append("(");
                                sbpush.Append(stinfo.convto);
                                sbpush.Append(")");
                            }
                            sbpush.Append(directfield.fullfieldname);
                            sbpush.Append(");");
                            clines.Add(sbpush.ToString());
                            clines.Add("lua_setfield(l, index, \"" + directfield.field + "\");");
                        }
                        else
                        {
                            List<ValueTypeKeyFieldInfo> children = new List<ValueTypeKeyFieldInfo>() { directfield };
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs != null)
                                {
                                    children.AddRange(child.subs);
                                }
                            }
                            clines.Add("lua_getfield(l, index, \"" + directfield.field + "\");");
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append("lua_set");
                            sbpush.Append(directfield.type.Name);
                            sbpush.Append("(l, -1");
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs == null || child.subs.Count <= 0)
                                {
                                    sbpush.Append(", ");
                                    sbpush.Append(child.fullfieldname);
                                }
                            }
                            sbpush.Append(");");
                            clines.Add(sbpush.ToString());
                            clines.Add("lua_pop(l, 1);");
                        }
                    }
                    clines.Add("}");
                }
                // get
                {
                    var sbdef = new System.Text.StringBuilder();
                    sbdef.Append("EXPORT_API void lua_get");
                    sbdef.Append(type.Name);
                    sbdef.Append("(lua_State *l, int index");
                    for (int i = 0; i < expanded.Count; ++i)
                    {
                        var field = expanded[i];
                        if (field.subs == null || field.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(field.type))
                            {
                                Debug.LogError("unknown c type for " + field.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[field.type];
                            sbdef.Append(", ");
                            sbdef.Append(stinfo.sname);
                            sbdef.Append("* ");
                            sbdef.Append(field.fullfieldname);
                        }
                    }
                    sbdef.Append(")");
                    clines.Add(sbdef.ToString());
                    if (clines[clines.Count - 1] != cprelines[3])
                    {
                        verdirty = true;
                    }

                    clines.Add("{");
                    clines.Add("lua_checkstack(l, 1);");
                    clines.Add("");
                    clines.Add("index = abs_index(l, index);");
                    clines.Add("");
                    for (int i = 0; i < fieldinfos.Count; ++i)
                    {
                        var directfield = fieldinfos[i];
                        clines.Add("lua_getfield(l, index, \"" + directfield.field + "\");");
                        if (directfield.subs == null || directfield.subs.Count <= 0)
                        {
                            if (!SpecialTypes.ContainsKey(directfield.type))
                            {
                                Debug.LogError("unknown c type for " + directfield.type.ToString());
                                continue;
                            }
                            var stinfo = SpecialTypes[directfield.type];
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append("*");
                            sbpush.Append(directfield.fullfieldname);
                            sbpush.Append(" = (");
                            sbpush.Append(stinfo.sname);
                            sbpush.Append(")");
                            sbpush.Append(stinfo.getfunc);
                            sbpush.Append("(l, -1);");
                            clines.Add(sbpush.ToString());
                        }
                        else
                        {
                            List<ValueTypeKeyFieldInfo> children = new List<ValueTypeKeyFieldInfo>() { directfield };
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs != null)
                                {
                                    children.AddRange(child.subs);
                                }
                            }
                            var sbpush = new System.Text.StringBuilder();
                            sbpush.Append("lua_get");
                            sbpush.Append(directfield.type.Name);
                            sbpush.Append("(l, -1");
                            for (int j = 0; j < children.Count; ++j)
                            {
                                var child = children[j];
                                if (child.subs == null || child.subs.Count <= 0)
                                {
                                    sbpush.Append(", ");
                                    sbpush.Append(child.fullfieldname);
                                }
                            }
                            sbpush.Append(");");
                            clines.Add(sbpush.ToString());
                        }
                        clines.Add("lua_pop(l, 1);");
                    }
                    clines.Add("}");
                }
                clines.Add("}");
            }

            if (verdirty)
            {
                var ver = csver + 1;
                cslines[csverline] = "public const int LIB_VER = " + ver + ";";
                clines[cverline] = "if (ver != " + ver + ")";
                WriteLines(csfilepath, cslines);
                WriteLines(cfilepath, clines);
            }
        }

        public static void WritePrecompile(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return;
            }
            bool overwrite = command.EndsWith("!");
            if (overwrite)
            {
                command = command.Substring(0, command.Length - "!".Length).TrimEnd();
            }
            WritePrecompile(command, overwrite);
        }
        public static string TrimComment(string command)
        {
            if (command == null)
            {
                return null;
            }
            var commentIndex = command.IndexOf("//");
            if (commentIndex >= 0)
            {
                return command.Substring(0, commentIndex).TrimEnd();
            }
            return command;
        }
        public static void WritePrecompile(string command, bool overwrite)
        {
            if (string.IsNullOrEmpty(command))
            {
                return;
            }
            if (command.StartsWith("hubc "))
            {
                var rest = command.Substring("hubc ".Length).Trim();
                WritePrecompileFuncForHubC(rest);
            }
            else if (command.StartsWith("type "))
            {
                var rest = command.Substring("type ".Length).Trim();
                WritePrecompileFileForType(rest, overwrite);
            }
            else if (command.StartsWith("member "))
            {
                WritePrecompileFuncForMember(command, overwrite);
            }
            else if (command.StartsWith("file "))
            {
                var path = TrimComment(command.Substring("file ".Length));
                if (PlatDependant.IsFileExist(path))
                {
                    using (var sr = PlatDependant.OpenReadText(path))
                    {
                        while (true)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                                break;
                            if (!string.IsNullOrEmpty(line))
                            {
                                if (overwrite)
                                {
                                    WritePrecompile(line, true);
                                }
                                else
                                {
                                    WritePrecompile(line);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static class LuaPrecompileWriter
    {
        internal abstract class ConstructorWrapper { }
        internal abstract class ConstructorWrapperOfType<T> : ConstructorWrapper
        {
            public ConstructorWrapperOfType() { }
        }

        public static void WriteType(this System.Text.StringBuilder sb, Type t)
        {
            WriteType(sb, t, false);
        }
        public static void WriteType(this System.Text.StringBuilder sb, Type t, bool EmptyOnGP)
        {
            if (t.IsArray)
            {
                var rank = t.GetArrayRank();
                WriteType(sb, t.GetElementType(), EmptyOnGP);
                sb.Append("[");
                if (rank > 1)
                {
                    sb.Append(',', rank - 1);
                }
                sb.Append("]");
            }
            else if (t.IsGenericType)
            {
                var split = t.FullName.IndexOf('`');
                sb.Append(t.FullName.Substring(0, split).Replace('+', '.'));
                sb.Append("<");
                var gpars = t.GetGenericArguments();
                for (int i = 0; i < gpars.Length; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    sb.WriteType(gpars[i], EmptyOnGP);
                }
                sb.Append(">");
            }
            else
            {
                if (t.IsGenericParameter)
                {
                    if (!EmptyOnGP)
                    {
                        sb.Append(t.Name);
                    }
                }
                else
                {
                    sb.Append(t.FullName.Replace('+', '.'));
                }
            }
        }

        public static Dictionary<Type, string> nativeTypeMap = new Dictionary<Type, string>()
        {
            { typeof(bool), "LUA_TBOOLEAN"},
            { typeof(string), "LUA_TSTRING"},
            { typeof(byte[]), "LUA_TSTRING"},
            { typeof(IntPtr), "LUA_TLIGHTUSERDATA"},
            { typeof(byte), "LUA_TNUMBER" },
            { typeof(char), "LUA_TNUMBER" },
            { typeof(decimal), "LUA_TNUMBER" },
            { typeof(double), "LUA_TNUMBER" },
            { typeof(short), "LUA_TNUMBER" },
            { typeof(int), "LUA_TNUMBER" },
            { typeof(long), "LUA_TNUMBER" },
            { typeof(sbyte), "LUA_TNUMBER" },
            { typeof(float), "LUA_TNUMBER" },
            { typeof(ushort), "LUA_TNUMBER" },
            { typeof(uint), "LUA_TNUMBER" },
            { typeof(ulong), "LUA_TNUMBER" },
        };
        public static bool TryGetLuaType(Type type, out string luatype)
        {
            if (type == null)
            {
                luatype = null;
                return false;
            }
            var nntype = Nullable.GetUnderlyingType(type);
            type = nntype ?? type;
            if (type.IsEnum)
            {
                luatype = "LUA_TNUMBER";
                return true;
            }
            return nativeTypeMap.TryGetValue(type, out luatype);
        }
        public static string GetLuaType(Type type)
        {
            string luatype;
            TryGetLuaType(type, out luatype);
            return luatype;
        }
        public static Dictionary<string, Type> nativeRevMap = new Dictionary<string, Type>()
        {
            { "LUA_TBOOLEAN", typeof(bool) },
            { "LUA_TSTRING", typeof(string) },
            { "LUA_TLIGHTUSERDATA", typeof(IntPtr) },
            { "LUA_TNUMBER", typeof(double) },
        };
        public static Dictionary<string, string> binOps = new Dictionary<string, string>()
        {
            { "op_Addition", "+" },
            { "op_Subtraction", "-" },
            { "op_Multiply", "*" },
            { "op_Division", "/" },
            { "op_Modulus", "%" },
            { "op_Inequality", "!=" },
            { "op_Equality", "==" },
            { "op_GreaterThan", ">" },
            { "op_LessThan", "<" },
            { "op_GreaterThanOrEqual", ">=" },
            { "op_LessThanOrEqual", "<=" },
        };
        public static Dictionary<string, string> unmOps = new Dictionary<string, string>()
        {
            { "op_Increment", "++" },
            { "op_Decrement", "--" },
            { "op_UnaryPlus", "+" },
            { "op_UnaryNegation", "-" },
        };
        public static HashSet<string> convertOps = new HashSet<string>()
        {
            "op_Explicit",
            "op_Implicit",
        };
        internal class MethodOverload
        {
            public MethodBase Method;
            public int LabelOffset;
            public Types ArgTypes;

            public static implicit operator MethodBase(MethodOverload overload)
            {
                return overload != null ? overload.Method : null;
            }

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < ArgTypes.Count; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(ArgTypes[i].Name);
                }
                return sb.ToString();
            }
        }
        internal static bool IsExplicitCall(Types thisArgs, Types checkingArgs)
        {
            if (thisArgs.Count < checkingArgs.Count)
            {
                return false;
            }
            for (int i = 0; i < checkingArgs.Count; ++i)
            {
                if (!checkingArgs[i].IsAssignableFrom(thisArgs[i]))
                {
                    return false;
                }
            }
            return true;
        }
        internal struct TypeOrLuaType
        {
            private Pack<Type, string> _Data;
            public Type ClrType { get { return _Data.t1; } set { _Data.t1 = value; } }
            public string LuaType { get { return _Data.t2; } set { _Data.t2 = value; } }

            public TypeOrLuaType(Type clrType, string luaType)
            {
                _Data = new Pack<Type, string>(clrType, luaType);
            }
            public TypeOrLuaType(Type clrType)
            {
                _Data = new Pack<Type, string>(clrType, null);
            }
            public TypeOrLuaType(string luaType)
            {
                _Data = new Pack<Type, string>(null, luaType);
            }

            public override int GetHashCode()
            {
                return _Data.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj is TypeOrLuaType)
                {
                    return Equals((TypeOrLuaType)obj);
                }
                else if (obj is Type)
                {
                    return Equals((Type)obj);
                }
                else if (obj is string)
                {
                    return Equals((string)obj);
                }
                else if (obj is Pack<Type, string>)
                {
                    return _Data.Equals((Pack<Type, string>)obj);
                }
                return false;
            }
            public bool Equals(TypeOrLuaType other)
            {
                return _Data.Equals(other._Data);
            }
            public bool Equals(Type clrType)
            {
                return ClrType.Equals(clrType);
            }
            public bool Equals(string luaType)
            {
                return LuaType == luaType;
            }
            public override string ToString()
            {
                return _Data.ToString();
            }

            public static bool operator==(TypeOrLuaType v1, TypeOrLuaType v2)
            {
                return v1.Equals(v2);
            }
            public static bool operator!=(TypeOrLuaType v1, TypeOrLuaType v2)
            {
                return !v1.Equals(v2);
            }
            public static bool operator==(TypeOrLuaType v1, Type v2)
            {
                return v1.Equals(v2);
            }
            public static bool operator!=(TypeOrLuaType v1, Type v2)
            {
                return !v1.Equals(v2);
            }
            public static bool operator==(TypeOrLuaType v1, string v2)
            {
                return v1.Equals(v2);
            }
            public static bool operator!=(TypeOrLuaType v1, string v2)
            {
                return !v1.Equals(v2);
            }
            public static implicit operator TypeOrLuaType(Type clrType)
            {
                return new TypeOrLuaType(clrType);
            }
            public static implicit operator TypeOrLuaType(string luaType)
            {
                return new TypeOrLuaType(luaType);
            }
            public static implicit operator Type(TypeOrLuaType v)
            {
                return v.ClrType;
            }
            public static implicit operator string(TypeOrLuaType v)
            {
                return v.LuaType;
            }
        }
        internal class WriteMethodBodyContext
        {
            internal struct MethodBaseWithExtraInfo
            {
                public MethodBase Method;
                public int Label;
                public bool LastArgIsParam;
                public bool Static;
                public Types ArgTypes;
                public Dictionary<int, string> RefOrOutArgs;

                public string GetArgByRefModifier(int index)
                {
                    if (RefOrOutArgs == null)
                    {
                        return "";
                    }
                    else
                    {
                        if (RefOrOutArgs.ContainsKey(index))
                            return RefOrOutArgs[index];
                        else
                            return "";
                    }
                }
                public bool IsArgRefOrOut(int index)
                {
                    var modifier = GetArgByRefModifier(index);
                    return !string.IsNullOrEmpty(modifier) && modifier != "in";
                }
                public bool IsArgByRef(int index)
                {
                    return !string.IsNullOrEmpty(GetArgByRefModifier(index));
                }
            }

            private System.Text.StringBuilder _sb = new System.Text.StringBuilder();
            private bool Written_gettop = false;
            private IList<MethodBase> _Methods;
            private HashSet<MethodBase> _DoneMethods = new HashSet<MethodBase>();
            private Dictionary<MethodBase, MethodBaseWithExtraInfo> _MethodEx = new Dictionary<MethodBase, MethodBaseWithExtraInfo>();
            private IList<MethodOverload> _Overloads;
            private List<Dictionary<TypeOrLuaType, HashSet<MethodOverload>>> _ByObjType;
            public IList<MethodOverload> Overloads
            {
                get
                {
                    if (_Overloads == null)
                    {
                        GenerateByObjType();
                    }
                    return _Overloads;
                }
            }
            public List<Dictionary<TypeOrLuaType, HashSet<MethodOverload>>> ByObjType
            {
                get
                {
                    if (_ByObjType == null)
                    {
                        GenerateByObjType();
                    }
                    return _ByObjType;
                }
            }

            public System.Text.StringBuilder sb { get { return _sb; } }
            public bool Write_gettop()
            {
                if (Written_gettop)
                {
                    return false;
                }
                else
                {
                    Written_gettop = true;
                    sb.AppendLine("var oldtop = l.gettop();");
                    return true;
                }
            }
            public IList<MethodBase> Methods
            {
                get { return _Methods; }
                set
                {
                    _Methods = value;
                    for (int i = 0; i < value.Count; ++i)
                    {
                        var method = value[i];
                        var exinfo = new MethodBaseWithExtraInfo();
                        exinfo.Method = method;
                        exinfo.Label = i * 10 + 10;

                        var pars = method.GetParameters();
                        Types types = new Types();
                        if (method is ConstructorInfo)
                        {
                            types.Add(typeof(Type));
                        }
                        else if (!method.IsStatic)
                        {
                            types.Add(method.ReflectedType);
                        }
                        else
                        {
                            exinfo.Static = true;
                        }
                        if (pars != null && pars.Length > 0)
                        {
                            for (int j = 0; j < pars.Length; ++j)
                            {
                                var ptype = pars[j].ParameterType;
                                if (ptype.IsByRef)
                                {
                                    if (exinfo.RefOrOutArgs == null)
                                    {
                                        exinfo.RefOrOutArgs = new Dictionary<int, string>();
                                    }
                                    if (pars[j].IsOut)
                                    {
                                        exinfo.RefOrOutArgs[types.Count] = "out";
                                    }
#if RUNTIME_HAS_READONLY_REF
                                    else if (pars[j].IsIn)
                                    {
                                        exinfo.RefOrOutArgs[types.Count] = "in";
                                    }
#endif
                                    else
                                    {
                                        exinfo.RefOrOutArgs[types.Count] = "ref";
                                    }
                                    types.Add(ptype.GetElementType());
                                }
                                else
                                {
                                    types.Add(ptype);
                                }
                            }
                            var lastpar = pars[pars.Length - 1];
                            if (lastpar.ParameterType.IsArray)
                            {
                                var attrs = lastpar.GetCustomAttributes(typeof(ParamArrayAttribute), true);
#if NETFX_CORE
                            if (attrs != null && attrs.Count() > 0)
#else
                                if (attrs != null && attrs.Length > 0)
#endif
                                {
                                    exinfo.LastArgIsParam = true;
                                }
                            }
                        }
                        exinfo.ArgTypes = types;

                        _MethodEx[method] = exinfo;
                    }
                }
            }
            public HashSet<MethodBase> DoneMethods { get { return _DoneMethods; } }
            public List<MethodBase> GetUndoneMethods()
            {
                List<MethodBase> list = new List<MethodBase>();
                if (_Methods != null)
                {
                    foreach (var m in _Methods)
                    {
                        if (!_DoneMethods.Contains(m))
                        {
                            list.Add(m);
                        }
                    }
                }
                return list;
            }
            public MethodBaseWithExtraInfo GetMethodEx(MethodBase method)
            {
                MethodBaseWithExtraInfo info;
                _MethodEx.TryGetValue(method, out info);
                return info;
            }
            private List<MethodOverload> GetOverloads()
            {
                int maxargsCnt = (from method in _Methods
                                  let pars = method.GetParameters()
                                  select pars == null ? 0 : pars.Length).Max() + 1;
                List<MethodOverload> overloads = new List<MethodOverload>();
                _Overloads = overloads;
                for (int i = 0; i < _Methods.Count; ++i)
                {
                    var method = _Methods[i];
                    //if (_DoneMethods.Contains(method))
                    //{
                    //    continue;
                    //}

                    var ex = GetMethodEx(method);
                    overloads.Add(new MethodOverload()
                    {
                        Method = method,
                        ArgTypes = ex.ArgTypes,
                        LabelOffset = ex.LastArgIsParam ? 1 : 0,
                    });
                    if (ex.LastArgIsParam)
                    {
                        Types overloadArgs = ex.ArgTypes.Clone();
                        overloadArgs.RemoveAt(overloadArgs.Count - 1);
                        overloads.Add(new MethodOverload()
                        {
                            Method = method,
                            ArgTypes = overloadArgs.Clone(),
                            LabelOffset = 2,
                        });
                        var eletype = ex.ArgTypes[overloadArgs.Count].GetElementType();
                        for (int j = overloadArgs.Count + 1; j <= maxargsCnt; ++j)
                        {
                            overloadArgs.Add(eletype);
                            overloads.Add(new MethodOverload()
                            {
                                Method = method,
                                ArgTypes = overloadArgs.Clone(),
                                LabelOffset = 2,
                            });
                        }
                    }
                }
                return overloads;
            }
            private void AddByObjType(int index, Type type, MethodOverload method)
            {
                var map = _ByObjType[index];
                HashSet<MethodOverload> list;
                if (!map.TryGetValue(type, out list))
                {
                    list = new HashSet<MethodOverload>();
                    map[type] = list;
                }
                list.Add(method);
                foreach (var kvp in map)
                {
                    var ot = kvp.Key.ClrType;
                    if (ot == null)
                        continue; // luaType
                    if (ot == type)
                        continue;
                    //string otluatype, tluatype;
                    //if (nativeTypeMap.TryGetValue(ot, out otluatype) && nativeTypeMap.TryGetValue(type, out tluatype) && otluatype == tluatype)
                    //{ // Numeric. but not Enums
                    //    kvp.Value.UnionWith(list);
                    //    list.UnionWith(kvp.Value);
                    //}
                    //else
                    {
                        // comp                 behav
                        if (ot.IsAssignableFrom(type))
                        {
                            foreach (var overload in kvp.Value)
                            {
                                if (!list.Any(exising => IsExplicitCall(exising.ArgTypes, overload.ArgTypes)))
                                {
                                    list.Add(overload);
                                }
                            }
                            //list.UnionWith(kvp.Value);
                        }
                        if (type.IsAssignableFrom(ot))
                        {
                            foreach (var overload in list)
                            {
                                if (!kvp.Value.Any(exising => IsExplicitCall(exising.ArgTypes, overload.ArgTypes)))
                                {
                                    kvp.Value.Add(overload);
                                }
                            }
                            //kvp.Value.UnionWith(list);
                        }
                    }
                }
            }
            private void AddByLuaType(int index, Type type, MethodOverload method)
            {
                string luaType;
                if (!TryGetLuaType(type, out luaType))
                {
                    return;
                }
                var map = _ByObjType[index];
                HashSet<MethodOverload> list;
                if (!map.TryGetValue(luaType, out list))
                {
                    list = new HashSet<MethodOverload>();
                    map[luaType] = list;
                }
                list.Add(method);
            }
            private void AddObjTypeToLuaType(int index)
            {
                var map = _ByObjType[index];
                HashSet<MethodOverload> listobj;
                if (map.TryGetValue(typeof(object), out listobj))
                {
                    foreach (var kvp in map)
                    {
                        if (kvp.Key.ClrType == null)
                        {
                            kvp.Value.UnionWith(listobj);
                        }
                    }
                }
            }
            private void CombineUniqueLuaType(int index)
            {
                var map = _ByObjType[index];

                Dictionary<string, Type> luatypecomb = new Dictionary<string, Type>();
                foreach (var kvplua in map)
                {
                    if (kvplua.Key.LuaType != null)
                    {
                        int clrcnt = 0;
                        Type clrtype = null;
                        foreach (var kvpclr in map)
                        {
                            if (kvpclr.Key.ClrType != null && GetLuaType(kvpclr.Key.ClrType) == kvplua.Key.LuaType)
                            {
                                ++clrcnt;
                                clrtype = kvpclr.Key.ClrType;
                            }
                        }

                        if (clrcnt == 1)
                        {
                            luatypecomb.Add(kvplua.Key.LuaType, clrtype);
                        }
                    }
                }
                foreach (var kvp in luatypecomb)
                {
                    var list = map[kvp.Value];
                    map.Remove(kvp.Key);
                    map.Remove(kvp.Value);
                    var combKey = new TypeOrLuaType(kvp.Value, kvp.Key);
                    map[combKey] = list;
                }
            }
            private void GenerateByObjType()
            {
                var methods = GetOverloads();
                _ByObjType = new List<Dictionary<TypeOrLuaType, HashSet<MethodOverload>>>();
                int maxargsCnt = (from method in _Methods
                                  let pars = method.GetParameters()
                                  select pars == null ? 0 : pars.Length).Max() + 1;
                for (int i = 0; i < maxargsCnt; ++i)
                {
                    var map = new Dictionary<TypeOrLuaType, HashSet<MethodOverload>>();
                    _ByObjType.Add(map);

                    foreach (var method in methods)
                    {
                        if (i < method.ArgTypes.Count)
                        {
                            var pt = method.ArgTypes[i];
                            AddByObjType(i, pt, method);
                            AddByLuaType(i, pt, method);
                        }
                    }
                    AddObjTypeToLuaType(i);
                    CombineUniqueLuaType(i);
                }
            }

            public WriteMethodBodyContext Clone()
            {
                return new WriteMethodBodyContext()
                {
                    _sb = _sb,
                    Written_gettop = Written_gettop,
                    _Methods = _Methods,
                    _DoneMethods = new HashSet<MethodBase>(_DoneMethods),
                    _MethodEx = _MethodEx,
                    _Overloads = _Overloads,
                    _ByObjType = _ByObjType,
                };
            }
        }
        internal enum WriteMethodBodyParamsTreatment
        {
            Normal,
            Params,
        }
        internal static void WriteMethodBody(this WriteMethodBodyContext context)
        {
            var sb = context.sb;
            var methods = context.Methods;
            if (methods != null && methods.Count > 0)
            {
                //context.Write_gettop();
                sb.AppendLine("try");
                sb.AppendLine("{");
                if (methods.Count == 1)
                {
                    var method = methods[0];
                    var exinfo = context.GetMethodEx(method);

                    if (exinfo.LastArgIsParam)
                    {
                        sb.Append("var ltype = l.GetType(");
                        sb.Append(exinfo.ArgTypes.Count);
                        sb.AppendLine(");");
                        sb.Append("if (ltype == typeof(");
                        sb.WriteType(exinfo.ArgTypes.Last());
                        sb.AppendLine("))");
                        sb.AppendLine("{");
                        WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Normal);
                        sb.AppendLine("}");
                        sb.AppendLine("else");
                        sb.AppendLine("{");
                        WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Params);
                        sb.AppendLine("}");
                    }
                    else
                    {
                        WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Normal);
                    }
                }
                else
                {
                    sb.AppendLine("{");

                    sb.AppendLine("{");
                    WriteMethodBody_10_ByArgCnt(context);
                    sb.AppendLine("}");
                    sb.AppendLine("goto Label_default;");

                    for (int i = 0; i < methods.Count; ++i)
                    {
                        var method = methods[i];
                        var exinfo = context.GetMethodEx(method);

                        if (exinfo.LastArgIsParam)
                        {
                            sb.Append("Label_");
                            sb.Append(exinfo.Label);
                            sb.AppendLine(":");
                            sb.AppendLine("{");
                            sb.Append("var ltype = l.GetType(");
                            sb.Append(exinfo.ArgTypes.Count);
                            sb.AppendLine(");");
                            sb.Append("if (ltype == typeof(");
                            sb.WriteType(exinfo.ArgTypes.Last());
                            sb.Append(")) goto Label_");
                            sb.Append(exinfo.Label + 1);
                            sb.AppendLine(";");
                            sb.Append("else goto Label_");
                            sb.Append(exinfo.Label + 2);
                            sb.AppendLine(";");
                            sb.AppendLine("}");
                            sb.AppendLine("goto Label_default;");

                            sb.Append("Label_");
                            sb.Append(exinfo.Label + 1);
                            sb.AppendLine(":");
                            sb.AppendLine("{");
                            WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Normal);
                            sb.AppendLine("}");
                            sb.AppendLine("goto Label_default;");

                            sb.Append("Label_");
                            sb.Append(exinfo.Label + 2);
                            sb.AppendLine(":");
                            sb.AppendLine("{");
                            WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Params);
                            sb.AppendLine("}");
                            sb.AppendLine("goto Label_default;");

                        }
                        else
                        {
                            sb.Append("Label_");
                            sb.Append(exinfo.Label);
                            sb.AppendLine(":");
                            sb.AppendLine("{");
                            WriteMethodBody_0_Single(context, method, WriteMethodBodyParamsTreatment.Normal);
                            sb.AppendLine("}");
                            sb.AppendLine("goto Label_default;");
                        }
                    }

                    sb.AppendLine("Label_default:");
                    sb.AppendLine("{");
                    sb.AppendLine("}");

                    sb.AppendLine("}");
                }
                sb.AppendLine("}");
                sb.AppendLine("catch (Exception exception)");
                sb.AppendLine("{");
                sb.AppendLine("l.LogError(exception);");
                //sb.AppendLine("l.settop(oldtop);");
                sb.AppendLine("return 0;");
                sb.AppendLine("}");
            }
            //else
            {
                sb.Append("return 0;");
            }
        }
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        internal static int WriteMethodBody_Return_GetTupleLength(Type type)
        {
            if (!LuaLib.LuaTupleUtils.IsValueTuple(type) && !LuaLib.LuaTupleUtils.IsTuple(type))
            {
                return 1;
            }
            if (!type.IsGenericType)
            {
                return 0;
            }
            var gargs = type.GetGenericArguments();
            int gcnt = gargs.Length;
            if (gcnt == 8)
            {
                gcnt = 7 + WriteMethodBody_Return_GetTupleLength(gargs[7]);
            }
            return gcnt;
        }
        internal static int WriteMethodBody_Return(System.Text.StringBuilder sb, MethodInfo minfo)
        {
            if (minfo.IsSpecialName)
            {
                sb.AppendLine("l.PushLua(rv);");
                return 1;
            }
            else
            {
                if (LuaLib.LuaTupleUtils.IsValueTuple(minfo.ReturnType) || LuaLib.LuaTupleUtils.IsTuple(minfo.ReturnType))
                {
                    int gcnt = WriteMethodBody_Return_GetTupleLength(minfo.ReturnType);
                    for (int g = 0; g < gcnt; ++g)
                    {
                        sb.Append("l.PushLua(rv.Item");
                        sb.Append(g + 1);
                        sb.Append(");");
                        sb.AppendLine();
                    }
                    return gcnt;
                }
                else
                {
                    sb.AppendLine("l.PushLua(rv);");
                    return 1;
                }
            }
        }
#else
        internal static int WriteMethodBody_Return(System.Text.StringBuilder sb, MethodInfo minfo)
        {
            sb.AppendLine("l.PushLua(rv);");
            return 1;
        }
#endif
        internal static void WriteMethodBody_0_Single(this WriteMethodBodyContext context, MethodBase method, WriteMethodBodyParamsTreatment treatParams)
        {
            bool isObsolete = false;
            bool isObsoleteError = false;
            var oattrs = method.GetCustomAttributes(typeof(ObsoleteAttribute));
            if (oattrs != null)
            {
                foreach (var oattr in oattrs)
                {
                    var realattr = oattr as ObsoleteAttribute;
                    if (realattr != null)
                    {
                        isObsolete = true;
                        if (realattr.IsError)
                        {
                            isObsoleteError = true;
                            break;
                        }
                    }
                }
            }

            var sb = context.sb;
            
            if (isObsolete)
            {
                if (isObsoleteError)
                {
                    PlatDependant.LogError(method.ToString() + " is Obsoleted.");
                    sb.Append("throw new System.NotSupportedException(\"");
                    sb.Append(method.ToString());
                    sb.AppendLine(" is Obsoleted.\");");
                    return;
                }
                else
                {
                    PlatDependant.LogWarning(method.ToString() + " is Obsoleted.");
                }
            }
            
            var exinfo = context.GetMethodEx(method);
            if (method is ConstructorInfo)
            {
                if (method.DeclaringType.IsSubclassOf(typeof(ConstructorWrapper)))
                {
                    var type = method.DeclaringType.GetGenericArguments()[0];
                    sb.Append("var rv = default(");
                    sb.WriteType(type);
                    sb.AppendLine(");");
                    sb.AppendLine("l.PushLua(rv);");
                    sb.AppendLine("return 1;");
                }
                else
                {
                    var real = method as System.Reflection.ConstructorInfo;
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        var arg = exinfo.ArgTypes[i];
                        sb.WriteType(arg);
                        sb.Append(" p");
                        sb.Append(i);
                        sb.AppendLine(";");
                        if (i == exinfo.ArgTypes.Count - 1 && treatParams == WriteMethodBodyParamsTreatment.Params)
                        {
                            var paramstype = arg.GetElementType();
                            sb.Append("var paramscnt = l.gettop() - ");
                            sb.Append(i);
                            sb.AppendLine(";");
                            sb.AppendLine("paramscnt = paramscnt < 0 ? 0 : paramscnt;");
                            sb.Append("p");
                            sb.Append(i);
                            sb.Append(" = new ");
                            sb.WriteType(paramstype);
                            sb.AppendLine("[paramscnt];");
                            sb.AppendLine("for (int i = 0; i < paramscnt; ++i)");
                            sb.AppendLine("{");
                            sb.WriteType(paramstype);
                            sb.AppendLine(" paramval;");
                            sb.Append("l.GetLua(i + ");
                            sb.Append(exinfo.ArgTypes.Count);
                            sb.AppendLine(", out paramval);");
                            sb.Append("p");
                            sb.Append(i);
                            sb.AppendLine("[i] = paramval;");
                            sb.AppendLine("}");
                        }
                        else
                        {
                            sb.Append("l.GetLua(");
                            sb.Append(i + 1);
                            sb.Append(", out p");
                            sb.Append(i);
                            sb.AppendLine(");");
                        }
                    }
                    // if return != void
                    {
                        sb.Append("var rv = new ");
                        sb.WriteType(method.DeclaringType);
                        sb.Append("(");
                        for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                        {
                            if (i > 1)
                            {
                                sb.Append(", ");
                            }
                            var reforout = exinfo.GetArgByRefModifier(i);
                            if (!string.IsNullOrEmpty(reforout))
                            {
                                sb.Append(reforout);
                                sb.Append(" ");
                            }
                            sb.Append("p");
                            sb.Append(i);
                        }
                        sb.AppendLine(");");
                        sb.AppendLine("l.PushLua(rv);");
                        int rvcnt = 1;
                        for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                        {
                            if (exinfo.IsArgRefOrOut(i))
                            {
                                sb.Append("l.PushLua(p");
                                sb.Append(i);
                                sb.AppendLine(");");
                                ++rvcnt;
                            }
                        }
                        sb.Append("return ");
                        sb.Append(rvcnt);
                        sb.AppendLine(";");
                    }
                }
            }
            else if (exinfo.Static)
            {
                var real = method as System.Reflection.MethodInfo;
                for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                {
                    var arg = exinfo.ArgTypes[i];
                    sb.WriteType(arg);
                    sb.Append(" p");
                    sb.Append(i);
                    sb.AppendLine(";");
                    if (i == exinfo.ArgTypes.Count - 1 && treatParams == WriteMethodBodyParamsTreatment.Params)
                    {
                        var paramstype = arg.GetElementType();
                        sb.Append("var paramscnt = l.gettop() - ");
                        sb.Append(i);
                        sb.AppendLine(";");
                        sb.AppendLine("paramscnt = paramscnt < 0 ? 0 : paramscnt;");
                        sb.Append("p");
                        sb.Append(i);
                        sb.Append(" = new ");
                        sb.WriteType(paramstype);
                        sb.AppendLine("[paramscnt];");
                        sb.AppendLine("for (int i = 0; i < paramscnt; ++i)");
                        sb.AppendLine("{");
                        sb.WriteType(paramstype);
                        sb.AppendLine(" paramval;");
                        sb.Append("l.GetLua(i + ");
                        sb.Append(exinfo.ArgTypes.Count);
                        sb.AppendLine(", out paramval);");
                        sb.Append("p");
                        sb.Append(i);
                        sb.AppendLine("[i] = paramval;");
                        sb.AppendLine("}");
                    }
                    else
                    {
                        sb.Append("l.GetLua(");
                        sb.Append(i + 1);
                        sb.Append(", out p");
                        sb.Append(i);
                        sb.AppendLine(");");
                    }
                }
                if (real.ReturnType == typeof(void))
                {
                    if (real.IsSpecialName)
                    {
                        if (method.Name.StartsWith("set_"))
                        {
                            sb.WriteType(method.DeclaringType);
                            sb.Append(".");
                            sb.Append(method.Name.Substring("set_".Length));
                            sb.Append(" = p0;");
                            sb.AppendLine();
                        }
                        else if (method.Name.StartsWith("add_"))
                        {
                            sb.WriteType(method.DeclaringType);
                            sb.Append(".");
                            sb.Append(method.Name.Substring("add_".Length));
                            sb.Append(" += p0;");
                            sb.AppendLine();
                        }
                        else if (method.Name.StartsWith("remove_"))
                        {
                            sb.WriteType(method.DeclaringType);
                            sb.Append(".");
                            sb.Append(method.Name.Substring("remove_".Length));
                            sb.Append(" -= p0;");
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        sb.WriteType(method.DeclaringType);
                        sb.Append(".");
                        sb.Append(method.Name);
                        sb.Append("(");
                        for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }
                            var reforout = exinfo.GetArgByRefModifier(i);
                            if (!string.IsNullOrEmpty(reforout))
                            {
                                sb.Append(reforout);
                                sb.Append(" ");
                            }
                            sb.Append("p");
                            sb.Append(i);
                        }
                        sb.AppendLine(");");
                        int rvcnt = 0;
                        for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                        {
                            if (exinfo.IsArgRefOrOut(i))
                            {
                                if (rvcnt == 0)
                                {
                                    sb.AppendLine("l.pushnil();");
                                    ++rvcnt;
                                }
                                sb.Append("l.PushLua(p");
                                sb.Append(i);
                                sb.AppendLine(");");
                                ++rvcnt;
                            }
                        }
                        sb.Append("return ");
                        sb.Append(rvcnt);
                        sb.AppendLine(";");
                    }
                }
                else
                {
                    if (binOps.ContainsKey(method.Name))
                    {
                        var op = binOps[method.Name];
                        sb.Append("var rv = p0 ");
                        sb.Append(op);
                        sb.AppendLine(" p1;");
                    }
                    else if (unmOps.ContainsKey(method.Name))
                    {
                        var op = unmOps[method.Name];
                        sb.Append("var rv = ");
                        sb.Append(op);
                        sb.AppendLine("p0;");
                    }
                    else if (convertOps.Contains(method.Name))
                    {
                        sb.Append("var rv = (");
                        sb.WriteType(((System.Reflection.MethodInfo)method).ReturnType);
                        sb.AppendLine(")p0;");
                    }
                    else if (real.IsSpecialName)
                    {
                        if (method.Name.StartsWith("get_"))
                        {
                            sb.Append("var rv = ");
                            sb.WriteType(method.DeclaringType);
                            sb.Append(".");
                            sb.Append(method.Name.Substring("get_".Length));
                            sb.Append(";");
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        sb.Append("var rv = ");
                        sb.WriteType(method.DeclaringType);
                        sb.Append(".");
                        sb.Append(method.Name);
                        sb.Append("(");
                        for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }
                            var reforout = exinfo.GetArgByRefModifier(i);
                            if (!string.IsNullOrEmpty(reforout))
                            {
                                sb.Append(reforout);
                                sb.Append(" ");
                            }
                            sb.Append("p");
                            sb.Append(i);
                        }
                        sb.AppendLine(");");
                    }
                    int rvcnt = WriteMethodBody_Return(sb, real);
                    for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (exinfo.IsArgRefOrOut(i))
                        {
                            sb.Append("l.PushLua(p");
                            sb.Append(i);
                            sb.AppendLine(");");
                            ++rvcnt;
                        }
                    }
                    sb.Append("return ");
                    sb.Append(rvcnt);
                    sb.AppendLine(";");
                }
            }
            else
            {
                bool isgetter = false;
                bool issetter = false;
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                {
                    var pName = method.Name.Substring(4);
                    PropertyInfo pi = null;
                    bool isIndex = false;
                    try
                    {
                        pi = method.ReflectedType.GetProperty(pName);
                    }
                    catch (System.Reflection.AmbiguousMatchException)
                    {
                        isIndex = true;
                    }
                    if (pi != null || isIndex)
                    {
                        var attrs = method.ReflectedType.GetCustomAttributes(typeof(DefaultMemberAttribute), false);
                        if (attrs != null && attrs.Length > 0)
                        {
                            var attr = attrs[0] as DefaultMemberAttribute;
                            if (attr.MemberName == pName)
                            {
                                if (method.Name.StartsWith("get_"))
                                {
                                    isgetter = true;
                                }
                                else
                                {
                                    issetter = true;
                                }
                            }
                        }
                    }
                }
                var real = method as System.Reflection.MethodInfo;
                for (int i = 0; i < exinfo.ArgTypes.Count; ++i)
                {
                    var arg = exinfo.ArgTypes[i];
                    sb.WriteType(arg);
                    sb.Append(" p");
                    sb.Append(i);
                    sb.AppendLine(";");
                    if (i == exinfo.ArgTypes.Count - 1 && treatParams == WriteMethodBodyParamsTreatment.Params)
                    {
                        var paramstype = arg.GetElementType();
                        sb.Append("var paramscnt = l.gettop() - ");
                        sb.Append(i);
                        sb.AppendLine(";");
                        sb.AppendLine("paramscnt = paramscnt < 0 ? 0 : paramscnt;");
                        sb.Append("p");
                        sb.Append(i);
                        sb.Append(" = new ");
                        sb.WriteType(paramstype);
                        sb.AppendLine("[paramscnt];");
                        sb.AppendLine("for (int i = 0; i < paramscnt; ++i)");
                        sb.AppendLine("{");
                        sb.WriteType(paramstype);
                        sb.AppendLine(" paramval;");
                        sb.Append("l.GetLua(i + ");
                        sb.Append(exinfo.ArgTypes.Count);
                        sb.AppendLine(", out paramval);");
                        sb.Append("p");
                        sb.Append(i);
                        sb.AppendLine("[i] = paramval;");
                        sb.AppendLine("}");
                    }
                    else
                    {
                        sb.Append("l.GetLua(");
                        sb.Append(i + 1);
                        sb.Append(", out p");
                        sb.Append(i);
                        sb.AppendLine(");");
                    }
                }
                if (isgetter)
                {
                    sb.Append("var rv = p0[");
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (i > 1)
                        {
                            sb.Append(", ");
                        }
                        sb.Append("p");
                        sb.Append(i);
                    }
                    sb.AppendLine("];");
                    sb.AppendLine("l.PushLua(rv);");
                    sb.AppendLine("return 1;");
                }
                else if (issetter)
                {
                    sb.Append("p0[");
                    for (int i = 1; i < exinfo.ArgTypes.Count - 1; ++i)
                    {
                        if (i > 1)
                        {
                            sb.Append(", ");
                        }
                        sb.Append("p");
                        sb.Append(i);
                    }
                    sb.Append("] = p");
                    sb.Append(exinfo.ArgTypes.Count - 1);
                    sb.AppendLine(";");
                    if (method.ReflectedType.IsValueType)
                    {
                        sb.AppendLine("SetDataRaw(l, 1, p0);");
                    }
                    sb.AppendLine("return 0;");
                }
                else if (real.IsSpecialName)
                {
                    if (method.Name.StartsWith("set_"))
                    {
                        sb.Append("p0.");
                        sb.Append(method.Name.Substring("set_".Length));
                        sb.Append(" = p1;");
                        sb.AppendLine();
                        if (method.ReflectedType.IsValueType)
                        {
                            sb.AppendLine("SetDataRaw(l, 1, p0);");
                        }
                        sb.AppendLine("return 0;");
                    }
                    if (method.Name.StartsWith("get_"))
                    {
                        sb.Append("var rv = p0.");
                        sb.Append(method.Name.Substring("get_".Length));
                        sb.Append(";");
                        sb.AppendLine();
                        sb.AppendLine("l.PushLua(rv);");
                        sb.AppendLine("return 1;");
                    }
                    else if (method.Name.StartsWith("add_"))
                    {
                        sb.Append("p0.");
                        sb.Append(method.Name.Substring("add_".Length));
                        sb.Append(" += p1;");
                        sb.AppendLine();
                        if (method.ReflectedType.IsValueType)
                        {
                            sb.AppendLine("SetDataRaw(l, 1, p0);");
                        }
                        sb.AppendLine("return 0;");
                    }
                    else if (method.Name.StartsWith("remove_"))
                    {
                        sb.Append("p0.");
                        sb.Append(method.Name.Substring("remove_".Length));
                        sb.Append(" -= p1;");
                        sb.AppendLine();
                        if (method.ReflectedType.IsValueType)
                        {
                            sb.AppendLine("SetDataRaw(l, 1, p0);");
                        }
                        sb.AppendLine("return 0;");
                    }
                }
                else if (real.ReturnType == typeof(void))
                {
                    sb.Append("p0.");
                    sb.Append(method.Name);
                    sb.Append("(");
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (i > 1)
                        {
                            sb.Append(", ");
                        }
                        var reforout = exinfo.GetArgByRefModifier(i);
                        if (!string.IsNullOrEmpty(reforout))
                        {
                            sb.Append(reforout);
                            sb.Append(" ");
                        }
                        sb.Append("p");
                        sb.Append(i);
                    }
                    sb.AppendLine(");");
                    int rvcnt = 0;
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (exinfo.IsArgRefOrOut(i))
                        {
                            if (rvcnt == 0)
                            {
                                sb.AppendLine("l.pushnil();");
                                ++rvcnt;
                            }
                            sb.Append("l.PushLua(p");
                            sb.Append(i);
                            sb.AppendLine(");");
                            ++rvcnt;
                        }
                    }
                    if (method.ReflectedType.IsValueType)
                    {
                        sb.AppendLine("SetDataRaw(l, 1, p0);"); // the func has no return value, so it may change its innner fields, so we should sync value back to lua.
                    }
                    sb.Append("return ");
                    sb.Append(rvcnt);
                    sb.AppendLine(";");
                }
                else
                {
                    sb.Append("var rv = p0.");
                    sb.Append(method.Name);
                    sb.Append("(");
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (i > 1)
                        {
                            sb.Append(", ");
                        }
                        var reforout = exinfo.GetArgByRefModifier(i);
                        if (!string.IsNullOrEmpty(reforout))
                        {
                            sb.Append(reforout);
                            sb.Append(" ");
                        }
                        sb.Append("p");
                        sb.Append(i);
                    }
                    sb.AppendLine(");");
                    int rvcnt = WriteMethodBody_Return(sb, real);
                    for (int i = 1; i < exinfo.ArgTypes.Count; ++i)
                    {
                        if (exinfo.IsArgRefOrOut(i))
                        {
                            sb.Append("l.PushLua(p");
                            sb.Append(i);
                            sb.AppendLine(");");
                            ++rvcnt;
                        }
                    }
                    if (method.ReflectedType.IsValueType)
                    {
                        sb.AppendLine("//SetDataRaw(l, 1, p0);"); // the func returns something, so it is supposed not to change its innner fields, so we do not need sync value back to lua.
                    }
                    sb.Append("return ");
                    sb.Append(rvcnt);
                    sb.AppendLine(";");
                }
            }
        }
        internal static bool IsValueTypeNonNullable(this Type t)
        {
            return t.IsValueType && Nullable.GetUnderlyingType(t) == null;
        }
        internal static void WriteMethodBody_10_ByArgCnt_Single(this WriteMethodBodyContext context, ref bool shouldelse, int cnt, MethodBase method)
        {
            var sb = context.sb;
            if (shouldelse)
            {
                sb.Append("else ");
            }
            else
            {
                shouldelse = true;
                context.Write_gettop();
            }
            sb.Append("if (oldtop == ");
            sb.Append(cnt);
            sb.Append(")");
            sb.AppendLine(); // if (oldtop == 8)

            sb.AppendLine("{"); // {
            sb.Append("goto Label_");
            var pexinfo = context.GetMethodEx(method);
            var label = pexinfo.Label;
            if (pexinfo.LastArgIsParam)
            {
                if (pexinfo.ArgTypes.Count != cnt)
                {
                    label += 2;
                }
            }
            sb.Append(label);
            sb.AppendLine(";"); // goto Label_82;
            sb.AppendLine("}"); // }
            if (pexinfo.ArgTypes.Count >= cnt)
            {
                if (cnt <= 0 || pexinfo.ArgTypes[cnt - 1].IsValueTypeNonNullable())
                {
                    context.DoneMethods.Add(method);
                }
            }
        }
        internal static void WriteMethodBody_10_ByArgCnt_Range(this WriteMethodBodyContext context, ref bool shouldelse, int from, int to, MethodBase method)
        {
            if (from == to)
            {
                WriteMethodBody_10_ByArgCnt_Single(context, ref shouldelse, from, method);
                return;
            }

            var sb = context.sb;
            if (shouldelse)
            {
                sb.Append("else ");
            }
            else
            {
                shouldelse = true;
                context.Write_gettop();
            }
            var pexinfo = context.GetMethodEx(method);
            var label = pexinfo.Label;
            if (pexinfo.ArgTypes.Count >= from && pexinfo.ArgTypes.Count <= to)
            {
                sb.Append("if (oldtop == ");
                sb.Append(pexinfo.ArgTypes.Count);
                sb.Append(")");
                sb.AppendLine();
                sb.AppendLine("{");
                sb.Append("goto Label_");
                sb.Append(label);
                sb.AppendLine(";");
                sb.AppendLine("}");
                sb.Append("else ");
                if (to - from == 1)
                {
                    sb.Append("if (oldtop == ");
                    sb.Append(from == pexinfo.ArgTypes.Count ? to : from);
                    sb.Append(")");
                    sb.AppendLine();
                    sb.AppendLine("{");
                    sb.Append("goto Label_");
                    sb.Append(label + 2);
                    sb.AppendLine(";");
                    sb.AppendLine("}");
                    return;
                }
            }

            sb.Append("if (oldtop >= ");
            sb.Append(from);
            if (to >= 0 && to < int.MaxValue)
            {
                sb.Append(" && oldtop <= ");
                sb.Append(to);
            }
            sb.Append(")");
            sb.AppendLine();
            sb.AppendLine("{");
            sb.Append("goto Label_");
            if (pexinfo.LastArgIsParam)
            {
                label += 2;
            }
            sb.Append(label);
            sb.AppendLine(";");
            sb.AppendLine("}");
        }
        internal static void WriteMethodBody_10_ByArgCnt_Branch(this WriteMethodBodyContext context, ref bool shouldelse, int cnt, IList<MethodBase> methods)
        {
            var sb = context.sb;
            if (shouldelse)
            {
                sb.Append("else ");
            }
            else
            {
                shouldelse = true;
                context.Write_gettop();
            }
            sb.Append("if (oldtop == ");
            sb.Append(cnt);
            sb.Append(")");
            sb.AppendLine(); // if (oldtop == 8)

            sb.AppendLine("{"); // {
            WriteMethodBodyContext subcontext = context.Clone();
            subcontext.DoneMethods.UnionWith(subcontext.Methods);
            subcontext.DoneMethods.ExceptWith(methods);
            WriteMethodBody_30_ByObjType(subcontext, cnt); // TODO: test and fix WriteMethodBody_15_ByArgCntAndParamType && WriteMethodBody_20_ByLuaType
            sb.AppendLine("}"); // }
            foreach (var method in methods)
            {
                var pexinfo = context.GetMethodEx(method);
                if (pexinfo.ArgTypes.Count == cnt && (cnt <= 0 || pexinfo.ArgTypes[cnt - 1].IsValueTypeNonNullable()))
                {
                    context.DoneMethods.Add(method);
                }
            }
        }
        internal static void WriteMethodBody_10_ByArgCnt(this WriteMethodBodyContext context)
        {
            var sb = context.sb;
            var methods = context.GetUndoneMethods();
            SortedDictionary<int, IList<MethodBase>> bycnt_valuetype = new SortedDictionary<int, IList<MethodBase>>();
            SortedDictionary<int, IList<MethodBase>> bycnt_nullable = new SortedDictionary<int, IList<MethodBase>>();

            var paramposes = from method in methods
                             let exinfo = context.GetMethodEx(method)
                             where exinfo.LastArgIsParam
                             select exinfo.ArgTypes.Count - 1;
            int minparampos = paramposes.Count() == 0 ? int.MaxValue : paramposes.Min();
            var vtmethods = from method in methods
                            let exinfo = context.GetMethodEx(method)
                            let types = exinfo.ArgTypes
                            let argcnt = types.Count
                            where !exinfo.LastArgIsParam
                            where argcnt == 0 || types[argcnt - 1].IsValueTypeNonNullable()
                            select method;

            if (vtmethods.Count() == 0)
            {
                minparampos = 0;
            }

            foreach (var method in methods)
            {
                var exinfo = context.GetMethodEx(method);
                var types = exinfo.ArgTypes;
                var argcnt = types.Count;
                if (!exinfo.LastArgIsParam && (argcnt == 0 || types[argcnt - 1].IsValueTypeNonNullable() || argcnt >= minparampos))
                {
                    IList<MethodBase> list;
                    if (!bycnt_valuetype.TryGetValue(argcnt, out list))
                    {
                        list = new List<MethodBase>();
                        bycnt_valuetype[argcnt] = list;
                    }
                    list.Add(method);
                }
            }

            int maxcnt = bycnt_valuetype.Count == 0 ? 0 : bycnt_valuetype.Keys.Last();
            foreach (var method in methods)
            {
                var exinfo = context.GetMethodEx(method);
                var types = exinfo.ArgTypes;
                var argcnt = types.Count;
                if (exinfo.LastArgIsParam || argcnt > 0 && !types[argcnt - 1].IsValueTypeNonNullable() && argcnt < minparampos)
                {
                    int max_non_null = -1;
                    for (int i = argcnt - 2; i >= 0; --i)
                    {
                        var argtype = types[i];
                        if (argtype.IsValueTypeNonNullable() || i >= minparampos)
                        {
                            max_non_null = i;
                            break;
                        }
                    }
                    ++max_non_null;

                    for (int vcnt = max_non_null; vcnt <= maxcnt; ++vcnt)
                    {
                        if (bycnt_valuetype.ContainsKey(vcnt))
                        {
                            if (vcnt > argcnt && !exinfo.LastArgIsParam)
                            {
                                break;
                            }
                            if (!(vcnt == argcnt && exinfo.LastArgIsParam))
                            {
                                Types vtypes = new Types();
                                for (int i = 0; i < vcnt; ++i)
                                {
                                    Type argType;
                                    if (i < argcnt - 1 || !exinfo.LastArgIsParam)
                                    {
                                        argType = types[i];
                                    }
                                    else
                                    {
                                        argType = types[argcnt - 1].GetElementType();
                                    }
                                    vtypes.Add(argType);
                                }
                                if (bycnt_valuetype[vcnt].Any(vtmethod =>
                                {
                                    var vtex = context.GetMethodEx(vtmethod);
                                    var vtargs = vtex.ArgTypes;
                                    return IsExplicitCall(vtypes, vtargs);
                                }))
                                {
                                    continue;
                                }
                            }

                            IList<MethodBase> list;
                            if (!bycnt_nullable.TryGetValue(vcnt, out list))
                            {
                                list = new List<MethodBase>();
                                bycnt_nullable[vcnt] = list;
                            }
                            list.Add(method);
                        }
                    }
                }
            }

            if (bycnt_valuetype.Count <= 1)
            { // all overloads have same arg_count
                WriteMethodBody_30_ByObjType(context);
            }
            //else if (bycnt_valuetype.All(kvp => kvp.Value.Count == methods.Count))
            //{ // all overloads have same arg_count
            //    WriteMethodBody_30_ByObjType(context);
            //}
            else
            {
                List<int> selectedcounts = new List<int>();
                foreach (var kvp in bycnt_valuetype)
                {
                    int argcnt = kvp.Key;
                    //IList<MethodBase> nullables = null;
                    //bycnt_nullable.TryGetValue(argcnt, out nullables);
                    //if (nullables == null || kvp.Value.Count >= nullables.Count)
                    {
                        selectedcounts.Add(argcnt);
                    }
                }
                selectedcounts.Sort((k1, k2) =>
                {
                    var vcnt1 = bycnt_valuetype[k1].Count;
                    var vcnt2 = bycnt_valuetype[k2].Count;
                    if (vcnt1 == vcnt2)
                    {
                        int ncnt1 = 0;
                        int ncnt2 = 0;
                        IList<MethodBase> nms1;
                        IList<MethodBase> nms2;
                        if (bycnt_nullable.TryGetValue(k1, out nms1))
                        {
                            ncnt1 = nms1.Count;
                        }
                        if (bycnt_nullable.TryGetValue(k2, out nms2))
                        {
                            ncnt2 = nms2.Count;
                        }
                        return ncnt1 - ncnt2;
                    }
                    return vcnt1 - vcnt2;
                });

                bool shouldelse = false;
                foreach (var argcnt in selectedcounts)
                {
                    List<MethodBase> branchMethods = new List<MethodBase>(bycnt_valuetype[argcnt]);
                    IList<MethodBase> nullables = null;
                    if (bycnt_nullable.TryGetValue(argcnt, out nullables))
                    {
                        branchMethods.AddRange(nullables);
                    }

                    if (branchMethods.Count == 1)
                    {
                        WriteMethodBody_10_ByArgCnt_Single(context, ref shouldelse, argcnt, branchMethods[0]);
                    }
                    else
                    {
                        WriteMethodBody_10_ByArgCnt_Branch(context, ref shouldelse, argcnt, branchMethods);
                    }
                    foreach (var method in bycnt_valuetype[argcnt])
                    {
                        context.DoneMethods.Add(method);
                    }
                }

                if (context.Methods.Count > context.DoneMethods.Count)
                {
                    if (shouldelse)
                    {
                        sb.AppendLine("else");
                    }
                    {
                        sb.AppendLine("{");
                        if (context.Methods.Count - context.DoneMethods.Count == 1)
                        {
                            var rest = context.GetUndoneMethods().First();
                            sb.Append("goto Label_");
                            sb.Append(context.GetMethodEx(rest).Label);
                            sb.AppendLine(";");
                        }
                        else
                        {
                            //WriteMethodBody_15_ByArgCntAndParamType(context);
                            WriteMethodBody_30_ByObjType(context, -1, new HashSet<int>(selectedcounts)); // TODO: test and fix WriteMethodBody_15_ByArgCntAndParamType && WriteMethodBody_20_ByLuaType
                        }
                        sb.AppendLine("}");
                    }
                }
            }
        }
        internal static void WriteMethodBody_15_ByArgCntAndParamType(this WriteMethodBodyContext context)
        { // TODO: this is usually unused. may have bugs here. Remove this step or debug carefully.
            var sb = context.sb;
            var methods = context.GetUndoneMethods();
            SortedDictionary<int, IList<MethodBase>> byfixed = new SortedDictionary<int, IList<MethodBase>>();
            SortedDictionary<int, IList<MethodBase>> unfixed = new SortedDictionary<int, IList<MethodBase>>();
            Dictionary<MethodBase, int> paramspos = new Dictionary<MethodBase, int>();

            foreach (var method in methods)
            {
                var exinfo = context.GetMethodEx(method);
                var pars = exinfo.ArgTypes;
                var argcnt = pars.Count;
                if (exinfo.LastArgIsParam)
                {
                    IList<MethodBase> list;
                    if (!unfixed.TryGetValue(argcnt, out list))
                    {
                        list = new List<MethodBase>();
                        unfixed[argcnt] = list;
                    }
                    list.Add(method);
                    paramspos[method] = argcnt;
                    --argcnt;
                }
                else
                {
                    IList<MethodBase> list;
                    if (!byfixed.TryGetValue(argcnt, out list))
                    {
                        list = new List<MethodBase>();
                        byfixed[argcnt] = list;
                    }
                    list.Add(method);
                }
            }

            bool shouldelse = false;
            foreach (var kvpfixed in byfixed)
            {
                if (kvpfixed.Value.Count == 1)
                {
                    var fmethod = kvpfixed.Value[0];
                    var umethods = from kvpumethod in paramspos
                                   where kvpumethod.Value - 1 <= kvpfixed.Key
                                   select kvpumethod.Key;

                    bool ismatch = true;
                    var fpars = fmethod.GetParameters();
                    for (int i = 0; i < kvpfixed.Key && i < fpars.Length; ++i)
                    {
                        var fpar = fpars[i].ParameterType;
                        foreach (var umethod in umethods)
                        {
                            var umcnt = paramspos[umethod];
                            if (umcnt <= i)
                            {
                                continue;
                            }
                            else
                            {
                                var upar = umethod.GetParameters()[i].ParameterType;
                                if (i == umcnt - 1)
                                {
                                    upar = upar.GetElementType();
                                }

                                if (upar != fpar)
                                {
                                    ismatch = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (ismatch)
                    {
                        if (shouldelse)
                        {
                            sb.Append("else ");
                        }
                        else
                        {
                            shouldelse = true;
                        }
                        sb.Append("if (oldtop == ");
                        sb.Append(kvpfixed.Key);
                        sb.Append(")");
                        sb.AppendLine();
                        sb.AppendLine("{");
                        sb.Append("goto Label_");
                        sb.Append(context.GetMethodEx(fmethod).Label);
                        sb.AppendLine(";");
                        context.DoneMethods.Add(fmethod);
                        sb.AppendLine("}");
                    }
                }
            }

            if (context.Methods.Count > context.DoneMethods.Count)
            {
                if (shouldelse)
                {
                    sb.AppendLine("else");
                }
                {
                    sb.AppendLine("{");
                    if (context.Methods.Count - context.DoneMethods.Count == 1)
                    {
                        var rest = context.GetUndoneMethods().First();
                        sb.Append("goto Label_");
                        sb.Append(context.GetMethodEx(rest).Label);
                        sb.AppendLine(";");
                    }
                    else
                    {
                        WriteMethodBody_20_ByLuaType(context);
                    }
                    sb.AppendLine("}");
                }
            }
        }
        internal static void WriteMethodBody_20_ByLuaType(this WriteMethodBodyContext context)
        {
            var sb = context.sb;
            var methods = context.GetUndoneMethods();
            List<Dictionary<string, List<MethodBase>>> ByLuaType = new List<Dictionary<string, List<MethodBase>>>();
            int maxargsCnt = methods.Max(method => (method.GetParameters() ?? new ParameterInfo[0]).Length + 1);
            for (int i = 0; i < maxargsCnt; ++i)
            {
                ByLuaType.Add(new Dictionary<string, List<MethodBase>>());
            }
            foreach (var method in methods)
            {
                var exinfo = context.GetMethodEx(method);
                var pars = exinfo.ArgTypes;
                for (int i = 0; i < pars.Count; ++i)
                {
                    var parType = pars[i];
                    if (i == pars.Count - 1)
                    {
                        if (exinfo.LastArgIsParam)
                        {
                            parType = parType.GetElementType();
                        }
                    }
                    string LuaType;
                    if (TryGetLuaType(parType, out LuaType))
                    {
                        List<MethodBase> group;
                        if (!ByLuaType[i].TryGetValue(LuaType, out group))
                        {
                            group = new List<MethodBase>();
                            ByLuaType[i][LuaType] = group;
                        }
                        group.Add(method);
                    }
                }
            }

            int lastParIndex = -1;
            while (true)
            {
                int parIndex = -1;
                for (int i = 0; i < ByLuaType.Count; ++i)
                {
                    var map = ByLuaType[i];
                    var selected = from info in map
                                   where info.Value.Count == 1
                                   select new { LuaType = info.Key, Method = info.Value[0] };
                    if (selected.Count() > 0)
                    {
                        parIndex = i;
                        var fsel = selected.First();

                        // Here writes the block.
                        if (lastParIndex == parIndex)
                        {
                            sb.Append("else ");
                        }
                        else
                        {
                            sb.Append("var ___lt");
                            sb.Append(i);
                            sb.Append(" = l.type(");
                            sb.Append(i + 1);
                            sb.AppendLine(");");
                        }
                        lastParIndex = parIndex;
                        sb.Append("if (___lt");
                        sb.Append(i);
                        sb.Append(" == LuaCoreLib.");
                        sb.Append(fsel.LuaType);
                        sb.AppendLine(")");
                        sb.AppendLine("{");
                        var exinfo = context.GetMethodEx(fsel.Method);
                        int label = exinfo.Label;
                        if (exinfo.LastArgIsParam && i == exinfo.ArgTypes.Count - 1)
                        {
                            label += 2;
                        }
                        sb.Append("goto Label_");
                        sb.Append(label);
                        sb.AppendLine(";");
                        sb.AppendLine("}");

                        map.Remove(fsel.LuaType);
                        break;
                    }
                }
                if (parIndex < 0)
                {
                    break;
                }
            }
            sb.AppendLine("{");
            WriteMethodBody_30_ByObjType(context);
            sb.AppendLine("}");
        }
        internal static bool WriteMethodBody_30_ByObjType_HasProcessed(List<HashSet<TypeOrLuaType>> processed, int index, TypeOrLuaType type)
        {
            if (index >= processed.Count)
            {
                return true;
            }
            return processed[index].Contains(type);
        }
        internal static bool WriteMethodBody_30_ByObjType_HasProcessed(List<HashSet<TypeOrLuaType>> processed, int index)
        {
            if (index >= processed.Count)
            {
                return true;
            }
            return processed[index].Count > 0;
        }
        internal static void WriteMethodBody_30_ByObjType_MarkProcessed(List<HashSet<TypeOrLuaType>> processed, int index, TypeOrLuaType type)
        {
            if (index < processed.Count)
            {
                processed[index].Add(type);
            }
        }
        internal static List<HashSet<TypeOrLuaType>> WriteMethodBody_30_ByObjType_CopyProcessed(List<HashSet<TypeOrLuaType>> processed)
        {
            var newp = new List<HashSet<TypeOrLuaType>>();
            for (int i = 0; i < processed.Count; ++i)
            {
                newp.Add(new HashSet<TypeOrLuaType>(processed[i]));
            }
            return newp;
        }
        internal static void WriteMethodBody_30_ByObjType_WriteIndexAndType(this WriteMethodBodyContext context, List<HashSet<TypeOrLuaType>> processed, int index, TypeOrLuaType type)
        {
            var sb = context.sb;

            if (!WriteMethodBody_30_ByObjType_HasProcessed(processed, index))
            {
                sb.Append("int ___lt");
                sb.Append(index);
                sb.AppendLine(";");
                sb.Append("var ___ot");
                sb.Append(index);
                sb.Append(" = l.GetType(");
                sb.Append(index + 1);
                sb.Append(", out ___lt");
                sb.Append(index);
                sb.AppendLine(");");
            }
            if (type.ClrType != null)
            {
                sb.Append("if (");
                if (type.LuaType != null)
                {
                    sb.Append("___lt");
                    sb.Append(index);
                    sb.Append(" == lua.");
                    sb.Append(type.LuaType);
                    sb.Append(" || ");
                }
                else if (typeof(LuaLib.LuaState).IsAssignableFrom(type.ClrType))
                {
                    sb.Append("___lt");
                    sb.Append(index);
                    sb.Append(" == lua.LUA_TTHREAD || ");
                }
                else if (Types.IsLuaWrapper(type.ClrType))
                {
                    if (typeof(Delegate).IsAssignableFrom(type))
                    { // delegates
                        sb.Append("___lt");
                        sb.Append(index);
                        sb.Append(" == lua.LUA_TFUNCTION || ");
                    }
                    // LuaWrapper
                    sb.Append("___ot");
                    sb.Append(index);
                    sb.Append(" == typeof(LuaLib.LuaTable) || ");
                }
                if (type.ClrType.IsValueType || type.ClrType.IsSealed)
                {
                    if (Nullable.GetUnderlyingType(type.ClrType) != null)
                    { // this is nullable
                        var rtype = Nullable.GetUnderlyingType(type.ClrType);
                        sb.Append("___ot");
                        sb.Append(index);
                        sb.Append(" == typeof(");
                        sb.WriteType(rtype);
                        sb.Append(") || ");

                        sb.Append("___ot");
                        sb.Append(index);
                        sb.Append(" == null");
                    }
                    else
                    {
                        sb.Append("___ot");
                        sb.Append(index);
                        sb.Append(" == typeof(");
                        sb.WriteType(type);
                        sb.Append(")");
                    }
                }
                else
                {
                    if (type == typeof(object))
                    {
                        sb.Append("___ot");
                        sb.Append(index);
                        sb.Append(" != null");
                    }
                    else
                    {
                        sb.Append("typeof(");
                        sb.WriteType(type);
                        sb.Append(").IsAssignableFrom(___ot");
                        sb.Append(index);
                        sb.Append(")");
                    }
                }
                if (typeof(LuaLib.ILuaWrapper).IsAssignableFrom(type))
                {
                    sb.Append(" || typeof(LuaLib.ILuaWrapper).IsAssignableFrom(___ot");
                    sb.Append(index);
                    sb.Append(")");
                }
                sb.AppendLine(")");
            }
            else
            {
                sb.Append("if (___lt");
                sb.Append(index);
                sb.Append(" == lua.");
                sb.Append(type.LuaType);
                sb.AppendLine(")");
            }
        }
        //internal static void WriteMethodBody_30_ByObjType_WriteIndexAndType(this WriteMethodBodyContext context, List<HashSet<Type>> processed, int index, Type type, IEnumerable<Type> convertTypes)
        //{
        //    var sb = context.sb;

        //    if (!WriteMethodBody_30_ByObjType_HasProcessed(processed, index))
        //    {
        //        sb.Append("var ___ot");
        //        sb.Append(index);
        //        sb.Append(" = l.GetType(");
        //        sb.Append(index + 1);
        //        sb.AppendLine(");");
        //    }
        //    Type nativeType;
        //    if (type.IsValueType || type.IsEnum || type.IsSealed)
        //    {
        //        sb.Append("if (___ot");
        //        sb.Append(index);
        //        sb.Append(" == typeof(");
        //        if (nativeTypeMap.ContainsKey(type))
        //        {
        //            var ltype = nativeRevMap[nativeTypeMap[type]];
        //            sb.WriteType(ltype);
        //            nativeType = ltype;
        //        }
        //        else
        //        {
        //            sb.WriteType(type);
        //            nativeType = type;
        //        }
        //        sb.Append(")");
        //        var ctypes = from ctype in convertTypes
        //                     where ctype != nativeType
        //                     select ctype;
        //        if (ctypes.Count() == 1)
        //        {
        //            var singleType = ctypes.First();
        //            sb.Append(" || ___ot");
        //            sb.Append(index);
        //            sb.Append(" == typeof(");
        //            sb.WriteType(singleType);
        //            sb.Append(")");
        //        }
        //        sb.AppendLine(")");
        //    }
        //    else
        //    {
        //        if (type == typeof(object))
        //        {
        //            sb.Append("if (___ot");
        //            sb.Append(index);
        //            sb.Append(" != null)");
        //            sb.AppendLine();
        //        }
        //        else
        //        {
        //            sb.Append("if (___ot");
        //            sb.Append(index);
        //            sb.Append(" == typeof(");
        //            sb.WriteType(type);
        //            sb.Append(") || typeof(");
        //            sb.WriteType(type);
        //            sb.Append(").IsAssignableFrom(___ot");
        //            sb.Append(index);
        //            sb.AppendLine("))");
        //        }
        //    }
        //}
        internal static int WriteMethodBody_30_ByObjType_CompareArgs(MethodOverload m1, MethodOverload m2)
        {
            Types t1 = m1.ArgTypes;
            Types t2 = m2.ArgTypes;
            var rv = Types.Compare(t1, t2);
            if (rv != 0)
            {
                return rv;
            }

            var o1 = m1.LabelOffset;
            var o2 = m2.LabelOffset;
            return o2 - o1;
        }
        internal static int WriteMethodBody_30_ByObjType_CompareType(Type t1, Type t2)
        {
            return Types.Compare(new Types() { t1 }, new Types() { t2 });
        }
        internal static int WriteMethodBody_30_ByObjType_CompareType(TypeOrLuaType t1, TypeOrLuaType t2)
        {
            bool isLua1 = t1.ClrType == null;
            bool isLua2 = t2.ClrType == null;
            if (isLua1 && isLua2)
            {
                return string.Compare(t1.LuaType, t2.LuaType);
            }
            else if (!isLua1 && !isLua2)
            {
                return WriteMethodBody_30_ByObjType_CompareType(t1.ClrType, t2.ClrType);
            }
            else if (isLua1)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
        internal static void WriteMethodBody_30_ByObjType_WorkStep(this WriteMethodBodyContext context, HashSet<MethodOverload> submethods, List<HashSet<TypeOrLuaType>> processed)
        {
            var sb = context.sb;
            if (submethods.Count <= 0)
                return;
            else if (submethods.Count == 1)
            {
                var rest = submethods.First();
                sb.Append("goto Label_");
                sb.Append(context.GetMethodEx(rest).Label + rest.LabelOffset);
                sb.AppendLine(";");
                return;
            }
            else
            {
                var first = submethods.First();
                if (submethods.All(overload => overload.Method == first.Method && overload.LabelOffset == first.LabelOffset))
                {
                    sb.Append("goto Label_");
                    sb.Append(context.GetMethodEx(first).Label + first.LabelOffset);
                    sb.AppendLine(";");
                    return;
                }
            }

            var subprocessed = WriteMethodBody_30_ByObjType_CopyProcessed(processed);
            var ByObjType = context.ByObjType;
            int ByObjTypeCnt = (from overload in submethods
                                let overloadex = context.GetMethodEx(overload)
                                select overloadex.ArgTypes.Count).Max() + 1;

            int minindex = -1;
            int mincnt = int.MaxValue;
            //int maxtype = int.MaxValue;
            TypeOrLuaType mintype = new TypeOrLuaType();
            MethodOverload[] partmethods = null;
            //HashSet<Type> minSelectableTypes = null;

            if (ByObjTypeCnt > ByObjType.Count)
            { // instance methods.
                ByObjTypeCnt = ByObjType.Count;
            }

            int min_max_group_ele_cnt = int.MaxValue;
            int min_max_group_cnt = int.MinValue;
            for (int i = 0; i < ByObjTypeCnt; ++i)
            {
                var map = ByObjType[i];
                var rest = new HashSet<MethodOverload>(submethods);
                foreach (var kvp in map)
                {
                    rest.ExceptWith(kvp.Value);
                }
                var groups = from kvp in map
                             where !WriteMethodBody_30_ByObjType_HasProcessed(processed, i, kvp.Key)
                             let subgroup = from overload in kvp.Value
                                            where submethods.Contains(overload)
                                            select overload
                             where subgroup.Any(overload => kvp.Key.ClrType != null && overload.ArgTypes[i] == kvp.Key.ClrType || kvp.Key.ClrType == null && GetLuaType(overload.ArgTypes[i]) == kvp.Key.LuaType)
                             //where !submethods.SetEquals(subgroup) // this is normally for lua-type. if we can not split group by lua-type, we should ignore this.
                             let cnt = subgroup.Count()
                             where cnt > 0
                             select new { Type = kvp.Key, Group = subgroup };
                if (groups.Count() <= 0)
                {
                    continue;
                }

                //var selectableTypes = new HashSet<Type>(from subgroup in groups
                //                                        where !groups.Any(candi => candi.Type != subgroup.Type && subgroup.Type.IsAssignableFrom(candi.Type))
                //                                        select subgroup.Type);

                var max_group_ele_cnt = (from subgroup in groups
                                         //where selectableTypes.Contains(subgroup.Type)
                                         select subgroup.Group.Count()).Max();
                max_group_ele_cnt = Math.Max(max_group_ele_cnt, rest.Count);

                if (max_group_ele_cnt < min_max_group_ele_cnt
                    || max_group_ele_cnt == min_max_group_ele_cnt && (min_max_group_cnt == 1 && groups.Count() > min_max_group_cnt || min_max_group_cnt > 1 && groups.Count() > 1 && groups.Count() < min_max_group_cnt) // this is normally for lua-type. Consider: if lua-type cannot split group but the 1st arg is "this"(all-same-type).
                    )
                {
                    minindex = i;
                    min_max_group_ele_cnt = max_group_ele_cnt;
                    min_max_group_cnt = groups.Count();
                    //minSelectableTypes = selectableTypes;
                }
            }
            if (minindex >= 0)
            {
                var map = ByObjType[minindex].ToArray();
                Array.Sort(map, (kvp1, kvp2) =>
                {
                    return -WriteMethodBody_30_ByObjType_CompareType(kvp1.Key, kvp2.Key);
                });

                foreach (var kvp in map)
                {
                    //if (!minSelectableTypes.Contains(kvp.Key))
                    //{
                    //    continue;
                    //}
                    if (mintype.ClrType != null && kvp.Key.ClrType != null  && kvp.Key.ClrType.IsAssignableFrom(mintype.ClrType))
                    {
                        continue;
                    }
                    if (mintype.LuaType != null && kvp.Key.LuaType == null)
                    {
                        continue;
                    }

                    if (WriteMethodBody_30_ByObjType_HasProcessed(processed, minindex, kvp.Key))
                    {
                        continue;
                    }

                    var restset = new HashSet<MethodOverload>(kvp.Value);
                    restset.IntersectWith(submethods);
                    //if (submethods.SetEquals(restset))
                    //{ // this is normally for lua-type. if we can not split group by lua-type, we should ignore this.
                    //    continue;
                    //}
                    if (kvp.Key.ClrType != null && !restset.Any(overload => overload.ArgTypes[minindex] == kvp.Key.ClrType) || !restset.Any())
                    {
                        continue;
                    }
                    if (restset.Count <= 0)
                    {
                        continue;
                    }

                    var rest = restset.ToArray();
                    Array.Sort(rest, WriteMethodBody_30_ByObjType_CompareArgs);

                    int comparecnt = rest.Length;
                    if (kvp.Key.ClrType != null)
                    {
                        if (rest.All(overload =>
                        {
                            var overloadex = context.GetMethodEx(overload);
                            return overloadex.LastArgIsParam
                                && minindex == overloadex.ArgTypes.Count - 1;
                        }))
                        {
                            if (rest.Any(overload =>
                            {
                                var overloadex = context.GetMethodEx(overload);
                                return overloadex.ArgTypes[minindex] == kvp.Key.ClrType;
                            }))
                            {
                                comparecnt += submethods.Count;
                            }
                        }
                    }

                    bool found = false;
                    if (mintype.LuaType == null && kvp.Key.LuaType != null)
                    {
                        found = true;
                    }
                    else if (comparecnt < mincnt)
                    {
                        found = true;
                    }
                    else if (comparecnt == mincnt)
                    {
                        //var compresult = WriteMethodBody_30_ByObjType_CompareType(kvp.Key, mintype);
                        //if (compresult > 0) // we have already sorted the map
                        {
                            found = true;
                        }
                        //else if (compresult == 0)
                        //{
                        //    if (maptotalcnt < maxtype)
                        //    {
                        //        found = true;
                        //    }
                        //}
                    }
                    if (found)
                    {
                        //maxtype = maptotalcnt;
                        mincnt = comparecnt;
                        mintype = kvp.Key;
                        partmethods = rest;
                        //minindex = i;
                    }
                }
            }

            if (minindex >= 0)
            {
                mincnt = partmethods.Length;
                WriteMethodBody_30_ByObjType_MarkProcessed(subprocessed, minindex, mintype);
                var subsub = new HashSet<MethodOverload>(submethods);
                subsub.ExceptWith(partmethods);
                foreach (var kvp in ByObjType[minindex])
                {
                    if (kvp.Key == mintype)
                    {
                        continue;
                    }
                    if (kvp.Key.ClrType == null && mintype.ClrType != null && GetLuaType(mintype.ClrType) == kvp.Key.LuaType)
                    {
                        continue;
                    }
                    if (WriteMethodBody_30_ByObjType_HasProcessed(processed, minindex, kvp.Key))
                    {
                        continue;
                    }

                    var restset = new HashSet<MethodOverload>(kvp.Value);
                    restset.IntersectWith(submethods);
                    if (kvp.Key.ClrType != null && !restset.Any(overload => overload.ArgTypes[minindex] == kvp.Key.ClrType) || !restset.Any())
                    {
                        continue;
                    }
                    if (restset.Count <= 0)
                    {
                        continue;
                    }
                    subsub.UnionWith(restset);
                }

                if (mincnt == submethods.Count)
                {
                    if (subsub.Count > 0)
                    {
                        WriteMethodBody_30_ByObjType_WriteIndexAndType(context, processed, minindex, mintype);
                        sb.AppendLine("{");
                    }
                    if ((from overload in partmethods
                         group overload by overload.Method into g
                         select g).Count() == 1)
                    {
                        var labeloffsets = from overload in partmethods
                                           group overload by overload.LabelOffset into g
                                           select g.Key;
                        int labeloffset = 0;
                        if (labeloffsets.Count() == 1)
                        {
                            labeloffset = labeloffsets.First();
                        }

                        // all the same method
                        sb.Append("goto Label_");
                        var ex = context.GetMethodEx(partmethods.First());
                        sb.Append(ex.Label + labeloffset);
                        sb.AppendLine(";");
                    }
                    else
                    {
                        // we can not split any further. we just sort and write the most common one.
                        //WriteMethodBody_35_ByObjTypeExplicit(context, submethods, parsedTypeIndex);
                        var selected = partmethods.Last();
                        sb.Append("goto Label_");
                        var ex = context.GetMethodEx(selected);
                        sb.Append(ex.Label);
                        sb.AppendLine(";");
                    }
                    if (subsub.Count > 0)
                    {
                        sb.AppendLine("}");
                        sb.AppendLine("else");
                        sb.AppendLine("{");
                        WriteMethodBody_30_ByObjType_WorkStep(context, subsub, subprocessed);
                        sb.AppendLine("}");
                    }
                }
                else
                {
                    //var realtypes = from overload in partmethods
                    //                where overload.ArgTypes.Count > minindex
                    //                let type = overload.ArgTypes[minindex]
                    //                group type by type into g
                    //                select g.Key;
                    //WriteMethodBody_30_ByObjType_WriteIndexAndType(context, processed, minindex, mintype, realtypes);

                    var subprocessedelse = WriteMethodBody_30_ByObjType_CopyProcessed(subprocessed);
                    if (mintype.ClrType == null)
                    {
                        // LuaType
                        var minluatype = mintype.LuaType;
                        foreach (var kvp in ByObjType[minindex])
                        {
                            if (kvp.Key.ClrType != null)
                            {
                                if (GetLuaType(kvp.Key.ClrType) == minluatype)
                                {
                                    WriteMethodBody_30_ByObjType_MarkProcessed(subprocessedelse, minindex, kvp.Key);
                                }
                            }
                        }
                    }
                    if (subsub.Count > 0)
                    {
                        WriteMethodBody_30_ByObjType_WriteIndexAndType(context, processed, minindex, mintype);
                        sb.AppendLine("{");
                    }
                    WriteMethodBody_30_ByObjType_WorkStep(context, new HashSet<MethodOverload>(partmethods), subprocessedelse);
                    if (subsub.Count > 0)
                    {
                        sb.AppendLine("}");
                        sb.AppendLine("else");
                        sb.AppendLine("{");
                        WriteMethodBody_30_ByObjType_WorkStep(context, subsub, subprocessed);
                        sb.AppendLine("}");
                    }
                }
            }
            else
            {
                // we can not split any further. we just sort and write the most common one.
                //WriteMethodBody_35_ByObjTypeExplicit(context, submethods, parsedTypeIndex);
                partmethods = submethods.ToArray();
                Array.Sort(partmethods, WriteMethodBody_30_ByObjType_CompareArgs);
                var selected = partmethods.Last();
                sb.Append("goto Label_");
                var ex = context.GetMethodEx(selected);
                sb.Append(ex.Label);
                sb.AppendLine(";");
            }
        }
        internal static void WriteMethodBody_30_ByObjType(this WriteMethodBodyContext context, int argcnt = -1, HashSet<int> doneArgCnts = null)
        {
            var sb = context.sb;
            var methods = context.Overloads;
            var byObjType = context.ByObjType;

            List<HashSet<TypeOrLuaType>> processed = new List<HashSet<TypeOrLuaType>>();
            for (int i = 0; i < byObjType.Count; ++i)
            {
                processed.Add(new HashSet<TypeOrLuaType>());
            }
            var submethods = new HashSet<MethodOverload>(from overload in methods
                                                         where !context.DoneMethods.Contains(overload)
                                                         where argcnt < 0 || overload.ArgTypes.Count == argcnt
                                                         where doneArgCnts == null || !doneArgCnts.Contains(overload.ArgTypes.Count)
                                                         select overload);
            if (argcnt >= 0)
            {
                submethods.RemoveWhere(method => (from overload in submethods
                                                  where overload.ArgTypes == method.ArgTypes && method.LabelOffset > 0 && overload.LabelOffset == 0
                                                  select overload).Any());
            }
            WriteMethodBody_30_ByObjType_WorkStep(context, submethods, processed);
        }
        //internal static void WriteMethodBody_35_ByObjTypeExplicit(this WriteMethodBodyContext context, HashSet<MethodBase> methods, HashSet<int> parsedTypeIndex)
        //{
        //    var sb = context.sb;
        //    List<Dictionary<Type, HashSet<MethodBase>>> ByObjType = new List<Dictionary<Type, HashSet<MethodBase>>>();
        //    int maxargsCnt = methods.Max(method => (method.GetParameters() ?? new ParameterInfo[0]).Length + 1);
        //    for (int i = 0; i < maxargsCnt; ++i)
        //    {
        //        var map = new Dictionary<Type, HashSet<MethodBase>>();
        //        ByObjType.Add(map);

        //        foreach (var method in methods)
        //        {
        //            var ex = context.GetMethodEx(method);
        //            if (ex.ArgTypes.Count <= i)
        //                continue;

        //            var pt = ex.ArgTypes[i];

        //            HashSet<MethodBase> list;
        //            if (!map.TryGetValue(pt, out list))
        //            {
        //                list = new HashSet<MethodBase>();
        //                map[pt] = list;
        //            }

        //            list.Add(method);
        //        }
        //    }

        //    var submethods = methods;
        //    if (submethods.Count <= 0)
        //        return;
        //    else if (submethods.Count == 1)
        //    {
        //        var rest = submethods.First();
        //        var ex = context.GetMethodEx(rest);
        //        sb.Append("goto Label_");
        //        sb.Append(ex.Label);
        //        sb.AppendLine(";");
        //        return;
        //    }

        //    int maxcnt = 0;
        //    int maxindex = -1;

        //    for (int i = 0; i < ByObjType.Count; ++i)
        //    {
        //        var map = ByObjType[i];
        //        int cnt = 0;
        //        foreach (var kvp in map)
        //        {
        //            var rest = new HashSet<MethodBase>(kvp.Value);
        //            rest.IntersectWith(submethods);
        //            if (rest.Count <= 0)
        //                continue;
        //            ++cnt;
        //        }
        //        if (cnt > maxcnt)
        //        {
        //            maxcnt = cnt;
        //            maxindex = i;
        //        }
        //    }

        //    if (maxcnt > 0)
        //    {
        //        var map = ByObjType[maxindex];
        //        var sortedTypes = from kvp in map
        //                          let typew = LuaHub.GetTypeWeight(kvp.Key)
        //                          orderby typew
        //                          select kvp.Key;
        //        sortedTypes = sortedTypes.Where(type =>
        //        {
        //            var rest = new HashSet<MethodBase>(map[type]);
        //            rest.IntersectWith(submethods);
        //            return rest.Count > 0;
        //        });
        //        Dictionary<string, Type> parsedNative = new Dictionary<string, Type>();
        //        foreach (var type in sortedTypes)
        //        {
        //            if (nativeTypeMap.ContainsKey(type))
        //            {
        //                var ltype = nativeTypeMap[type];
        //                if (parsedNative.ContainsKey(ltype) && parsedNative[ltype] == nativeRevMap[ltype])
        //                {
        //                    continue;
        //                }
        //                parsedNative[nativeTypeMap[type]] = type;
        //            }
        //        }
        //        sortedTypes = sortedTypes.Where(type =>
        //        {
        //            if (!nativeTypeMap.ContainsKey(type))
        //            {
        //                return true;
        //            }
        //            var parsed = parsedNative[nativeTypeMap[type]];
        //            return parsed == type;
        //        });
        //        var sorted = sortedTypes.ToArray();

        //        for (int j = 0; j < sorted.Length; ++j)
        //        {
        //            var type = sorted[j];
        //            var rest = new HashSet<MethodBase>(map[type]);
        //            rest.IntersectWith(submethods);
        //            if (submethods.Count > rest.Count)
        //            {
        //                if (parsedTypeIndex.Add(maxindex))
        //                {
        //                    sb.Append("var ___ot");
        //                    sb.Append(maxindex);
        //                    sb.Append(" = l.GetType(");
        //                    sb.Append(maxindex + 1);
        //                    sb.AppendLine(");");
        //                }
        //                if (nativeTypeMap.ContainsKey(type))
        //                {
        //                    var rtype = nativeRevMap[nativeTypeMap[type]];
        //                    sb.Append("if (___ot");
        //                    sb.Append(maxindex);
        //                    sb.Append(" == typeof(");
        //                    sb.WriteType(rtype);
        //                    sb.AppendLine("))");
        //                }
        //                else
        //                {
        //                    if (type.IsValueType || type.IsEnum || type.IsSealed)
        //                    {
        //                        sb.Append("if (___ot");
        //                        sb.Append(maxindex);
        //                        sb.Append(" == typeof(");
        //                        sb.WriteType(type);
        //                        sb.AppendLine("))");
        //                    }
        //                    else
        //                    {
        //                        sb.Append("if (___ot");
        //                        sb.Append(maxindex);
        //                        sb.Append(" == typeof(");
        //                        sb.WriteType(type);
        //                        sb.Append(") || typeof(");
        //                        sb.WriteType(type);
        //                        sb.Append(").IsAssignableFrom(___ot");
        //                        sb.Append(maxindex);
        //                        sb.AppendLine("))");
        //                    }
        //                }
        //            }
        //            if (rest.Count == submethods.Count || rest.Count == 1)
        //            {
        //                var method = rest.First();
        //                var ex = context.GetMethodEx(method);
        //                sb.AppendLine("{");
        //                sb.Append("goto Label_");
        //                if (ex.LastArgIsParam)
        //                {
        //                    if (ex.ArgTypes.Count - 1 == maxindex)
        //                    {
        //                        sb.Append(ex.Label + 2);
        //                    }
        //                    else
        //                    {
        //                        sb.Append(ex.Label);
        //                    }
        //                }
        //                else
        //                {
        //                    sb.Append(ex.Label);
        //                }
        //                sb.AppendLine(";");
        //                sb.AppendLine("}");
        //                if (rest.Count == submethods.Count)
        //                {
        //                    return;
        //                }
        //            }
        //            else
        //            {
        //                sb.AppendLine("{");
        //                WriteMethodBody_35_ByObjTypeExplicit(context, rest, new HashSet<int>(parsedTypeIndex));
        //                sb.AppendLine("}");
        //            }
        //        }
        //    }
        //}
    }

    public class LuaPrecompileEditor : EditorWindow
    {
        string cmdstr = "";
        [MenuItem("Lua/Precompile/Precompile Manually", priority = 100010)]
        static void Init()
        {
            GetWindow(typeof(LuaPrecompileEditor)).titleContent = new GUIContent("LuaPrecompile");
        }
        void OnGUI()
        {
            cmdstr = EditorGUILayout.TextField(cmdstr);
            GUI.enabled = !string.IsNullOrEmpty(cmdstr);
            if (GUILayout.Button("Go!"))
            {
                LuaPrecompile.WritePrecompile(cmdstr);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Lua/Precompile/Precompile Batch", priority = 100020)]
        public static void PrecompileAutoBatch()
        {
            ParseEngineMemberList();
            var list = LuaPrecompile.ParseFullWhiteList();

            foreach (var command in list)
            {
                LuaPrecompile.WritePrecompile(command);
            }

            //PlatDependant.DeleteFile("EditorOutput/LuaPrecompile/CachedCommands.txt"); // maybe we donot need to delete this, in order to regenerate precompile files.
            //PlatDependant.DeleteFile("Assets/Mods/" + ModEditorUtils.__MOD__ + "/LuaPrecompile/MemberList.txt"); // maybe we donot need to delete this, in order to regenerate precompile files.

            BuildDelegateWrapperAOT.BuildDelegateWrapperForAOT();

            AssetDatabase.Refresh();
        }

        [MenuItem("Lua/Precompile/Delete All Precompile Files", priority = 100030)]
        public static void DeleteAllPrecompileFiles()
        {
            var manmod = ModEditorUtils.__MOD__;
            var manidir = "Assets/Mods/" + manmod + "/LuaHubSub/";
            var files = PlatDependant.GetAllFiles(manidir);
            foreach (var file in files)
            {
                var sub = file.Substring(manidir.Length);
                if (!sub.Contains("/") && !sub.Contains("\\") && sub.EndsWith(".cs") && sub.StartsWith("LuaHub_"))
                {
                    PlatDependant.DeleteFile(file);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Lua/Precompile/Parse Engine Member List", priority = 150010)]
        public static void ParseEngineMemberList()
        {
            var fulllist = ReflectAnalyzer.ParseMemberList();
            using (var sw = PlatDependant.OpenWriteText("EditorOutput/LuaPrecompile/MemberList.txt"))
            {
                for (int i = 0; i < fulllist.Count; ++i)
                {
                    sw.WriteLine(fulllist[i]);
                }
            }
            LoadEngineMemberList();
        }
        [MenuItem("Lua/Precompile/Load Engine Member List", priority = 150020)]
        public static void LoadEngineMemberList()
        {
            LuaPrecompile.LoadMemberList();
        }

        //[MenuItem("Lua/Test", priority = 900010)]
        //public static void Test()
        //{
        //    using (var sw = PlatDependant.OpenWriteText("EditorOutput/temp.txt"))
        //    {
        //        var asms = System.AppDomain.CurrentDomain.GetAssemblies();
        //        foreach (var asm in asms)
        //        {
        //            var types = asm.GetTypes(); // It seems that the GetTypes returns nested types.
        //            //var types = typeof(LuaLib.ReflectAnalyzerTestClass).GetNestedTypes();
        //            foreach (var type in types)
        //            {
        //                sw.Write("type ");
        //                sw.WriteLine(type.FullName);

        //                foreach (var member in type.GetMembers())
        //                {
        //                    sw.Write("memeber ");
        //                    sw.WriteLine(ReflectAnalyzer.GetIDString(member));
        //                }
        //            }
        //        }
        //    }
        //}
    }
}