using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Auger
{
    public static class MiscHelpers
    {
        public static string GetName(this IPrincipal principal)
        {
            return principal?.Identity?.Name.Replace('\\', '_');
        }

        public static bool EqualsIgnoreCase(this string self, string other)
        {
            return self.Equals(other, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this IEnumerable<string> self, string value)
        {
            return self.Contains(value, StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool EqualsWildcard(this string self, string wildcardString)
        {
            var pattern = wildcardString.Replace(".", @"\.");
            pattern = pattern.Replace("?", ".");
            pattern = pattern.Replace("*", ".*?");
            pattern = pattern.Replace(@"\", @"\\");
            pattern = pattern.Replace(" ", @"\s");
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(self);
        }

        private class WildcardComparer : StringComparer
        {
            public override int Compare(string x, string y)
            {
                return x.EqualsWildcard(y) ? 0 : StringComparer.InvariantCultureIgnoreCase.Compare(x,y);
            }

            public override bool Equals(string x, string y)
            {
                return x.EqualsWildcard(y);
            }

            public override int GetHashCode(string obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj);
            }
        }

        private static WildcardComparer _wildcardComparer = new WildcardComparer();

        public static bool ContainsWildcard(this IEnumerable<string> self, string value)
        {
            return self.Contains(value, _wildcardComparer);
        }

        public static void CopyTo(this DirectoryInfo source, DirectoryInfo target, bool includeHidden = false)
        {
            foreach (var dir in source.GetDirectories())
            {
                if (includeHidden || (!dir.Attributes.HasFlag(FileAttributes.Hidden) && !dir.Name.StartsWith(".")))
                {
                    dir.CopyTo(target.CreateSubdirectory(dir.Name));
                }
            }
            foreach (var file in source.GetFiles())
            {
                if (includeHidden || (!file.Attributes.HasFlag(FileAttributes.Hidden) && !file.Name.StartsWith(".")))
                {
                    file.CopyTo(Path.Combine(target.FullName, file.Name), true);
                }
            }
        }

        public static void CopyTo(this DirectoryInfo source, string targetPath, bool includeHidden = false)
        {
            source.CopyTo(Directory.CreateDirectory(targetPath), includeHidden);
        }

        public static Stream GetAsZipStream(this DirectoryInfo source, bool includeHidden = false)
        {
            var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                source.CopyTo(zip, "", includeHidden);
            }
            stream.Seek(0, 0);
            return stream;
        }

        public static void CopyTo(this DirectoryInfo source, ZipArchive zip, string path = "", bool includeHidden = false)
        {
            foreach (var dir in source.GetDirectories())
            {
                if (includeHidden || (!dir.Attributes.HasFlag(FileAttributes.Hidden) && !dir.Name.StartsWith(".")))
                {
                    dir.CopyTo(zip, path + dir.Name + "\\", includeHidden);
                }
            }
            foreach (var file in source.GetFiles())
            {
                if (includeHidden || (!file.Attributes.HasFlag(FileAttributes.Hidden) && !file.Name.StartsWith(".")))
                {
                    zip.CreateEntryFromFile(file.FullName, path + file.Name);
                    //var entry = zip.CreateEntry(path + file.Name);
                    //using (var instr = file.OpenRead())
                    //{
                    //    using (var outstr = entry.Open())
                    //    {
                    //        instr.CopyTo(outstr);
                    //    }
                    //}
                }
            }
        }

    }
}
