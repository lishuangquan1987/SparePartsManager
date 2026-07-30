using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class StockAlertView : UserControl
{
    public StockAlertView()
    {
        InitializeComponent();
        DataContext = new StockAlertViewModel();
    }
}
