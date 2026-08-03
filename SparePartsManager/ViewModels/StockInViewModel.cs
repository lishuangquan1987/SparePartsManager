using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class StockInViewModel : ObservableObject
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private int? _specificationId;
    public int? SpecificationId
    {
        get => _specificationId;
        set => SetProperty(ref _specificationId, value);
    }

    private int? _modelId;
    public int? ModelId
    {
        get => _modelId;
        set => SetProperty(ref _modelId, value);
    }

    private int? _manufacturerId;
    public int? ManufacturerId
    {
        get => _manufacturerId;
        set => SetProperty(ref _manufacturerId, value);
    }

    private int? _projectId;
    public int? ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
    }

    private int _shelfNo;
    public int ShelfNo
    {
        get => _shelfNo;
        set => SetProperty(ref _shelfNo, value);
    }

    private int _layerNo;
    public int LayerNo
    {
        get => _layerNo;
        set => SetProperty(ref _layerNo, value);
    }

    private int _positionNo;
    public int PositionNo
    {
        get => _positionNo;
        set => SetProperty(ref _positionNo, value);
    }

    private DateTime _stockInDate = DateTime.Now;
    public DateTime StockInDate
    {
        get => _stockInDate;
        set => SetProperty(ref _stockInDate, value);
    }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    private string _remark = string.Empty;
    public string Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    public string StockInPerson => CurrentUser.LoginUser?.RealName ?? "";

    // ========== 共享下拉数据源（只读选择，不可新增） ==========

    public ObservableCollection<DictItem> Specifications => DropdownDataService.Instance.Specifications;
    public ObservableCollection<DictItem> Models => DropdownDataService.Instance.Models;
    public ObservableCollection<DictItem> Manufacturers => DropdownDataService.Instance.Manufacturers;
    public ObservableCollection<DictItem> Projects => DropdownDataService.Instance.Projects;

    public RelayCommand SaveCommand { get; }
    public RelayCommand ClearCommand { get; }

    public StockInViewModel()
    {
        SaveCommand = new RelayCommand(Save);
        ClearCommand = new RelayCommand(ClearForm);

        DropdownDataService.Instance.RefreshAll();
    }

    private static string GetName(ObservableCollection<DictItem> items, int? id)
    {
        if (!id.HasValue) return "";
        var item = items.FirstOrDefault(i => i.Id == id.Value);
        return item?.Name ?? "";
    }

    private void Save()
    {
        var name = Name.Trim();

        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入备件名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (SpecificationId == null) { MessageBox.Show("请选择规格。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (ModelId == null) { MessageBox.Show("请选择型号。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var specName = GetName(Specifications, SpecificationId);
        var modelName = GetName(Models, ModelId);
        var manName = GetName(Manufacturers, ManufacturerId);
        var projName = GetName(Projects, ProjectId);
        var locDesc = $"{ShelfNo}-{LayerNo}-{PositionNo}";

        var confirm = MessageBox.Show(
            $"确认入库？\n\n名称：{name}\n规格：{specName}\n型号：{modelName}\n" +
            $"厂家：{(string.IsNullOrEmpty(manName) ? "（无）" : manName)}\n项目：{(string.IsNullOrEmpty(projName) ? "（无）" : projName)}\n货位：{locDesc}\n" +
            $"数量：{Quantity}\n入库日期：{StockInDate:yyyy-MM-dd}\n入库人：{StockInPerson}",
            "确认入库", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var db = SqlSugarHelper.Db;

            var baseData = new SparePart
            {
                SpecificationId = SpecificationId.Value,
                ModelId = ModelId.Value,
                ManufacturerId = ManufacturerId,
                ProjectId = ProjectId,
                ShelfNo = ShelfNo,
                LayerNo = LayerNo,
                PositionNo = PositionNo,
                StockInDate = StockInDate.Date,
                StockInPerson = StockInPerson,
                Remark = Remark.Trim(),
                Status = "InStock"
            };

            if (Quantity == 1)
            {
                baseData.Name = name;
                db.Insertable(baseData).ExecuteCommand();
            }
            else
            {
                var list = new System.Collections.Generic.List<SparePart>(Quantity);
                for (int i = 1; i <= Quantity; i++)
                {
                    list.Add(new SparePart
                    {
                        Name = $"{name}-{i}",
                        SpecificationId = baseData.SpecificationId,
                        ModelId = baseData.ModelId,
                        ManufacturerId = baseData.ManufacturerId,
                        ProjectId = baseData.ProjectId,
                        ShelfNo = baseData.ShelfNo,
                        LayerNo = baseData.LayerNo,
                        PositionNo = baseData.PositionNo,
                        StockInDate = baseData.StockInDate,
                        StockInPerson = baseData.StockInPerson,
                        Remark = baseData.Remark,
                        Status = baseData.Status
                    });
                }
                db.Insertable(list).ExecuteCommand();
            }

            MessageBox.Show($"入库成功！共 {Quantity} 件。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearForm();
            DropdownDataService.Instance.RefreshAll();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"入库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        SpecificationId = null;
        ModelId = null;
        ManufacturerId = null;
        ProjectId = null;
        ShelfNo = 0;
        LayerNo = 0;
        PositionNo = 0;
        StockInDate = DateTime.Now;
        Quantity = 1;
        Remark = string.Empty;
    }
}
