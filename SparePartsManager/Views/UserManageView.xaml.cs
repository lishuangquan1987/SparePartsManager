using System.Windows.Controls;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class UserManageView : UserControl
{
    public UserManageView()
    {
        InitializeComponent();
        DataContext = new UserManageViewModel();
    }
}
