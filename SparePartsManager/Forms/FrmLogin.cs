using SparePartsManager.Data;
using SparePartsManager.Services;

namespace SparePartsManager.Forms;

public partial class FrmLogin : Form
{
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Button btnCancel = null!;
    private Label lblTitle = null!;
    private Label lblUsername = null!;
    private Label lblPassword = null!;
    private int _loginFailCount = 0;
    private System.Windows.Forms.Timer? _lockTimer;

    public FrmLogin()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "备件库管理系统 — 登录";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Size = new Size(420, 320);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // 标题
        lblTitle = new Label
        {
            Text = "备件库管理系统",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(135, 30)
        };

        // 用户名
        lblUsername = new Label
        {
            Text = "用户名：",
            Location = new Point(80, 100),
            AutoSize = true
        };
        txtUsername = new TextBox
        {
            Location = new Point(160, 97),
            Size = new Size(180, 25)
        };

        // 密码
        lblPassword = new Label
        {
            Text = "密　码：",
            Location = new Point(80, 145),
            AutoSize = true
        };
        txtPassword = new TextBox
        {
            Location = new Point(160, 142),
            Size = new Size(180, 25),
            PasswordChar = '*'
        };

        // 登录按钮
        btnLogin = new Button
        {
            Text = "登录",
            Location = new Point(100, 200),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        // 取消按钮
        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(210, 200),
            Size = new Size(90, 35)
        };
        btnCancel.Click += (s, e) => Application.Exit();

        this.Controls.AddRange(new Control[]
        {
            lblTitle, lblUsername, txtUsername,
            lblPassword, txtPassword, btnLogin, btnCancel
        });

        this.AcceptButton = btnLogin;
        this.CancelButton = btnCancel;
    }

    private void ReportFail()
    {
        var remain = 5 - _loginFailCount;
        if (remain > 0)
        {
            MessageBox.Show($"用户名或密码错误。\n剩余尝试次数：{remain}", "登录失败",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            // 真正锁定
            btnLogin.Enabled = false;
            _lockTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
            _lockTimer.Tick += (s, e) =>
            {
                _lockTimer.Stop();
                _loginFailCount = 0;
                btnLogin.Enabled = true;
            };
            _lockTimer.Start();
            MessageBox.Show("登录失败次数过多，已锁定 30 秒。", "安全锁定",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("请输入用户名和密码。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 暴力破解防护：5次失败后锁定30秒
        if (_loginFailCount >= 5)
        {
            MessageBox.Show("登录失败次数过多，请等待 30 秒后重试。", "安全锁定",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            // 先查用户获取 salt
            var user = db.Queryable<Models.User>()
                .First(u => u.Username == username);
            if (user == null)
            {
                _loginFailCount++;
                ReportFail();
                return;
            }

            var passwordHash = SqlSugarHelper.HashPassword(password, user.Salt);
            if (user.PasswordHash != passwordHash)
            {
                _loginFailCount++;
                ReportFail();
                return;
            }

            // 登录成功，重置计数器
            _loginFailCount = 0;
            CurrentUser.LoginUser = user;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"登录时发生错误：{ex.Message}", "系统错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
