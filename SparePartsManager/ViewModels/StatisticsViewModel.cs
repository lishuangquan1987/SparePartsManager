using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SparePartsManager.Data;
using SparePartsManager.Models;
using SqlSugar;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SparePartsManager.ViewModels;

/// <summary>
/// 将 true→Collapsed, false→Visible（与 BooleanToVisibilityConverter 反向）
/// </summary>
public class InvertedBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Visibility v) return v != Visibility.Visible;
        return false;
    }
}

public class StatisticsViewModel : ObservableObject
{
    /// <summary>SkiaSharp 渲染中文字体（Windows 微软雅黑）</summary>
    private static readonly SKTypeface ChineseTypeface = SKTypeface.FromFamilyName("Microsoft YaHei");

    /// <summary>Tooltip 文字画笔（解决默认 SkiaSharp 字体不渲染中文导致的乱码）</summary>
    public SolidColorPaint TooltipTextPaint { get; } = new SolidColorPaint(SKColors.White)
    {
        SKTypeface = SKTypeface.FromFamilyName("Microsoft YaHei")
    };

    /// <summary>Tooltip 背景画笔</summary>
    public SolidColorPaint TooltipBackgroundPaint { get; } = new SolidColorPaint(new SKColor(40, 40, 40));

    private DateTime _startDate = DateTime.Now.AddMonths(-6);
    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    private DateTime _endDate = DateTime.Now;
    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    private string _dimension = "库存总览";
    public string Dimension
    {
        get => _dimension;
        set => SetProperty(ref _dimension, value);
    }

    private string _statusText = "就绪";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<string> DimensionOptions { get; } = new ObservableCollection<string>
    {
        "库存总览", "出入库趋势", "规格占比", "项目统计", "人员统计"
    };

    private ObservableCollection<ISeries> _series = new();
    public ObservableCollection<ISeries> Series
    {
        get => _series;
        set => SetProperty(ref _series, value);
    }

    private ObservableCollection<Axis> _xAxes = new();
    public ObservableCollection<Axis> XAxes
    {
        get => _xAxes;
        set => SetProperty(ref _xAxes, value);
    }

    private ObservableCollection<Axis> _yAxes = new();
    public ObservableCollection<Axis> YAxes
    {
        get => _yAxes;
        set => SetProperty(ref _yAxes, value);
    }

    private string _chartType = "Cartesian";
    /// <summary>"Cartesian" 或 "Pie"</summary>
    public string ChartType
    {
        get => _chartType;
        set => SetProperty(ref _chartType, value);
    }

    private bool _isPieChart;
    /// <summary>是否为饼图模式（用于 XAML 可见性切换）</summary>
    public bool IsPieChart
    {
        get => _isPieChart;
        set => SetProperty(ref _isPieChart, value);
    }

    public RelayCommand QueryCommand { get; }

    public StatisticsViewModel()
    {
        QueryCommand = new RelayCommand(Query);
    }

