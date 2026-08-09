using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Linq;

namespace KibiHex.MathsGen
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 1 && args[0] == "--dump-api")
            {
                var surface = Validation.ApiSurfaceReader.Read(args[1]);
                foreach (var type in surface.Types.Values.OrderBy(type => type.Name))
                foreach (var member in type.Members.OrderBy(member => member))
                    Console.WriteLine(type.Name + "|" + member);
                return;
            }

            if (args.Length > 1 && args[1] == "--model-preview")
            {
                var declaration = KibiHex.MathsGen.Model.VectorDeclarations.Float2();
                var renderer = new KibiHex.MathsGen.Model.Rendering.CSharpRenderer();
                Console.WriteLine(renderer.Render(declaration));
                return;
            }

            GenerateDeclarativeVectors(args[0]);
        }

        private static void GenerateDeclarativeVectors(string folder)
        {
            var types = new[] { "bool", "int", "uint", "float", "double", "fix" }
                .SelectMany(scalar => new[] { 2, 3, 4 }.Select(dimension =>
                    new Model.VectorFamily { ScalarName = scalar, Dimension = dimension }.Create()))
                .ToArray();
            var layout = new Model.Rendering.TypeFileLayout();

            foreach (var type in types)
            foreach (var file in layout.Render(type))
            {
                var path = Path.Combine(folder, file.Name);
                new FileInfo(path).Directory?.Create();
                File.WriteAllText(path, file.Source);
                Console.WriteLine("    WROTE " + path);
            }

            var maths = new Model.Rendering.ShaderMathsRenderer();
            if (maths.CanRender(types))
            {
                var path = Path.Combine(folder, "maths.vectors.cs");
                File.WriteAllText(path, maths.Render(types));
                Console.WriteLine("    WROTE " + path);
            }

            var mathsSources = Directory.GetFiles(Directory.GetParent(folder)!.FullName, "Maths*.cs", SearchOption.TopDirectoryOnly);
            var scalarMethods = new Model.ScalarMathsScanner().Scan(mathsSources);
            var scalarMaths = new Model.Rendering.ScalarMathsRenderer().Render(scalarMethods);
            var scalarMathsPath = Path.Combine(folder, "maths.cs");
            File.WriteAllText(scalarMathsPath, scalarMaths);
            Console.WriteLine("    WROTE " + scalarMathsPath);
        }
    }
}
