using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;

namespace SparePartsManager.Forms;

public partial class FrmStockIn : Form
{
    private TextBox txtName = null!;
    private ComboBox cmbSpecification = null!;
    private Button btnAddSpec = null!;
    private TextBox txtModel = null!;
    private TextBox txtManufacturer = null!;
    private NumericUpDown nudShelfNo = null!;
    private NumericUpDown nudLayerNo = null!;
    private NumericUpDown nudPositionNo = null!;
    private DateTimePicker dtpStockInDate = null!;
    private NumericUpDown nudQuantity = null!;
    private TextBox txtRemark = null!;
    private Button btnSave = null!;
    private Button btnClear = null!;
    private Label lblPerson = null!;

    public FrmStockIn()
    {
        InitializeComponent();
        this.Load += FrmStockIn_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "备件入库";
        this.BackColor = Color.White;
        this.Padding = new Padding(20);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 12,
            Padding = new Padding(20, 20, 20, 10),
            AutoSize = true
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        for (int i = 0; i < 12; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var lblTitle = new Label
        {
            Text = "📥 备件入库登记",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(20, 15, 0, 15),
            Height = 50
        };

        int row = 0;

        // 名称
        AddLabelRow(table, "名称：", ref row);
        txtName = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(txtName, 1, row - 1);

        // 规格 + +号按钮
        table.Controls.Add(new Label { Text = "规格：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        var pnlSpec = new Panel { Dock = DockStyle.Fill };
        cmbSpecification = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        btnAddSpec = new Button
        {
            Text = "＋",
            Dock = DockStyle.Right,
            Width = 30,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 100),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        btnAddSpec.Click += BtnAddSpec_Click;
        pnlSpec.Controls.Add(btnAddSpec);
        pnlSpec.Controls.Add(cmbSpecification);
        table.Controls.Add(pnlSpec, 1, row);
        row++;

        // 型号
        AddLabelRow(table, "型号：", ref row);
        txtModel = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(txtModel, 1, row - 1);

        // 厂家
        AddLabelRow(table, "厂家：", ref row);
        txtManufacturer = new TextBox { Dock = DockStyle.Fill };
        table.Controls.Add(txtManufacturer, 1, row - 1);

        // 货位：货架号 + 层号 + 区位号
        table.Controls.Add(new Label { Text = "货位：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        var pnlLoc = new Panel { Dock = DockStyle.Fill };
        pnlLoc.Controls.Add(new Label { Text = "架", Location = new Point(72, 12), AutoSize = true });
        nudShelfNo = new NumericUpDown { Location = new Point(10, 9), Width = 55, Minimum = 0, Maximum = 9999 };
        pnlLoc.Controls.Add(new Label { Text = "层", Location = new Point(170, 12), AutoSize = true });
        nudLayerNo = new NumericUpDown { Location = new Point(105, 9), Width = 55, Minimum = 0, Maximum = 9999 };
        pnlLoc.Controls.Add(new Label { Text = "位", Location = new Point(268, 12), AutoSize = true });
        nudPositionNo = new NumericUpDown { Location = new Point(200, 9), Width = 55, Minimum = 0, Maximum = 9999 };
        pnlLoc.Controls.Add(nudShelfNo);
        pnlLoc.Controls.Add(nudLayerNo);
        pnlLoc.Controls.Add(nudPositionNo);
        table.Controls.Add(pnlLoc, 1, row);
        row++;

        // 入库日期 + 入库人
        table.Controls.Add(new Label { Text = "入库日期：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        dtpStockInDate = new DateTimePicker { Dock = DockStyle.Fill, Value = DateTime.Now, Format = DateTimePickerFormat.Short };
        table.Controls.Add(dtpStockInDate, 1, row);
        table.Controls.Add(new Label { Text = "入库人：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 2, row);
        lblPerson = new Label
        {
            Text = CurrentUser.IsLoggedIn ? CurrentUser.LoginUser!.RealName : "",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold)
        };
        table.Controls.Add(lblPerson, 3, row);
        row++;

        // 数量
        table.Controls.Add(new Label { Text = "数量：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        nudQuantity = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 9999, Value = 1 };
        table.Controls.Add(nudQuantity, 1, row);
        row++;

        // 备注
        table.Controls.Add(new Label { Text = "备注：", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        txtRemark = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 50 };
        table.RowStyles[row].Height = 50;
        table.Controls.Add(txtRemark, 1, row);
        table.SetColumnSpan(txtRemark, 3);
        row++;
        row++;

        // 按钮
        btnSave = new Button
        {
            Text = "💾 保存入库",
            Size = new Size(130, 40),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        btnSave.Click += BtnSave_Click;
        btnClear = new Button { Text = "清空重填", Size = new Size(130, 40), Font = new Font("Microsoft YaHei", 10) };
        btnClear.Click += (s, e) => ClearForm();

        var pnlButton = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 10, 20, 15),
            Height = 60
        };
        pnlButton.Controls.Add(btnSave);
        pnlButton.Controls.Add(btnClear);

        this.Controls.Add(table);
        this.Controls.Add(pnlButton);
        this.Controls.Add(lblTitle);
    }

    private static void AddLabelRow(TableLayoutPanel table, string text, ref int row)
    {
        table.Controls.Add(new Label { Text = text, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        row++;
    }

    private void FrmStockIn_Load(object? sender, EventArgs e) => LoadSpecs();

    private void LoadSpecs()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var specs = db.Queryable<Specification>().Select(s => s.Name).ToList();
            cmbSpecification.Items.Clear();
            foreach (var s in specs)
                if (!string.IsNullOrEmpty(s)) cmbSpecification.Items.Add(s);
        }
        catch { }
    }

    private void BtnAddSpec_Click(object? sender, EventArgs e)
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "请输入新规格名称：", "新增规格", "");
        var name = input?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var db = SqlSugarHelper.Db;
            if (db.Queryable<Specification>().Any(s => s.Name == name))
            {
                MessageBox.Show("该规格已存在。", "提示");
                return;
            }
            db.Insertable(new Specification { Name = name }).ExecuteCommand();
            LoadSpecs();
            cmbSpecification.Text = name;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误");
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var name = txtName.Text.Trim();
        var spec = cmbSpecification.Text.Trim();
        var model = txtModel.Text.Trim();
        var quantity = (int)nudQuantity.Value;

        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入备件名称。"); txtName.Focus(); return; }
        if (string.IsNullOrEmpty(spec)) { MessageBox.Show("请输入/选择规格。"); cmbSpecification.Focus(); return; }
        if (string.IsNullOrEmpty(model)) { MessageBox.Show("请输入型号。"); txtModel.Focus(); return; }

        var locDesc = $"{nudShelfNo.Value}-{nudLayerNo.Value}-{nudPositionNo.Value}";
        var confirm = MessageBox.Show(
            $"确认入库？\n\n名称：{name}\n规格：{spec}\n型号：{model}\n" +
            $"厂家：{txtManufacturer.Text.Trim()}\n货位：{locDesc}\n" +
            $"数量：{quantity}\n入库日期：{dtpStockInDate.Value:yyyy-MM-dd}\n入库人：{CurrentUser.LoginUser?.RealName}",
            "确认入库", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            var db = SqlSugarHelper.Db;
            // 自动保存新规格到字典
            if (!string.IsNullOrEmpty(spec) && !db.Queryable<Specification>().Any(s => s.Name == spec))
                db.Insertable(new Specification { Name = spec }).ExecuteCommand();

            var baseData = new SparePart
            {
                Specification = spec, Model = model,
                Manufacturer = txtManufacturer.Text.Trim(),
                ShelfNo = (int)nudShelfNo.Value, LayerNo = (int)nudLayerNo.Value, PositionNo = (int)nudPositionNo.Value,
                StockInDate = dtpStockInDate.Value.Date,
                StockInPerson = CurrentUser.LoginUser?.RealName ?? "",
                Remark = txtRemark.Text.Trim(), Status = "InStock"
            };

            if (quantity == 1)
            {
                baseData.Name = name;
                db.Insertable(baseData).ExecuteCommand();
            }
            else
            {
                var list = new List<SparePart>(quantity);
                for (int i = 1; i <= quantity; i++)
                {
                    var item = new SparePart
                    {
                        Name = $"{name}-{i}",
                        Specification = baseData.Specification, Model = baseData.Model,
                        Manufacturer = baseData.Manufacturer,
                        ShelfNo = baseData.ShelfNo, LayerNo = baseData.LayerNo, PositionNo = baseData.PositionNo,
                        StockInDate = baseData.StockInDate, StockInPerson = baseData.StockInPerson,
                        Remark = baseData.Remark, Status = baseData.Status
                    };
                    list.Add(item);
                }
                db.Insertable(list).ExecuteCommand();
            }

            MessageBox.Show($"入库成功！共 {quantity} 件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
            LoadSpecs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"入库失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearForm()
    {
        txtName.Clear(); cmbSpecification.Text = ""; txtModel.Clear();
        txtManufacturer.Clear();
        nudShelfNo.Value = 0; nudLayerNo.Value = 0; nudPositionNo.Value = 0;
        dtpStockInDate.Value = DateTime.Now; nudQuantity.Value = 1;
        txtRemark.Clear(); txtName.Focus();
    }
}
