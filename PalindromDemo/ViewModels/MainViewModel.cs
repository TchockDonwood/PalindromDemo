using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PalindromDemo.Models;
using PalindromDemo.Services;

namespace PalindromDemo.ViewModels;

public enum AnimationSpeed { Slow, Normal, Fast }
public enum CheckResult { Unknown, IsPalindrome, NotPalindrome }

public partial class MainViewModel : ObservableObject
{
    public const double CharWidth = 32.0;
    public const double CharHeight = 40.0;
    private const int MaxVisualizedChars = 200;

    private CancellationTokenSource? _cts;
    private string _currentNormalized = "";
    private int _visualizedCount;

    public MainViewModel()
    {
        Characters.CollectionChanged += OnCharactersChanged;
    }

    private void OnCharactersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(TotalWidth));

    public double TotalWidth => Characters.Count * CharWidth;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _ignoreCase = true;
    [ObservableProperty] private bool _ignorePunctuation = true;
    [ObservableProperty] private bool _showTrace = true;
    [ObservableProperty] private bool _animateCheck = true;
    [ObservableProperty] private AnimationSpeed _animationSpeed = AnimationSpeed.Normal;
    [ObservableProperty] private string _normalizedText = "";
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private CheckResult _result = CheckResult.Unknown;
    [ObservableProperty] private int _callCount = 0;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private double _leftArrowX = 0;
    [ObservableProperty] private double _rightArrowX = 0;
    [ObservableProperty] private bool _arrowsFinished = false;
    [ObservableProperty] private bool _arrowsMismatch = false;
    [ObservableProperty] private bool _arrowsVisible = false;

    public ObservableCollection<CharViewModel> Characters { get; } = new();
    public ObservableCollection<TraceEntry> Trace { get; } = new();
    public ObservableCollection<HistoryEntry> History { get; } = new();

    private bool CanCheck() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => CheckCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckAsync()
    {
        ErrorMessage = null;
        Trace.Clear();
        Characters.Clear();
        ArrowsVisible = false;
        ArrowsFinished = false;
        ArrowsMismatch = false;
        Result = CheckResult.Unknown;
        ResultText = "";
        CallCount = 0;

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

        try
        {
            int baseDelay = AnimationSpeed switch
            {
                AnimationSpeed.Slow => 600,
                AnimationSpeed.Normal => 300,
                AnimationSpeed.Fast => 100,
                _ => 300
            };

            int delay;
            if (!AnimateCheck || normalized.Length > 500) delay = 0;
            else if (normalized.Length > 100) delay = Math.Max(20, baseDelay * 100 / normalized.Length);
            else delay = baseDelay;

            var animator = new PalindromeAnimator();
            bool isPalindrome = await animator.CheckAsync(normalized, delay, OnStepAsync, _cts.Token);

            CallCount = animator.CallCount;
            Result = isPalindrome ? CheckResult.IsPalindrome : CheckResult.NotPalindrome;
            ResultText = isPalindrome ? "Является палиндромом" : "Не является палиндромом";

            History.Insert(0, new HistoryEntry(InputText, normalized, isPalindrome, DateTime.Now));
            while (History.Count > 10) History.RemoveAt(History.Count - 1);
        }
        catch (OperationCanceledException) { /* отмена — не ошибка */ }
        catch (Exception ex)
        {
            ErrorMessage = "Ошибка: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private Task OnStepAsync(CheckStep step)
    {
        int leftViz = Math.Min(step.Left, _visualizedCount - 1);
        int rightViz = Math.Min(step.Right, _visualizedCount - 1);
        LeftArrowX = leftViz * CharWidth;
        RightArrowX = rightViz * CharWidth;

        var state = step.Type switch
        {
            StepType.Comparing => CharState.Comparing,
            StepType.Matched => CharState.Matched,
            StepType.Mismatched => CharState.Mismatched,
            StepType.BaseCase => CharState.Matched,
            _ => CharState.Normal
        };

        if (step.Left >= 0 && step.Left < _visualizedCount)
            Characters[step.Left].State = state;

        if (step.Right >= 0 && step.Right < _visualizedCount && step.Right != step.Left)
            Characters[step.Right].State = state;

        if (step.Type == StepType.BaseCase) ArrowsFinished = true;
        if (step.Type == StepType.Mismatched) ArrowsMismatch = true;

        if (ShowTrace && step.Type != StepType.Comparing)
        {
            char lc = step.Left >= 0 && step.Left < _currentNormalized.Length ? _currentNormalized[step.Left] : '?';
            char rc = step.Right >= 0 && step.Right < _currentNormalized.Length ? _currentNormalized[step.Right] : '?';
            string text = step.Type switch
            {
                StepType.Matched => $"IsPalindrome(left={step.Left}, right={step.Right}): '{lc}' == '{rc}' → рекурсия",
                StepType.Mismatched => $"IsPalindrome(left={step.Left}, right={step.Right}): '{lc}' != '{rc}' → false",
                StepType.BaseCase => $"IsPalindrome(left={step.Left}, right={step.Right}): база рекурсии → true",
                _ => ""
            };
            if (!string.IsNullOrEmpty(text)) Trace.Add(new TraceEntry(text));
        }

        return Task.CompletedTask;
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
        Trace.Clear();
        Characters.Clear();
        ArrowsVisible = false;
        ArrowsFinished = false;
        ArrowsMismatch = false;
        LeftArrowX = 0;
        RightArrowX = 0;
    }

    [RelayCommand]
    private void ClearHistory() => History.Clear();
}