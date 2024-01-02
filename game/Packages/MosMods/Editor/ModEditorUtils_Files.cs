using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditorEx
{
    public static partial class ModEditorUtils
    {
        public static void HideFile(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("chflags", "-h hidden \"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                di.Attributes |= System.IO.FileAttributes.Hidden;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    fi.Attributes |= System.IO.FileAttributes.Hidden;
                }
            }
#endif
        }
        public static void UnhideFile(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("chflags", "-h nohidden \"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                di.Attributes &= ~System.IO.FileAttributes.Hidden;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    fi.Attributes &= ~System.IO.FileAttributes.Hidden;
                }
            }
#endif
        }
        public static bool IsFileHidden(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("ls", "-lOdP \"" + path + "\"");
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
            var output = p.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(output))
            {
                return false;
            }
            output = output.Trim();
            int indexpath;
            if ((indexpath = output.IndexOf(path, System.StringComparison.InvariantCultureIgnoreCase)) >= 0)
            {
                output = output.Substring(0, indexpath).Trim();
            }
            var idsplit = output.IndexOfAny(new[] { '/', '\\' });
            if (idsplit >= 0)
            {
                output = output.Substring(0, idsplit);
            }
            return output.Contains("hidden");
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                return (di.Attributes & System.IO.FileAttributes.Hidden) != 0;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    return (fi.Attributes & System.IO.FileAttributes.Hidden) != 0;
                }
            }
            return false;
#endif
        }

        public static void DeleteDirLink(string path)
        {
#if UNITY_EDITOR_WIN
            var si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"rmdir \"" + path.Replace('/', '\\') + "\"\"");
            si.CreateNoWindow = true;
            si.UseShellExecute = false;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            var si = new System.Diagnostics.ProcessStartInfo("rm", "\"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#endif
        }
        public static void MakeDirLink(string link, string target)
        {
#if UNITY_EDITOR_WIN
            var si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"mklink /D \"" + link.Replace('/', '\\') + "\"" + " \"" + target.Replace('/', '\\') + "\"\"");
            si.CreateNoWindow = true;
            si.UseShellExecute = false;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"mklink /J \"" + link.Replace('/', '\\') + "\"" + " \"" + (target.StartsWith(".") ? System.IO.Path.GetDirectoryName(link.Replace('/', '\\').TrimEnd('\\')) + "\\" : "") + target.Replace('/', '\\') + "\"\"");
                si.CreateNoWindow = true;
                si.UseShellExecute = false;
                p = System.Diagnostics.Process.Start(si);
                p.WaitForExit();
            }
