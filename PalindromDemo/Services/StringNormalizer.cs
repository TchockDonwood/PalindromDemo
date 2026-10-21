using System.Text;

namespace PalindromDemo.Services;

public static class StringNormalizer
{
    public static string Normalize(string input, bool ignoreCase, bool ignorePunctuation)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ignorePunctuation && (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch)))
                continue;
            sb.Append(ignoreCase ? char.ToLowerInvariant(ch) : ch);
        }
        return sb.ToString();
    }
}