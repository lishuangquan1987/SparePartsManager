using SparePartsManager.Data;
using SparePartsManager.Models;

namespace SparePartsManager.Forms;

public partial class FrmSparePartEdit : Form
{
    private readonly int _partId;
    private TextBox txtName = null!;
    private ComboBox cmbSpecification = null!;
    private TextBox txtModel = null!;
    private TextBox txtManufacturer = null!;
    private NumericUpDown nudShelfNo = null!;
    private NumericUpDown nudLayerNo = null!;
    private NumericUpDown nudPositionNo = null!;
    private TextBox txtRemark = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public FrmSparePartEdit(int partId)
    {
        _partId = partId;
        InitializeComponent();
        LoadPart();
        LoadSpecs();
    }

    private void InitializeComponent()
    {
        this.Text = "编辑备件";
        this.Size = new Size(500, 480);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var lw = 90; var y = 20;
        AddRow("名称：", y, out txtName, 280); y += 40;
        AddCmbRow("规格：", y, out cmbSpecification, 280); y += 40;
        AddRow("型号：", y, out txtModel, 280); y += 40;
        AddRow("厂家：", y, out txtManufacturer, 280); y += 40;

        AddLabel("货位：", y);
        var px = lw + 20;
        nudShelfNo = new NumericUpDown { Location = new Point(px, y - 3), Width = 70, Minimum = 0, Maximum = 9999 };
        this.Controls.Add(new Label { Text = "架", Location = new Point(px + 72, y), AutoSize = true });
        nudLayerNo = new NumericUpDown { Location = new Point(px + 90, y - 3), Width = 70, Minimum = 0, Maximum = 9999 };
        this.Controls.Add(new Label { Text = "层", Location = new Point(px + 162, y), AutoSize = true });
        nudPositionNo = new NumericUpDown { Location = new Point(px + 180, y - 3), Width = 70, Minimum = 0, Maximum = 9999 };
        this.Controls.Add(new Label { Text = "位", Location = new Point(px + 252, y), AutoSize = true });
        this.Controls.Add(nudShelfNo); this.Controls.Add(nudLayerNo); this.Controls.Add(nudPositionNo);
        y += 40;

        AddRow("备注：", y, out txtRemark, 280, 60, true); y += 70;

        btnSave = new Button { Text = "保存", Location = new Point(lw + 40, y), Size = new Size(100, 38), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
        btnSave.Click += BtnSave_Click;
        btnCancel = new Button { Text = "取消", Location = new Point(lw + 160, y), Size = new Size(100, 38) };
        btnCancel.Click += (s, e) => this.Close();
        this.Controls.AddRange(new Control[] { txtName, cmbSpecification, txtModel, txtManufacturer, txtRemark, btnSave, btnCancel });
    }

    private void AddRow(string lbl, int y, out TextBox tb, int w, int h = 25, bool multi = false)
    {
        AddLabel(lbl, y);
        tb = new TextBox { Location = new Point(110, y - 3), Size = new Size(w, h), Multiline = multi };
        this.Controls.Add(tb);
    }
    private void AddCmbRow(string lbl, int y, out ComboBox cmb, int w)
    {
        AddLabel(lbl, y);
        cmb = new ComboBox { Location = new Point(110, y - 3), Size = new Size(w, 25), DropDownStyle = ComboBoxStyle.DropDown };
        this.Controls.Add(cmb);
    }
    private void AddLabel(string text, int y)
    {
        this.Controls.Add(new Label { Text = text, Location = new Point(25, y), AutoSize = true });
    }

    private void LoadSpecs()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var specs = db.Queryable<Specification>().Select(s => s.Name).ToList();
            foreach (var s in specs) if (!string.IsNullOrEmpty(s) && !cmbSpecification.Items.Contains(s)) cmbSpecification.Items.Add(s);
        }
        catch { }
    }

    private void LoadPart()
    {
        var db = SqlSugarHelper.Db;
        var part = db.Queryable<SparePart>().InSingle(_partId);
        if (part == null) return;
        txtName.Text = part.Name; cmbSpecification.Text = part.Specification; txtModel.Text = part.Model;
        txtManufacturer.Text = part.Manufacturer;
        nudShelfNo.Value = part.ShelfNo; nudLayerNo.Value = part.LayerNo; nudPositionNo.Value = part.PositionNo;
        txtRemark.Text = part.Remark;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var name = txtName.Text.Trim(); var spec = cmbSpecification.Text.Trim(); var model = txtModel.Text.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(spec) || string.IsNullOrEmpty(model))
        { MessageBox.Show("名称、规格、型号不能为空。"); return; }
        try
        {
            var db = SqlSugarHelper.Db;
            db.Updateable<SparePart>()
                .SetColumns(it => new SparePart
                {
                    Name = name, Specification = spec, Model = model,
                    Manufacturer = txtManufacturer.Text.Trim(),
                    ShelfNo = (int)nudShelfNo.Value, LayerNo = (int)nudLayerNo.Value, PositionNo = (int)nudPositionNo.Value,
                    Remark = txtRemark.Text.Trim()
                }).Where(it => it.Id == _partId).ExecuteCommand();
            this.DialogResult = DialogResult.OK; this.Close();
        }
        catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
