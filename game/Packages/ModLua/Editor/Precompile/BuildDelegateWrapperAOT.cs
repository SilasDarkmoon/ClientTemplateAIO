using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.CodeDom;
using System.Reflection;
using System;
using UnityEngineEx;
using LuaLib;

using Types = LuaLib.Types;
using LuaHub = LuaLib.LuaHub;

namespace UnityEditorEx
{
    public static class BuildDelegateWrapperAOT
    {
        private static Dictionary<string, Type> _TypeList;
        //private static Dictionary<string, Type> TypeList
        //{
        //    get
        //    {
        //        if (_TypeList == null)
        //        {
        //            _TypeList = LuaPrecompile.GetFullTypeList();
        //        }
        //        return _TypeList;
        //    }
        //}

        [MenuItem("Lua/Precompile/Build Delegate Wrapper for AOT", priority = 100040)]
        public static void BuildDelegateWrapperForAOT()
        {
            HashSet<Types> list = new HashSet<Types>();
            HashSet<Type> list_uevent = new HashSet<Type>();
            HashSet<Type> typeSearched = new HashSet<Type>();

            var typelist = _TypeList = LuaPrecompile.GetFullTypeList();
            foreach (var kvp in typelist)
            {
                GetDelTypes(list, list_uevent, kvp.Value, typeSearched);
            }

            var manmod = ModEditorUtils.__MOD__;
            var manidir = "Assets/Mods/" + manmod + "/LuaHubSub/";

            BuildDelegateWrapperForAOT(list, manidir + "LuaDelegateWrapperAOT.cs");
            BuildEventReceiver(list_uevent, manidir + "LuaEventReceiver.cs");
            var root = ModEditor.GetPackageOrModRoot(manmod);
            var receivermetafile = root + "/Runtime/LuaBehav/LuaEventBridge/LuaEventReceiver.cs.meta~";
            if (PlatDependant.IsFileExist(receivermetafile))
            {
                PlatDependant.CopyFile(receivermetafile, manidir + "LuaEventReceiver.cs.meta");
            }
        }

        private static bool IsForbiddenType(Type type)
        {
            if (type.IsArray)
            {
                if (IsForbiddenType(type.GetElementType()))
                    return true;
            }

            if (type.IsGenericType)
            {
                foreach (var gtype in type.GetGenericArguments())
                {
                    if (IsForbiddenType(gtype))
                        return true;
                }
            }

            var typelist = _TypeList;
            if (!typelist.ContainsKey(type.FullName))
            {
                return true;
            }

            return false;
        }

