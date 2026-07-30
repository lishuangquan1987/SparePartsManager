using SparePartsManager.Data;
using SparePartsManager.Models;
using SqlSugar;

namespace SparePartsManager.Forms;

public partial class FrmStockAlert : Form
{
    private DataGridView dgvAlerts = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Panel pnlToolbar = null!;
    private Label lblTitle = null!;

    public FrmStockAlert()
    {
        InitializeComponent();
        this.Load += FrmStockAlert_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "库存警告设置";
        this.BackColor = Color.White;

        // 标题
        lblTitle = new Label
        {
            Text = "⚠️ 库存警告阈值设置（按「规格 + 型号」设定最低库存数量）",
            Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0),
            ForeColor = Color.FromArgb(180, 53, 30)
        };

        // 工具栏
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(250, 250, 250),
            Padding = new Padding(10, 8, 10, 8)
        };

        btnAdd = CreateButton("➕ 新增规则", Color.FromArgb(0, 122, 204), 10);
        btnAdd.Click += BtnAdd_Click;

        btnEdit = CreateButton("✏️ 编辑", Color.FromArgb(255, 185, 15), 145);
        btnEdit.Click += BtnEdit_Click;

        btnDelete = CreateButton("🗑️ 删除", Color.FromArgb(220, 53, 69), 280);
        btnDelete.Click += BtnDelete_Click;

        pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        // 说明
        var lblHint = new Label
        {
            Text = "提示：当某种「规格+型号」的在库数量低于设定阈值时，系统将在登录时弹窗警告，并在备件列表中红色高亮显示。",
            Dock = DockStyle.Bottom,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0),
            ForeColor = Color.Gray,
            Font = new Font("Microsoft YaHei", 9)
        };

        // 数据表格
        dgvAlerts = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false
        };

        this.Controls.Add(dgvAlerts);
        this.Controls.Add(lblHint);
        this.Controls.Add(pnlToolbar);
        this.Controls.Add(lblTitle);
    }

    private static Button CreateButton(string text, Color backColor, int x)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, 8),
            Size = new Size(120, 34),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9),
            UseVisualStyleBackColor = false
        };
    }

    private void FrmStockAlert_Load(object? sender, EventArgs e) => LoadAlerts();

    private void LoadAlerts()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var alerts = db.Queryable<StockAlert>()
            .OrderBy(a => a.Specification + "|" + a.Model)
            .Select(a => new AlertViewModel
            {
                Id = a.Id,
                Specification = a.Specification,
                Model = a.Model,
                Threshold = a.Threshold,
                CurrentStock = SqlFunc.Subqueryable<SparePart>()
                    .Where(p => p.Specification == a.Specification
                        && p.Model == a.Model
                        && p.Status == "InStock")
                    .Count()
            })
            .ToList();

        dgvAlerts.DataSource = null;
        dgvAlerts.DataSource = alerts;

        if (dgvAlerts.Columns["Id"] != null) dgvAlerts.Columns["Id"].Visible = false;
        if (dgvAlerts.Columns["Specification"] != null) dgvAlerts.Columns["Specification"].HeaderText = "规格";
        if (dgvAlerts.Columns["Model"] != null) dgvAlerts.Columns["Model"].HeaderText = "型号";
        if (dgvAlerts.Columns["Threshold"] != null) dgvAlerts.Columns["Threshold"].HeaderText = "最低阈值";
        if (dgvAlerts.Columns["CurrentStock"] != null) dgvAlerts.Columns["CurrentStock"].HeaderText = "当前库存";
        if (dgvAlerts.Columns["IsWarning"] != null) dgvAlerts.Columns["IsWarning"].Visible = false;

        // 高亮不足的行
        foreach (DataGridViewRow row in dgvAlerts.Rows)
        {
            if (row.DataBoundItem is AlertViewModel { IsWarning: true })
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 30, 30);
            }
        }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载警告规则失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new FrmAlertEdit();
        if (dlg.ShowDialog() == DialogResult.OK)
            LoadAlerts();
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvAlerts.CurrentRow?.DataBoundItem is not AlertViewModel item)
        {
            MessageBox.Show("请先选择一条规则。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var alert = db.Queryable<StockAlert>().InSingle(item.Id);
            if (alert == null) return;

            using var dlg = new FrmAlertEdit(alert);
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadAlerts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"编辑失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvAlerts.CurrentRow?.DataBoundItem is not AlertViewModel item)
        {
            MessageBox.Show("请先选择一条规则。", "提示");
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除「{item.Specification} - {item.Model}」的警告规则吗？",
            "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            try
            {
                SqlSugarHelper.Db.Deleteable<StockAlert>().In(item.Id).ExecuteCommand();
                LoadAlerts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

/// <summary>
/// 警告视图模型
/// </summary>
public class AlertViewModel
{
    public int Id { get; set; }
    public string Specification { get; set; } = "";
    public string Model { get; set; } = "";
    public int Threshold { get; set; }
    public int CurrentStock { get; set; }
    public bool IsWarning => CurrentStock < Threshold;
}
