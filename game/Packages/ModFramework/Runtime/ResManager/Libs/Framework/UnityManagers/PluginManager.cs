#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngineEx
{
    public static class PluginManager
    {
        private static ConcurrentDictionary<Guid, IntPtr> _PluginRegistry = new ConcurrentDictionary<Guid, IntPtr>();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate IntPtr GetInterfaceDel(Guid guid);
        public static IntPtr GetInterface(Guid guid)
        {
            IntPtr rv;
            _PluginRegistry.TryGetValue(guid, out rv);
            return rv;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void RegisterInterfaceDel(Guid guid, IntPtr ptr);
        public static void RegisterInterface(Guid guid, IntPtr ptr)
        {
            _PluginRegistry[guid] = ptr;
        }

        public static Guid ConcatGuid(ulong guidHigh, ulong guidLow)
        {
            var hbytes = BitConverter.GetBytes(guidHigh);
            var lowbytes = BitConverter.GetBytes(guidLow);
            var bytes = new byte[hbytes.Length + lowbytes.Length];
            hbytes.CopyTo(bytes, 0);
            lowbytes.CopyTo(bytes, hbytes.Length);
            Guid guid = new Guid(bytes);
            return guid;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate IntPtr GetInterfaceSplitDel(ulong guidHigh, ulong guidLow);
        public static IntPtr GetInterfaceSplit(ulong guidHigh, ulong guidLow)
        {
            return GetInterface(ConcatGuid(guidHigh, guidLow));
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void RegisterInterfaceSplitDel(ulong guidHigh, ulong guidLow, IntPtr ptr);
        public static void RegisterInterfaceSplit(ulong guidHigh, ulong guidLow, IntPtr ptr)
        {
            RegisterInterface(ConcatGuid(guidHigh, guidLow), ptr);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IUnityInterfaces
        {
            public GetInterfaceDel GetInterfaceFunc;
            public RegisterInterfaceDel RegisterInterfaceFunc;
            public GetInterfaceSplitDel GetInterfaceSplitFunc;
            public RegisterInterfaceSplitDel RegisterInterfaceSplitFunc;
        }
        public static IUnityInterfaces UnityInterfaces = new IUnityInterfaces()
        {
            GetInterfaceFunc = GetInterface,
            RegisterInterfaceFunc = RegisterInterface,
            GetInterfaceSplitFunc = GetInterfaceSplit,
            RegisterInterfaceSplitFunc = RegisterInterfaceSplit,
        };


        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void UnityPluginLoadDel(ref IUnityInterfaces unityInterfaces);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void UnityPluginUnloadDel();
        
        private class LibEntryPointInfo
        {
            public UnityPluginLoadDel OnLoad;
            public UnityPluginUnloadDel OnUnload;
        }
        private static ConcurrentDictionary<string, LibEntryPointInfo> _LoadedLibs = new ConcurrentDictionary<string, LibEntryPointInfo>();
        private static ConcurrentStack<string> _LoadedLibsSeq = new ConcurrentStack<string>();

        static PluginManager()
        {
            PlatDependant.Quitting += UnloadAllLibs;
        }

        public static void LoadLib(string libname)
        {
            if (!string.IsNullOrEmpty(libname))
            {
                _LoadedLibs.GetOrAdd(libname, lib =>
                {
                    var info = new LibEntryPointInfo();

                    var assemblyName = new AssemblyName();
                    assemblyName.Name = "CodeEmit_LibEntry_" + lib;
                    var asmbuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                    var codeEmitModule = asmbuilder.DefineDynamicModule(assemblyName.Name);
                    var typebuilder = codeEmitModule.DefineType(assemblyName.Name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed, typeof(object));

                    var dllname = lib;
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
#if DLLIMPORT_NAME_FULL
                        //var ext = System.IO.Path.GetExtension(dllname).ToLower();
                        //if (ext != ".so" && ext != ".dylib" && ext != ".bundle")
                        //{
                        //    dllname = dllname + ".so";
                        //}
                        //if (!dllname.StartsWith("lib", StringComparison.InvariantCultureIgnoreCase))
                        //{
                        //    dllname = "lib" + dllname;
                        //}
#else
                        // TODO: on higher version .NET Core, we should consider MacOS X.
                        dllname = AppDomain.CurrentDomain.BaseDirectory + "/lib" + lib + ".so";
#endif
                    }
//#if UNITY_EDITOR_OSX
//                    var epLoad = "ModPluginLoad";
//                    var epUnload = "ModPluginUnload";
//#else
                    var epLoad = "UnityPluginLoad";
                    var epUnload = "UnityPluginUnload";
//#endif
                    var mbuilder_load = typebuilder.DefineMethod(epLoad, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl, typeof(void), new[] { typeof(IUnityInterfaces).MakeByRefType() });
                    var dllimport = new CustomAttributeBuilder(typeof(DllImportAttribute).GetConstructor(new[] { typeof(string) }), new[] { dllname });
                    mbuilder_load.SetCustomAttribute(dllimport);

                    var mbuilder_unload = typebuilder.DefineMethod(epUnload, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl, typeof(void), new Type[0]);
                    //var dllimport = new CustomAttributeBuilder(typeof(DllImportAttribute).GetConstructor(new[] { typeof(string) }), new[] { dllname });
                    mbuilder_unload.SetCustomAttribute(dllimport);

                    var createdtype = typebuilder.CreateType();
                    info.OnLoad = (UnityPluginLoadDel)Delegate.CreateDelegate(typeof(UnityPluginLoadDel), createdtype.GetMethod(epLoad));
                    info.OnUnload = (UnityPluginUnloadDel)Delegate.CreateDelegate(typeof(UnityPluginUnloadDel), createdtype.GetMethod(epUnload));
                    try
                    {
                        info.OnLoad(ref UnityInterfaces);
                    }
                    catch (EntryPointNotFoundException) { }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                    return info;
                });
                _LoadedLibsSeq.Push(libname);
            }
        }
        public static void UnloadLib(string lib)
        {
            LibEntryPointInfo info;
            if (_LoadedLibs.TryRemove(lib, out info))
            {
                if (info != null && info.OnUnload != null)
                {
                    try
                    {
                        info.OnUnload();
                    }
                    catch (EntryPointNotFoundException) { }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }
        private static void UnloadAllLibs()
        {
            string lib;
            while (_LoadedLibsSeq.TryPop(out lib))
            {
                UnloadLib(lib);
            }
        }
    }
}
#else
namespace UnityEngineEx
{
    public static class PluginManager
    {
        public static void LoadLib(string lib)
        {
        }
        public static void UnloadLib(string lib)
        {
        }
    }
}
#endif