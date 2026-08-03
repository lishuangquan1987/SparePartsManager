using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using SqlSugar;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class QueryItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Specification { get; set; } = "";
    public string Model { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public int ShelfNo { get; set; }
    public int LayerNo { get; set; }
    public int PositionNo { get; set; }
    public DateTime StockInDate { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string StockInPerson { get; set; } = "";
    public string? StockOutPerson { get; set; }
    public string Status { get; set; } = "";
    public string Remark { get; set; } = "";
    public string? ProjectName { get; set; }
    public bool IsLowStock { get; set; }
    public string StatusDisplay => Status == "InStock" ? "在库" : "已出库";
}

public class QueryViewModel : ObservableObject
{
    // ========== 下拉框选项（只读选择） ==========

    public ObservableCollection<DictItem> SpecOptions => DropdownDataService.Instance.Specifications;
    public ObservableCollection<DictItem> ModelOptions => DropdownDataService.Instance.Models;
    public ObservableCollection<DictItem> ManufacturerOptions => DropdownDataService.Instance.Manufacturers;
    public ObservableCollection<DictItem> ProjectOptions => DropdownDataService.Instance.Projects;

    // ========== 搜索字段 ==========

    private string _searchName = string.Empty;
    public string SearchName
    {
        get => _searchName;
        set => SetProperty(ref _searchName, value);
    }

    private int? _searchSpecificationId;
    public int? SearchSpecificationId
    {
        get => _searchSpecificationId;
        set => SetProperty(ref _searchSpecificationId, value);
    }

    private int? _searchModelId;
    public int? SearchModelId
    {
        get => _searchModelId;
        set => SetProperty(ref _searchModelId, value);
    }

    private int? _searchManufacturerId;
    public int? SearchManufacturerId
    {
        get => _searchManufacturerId;
        set => SetProperty(ref _searchManufacturerId, value);
    }

    private int? _searchProjectId;
    public int? SearchProjectId
    {
        get => _searchProjectId;
        set => SetProperty(ref _searchProjectId, value);
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

    private DateTime? _stockOutDateFrom;
    public DateTime? StockOutDateFrom
    {
        get => _stockOutDateFrom;
        set => SetProperty(ref _stockOutDateFrom, value);
    }

    private DateTime? _stockOutDateTo;
    public DateTime? StockOutDateTo
    {
        get => _stockOutDateTo;
        set => SetProperty(ref _stockOutDateTo, value);
    }

    private string _searchStockInPerson = string.Empty;
    public string SearchStockInPerson
    {
        get => _searchStockInPerson;
        set => SetProperty(ref _searchStockInPerson, value);
    }

    private string _searchStockOutPerson = string.Empty;
    public string SearchStockOutPerson
    {
        get => _searchStockOutPerson;
        set => SetProperty(ref _searchStockOutPerson, value);
    }

    private string _searchStatus = "全部";
    public string SearchStatus
    {
        get => _searchStatus;
        set => SetProperty(ref _searchStatus, value);
    }

    private string _searchRemark = string.Empty;
    public string SearchRemark
    {
        get => _searchRemark;
        set => SetProperty(ref _searchRemark, value);
    }

    public ObservableCollection<string> StatusFilterOptions { get; } = new ObservableCollection<string> { "全部", "在库", "已出库" };

    // ========== 忙碌状态 ==========

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    // ========== wd:Pagination 分页 ==========

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
                LoadParts();
        }
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
                LoadParts();
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set
        {
            SetProperty(ref _totalCount, value);
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(TotalPages));
        }
    }

    public string PageInfo => TotalCount > 0 ? $"第{CurrentPage}页/共{TotalPages}页 共{TotalCount}条" : "无数据";

    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    // ========== 数据集合 ==========

    public ObservableCollection<QueryItemViewModel> Parts { get; } = new ObservableCollection<QueryItemViewModel>();

    private QueryItemViewModel? _selectedPart;
    public QueryItemViewModel? SelectedPart
    {
        get => _selectedPart;
        set => SetProperty(ref _selectedPart, value);
    }

    // ========== 命令 ==========

    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand FirstPageCommand { get; }
    public RelayCommand PrevPageCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public RelayCommand LastPageCommand { get; }

    public QueryViewModel()
    {
        SearchCommand = new RelayCommand(Search);
        ResetCommand = new RelayCommand(Reset);
        EditCommand = new RelayCommand(EditPart);
        ExportCommand = new RelayCommand(Export);
        FirstPageCommand = new RelayCommand(FirstPage, () => CurrentPage > 1);
        PrevPageCommand = new RelayCommand(PrevPage, () => CurrentPage > 1);
        NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(LastPage, () => CurrentPage < TotalPages);

        DropdownDataService.Instance.RefreshAll();
        LoadParts();
    }

    private void FirstPage() { CurrentPage = 1; LoadParts(); }
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; LoadParts(); } }
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; LoadParts(); } }
    private void LastPage() { CurrentPage = TotalPages; LoadParts(); }

    private void Search()
    {
        CurrentPage = 1;
        LoadParts();
    }

    public void LoadParts()
    {
        IsBusy = true;
        try
        {
            var db = SqlSugarHelper.Db;

            var name = SearchName.Trim();
            var stockInPerson = SearchStockInPerson.Trim();
            var stockOutPerson = SearchStockOutPerson.Trim();
            var remark = SearchRemark.Trim();

            var statusFilter = SearchStatus switch
            {
                "在库" => "InStock",
                "已出库" => "OutStock",
                _ => null
            };

            var alertDict = db.Queryable<StockAlert>()
                .ToList()
                .ToDictionary(a => $"{a.SpecificationId}|{a.ModelId}", a => a.Threshold);

            // 查询总数
            var totalQuery = db.Queryable<SparePart>()
                .WhereIF(!string.IsNullOrEmpty(name), p => p.Name.Contains(name))
                .WhereIF(SearchSpecificationId.HasValue, p => p.SpecificationId == SearchSpecificationId.Value)
                .WhereIF(SearchModelId.HasValue, p => p.ModelId == SearchModelId.Value)
                .WhereIF(SearchManufacturerId.HasValue, p => p.ManufacturerId == SearchManufacturerId.Value)
                .WhereIF(SearchProjectId.HasValue, p => p.ProjectId == SearchProjectId.Value)
                .WhereIF(SearchShelfNo.HasValue, p => p.ShelfNo == SearchShelfNo.Value)
                .WhereIF(SearchLayerNo.HasValue, p => p.LayerNo == SearchLayerNo.Value)
                .WhereIF(SearchPositionNo.HasValue, p => p.PositionNo == SearchPositionNo.Value)
                .WhereIF(StockInDateFrom.HasValue, p => p.StockInDate >= StockInDateFrom.Value)
                .WhereIF(StockInDateTo.HasValue, p => p.StockInDate <= StockInDateTo.Value)
                .WhereIF(StockOutDateFrom.HasValue, p => p.StockOutDate >= StockOutDateFrom.Value)
                .WhereIF(StockOutDateTo.HasValue, p => p.StockOutDate <= StockOutDateTo.Value)
                .WhereIF(!string.IsNullOrEmpty(stockInPerson), p => p.StockInPerson.Contains(stockInPerson))
                .WhereIF(!string.IsNullOrEmpty(stockOutPerson), p => p.StockOutPerson != null && p.StockOutPerson.Contains(stockOutPerson))
                .WhereIF(!string.IsNullOrEmpty(statusFilter), p => p.Status == statusFilter)
                .WhereIF(!string.IsNullOrEmpty(remark), p => p.Remark.Contains(remark));

            TotalCount = totalQuery.Count();

            // 查询分页数据
            var parts = totalQuery
                .OrderBy(p => p.StockInDate, OrderByType.Desc)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var stockCountDict = db.Queryable<SparePart>()
                .Where(p => p.Status == "InStock")
                .GroupBy(p => new { p.SpecificationId, p.ModelId })
                .Select(g => new { g.SpecificationId, g.ModelId, Count = SqlFunc.AggregateCount(g.Id) })
                .ToList()
                .ToDictionary(x => $"{x.SpecificationId}|{x.ModelId}", x => x.Count);

            var specDict = db.Queryable<Specification>().ToList().ToDictionary(s => s.Id, s => s.Name);
            var modelDict = db.Queryable<PartModel>().ToList().ToDictionary(m => m.Id, m => m.Name);
            var manDict = db.Queryable<Manufacturer>().ToList().ToDictionary(m => m.Id, m => m.Name);
            var projDict = db.Queryable<Project>().ToList().ToDictionary(p => p.Id, p => p.Name);

            Parts.Clear();
            foreach (var p in parts)
            {
                var key = $"{p.SpecificationId}|{p.ModelId}";
                var hasAlert = alertDict.TryGetValue(key, out var threshold);
                var stockCount = stockCountDict.TryGetValue(key, out var cnt) ? cnt : 0;

                Parts.Add(new QueryItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Specification = p.SpecificationId.HasValue && specDict.TryGetValue(p.SpecificationId.Value, out var sn) ? sn : "",
                    Model = p.ModelId.HasValue && modelDict.TryGetValue(p.ModelId.Value, out var mn) ? mn : "",
                    Manufacturer = p.ManufacturerId.HasValue && manDict.TryGetValue(p.ManufacturerId.Value, out var man) ? man : "",
                    ProjectName = p.ProjectId.HasValue && projDict.TryGetValue(p.ProjectId.Value, out var proj) ? proj : null,
                    ShelfNo = p.ShelfNo,
                    LayerNo = p.LayerNo,
                    PositionNo = p.PositionNo,
                    StockInDate = p.StockInDate,
                    StockOutDate = p.StockOutDate,
                    StockInPerson = p.StockInPerson,
                    StockOutPerson = p.StockOutPerson,
                    Status = p.Status,
                    Remark = p.Remark,
                    IsLowStock = hasAlert && p.Status == "InStock" && stockCount < threshold
                });
            }

            OnPropertyChanged(nameof(PageInfo));
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"查询失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Reset()
    {
        SearchName = string.Empty;
        SearchSpecificationId = null;
        SearchModelId = null;
        SearchManufacturerId = null;
        SearchProjectId = null;
        SearchShelfNo = null;
        SearchLayerNo = null;
        SearchPositionNo = null;
        StockInDateFrom = null;
        StockInDateTo = null;
        StockOutDateFrom = null;
        StockOutDateTo = null;
        SearchStockInPerson = string.Empty;
        SearchStockOutPerson = string.Empty;
        SearchStatus = "全部";
        SearchRemark = string.Empty;
        CurrentPage = 1;
        LoadParts();
    }

    private void EditPart()
    {
        if (SelectedPart == null)
        {
            MessageBox.Show("请先选择要编辑的备件。", "提示");
            return;
        }

        var window = new Views.SparePartEditWindow(SelectedPart.Id);
        if (window.ShowDialog() == true)
            LoadParts();
    }

    private void Export()
    {
        if (Parts.Count == 0) { MessageBox.Show("无数据可导出。"); return; }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV 文件|*.csv",
            FileName = $"备件查询导出_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.Write('\uFEFF');
            sw.WriteLine("Id,名称,规格,型号,厂家,项目,货架,层,位,入库日期,出库日期,入库人,出库人,状态,备注");
            foreach (var p in Parts)
            {
                sw.WriteLine($"{p.Id},{EscapeCsv(p.Name)},{EscapeCsv(p.Specification)},{EscapeCsv(p.Model)},{EscapeCsv(p.Manufacturer)},{EscapeCsv(p.ProjectName ?? "")},{p.ShelfNo},{p.LayerNo},{p.PositionNo},{p.StockInDate:yyyy-MM-dd},{p.StockOutDate?.ToString("yyyy-MM-dd") ?? ""},{EscapeCsv(p.StockInPerson)},{EscapeCsv(p.StockOutPerson ?? "")},{p.StatusDisplay},{EscapeCsv(p.Remark)}");
            }
            MessageBox.Show($"导出成功！\n{dlg.FileName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
