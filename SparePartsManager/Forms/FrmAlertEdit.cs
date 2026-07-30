using SparePartsManager.Data;
using SparePartsManager.Models;

namespace SparePartsManager.Forms;

/// <summary>
/// 库存警告规则新增/编辑
/// </summary>
public partial class FrmAlertEdit : Form
{
    private readonly StockAlert? _editAlert;
    private ComboBox cmbSpecification = null!;
    private TextBox txtModel = null!;
    private NumericUpDown nudThreshold = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public FrmAlertEdit(StockAlert? alert = null)
    {
        _editAlert = alert;
        InitializeComponent();
        if (alert != null)
        {
            this.Text = "编辑警告规则";
            cmbSpecification.Text = alert.Specification;
            cmbSpecification.Enabled = false; // 编辑时不可修改规格
            txtModel.Text = alert.Model;
            txtModel.ReadOnly = true; // 编辑时不可修改型号
            nudThreshold.Value = alert.Threshold;
        }
        else
        {
            this.Text = "新增警告规则";
        }
    }

    private void InitializeComponent()
    {
        this.Size = new Size(460, 280);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var labelWidth = 100;
        var y = 25;

        // 规格
        AddLabel("规格：", y);
        cmbSpecification = new ComboBox
        {
            Location = new Point(labelWidth + 20, y - 3),
            Size = new Size(260, 25),
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cmbSpecification.Items.AddRange(new[] { "电源", "PLC", "空开", "接触器", "继电器", "传感器", "变频器", "按钮", "指示灯", "端子" });
        y += 42;

        // 型号
        AddLabel("型号：", y);
        txtModel = new TextBox { Location = new Point(labelWidth + 20, y - 3), Size = new Size(260, 25) };
        y += 42;

        // 阈值
        AddLabel("最低阈值：", y);
        nudThreshold = new NumericUpDown
        {
            Location = new Point(labelWidth + 20, y - 3),
            Size = new Size(120, 25),
            Minimum = 1,
            Maximum = 9999,
            Value = 5
        };
        y += 50;

        // 按钮
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(labelWidth + 40, y),
            Size = new Size(100, 38),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(labelWidth + 160, y),
            Size = new Size(100, 38)
        };
        btnCancel.Click += (s, e) => this.Close();

        this.Controls.AddRange(new Control[]
        {
            cmbSpecification, txtModel, nudThreshold, btnSave, btnCancel
        });
    }

    private void AddLabel(string text, int y)
    {
        this.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(25, y),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight
        });
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var spec = cmbSpecification.Text.Trim();
        var model = txtModel.Text.Trim();
        var threshold = (int)nudThreshold.Value;

        if (string.IsNullOrEmpty(spec))
        {
            MessageBox.Show("请输入/选择规格。", "提示");
            return;
        }
        if (string.IsNullOrEmpty(model))
        {
            MessageBox.Show("请输入型号。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;

            // 检查是否已存在相同规格+型号的规则
            var exists = db.Queryable<StockAlert>()
                .Any(a => a.Specification == spec && a.Model == model
                    && (_editAlert == null || a.Id != _editAlert.Id));
            if (exists)
            {
                MessageBox.Show("该「规格 + 型号」的警告规则已存在，请编辑已有规则。", "提示");
                return;
            }

            if (_editAlert == null)
            {
                db.Insertable(new StockAlert
                {
                    Specification = spec,
                    Model = model,
                    Threshold = threshold
                }).ExecuteCommand();
            }
            else
            {
                db.Updateable<StockAlert>()
                    .SetColumns(it => new StockAlert
                    {
                        Specification = spec,
                        Model = model,
                        Threshold = threshold
                    })
                    .Where(it => it.Id == _editAlert.Id)
                    .ExecuteCommand();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