        private static bool ContainsGenericParametersHierachical(this Type t)
        {
            if (t == null)
            {
                return false;
            }
            if (t.IsGenericParameter)
            {
                return true;
            }
            if (t.IsArray)
            {
                return ContainsGenericParametersHierachical(t.GetElementType());
            }
            if (t.IsGenericType)
            {
                return t.ContainsGenericParameters;
            }
            return false;
        }
        private static void GetDelTypes(HashSet<Types> list, HashSet<Type> list_uevent, Type searchType, HashSet<Type> typeSearched)
        {
            if (searchType == null || list == null || searchType.FullName == null)
            {
                return;
            }
            var preActionWrapper = typeof(LuaLib.LuaDelegateGenerator.ActionLuaWrapper).FullName;
            var preFuncWrapper = typeof(LuaLib.LuaDelegateGenerator.FuncLuaWrapper<>).FullName;
            preFuncWrapper = preFuncWrapper.Substring(0, preFuncWrapper.IndexOf('`'));
            if (searchType.FullName == preActionWrapper || searchType.FullName.StartsWith(preActionWrapper + "`") || searchType.FullName == preFuncWrapper || searchType.FullName.StartsWith(preFuncWrapper + "`"))
            {
                return;
            }

            if (typeSearched == null || typeSearched.Add(searchType))
            {
                if (searchType.ContainsGenericParametersHierachical())
                {
                    return;
                }

                GetDelTypes(list, list_uevent, searchType.BaseType, typeSearched);

                if (!searchType.IsPublic && !searchType.IsNestedPublic)
                {
                    return;
                }

                if (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(searchType))
                {
                    list_uevent.Add(searchType);
                }

                if (searchType.IsSubclassOf(typeof(Delegate)))
                {
                    if (searchType != typeof(Delegate) && searchType != typeof(MulticastDelegate))
                    {
                        bool shouldAOT = false;

                        Types pars = new Types();
                        var methodInvoke = searchType.GetMethod("Invoke");

                        if (methodInvoke.ReturnType != typeof(void) && (methodInvoke.ReturnType.IsValueType || methodInvoke.ReturnType.IsEnum))
                        {
                            shouldAOT = true;
                        }
                        pars.Add(methodInvoke.ReturnType);
                        var dpars = methodInvoke.GetParameters();
                        if (dpars != null)
                        {
                            foreach (var dpar in dpars)
                            {
                                pars.Add(dpar.ParameterType);
                            }
                        }

                        if (!shouldAOT)
                        {
                            for (int i = 1; i < pars.Count; ++i)
                            {
                                var par = pars[i];
                                if (par.IsValueType || par.IsEnum)
                                {
                                    shouldAOT = true;
                                    break;
                                }
                            }
                        }

                        bool allpublic = true;
                        for (int i = 0; i < pars.Count; ++i)
                        {
                            if (!pars[i].IsPublic && !pars[i].IsNestedPublic || pars[i].IsPointer || IsForbiddenType(pars[i]))
                            {
                                allpublic = false;
                                break;
                            }
                        }

                        if (shouldAOT && allpublic)
                        {
                            list.Add(pars);
                        }
                    }
                }
                else
                {
                    var ntypes = searchType.GetNestedTypes();
                    if (ntypes != null)
                    {
                        foreach (var ntype in ntypes)
                        {
                            GetDelTypes(list, list_uevent, ntype, typeSearched);
                        }
                    }

                    var members = searchType.GetMembers();
                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            if (member.MemberType == MemberTypes.Field)
                            {
                                var finfo = member as FieldInfo;
                                if (finfo != null)
                                {
                                    //if (finfo.FieldType.IsSubclassOf(typeof(Delegate)))
                                    {
                                        GetDelTypes(list, list_uevent, finfo.FieldType, typeSearched);
                                    }
                                }
                            }
                            else if (member.MemberType == MemberTypes.Property)
                            {
                                var pinfo = member as PropertyInfo;
                                if (pinfo != null)
                                {
                                    //if (pinfo.PropertyType.IsSubclassOf(typeof(Delegate)))
                                    {
                                        GetDelTypes(list, list_uevent, pinfo.PropertyType, typeSearched);
                                    }
                                }
                            }
                            else if (member.MemberType == MemberTypes.Event)
                            {
                                var einfo = member as EventInfo;
                                if (einfo != null)
                                {
                                    GetDelTypes(list, list_uevent, einfo.EventHandlerType, typeSearched);
                                }
                            }
                            else if (member.MemberType == MemberTypes.Constructor || member.MemberType == MemberTypes.Method)
                            {
                                MethodBase minfo = member as MethodBase;
                                if (minfo != null)
                                {
                                    var pars = minfo.GetParameters();
                                    if (pars != null)
                                    {
                                        foreach (var par in pars)
                                        {
                                            //if (par.ParameterType.IsSubclassOf(typeof(Delegate)))
                                            {
                                                GetDelTypes(list, list_uevent, par.ParameterType, typeSearched);
                                            }
                                        }
                                    }
                                    if (minfo is MethodInfo)
                                    {
                                        var method = minfo as MethodInfo;
                                        //if (method.ReturnType.IsSubclassOf(typeof(Delegate)))
                                        {
                                            GetDelTypes(list, list_uevent, method.ReturnType, typeSearched);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void BuildDelegateWrapperForAOT(IEnumerable<Types> deltypes, string file)
        {
            CodeCompileUnit cu = new CodeCompileUnit();
            CodeNamespace ns = new CodeNamespace("LuaLib.DelWrapperAOT");
            cu.Namespaces.Add(ns);

            CodeTypeDeclaration type_Entry = new CodeTypeDeclaration("DelWrapperAOTEntry");
            type_Entry.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;
            ns.Types.Add(type_Entry);

            CodeMemberMethod method_Entry = new CodeMemberMethod();
            method_Entry.Name = "Entry";
            method_Entry.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            method_Entry.ReturnType = new CodeTypeReference(typeof(void));
            type_Entry.Members.Add(method_Entry);
            method_Entry.Statements.Add(new CodeSnippetStatement("#pragma warning disable CS0618"));
            method_Entry.Statements.Add(new CodeSnippetStatement("            new LuaLib.LuaDelegateGenerator.ActionLuaWrapper();"));
            for (int i = 0; i < 9; ++i)
            {
                System.Text.StringBuilder line = new System.Text.StringBuilder();
                line.Append("            new LuaLib.LuaDelegateGenerator.ActionLuaWrapper<");
                for (int j = 0; j <= i; ++j)
                {
                    if (j != 0)
                    {
                        line.Append(", ");
                    }
                    line.Append("object");
                }
                line.Append(">();");
                method_Entry.Statements.Add(new CodeSnippetStatement(line.ToString()));
            }
            for (int i = 0; i < 10; ++i)
            {
                System.Text.StringBuilder line = new System.Text.StringBuilder();
                line.Append("            new LuaLib.LuaDelegateGenerator.FuncLuaWrapper<object");
                for (int j = 0; j < i; ++j)
                {
                    line.Append(", ");
                    line.Append("object");
                }
                line.Append(">();");
                method_Entry.Statements.Add(new CodeSnippetStatement(line.ToString()));
            }

            foreach (var deltype in deltypes)
            {
                var delpars = deltype;
                if (delpars[0] == typeof(void))
                {
                    var wrapper = LuaDelegateGenerator.GetWrapperType(delpars[0], delpars.Count - 1);
                    if (wrapper != null)
                    {
                        CodeTypeReference wtype = new CodeTypeReference(wrapper);
                        for (int i = 1; i < delpars.Count; ++i)
                        {
                            wtype.TypeArguments.Add(delpars[i]);
                        }
                        CodeObjectCreateExpression exp_new = new CodeObjectCreateExpression(wtype);
                        method_Entry.Statements.Add(exp_new);
                    }
                }
                else
                {
                    var wrapper = LuaDelegateGenerator.GetWrapperType(delpars[0], delpars.Count - 1);
                    if (wrapper != null)
                    {
                        CodeTypeReference wtype = new CodeTypeReference(wrapper);
                        for (int i = 0; i < delpars.Count; ++i)
                        {
                            wtype.TypeArguments.Add(delpars[i]);
                        }
                        CodeObjectCreateExpression exp_new = new CodeObjectCreateExpression(wtype);
                        method_Entry.Statements.Add(exp_new);
                    }
                }
            }
            method_Entry.Statements.Add(new CodeSnippetStatement("#pragma warning restore CS0618"));

            Microsoft.CSharp.CSharpCodeProvider csharpcodeprovider = new Microsoft.CSharp.CSharpCodeProvider();
            using (var sw = PlatDependant.OpenWriteText(file))
            {
                csharpcodeprovider.GenerateCodeFromCompileUnit(cu, sw, new System.CodeDom.Compiler.CodeGeneratorOptions());
            }
        }

        public static void BuildEventReceiver(IEnumerable<Type> ueventtypes, string file)
        { 
            // Generate LuaEventReceiver
            var cu = new CodeCompileUnit();
            var ns = new CodeNamespace("LuaLib.UI");
            cu.Namespaces.Add(ns);
            var type_Entry = new CodeTypeDeclaration("LuaEventReceiver");
            type_Entry.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;
            type_Entry.BaseTypes.Add("LuaBehavEx");
            ns.Types.Add(type_Entry);

            type_Entry.Members.Add(new CodeMemberField(typeof(string), "FuncName") { Attributes = MemberAttributes.Public });
            type_Entry.Members.Add(new CodeMemberMethod() { Name = "Start", ReturnType = new CodeTypeReference(typeof(void)), Attributes = MemberAttributes.Family });

            HashSet<Types> argTypes = new HashSet<Types>();
            System.Text.StringBuilder methodLine = new System.Text.StringBuilder();
            foreach (var ueventtype in ueventtypes)
            {
                Types pars = new Types();
                var methodInvoke = ueventtype.GetMethod("Invoke");
                if (methodInvoke != null)
                {
                    foreach (var par in methodInvoke.GetParameters())
                    {
                        pars.Add(par.ParameterType);
                    }
                }

                if (argTypes.Add(pars))
                {
                    var method = new CodeMemberMethod();
                    method.Name = "EventAction";
                    method.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                    method.ReturnType = new CodeTypeReference(typeof(void));
                    type_Entry.Members.Add(method);

                    methodLine.Clear();
                    methodLine.AppendLine("            if (this.isActiveAndEnabled)");
                    methodLine.Append("                this.CallLuaFunc(FuncName");
                    for (int i = 0; i < pars.Count; ++i)
                    {
                        method.Parameters.Add(new CodeParameterDeclarationExpression(pars[i], "p" + i));
                        methodLine.Append(", p");
                        methodLine.Append(i);
                    }
                    methodLine.Append(");");
                    method.Statements.Add(new CodeSnippetStatement(methodLine.ToString()));
                }
            }
            Microsoft.CSharp.CSharpCodeProvider csharpcodeprovider = new Microsoft.CSharp.CSharpCodeProvider();
            using (var sw = PlatDependant.OpenWriteText(file))
            {
                csharpcodeprovider.GenerateCodeFromCompileUnit(cu, sw, new System.CodeDom.Compiler.CodeGeneratorOptions());
            }
        }
    }
}