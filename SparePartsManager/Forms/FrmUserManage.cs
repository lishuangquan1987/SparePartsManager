using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;

namespace SparePartsManager.Forms;

public partial class FrmUserManage : Form
{
    private DataGridView dgvUsers = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Panel pnlToolbar = null!;

    public FrmUserManage()
    {
        InitializeComponent();
        this.Load += FrmUserManage_Load;
    }

    private void InitializeComponent()
    {
        this.Text = "用户管理";
        this.BackColor = Color.White;

        // 工具栏
        pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.FromArgb(250, 250, 250),
            Padding = new Padding(10, 8, 10, 8)
        };

        btnAdd = CreateToolButton("➕ 新增用户", Color.FromArgb(0, 122, 204));
        btnAdd.Location = new Point(10, 8);
        btnAdd.Click += BtnAdd_Click;

        btnEdit = CreateToolButton("✏️ 编辑", Color.FromArgb(255, 185, 15));
        btnEdit.Location = new Point(130, 8);
        btnEdit.Click += BtnEdit_Click;

        btnDelete = CreateToolButton("🗑️ 删除", Color.FromArgb(220, 53, 69));
        btnDelete.Location = new Point(220, 8);
        btnDelete.Click += BtnDelete_Click;

        pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        // 数据表格
        dgvUsers = new DataGridView
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

        this.Controls.Add(dgvUsers);
        this.Controls.Add(pnlToolbar);
    }

    private static Button CreateToolButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            Size = new Size(110, 34),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9),
            UseVisualStyleBackColor = false
        };
    }

    private void FrmUserManage_Load(object? sender, EventArgs e) => LoadUsers();

    private void LoadUsers()
    {
        var db = SqlSugarHelper.Db;
        var users = db.Queryable<User>()
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.Username, u.RealName, u.Role, u.CreatedAt })
            .ToList();

        dgvUsers.DataSource = null;
        dgvUsers.DataSource = users;

        // 隐藏 Id 列
        if (dgvUsers.Columns["Id"] != null)
            dgvUsers.Columns["Id"].Visible = false;

        // 调整列头
        if (dgvUsers.Columns["Username"] != null)
            dgvUsers.Columns["Username"].HeaderText = "用户名";
        if (dgvUsers.Columns["RealName"] != null)
            dgvUsers.Columns["RealName"].HeaderText = "真实姓名";
        if (dgvUsers.Columns["Role"] != null)
            dgvUsers.Columns["Role"].HeaderText = "角色";
        if (dgvUsers.Columns["CreatedAt"] != null)
            dgvUsers.Columns["CreatedAt"].HeaderText = "创建时间";
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new FrmUserEdit();
        if (dlg.ShowDialog() == DialogResult.OK)
            LoadUsers();
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow?.DataBoundItem == null)
        {
            MessageBox.Show("请先选择要编辑的用户。", "提示");
            return;
        }

        try
        {
            dynamic row = dgvUsers.CurrentRow.DataBoundItem;
            var db = SqlSugarHelper.Db;
            var user = db.Queryable<User>().InSingle(row.Id);
            if (user == null) return;

            using var dlg = new FrmUserEdit(user);
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"编辑失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow?.DataBoundItem == null)
        {
            MessageBox.Show("请先选择要删除的用户。", "提示");
            return;
        }

        dynamic row = dgvUsers.CurrentRow.DataBoundItem;
        int userId = row.Id;

        // 不允许删除自己
        if (userId == CurrentUser.LoginUser!.Id)
        {
            MessageBox.Show("不能删除当前登录用户。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 不允许删除最后一个管理员
        try
        {
            var db = SqlSugarHelper.Db;
            var adminCount = db.Queryable<User>().Count(u => u.Role == "Admin");
            var targetUser = db.Queryable<User>().InSingle(userId);
            if (targetUser?.Role == "Admin" && adminCount <= 1)
            {
                MessageBox.Show("不能删除最后一个管理员账户。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除用户「{targetUser?.RealName}」吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                db.Deleteable<User>().In(userId).ExecuteCommand();
                LoadUsers();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
