using System.Windows;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class SparePartEditWindow : Window
{
    private SparePartEditViewModel ViewModel => (SparePartEditViewModel)DataContext;

    public SparePartEditWindow(int partId)
    {
        InitializeComponent();
        DataContext = new SparePartEditViewModel(partId);
        ViewModel.RequestClose += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
