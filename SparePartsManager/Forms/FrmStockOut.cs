using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;

namespace SparePartsManager.Forms;

public partial class FrmStockOut : Form
{
    private DataGridView dgvParts = null!;
    private Button btnStockOut = null!;
    private TextBox txtSearch = null!;
    private Label lblTitle = null!;
    private CheckBox chkSelectAll = null!;

    public FrmStockOut()
    {
        InitializeComponent();
        this.Load += FrmStockOut_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "备件出库";
        this.BackColor = Color.White;
        this.Padding = new Padding(15);

        lblTitle = new Label
        {
            Text = "📤 备件出库 — 勾选多条在库备件，批量确认出库",
            Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
            Dock = DockStyle.Top, Height = 45, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0)
        };

        var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5, 8, 5, 5) };
        pnlSearch.Controls.Add(new Label { Text = "搜索：", Location = new Point(10, 12), AutoSize = true });
        txtSearch = new TextBox { Location = new Point(80, 9), Size = new Size(220, 25) };
        txtSearch.TextChanged += (s, e) => LoadParts();
        pnlSearch.Controls.Add(txtSearch);
        chkSelectAll = new CheckBox { Text = "全选", Location = new Point(320, 11), AutoSize = true };
        chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
        pnlSearch.Controls.Add(chkSelectAll);

        dgvParts = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            ReadOnly = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false
        };
        dgvParts.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Select", HeaderText = "", Width = 40, ReadOnly = false
        });

        btnStockOut = new Button
        {
            Text = "📤 确认出库（选中项）", Dock = DockStyle.Bottom, Height = 45,
            BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        btnStockOut.Click += BtnStockOut_Click;

        this.Controls.Add(dgvParts);
        this.Controls.Add(btnStockOut);
        this.Controls.Add(pnlSearch);
        this.Controls.Add(lblTitle);
    }

    private void FrmStockOut_Load(object? sender, EventArgs e) => LoadParts();

    private void ChkSelectAll_CheckedChanged(object? sender, EventArgs e)
    {
        var check = chkSelectAll.Checked;
        foreach (DataGridViewRow row in dgvParts.Rows)
            if (row.Cells["Select"] is DataGridViewCheckBoxCell cell)
                cell.Value = check;
    }

    private void LoadParts()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var keyword = txtSearch?.Text.Trim() ?? "";
            var parts = db.Queryable<SparePart>()
                .Where(p => p.Status == "InStock")
                .WhereIF(!string.IsNullOrEmpty(keyword), p => p.Name.Contains(keyword) || p.Model.Contains(keyword))
                .OrderBy(p => p.StockInDate)
                .Select(p => new StockOutItem
                {
                    Id = p.Id, Name = p.Name, Specification = p.Specification, Model = p.Model,
                    Manufacturer = p.Manufacturer,
                    ShelfNo = p.ShelfNo, LayerNo = p.LayerNo, PositionNo = p.PositionNo,
                    StockInDate = p.StockInDate, StockInPerson = p.StockInPerson, Remark = p.Remark
                }).ToList();

            dgvParts.DataSource = null;
            dgvParts.DataSource = parts;

            if (dgvParts.Columns["Select"] != null) dgvParts.Columns["Select"].DisplayIndex = 0;
            foreach (DataGridViewColumn col in dgvParts.Columns)
                if (col.Name != "Select") col.ReadOnly = true;

            if (dgvParts.Columns["Id"] != null) dgvParts.Columns["Id"].Visible = false;
            if (dgvParts.Columns["Name"] != null) dgvParts.Columns["Name"].HeaderText = "名称";
            if (dgvParts.Columns["Specification"] != null) dgvParts.Columns["Specification"].HeaderText = "规格";
            if (dgvParts.Columns["Model"] != null) dgvParts.Columns["Model"].HeaderText = "型号";
            if (dgvParts.Columns["Manufacturer"] != null) dgvParts.Columns["Manufacturer"].HeaderText = "厂家";
            if (dgvParts.Columns["ShelfNo"] != null) dgvParts.Columns["ShelfNo"].HeaderText = "货架";
            if (dgvParts.Columns["LayerNo"] != null) dgvParts.Columns["LayerNo"].HeaderText = "层";
            if (dgvParts.Columns["PositionNo"] != null) dgvParts.Columns["PositionNo"].HeaderText = "位";
            if (dgvParts.Columns["StockInDate"] != null) dgvParts.Columns["StockInDate"].HeaderText = "入库日期";
            if (dgvParts.Columns["StockInPerson"] != null) dgvParts.Columns["StockInPerson"].HeaderText = "入库人";
            if (dgvParts.Columns["Remark"] != null) dgvParts.Columns["Remark"].HeaderText = "备注";
        }
        catch (Exception ex) { MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private List<StockOutItem> GetCheckedItems()
    {
        var list = new List<StockOutItem>();
        foreach (DataGridViewRow row in dgvParts.Rows)
        {
            if (row.DataBoundItem is not StockOutItem item) continue;
            if (row.Cells["Select"] is DataGridViewCheckBoxCell cell && cell.Value is true)
                list.Add(item);
        }
        return list;
    }

    private void BtnStockOut_Click(object? sender, EventArgs e)
    {
        var selected = GetCheckedItems();
        if (selected.Count == 0) { MessageBox.Show("请至少勾选一条在库备件。"); return; }

        var grouped = selected.GroupBy(s => $"{s.Specification}-{s.Model}")
            .Select(g => $"【{g.Key}】×{g.Count()}")
            .ToList();
        var confirm = MessageBox.Show(
            $"确认出库以下 {selected.Count} 件？\n\n" + string.Join("\n", grouped.Take(30)) +
            (grouped.Count > 30 ? $"\n... 共 {grouped.Count} 类" : ""),
            "确认出库", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            var outPerson = CurrentUser.LoginUser?.RealName ?? "";
            var now = DateTime.Now;
            var ids = selected.Select(s => s.Id).ToList();
            var db = SqlSugarHelper.Db;
            var affected = db.Updateable<SparePart>()
                .SetColumns(it => new SparePart { Status = "OutStock", StockOutDate = now, StockOutPerson = outPerson })
                .Where(it => ids.Contains(it.Id) && it.Status == "InStock")
                .ExecuteCommand();

            MessageBox.Show($"出库完成！成功 {affected} 件" +
                (affected < selected.Count ? $"，{selected.Count - affected} 件可能已被他人出库" : ""),
                "结果", MessageBoxButtons.OK,
                affected == selected.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            LoadParts();
        }
        catch (Exception ex) { MessageBox.Show($"出库失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public class StockOutItem
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
    public string StockInPerson { get; set; } = "";
    public string Remark { get; set; } = "";
}
