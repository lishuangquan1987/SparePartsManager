using SqlSugar;

namespace SparePartsManager.Models;

[SugarTable("SpareParts")]
public class SparePart
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = false)]
    public string Specification { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Model { get; set; } = string.Empty;

    [SugarColumn(Length = 200, IsNullable = true)]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>货架号</summary>
    public int ShelfNo { get; set; }

    /// <summary>层号</summary>
    public int LayerNo { get; set; }

    /// <summary>区位号</summary>
    public int PositionNo { get; set; }

    public DateTime StockInDate { get; set; } = DateTime.Now;

    [SugarColumn(IsNullable = true)]
    public DateTime? StockOutDate { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string Remark { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string StockInPerson { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? StockOutPerson { get; set; }

    [SugarColumn(Length = 20, IsNullable = false)]
    public string Status { get; set; } = "InStock";

    /// <summary>所属项目（可为空）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? ProjectName { get; set; }
}
