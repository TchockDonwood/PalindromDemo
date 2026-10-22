using System.Diagnostics;

namespace PalindromDemo.Services;

public class PalindromeChecker
{
    public int CallCount { get; private set; }
    public double ElapsedMs { get; private set; }

    public bool IsPalindrome(string s)
    {
        CallCount = 0;
        var sw = Stopwatch.StartNew();
        var result = IsPalindromeRecursive(s, 0, s.Length - 1);
        sw.Stop();
        ElapsedMs = sw.Elapsed.TotalMilliseconds;
        return result;
    }

    private bool IsPalindromeRecursive(string s, int left, int right)
    {
        CallCount++;
        if (left >= right) return true;
        if (s[left] != s[right]) return false;
        return IsPalindromeRecursive(s, left + 1, right - 1);
    }
}