    private void Query()
    {
        try
        {
            switch (Dimension)
            {
                case "库存总览": QueryOverview(); break;
                case "出入库趋势": QueryTrend(); break;
                case "规格占比": QuerySpecPie(); break;
                case "项目统计": QueryProjectBar(); break;
                case "人员统计": QueryPersonBar(); break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ========== 库存总览（柱状图） ==========

    private void QueryOverview()
    {
        var db = SqlSugarHelper.Db;
        var s = StartDate.Date;
        var e = EndDate.Date.AddDays(1);

        var inCount = db.Queryable<SparePart>().Count(p => p.StockInDate >= s && p.StockInDate < e);
        var outCount = db.Queryable<SparePart>().Count(p => p.StockOutDate >= s && p.StockOutDate < e);
        var totalInStock = db.Queryable<SparePart>().Count(p => p.Status == "InStock");

        // 低库存数量：按 "规格ID|型号ID" 分组与 StockAlert 阈值比较
        var stockGroup = db.Queryable<SparePart>()
            .Where(p => p.Status == "InStock")
            .GroupBy(p => new { p.SpecificationId, p.ModelId })
            .Select(g => new { g.SpecificationId, g.ModelId, Count = SqlFunc.AggregateCount(g.Id) })
            .ToList();

        var alerts = db.Queryable<StockAlert>().ToList();
        var alertLookup = alerts.ToLookup(a => $"{a.SpecificationId}|{a.ModelId}");
        var lowStockCount = stockGroup.Count(g =>
        {
            var key = $"{g.SpecificationId}|{g.ModelId}";
            var threshold = alertLookup[key]?.FirstOrDefault()?.Threshold ?? int.MaxValue;
            return g.Count < threshold;
        });

        StatusText = $"总入库: {inCount}  总出库: {outCount}  当前库存: {totalInStock}  低库存: {lowStockCount}";

        ChartType = "Cartesian";
        IsPieChart = false;

        Series = new ObservableCollection<ISeries>
        {
            new ColumnSeries<double>
            {
                Values = new double[] { inCount, outCount, totalInStock, lowStockCount },
                Fill = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 0 },
                Stroke = null,
                Name = "数量"
            }
        };

        XAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = new[] { "总入库", "总出库", "当前库存", "低库存" },
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(SKColors.Transparent),
                
            }
        };

        YAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };
    }

    // ========== 出入库趋势（折线图） ==========

    private void QueryTrend()
    {
        var db = SqlSugarHelper.Db;
        var s = StartDate.Date;
        var e = EndDate.Date.AddDays(1);

        var months = GetMonthLabels(s, e);
        var inCounts = new double[months.Count];
        var outCounts = new double[months.Count];

        for (int i = 0; i < months.Count; i++)
        {
            var ms = DateTime.Parse(months[i] + "-01");
            var me = ms.AddMonths(1);
            inCounts[i] = db.Queryable<SparePart>().Count(p => p.StockInDate >= ms && p.StockInDate < me);
            outCounts[i] = db.Queryable<SparePart>().Count(p => p.StockOutDate >= ms && p.StockOutDate < me);
        }

        var totalIn = inCounts.Sum();
        var totalOut = outCounts.Sum();
        StatusText = $"共 {months.Count} 个月数据  总入库: {totalIn}  总出库: {totalOut}";

        ChartType = "Cartesian";
        IsPieChart = false;

        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Values = inCounts,
                Name = "入库",
                Stroke = new SolidColorPaint(new SKColor(0, 122, 204)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(new SKColor(0, 122, 204)) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White)
            },
            new LineSeries<double>
            {
                Values = outCounts,
                Name = "出库",
                Stroke = new SolidColorPaint(new SKColor(231, 76, 60)) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(new SKColor(231, 76, 60)) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White)
            }
        };

        XAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = months.ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };

        YAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };
    }

    // ========== 规格占比（饼图） ==========

    private void QuerySpecPie()
    {
        var db = SqlSugarHelper.Db;
        var s = StartDate.Date;
        var e = EndDate.Date.AddDays(1);

        var specDict = db.Queryable<Specification>().ToList().ToDictionary(s => s.Id, s => s.Name);

        var specData = db.Queryable<SparePart>()
            .Where(p => p.StockInDate >= s && p.StockInDate < e)
            .ToList()
            .GroupBy(p => p.SpecificationId)
            .Select(g => new
            {
                SpecName = g.Key.HasValue && specDict.TryGetValue(g.Key.Value, out var sn) ? sn : $"(未知ID {g.Key})",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        StatusText = $"共 {specData.Count} 种规格  统计区间总入库: {specData.Sum(x => x.Count)}";

        ChartType = "Pie";
        IsPieChart = true;

        var colors = new SKColor[]
        {
            new(0, 122, 204),
            new(242, 148, 55),
            new(39, 174, 96),
            new(231, 76, 60),
            new(155, 89, 182),
            new(52, 152, 219),
            new(26, 188, 156),
            new(243, 156, 18),
            new(211, 84, 0),
            new(142, 68, 173)
        };

        var pieSeries = new ObservableCollection<ISeries>();
        for (int i = 0; i < specData.Count; i++)
        {
            var idx = i;
            pieSeries.Add(new PieSeries<double>
            {
                Values = new double[] { specData[idx].Count },
                Name = specData[idx].SpecName,
                Fill = new SolidColorPaint(colors[idx % colors.Length]),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 1 },
                HoverPushout = 5,
                Pushout = idx == 0 ? 5 : 0,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                DataLabelsSize = 12
            });
        }

        Series = pieSeries;
        XAxes = new ObservableCollection<Axis>();
        YAxes = new ObservableCollection<Axis>();
    }

    // ========== 项目统计（柱状图） ==========

    private void QueryProjectBar()
    {
        var db = SqlSugarHelper.Db;
        var s = StartDate.Date;
        var e = EndDate.Date.AddDays(1);

        var projDict = db.Queryable<Project>().ToList().ToDictionary(p => p.Id, p => p.Name);

        var inData = db.Queryable<SparePart>()
            .Where(p => p.StockInDate >= s && p.StockInDate < e && p.ProjectId != null)
            .ToList()
            .GroupBy(p => p.ProjectId!.Value)
            .ToDictionary(g => projDict.TryGetValue(g.Key, out var pn) ? pn : $"(未知ID {g.Key})", g => g.Count());

        var outData = db.Queryable<SparePart>()
            .Where(p => p.StockOutDate >= s && p.StockOutDate < e && p.ProjectId != null)
            .ToList()
            .GroupBy(p => p.ProjectId!.Value)
            .ToDictionary(g => projDict.TryGetValue(g.Key, out var pn) ? pn : $"(未知ID {g.Key})", g => g.Count());

        var allProjects = inData.Keys.Union(outData.Keys)
            .OrderByDescending(k => inData.GetValueOrDefault(k, 0) + outData.GetValueOrDefault(k, 0))
            .ToList();

        StatusText = $"共 {allProjects.Count} 个项目";

        ChartType = "Cartesian";
        IsPieChart = false;

        var inValues = allProjects.Select(p => (double)inData.GetValueOrDefault(p, 0)).ToArray();
        var outValues = allProjects.Select(p => (double)outData.GetValueOrDefault(p, 0)).ToArray();

        Series = new ObservableCollection<ISeries>
        {
            new ColumnSeries<double>
            {
                Values = inValues,
                Name = "入库",
                Fill = new SolidColorPaint(new SKColor(0, 122, 204)) { StrokeThickness = 0 },
                Stroke = null
            },
            new ColumnSeries<double>
            {
                Values = outValues,
                Name = "出库",
                Fill = new SolidColorPaint(new SKColor(231, 76, 60)) { StrokeThickness = 0 },
                Stroke = null
            }
        };

        XAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = allProjects.ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };

        YAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };
    }

    // ========== 人员统计（柱状图） ==========

    private void QueryPersonBar()
    {
        var db = SqlSugarHelper.Db;
        var s = StartDate.Date;
        var e = EndDate.Date.AddDays(1);

        var inData = db.Queryable<SparePart>()
            .Where(p => p.StockInDate >= s && p.StockInDate < e && !string.IsNullOrEmpty(p.StockInPerson))
            .ToList()
            .GroupBy(p => p.StockInPerson)
            .ToDictionary(g => g.Key, g => g.Count());

        var outData = db.Queryable<SparePart>()
            .Where(p => p.StockOutDate >= s && p.StockOutDate < e && p.StockOutPerson != null && p.StockOutPerson != "")
            .ToList()
            .GroupBy(p => p.StockOutPerson!)
            .ToDictionary(g => g.Key, g => g.Count());

        var allPersons = inData.Keys.Union(outData.Keys)
            .OrderByDescending(k => inData.GetValueOrDefault(k, 0) + outData.GetValueOrDefault(k, 0))
            .ToList();

        StatusText = $"共 {allPersons.Count} 人参与";

        ChartType = "Cartesian";
        IsPieChart = false;

        var inValues = allPersons.Select(p => (double)inData.GetValueOrDefault(p, 0)).ToArray();
        var outValues = allPersons.Select(p => (double)outData.GetValueOrDefault(p, 0)).ToArray();

        Series = new ObservableCollection<ISeries>
        {
            new ColumnSeries<double>
            {
                Values = inValues,
                Name = "入库",
                Fill = new SolidColorPaint(new SKColor(0, 122, 204)) { StrokeThickness = 0 },
                Stroke = null
            },
            new ColumnSeries<double>
            {
                Values = outValues,
                Name = "出库",
                Fill = new SolidColorPaint(new SKColor(242, 148, 55)) { StrokeThickness = 0 },
                Stroke = null
            }
        };

        XAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = allPersons.ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };

        YAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = ChineseTypeface },
                SeparatorsPaint = new SolidColorPaint(new SKColor(200, 210, 220)) { StrokeThickness = 1 }
            }
        };
    }

    // ========== 辅助方法 ==========

    private List<string> GetMonthLabels(DateTime from, DateTime to)
    {
        var labels = new List<string>();
        var cur = new DateTime(from.Year, from.Month, 1);
        while (cur < to)
        {
            labels.Add(cur.ToString("yyyy-MM"));
            cur = cur.AddMonths(1);
        }
        return labels;
    }
}
