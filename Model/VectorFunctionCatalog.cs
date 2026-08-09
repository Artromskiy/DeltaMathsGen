using System;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Model
{
    internal static class VectorFunctionCatalog
    {
        public static MemberSpec[] Create(string scalarName, int dimension)
        {
            if (scalarName is not ("float" or "double" or "fix") || dimension is < 2 or > 4)
                return Array.Empty<MemberSpec>();

            var vector = TypeRef.Named(scalarName + dimension);
            var scalar = TypeRef.Named(scalarName);
            var fields = "xyzw"[..dimension].Select(c => c.ToString()).ToArray();
            var members = new List<MemberSpec>();

            AddComponentWise(members, "Clamp", vector, [P("value", vector), P("min", scalar), P("max", scalar)], fields,
                f => $"Maths.Clamp(value.{f}, min, max)");

            Add(members, "Length", scalar, [P("value", vector)], $"return Maths.Sqrt(SqrLength(value));");
            Add(members, "Distance", scalar, [P("a", vector), P("b", vector)], "return Length(a - b);");
            Add(members, "Dot", scalar, [P("a", vector), P("b", vector)], $"return {Sum(fields.Select(f => $"a.{f} * b.{f}"))};");
            if (dimension == 3)
                Add(members, "Cross", vector, [P("a", vector), P("b", vector)],
                    "return new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);");
            Add(members, "Normalize", vector, [P("value", vector)], "return value / Length(value);");
            Add(members, "FaceForward", vector, [P("N", vector), P("I", vector), P("Nref", vector)], "return Dot(Nref, I) < 0 ? N : -N;");
            Add(members, "Reflect", vector, [P("I", vector), P("N", vector)], "return I - 2 * Dot(N, I) * N;");
            Add(members, "Refract", vector, [P("I", vector), P("N", vector), P("eta", scalar)],
                "var dNI = Dot(N, I);\nvar k = 1 - eta * eta * (1 - dNI * dNI);\nif (k < 0) return new();\nreturn eta * I - (eta * dNI + Maths.Sqrt(k)) * N;");

            Add(members, "SqrLength", scalar, [P("value", vector)], $"return {Sum(fields.Select(f => $"value.{f} * value.{f}"))};");
            Add(members, "SqrDistance", scalar, [P("a", vector), P("b", vector)], "return SqrLength(a - b);");
            Add(members, "ClampLength", vector, [P("value", vector), P("maxLength", scalar)],
                "var sqrLength = SqrLength(value);\nif (sqrLength > maxLength * maxLength)\n{\n    var ratio = maxLength * Maths.InverseSqrt(sqrLength);\n    return value * ratio;\n}\nreturn value;");
            Add(members, "MoveTowards", vector, [P("current", vector), P("target", vector), P("maxDelta", scalar)],
                "var delta = target - current;\nvar sqrDistance = SqrLength(delta);\nreturn sqrDistance <= maxDelta * maxDelta ? target : current + delta * maxDelta * Maths.InverseSqrt(sqrDistance);");

            AddComponentWise(members, "Abs", vector, [P("value", vector)], fields, f => $"Maths.Abs(value.{f})");
            AddComponentWise(members, "Sign", vector, [P("value", vector)], fields, f => $"Maths.Sign(value.{f})");
            AddComponentWise(members, "Lerp", vector, [P("a", vector), P("b", vector), P("t", scalar)], fields, f => $"Maths.Lerp(a.{f}, b.{f}, t)");
            AddComponentWise(members, "Lerp", vector, [P("a", vector), P("b", vector), P("t", vector)], fields, f => $"Maths.Lerp(a.{f}, b.{f}, t.{f})");
            AddComponentWise(members, "Min", vector, [P("a", vector), P("b", vector)], fields, f => $"Maths.Min(a.{f}, b.{f})");
            AddComponentWise(members, "Max", vector, [P("a", vector), P("b", vector)], fields, f => $"Maths.Max(a.{f}, b.{f})");
            AddComponentWise(members, "InvLerp", vector, [P("edge0", vector), P("edge1", vector), P("value", vector)], fields, f => $"Maths.InvLerp(edge0.{f}, edge1.{f}, value.{f})");
            AddComponentWise(members, "InvLerp", vector, [P("edge0", vector), P("edge1", vector), P("value", scalar)], fields, f => $"Maths.InvLerp(edge0.{f}, edge1.{f}, value)");

            Add(members, "SmoothDamp", vector,
                [P("source", vector), P("target", vector), P("velocity", vector, "ref"), P("smoothTime", scalar), P("deltaTime", scalar)],
                $"return new({string.Join(", ", fields.Select(f => $"Maths.SmoothDamp(source.{f}, target.{f}, ref velocity.{f}, smoothTime, deltaTime)"))});");

            if (scalarName == "float")
                foreach (var name in new[] { "Pow", "Exp", "Log", "Exp2", "Log2" })
                    AddComponentWise(members, name, vector, name == "Pow" ? [P("a", vector), P("b", vector)] : [P("value", vector)], fields,
                        f => name == "Pow" ? $"Maths.Pow(a.{f}, b.{f})" : $"Maths.{name}(value.{f})");
            if (scalarName is "float" or "double")
                foreach (var name in new[] { "Sqrt", "InverseSqrt" })
                    AddComponentWise(members, name, vector, [P("value", vector)], fields, f => $"Maths.{name}(value.{f})");

            return members.ToArray();
        }

        private static void Add(List<MemberSpec> result, string name, TypeRef returnType, ParameterSpec[] parameters, string body) => result.Add(new FunctionSpec { Name = name, ReturnType = returnType, Parameters = parameters, Body = body, Modifiers = Modifiers.Public | Modifiers.Static, Api = ApiSurface.Vector, Part = TypePart.Geometry });
        private static void AddComponentWise(List<MemberSpec> result, string name, TypeRef type, ParameterSpec[] parameters, string[] fields, Func<string, string> expression) => Add(result, name, type, parameters, $"return new({string.Join(", ", fields.Select(expression))});");
        private static ParameterSpec P(string name, TypeRef type, string modifier = null) => new() { Name = name, Type = type, Modifier = modifier };
        private static string Sum(IEnumerable<string> terms) => string.Join(" + ", terms);
    }
}
