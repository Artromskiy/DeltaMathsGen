using DVG.GLSH.Generator.Types;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace DVG.GLSH.Generator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            string folder = args[0];
            var genFolder = folder;

            Console.WriteLine("GLSH Generator");

            AbstractType.InitTypes();

            foreach (var type in AbstractType.Types.Values)
            {
                var path = Path.Combine(folder, type.Name + ".cs");
                new FileInfo(path).Directory?.Create();
                if (type.CSharpFile.WriteToFileIfChanged(path))
                    Console.WriteLine("    CHANGED " + path);

                path = Path.Combine(folder, type.Name + ".glsh.cs");
                new FileInfo(path).Directory?.Create();
                if (type.GlmSharpFile.WriteToFileIfChanged(path))
                    Console.WriteLine("    CHANGED " + path);

                if (AbstractType.SeparateUnmanagedAsExtensions)
                {
                    path = Path.Combine(folder, type.Name + ".ext.cs");
                    new FileInfo(path).Directory?.Create();
                    if (type.ExtCSharpFile.WriteToFileIfChanged(path))
                        Console.WriteLine("    CHANGED " + path);
                }
            }
            /*
            var file = AbstractType.InfoPathOf(infoPath, "GLSHInfo");
            new FileInfo(file).Directory?.Create();
            if (InfoGenerator.InfoFile().WriteToFileIfChanged(file))
                Console.WriteLine("    CHANGED " + file);
            */
        }
    }
}