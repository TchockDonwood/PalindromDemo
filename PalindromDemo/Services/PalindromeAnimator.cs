using System.Diagnostics;
using PalindromDemo.Models;

namespace PalindromDemo.Services;

public class PalindromeAnimator
{
    public int CallCount { get; private set; }
    public double AlgorithmElapsedMs { get; private set; }

    private long _totalDelayTicks;

    public async Task<bool> CheckAsync(string s, int delayMs, Func<CheckStep, Task> onStep,
        CancellationToken ct = default)
    {
        CallCount = 0;
        _totalDelayTicks = 0;
        var sw = Stopwatch.StartNew();

        var result = await CheckRecursiveAsync(s, 0, s.Length - 1, delayMs, onStep, ct);

        sw.Stop();
        // ↓ Переименовано: delayTotalMs вместо delayMs
        var delayTotalMs = _totalDelayTicks * 1000.0 / Stopwatch.Frequency;
        AlgorithmElapsedMs = Math.Max(0, sw.Elapsed.TotalMilliseconds - delayTotalMs);
        return result;
    }

    private async Task<bool> CheckRecursiveAsync(string s, int left, int right, int delayMs,
        Func<CheckStep, Task> onStep, CancellationToken ct)
    {
        CallCount++;
        ct.ThrowIfCancellationRequested();

        long start = Stopwatch.GetTimestamp();
        long localDelayTicks = 0;

        async Task DelayAsync()
        {
            if (delayMs <= 0) return;
            long d = Stopwatch.GetTimestamp();
            await Task.Delay(delayMs, ct);
            long delta = Stopwatch.GetTimestamp() - d;
            localDelayTicks += delta;
            _totalDelayTicks += delta;
        }

        if (left >= right)
        {
            await onStep(new CheckStep(left, right, StepType.BaseCase));
            await DelayAsync();
            long ticks = Stopwatch.GetTimestamp() - start - localDelayTicks;
            await onStep(new CheckStep(left, right, StepType.Returned, true, ticks));
            return true;
        }

        await onStep(new CheckStep(left, right, StepType.Comparing));
        await DelayAsync();

        if (s[left] != s[right])
        {
            await onStep(new CheckStep(left, right, StepType.Mismatched));
            await DelayAsync();
            long ticks = Stopwatch.GetTimestamp() - start - localDelayTicks;
            await onStep(new CheckStep(left, right, StepType.Returned, false, ticks));
            return false;
        }

        await onStep(new CheckStep(left, right, StepType.Matched));
        await DelayAsync();

        var childResult = await CheckRecursiveAsync(s, left + 1, right - 1, delayMs, onStep, ct);

        long nodeTicks = Stopwatch.GetTimestamp() - start - localDelayTicks;
        await onStep(new CheckStep(left, right, StepType.Returned, childResult, nodeTicks));
        return childResult;
    }
}