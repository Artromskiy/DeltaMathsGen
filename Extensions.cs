using KibiHex.MathsGen.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KibiHex.MathsGen
{
    internal static class Extensions
    {
        public static bool WriteToFileIfChanged(this string source, string filename)
        {
            var current = File.Exists(filename) ? File.ReadAllText(filename) : null;
            if (source == current) return false;
            File.WriteAllText(filename, source);
            return true;
        }

        public static bool WriteToFileIfChanged(this IEnumerable<string> flines, string filename)
        {
            var lines = flines.ToArray();
            var currLines = File.Exists(filename) ? File.ReadAllLines(filename) : new string[] { };
            if (lines.SequenceEqual(currLines)) return false;
            File.WriteAllLines(filename, lines);
            return true;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
            return source;
        }

        public static string ParameterNameExtract(this string s)
        {
            return s.Split('=')[0].Trim().Split(' ').Last().Trim();
        }

        public static string[] ArgNames(this IEnumerable<string> paras) => paras.CommaSeparated().Split(',').Select(p => p.ParameterNameExtract()).ToArray();
        public static string[] ParasRecovered(this IEnumerable<string> paras) => paras.CommaSeparated().Split(',').ToArray();

        private static string NestedSymmetricFunction(IReadOnlyList<string> fields, string funcFormat, int start, int end)
        {
            if (start == end)
                return fields[start];

            var mid = (start + end) / 2;
            return string.Format(funcFormat,
                NestedSymmetricFunction(fields, funcFormat, start, mid),
                NestedSymmetricFunction(fields, funcFormat, mid + 1, end));
        }

        private static string NestedSymmetricFunction(IEnumerable<string> ffs, string funcFormat)
        {
            var fs = ffs.ToArray();
            return NestedSymmetricFunction(fs, funcFormat, 0, fs.Length - 1);
        }

        public static string Indent(this string s, int lvl = 1)
        {
            return new string(' ', lvl * 4) + s;
        }

        public static string[] TypedArgs(this IEnumerable<string> ss, AbstractType type)
        {
            return ss.Select(s => type.Name + " " + s).ToArray();
        }

        public static string CommaSeparated<T>(this IEnumerable<T> coll)
        {
            var cc = coll.Select(c => c.ToString()).ToArray();
            return cc.Length == 0 ? "" : cc.Aggregate((s1, s2) => s1 + ", " + s2);
        }
        public static string Aggregated<T>(this IEnumerable<T> coll, string seperator)
        {
            var cc = coll.Select(c => c.ToString()).ToArray();
            return cc.Length == 0 ? "" : NestedSymmetricFunction(cc, "({0}" + seperator + "{1})");
        }

        public static string[] LhsRhs(this AbstractType type) => new[]
        {
            type.Name + " lhs",
            type.Name + " rhs"
        };

        public static string[] TypedArgs(this AbstractType type, params string[] args)
        {
            return args.Select(a => type.Name + " " + a).ToArray();
        }

        public static string[] SConcat(this IEnumerable<string> coll, params string[] args)
        {
            return coll.Concat(args).ToArray();
        }

        public static string Capitalized(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static IEnumerable<string> AsComment(this string s, bool withTrailingEmptyLine = true)
        {
            if (withTrailingEmptyLine)
                yield return "";
            if (string.IsNullOrEmpty(s))
                yield break;
            yield return "/// <summary>";
            yield return "/// " + s;
            yield return "/// </summary>";
        }

        public static string[] RepeatTimes(this string s, int times)
        {
            var result = new string[times];
            for (var i = 0; i < times; ++i)
                result[i] = s;
            return result;
        }

        public static string[] DotComp(this string s, int maxComp = 4)
        {
            var result = new string[maxComp];
            for (var i = 0; i < maxComp; ++i)
                result[i] = s + "." + "xyzw"[i];
            return result;
        }

        public static string[] ImpulseString(this int arg, string imp, string nonimp, int maxComp = 4)
        {
            var result = new string[maxComp];
            for (var i = 0; i < maxComp; ++i)
                result[i] = i == arg ? imp : nonimp;
            return result;
        }

        public static T[] ExactlyN<T>(this IEnumerable<T> coll, int n, T obj)
        {
            var result = new T[n];
            var it = coll.GetEnumerator();
            for (var i = 0; i < n; ++i)
                result[i] = it.MoveNext() ? it.Current : obj;
            return result;
        }

        public static T[] ForIndexUpTo<T>(this int n, Func<int, T> f)
        {
            var result = new T[n];
            for (var i = 0; i < n; ++i)
                result[i] = f(i);
            return result;
        }
    }
}

