using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using DVG.MathsGen.Generation;

namespace DVG.MathsGen
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: DVG.MathsGen <vectors-output-directory>");
                Environment.ExitCode = 2;
                return;
            }

            GenerateDeclarativeVectors(args[0]);
        }

        private static void GenerateDeclarativeVectors(string folder)
        {
            var typeList = new List<Model.TypeSpec>(Model.ScalarTypes.All.Length * 3);
            foreach (var scalar in Model.ScalarTypes.All)
            foreach (var dimension in new[] { 2, 3, 4 })
                typeList.Add(new Model.VectorFamily { Scalar = scalar, Dimension = dimension }.Create());
            var types = typeList.ToArray();
            Validation.ModelValidator.Validate(Model.ScalarTypes.All, types);
            var layout = new Model.Rendering.TypeFileLayout();
            var sources = new List<GeneratedSource>();

            foreach (var type in types)
            foreach (var file in layout.Render(type))
                sources.Add(new GeneratedSource(file.Name, file.Source));

            var maths = new Model.Rendering.ShaderMathsRenderer();
            if (maths.CanRender(types))
                sources.Add(new GeneratedSource("maths.vectors.cs", maths.Render(types)));

            var output = Path.GetFullPath(folder);
            var mathsFolder = Directory.GetParent(output)?.FullName
                ?? throw new InvalidOperationException("The vectors output directory must have a parent directory.");
            var mathsSources = Directory.GetFiles(mathsFolder, "Maths*.cs", SearchOption.TopDirectoryOnly);
            var scalarMethods = new Model.ScalarMathsScanner().Scan(mathsSources);
            var scalarMaths = new Model.Rendering.ScalarMathsRenderer().Render(scalarMethods);
            sources.Add(new GeneratedSource("maths.cs", scalarMaths));

            GeneratedFileWriter.Write(output, sources.ToArray());
        }
    }
}
