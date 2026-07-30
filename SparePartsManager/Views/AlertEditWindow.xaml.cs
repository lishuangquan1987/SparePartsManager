using System.Windows;
using SparePartsManager.Models;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class AlertEditWindow : Window
{
    private AlertEditViewModel ViewModel => (AlertEditViewModel)DataContext;

    public AlertEditWindow(StockAlert? alert = null)
    {
        InitializeComponent();
        DataContext = new AlertEditViewModel(alert);
        ViewModel.RequestClose += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
