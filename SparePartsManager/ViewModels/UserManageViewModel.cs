using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string RealName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UserManageViewModel : ObservableObject
{
    public ObservableCollection<UserListItemViewModel> Users { get; } = new ObservableCollection<UserListItemViewModel>();

    private UserListItemViewModel? _selectedUser;
    public UserListItemViewModel? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public UserManageViewModel()
    {
        AddCommand = new RelayCommand(Add);
        EditCommand = new RelayCommand(Edit);
        DeleteCommand = new RelayCommand(Delete);
        LoadUsers();
    }

    public void LoadUsers()
    {
        var db = SqlSugarHelper.Db;
        var users = db.Queryable<User>()
            .OrderBy(u => u.Id)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Username = u.Username,
                RealName = u.RealName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToList();

        Users.Clear();
        foreach (var u in users) Users.Add(u);
    }

    private void Add()
    {
        var window = new Views.UserEditWindow();
        if (window.ShowDialog() == true)
            LoadUsers();
    }

    private void Edit()
    {
        if (SelectedUser == null)
        {
            MessageBox.Show("请先选择要编辑的用户。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var user = db.Queryable<User>().InSingle(SelectedUser.Id);
            if (user == null) return;

            var window = new Views.UserEditWindow(user);
            if (window.ShowDialog() == true)
                LoadUsers();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"编辑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete()
    {
        if (SelectedUser == null)
        {
            MessageBox.Show("请先选择要删除的用户。", "提示");
            return;
        }

        int userId = SelectedUser.Id;

        if (userId == CurrentUser.LoginUser!.Id)
        {
            MessageBox.Show("不能删除当前登录用户。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var adminCount = db.Queryable<User>().Count(u => u.Role == "Admin");
            var targetUser = db.Queryable<User>().InSingle(userId);
            if (targetUser?.Role == "Admin" && adminCount <= 1)
            {
                MessageBox.Show("不能删除最后一个管理员账户。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除用户「{targetUser?.RealName}」吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                db.Deleteable<User>().In(userId).ExecuteCommand();
                LoadUsers();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
