using CommunityToolkit.Mvvm.ComponentModel;

namespace PalindromDemo.ViewModels;

public enum CharState { Normal, Comparing, Matched, Mismatched }

public partial class CharViewModel : ObservableObject
{
    public int Index { get; }
    public char Char { get; }

    [ObservableProperty]
    private CharState _state = CharState.Normal;

    public CharViewModel(int index, char c)
    {
        Index = index;
        Char = c;
    }
}