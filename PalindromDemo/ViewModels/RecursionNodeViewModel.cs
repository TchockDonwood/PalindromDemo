using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PalindromDemo.ViewModels;

public enum NodeState { Active, Matched, Mismatched, BaseCase, Returned }

public partial class RecursionNodeViewModel : ObservableObject
{
    public int Depth { get; }
    public int Left { get; }
    public int Right { get; }
    public char LeftChar { get; }
    public char RightChar { get; }

    [ObservableProperty] private NodeState _state = NodeState.Active;
    [ObservableProperty] private bool? _result;
    [ObservableProperty] private bool _isCollapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ElapsedText))]
    private double _stepElapsedMs;

    public RecursionNodeViewModel(int depth, int left, int right, char leftChar, char rightChar)
    {
        Depth = depth;
        Left = left;
        Right = right;
        LeftChar = leftChar;
        RightChar = rightChar;
    }

    public Thickness IndentMargin => new(Depth * 24.0, 0, 0, 4);
    public string HeaderText => $"IsPalindrome({Left}, {Right})";
    public string CharsDisplay => Left >= Right
        ? $"'{LeftChar}' (база)"
        : $"'{LeftChar}' ↔ '{RightChar}'";
    public string ElapsedText => StepElapsedMs > 0
        ? PalindromDemo.Helpers.TimeFormatter.FormatMs3(StepElapsedMs)
        : "";
}