using OfficeOpenXml;
using SparePartsManager.Data;
using SparePartsManager.Dtos;
using SparePartsManager.Models;
using SqlSugar;
using System.Text;

namespace SparePartsManager.Services;

/// <summary>
/// Excel 导入导出服务（EPPlus 4.x，.xlsx）。模板列与导出一致：
/// Id,名称,规格,型号,厂家,项目,货架,层,位,入库日期,出库日期,入库人,出库人,状态,备注
/// 导入时按行新增（忽略 Id 列），规格/型号/厂家/项目按名称匹配字典表，
/// 不存在时按导入选项自动创建，或（未勾选自动创建）收集错误行。
/// </summary>
public static class ExcelService
{
    public class ImportOptions
    {
        public bool AutoCreateSpec { get; set; } = true;
        public bool AutoCreateModel { get; set; } = true;
        public bool AutoCreateManufacturer { get; set; } = true;
        public bool AutoCreateProject { get; set; } = true;
    }

    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; } = new();
    }

    public static readonly string[] HeaderTemplate =
    {
        "Id", "名称", "规格", "型号", "厂家", "项目", "货架", "层", "位",
        "入库日期", "出库日期", "入库人", "出库人", "状态", "备注"
    };

    private const int HeaderRow = 1;
    private const int ColName = 2;
    private const int ColSpec = 3;
    private const int ColModel = 4;
    private const int ColManufacturer = 5;
    private const int ColProject = 6;
    private const int ColShelf = 7;
    private const int ColLayer = 8;
    private const int ColPosition = 9;
    private const int ColStockInDate = 10;
    private const int ColStockOutDate = 11;
    private const int ColStockInPerson = 12;
    private const int ColStockOutPerson = 13;
    private const int ColStatus = 14;
    private const int ColRemark = 15;

    /// <summary>导出 .xlsx 备件数据（模板列与导入一致）。</summary>
    public static void ExportXlsx(string filePath, IReadOnlyList<SparePartDto> data)
    {
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("备件");

        for (int c = 0; c < HeaderTemplate.Length; c++)
            ws.Cells[HeaderRow, c + 1].Value = HeaderTemplate[c];

        int r = HeaderRow + 1;
        foreach (var d in data)
        {
            ws.Cells[r, 1].Value = d.Id;
            ws.Cells[r, 2].Value = SanitizeCell(d.Name);
            ws.Cells[r, 3].Value = SanitizeCell(d.SpecificationName);
            ws.Cells[r, 4].Value = SanitizeCell(d.ModelName);
            ws.Cells[r, 5].Value = SanitizeCell(d.ManufacturerName);
            ws.Cells[r, 6].Value = SanitizeCell(d.ProjectName);
            ws.Cells[r, 7].Value = d.ShelfNo;
            ws.Cells[r, 8].Value = d.LayerNo;
            ws.Cells[r, 9].Value = d.PositionNo;
            ws.Cells[r, 10].Value = d.StockInDate == default ? "" : d.StockInDate.ToString("yyyy-MM-dd");
            ws.Cells[r, 11].Value = d.StockOutDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cells[r, 12].Value = SanitizeCell(d.StockInPerson);
            ws.Cells[r, 13].Value = SanitizeCell(d.StockOutPerson ?? "");
            ws.Cells[r, 14].Value = d.Status == "InStock" ? "在库" : "已出库";
            ws.Cells[r, 15].Value = SanitizeCell(d.Remark);
            r++;
        }

        ws.Cells[HeaderRow, 1, r - 1, HeaderTemplate.Length].AutoFitColumns();
        pkg.SaveAs(new FileInfo(filePath));
    }

    /// <summary>导入 .xlsx 备件数据（模板与导出格式一致）。</summary>
    public static ImportResult Import(string filePath, ImportOptions options)
    {
        var result = new ImportResult();
        if (!File.Exists(filePath))
        {
            result.Errors.Add($"文件不存在：{filePath}");
            return result;
        }

        try
        {
            using var pkg = new ExcelPackage(new FileInfo(filePath));
            var ws = pkg.Workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Errors.Add("工作簿中没有工作表。");
                return result;
            }
            if (ws.Dimension == null || ws.Dimension.End.Row < HeaderRow)
            {
                result.Errors.Add("模板为空或缺少表头。");
                return result;
            }

            // 表头校验：与导出模板列序完全一致（防止列错位导致静默错数据）
            for (int c = 1; c <= HeaderTemplate.Length; c++)
            {
                var cellText = (ws.Cells[HeaderRow, c].Text ?? "").Trim();
                if (cellText != HeaderTemplate[c - 1])
                {
                    result.Errors.Add("模板表头与导出模板不一致，请使用「导出」生成的 .xlsx 文件作为模板（列顺序：Id,名称,规格,型号,厂家,项目,货架,层,位,入库日期,出库日期,入库人,出库人,状态,备注）。");
                    return result;
                }
            }

            var db = SqlSugarHelper.Db;

            // 字典名称 → ID 映射（含自动创建登记）
            var specMap = db.Queryable<Specification>().ToList().ToDictionary(s => s.Name, s => s.Id);
            var modelMap = db.Queryable<PartModel>().ToList().ToDictionary(m => m.Name, m => m.Id);
            var manMap = db.Queryable<Manufacturer>().ToList().ToDictionary(m => m.Name, m => m.Id);
            var projMap = db.Queryable<Project>().ToList().ToDictionary(p => p.Name, p => p.Id);

            int lastRow = ws.Dimension.End.Row;
            for (int r = HeaderRow + 1; r <= lastRow; r++)
            {
                try
                {
                    var name = Get(ws, r, ColName);
                    if (string.IsNullOrEmpty(name))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"第{r}行：名称为空。");
                        continue;
                    }

                    // 规格/型号：必填
                    var specName = Get(ws, r, ColSpec);
                    if (string.IsNullOrEmpty(specName))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"第{r}行（{name}）：规格为空。");
                        continue;
                    }
                    var specId = ResolveDictId(db, specMap, specName, options.AutoCreateSpec, "规格", r, name, result);
                    if (!specId.HasValue) continue;

                    var modelName = Get(ws, r, ColModel);
                    if (string.IsNullOrEmpty(modelName))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"第{r}行（{name}）：型号为空。");
                        continue;
                    }
                    var modelId = ResolveDictId(db, modelMap, modelName, options.AutoCreateModel, "型号", r, name, result);
                    if (!modelId.HasValue) continue;

                    // 厂家/项目：可空
                    var manName = Get(ws, r, ColManufacturer);
                    int? manId = null;
                    if (!string.IsNullOrEmpty(manName))
                    {
                        manId = ResolveDictId(db, manMap, manName, options.AutoCreateManufacturer, "厂家", r, name, result);
                        if (!manId.HasValue) continue;
                    }

                    var projName = Get(ws, r, ColProject);
                    int? projId = null;
                    if (!string.IsNullOrEmpty(projName))
                    {
                        projId = ResolveDictId(db, projMap, projName, options.AutoCreateProject, "项目", r, name, result);
                        if (!projId.HasValue) continue;
                    }

                    int.TryParse(Get(ws, r, ColShelf), out var shelf);
                    int.TryParse(Get(ws, r, ColLayer), out var layer);
                    int.TryParse(Get(ws, r, ColPosition), out var position);

                    var stockInDate = DateTime.TryParse(Get(ws, r, ColStockInDate), out var sid)
                        ? sid.Date
                        : DateTime.Today;
                    DateTime? stockOutDate = null;
                    var soDateStr = Get(ws, r, ColStockOutDate);
                    if (!string.IsNullOrEmpty(soDateStr) && DateTime.TryParse(soDateStr, out var sod))
                        stockOutDate = sod;

                    var statusText = Get(ws, r, ColStatus);
                    var status = statusText == "已出库" ? "OutStock" : "InStock";

                    var stockInPerson = Get(ws, r, ColStockInPerson);
                    var stockOutPerson = Get(ws, r, ColStockOutPerson);

                    db.Insertable(new SparePart
                    {
                        Name = name,
                        SpecificationId = specId,
                        ModelId = modelId,
                        ManufacturerId = manId,
                        ProjectId = projId,
                        ShelfNo = shelf,
                        LayerNo = layer,
                        PositionNo = position,
                        StockInDate = stockInDate,
                        StockOutDate = stockOutDate,
                        StockInPerson = stockInPerson,
                        StockOutPerson = string.IsNullOrEmpty(stockOutPerson) ? null : stockOutPerson,
                        Status = status,
                        Remark = Get(ws, r, ColRemark)
                    }).ExecuteCommand();

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"第{r}行：{ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"读取文件失败：{ex.Message}");
        }

        return result;
    }

    private static string Get(ExcelWorksheet ws, int row, int col)
    {
        var cell = ws.Cells[row, col];
        var text = cell.Value == null ? "" : Convert.ToString(cell.Text);
        return (text ?? "").Trim();
    }

    /// <summary>
    /// 防公式注入（CWE-1236）：以 = + - @ \t \r 开头的字符串在 Excel 中会被解析为公式，
    /// 写入前加前导单引号使其按文本显示。
    /// </summary>
    private static object SanitizeCell(object value)
    {
        if (value is string s && s.Length > 0)
        {
            var c = s[0];
            if (c == '=' || c == '+' || c == '-' || c == '@' || c == '\t' || c == '\r')
                return "'" + s;
        }
        return value;
    }

    /// <summary>
    /// 解析字典名称：已存在则返回 ID；不存在时按 autoCreate 创建或收集错误。
    /// </summary>
    private static int? ResolveDictId(ISqlSugarClient db, Dictionary<string, int> map,
        string name, bool autoCreate, string kind, int row, string partName, ImportResult result)
    {
        if (map.TryGetValue(name, out var id))
            return id;

        if (!autoCreate)
        {
            result.ErrorCount++;
            result.Errors.Add($"第{row}行（{partName}）：{kind}「{name}」不存在（未勾选自动创建）。");
            return null;
        }

        // 自动创建：插入字典并登记（若并发/重复插入失败则回查一次）
        try
        {
            int newId;
            switch (kind)
            {
                case "规格":
                    newId = db.Insertable(new Specification { Name = name }).ExecuteReturnIdentity();
                    break;
                case "型号":
                    newId = db.Insertable(new PartModel { Name = name }).ExecuteReturnIdentity();
                    break;
                case "厂家":
                    newId = db.Insertable(new Manufacturer { Name = name }).ExecuteReturnIdentity();
                    break;
                default:
                    newId = db.Insertable(new Project { Name = name }).ExecuteReturnIdentity();
                    break;
            }
            map[name] = newId;
            return newId;
        }
        catch
        {
            // 唯一索引冲突（并发场景）：回查已存在的 ID
            int existingId;
            switch (kind)
            {
                case "规格":
                    existingId = db.Queryable<Specification>().First(s => s.Name == name).Id;
                    break;
                case "型号":
                    existingId = db.Queryable<PartModel>().First(m => m.Name == name).Id;
                    break;
                case "厂家":
                    existingId = db.Queryable<Manufacturer>().First(m => m.Name == name).Id;
                    break;
                default:
                    existingId = db.Queryable<Project>().First(p => p.Name == name).Id;
                    break;
            }
            map[name] = existingId;
            return existingId;
        }
    }
}
