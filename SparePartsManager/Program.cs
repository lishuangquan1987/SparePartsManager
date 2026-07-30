using SparePartsManager.Data;
using SparePartsManager.Forms;

namespace SparePartsManager;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
#if NET8_0_OR_GREATER
        ApplicationConfiguration.Initialize();
#else
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
#endif

        // 初始化数据库（建表 + 默认管理员）
        SqlSugarHelper.InitDatabase();

        // 登录
        using var loginForm = new FrmLogin();
        if (loginForm.ShowDialog() != DialogResult.OK)
            return;

        // 登录成功，打开主窗体
        Application.Run(new FrmMain());
    }
}