#else
            for (int i = 0; i < 3; ++i)
            {
                if (System.IO.Directory.Exists(link))
                {
                    Debug.LogWarning("Symbol link src is already exists! " + link);
                    var dirinfo = new System.IO.DirectoryInfo(link);
                    if ((dirinfo.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint)
                    {
                        DeleteDirLink(link);
                    }
                    else
                    {
                        dirinfo.Delete();
                    }
                }
                else if (System.IO.File.Exists(link))
                {
                    Debug.LogError("Symbol link is already exists (as file)! " + link);
                    System.IO.File.Delete(link);
                }
                else
                {
                    break;
                }
            }
            if (System.IO.Directory.Exists(link) || System.IO.File.Exists(link))
            {
                Debug.LogError("Symbol link src is already exists! (skip) " + link);
                return;
            }

            var interdir = link + ".tmplink~";
            System.IO.Directory.CreateDirectory(interdir);
            var inter = interdir + "/" + System.IO.Path.GetFileName(target);
            for (int i = 0; i < 3; ++i)
            {
                if (System.IO.Directory.Exists(inter))
                {
                    var dirinfo = new System.IO.DirectoryInfo(inter);
                    if ((dirinfo.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint)
                    {
                        DeleteDirLink(inter);
                    }
                    else
                    {
                        dirinfo.Delete();
                    }
                }
                else if (System.IO.File.Exists(inter))
                {
                    System.IO.File.Delete(inter);
                }
                else
                {
                    break;
                }
            }
            if (System.IO.Directory.Exists(inter) || System.IO.File.Exists(inter))
            {
                Debug.LogError("Symbol link src is already exists! (skip) " + inter);
                return;
            }
            var si = new System.Diagnostics.ProcessStartInfo("ln", "-s \"" + target + "\"" + " \"" + interdir + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
            System.IO.Directory.Move(inter, link);
            System.IO.Directory.Delete(interdir, false);
#endif
        }
        public static bool IsDirLink(string path)
        {
#if UNITY_EDITOR_WIN
            var di = new System.IO.DirectoryInfo(path);
            return di.Exists && (di.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
#else
            if (System.IO.Directory.Exists(path) || System.IO.File.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                return (di.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
            }
            return false;
#endif
        }
        // TODO: Test these on Mac
        public static string ResolveLink(string path)
        {
#if UNITY_EDITOR_WIN
            return NativeWindowsMethods.GetFinalPathName(path);
#else
            string link = ReadLink(path);
            if (System.IO.Path.IsPathRooted(link))
            {
                return link;
            }
            else
            {
                var fullpath = System.IO.Path.GetFullPath(path);
                var dir = System.IO.Path.GetDirectoryName(fullpath);
                var abslink = System.IO.Path.Combine(dir, link);
                var fulllink = System.IO.Path.GetFullPath(abslink);
                return fulllink;
            }
#endif
        }

        public static bool IsDirLinkTo(string link, string target)
        {
            if (string.IsNullOrEmpty(link))
            {
                return string.IsNullOrEmpty(target);
            }
            var rawtarget = ResolveLink(link);
            if (string.IsNullOrEmpty(rawtarget))
            {
                return string.IsNullOrEmpty(target);
            }
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }
            return string.Equals(System.IO.Path.GetFullPath(rawtarget), System.IO.Path.GetFullPath(target), System.StringComparison.InvariantCultureIgnoreCase);
        }

#if UNITY_EDITOR_WIN
        static class NativeWindowsMethods
        {
            private static readonly System.IntPtr INVALID_HANDLE_VALUE = new System.IntPtr(-1);

            private const uint FILE_READ_EA = 0x0008;
            private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

            [System.Runtime.InteropServices.DllImport("Kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern uint GetFinalPathNameByHandle(System.IntPtr hFile, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] System.Text.StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            static extern bool CloseHandle(System.IntPtr hObject);

            [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
            public static extern System.IntPtr CreateFile(
                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string filename,
                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)] uint access,
                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)] System.IO.FileShare share,
                    System.IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)] System.IO.FileMode creationDisposition,
                    [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)] uint flagsAndAttributes,
                    System.IntPtr templateFile);

            public static string GetFinalPathName(string path)
            {
                var h = CreateFile(path,
                    FILE_READ_EA,
                    System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete,
                    System.IntPtr.Zero,
                    System.IO.FileMode.Open,
                    FILE_FLAG_BACKUP_SEMANTICS,
                    System.IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                    return null;

                try
                {
                    var sb = new System.Text.StringBuilder(1024);
                    var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                    if (res == 0)
                        return null;

                    return sb.ToString();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    return null;
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }
#else
        static string ReadLink(string path)
        {
            var si = new System.Diagnostics.ProcessStartInfo("readlink", "\"" + path + "\"");
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
            var output = p.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(output))
            {
                return null;
            }
            else
            {
                return output;
            }
        }
#endif

        public static bool ZipFolderNoCompress(string folder, string dest)
        {
            if (System.IO.File.Exists(dest))
            {
                System.IO.File.Delete(dest);
            }
            var si = new System.Diagnostics.ProcessStartInfo();
            si.WorkingDirectory = System.IO.Path.GetFullPath(folder);
#if UNITY_EDITOR_OSX
            si.FileName = "zip";
#else
            si.FileName = System.IO.Path.GetFullPath(ModEditor.GetPackageOrModRoot(__MOD__)) + "/~Tools~/zip.exe";
#endif
            si.Arguments = "-0 -r \"" + System.IO.Path.GetFullPath(dest) + "\" .";
            return ExecuteProcess(si);
        }

        public static int __LINE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileLineNumber();
            }
        }
        public static string __FILE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();
            }
        }
        public static string __ASSET__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();

                return GetAssetNameFromPath(file) ?? file;
            }
        }
        public static string __MOD__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();

                //return ModEditor.GetAssetModName(GetAssetNameFromPath(file));

                var package = ModEditor.GetPackageNameFromPath(file);
                if (string.IsNullOrEmpty(package))
                {
                    var rootdir = System.Environment.CurrentDirectory;
                    if (file.StartsWith(rootdir))
                    {
                        file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                    }
                    file = file.Replace('\\', '/');
                    //var iassets = file.IndexOf("Assets/");
                    //if (iassets > 0)
                    //{
                    //    file = file.Substring(iassets);
                    //}
                    return ModEditor.GetAssetModName(file);
                }
                else
                {
                    return ModEditor.GetPackageModName(package);
                }
            }
        }

        public static string GetAssetNameFromPath(string path)
        {
            var file = path;
            var package = ModEditor.GetPackageNameFromPath(file);
            if (string.IsNullOrEmpty(package))
            {
                var rootdir = System.Environment.CurrentDirectory;
                if (file.StartsWith(rootdir, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                }
                else
                {
                    return null;
                }
                //var iassets = file.IndexOf("Assets/");
                //if (iassets > 0)
                //{
                //    file = file.Substring(iassets);
                //}
                file = file.Replace('\\', '/');
                return file;
            }
            else
            {
                var rootdir = ModEditor.GetPackageRoot(package);
                file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                file = file.Replace('\\', '/');
                file = "Packages/" + package + "/" + file;
                return file;
            }
        }

        public static System.Diagnostics.Process StartProcess(System.Diagnostics.ProcessStartInfo si)
        {
            si.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardInput = true;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;

            var process = new System.Diagnostics.Process();
            process.StartInfo = si;
            process.Start();
            return process;
        }
        public static bool ParseCommand(string command, out string exe, out string arg)
        {
            exe = null;
            arg = null;
            if (string.IsNullOrEmpty(command))
            {
                return false;
            }
            command = command.TrimStart(' ', '\t');
            System.Text.StringBuilder sbexe = new System.Text.StringBuilder();

            int index = 0;
            char starttoken = '\0';
            for (; index < command.Length; ++index)
            {
                var ch = command[index];
                if (starttoken == '\0')
                {
                    if (ch == '\'' || ch == '\"')
                    {
                        starttoken = ch;
                    }
                    else if (ch == ' ' || ch == '\t')
                    {
                        break;
                    }
                    else
                    {
                        sbexe.Append(ch);
                    }
                }
                else
                {
                    if (ch == starttoken)
                    {
                        starttoken = '\0';
                    }
                    else
                    {
                        sbexe.Append(ch);
                    }
                }
            }

            if (sbexe.Length == 0)
            {
                return false;
            }
            exe = sbexe.ToString();

            if (index < command.Length)
            {
                arg = command.Substring(index).TrimStart(' ', '\t');
            }
            return true;
        }
        public static System.Diagnostics.Process StartProcess(string command)
        {
            string exe, arg;
            if (ParseCommand(command, out exe, out arg))
            {
                System.Diagnostics.ProcessStartInfo si;
                if (string.IsNullOrEmpty(arg))
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe);
                }
                else
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe, arg);
                }
                return StartProcess(si);
            }
            return null;
        }
        // TODO: on Mac
        public static System.Diagnostics.Process StartProcessAdmin(string command)
        {
            string exe, arg;
            if (ParseCommand(command, out exe, out arg))
            {
                System.Diagnostics.ProcessStartInfo si;
                if (string.IsNullOrEmpty(arg))
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe);
                }
                else
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe, arg);
                }
                si.Verb = "runas";
                return StartProcess(si);
            }
            return null;
        }
        public static System.Diagnostics.Process StartShell()
        {
#if UNITY_EDITOR_WIN
            return StartProcess("cmd");
#else
            return StartProcess("bash");
#endif
        }
        private static int _ShellProcessOutputFileID = 0;
        public static int ExecuteProcessInShell(System.Diagnostics.Process shellproc, System.Diagnostics.ProcessStartInfo si)
        {
            var oid = System.Threading.Interlocked.Increment(ref _ShellProcessOutputFileID);
            System.IO.Directory.CreateDirectory("EditorOutput/ShellOutput");
            var stdoutfile = System.IO.Path.GetFullPath("EditorOutput/ShellOutput/StdOut" + oid + ".txt");
            var stderrfile = System.IO.Path.GetFullPath("EditorOutput/ShellOutput/StdErr" + oid + ".txt");

            var input = shellproc.StandardInput;
            if (!string.IsNullOrEmpty(si.WorkingDirectory))
            {
                input.Write("cd \"");
                input.Write(si.WorkingDirectory);
                input.Write("\"\n");
            }
            input.Write("\"");
            input.Write(si.FileName);
            input.Write("\" ");
            input.Write(si.Arguments);
            if (!si.RedirectStandardOutput)
            {
                input.Write(" 1> \"");
                input.Write(stdoutfile);
                input.Write("\"");
            }
            if (!si.RedirectStandardError)
            {
                input.Write(" 2> \"");
                input.Write(stderrfile);
                input.Write("\"");
            }
            input.Write("\n");
#if UNITY_EDITOR_WIN
            input.Write("echo %errorlevel%\n");
#else
            input.Write("echo $?\n");
#endif

            var result = shellproc.StandardOutput.ReadLine();
            int exitcode;
            int.TryParse(result, out exitcode);

            string commandecho = si.FileName;
            if (si.Arguments != null)
            {
                commandecho = commandecho + " " + si.Arguments;
            }

            bool hasOutput = false;
            if (System.IO.File.Exists(stderrfile))
            {
                var content = System.IO.File.ReadAllText(stderrfile);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogErrorFormat("[Error {0}] {1}\n{2}", exitcode, commandecho, content);
                    hasOutput = true;
                }
                System.IO.File.Delete(stderrfile);
            }
            if (System.IO.File.Exists(stdoutfile))
            {
                var content = System.IO.File.ReadAllText(stdoutfile);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogFormat("[Output {0}] {1}\n{2}", exitcode, commandecho, content);
                    hasOutput = true;
                }
                System.IO.File.Delete(stdoutfile);
            }
            if (!hasOutput)
            {
                if (exitcode == 0)
                {
                    Debug.LogFormat("[Done {0}] {1}", exitcode, commandecho);
                }
                else
                {
                    Debug.LogErrorFormat("[Error {0}] {1}", exitcode, commandecho);
                }
            }

            return exitcode;
        }
        public static int ExecuteProcessInShell(System.Diagnostics.Process shellproc, string command, bool cdToExeFolder)
        {
            string exe, arg;
            if (ParseCommand(command, out exe, out arg))
            {
                System.Diagnostics.ProcessStartInfo si;
                if (string.IsNullOrEmpty(arg))
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe);
                }
                else
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe, arg);
                }
                if (cdToExeFolder)
                {
                    var dir = System.IO.Path.GetDirectoryName(exe);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        si.WorkingDirectory = dir;
                    }
                }
                return ExecuteProcessInShell(shellproc, si);
            }
            Debug.LogErrorFormat("Cannot parse command: " + command);
            return -1;
        }
        public static int ExecuteProcessInShell(System.Diagnostics.Process shellproc, string command)
        {
            return ExecuteProcessInShell(shellproc, command, false);
        }
        public static int ExecuteProcessInShell(System.Diagnostics.Process shellproc, string command, string workingDir)
        {
            string exe, arg;
            if (ParseCommand(command, out exe, out arg))
            {
                System.Diagnostics.ProcessStartInfo si;
                if (string.IsNullOrEmpty(arg))
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe);
                }
                else
                {
                    si = new System.Diagnostics.ProcessStartInfo(exe, arg);
                }
                si.WorkingDirectory = workingDir;
                return ExecuteProcessInShell(shellproc, si);
            }
            Debug.LogErrorFormat("Cannot parse command: " + command);
            return -1;
        }

        public static bool ExecuteProcess(System.Diagnostics.ProcessStartInfo si)
        {
            bool safeWaitMode = true;
#if UNITY_EDITOR_WIN
            safeWaitMode = false;
#endif
            // TODO: on Apple M1, we must use safeWaitMode. we should test non-safeMode on Mac-on-Intel and Linux and add "#if" here. NOTICE: use SystemInfo.processorType to get cpu model name.

            si.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = si;
                process.OutputDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, false);
                process.ErrorDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, true);

                System.Threading.ManualResetEventSlim waitHandleForProcess = null;
                if (safeWaitMode)
                {
                    waitHandleForProcess = new System.Threading.ManualResetEventSlim();
                    process.Exited += (s, e) => waitHandleForProcess.Set();
                }

                Debug.LogFormat("Starting process {0} {1}", si.FileName, si.Arguments);
                process.Start();
                Debug.LogFormat("Started process {0} {1}", si.FileName, si.Arguments);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (waitHandleForProcess)
                {
                    while (!process.HasExited)
                    {
                        if (safeWaitMode)
                        {
                            waitHandleForProcess.Wait(1000);
                        }
                        else
                        {
                            process.WaitForExit(1000);
                        }
                    }
                }

                if (process.ExitCode != 0)
                {
                    Debug.LogErrorFormat("Error executing process {0} {1}", si.FileName, si.Arguments);
                    return false;
                }
                else
                {
                    Debug.LogFormat("Successfully executed process {0} {1}", si.FileName, si.Arguments);
                    return true;
                }
            }
        }
        private static void WriteProcessOutput(System.Diagnostics.Process p, string data, bool isError)
        {
            if (!string.IsNullOrEmpty(data))
            {
                string processName = System.IO.Path.GetFileName(p.StartInfo.FileName);
#if UNITY_EDITOR_OSX
                if (processName == "wine" || processName == "mono")
                {
                    processName = System.IO.Path.GetFileName(p.StartInfo.Arguments.Split(' ').FirstOrDefault());
                }
#endif
                if (!isError)
                {
                    Debug.LogFormat("[{0}] {1}", processName, data);
                }
                else
                {
                    Debug.LogErrorFormat("[{0} Error] {1}", processName, data);
                }
            }
        }

        public static void AddGitIgnore(string gitignorepath, params string[] items)
        {
            List<string> lines = new List<string>();
            HashSet<string> lineset = new HashSet<string>();
            if (System.IO.File.Exists(gitignorepath))
            {
                try
                {
                    using (var sr = new System.IO.StreamReader(gitignorepath))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                            lineset.Add(line);
                        }
                    }
                }
                catch { }
            }

            if (items != null)
            {
                for (int i = 0; i < items.Length; ++i)
                {
                    var item = items[i];
                    if (lineset.Add(item))
                    {
                        lines.Add(item);
                    }
                }
            }

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(gitignorepath));
            using (var sw = new System.IO.StreamWriter(gitignorepath))
            {
                for (int i = 0; i < lines.Count; ++i)
                {
                    sw.WriteLine(lines[i]);
                }
            }
        }

        public static void RemoveGitIgnore(string gitignorepath, params string[] items)
        {
            List<string> lines = new List<string>();
            HashSet<string> removes = new HashSet<string>();
            if (items != null)
            {
                removes.UnionWith(items);
            }
            if (System.IO.File.Exists(gitignorepath))
            {
                try
                {
                    using (var sr = new System.IO.StreamReader(gitignorepath))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!removes.Contains(line))
                            {
                                lines.Add(line);
                            }
                        }
                    }
                }
                catch { }
            }
            if (lines.Count == 0)
            {
                if (System.IO.File.Exists(gitignorepath))
                {
                    System.IO.File.Delete(gitignorepath);
                }
            }
            else
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(gitignorepath));
                using (var sw = new System.IO.StreamWriter(gitignorepath))
                {
                    for (int i = 0; i < lines.Count; ++i)
                    {
                        sw.WriteLine(lines[i]);
                    }
                }
            }
        }

        public static void MergeXml(System.Xml.Linq.XElement eledest, System.Xml.Linq.XElement elesrc)
        {
            foreach (var attr in elesrc.Attributes())
            {
                eledest.SetAttributeValue(attr.Name, attr.Value);
            }
            foreach (var srcchild in elesrc.Elements())
            {
                //var dstchild = eledest.Element(srcchild.Name);
                //if (dstchild != null)
                //{
                //    MergeXml(dstchild, srcchild);
                //}
                //else
                //{
                //    dstchild = new System.Xml.Linq.XElement(srcchild);
                //    eledest.SetElementValue(srcchild.Name, dstchild);
                //}
                var dstchild = new System.Xml.Linq.XElement(srcchild);
                eledest.Add(dstchild);
            }
        }
        public static void MergeXml(string pathdst, string pathsrc)
        {
            System.Xml.Linq.XDocument src = null;
            try
            {
                src = System.Xml.Linq.XDocument.Load(pathsrc);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            if (src == null)
            {
                return;
            }

            System.Xml.Linq.XDocument dst = null;
            try
            {
                dst = System.Xml.Linq.XDocument.Load(pathdst);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            if (dst == null)
            {
                dst = new System.Xml.Linq.XDocument(src);
            }
            else
            {
                MergeXml(dst.Root, src.Root);
            }

            dst.Save(pathdst);
        }

        public static void MergeXml(System.Xml.Linq.XDocument dst, string pathsrc)
        {
            System.Xml.Linq.XDocument src = null;
            try
            {
                src = System.Xml.Linq.XDocument.Load(pathsrc);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            if (src == null)
            {
                return;
            }

            if (dst.Root == null)
            {
                dst.Add(new System.Xml.Linq.XElement(src.Root));
            }
            else
            {
                MergeXml(dst.Root, src.Root);
            }
        }

        public static string GetStreamMD5(System.IO.Stream stream)
        { // TODO: test and move to runtime.
            try
            {
                byte[] hash = null;
                if (stream != null)
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        hash = md5.ComputeHash(stream);
                    }
                }
                if (hash == null || hash.Length <= 0) return "";
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hash.Length; ++i)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
            catch { }
            return "";
        }
        public static string GetFileMD5(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    using (var stream = System.IO.File.OpenRead(path))
                    {
                        return GetStreamMD5(stream);
                    }
                }
            }
            catch { }
            return "";
        }
        public static long GetFileLength(string path)
        {
            try
            {
                var f = new System.IO.FileInfo(path);
                return f.Length;
            }
            catch { }
            return 0;
        }
    }
}