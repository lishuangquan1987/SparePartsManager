using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class QueryView : UserControl
{
    public QueryView()
    {
        InitializeComponent();
        DataContext = new QueryViewModel();
    }
}
