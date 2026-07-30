using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Services;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class LoginViewModel : ObservableObject
{
    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private int _loginFailCount;
    private bool _isLocked;
    private DateTime _lockEndTime;

    public RelayCommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(Login);
    }

    public bool TryLogin()
    {
        return LoginCore();
    }

    private void Login()
    {
        LoginCore();
    }

    private bool LoginCore()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "请输入用户名和密码。";
            return false;
        }

        if (_isLocked && DateTime.Now < _lockEndTime)
        {
            var remain = (int)(_lockEndTime - DateTime.Now).TotalSeconds;
            ErrorMessage = $"登录失败次数过多，请等待 {remain} 秒后重试。";
            return false;
        }

        // 锁定时间已过，重置计数器
        if (_isLocked)
        {
            _isLocked = false;
            _loginFailCount = 0;
        }

        if (_loginFailCount >= 5)
        {
            _isLocked = true;
            _lockEndTime = DateTime.Now.AddSeconds(30);
            ErrorMessage = "登录失败次数过多，已锁定 30 秒。";
            return false;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var user = db.Queryable<Models.User>()
                .First(u => u.Username == Username);

            if (user == null)
            {
                _loginFailCount++;
                var remain = 5 - _loginFailCount;
                ErrorMessage = remain > 0
                    ? $"用户名或密码错误。剩余尝试次数：{remain}"
                    : "登录失败次数过多，已锁定 30 秒。";
                return false;
            }

            var passwordHash = SqlSugarHelper.HashPassword(Password, user.Salt);
            if (user.PasswordHash != passwordHash)
            {
                _loginFailCount++;
                var remain = 5 - _loginFailCount;
                ErrorMessage = remain > 0
                    ? $"用户名或密码错误。剩余尝试次数：{remain}"
                    : "登录失败次数过多，已锁定 30 秒。";
                return false;
            }

            _loginFailCount = 0;
            CurrentUser.LoginUser = user;
            return true;
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"登录时发生错误：{ex.Message}";
            return false;
        }
    }
}
