using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class StockOutView : UserControl
{
    public StockOutView()
    {
        InitializeComponent();
        DataContext = new StockOutViewModel();
    }
}
