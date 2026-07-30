using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using SqlSugar;

namespace SparePartsManager.Forms;

public partial class FrmQuery : Form
{
    private DataGridView dgvParts = null!;
    private ComboBox cmbSpecFilter = null!;
    private ComboBox cmbStatusFilter = null!;
    private TextBox txtSearch = null!;
    private Button btnEdit = null!;
    private Button btnSearch = null!;
    private Button btnReset = null!;

    public FrmQuery()
    {
        InitializeComponent();
        this.Load += FrmQuery_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "备件查询";
        this.BackColor = Color.White;
        this.Padding = new Padding(10);

        // 标题
        var lblTitle = new Label
        {
            Text = "🔍 备件查询与管理",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 45,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };

        // 筛选栏
        var pnlFilter = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(245, 245, 250),
            Padding = new Padding(8, 10, 8, 8)
        };

        pnlFilter.Controls.Add(new Label { Text = "搜索：", Location = new Point(10, 14), AutoSize = true });
        txtSearch = new TextBox { Location = new Point(60, 11), Size = new Size(170, 25) };
        pnlFilter.Controls.Add(txtSearch);

        pnlFilter.Controls.Add(new Label { Text = "规格：", Location = new Point(250, 14), AutoSize = true });
        cmbSpecFilter = new ComboBox
        {
            Location = new Point(305, 11),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbSpecFilter.Items.Insert(0, "全部");
        cmbSpecFilter.SelectedIndex = 0;
        pnlFilter.Controls.Add(cmbSpecFilter);

        pnlFilter.Controls.Add(new Label { Text = "状态：", Location = new Point(420, 14), AutoSize = true });
        cmbStatusFilter = new ComboBox
        {
            Location = new Point(470, 11),
            Size = new Size(80, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbStatusFilter.Items.AddRange(new[] { "全部", "在库", "已出库" });
        cmbStatusFilter.SelectedIndex = 1;
        pnlFilter.Controls.Add(cmbStatusFilter);

        btnSearch = new Button
        {
            Text = "🔍 查询",
            Location = new Point(565, 9),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        btnSearch.Click += (s, e) => LoadParts();
        pnlFilter.Controls.Add(btnSearch);

        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(655, 9),
            Size = new Size(80, 30)
        };
        btnReset.Click += (s, e) => { txtSearch.Clear(); cmbSpecFilter.SelectedIndex = 0; cmbStatusFilter.SelectedIndex = 1; LoadParts(); };
        pnlFilter.Controls.Add(btnReset);

        var btnExport = new Button
        {
            Text = "📥 导出",
            Location = new Point(745, 9),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        btnExport.Click += BtnExport_Click;
        pnlFilter.Controls.Add(btnExport);

        // 编辑按钮
        btnEdit = new Button
        {
            Text = "✏️ 编辑选中",
            Dock = DockStyle.Bottom,
            Height = 42,
            BackColor = Color.FromArgb(255, 185, 15),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        btnEdit.Click += BtnEdit_Click;

        // 数据表格
        dgvParts = new DataGridView
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
        dgvParts.CellFormatting += DgvParts_CellFormatting;

        this.Controls.Add(dgvParts);
        this.Controls.Add(btnEdit);
        this.Controls.Add(pnlFilter);
        this.Controls.Add(lblTitle);
    }

    private void FrmQuery_Load(object? sender, EventArgs e) { LoadSpecFilter(); LoadParts(); }

    private void LoadSpecFilter()
    {
        try
        {
            var specs = SqlSugarHelper.Db.Queryable<Models.Specification>().Select(s => s.Name).ToList();
            foreach (var s in specs)
                if (!string.IsNullOrEmpty(s) && !cmbSpecFilter.Items.Contains(s))
                    cmbSpecFilter.Items.Add(s);
        }
        catch { }
    }

    private void LoadParts()
    {
        try
        {
            var db = SqlSugarHelper.Db;
        var keyword = txtSearch.Text.Trim();
        var specFilter = cmbSpecFilter.SelectedIndex > 0 ? cmbSpecFilter.SelectedItem?.ToString() : null;
        var statusFilter = cmbStatusFilter.SelectedIndex switch
        {
            1 => "InStock",
            2 => "OutStock",
            _ => null
        };

        // 加载库存警告阈值
        var alertDict = db.Queryable<StockAlert>()
            .ToList()
            .ToDictionary(a => $"{a.Specification}|{a.Model}", a => a.Threshold);

        // 查询备件
        var parts = db.Queryable<SparePart>()
            .WhereIF(!string.IsNullOrEmpty(keyword),
                p => p.Name.Contains(keyword) || p.Model.Contains(keyword) || p.Manufacturer.Contains(keyword))
            .WhereIF(!string.IsNullOrEmpty(specFilter),
                p => p.Specification == specFilter)
            .WhereIF(!string.IsNullOrEmpty(statusFilter),
                p => p.Status == statusFilter)
            .OrderBy(p => p.StockInDate, OrderByType.Desc)
            .ToList();

        // 统计每种规格+型号的在库数量
        var stockCountDict = db.Queryable<SparePart>()
            .Where(p => p.Status == "InStock")
            .GroupBy(p => new { p.Specification, p.Model })
            .Select(g => new { g.Specification, g.Model, Count = SqlFunc.AggregateCount(g.Id) })
            .ToList()
            .ToDictionary(x => $"{x.Specification}|{x.Model}", x => x.Count);

        // 映射到视图模型
        var viewModels = parts.Select(p =>
        {
            var key = $"{p.Specification}|{p.Model}";
            // 只有明确设置了阈值规则的才判断低库存
            var hasAlert = alertDict.TryGetValue(key, out var threshold);
            var stockCount = stockCountDict.GetValueOrDefault(key, 0);

            return new QueryViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Specification = p.Specification,
                Model = p.Model,
                Manufacturer = p.Manufacturer,
                ShelfNo = p.ShelfNo,
                LayerNo = p.LayerNo,
                PositionNo = p.PositionNo,
                StockInDate = p.StockInDate,
                StockOutDate = p.StockOutDate,
                StockInPerson = p.StockInPerson,
                StockOutPerson = p.StockOutPerson,
                Status = p.Status,
                Remark = p.Remark,
                IsLowStock = hasAlert && p.Status == "InStock" && stockCount < threshold
            };
        }).ToList();

        dgvParts.DataSource = null;
        dgvParts.DataSource = viewModels;

        // 列头
        if (dgvParts.Columns["Id"] != null) dgvParts.Columns["Id"].Visible = false;
        if (dgvParts.Columns["IsLowStock"] != null) dgvParts.Columns["IsLowStock"].Visible = false;
        if (dgvParts.Columns["Name"] != null) dgvParts.Columns["Name"].HeaderText = "名称";
        if (dgvParts.Columns["Specification"] != null) dgvParts.Columns["Specification"].HeaderText = "规格";
        if (dgvParts.Columns["Model"] != null) dgvParts.Columns["Model"].HeaderText = "型号";
        if (dgvParts.Columns["Manufacturer"] != null) dgvParts.Columns["Manufacturer"].HeaderText = "厂家";
        if (dgvParts.Columns["ShelfNo"] != null) dgvParts.Columns["ShelfNo"].HeaderText = "货架";
        if (dgvParts.Columns["LayerNo"] != null) dgvParts.Columns["LayerNo"].HeaderText = "层";
        if (dgvParts.Columns["PositionNo"] != null) dgvParts.Columns["PositionNo"].HeaderText = "位";
        if (dgvParts.Columns["StockInDate"] != null) dgvParts.Columns["StockInDate"].HeaderText = "入库日期";
        if (dgvParts.Columns["StockOutDate"] != null) dgvParts.Columns["StockOutDate"].HeaderText = "出库日期";
        if (dgvParts.Columns["StockInPerson"] != null) dgvParts.Columns["StockInPerson"].HeaderText = "入库人";
        if (dgvParts.Columns["StockOutPerson"] != null) dgvParts.Columns["StockOutPerson"].HeaderText = "出库人";
        if (dgvParts.Columns["Status"] != null) dgvParts.Columns["Status"].HeaderText = "状态";
        if (dgvParts.Columns["Remark"] != null) dgvParts.Columns["Remark"].HeaderText = "备注";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvParts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (dgvParts.Rows[e.RowIndex].DataBoundItem is QueryViewModel item)
        {
            // 低库存红色高亮
            if (item.IsLowStock)
            {
                e.CellStyle!.BackColor = Color.FromArgb(255, 230, 230);
                e.CellStyle!.ForeColor = Color.FromArgb(180, 30, 30);
            }
            // 状态列中文显示
            if (dgvParts.Columns[e.ColumnIndex].Name == "Status" && e.Value is string status)
            {
                e.Value = status == "InStock" ? "在库" : "已出库";
                e.FormattingApplied = true;
            }
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvParts.CurrentRow?.DataBoundItem is not QueryViewModel item)
        {
            MessageBox.Show("请先选择要编辑的备件。", "提示");
            return;
        }

        using var dlg = new FrmSparePartEdit(item.Id);
        if (dlg.ShowDialog() == DialogResult.OK)
            LoadParts();
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (dgvParts.Rows.Count == 0) { MessageBox.Show("无数据可导出。"); return; }
        using var sfd = new SaveFileDialog
        {
            Filter = "CSV 文件|*.csv",
            FileName = $"备件查询导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
            sw.Write('\uFEFF');

            var headers = new List<string>();
            foreach (DataGridViewColumn col in dgvParts.Columns)
                if (col.Visible) headers.Add(EscapeCsv(col.HeaderText));
            sw.WriteLine(string.Join(",", headers));

            foreach (DataGridViewRow row in dgvParts.Rows)
            {
                var cells = new List<string>();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Visible)
                    {
                        var val = cell.FormattedValue?.ToString() ?? "";
                        cells.Add(EscapeCsv(val));
                    }
                }
                sw.WriteLine(string.Join(",", cells));
            }
            MessageBox.Show($"导出成功！\n{sfd.FileName}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}

/// <summary>
/// 查询视图模型（含低库存标记）
/// </summary>
public class QueryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public int ShelfNo { get; set; }
    public int LayerNo { get; set; }
    public int PositionNo { get; set; }
    public DateTime StockInDate { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string StockInPerson { get; set; } = "";
    public string? StockOutPerson { get; set; }
    public string Status { get; set; } = "";
    public string Remark { get; set; } = "";
    public bool IsLowStock { get; set; }
}
