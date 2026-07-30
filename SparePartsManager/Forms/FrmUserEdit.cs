using SparePartsManager.Data;
using SparePartsManager.Models;

namespace SparePartsManager.Forms;

/// <summary>
/// 用户新增/编辑窗体
/// </summary>
public partial class FrmUserEdit : Form
{
    private readonly User? _editUser;
    private bool _isPasswordChanged;
    private TextBox txtUsername = null!;
    private TextBox txtRealName = null!;
    private TextBox txtPassword = null!;
    private ComboBox cmbRole = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public FrmUserEdit(User? user = null)
    {
        _editUser = user;
        InitializeComponent();
        if (user != null)
        {
            this.Text = "编辑用户";
            txtUsername.Text = user.Username;
            txtUsername.ReadOnly = true;
            txtRealName.Text = user.RealName;
            cmbRole.SelectedItem = user.Role;
#if NET8_0_OR_GREATER
            txtPassword.PlaceholderText = "留空则不修改密码";
#endif
        }
        else
        {
            this.Text = "新增用户";
        }
    }

    private void InitializeComponent()
    {
        this.Size = new Size(460, 320);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        var y = 20;
        var labelWidth = 100;

        // 用户名
        AddLabel("用户名：", y);
        txtUsername = new TextBox { Location = new Point(labelWidth + 20, y - 3), Size = new Size(240, 25) };
        y += 40;

        // 真实姓名
        AddLabel("真实姓名：", y);
        txtRealName = new TextBox { Location = new Point(labelWidth + 20, y - 3), Size = new Size(240, 25) };
        y += 40;

        // 密码
        AddLabel("密码：", y);
        txtPassword = new TextBox { Location = new Point(labelWidth + 20, y - 3), Size = new Size(240, 25), PasswordChar = '*' };
        txtPassword.TextChanged += (s, e) => _isPasswordChanged = true;
        y += 40;

        // 角色
        AddLabel("角色：", y);
        cmbRole = new ComboBox
        {
            Location = new Point(labelWidth + 20, y - 3),
            Size = new Size(240, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbRole.Items.AddRange(new[] { "Admin", "Operator" });
        cmbRole.SelectedIndex = 1;
        y += 50;

        // 保存按钮
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(labelWidth + 40, y),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.Click += BtnSave_Click;

        // 取消按钮
        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(labelWidth + 150, y),
            Size = new Size(90, 35)
        };
        btnCancel.Click += (s, e) => this.Close();

        this.Controls.AddRange(new Control[]
        {
            txtUsername, txtRealName, txtPassword, cmbRole, btnSave, btnCancel
        });

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
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
        var username = txtUsername.Text.Trim();
        var realName = txtRealName.Text.Trim();
        var password = txtPassword.Text;
        var role = cmbRole.SelectedItem?.ToString() ?? "Operator";

        if (string.IsNullOrEmpty(username))
        {
            MessageBox.Show("请输入用户名。", "提示");
            return;
        }
        if (string.IsNullOrEmpty(realName))
        {
            MessageBox.Show("请输入真实姓名。", "提示");
            return;
        }

        var db = SqlSugarHelper.Db;

        try
        {
            // 新增时检查用户名唯一性
            if (_editUser == null)
            {
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("请设置密码。", "提示");
                    return;
                }
                var exists = db.Queryable<User>().Any(u => u.Username == username);
                if (exists)
                {
                    MessageBox.Show("用户名已存在。", "提示");
                    return;
                }

                var salt = SqlSugarHelper.GenerateSalt();
                db.Insertable(new User
                {
                    Username = username,
                    RealName = realName,
                    PasswordHash = SqlSugarHelper.HashPassword(password, salt),
                    Salt = salt,
                    Role = role,
                    CreatedAt = DateTime.Now
                }).ExecuteCommand();
            }
            else
            {
                // 编辑：更新姓名、角色
                db.Updateable<User>()
                    .SetColumns(it => new User { RealName = realName, Role = role })
                    .Where(it => it.Id == _editUser.Id)
                    .ExecuteCommand();

                // 如果用户修改了密码（生成新盐）
                if (_isPasswordChanged && !string.IsNullOrEmpty(password))
                {
                    var newSalt = SqlSugarHelper.GenerateSalt();
                    db.Updateable<User>()
                        .SetColumns(it => new User
                        {
                            PasswordHash = SqlSugarHelper.HashPassword(password, newSalt),
                            Salt = newSalt
                        })
                        .Where(it => it.Id == _editUser.Id)
                        .ExecuteCommand();
                }
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
