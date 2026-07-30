using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;

namespace SparePartsManager.Forms;

public partial class FrmMain : Form
{
    private Panel pnlLeft = null!;
    private Panel pnlRight = null!;
    private TreeView treeMenu = null!;
    private Label lblUserInfo = null!;
    private Label lblStatus = null!;

    // 子窗体引用（防止重复打开）
    private Form? _currentChild;

    public FrmMain()
    {
        InitializeComponent();
        this.Load += FrmMain_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "备件库管理系统";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Size = new Size(1200, 750);
        this.MinimumSize = new Size(900, 600);

        // 左侧面板
        pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = Color.FromArgb(45, 45, 48)
        };

        // 用户信息
        lblUserInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 33),
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };

        // 菜单树
        treeMenu = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Microsoft YaHei", 10),
            ItemHeight = 36,
            Indent = 20,
            ShowLines = false,
            ShowPlusMinus = false,
            ShowRootLines = false
        };
        treeMenu.AfterSelect += TreeMenu_AfterSelect;

        pnlLeft.Controls.Add(treeMenu);
        pnlLeft.Controls.Add(lblUserInfo);

        // 右侧面板
        pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 240, 245)
        };

        // 状态栏
        lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            Padding = new Padding(10, 0, 0, 0),
            Text = "就绪"
        };

        this.Controls.Add(pnlRight);
        this.Controls.Add(pnlLeft);
        this.Controls.Add(lblStatus);
    }

    private void FrmMain_Load(object? sender, EventArgs e)
    {
        if (!CurrentUser.IsLoggedIn) return;

        lblUserInfo.Text = $"👤 {CurrentUser.LoginUser!.RealName}\n({CurrentUser.LoginUser.Role})";

        BuildMenu();

        // 登录后检查库存警告
        CheckStockAlerts();
    }

    private void BuildMenu()
    {
        treeMenu.Nodes.Clear();

        // 备件管理节点
        var spareNode = new TreeNode("📦 备件管理") { Tag = "spare" };
        spareNode.Nodes.Add(new TreeNode("📥 备件入库") { Tag = "stockin" });
        spareNode.Nodes.Add(new TreeNode("📤 备件出库") { Tag = "stockout" });
        spareNode.Nodes.Add(new TreeNode("🔍 备件查询") { Tag = "query" });
        spareNode.Nodes.Add(new TreeNode("📊 统计分析") { Tag = "stats" });
        treeMenu.Nodes.Add(spareNode);

        // 管理员专属
        if (CurrentUser.IsAdmin)
        {
            var sysNode = new TreeNode("⚙️ 系统管理") { Tag = "system" };
            sysNode.Nodes.Add(new TreeNode("👥 用户管理") { Tag = "users" });
            sysNode.Nodes.Add(new TreeNode("⚠️ 库存警告设置") { Tag = "alert" });
            treeMenu.Nodes.Add(sysNode);
        }

        treeMenu.Nodes.Add(new TreeNode("🚪 退出系统") { Tag = "exit" });

        treeMenu.ExpandAll();
    }

    private void TreeMenu_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag == null) return;
        var tag = e.Node.Tag.ToString();

        switch (tag)
        {
            case "stockin":
                OpenChild(new FrmStockIn());
                break;
            case "stockout":
                OpenChild(new FrmStockOut());
                break;
            case "query":
                OpenChild(new FrmQuery());
                break;
            case "stats":
                OpenChild(new FrmStatistics());
                break;
            case "users":
                if (CurrentUser.IsAdmin)
                    OpenChild(new FrmUserManage());
                break;
            case "alert":
                if (CurrentUser.IsAdmin)
                    OpenChild(new FrmStockAlert());
                break;
            case "exit":
                Application.Exit();
                break;
        }
    }

    private void OpenChild(Form child)
    {
        _currentChild?.Close();
        _currentChild = child;
        child.TopLevel = false;
        child.FormBorderStyle = FormBorderStyle.None;
        child.Dock = DockStyle.Fill;
        pnlRight.Controls.Clear();
        pnlRight.Controls.Add(child);
        child.Show();
        lblStatus.Text = $"当前：{child.Text}";
    }

    /// <summary>
    /// 检查库存警告
    /// </summary>
    private void CheckStockAlerts()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var alerts = db.Queryable<StockAlert>().ToList();

            var warningList = new List<string>();
            foreach (var alert in alerts)
            {
                var count = db.Queryable<SparePart>()
                    .Count(p => p.Specification == alert.Specification
                        && p.Model == alert.Model
                        && p.Status == "InStock");

                if (count < alert.Threshold)
                {
                    warningList.Add($"【{alert.Specification}】{alert.Model}：库存 {count}，低于阈值 {alert.Threshold}");
                }
            }

            if (warningList.Count > 0)
            {
                var msg = "以下备件库存不足：\n\n" + string.Join("\n", warningList);
                MessageBox.Show(msg, "⚠️ 库存警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch
        {
            // 库存检查失败不影响系统使用
        }
    }
}
