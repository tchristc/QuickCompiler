using System;

namespace QuickCompiler.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var code = @"
                using System;
                namespace RoslynCompileSample
                {
                    public class Writer
                    {
                        public void Write(string message)
                        {
                            Console.WriteLine(message);
                        }
                    }
                }";

            var compiler = new DefaultCSharpMemoryCompiler(code);
            var assembly = compiler.Compile();
            var obj = new ObjectWrapper(assembly, "RoslynCompileSample.Writer");
            obj.Action("Write", "Hello World");

            Console.ReadLine();
        }
    }
}
