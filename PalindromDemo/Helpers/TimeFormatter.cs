namespace PalindromDemo.Helpers;

public static class TimeFormatter
{
    /// <summary>Форматирует время в мс/мкс/с с двумя знаками.</summary>
    public static string FormatMs(double ms)
    {
        if (double.IsNaN(ms) || ms < 0) return "—";
        if (ms < 1.0) return $"{ms * 1000.0:F2} мкс";
        if (ms >= 1000.0) return $"{ms / 1000.0:F2} с";
        return $"{ms:F2} мс";
    }

    public static string FormatMs3(double ms)
        => double.IsNaN(ms) || ms <= 0 ? "—" : $"{ms:F3} мс";
}