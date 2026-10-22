using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalindromDemo.Helpers;
using PalindromDemo.Models;
using PalindromDemo.Services;

namespace PalindromDemo.ViewModels;

public enum AnimationSpeed { Slow, Normal, Fast }
public enum CheckResult { Unknown, IsPalindrome, NotPalindrome }

public partial class MainViewModel : ObservableObject
{
    public const double CharWidth = 32.0;
    private const int MaxVisualizedChars = 200;
    private const int MaxTreeNodes = 1000;

    private CancellationTokenSource? _cts;
    private string _currentNormalized = "";
    private int _visualizedCount;
    private long _checkStartTimestamp;

    private readonly Stack<RecursionNodeViewModel?> _nodeStack = new();
    private int _currentDepth;
    private bool _treeOverflow;

    public MainViewModel()
    {
        Characters.CollectionChanged += OnCharactersChanged;
    }

    private void OnCharactersChanged(object? s, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(TotalWidth));

    public double TotalWidth => Characters.Count * CharWidth;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _ignoreCase = true;
    [ObservableProperty] private bool _ignorePunctuation = true;
    [ObservableProperty] private bool _showTrace = true;
    [ObservableProperty] private bool _animateCheck = true;
    [ObservableProperty] private bool _showRecursionTree = true;
    [ObservableProperty] private bool _showStepTiming = false;
    [ObservableProperty] private AnimationSpeed _animationSpeed = AnimationSpeed.Normal;
    [ObservableProperty] private string _normalizedText = "";
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private CheckResult _result = CheckResult.Unknown;
    [ObservableProperty] private int _callCount = 0;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private double _leftArrowX;
    [ObservableProperty] private double _rightArrowX;
    [ObservableProperty] private bool _arrowsFinished;
    [ObservableProperty] private bool _arrowsMismatch;
    [ObservableProperty] private bool _arrowsVisible;

    [ObservableProperty] private double _algorithmElapsedMs;
    [ObservableProperty] private double _totalElapsedMs;
    [ObservableProperty] private string _algorithmTimeText = "";
    [ObservableProperty] private string _totalTimeText = "";
    [ObservableProperty] private string _referenceTimeText = "";
    [ObservableProperty] private string? _treeOverflowMessage;

