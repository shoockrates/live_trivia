namespace live_trivia.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string? str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrWhiteSpace(this string? str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static string CapitalizeFirstLetter(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}