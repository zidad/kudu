using System;
using System.Globalization;

namespace Kudu.Core
{
    public static class StringExtensions
    {
        public static string FormatInvariant(this string format, params object[] args)
        {
            return String.Format(CultureInfo.InvariantCulture, format, args);
        }

        public static string FormatCurrentCulture(this string format, params object[] args)
        {
            return String.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static string EscapeForFormat(this string format)
        {
            return format != null ? format.Replace("{", "{{").Replace("}", "}}") : null;
        }
    }
}
