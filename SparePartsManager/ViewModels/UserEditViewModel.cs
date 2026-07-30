using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class UserEditViewModel : ObservableObject
{
    private readonly User? _editUser;
    private bool _isPasswordChanged;

    public string WindowTitle => _editUser == null ? "新增用户" : "编辑用户";

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private bool _isUsernameReadOnly;
    public bool IsUsernameReadOnly
    {
        get => _isUsernameReadOnly;
        set => SetProperty(ref _isUsernameReadOnly, value);
    }

    private string _realName = string.Empty;
    public string RealName
    {
        get => _realName;
        set => SetProperty(ref _realName, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); _isPasswordChanged = true; }
    }

    private string _role = "Operator";
    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public UserEditViewModel(User? user = null)
    {
        _editUser = user;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);

        if (user != null)
        {
            Username = user.Username;
            IsUsernameReadOnly = true;
            RealName = user.RealName;
            Role = user.Role;
        }
    }

    private void Save()
    {
        var username = Username.Trim();
        var realName = RealName.Trim();
        var password = Password;
        var role = Role;

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
                    CreatedAt = System.DateTime.Now
                }).ExecuteCommand();
            }
            else
            {
                db.Updateable<User>()
                    .SetColumns(it => new User { RealName = realName, Role = role })
                    .Where(it => it.Id == _editUser.Id)
                    .ExecuteCommand();

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

            RequestClose?.Invoke(this, true);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    public event EventHandler<bool>? RequestClose;
}
