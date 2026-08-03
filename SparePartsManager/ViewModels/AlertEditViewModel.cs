using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Dtos;
using SparePartsManager.Models;
using SparePartsManager.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class AlertEditViewModel : ObservableObject
{
    private readonly StockAlert? _editAlert;

    public string WindowTitle => _editAlert == null ? "新增警告规则" : "编辑警告规则";

    private int? _specificationId;
    public int? SpecificationId
    {
        get => _specificationId;
        set => SetProperty(ref _specificationId, value);
    }

    private bool _isSpecEnabled = true;
    public bool IsSpecEnabled
    {
        get => _isSpecEnabled;
        set => SetProperty(ref _isSpecEnabled, value);
    }

    private int? _modelId;
    public int? ModelId
    {
        get => _modelId;
        set => SetProperty(ref _modelId, value);
    }

    private bool _isModelEnabled = true;
    public bool IsModelEnabled
    {
        get => _isModelEnabled;
        set => SetProperty(ref _isModelEnabled, value);
    }

    private int _threshold = 5;
    public int Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value);
    }

    public ObservableCollection<DictItemDto> SpecOptions => DropdownDataService.Instance.Specifications;
    public ObservableCollection<DictItemDto> ModelOptions => DropdownDataService.Instance.Models;

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public AlertEditViewModel(StockAlert? alert = null)
    {
        _editAlert = alert;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);

        DropdownDataService.Instance.RefreshAll();

        if (alert != null)
        {
            SpecificationId = alert.SpecificationId;
            IsSpecEnabled = false;
            ModelId = alert.ModelId;
            IsModelEnabled = false;
            Threshold = alert.Threshold;
        }
    }

    private void Save()
    {
        if (SpecificationId == null)
        {
            MessageBox.Show("请选择规格。", "提示");
            return;
        }
        if (ModelId == null)
        {
            MessageBox.Show("请选择型号。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            var specId = SpecificationId.Value;
            var modelId = ModelId.Value;

            // 拆开闭包引用，避免 SqlSugar 表达式树解析异常
            bool exists;
            if (_editAlert == null)
            {
                exists = db.Queryable<StockAlert>()
                    .Any(a => a.SpecificationId == specId && a.ModelId == modelId);
            }
            else
            {
                exists = db.Queryable<StockAlert>()
                    .Any(a => a.SpecificationId == specId && a.ModelId == modelId && a.Id != _editAlert.Id);
            }

            if (exists)
            {
                MessageBox.Show("该「规格 + 型号」的警告规则已存在，请编辑已有规则。", "提示");
                return;
            }

            if (_editAlert == null)
            {
                db.Insertable(new StockAlert
                {
                    SpecificationId = specId,
                    ModelId = modelId,
                    Threshold = Threshold
                }).ExecuteCommand();
            }
            else
            {
                db.Updateable<StockAlert>()
                    .SetColumns(it => new StockAlert { Threshold = Threshold })
                    .Where(it => it.Id == _editAlert.Id)
                    .ExecuteCommand();
            }

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
