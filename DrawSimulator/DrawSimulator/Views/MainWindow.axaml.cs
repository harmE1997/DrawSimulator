using Avalonia.Controls;

namespace DrawSimulator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        isoPopup popup = new isoPopup();
    }
}
