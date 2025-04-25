using GLSHGenerator.Types;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace GLSHGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            string folder = args[0];
            var genFolder = Path.Combine(folder, "Primitives");
            var extGenFolder = Path.Combine(folder, "Extensions");
            var infoGenFolder = Path.Combine(folder, "GLSH");

            Console.WriteLine("GlmSharp Generator");

            string path = genFolder;
            string extPath = extGenFolder;
            string infoPath = infoGenFolder;
            AbstractType.InitTypes();

            foreach (var type in AbstractType.Types.Values)
            {
                var basePath = type.PathOf(path);
                new FileInfo(basePath).Directory?.Create();
                if (type.CSharpFile.WriteToFileIfChanged(basePath))
                    Console.WriteLine("    CHANGED " + basePath);

                var glmPath = type.GlmPathOf(path);
                new FileInfo(glmPath).Directory?.Create();
                if (type.GlmSharpFile.WriteToFileIfChanged(glmPath))
                    Console.WriteLine("    CHANGED " + glmPath);

                if (AbstractType.SeparateUnmanagedAsExtensions)
                {
                    var extendPath = type.ExtPathOf(extPath);
                    new FileInfo(extendPath).Directory?.Create();
                    if (type.ExtCSharpFile.WriteToFileIfChanged(extendPath))
                        Console.WriteLine("    CHANGED " + extendPath);
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