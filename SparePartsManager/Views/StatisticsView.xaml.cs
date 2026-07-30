using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
        DataContext = new StatisticsViewModel();
    }
}
