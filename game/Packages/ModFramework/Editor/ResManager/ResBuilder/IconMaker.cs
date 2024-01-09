using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    public static class IconMaker
    {
        public static bool WriteTextToImage(string text, string imagepath)
        {
            try
            {
                var thisfile = ModEditorUtils.__ASSET__;
                var dirlast = thisfile.LastIndexOfAny(new[] { '\\', '/' });
                var dir = thisfile.Substring(0, dirlast);
                var prefabfile = dir + "/IconMaker/IconMaker.prefab";
                //var scenefile = dir + "/IconMaker/IconMaker.unity";
                var rtfile = dir + "/IconMaker/IconRT.renderTexture";

                var prefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(prefabfile);
                var obj = GameObject.Instantiate(prefab);
                try
                {
                    var txt = obj.GetComponentInChildren<UnityEngine.UI.Text>();
                    txt.text = text;

                    var cam = obj.GetComponentInChildren<Camera>();
                    cam.Render();

                    var rt = (RenderTexture)AssetDatabase.LoadMainAssetAtPath(rtfile);
                    var oldactive = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = rt;
                        var t2d = new Texture2D(rt.width, rt.height, rt.graphicsFormat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
                        t2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        var bytes = t2d.EncodeToPNG();
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(imagepath));
                        System.IO.File.WriteAllBytes(imagepath, bytes);
                    }
                    finally
                    {
                        RenderTexture.active = oldactive;
                    }
                }
                finally
                {
                    if (obj)
                    {
                        GameObject.DestroyImmediate(obj);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            return true;
        }

#pragma warning disable CS0162
        public static bool ChangeImageToIco(string srcpath, string icopath)
        {
#if UNITY_EDITOR_WIN
            try
            {
                if (!System.IO.File.Exists(srcpath))
                {
                    return false;
                }
                var si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = System.IO.Path.GetFullPath(ModEditor.GetPackageOrModRoot(ModEditorUtils.__MOD__)) + "/~Tools~/BitmapToIcon.exe";
                si.Arguments = "\"" + srcpath + "\"";
                if (ModEditorUtils.ExecuteProcess(si))
                {
                    var interpath = System.IO.Path.ChangeExtension(srcpath, ".ico");
                    if (System.IO.File.Exists(interpath))
                    {
                        if (!string.IsNullOrEmpty(icopath))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(icopath));
                            System.IO.File.Move(interpath, icopath);
                        }
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            return true;
#endif
            return false;
        }
#pragma warning restore CS0162

        //#if UNITY_EDITOR_WIN
        //        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        //        private extern static void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        //#endif

        public static bool SetFolderIcon(string folder, string icopath)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                return false;
            }

#if UNITY_EDITOR_WIN
            try
            {
                var iniPath = System.IO.Path.Combine(folder, "desktop.ini");
                if (System.IO.File.Exists(iniPath))
                {
                    //remove hidden and system attributes to make ini file writable
                    System.IO.File.SetAttributes(
                        iniPath,
                        System.IO.File.GetAttributes(iniPath) &
                            ~(System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System));
                }

                var relpath = PlatDependant.GetRelativePath(folder, icopath);

                //create new ini file with the required contents
                var iniContents = new System.Text.StringBuilder()
                    .AppendLine("[.ShellClassInfo]")
                    .Append("IconResource=").Append(relpath).Append(",0").AppendLine()
                    .Append("IconFile=").Append(relpath).AppendLine()
                    .AppendLine("IconIndex=0")
                    .ToString();
                System.IO.File.WriteAllText(iniPath, iniContents);

                //hide the ini file and set it as system
                System.IO.File.SetAttributes(
                    iniPath,
                    System.IO.File.GetAttributes(iniPath) | System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System);
                //set the folder as system -- without system attribute, windows will not apply desktop.ini of this folder.
                System.IO.File.SetAttributes(
                    folder,
                    System.IO.File.GetAttributes(folder) | System.IO.FileAttributes.System);

                // make the icon change instantly refreshed.
                //var hstr = System.Runtime.InteropServices.Marshal.StringToHGlobalUni(System.IO.Path.GetFullPath(folder));
                //SHChangeNotify(0x00002000 /*SHCNE_UPDATEITEM*/, 0x0005 /*SHCNF_PATH*/, hstr, IntPtr.Zero);
                //System.Runtime.InteropServices.Marshal.FreeHGlobal(hstr);
                var si = new System.Diagnostics.ProcessStartInfo("ie4uinit", "-ClearIconCache");
                ModEditorUtils.ExecuteProcess(si);
                si = new System.Diagnostics.ProcessStartInfo("ie4uinit", "-show");
                ModEditorUtils.ExecuteProcess(si);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            return true;
#elif UNITY_EDITOR_OSX
            var shell = ModEditorUtils.StartShell();
            try
            {
                var realfolder = System.IO.Path.GetFullPath(folder);
                var realicon = System.IO.Path.GetFullPath(icopath);
                // rm -rf "$droplet"$'/Icon\r'
                var si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "rm";
                si.Arguments = "-rf $'" + realfolder + @"/Icon\r'";
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // sips -i "$icon" >/dev/null
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "sips";
                si.Arguments = "-i '" + realicon + "' >/dev/null";
                si.RedirectStandardOutput = true;
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // DeRez -only icns "$icon" > /tmp/icns.rsrc
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "DeRez";
                si.Arguments = "-only icns '" + realicon + "' > '" + realfolder + "/tmpicon.rsrc'";
                si.RedirectStandardOutput = true;
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // Rez -append /tmp/icns.rsrc -o "$droplet"$'/Icon\r'
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "Rez";
                si.Arguments = "-append '" + realfolder + "/tmpicon.rsrc' -o $'" + realfolder + @"/Icon\r'";
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // SetFile -a C "$droplet"
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "SetFile";
                si.Arguments = "-a C '" + realfolder + "'";
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // SetFile -a V "$droplet"$'/Icon\r'
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "SetFile";
                si.Arguments = "-a V $'" + realfolder + @"/Icon\r'";
                ModEditorUtils.ExecuteProcessInShell(shell, si);
                // rm -rf /tmp/icns.rsrc
                si = new System.Diagnostics.ProcessStartInfo();
                si.FileName = "rm";
                si.Arguments = "-rf '" + realfolder + "/tmpicon.rsrc'";
                ModEditorUtils.ExecuteProcessInShell(shell, si);

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            finally
            {
                shell.Kill();
                shell = null;
            }
#else
            return false;
#endif
        }

        public static bool SetFolderIconToText(string folder, string text)
        {
            var iconimg = System.IO.Path.Combine(folder, "icon.png");
            var iconico = System.IO.Path.Combine(folder, "icon.ico");
            if (IconMaker.WriteTextToImage(text, iconimg))
            {
                if (IconMaker.ChangeImageToIco(iconimg, null))
                {
                    return IconMaker.SetFolderIcon(folder, iconico);
                }
                else
                {
                    return IconMaker.SetFolderIcon(folder, iconimg);
                }
            }
            return false;
        }
        public static void FixIcon(string folder)
        {
            string iconfile = null;
#if UNITY_EDITOR_WIN
            iconfile = System.IO.Path.Combine(folder, "icon.ico");
#elif UNITY_EDITOR_OSX
            iconfile = System.IO.Path.Combine(folder, "icon.png");
#endif
            if (!string.IsNullOrEmpty(iconfile) && System.IO.File.Exists(iconfile))
            {
                IconMaker.SetFolderIcon(folder, iconfile);
            }
        }
        public static bool SetFolderIconToFileContent(string folder, string file)
        {
            if (!System.IO.File.Exists(file))
            {
                return false;
            }
            string content;
            try
            {
                content = System.IO.File.ReadAllText(file);
                if (!string.IsNullOrEmpty(content))
                {
                    SetFolderIconToText(folder, content);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            return false;
        }
    }
}
