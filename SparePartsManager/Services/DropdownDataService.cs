using SparePartsManager.Data;
using SparePartsManager.Models;
using System.Collections.ObjectModel;

namespace SparePartsManager.Services;

/// <summary>
/// 共享下拉数据源服务（单例）。所有页面共用同一份数据，
/// 增删改后调用 <see cref="RefreshAll"/> 通知各页面更新。
/// </summary>
public sealed class DropdownDataService
{
    private static readonly Lazy<DropdownDataService> _instance = new(() => new());
    public static DropdownDataService Instance => _instance.Value;

    private readonly object _lock = new();

    public ObservableCollection<string> Specifications { get; } = new();
    public ObservableCollection<string> Models { get; } = new();
    public ObservableCollection<string> Manufacturers { get; } = new();
    public ObservableCollection<string> Projects { get; } = new();

    /// <summary>数据源变更时触发，各 ViewModel 订阅后刷新绑定。</summary>
    public event Action? DataChanged;

    private DropdownDataService() { }

    /// <summary>从数据库重新加载所有下拉选项。</summary>
    public void RefreshAll()
    {
        lock (_lock)
        {
            var db = SqlSugarHelper.Db;

            // 规格：Specification 字典表 + SpareParts 已有记录
            var specsFromDict = db.Queryable<Specification>()
                .Select(s => s.Name)
                .ToList();
            var specsFromParts = db.Queryable<SparePart>()
                .Where(p => p.Specification != null && p.Specification != "")
                .Select(p => p.Specification)
                .ToList();
            var specs = specsFromDict.Union(specsFromParts)
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s)
                .ToList();
            RefreshCollection(Specifications, specs);

            // 型号：从 SpareParts 表 DISTINCT（内存排序避免 SQL 语法问题）
            var models = db.Queryable<SparePart>()
                .Where(p => p.Model != null && p.Model != "")
                .Select(p => p.Model)
                .Distinct()
                .ToList();
            models.Sort();
            RefreshCollection(Models, models);

            // 厂家：从 SpareParts 表 DISTINCT
            var manufacturers = db.Queryable<SparePart>()
                .Where(p => p.Manufacturer != null && p.Manufacturer != "")
                .Select(p => p.Manufacturer)
                .Distinct()
                .ToList();
            manufacturers.Sort();
            RefreshCollection(Manufacturers, manufacturers);

            // 项目：Project 字典表 + SpareParts 已有记录
            var projectsFromDict = db.Queryable<Project>()
                .Select(p => p.Name)
                .ToList();
            var projectsFromParts = db.Queryable<SparePart>()
                .Where(p => p.ProjectName != null && p.ProjectName != "")
                .Select(p => p.ProjectName)
                .ToList();
            var projects = projectsFromDict.Union(projectsFromParts)
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s)
                .ToList();
            RefreshCollection(Projects, projects);
        }

        DataChanged?.Invoke();
    }

    /// <summary>复用 ObservableCollection，避免重建导致 UI 绑定断开。</summary>
    private static void RefreshCollection(ObservableCollection<string> target, List<string> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            if (!string.IsNullOrEmpty(item))
                target.Add(item);
        }
    }
}
