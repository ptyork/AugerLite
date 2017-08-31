using System;
using System.Collections.Generic;
using System.IO;
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

        public static bool EqualsWildcard(this string self, string wildcardString)
        {
            var pattern = wildcardString.Replace(".", @"\.");
            pattern = pattern.Replace("?", ".");
            pattern = pattern.Replace("*", ".*?");
            pattern = pattern.Replace(@"\", @"\\");
            pattern = pattern.Replace(" ", @"\s");
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(self);
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

    }
}
