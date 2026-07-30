using System.Windows;

namespace SparePartsManager.Views;

public partial class InputDialog : Window
{
    public string InputText { get; private set; } = string.Empty;

    public InputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Focus();
    }

    private void InputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        OkButton.IsEnabled = InputBox.Text.Trim().Length > 0;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
