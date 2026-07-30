using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class StockOutItemViewModel : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string? ProjectName { get; set; }
    public int ShelfNo { get; set; }
    public int LayerNo { get; set; }
    public int PositionNo { get; set; }
    public DateTime StockInDate { get; set; }
    public string StockInPerson { get; set; } = "";
    public string Remark { get; set; } = "";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class StockOutViewModel : ObservableObject
{
    // ========== 下拉框选项 ==========

    private ObservableCollection<string> _specOptions = new();
    public ObservableCollection<string> SpecOptions
    {
        get => _specOptions;
        set => SetProperty(ref _specOptions, value);
    }

    private ObservableCollection<string> _modelOptions = new();
    public ObservableCollection<string> ModelOptions
    {
        get => _modelOptions;
        set => SetProperty(ref _modelOptions, value);
    }

    private ObservableCollection<string> _manufacturerOptions = new();
    public ObservableCollection<string> ManufacturerOptions
    {
        get => _manufacturerOptions;
        set => SetProperty(ref _manufacturerOptions, value);
    }

    private ObservableCollection<string> _projectOptions = new();
    public ObservableCollection<string> ProjectOptions
    {
        get => _projectOptions;
        set => SetProperty(ref _projectOptions, value);
    }

    // ========== 搜索字段 ==========

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    private string _searchName = string.Empty;
    public string SearchName
    {
        get => _searchName;
        set => SetProperty(ref _searchName, value);
    }

    private string _searchSpecification = string.Empty;
    public string SearchSpecification
    {
        get => _searchSpecification;
        set => SetProperty(ref _searchSpecification, value);
    }

    private string _searchModel = string.Empty;
    public string SearchModel
    {
        get => _searchModel;
        set => SetProperty(ref _searchModel, value);
    }

    private string _searchManufacturer = string.Empty;
    public string SearchManufacturer
    {
        get => _searchManufacturer;
        set => SetProperty(ref _searchManufacturer, value);
    }

    private string _searchProjectName = string.Empty;
    public string SearchProjectName
    {
        get => _searchProjectName;
        set => SetProperty(ref _searchProjectName, value);
    }

    private int? _searchShelfNo;
    public int? SearchShelfNo
    {
        get => _searchShelfNo;
        set => SetProperty(ref _searchShelfNo, value);
    }

    private int? _searchLayerNo;
    public int? SearchLayerNo
    {
        get => _searchLayerNo;
        set => SetProperty(ref _searchLayerNo, value);
    }

    private int? _searchPositionNo;
    public int? SearchPositionNo
    {
        get => _searchPositionNo;
        set => SetProperty(ref _searchPositionNo, value);
    }

    private DateTime? _stockInDateFrom;
    public DateTime? StockInDateFrom
    {
        get => _stockInDateFrom;
        set => SetProperty(ref _stockInDateFrom, value);
    }

    private DateTime? _stockInDateTo;
    public DateTime? StockInDateTo
    {
        get => _stockInDateTo;
        set => SetProperty(ref _stockInDateTo, value);
    }

    private string _searchStockInPerson = string.Empty;
    public string SearchStockInPerson
    {
        get => _searchStockInPerson;
        set => SetProperty(ref _searchStockInPerson, value);
    }

    // ========== 忙碌状态 ==========

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private bool _selectAll;
    public bool SelectAll
    {
        get => _selectAll;
        set
        {
            if (SetProperty(ref _selectAll, value))
            {
                foreach (var item in Parts)
                    item.IsSelected = value;
            }
        }
    }

    // ========== 分页 ==========

    public int PageSize { get; } = 20;

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set
        {
            SetProperty(ref _totalCount, value);
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    public string PageInfo => TotalCount > 0
        ? $"第{CurrentPage}页/共{TotalPages}页 共{TotalCount}条"
        : "无数据";

    // ========== 备件列表 ==========

    public ObservableCollection<StockOutItemViewModel> Parts { get; } = new();

    // ========== 命令 ==========

    public RelayCommand StockOutCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand FirstPageCommand { get; }
    public RelayCommand PrevPageCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public RelayCommand LastPageCommand { get; }

    public StockOutViewModel()
    {
        StockOutCommand = new RelayCommand(StockOut);
        SearchCommand = new RelayCommand(Search);
        ResetCommand = new RelayCommand(Reset);
        FirstPageCommand = new RelayCommand(FirstPage, () => CurrentPage > 1);
        PrevPageCommand = new RelayCommand(PrevPage, () => CurrentPage > 1);
        NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(LastPage, () => CurrentPage < TotalPages);

        LoadDropdownOptions();
        LoadParts();
    }

    // ========== 加载下拉选项 ==========

    private void LoadDropdownOptions()
    {
        try
        {
            var db = SqlSugarHelper.Db;

            var specs = db.Queryable<SparePart>()
                .Where(p => p.Specification != null && p.Specification != "")
                .Select(p => p.Specification)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            SpecOptions = new ObservableCollection<string>(specs);

            var models = db.Queryable<SparePart>()
                .Where(p => p.Model != null && p.Model != "")
                .Select(p => p.Model)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ModelOptions = new ObservableCollection<string>(models);

            var manufacturers = db.Queryable<SparePart>()
                .Where(p => p.Manufacturer != null && p.Manufacturer != "")
                .Select(p => p.Manufacturer)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ManufacturerOptions = new ObservableCollection<string>(manufacturers);

            var projects = db.Queryable<SparePart>()
                .Where(p => p.ProjectName != null && p.ProjectName != "")
                .Select(p => p.ProjectName)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            ProjectOptions = new ObservableCollection<string>(projects);
        }
        catch
        {
            // 忽略加载下拉选项时的错误
        }
    }

    // ========== 加载备件列表 ==========

    public void LoadParts()
    {
        IsBusy = true;
        try
        {
            var db = SqlSugarHelper.Db;

            var keyword = SearchText?.Trim() ?? "";
            var name = SearchName?.Trim() ?? "";
            var spec = SearchSpecification?.Trim() ?? "";
            var model = SearchModel?.Trim() ?? "";
            var manufacturer = SearchManufacturer?.Trim() ?? "";
            var projectName = SearchProjectName?.Trim() ?? "";
            var stockInPerson = SearchStockInPerson?.Trim() ?? "";

            var query = db.Queryable<SparePart>()
                .Where(p => p.Status == "InStock")
                .WhereIF(!string.IsNullOrEmpty(keyword),
                    p => p.Name.Contains(keyword) || p.Model.Contains(keyword))
                .WhereIF(!string.IsNullOrEmpty(name), p => p.Name.Contains(name))
                .WhereIF(!string.IsNullOrEmpty(spec), p => p.Specification.Contains(spec))
                .WhereIF(!string.IsNullOrEmpty(model), p => p.Model.Contains(model))
                .WhereIF(!string.IsNullOrEmpty(manufacturer), p => p.Manufacturer.Contains(manufacturer))
                .WhereIF(!string.IsNullOrEmpty(projectName), p => p.ProjectName != null && p.ProjectName.Contains(projectName))
                .WhereIF(SearchShelfNo.HasValue, p => p.ShelfNo == SearchShelfNo.Value)
                .WhereIF(SearchLayerNo.HasValue, p => p.LayerNo == SearchLayerNo.Value)
                .WhereIF(SearchPositionNo.HasValue, p => p.PositionNo == SearchPositionNo.Value)
                .WhereIF(StockInDateFrom.HasValue, p => p.StockInDate >= StockInDateFrom.Value)
                .WhereIF(StockInDateTo.HasValue, p => p.StockInDate <= StockInDateTo.Value)
                .WhereIF(!string.IsNullOrEmpty(stockInPerson), p => p.StockInPerson.Contains(stockInPerson));

            TotalCount = query.Count();

            var parts = query
                .OrderBy(p => p.StockInDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new StockOutItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Specification = p.Specification,
                    Model = p.Model,
                    Manufacturer = p.Manufacturer,
                    ShelfNo = p.ShelfNo,
                    LayerNo = p.LayerNo,
                    PositionNo = p.PositionNo,
                    StockInDate = p.StockInDate,
                    StockInPerson = p.StockInPerson,
                    Remark = p.Remark,
                    ProjectName = p.ProjectName
                }).ToList();

            Parts.Clear();
            foreach (var item in parts)
                Parts.Add(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ========== 出库 ==========

    private List<StockOutItemViewModel> GetCheckedItems()
    {
        return Parts.Where(p => p.IsSelected).ToList();
    }

    private void StockOut()
    {
        var selected = GetCheckedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show("请至少勾选一条在库备件。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var grouped = selected
            .GroupBy(s => $"{s.Specification}-{s.Model}")
            .Select(g => $"【{g.Key}】×{g.Count()}")
            .ToList();

        var message = $"确认出库以下 {selected.Count} 件？\n\n" +
                      string.Join("\n", grouped.Take(30)) +
                      (grouped.Count > 30 ? $"\n... 共 {grouped.Count} 类" : "");

        var confirm = MessageBox.Show(message, "确认出库", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var outPerson = CurrentUser.LoginUser?.RealName ?? "";
            var now = DateTime.Now;
            var ids = selected.Select(s => s.Id).ToList();
            var db = SqlSugarHelper.Db;

            var affected = db.Updateable<SparePart>()
                .SetColumns(it => new SparePart
                {
                    Status = "OutStock",
                    StockOutDate = now,
                    StockOutPerson = outPerson
                })
                .Where(it => ids.Contains(it.Id) && it.Status == "InStock")
                .ExecuteCommand();

            var resultMsg = $"出库完成！成功 {affected} 件";
            if (affected < selected.Count)
                resultMsg += $"，{selected.Count - affected} 件可能已被他人出库";

            MessageBox.Show(resultMsg, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadParts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"出库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ========== 搜索 ==========

    private void Search()
    {
        CurrentPage = 1;
        LoadParts();
    }

    // ========== 重置 ==========

    private void Reset()
    {
        SearchText = string.Empty;
        SearchName = string.Empty;
        SearchSpecification = string.Empty;
        SearchModel = string.Empty;
        SearchManufacturer = string.Empty;
        SearchProjectName = string.Empty;
        SearchShelfNo = null;
        SearchLayerNo = null;
        SearchPositionNo = null;
        StockInDateFrom = null;
        StockInDateTo = null;
        SearchStockInPerson = string.Empty;
        CurrentPage = 1;
        LoadParts();
    }

    // ========== 翻页 ==========

    private void FirstPage()
    {
        CurrentPage = 1;
        LoadParts();
    }

    private void PrevPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            LoadParts();
        }
    }

    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            LoadParts();
        }
    }

    private void LastPage()
    {
        CurrentPage = TotalPages;
        LoadParts();
    }
}
