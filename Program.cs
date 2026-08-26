using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using DeltaMathsGen.Generation;

namespace DeltaMathsGen
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: DeltaMathsGen <vectors-output-directory>");
                Environment.ExitCode = 2;
                return;
            }

            GenerateDeclarativeVectors(args[0]);
        }

        private static void GenerateDeclarativeVectors(string folder)
        {
            var typeList = new List<Model.TypeSpec>(Model.ScalarTypes.All.Length * 3 + 2);
            foreach (var scalar in Model.ScalarTypes.All)
            {
                foreach (var dimension in new[] { 2, 3, 4 })
                {
                    typeList.Add(new Model.VectorFamily { Scalar = scalar, Dimension = dimension }.Create());
                }
            }

            typeList.AddRange(Model.MatrixQuaternionDefinitions.Create());
            var types = typeList.ToArray();
            Validation.ModelValidator.Validate(Model.ScalarTypes.All, types);
            var layout = new Model.Rendering.TypeFileLayout();
            var sources = new List<GeneratedSource>();
            foreach (var type in types)
            {
                foreach (var file in layout.Render(type))
                {
                    sources.Add(new GeneratedSource(file.Name, file.Source));
                }
            }

            if (Model.Rendering.ShaderDeltaMathsRenderer.CanRender(types))
            {
                sources.Add(new GeneratedSource("maths.vectors.cs", Model.Rendering.ShaderDeltaMathsRenderer.Render(types)));
            }

            var output = Path.GetFullPath(folder);
            var mathsFolder = Directory.GetParent(output)?.FullName
                ?? throw new InvalidOperationException("The vectors output directory must have a parent directory.");
            var mathsSources = Directory.GetFiles(mathsFolder, "DeltaMaths*.cs", SearchOption.TopDirectoryOnly);
            var scalarMethods = new Model.ScalarDeltaMathsScanner().Scan(mathsSources);
            var scalarDeltaMaths = new Model.Rendering.ScalarDeltaMathsRenderer().Render(scalarMethods);
            sources.Add(new GeneratedSource("maths.cs", scalarDeltaMaths));
            sources.Add(new GeneratedSource("shader-contract.json", Model.Rendering.ShaderContractManifestRenderer.Render(types)));

            GeneratedFileWriter.Write(output, sources.ToArray());
        }
    }
}
