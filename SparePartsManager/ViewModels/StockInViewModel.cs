using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SparePartsManager.Services;
using SparePartsManager.Views;
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

    // ========== 共享下拉数据源 ==========

    public ObservableCollection<string> Specifications => DropdownDataService.Instance.Specifications;
    public ObservableCollection<string> Models => DropdownDataService.Instance.Models;
    public ObservableCollection<string> Manufacturers => DropdownDataService.Instance.Manufacturers;
    public ObservableCollection<string> Projects => DropdownDataService.Instance.Projects;

    private static Window? GetOwnerWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
    }

    public RelayCommand AddSpecCommand { get; }
    public RelayCommand AddModelCommand { get; }
    public RelayCommand AddManufacturerCommand { get; }
    public RelayCommand AddProjectCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand ClearCommand { get; }

    public StockInViewModel()
    {
        AddSpecCommand = new RelayCommand(AddSpec);
        AddModelCommand = new RelayCommand(AddModel);
        AddManufacturerCommand = new RelayCommand(AddManufacturer);
        AddProjectCommand = new RelayCommand(AddProject);
        SaveCommand = new RelayCommand(Save);
        ClearCommand = new RelayCommand(ClearForm);

        DropdownDataService.Instance.RefreshAll();
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
            DropdownDataService.Instance.RefreshAll();
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
            var db = SqlSugarHelper.Db;
            // 直接刷新列表，型号不单独建字典表，只需刷新下拉选项
            DropdownDataService.Instance.RefreshAll();
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
            var db = SqlSugarHelper.Db;
            // 直接刷新列表，厂家不单独建字典表
            DropdownDataService.Instance.RefreshAll();
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
            DropdownDataService.Instance.RefreshAll();
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

        if (string.IsNullOrEmpty(name)) { MessageBox.Show("请输入备件名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (string.IsNullOrEmpty(spec)) { MessageBox.Show("请输入/选择规格。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (string.IsNullOrEmpty(model)) { MessageBox.Show("请输入型号。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var locDesc = $"{ShelfNo}-{LayerNo}-{PositionNo}";
        var project = ProjectName.Trim();
        var confirm = MessageBox.Show(
            $"确认入库？\n\n名称：{name}\n规格：{spec}\n型号：{model}\n" +
            $"厂家：{Manufacturer.Trim()}\n项目：{(string.IsNullOrEmpty(project) ? "（无）" : project)}\n货位：{locDesc}\n" +
            $"数量：{Quantity}\n入库日期：{StockInDate:yyyy-MM-dd}\n入库人：{StockInPerson}",
            "确认入库", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var db = SqlSugarHelper.Db;

            // 如果规格不存在则自动新增
            if (!string.IsNullOrEmpty(spec) && !db.Queryable<Specification>().Any(s => s.Name == spec))
            {
                db.Insertable(new Specification { Name = spec }).ExecuteCommand();
            }

            var baseData = new SparePart
            {
                Specification = spec,
                Model = model,
                Manufacturer = Manufacturer.Trim(),
                ProjectName = string.IsNullOrEmpty(project) ? null : project,
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
                        Specification = baseData.Specification,
                        Model = baseData.Model,
                        Manufacturer = baseData.Manufacturer,
                        ProjectName = baseData.ProjectName,
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
        Specification = string.Empty;
        Model = string.Empty;
        Manufacturer = string.Empty;
        ProjectName = string.Empty;
        ShelfNo = 0;
        LayerNo = 0;
        PositionNo = 0;
        StockInDate = DateTime.Now;
        Quantity = 1;
        Remark = string.Empty;
    }
}
