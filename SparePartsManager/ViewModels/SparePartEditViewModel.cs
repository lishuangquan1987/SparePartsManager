using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using System.Collections.ObjectModel;
using System.Linq;
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

    private string _specification = string.Empty;
    public string Specification
    {
        get => _specification;
        set => SetProperty(ref _specification, value);
    }

    private string _model = string.Empty;
    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    private string _manufacturer = string.Empty;
    public string Manufacturer
    {
        get => _manufacturer;
        set => SetProperty(ref _manufacturer, value);
    }

    private string _projectName = string.Empty;
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
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

    public ObservableCollection<string> Specifications { get; } = new ObservableCollection<string>();
    public ObservableCollection<string> Projects { get; } = new ObservableCollection<string>();

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public SparePartEditViewModel(int partId)
    {
        _partId = partId;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);

        LoadSpecs();
        LoadProjects();
        LoadPart();
    }

    private void LoadSpecs()
    {
        try
        {
            var specs = SqlSugarHelper.Db.Queryable<Specification>().Select(s => s.Name).ToList();
            foreach (var s in specs)
                if (!string.IsNullOrEmpty(s) && !Specifications.Contains(s))
                    Specifications.Add(s);
        }
        catch { }
    }

    private void LoadProjects()
    {
        try
        {
            var projects = SqlSugarHelper.Db.Queryable<Project>().Select(p => p.Name).ToList();
            foreach (var p in projects)
                if (!string.IsNullOrEmpty(p) && !Projects.Contains(p))
                    Projects.Add(p);
        }
        catch { }
    }

    private void LoadPart()
    {
        var db = SqlSugarHelper.Db;
        var part = db.Queryable<SparePart>().InSingle(_partId);
        if (part == null) return;
        Name = part.Name;
        Specification = part.Specification;
        Model = part.Model;
        Manufacturer = part.Manufacturer;
        ProjectName = part.ProjectName ?? "";
        ShelfNo = part.ShelfNo;
        LayerNo = part.LayerNo;
        PositionNo = part.PositionNo;
        Remark = part.Remark;
    }

    private void Save()
    {
        var name = Name.Trim();
        var spec = Specification.Trim();
        var model = Model.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(spec) || string.IsNullOrEmpty(model))
        {
            MessageBox.Show("名称、规格、型号不能为空。");
            return;
        }

        try
        {
            var db = SqlSugarHelper.Db;
            db.Updateable<SparePart>()
                .SetColumns(it => new SparePart
                {
                    Name = name, Specification = spec, Model = model,
                    Manufacturer = Manufacturer.Trim(),
                    ProjectName = string.IsNullOrEmpty(ProjectName.Trim()) ? null : ProjectName.Trim(),
                    ShelfNo = ShelfNo, LayerNo = LayerNo, PositionNo = PositionNo,
                    Remark = Remark.Trim()
                }).Where(it => it.Id == _partId).ExecuteCommand();

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
