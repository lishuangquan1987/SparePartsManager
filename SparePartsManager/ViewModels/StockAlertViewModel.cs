using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Dtos;
using SparePartsManager.Models;
using SqlSugar;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class AlertItemViewModel
{
    public int Id { get; set; }
    public int? SpecificationId { get; set; }
    public int? ModelId { get; set; }
    public string Specification { get; set; } = "";
    public string Model { get; set; } = "";
    public int Threshold { get; set; }
    public int CurrentStock { get; set; }
    public bool IsWarning => CurrentStock < Threshold;

    /// <summary>DTO → VO（库存警告列表展示对象）。</summary>
    public static AlertItemViewModel FromDto(StockAlertDto dto) => new()
    {
        Id = dto.Id,
        SpecificationId = dto.SpecificationId,
        ModelId = dto.ModelId,
        Specification = dto.SpecificationName,
        Model = dto.ModelName,
        Threshold = dto.Threshold,
        CurrentStock = dto.CurrentStock
    };
}

public class StockAlertViewModel : ObservableObject
{
    /// <summary>
    /// 规则列表发生变化（新增/编辑/删除）后触发，供 MainViewModel 重新检查报警。
    /// </summary>
    public event Action? AlertsChanged;

    public ObservableCollection<AlertItemViewModel> Alerts { get; } = new ObservableCollection<AlertItemViewModel>();

    private AlertItemViewModel? _selectedAlert;
    public AlertItemViewModel? SelectedAlert
    {
        get => _selectedAlert;
        set => SetProperty(ref _selectedAlert, value);
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public StockAlertViewModel()
    {
        AddCommand = new RelayCommand(Add);
        EditCommand = new RelayCommand(Edit);
        DeleteCommand = new RelayCommand(Delete);
        LoadAlerts();
    }

    public void LoadAlerts()
    {
        try
        {
            var db = SqlSugarHelper.Db;

            var specDict = db.Queryable<Specification>().ToList().ToDictionary(s => s.Id, s => s.Name);
            var modelDict = db.Queryable<PartModel>().ToList().ToDictionary(m => m.Id, m => m.Name);

            // entities → dto → vo（三层：StockAlert Entity → DTO(含字典名称) → 展示 VO）
            // CurrentStock 通过 SqlFunc 子查询在一次 SQL 中计算，避免 N+1 查询
            var dtos = db.Queryable<StockAlert>()
                .OrderBy(a => a.SpecificationId)
                .Select(a => new StockAlertDto
                {
                    Id = a.Id,
                    SpecificationId = a.SpecificationId,
                    ModelId = a.ModelId,
                    Threshold = a.Threshold,
                    CurrentStock = SqlFunc.Subqueryable<SparePart>()
                        .Where(p => p.SpecificationId == a.SpecificationId
                            && p.ModelId == a.ModelId
                            && p.Status == "InStock")
                        .Count()
                })
                .ToList();

            Alerts.Clear();
            foreach (var dto in dtos)
            {
                dto.SpecificationName = dto.SpecificationId.HasValue && specDict.TryGetValue(dto.SpecificationId.Value, out var sn) ? sn : "";
                dto.ModelName = dto.ModelId.HasValue && modelDict.TryGetValue(dto.ModelId.Value, out var mn) ? mn : "";
                Alerts.Add(AlertItemViewModel.FromDto(dto));
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"加载警告规则失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Add()
    {
        var window = new Views.AlertEditWindow();
        if (window.ShowDialog() == true)
        {
            LoadAlerts();
            AlertsChanged?.Invoke();
        }
    }

    private void Edit()
    {
        if (SelectedAlert == null)
        {
            MessageBox.Show("请先选择一条规则。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var alert = db.Queryable<StockAlert>().InSingle(SelectedAlert.Id);
            if (alert == null) return;

            var window = new Views.AlertEditWindow(alert);
            if (window.ShowDialog() == true)
            {
                LoadAlerts();
                AlertsChanged?.Invoke();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"编辑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Delete()
    {
        if (SelectedAlert == null)
        {
            MessageBox.Show("请先选择一条规则。", "提示");
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除「{SelectedAlert.Specification} - {SelectedAlert.Model}」的警告规则吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                SqlSugarHelper.Db.Deleteable<StockAlert>().In(SelectedAlert.Id).ExecuteCommand();
                LoadAlerts();
                AlertsChanged?.Invoke();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
