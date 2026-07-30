using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class StockInView : UserControl
{
    public StockInView()
    {
        InitializeComponent();
        DataContext = new StockInViewModel();
    }
}
