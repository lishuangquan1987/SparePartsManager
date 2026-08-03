using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Dtos;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class SparePartEditViewModel : ObservableObject
{
    private readonly int _partId;

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

    private string _remark = string.Empty;
    public string Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    // ========== 共享下拉数据源（只读选择，不可新增） ==========

    public ObservableCollection<DictItemDto> Specifications => DropdownDataService.Instance.Specifications;
    public ObservableCollection<DictItemDto> Models => DropdownDataService.Instance.Models;
    public ObservableCollection<DictItemDto> Manufacturers => DropdownDataService.Instance.Manufacturers;
    public ObservableCollection<DictItemDto> Projects => DropdownDataService.Instance.Projects;

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public SparePartEditViewModel(int partId)
    {
        _partId = partId;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);

        DropdownDataService.Instance.RefreshAll();
        LoadPart();
    }

    private void LoadPart()
    {
        var db = SqlSugarHelper.Db;
        var part = db.Queryable<SparePart>().InSingle(_partId);
        if (part == null) return;
        Name = part.Name;
        SpecificationId = part.SpecificationId;
        ModelId = part.ModelId;
        ManufacturerId = part.ManufacturerId;
        ProjectId = part.ProjectId;
        ShelfNo = part.ShelfNo;
        LayerNo = part.LayerNo;
        PositionNo = part.PositionNo;
        Remark = part.Remark ?? "";
    }

    private void Save()
    {
        var name = Name.Trim();

        // 迁移遗留数据可能为 SpecificationId=0（无有效规格），视为未选择
        if (string.IsNullOrEmpty(name)
            || !SpecificationId.HasValue || SpecificationId.Value <= 0
            || !ModelId.HasValue || ModelId.Value <= 0)
        {
            MessageBox.Show("名称、规格、型号不能为空。");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var part = db.Queryable<SparePart>().InSingle(_partId);
            if (part == null) return;

            // vo → dto → entities（写入链路分层）
            var dto = new SparePartDto
            {
                Id = _partId,
                Name = name,
                SpecificationId = SpecificationId,
                ModelId = ModelId,
                ManufacturerId = ManufacturerId,
                ProjectId = ProjectId,
                ShelfNo = ShelfNo,
                LayerNo = LayerNo,
                PositionNo = PositionNo,
                Remark = (Remark ?? "").Trim()
            };
            var updated = EntityMapper.ToSparePartEntity(dto);

            // 保留本窗口未编辑的字段（入库/出库信息、状态）
            updated.StockInDate = part.StockInDate;
            updated.StockOutDate = part.StockOutDate;
            updated.StockInPerson = part.StockInPerson;
            updated.StockOutPerson = part.StockOutPerson;
            updated.Status = part.Status;

            db.Updateable(updated).ExecuteCommand();

            RequestClose?.Invoke(this, true);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    public event EventHandler<bool>? RequestClose;
}
