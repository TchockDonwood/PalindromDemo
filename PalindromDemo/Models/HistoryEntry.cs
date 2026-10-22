namespace PalindromDemo.Models;

public record HistoryEntry(
    string Original,
    string Normalized,
    bool IsPalindrome,
    DateTime Timestamp,
    double AlgorithmElapsedMs)
{
    public string ResultText => IsPalindrome ? "Палиндром" : "Не палиндром";
    public string ElapsedText => AlgorithmElapsedMs.ToString("F2");
}