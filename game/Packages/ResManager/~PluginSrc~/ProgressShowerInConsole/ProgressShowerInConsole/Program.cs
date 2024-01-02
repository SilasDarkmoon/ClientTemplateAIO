using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressShowerInConsole
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            for (int i = 0; i < 100; ++i)
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
#endif
            Console.WriteLine("Started");

            var pipeName = "";
            if (args != null && args.Length > 0)
            {
                pipeName = args[0];
            }
            if (pipeName == null)
            {
                pipeName = "";
            }

            using (var pipein = new System.IO.Pipes.NamedPipeClientStream(".", "ProgressShowerInConsole" + pipeName, System.IO.Pipes.PipeDirection.In))
            {
                using (var pipeout = new System.IO.Pipes.NamedPipeClientStream(".", "ProgressShowerInConsoleControl" + pipeName, System.IO.Pipes.PipeDirection.Out))
                {
                    pipein.Connect();
                    pipeout.Connect();
                    Console.WriteLine("Connected");

                    var thd_Read = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            var sr = new System.IO.StreamReader(pipein);
                            while (true)
                            {
                                var line = sr.ReadLine();
                                if (line == "\uEE05Message")
                                {
                                    var mess = sr.ReadLine();
                                    Console.WriteLine(mess);
                                }
                                else if (line == "\uEE05Title")
                                {
                                    var title = sr.ReadLine();
                                    Console.Title = title;
                                    Console.WriteLine("Showing progress of " + title);
                                }
                                else if (line == "\uEE05Quit")
                                {
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            Console.WriteLine("Closing...");
                            System.Threading.Thread.Sleep(3000);
                        }
                    });
                    thd_Read.Start();

                    var thd_Control = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            var sw = new System.IO.StreamWriter(pipeout);
                            while (true)
                            {
                                var kinfo = Console.ReadKey(true);
                                if (kinfo.Key == ConsoleKey.Q && (kinfo.Modifiers & ConsoleModifiers.Control) != 0)
                                {
                                    sw.WriteLine("\uEE05Quit");
                                    sw.Flush();
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            thd_Read.Abort();
                        }
                    });
                    thd_Control.Start();

                    thd_Read.Join();
                    Environment.Exit(0);
                }
            }
        }
    }
}
