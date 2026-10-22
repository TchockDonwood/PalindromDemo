namespace PalindromDemo.Models;

public enum StepType { Comparing, Matched, Mismatched, BaseCase, Returned }

public record CheckStep(
    int Left,
    int Right,
    StepType Type,
    bool? Result = null,
    long StepTicks = 0);