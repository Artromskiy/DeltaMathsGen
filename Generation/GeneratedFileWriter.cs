using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeltaMathsGen.Generation
{
    internal sealed record GeneratedSource(string Name, string Source);

    internal static class GeneratedFileWriter
    {
        private const string ManifestName = ".delta-generated-files";

        public static void Write(string folder, GeneratedSource[] sources)
        {
            var root = Path.GetFullPath(folder);
            Directory.CreateDirectory(root);

            var duplicate = sources.GroupBy(source => source.Name, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"Generated file '{duplicate.Key}' was declared more than once.");
            }

            var names = sources.Select(source => NormalizeName(root, source.Name)).ToArray();
            DeleteStaleFiles(root, names);

            for (var index = 0; index < sources.Length; index++)
            {
                var path = Path.Combine(root, names[index]);
                new FileInfo(path).Directory?.Create();
                File.WriteAllText(path, sources[index].Source);
                Console.WriteLine("    WROTE " + path);
            }

            File.WriteAllLines(Path.Combine(root, ManifestName), names.OrderBy(name => name, StringComparer.Ordinal));
        }

        private static void DeleteStaleFiles(string root, string[] currentNames)
        {
            var manifest = Path.Combine(root, ManifestName);
            if (!File.Exists(manifest))
            {
                return;
            }

            var current = new HashSet<string>(currentNames, StringComparer.Ordinal);
            foreach (var previousName in File.ReadAllLines(manifest))
            {
                var normalized = NormalizeName(root, previousName);
                if (current.Contains(normalized))
                {
                    continue;
                }

                var path = Path.Combine(root, normalized);
                if (!File.Exists(path))
                {
                    continue;
                }

                File.Delete(path);
                Console.WriteLine("    REMOVED " + path);
            }
        }

        private static string NormalizeName(string root, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name))
            {
                throw new InvalidOperationException($"Invalid generated file name '{name}'.");
            }

            var path = Path.GetFullPath(Path.Combine(root, name));
            var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!path.StartsWith(rootPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Generated file '{name}' escapes the output directory.");
            }

            return Path.GetRelativePath(root, path);
        }
    }
}
