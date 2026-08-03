using SqlSugar;

namespace SparePartsManager.Models;

[SugarTable("SpareParts")]
public class SparePart
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>规格 ID（关联 Specifications 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? SpecificationId { get; set; }

    /// <summary>型号 ID（关联 PartModels 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? ModelId { get; set; }

    /// <summary>厂家 ID（关联 Manufacturers 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? ManufacturerId { get; set; }

    /// <summary>所属项目 ID（关联 Projects 表，可为空）</summary>
    [SugarColumn(IsNullable = true)]
    public int? ProjectId { get; set; }

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
}
