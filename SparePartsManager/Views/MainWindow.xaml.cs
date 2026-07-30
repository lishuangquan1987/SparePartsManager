using System.Windows;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
