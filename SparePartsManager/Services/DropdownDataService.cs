using SparePartsManager.Data;
using SparePartsManager.Models;
using System.Collections.ObjectModel;

namespace SparePartsManager.Services;

/// <summary>
/// 共享下拉数据源服务（单例）。所有页面共用同一份数据，
/// 增删改后调用 <see cref="RefreshAll"/> 通知各页面更新。
/// 数据源：Specifications / PartModels / Manufacturers / Projects 四个字典表。
/// </summary>
public sealed class DropdownDataService
{
    private static readonly Lazy<DropdownDataService> _instance = new(() => new());
    public static DropdownDataService Instance => _instance.Value;

    private readonly object _lock = new();

    public ObservableCollection<DictItem> Specifications { get; } = new();
    public ObservableCollection<DictItem> Models { get; } = new();
    public ObservableCollection<DictItem> Manufacturers { get; } = new();
    public ObservableCollection<DictItem> Projects { get; } = new();

    /// <summary>数据源变更时触发，各 ViewModel 订阅后刷新绑定。</summary>
    public event Action? DataChanged;

    private DropdownDataService() { }

    /// <summary>从四个字典表重新加载所有下拉选项。</summary>
    public void RefreshAll()
    {
        lock (_lock)
        {
            var db = SqlSugarHelper.Db;

            var specs = db.Queryable<Specification>()
                .OrderBy(s => s.Name)
                .Select(s => new DictItem { Id = s.Id, Name = s.Name })
                .ToList();
            RefreshCollection(Specifications, specs);

            var models = db.Queryable<PartModel>()
                .OrderBy(m => m.Name)
                .Select(m => new DictItem { Id = m.Id, Name = m.Name })
                .ToList();
            RefreshCollection(Models, models);

            var manufacturers = db.Queryable<Manufacturer>()
                .OrderBy(m => m.Name)
                .Select(m => new DictItem { Id = m.Id, Name = m.Name })
                .ToList();
            RefreshCollection(Manufacturers, manufacturers);

            var projects = db.Queryable<Project>()
                .OrderBy(p => p.Name)
                .Select(p => new DictItem { Id = p.Id, Name = p.Name })
                .ToList();
            RefreshCollection(Projects, projects);
        }

        DataChanged?.Invoke();
    }

    /// <summary>复用 ObservableCollection，避免重建导致 UI 绑定断开。</summary>
    private static void RefreshCollection(ObservableCollection<DictItem> target, List<DictItem> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            if (!string.IsNullOrEmpty(item.Name))
                target.Add(item);
        }
    }
}
