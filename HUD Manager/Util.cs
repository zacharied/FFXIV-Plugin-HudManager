using System.Globalization;

namespace HUD_Manager {
    public static class Util {
        public static bool ContainsIgnoreCase(this string haystack, string needle) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }
    }
}