    public ObservableCollection<CharViewModel> Characters { get; } = new();
    public ObservableCollection<TraceEntry> Trace { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();
    public ObservableCollection<RecursionNodeViewModel> RecursionNodes { get; } = new();

    private bool CanCheck() => !IsBusy;
    partial void OnIsBusyChanged(bool value) => CheckCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckAsync()
    {
        ErrorMessage = null;
        Trace.Clear();
        Characters.Clear();
        RecursionNodes.Clear();
        _nodeStack.Clear();
        _currentDepth = 0;
        _treeOverflow = false;
        TreeOverflowMessage = null;
        ArrowsVisible = ArrowsFinished = ArrowsMismatch = false;
        Result = CheckResult.Unknown;
        ResultText = "";
        CallCount = 0;
        AlgorithmElapsedMs = TotalElapsedMs = 0;
        AlgorithmTimeText = TotalTimeText = ReferenceTimeText = "";

        if (string.IsNullOrWhiteSpace(InputText))
        {
            ErrorMessage = "Введите строку для проверки";
            return;
        }

        var normalized = StringNormalizer.Normalize(InputText, IgnoreCase, IgnorePunctuation);
        NormalizedText = normalized;
        _currentNormalized = normalized;

        if (normalized.Length == 0)
        {
            Result = CheckResult.IsPalindrome;
            ResultText = "Является палиндромом";
            return;
        }

        _visualizedCount = Math.Min(normalized.Length, MaxVisualizedChars);
        for (int i = 0; i < _visualizedCount; i++)
            Characters.Add(new CharViewModel(i, normalized[i]));

        LeftArrowX = 0;
        RightArrowX = (_visualizedCount - 1) * CharWidth;
        ArrowsVisible = true;

        IsBusy = true;
        _cts = new CancellationTokenSource();
        var totalSw = Stopwatch.StartNew();
        _checkStartTimestamp = Stopwatch.GetTimestamp();

        try
        {
            int baseDelay = AnimationSpeed switch
            {
                AnimationSpeed.Slow => 600,
                AnimationSpeed.Normal => 300,
                AnimationSpeed.Fast => 100,
                _ => 300
            };
            int delay = (!AnimateCheck || normalized.Length > 500) ? 0
                : normalized.Length > 100 ? Math.Max(20, baseDelay * 100 / normalized.Length)
                : baseDelay;

            var animator = new PalindromeAnimator();
            bool isPalindrome = await animator.CheckAsync(normalized, delay, OnStepAsync, _cts.Token);
            totalSw.Stop();

            CallCount = animator.CallCount;
            AlgorithmElapsedMs = animator.AlgorithmElapsedMs;
            TotalElapsedMs = totalSw.Elapsed.TotalMilliseconds;
            AlgorithmTimeText = "Время алгоритма: " + TimeFormatter.FormatMs(AlgorithmElapsedMs);
            TotalTimeText = "Полное время: " + TimeFormatter.FormatMs(TotalElapsedMs);

            Result = isPalindrome ? CheckResult.IsPalindrome : CheckResult.NotPalindrome;
            ResultText = isPalindrome ? "Является палиндромом" : "Не является палиндромом";

            History.Insert(0, new HistoryEntry(InputText, normalized, isPalindrome,
                DateTime.Now, AlgorithmElapsedMs));
            while (History.Count > 10) History.RemoveAt(History.Count - 1);
        }
        catch (OperationCanceledException) { AlgorithmTimeText = TotalTimeText = "—"; }
        catch (Exception ex) { ErrorMessage = "Ошибка: " + ex.Message; }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private Task OnStepAsync(CheckStep step)
    {
        UpdateCharacters(step);
        UpdateArrows(step);
        UpdateTree(step);
        UpdateTrace(step);
        return Task.CompletedTask;
    }

    private void UpdateCharacters(CheckStep step)
    {
        if (step.Type == StepType.Returned) return;
        var state = step.Type switch
        {
            StepType.Comparing => CharState.Comparing,
            StepType.Matched => CharState.Matched,
            StepType.Mismatched => CharState.Mismatched,
            StepType.BaseCase => CharState.Matched,
            _ => CharState.Normal
        };
        if (step.Left >= 0 && step.Left < _visualizedCount) Characters[step.Left].State = state;
        if (step.Right >= 0 && step.Right < _visualizedCount && step.Right != step.Left)
            Characters[step.Right].State = state;
    }

    private void UpdateArrows(CheckStep step)
    {
        if (step.Type == StepType.Returned) return;
        int leftViz = Math.Min(step.Left, _visualizedCount - 1);
        int rightViz = Math.Min(step.Right, _visualizedCount - 1);
        LeftArrowX = leftViz * CharWidth;
        RightArrowX = rightViz * CharWidth;
        if (step.Type == StepType.BaseCase) ArrowsFinished = true;
        if (step.Type == StepType.Mismatched) ArrowsMismatch = true;
    }

    private void UpdateTree(CheckStep step)
    {
        if (!ShowRecursionTree) return;

        switch (step.Type)
        {
            case StepType.Comparing:
            case StepType.BaseCase:
                {
                    _currentDepth++;
                    if (RecursionNodes.Count < MaxTreeNodes)
                    {
                        char lc = step.Left < _currentNormalized.Length ? _currentNormalized[step.Left] : '?';
                        char rc = step.Right >= 0 && step.Right < _currentNormalized.Length
                            ? _currentNormalized[step.Right] : '?';
                        var node = new RecursionNodeViewModel(_currentDepth - 1, step.Left, step.Right, lc, rc);
                        if (step.Type == StepType.BaseCase) node.State = NodeState.BaseCase;
                        RecursionNodes.Add(node);
                        _nodeStack.Push(node);
                    }
                    else
                    {
                        if (!_treeOverflow)
                        {
                            _treeOverflow = true;
                            TreeOverflowMessage =
                                $"Дерево превысило {MaxTreeNodes} узлов — дальнейшее построение приостановлено";
                        }
                        _nodeStack.Push(null);
                    }
                    break;
                }
            case StepType.Matched:
            case StepType.Mismatched:
                {
                    if (_nodeStack.Count == 0) break;
                    var top = _nodeStack.Peek();
                    if (top == null) break;
                    top.State = step.Type == StepType.Matched ? NodeState.Matched : NodeState.Mismatched;
                    break;
                }
            case StepType.Returned:
                {
                    _currentDepth--;
                    if (_nodeStack.Count == 0) break;
                    var popped = _nodeStack.Pop();
                    if (popped == null) break;
                    popped.Result = step.Result;
                    popped.StepElapsedMs = step.StepTicks * 1000.0 / Stopwatch.Frequency;
                    popped.State = NodeState.Returned;
                    break;
                }
        }
    }

    private void UpdateTrace(CheckStep step)
    {
        if (!ShowTrace || step.Type == StepType.Comparing || step.Type == StepType.Returned) return;

        char lc = step.Left >= 0 && step.Left < _currentNormalized.Length ? _currentNormalized[step.Left] : '?';
        char rc = step.Right >= 0 && step.Right < _currentNormalized.Length ? _currentNormalized[step.Right] : '?';

        string baseText = step.Type switch
        {
            StepType.Matched => $"IsPalindrome({step.Left}, {step.Right}): '{lc}' == '{rc}' → рекурсия",
            StepType.Mismatched => $"IsPalindrome({step.Left}, {step.Right}): '{lc}' != '{rc}' → false",
            StepType.BaseCase => $"IsPalindrome({step.Left}, {step.Right}): база рекурсии → true",
            _ => ""
        };
        if (string.IsNullOrEmpty(baseText)) return;

        if (ShowStepTiming)
        {
            double relMs = (Stopwatch.GetTimestamp() - _checkStartTimestamp) * 1000.0 / Stopwatch.Frequency;
            baseText = $"[+{relMs:F2} мс] " + baseText;
        }
        Trace.Add(new TraceEntry(baseText));
    }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void Measure()
    {
        ErrorMessage = null;
        ReferenceTimeText = "";
        if (string.IsNullOrWhiteSpace(InputText))
        {
            ErrorMessage = "Введите строку для замера";
            return;
        }
        var normalized = StringNormalizer.Normalize(InputText, IgnoreCase, IgnorePunctuation);
        if (normalized.Length == 0) return;

        var checker = new PalindromeChecker();
        checker.IsPalindrome(normalized); // прогрев JIT

        const int runs = 3;
        double sum = 0;
        for (int i = 0; i < runs; i++)
        {
            checker.IsPalindrome(normalized);
            sum += checker.ElapsedMs;
        }
        var avg = sum / runs;
        ReferenceTimeText =
            $"Среднее за {runs} прогона (без анимации): {TimeFormatter.FormatMs(avg)}";
    }

    [RelayCommand]
    private void Clear()
    {
        _cts?.Cancel();
        InputText = "";
        NormalizedText = "";
        _currentNormalized = "";
        Result = CheckResult.Unknown;
        ResultText = "";
        CallCount = 0;
        ErrorMessage = null;
        AlgorithmElapsedMs = TotalElapsedMs = 0;
        AlgorithmTimeText = TotalTimeText = ReferenceTimeText = "";
        TreeOverflowMessage = null;
        Trace.Clear();
        Characters.Clear();
        RecursionNodes.Clear();
        _nodeStack.Clear();
        _currentDepth = 0;
        _treeOverflow = false;
        ArrowsVisible = ArrowsFinished = ArrowsMismatch = false;
        LeftArrowX = RightArrowX = 0;
    }

    [RelayCommand]
    private void ClearHistory() => History.Clear();
}