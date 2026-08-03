using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 库存警告阈值 — 按「规格 + 型号」设置最低库存数量
/// </summary>
[SugarTable("StockAlerts")]
public class StockAlert
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>规格 ID（关联 Specifications 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? SpecificationId { get; set; }

    /// <summary>型号 ID（关联 PartModels 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? ModelId { get; set; }

    /// <summary>最低库存阈值</summary>
    public int Threshold { get; set; } = 1;
}
