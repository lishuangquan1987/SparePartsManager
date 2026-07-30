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

    /// <summary>规格</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Specification { get; set; } = string.Empty;

    /// <summary>型号</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Model { get; set; } = string.Empty;

    /// <summary>最低库存阈值</summary>
    public int Threshold { get; set; } = 1;
}
