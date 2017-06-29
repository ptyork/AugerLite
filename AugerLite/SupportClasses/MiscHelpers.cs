using System;
using System.Collections.Generic;
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
    }
}
