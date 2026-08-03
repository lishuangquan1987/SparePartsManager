using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using SparePartsManager.Views;
using SqlSugar;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

/// <summary>基础字典条目（含备件引用数）</summary>
public class DictEntryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>被 SpareParts 引用的数量</summary>
    public int RefCount { get; set; }
}

/// <summary>
/// 基础信息维护：规格 / 型号 / 厂家 / 项目 四个字典表的增删改查。
/// 删除时若 SpareParts 表已引用该条目则禁止删除。
/// </summary>
public class BasicDataViewModel : ObservableObject
{
    public ObservableCollection<DictEntryViewModel> Specifications { get; } = new();
    public ObservableCollection<DictEntryViewModel> Models { get; } = new();
    public ObservableCollection<DictEntryViewModel> Manufacturers { get; } = new();
    public ObservableCollection<DictEntryViewModel> Projects { get; } = new();

    private DictEntryViewModel? _selectedSpecification;
    public DictEntryViewModel? SelectedSpecification
    {
        get => _selectedSpecification;
        set => SetProperty(ref _selectedSpecification, value);
    }

    private DictEntryViewModel? _selectedModel;
    public DictEntryViewModel? SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    private DictEntryViewModel? _selectedManufacturer;
    public DictEntryViewModel? SelectedManufacturer
    {
        get => _selectedManufacturer;
        set => SetProperty(ref _selectedManufacturer, value);
    }

    private DictEntryViewModel? _selectedProject;
    public DictEntryViewModel? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public BasicDataViewModel()
    {
        AddCommand = new RelayCommand(Add);
        EditCommand = new RelayCommand(Edit);
        DeleteCommand = new RelayCommand(Delete);
        LoadAll();
    }

    private string CurrentKind => SelectedTabIndex switch
    {
        0 => "规格",
        1 => "型号",
        2 => "厂家",
        3 => "项目",
        _ => ""
    };

    private DictEntryViewModel? SelectedEntry => SelectedTabIndex switch
    {
        0 => SelectedSpecification,
        1 => SelectedModel,
        2 => SelectedManufacturer,
        3 => SelectedProject,
        _ => null
    };

