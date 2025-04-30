using System;
using System.Collections.Generic;
using System.Linq;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType : AbstractType
    {
        public override string GlslName => BaseType.Prefix + "vec" + Length;

        public VectorType(BuiltinType type, int comps)
        {
            Length = comps;
            BaseType = type;
        }

        public readonly int Length;
        public IEnumerable<string> Fields => "xyzw".Substring(0, Length).Select(c => c.ToString());
        public override string Name => GetName(BaseType, Length);
        public override string TypeComment => $"A vector of type {BaseTypeName} with {Length} components.";
        public override IEnumerable<string> BaseClasses => new string[] { $"IEquatable<{Name}>" };

        public string CompString => "xyzw".Substring(0, Length);
        public static char ArgOf(int c) => "xyzw"[c];
        public static char ArgOfRGBA(int c) => "rgba"[c];
        public static char ArgOfSTPQ(int c) => "stpq"[c];
        public static string ArgOfs(int c) => "xyzw"[c].ToString();
        public static char ArgOfUpper(int c) => char.ToUpper("xyzw"[c]);
        public static string GetName(BuiltinType type, int components) => type.Name + components;


        public IEnumerable<string> SubCompParameters(int start, int end) => "xyzw".Substring(start, end - start + 1).Select(c => BaseTypeName + " " + c);
        public string SubCompParameterString(int start, int end) => SubCompParameters(start, end).CommaSeparated();

        public static IEnumerable<string> SubCompArgs(int start, int end) => "xyzw".Substring(start, end - start + 1).Select(c => c.ToString());
        public static string NestedBiFuncFor(string format, int c, Func<int, string> argOf) => c == 0 ? argOf(c) : string.Format(format, NestedBiFuncFor(format, c - 1, argOf), argOf(c));

    }
}
