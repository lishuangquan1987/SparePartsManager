using System.Windows;
using System.Windows.Input;
using SparePartsManager.ViewModels;

namespace SparePartsManager.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel ViewModel => (LoginViewModel)DataContext;

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1) DragMove();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBox.Password;
        if (ViewModel.TryLogin())
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
