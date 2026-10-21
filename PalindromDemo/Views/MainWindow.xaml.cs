using System.Windows;
using PalindromDemo.ViewModels;

namespace PalindromDemo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}