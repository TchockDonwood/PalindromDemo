using PalindromDemo.Models;

namespace PalindromDemo.Services;

/// <summary>
/// Асинхронная обёртка над рекурсивным алгоритмом, публикующая шаги через callback.
/// </summary>
public class PalindromeAnimator
{
    public int CallCount { get; private set; }

    public async Task<bool> CheckAsync(string s, int delayMs, Func<CheckStep, Task> onStep,
        CancellationToken ct = default)
    {
        CallCount = 0;
        return await CheckRecursiveAsync(s, 0, s.Length - 1, delayMs, onStep, ct);
    }

    private async Task<bool> CheckRecursiveAsync(string s, int left, int right, int delayMs,
        Func<CheckStep, Task> onStep, CancellationToken ct)
    {
        CallCount++;
        ct.ThrowIfCancellationRequested();

        if (left >= right)
        {
            await onStep(new CheckStep(left, right, StepType.BaseCase));
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            return true;
        }

        await onStep(new CheckStep(left, right, StepType.Comparing));
        if (delayMs > 0) await Task.Delay(delayMs, ct);

        if (s[left] != s[right])
        {
            await onStep(new CheckStep(left, right, StepType.Mismatched));
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            return false;
        }

        await onStep(new CheckStep(left, right, StepType.Matched));
        if (delayMs > 0) await Task.Delay(delayMs, ct);

        return await CheckRecursiveAsync(s, left + 1, right - 1, delayMs, onStep, ct);
    }
}