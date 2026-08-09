using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace KibiHex.MathsGen.Validation
{
    public sealed class ApiTypeSurface
    {
        public ApiTypeSurface(string name, IEnumerable<string> members)
        {
            Name = name;
            Members = new HashSet<string>(members, StringComparer.Ordinal);
        }

        public string Name { get; }
        public IReadOnlyCollection<string> Members { get; }
    }

    public sealed class ApiSurface
    {
        public ApiSurface(IEnumerable<ApiTypeSurface> types)
        {
            Types = types.ToDictionary(x => x.Name, StringComparer.Ordinal);
        }

        public IReadOnlyDictionary<string, ApiTypeSurface> Types { get; }
    }

    public static class ApiSurfaceReader
    {
        private static readonly string[] Scalars = { "bool", "int", "uint", "float", "double", "fix" };

        public static ApiSurface Read(string assemblyPath)
        {
            if (assemblyPath == null) throw new ArgumentNullException(nameof(assemblyPath));
            var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
            var types = Scalars.SelectMany(scalar => Enumerable.Range(2, 3).Select(dimension => scalar + dimension));
            return new ApiSurface(types.Select(name => ReadType(assembly, name)));
        }

        private static ApiTypeSurface ReadType(Assembly assembly, string name)
        {
            var type = assembly.GetType(name) ?? assembly.GetType(assembly.GetName().Name + "." + name)
                ?? assembly.GetTypes().SingleOrDefault(candidate => candidate.Name == name);
            if (type == null)
                return new ApiTypeSurface(name, Array.Empty<string>());

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var members = type.GetMembers(flags)
                .Where(member => member.MemberType != MemberTypes.Constructor || ((ConstructorInfo)member).IsPublic)
                .Where(member => member.MemberType != MemberTypes.Method || !((MethodInfo)member).IsSpecialName || ((MethodInfo)member).Name.StartsWith("op_", StringComparison.Ordinal))
                .Where(member => member.MemberType != MemberTypes.Property || ((PropertyInfo)member).GetMethod?.IsPublic == true || ((PropertyInfo)member).SetMethod?.IsPublic == true)
                .Select(Signature)
                .OrderBy(x => x, StringComparer.Ordinal);
            return new ApiTypeSurface(name, members);
        }

        private static string Signature(MemberInfo member)
        {
            return member switch
            {
                ConstructorInfo constructor => "ctor(" + string.Join(",", constructor.GetParameters().Select(x => TypeName(x.ParameterType))) + ")",
                MethodInfo method => "method " + method.Name + "(" + string.Join(",", method.GetParameters().Select(x => TypeName(x.ParameterType))) + "):" + TypeName(method.ReturnType),
                PropertyInfo property => "property " + property.Name + "(" + string.Join(",", property.GetIndexParameters().Select(x => TypeName(x.ParameterType))) + "):" + TypeName(property.PropertyType),
                FieldInfo field => "field " + field.Name + ":" + TypeName(field.FieldType),
                EventInfo @event => "event " + @event.Name + ":" + TypeName(@event.EventHandlerType!),
                _ => throw new InvalidOperationException("Unsupported public member: " + member.MemberType)
            };
        }

        private static string TypeName(Type type)
        {
            if (type.IsByRef) return TypeName(type.GetElementType()!) + "&";
            if (type.IsArray) return TypeName(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            if (!type.IsGenericType) return type.FullName ?? type.Name;
            var tick = type.GetGenericTypeDefinition().FullName!.IndexOf('`');
            return type.GetGenericTypeDefinition().FullName!.Substring(0, tick) + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
        }
    }
}
