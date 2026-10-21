namespace PalindromDemo.Models;

public record HistoryEntry(string Original, string Normalized, bool IsPalindrome, DateTime Timestamp)
{
    public string ResultText => IsPalindrome ? "Палиндром" : "Не палиндром";
}