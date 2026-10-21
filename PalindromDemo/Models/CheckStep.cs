namespace PalindromDemo.Models;

public enum StepType { Comparing, Matched, Mismatched, BaseCase }

public record CheckStep(int Left, int Right, StepType Type);