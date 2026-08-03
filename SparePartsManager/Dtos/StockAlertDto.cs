using SparePartsManager.Models;

namespace SparePartsManager.Dtos;

/// <summary>
/// 库存警告规则数据传输对象（entity → dto → vo）。
/// </summary>
public class StockAlertDto
{
    public int Id { get; set; }
    public int? SpecificationId { get; set; }
    public string SpecificationName { get; set; } = "";
    public int? ModelId { get; set; }
    public string ModelName { get; set; } = "";
    public int Threshold { get; set; } = 1;
    public int CurrentStock { get; set; }
}

public static partial class EntityMapper
{
    /// <summary>
    /// StockAlert Entity → DTO（填充规格/型号名称）。
    /// </summary>
    public static StockAlertDto ToStockAlertDto(StockAlert a,
        Dictionary<int, string> specDict, Dictionary<int, string> modelDict)
    {
        return new StockAlertDto
        {
            Id = a.Id,
            SpecificationId = a.SpecificationId,
            SpecificationName = a.SpecificationId.HasValue && specDict.TryGetValue(a.SpecificationId.Value, out var sn) ? sn : "",
            ModelId = a.ModelId,
            ModelName = a.ModelId.HasValue && modelDict.TryGetValue(a.ModelId.Value, out var mn) ? mn : "",
            Threshold = a.Threshold
        };
    }
}
