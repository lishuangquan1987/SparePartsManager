using System.Windows;
using SparePartsManager.Services;

namespace SparePartsManager.Views;

public partial class ImportOptionsDialog : Window
{
    /// <summary>已选择的 Excel 文件路径（确定时有效）。</summary>
    public string FilePath { get; private set; } = "";

    public ExcelService.ImportOptions Options { get; private set; } = new();

    public ImportOptionsDialog(bool allowAutoCreate = true)
    {
        InitializeComponent();
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        // 未选择文件前不允许导入
        OkButton.IsEnabled = false;

        // 自动创建字典项属管理员权限：非管理员强制关闭并提示
        if (!allowAutoCreate)
        {
            AutoSpec.IsChecked = false;
            AutoModel.IsChecked = false;
            AutoManufacturer.IsChecked = false;
            AutoProject.IsChecked = false;
            AutoSpec.IsEnabled = false;
            AutoModel.IsEnabled = false;
            AutoManufacturer.IsEnabled = false;
            AutoProject.IsEnabled = false;
            HintText.Text = "提示：仅管理员可自动创建字典项；模板中的名称必须已存在，否则该行报错中止。";
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx|所有文件|*.*",
            Title = "选择要导入的备件文件（模板与导出格式一致）"
        };
        if (dlg.ShowDialog() == true)
            FilePathText.Text = dlg.FileName;
    }

    private void FilePathText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        OkButton.IsEnabled = FilePathText.Text.Trim().Length > 0;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = FilePathText.Text.Trim();
        if (string.IsNullOrEmpty(filePath)) return; // 未选择文件，不允许导入

        FilePath = filePath;
        Options = new ExcelService.ImportOptions
        {
            AutoCreateSpec = AutoSpec.IsChecked == true,
            AutoCreateModel = AutoModel.IsChecked == true,
            AutoCreateManufacturer = AutoManufacturer.IsChecked == true,
            AutoCreateProject = AutoProject.IsChecked == true
        };
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
