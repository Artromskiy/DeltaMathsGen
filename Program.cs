using KibiHex.MathsGen.Types;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace KibiHex.MathsGen
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 1 && args[1] == "--model-preview")
            {
                var declaration = KibiHex.MathsGen.Model.VectorDeclarations.Float2();
                var renderer = new KibiHex.MathsGen.Model.Rendering.CSharpRenderer();
                Console.WriteLine(renderer.Render(declaration));
                return;
            }

            string folder = args[0];
            var genFolder = folder;

            Console.WriteLine("KibiHex MathsGen");

            AbstractType.InitTypes();

            foreach (var type in AbstractType.Types.Values)
            {
                var path = Path.Combine(folder, type.Name + ".cs");
                new FileInfo(path).Directory?.Create();
                if (type.RenderedCSharpFile.WriteToFileIfChanged(path))
                    Console.WriteLine("    CHANGED " + path);

                var swizzlesPath = Path.Combine(folder, type.Name + ".swizzles.cs");
                if (type.RenderedSwizzlesFile.WriteToFileIfChanged(swizzlesPath))
                    Console.WriteLine("    CHANGED " + swizzlesPath);

                //if (AbstractType.SeparateUnmanagedAsExtensions)
                //{
                //    path = Path.Combine(folder, type.Name + ".ext.cs");
                //    new FileInfo(path).Directory?.Create();
                //    if (type.ExtCSharpFile.WriteToFileIfChanged(path))
                //        Console.WriteLine("    CHANGED " + path);
                //}
            }

            var mathsPath = Path.Combine(folder, "maths.cs");
            if (MathsFacade.Render(AbstractType.Types.Values).WriteToFileIfChanged(mathsPath))
                Console.WriteLine("    CHANGED " + mathsPath);
        }
    }
}

