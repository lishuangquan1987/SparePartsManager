using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SparePartsManager.ViewModels;

public class AlertEditViewModel : ObservableObject
{
    private readonly StockAlert? _editAlert;

    public string WindowTitle => _editAlert == null ? "新增警告规则" : "编辑警告规则";

    private string _specification = string.Empty;
    public string Specification
    {
        get => _specification;
        set => SetProperty(ref _specification, value);
    }

    private bool _isSpecEnabled = true;
    public bool IsSpecEnabled
    {
        get => _isSpecEnabled;
        set => SetProperty(ref _isSpecEnabled, value);
    }

    private string _model = string.Empty;
    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    private bool _isModelReadOnly;
    public bool IsModelReadOnly
    {
        get => _isModelReadOnly;
        set => SetProperty(ref _isModelReadOnly, value);
    }

    private int _threshold = 5;
    public int Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value);
    }

    public ObservableCollection<string> SpecOptions { get; } = new ObservableCollection<string>();

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public AlertEditViewModel(StockAlert? alert = null)
    {
        _editAlert = alert;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);

        LoadSpecs();

        if (alert != null)
        {
            Specification = alert.Specification;
            IsSpecEnabled = false;
            Model = alert.Model;
            IsModelReadOnly = true;
            Threshold = alert.Threshold;
        }
    }

    private void LoadSpecs()
    {
        try
        {
            var specs = SqlSugarHelper.Db.Queryable<SparePartsManager.Models.Specification>()
                .Select(s => s.Name).ToList();
            SpecOptions.Clear();
            foreach (var s in specs)
                if (!string.IsNullOrEmpty(s)) SpecOptions.Add(s);
        }
        catch { }
    }

    private void Save()
    {
        var spec = Specification.Trim();
        var model = Model.Trim();
        var threshold = Threshold;

        if (string.IsNullOrEmpty(spec))
        {
            MessageBox.Show("请输入/选择规格。", "提示");
            return;
        }
        if (string.IsNullOrEmpty(model))
        {
            MessageBox.Show("请输入型号。", "提示");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;

            // 拆开闭包引用，避免 SqlSugar 表达式树解析异常
            bool exists;
            if (_editAlert == null)
            {
                exists = db.Queryable<StockAlert>()
                    .Any(a => a.Specification == spec && a.Model == model);
            }
            else
            {
                exists = db.Queryable<StockAlert>()
                    .Any(a => a.Specification == spec && a.Model == model && a.Id != _editAlert.Id);
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
                    Specification = spec,
                    Model = model,
                    Threshold = threshold
                }).ExecuteCommand();
            }
            else
            {
                db.Updateable<StockAlert>()
                    .SetColumns(it => new StockAlert { Threshold = threshold })
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
