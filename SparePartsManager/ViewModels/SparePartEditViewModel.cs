using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Views;
using SparePartsManager.Services;
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

    public ObservableCollection<string> Specifications => DropdownDataService.Instance.Specifications;
    public ObservableCollection<string> Models => DropdownDataService.Instance.Models;
    public ObservableCollection<string> Manufacturers => DropdownDataService.Instance.Manufacturers;
    public ObservableCollection<string> Projects => DropdownDataService.Instance.Projects;

    public RelayCommand AddSpecCommand { get; }
    public RelayCommand AddModelCommand { get; }
    public RelayCommand AddManufacturerCommand { get; }
    public RelayCommand AddProjectCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public SparePartEditViewModel(int partId)
    {
        _partId = partId;
        AddSpecCommand = new RelayCommand(AddSpec);
        AddModelCommand = new RelayCommand(AddModel);
        AddManufacturerCommand = new RelayCommand(AddManufacturer);
        AddProjectCommand = new RelayCommand(AddProject);
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
        Specification = part.Specification;
        Model = part.Model;
        Manufacturer = part.Manufacturer;
        ProjectName = part.ProjectName ?? "";
        ShelfNo = part.ShelfNo;
        LayerNo = part.LayerNo;
        PositionNo = part.PositionNo;
        Remark = part.Remark;
    }

    private static Window? GetOwnerWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
    }

    private void AddSpec()
    {
        var dialog = new InputDialog("新增规格", "请输入新规格名称：");
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var db = SqlSugarHelper.Db;
            if (db.Queryable<Specification>().Any(s => s.Name == name))
            {
                MessageBox.Show("该规格已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            db.Insertable(new Specification { Name = name }).ExecuteCommand();
            Specifications.Add(name);
            Specification = name;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddModel()
    {
        var dialog = new InputDialog("新增型号", "请输入新型号：");
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            if (!Models.Contains(name))
                Models.Add(name);
            Model = name;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddManufacturer()
    {
        var dialog = new InputDialog("新增厂家", "请输入新厂家名称：");
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            if (!Manufacturers.Contains(name))
                Manufacturers.Add(name);
            Manufacturer = name;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddProject()
    {
        var dialog = new InputDialog("新增项目", "请输入新项目名称：");
        dialog.Owner = GetOwnerWindow();
        if (dialog.ShowDialog() != true) return;

        var name = dialog.InputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var db = SqlSugarHelper.Db;
            if (db.Queryable<Project>().Any(p => p.Name == name))
            {
                MessageBox.Show("该项目已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            db.Insertable(new Project { Name = name }).ExecuteCommand();
            Projects.Add(name);
            ProjectName = name;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
