using System;

namespace TestAssemblyInitializer
{
    public class Initializer : AssemblyInitializer.IAssemblyInitializer
    {
        public void Init()
        {
            Console.WriteLine("IAssemblyInitializer.Init");
        }
    }

    class Program
    {
        static Program() { }
        static AssemblyInitializer.AssemblyInitializerDummy _D = new AssemblyInitializer.AssemblyInitializerDummy();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
