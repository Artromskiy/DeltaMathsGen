using KibiHex.MathsGen.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KibiHex.MathsGen
{
    internal static class MathsFacade
    {
        public static string Render(IEnumerable<AbstractType> types)
        {
            var writer = new StringBuilder();
            writer.AppendLine("#pragma warning disable IDE1006");
            writer.AppendLine("#nullable enable");
            writer.AppendLine("using System.Runtime.CompilerServices;");
            writer.AppendLine();
            writer.AppendLine("namespace KibiHex");
            writer.AppendLine("{");
            writer.AppendLine("    public static partial class maths");
            writer.AppendLine("    {");

            foreach (var vector in types.OfType<VectorType>().Where(vector =>
                vector.BaseType.Name == "float" ||
                vector.BaseType.Name == "double" ||
                vector.BaseType.Name == "fix"))
            {
                writer.AppendLine();
                writer.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                writer.AppendLine($"        public static {vector.Name} lerp({vector.Name} edge0, {vector.Name} edge1, {vector.Name} value) => {vector.Name}.Lerp(edge0, edge1, value);");
                writer.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                writer.AppendLine($"        public static {vector.Name} lerp({vector.Name} edge0, {vector.Name} edge1, {vector.BaseTypeName} value) => {vector.Name}.Lerp(edge0, edge1, value);");
                writer.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                writer.AppendLine($"        public static {vector.BaseTypeName} dot({vector.Name} left, {vector.Name} right) => {vector.Name}.Dot(left, right);");
                writer.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                writer.AppendLine($"        public static {vector.Name} normalize({vector.Name} value) => {vector.Name}.Normalize(value);");
            }

            writer.AppendLine("    }");
            writer.AppendLine("}");
            return writer.ToString();
        }
    }
}

