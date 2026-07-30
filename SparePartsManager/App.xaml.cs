using System.Windows;
using SparePartsManager.Data;

namespace SparePartsManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化数据库（建表 + 默认管理员）
        SqlSugarHelper.InitDatabase();
    }
}
