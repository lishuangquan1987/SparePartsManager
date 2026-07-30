using System.Windows;
using SparePartsManager.Models;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class UserEditWindow : Window
{
    private UserEditViewModel ViewModel => (UserEditViewModel)DataContext;

    public UserEditWindow(User? user = null)
    {
        InitializeComponent();
        DataContext = new UserEditViewModel(user);
        ViewModel.RequestClose += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBox.Password;
        ViewModel.SaveCommand.Execute(null);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelCommand.Execute(null);
    }
}
