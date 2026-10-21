namespace PalindromDemo.Services;

/// <summary>Классическая рекурсивная проверка строки на палиндром.</summary>
public class PalindromeChecker
{
    public int CallCount { get; private set; }

    public bool IsPalindrome(string s)
    {
        CallCount = 0;
        return IsPalindromeRecursive(s, 0, s.Length - 1);
    }

    private bool IsPalindromeRecursive(string s, int left, int right)
    {
        CallCount++;
        if (left >= right) return true;
        if (s[left] != s[right]) return false;
        return IsPalindromeRecursive(s, left + 1, right - 1);
    }
}