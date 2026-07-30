using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SparePartsManager.ViewModels;

public class MainViewModel : ObservableObject
{
    private string _userInfo = string.Empty;
    public string UserInfo
    {
        get => _userInfo;
        set => SetProperty(ref _userInfo, value);
    }

    private string _statusText = "就绪";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isWarningFlashing;
    public bool IsWarningFlashing
    {
        get => _isWarningFlashing;
        set => SetProperty(ref _isWarningFlashing, value);
    }

    private int _warningCount;
    public int WarningCount
    {
        get => _warningCount;
        set
        {
            SetProperty(ref _warningCount, value);
            OnPropertyChanged(nameof(HasWarning));
        }
    }

    public bool HasWarning => WarningCount > 0;

    private string _warningDetailText = string.Empty;
    public string WarningDetailText
    {
        get => _warningDetailText;
        set => SetProperty(ref _warningDetailText, value);
    }

    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set
        {
            SetProperty(ref _currentView, value);
            OnPropertyChanged(nameof(HasCurrentView));
        }
    }

    public bool HasCurrentView => CurrentView != null;

    private DispatcherTimer? _flashTimer;

    public RelayCommand<string> NavigateCommand { get; }
    public RelayCommand ExitCommand { get; }

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
        ExitCommand = new RelayCommand(Exit);

        if (CurrentUser.IsLoggedIn)
        {
            UserInfo = $"👤 {CurrentUser.LoginUser!.RealName}\n({CurrentUser.LoginUser.Role})";
        }

        CheckStockAlerts();
    }

    private void Navigate(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        StatusText = tag switch
        {
            "stockin" => "备件入库",
            "stockout" => "备件出库",
            "query" => "备件查询",
            "stats" => "统计分析",
            "users" => "用户管理",
            "alert" => "库存警告设置",
            _ => "就绪"
        };

        CurrentView = tag switch
        {
            "stockin" => new StockInViewModel(),
            "stockout" => new StockOutViewModel(),
            "query" => new QueryViewModel(),
            "stats" => new StatisticsViewModel(),
            "users" => CurrentUser.IsAdmin ? new UserManageViewModel() : null,
            "alert" => CurrentUser.IsAdmin ? new StockAlertViewModel() : null,
            _ => null
        };
    }

    private void Exit()
    {
        StopWarningFlash();
        Application.Current.Shutdown();
    }

    public void StartWarningFlash()
    {
        if (_flashTimer != null) return;

        _flashTimer = new DispatcherTimer();
        _flashTimer.Interval = TimeSpan.FromMilliseconds(500);
        _flashTimer.Tick += (s, e) =>
        {
            IsWarningFlashing = !IsWarningFlashing;
        };
        _flashTimer.Start();
        IsWarningFlashing = true;
    }

    public void StopWarningFlash()
    {
        if (_flashTimer == null) return;

        _flashTimer.Stop();
        _flashTimer = null;
        IsWarningFlashing = false;
    }

    private void CheckStockAlerts()
    {
        try
        {
            var db = SqlSugarHelper.Db;
            var alerts = db.Queryable<StockAlert>().ToList();

            var warningList = new System.Collections.Generic.List<string>();
            foreach (var alert in alerts)
            {
                var count = db.Queryable<SparePart>()
                    .Count(p => p.Specification == alert.Specification
                        && p.Model == alert.Model
                        && p.Status == "InStock");

                if (count < alert.Threshold)
                {
                    warningList.Add($"【{alert.Specification}】{alert.Model}：库存 {count}，低于阈值 {alert.Threshold}");
                }
            }

            if (warningList.Count > 0)
            {
                var msg = "以下备件库存不足：\n\n" + string.Join("\n", warningList);
                MessageBox.Show(msg, "⚠️ 库存警告", MessageBoxButton.OK, MessageBoxImage.Warning);

                WarningCount = warningList.Count;
                WarningDetailText = string.Join("  |  ", warningList);
                StatusText = $"⚠️ {WarningCount}种备件库存不足";
                StartWarningFlash();
            }
            else
            {
                WarningDetailText = string.Empty;
                StopWarningFlash();
            }
        }
        catch { }
    }
}
