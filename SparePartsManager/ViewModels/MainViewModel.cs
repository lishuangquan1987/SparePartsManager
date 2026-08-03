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

    private string? _currentNavTag;

    private StockAlertViewModel? _alertViewModel;

    /// <summary>
    /// 会话内是否已单次消除报警（消除后规则变更不再自动重新拉起，重启后重置）。
    /// </summary>
    private bool _warningsDismissed;

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
    public RelayCommand DismissWarningCommand { get; }

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
        ExitCommand = new RelayCommand(Exit);
        DismissWarningCommand = new RelayCommand(DismissWarning);

        if (CurrentUser.IsLoggedIn)
        {
            UserInfo = $"👤 {CurrentUser.LoginUser!.RealName}\n({CurrentUser.LoginUser.Role})";
        }

        // 基础字典（规格/型号/厂家/项目）变化时重新检查库存报警
        DropdownDataService.Instance.DataChanged += RefreshStockAlerts;

        CheckStockAlerts();
    }

    private void Navigate(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // 离开库存警告页时解除订阅，避免旧视图被事件长期引用
        if (_alertViewModel != null)
        {
            _alertViewModel.AlertsChanged -= RefreshStockAlerts;
            _alertViewModel = null;
        }

        _currentNavTag = tag;
        StatusText = GetStatusText(tag);

        switch (tag)
        {
            case "stockin":
                CurrentView = new StockInViewModel();
                break;
            case "stockout":
                CurrentView = new StockOutViewModel();
                break;
            case "query":
                CurrentView = new QueryViewModel();
                break;
            case "stats":
                CurrentView = new StatisticsViewModel();
                break;
            case "users":
                CurrentView = CurrentUser.IsAdmin ? new UserManageViewModel() : null;
                break;
            case "basicdata":
                CurrentView = CurrentUser.IsAdmin ? new BasicDataViewModel() : null;
                break;
            case "alert":
                _alertViewModel = CurrentUser.IsAdmin ? new StockAlertViewModel() : null;
                if (_alertViewModel != null)
                    _alertViewModel.AlertsChanged += RefreshStockAlerts;

                CurrentView = _alertViewModel;
                break;
            default:
                CurrentView = null;
                break;
        }
    }

    private static string GetStatusText(string? tag) => tag switch
    {
        "stockin" => "备件入库",
        "stockout" => "备件出库",
        "query" => "备件查询",
        "stats" => "统计分析",
        "users" => "用户管理",
        "basicdata" => "基础信息维护",
        "alert" => "库存警告设置",
        _ => "就绪"
    };

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
            ApplyWarnings(ComputeWarnings(), showPopup: true);
        }
        catch { }
    }

    /// <summary>
    /// 库存警告规则发生变化后重新检查报警（静默，不弹窗）。
    /// 由 StockAlertViewModel 在新增/编辑/删除规则后触发。
    /// </summary>
    public void RefreshStockAlerts()
    {
        // 本次会话已单次消除报警：规则变更不再把相同报警重新拉起，重启后自然恢复
        if (_warningsDismissed) return;

        try
        {
            ApplyWarnings(ComputeWarnings(), showPopup: false);
        }
        catch { }
    }

    /// <summary>
    /// 单次消除报警：只清除当前 UI 报警状态，不删除规则，下次启动仍会重新报警。
    /// </summary>
    private void DismissWarning()
    {
        _warningsDismissed = true;
        WarningCount = 0;
        WarningDetailText = string.Empty;
        StatusText = GetStatusText(_currentNavTag);
        StopWarningFlash();
    }

    private System.Collections.Generic.List<string> ComputeWarnings()
    {
        var db = SqlSugarHelper.Db;
        var alerts = db.Queryable<StockAlert>().ToList();

        var specDict = db.Queryable<Specification>().ToList().ToDictionary(s => s.Id, s => s.Name);
        var modelDict = db.Queryable<PartModel>().ToList().ToDictionary(m => m.Id, m => m.Name);

        var warningList = new System.Collections.Generic.List<string>();
        foreach (var alert in alerts)
        {
            var count = db.Queryable<SparePart>()
                .Count(p => p.SpecificationId == alert.SpecificationId
                    && p.ModelId == alert.ModelId
                    && p.Status == "InStock");

            if (count < alert.Threshold)
            {
                var specName = alert.SpecificationId.HasValue && specDict.TryGetValue(alert.SpecificationId.Value, out var sn) ? sn : "";
                var modelName = alert.ModelId.HasValue && modelDict.TryGetValue(alert.ModelId.Value, out var mn) ? mn : "";
                warningList.Add($"【{specName}】{modelName}：库存 {count}，低于阈值 {alert.Threshold}");
            }
        }

        return warningList;
    }

    private void ApplyWarnings(System.Collections.Generic.List<string> warningList, bool showPopup)
    {
        if (warningList.Count > 0)
        {
            if (showPopup)
            {
                var msg = "以下备件库存不足：\n\n" + string.Join("\n", warningList);
                MessageBox.Show(msg, "⚠️ 库存警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            WarningCount = warningList.Count;
            WarningDetailText = string.Join("  |  ", warningList);
            StatusText = $"⚠️ {WarningCount}种备件库存不足";
            StartWarningFlash();
        }
        else
        {
            // 报警全部消除：完整重置状态，避免残留旧报警
            WarningCount = 0;
            WarningDetailText = string.Empty;
            StatusText = GetStatusText(_currentNavTag);
            StopWarningFlash();
        }
    }
}
