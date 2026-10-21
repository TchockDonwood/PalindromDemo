using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PalindromDemo.ViewModels;

namespace PalindromDemo.Controls;

public partial class PalindromeVisualizer : UserControl
{
    private MainViewModel? _vm;

    public PalindromeVisualizer()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = e.NewValue as MainViewModel;
        if (_vm != null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            SnapArrows();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm == null) return;
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.LeftArrowX):
                AnimateArrow(LeftArrow, _vm.LeftArrowX);
                break;
            case nameof(MainViewModel.RightArrowX):
                AnimateArrow(RightArrow, _vm.RightArrowX);
                break;
        }
    }

    private static void AnimateArrow(Path arrow, double targetX)
    {
        // Плавное перемещение стрелки к новой позиции
        var current = arrow.GetAnimationBaseValue(Canvas.LeftProperty) is double d ? d : 0;
        var animation = new DoubleAnimation(current, targetX, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        arrow.BeginAnimation(Canvas.LeftProperty, animation);
    }

    private void SnapArrows()
    {
        if (_vm == null) return;
        LeftArrow.BeginAnimation(Canvas.LeftProperty, null);
        RightArrow.BeginAnimation(Canvas.LeftProperty, null);
        Canvas.SetLeft(LeftArrow, _vm.LeftArrowX);
        Canvas.SetLeft(RightArrow, _vm.RightArrowX);
    }
}