    private static Window? GetOwnerWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
    }

    public void LoadAll()
    {
        try
        {
            var db = SqlSugarHelper.Db;

            var specRefs = db.Queryable<SparePart>()
                .GroupBy(p => p.SpecificationId)
                .Select(g => new { Id = g.SpecificationId, C = SqlSugar.SqlFunc.AggregateCount(g.Id) })
                .ToList().ToDictionary(x => x.Id ?? -1, x => x.C);
            var modelRefs = db.Queryable<SparePart>()
                .GroupBy(p => p.ModelId)
                .Select(g => new { Id = g.ModelId, C = SqlSugar.SqlFunc.AggregateCount(g.Id) })
                .ToList().ToDictionary(x => x.Id ?? -1, x => x.C);
            var manRefs = db.Queryable<SparePart>()
                .GroupBy(p => p.ManufacturerId)
                .Select(g => new { Id = g.ManufacturerId, C = SqlSugar.SqlFunc.AggregateCount(g.Id) })
                .ToList().ToDictionary(x => x.Id ?? -1, x => x.C);
            var projRefs = db.Queryable<SparePart>()
                .GroupBy(p => p.ProjectId)
                .Select(g => new { Id = g.ProjectId, C = SqlSugar.SqlFunc.AggregateCount(g.Id) })
                .ToList().ToDictionary(x => x.Id ?? -1, x => x.C);

            Specifications.Clear();
            foreach (var s in db.Queryable<Specification>().OrderBy(x => x.Name).ToList())
            {
                Specifications.Add(new DictEntryViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    RefCount = specRefs.TryGetValue(s.Id, out var c) ? c : 0
                });
            }

            Models.Clear();
            foreach (var m in db.Queryable<PartModel>().OrderBy(x => x.Name).ToList())
            {
                Models.Add(new DictEntryViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    RefCount = modelRefs.TryGetValue(m.Id, out var c) ? c : 0
                });
            }

            Manufacturers.Clear();
            foreach (var m in db.Queryable<Manufacturer>().OrderBy(x => x.Name).ToList())
            {
                Manufacturers.Add(new DictEntryViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    RefCount = manRefs.TryGetValue(m.Id, out var c) ? c : 0
                });
            }

            Projects.Clear();
            foreach (var p in db.Queryable<Project>().OrderBy(x => x.Name).ToList())
            {
                Projects.Add(new DictEntryViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    RefCount = projRefs.TryGetValue(p.Id, out var c) ? c : 0
                });
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"加载基础信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Add()
    {
        var dialog = new InputDialog($"新增{CurrentKind}", $"请输入新{CurrentKind}名称：");
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var db = SqlSugarHelper.Db;
            if (NameExists(db, name))
            {
                MessageBox.Show($"该{CurrentKind}已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            switch (SelectedTabIndex)
            {
                case 0: db.Insertable(new Specification { Name = name }).ExecuteCommand(); break;
                case 1: db.Insertable(new PartModel { Name = name }).ExecuteCommand(); break;
                case 2: db.Insertable(new Manufacturer { Name = name }).ExecuteCommand(); break;
                case 3: db.Insertable(new Project { Name = name }).ExecuteCommand(); break;
            }

            LoadAll();
            DropdownDataService.Instance.RefreshAll();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Edit()
    {
        var entry = SelectedEntry;
        if (entry == null)
        {
            MessageBox.Show($"请先选择要编辑的{CurrentKind}。", "提示");
            return;
        }

        var dialog = new InputDialog($"编辑{CurrentKind}", $"请输入新的{CurrentKind}名称：", entry.Name);
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (name == entry.Name) return; // 未修改

        try
        {
            var db = SqlSugarHelper.Db;
            if (NameExists(db, name))
            {
                MessageBox.Show($"该{CurrentKind}已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            switch (SelectedTabIndex)
            {
                case 0:
                    db.Updateable<Specification>()
                        .SetColumns(x => new Specification { Name = name })
                        .Where(x => x.Id == entry.Id).ExecuteCommand();
                    break;
                case 1:
                    db.Updateable<PartModel>()
                        .SetColumns(x => new PartModel { Name = name })
                        .Where(x => x.Id == entry.Id).ExecuteCommand();
                    break;
                case 2:
                    db.Updateable<Manufacturer>()
                        .SetColumns(x => new Manufacturer { Name = name })
                        .Where(x => x.Id == entry.Id).ExecuteCommand();
                    break;
                case 3:
                    db.Updateable<Project>()
                        .SetColumns(x => new Project { Name = name })
                        .Where(x => x.Id == entry.Id).ExecuteCommand();
                    break;
            }

            LoadAll();
            DropdownDataService.Instance.RefreshAll();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"编辑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete()
    {
        var entry = SelectedEntry;
        if (entry == null)
        {
            MessageBox.Show($"请先选择要删除的{CurrentKind}。", "提示");
            return;
        }

        var confirm = MessageBox.Show(
            $"确定要删除「{entry.Name}」吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var db = SqlSugarHelper.Db;

            // 引用计数检查与删除在同一事务内，避免 TOCTOU
            db.Ado.BeginTran();
            try
            {
                var refCount = SelectedTabIndex switch
                {
                    0 => db.Queryable<SparePart>().Count(p => p.SpecificationId == entry.Id),
                    1 => db.Queryable<SparePart>().Count(p => p.ModelId == entry.Id),
                    2 => db.Queryable<SparePart>().Count(p => p.ManufacturerId == entry.Id),
                    3 => db.Queryable<SparePart>().Count(p => p.ProjectId == entry.Id),
                    _ => 0
                };

                if (refCount > 0)
                {
                    db.Ado.RollbackTran();
                    MessageBox.Show(
                        $"无法删除：该{CurrentKind}已被 {refCount} 件备件使用，请先修改相关备件。",
                        "无法删除", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                switch (SelectedTabIndex)
                {
                    case 0:
                        // 规格被删除后，其库存警告规则一并失效，同步清理
                        db.Deleteable<StockAlert>().Where(a => a.SpecificationId == entry.Id).ExecuteCommand();
                        db.Deleteable<Specification>().In(entry.Id).ExecuteCommand();
                        break;
                    case 1:
                        db.Deleteable<StockAlert>().Where(a => a.ModelId == entry.Id).ExecuteCommand();
                        db.Deleteable<PartModel>().In(entry.Id).ExecuteCommand();
                        break;
                    case 2:
                        db.Deleteable<Manufacturer>().In(entry.Id).ExecuteCommand();
                        break;
                    case 3:
                        db.Deleteable<Project>().In(entry.Id).ExecuteCommand();
                        break;
                }
                db.Ado.CommitTran();
            }
            catch
            {
                db.Ado.RollbackTran();
                throw;
            }

            LoadAll();
            DropdownDataService.Instance.RefreshAll();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool NameExists(ISqlSugarClient db, string name) => SelectedTabIndex switch
    {
        0 => db.Queryable<Specification>().Any(s => s.Name == name),
        1 => db.Queryable<PartModel>().Any(m => m.Name == name),
        2 => db.Queryable<Manufacturer>().Any(m => m.Name == name),
        3 => db.Queryable<Project>().Any(p => p.Name == name),
        _ => false
    };
}
