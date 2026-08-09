using System;
using System.Linq;
using KibiHex.MathsGen.CodeModel;

namespace KibiHex.MathsGen.Model.Rendering
{
    internal sealed class ExtensionRenderer
    {
        public bool CanRender(TypeSpec type) => ExtensionFunctions(type).Any();

        public string Render(TypeSpec type)
        {
            var functions = ExtensionFunctions(type).ToArray();
            if (functions.Length == 0)
                throw new InvalidOperationException($"Type {type.Name} has no extension functions.");

            var writer = new CodeWriter();
            writer.Line("#nullable enable");
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line();
            writer.Block("namespace KibiHex.Extensions", () =>
            {
                writer.Block($"public static partial class {Pascalize(type.Name)}Extensions", () =>
                {
                    foreach (var function in functions)
                    {
                        writer.Line();
                        RenderFunction(writer, function);
                    }
                });
            });
            return writer.ToString();
        }

        private static void RenderFunction(CodeWriter writer, FunctionSpec function)
        {
            var receiver = ResolveReceiver(function);
            var parameters = function.Parameters.Select(parameter =>
                parameter == receiver
                    ? $"this {parameter.Type} {parameter.Name}"
                    : $"{parameter.Modifier} {parameter.Type} {parameter.Name}".Trim());
            var arguments = string.Join(", ", function.Parameters.Select(parameter => parameter.Name));

            writer.Line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.Line($"public static {function.ReturnType} {function.Name}({string.Join(", ", parameters)}) => maths.{function.MathsName}({arguments});");
        }

        private static ParameterSpec ResolveReceiver(FunctionSpec function)
        {
            if (string.IsNullOrWhiteSpace(function.ExtensionReceiver))
                throw new InvalidOperationException($"Extension function {function.Name} has no receiver.");

            return function.Parameters.SingleOrDefault(parameter => parameter.Name == function.ExtensionReceiver)
                ?? throw new InvalidOperationException($"Receiver {function.ExtensionReceiver} is not a parameter of {function.Name}.");
        }

        private static FunctionSpec[] ExtensionFunctions(TypeSpec type) =>
            type.Members
                .OfType<FunctionSpec>()
                .Where(function => function.Api.HasFlag(ApiSurface.Extension))
                .ToArray();

        private static string Pascalize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return char.ToUpperInvariant(name[0]) + name[1..];
        }
    }
}
