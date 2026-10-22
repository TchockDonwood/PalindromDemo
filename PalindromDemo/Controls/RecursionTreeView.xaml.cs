using System.Windows.Controls;

namespace PalindromDemo.Controls;

public partial class RecursionTreeView : UserControl
{
    public RecursionTreeView() => InitializeComponent();

    private void TreeScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0 && e.ExtentHeight > e.ViewportHeight)
            ((ScrollViewer)sender).ScrollToEnd();
    }
}