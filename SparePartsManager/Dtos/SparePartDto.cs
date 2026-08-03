using SparePartsManager.Models;

namespace SparePartsManager.Dtos;

/// <summary>
/// 备件数据传输对象：介于 Entity（SparePart）与展示 VO 之间，
/// 附带字典名称字段（SpecificationName 等）供查询展示使用。
/// </summary>
public class SparePartDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public int? SpecificationId { get; set; }
    public string SpecificationName { get; set; } = "";
    public int? ModelId { get; set; }
    public string ModelName { get; set; } = "";
    public int? ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = "";
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = "";

    public int ShelfNo { get; set; }
    public int LayerNo { get; set; }
    public int PositionNo { get; set; }
    public DateTime StockInDate { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string StockInPerson { get; set; } = "";
    public string? StockOutPerson { get; set; }
    public string Status { get; set; } = "InStock";
    public string Remark { get; set; } = "";
}

/// <summary>
/// Entity ↔ DTO 映射工具。
/// </summary>
public static partial class EntityMapper
{
    /// <summary>
    /// Entity → DTO（查询展示链路：entities → dto → vo）。
    /// 通过四张字典表的名称为 DTO 填充展示名称。
    /// </summary>
    public static SparePartDto ToSparePartDto(SparePart e,
        Dictionary<int, string> specDict, Dictionary<int, string> modelDict,
        Dictionary<int, string> manDict, Dictionary<int, string> projDict)
    {
        return new SparePartDto
        {
            Id = e.Id,
            Name = e.Name,
            SpecificationId = e.SpecificationId,
            SpecificationName = e.SpecificationId.HasValue && specDict.TryGetValue(e.SpecificationId.Value, out var sn) ? sn : "",
            ModelId = e.ModelId,
            ModelName = e.ModelId.HasValue && modelDict.TryGetValue(e.ModelId.Value, out var mn) ? mn : "",
            ManufacturerId = e.ManufacturerId,
            ManufacturerName = e.ManufacturerId.HasValue && manDict.TryGetValue(e.ManufacturerId.Value, out var man) ? man : "",
            ProjectId = e.ProjectId,
            ProjectName = e.ProjectId.HasValue && projDict.TryGetValue(e.ProjectId.Value, out var proj) ? proj : "",
            ShelfNo = e.ShelfNo,
            LayerNo = e.LayerNo,
            PositionNo = e.PositionNo,
            StockInDate = e.StockInDate,
            StockOutDate = e.StockOutDate,
            StockInPerson = e.StockInPerson,
            StockOutPerson = e.StockOutPerson,
            Status = e.Status,
            Remark = e.Remark
        };
    }

    /// <summary>
    /// DTO → Entity（写入链路：vo → dto → entities）。
    /// </summary>
    public static SparePart ToSparePartEntity(SparePartDto dto)
    {
        return new SparePart
        {
            Id = dto.Id,
            Name = dto.Name,
            SpecificationId = dto.SpecificationId,
            ModelId = dto.ModelId,
            ManufacturerId = dto.ManufacturerId,
            ProjectId = dto.ProjectId,
            ShelfNo = dto.ShelfNo,
            LayerNo = dto.LayerNo,
            PositionNo = dto.PositionNo,
            StockInDate = dto.StockInDate,
            StockOutDate = dto.StockOutDate,
            StockInPerson = dto.StockInPerson,
            StockOutPerson = dto.StockOutPerson,
            Status = dto.Status,
            Remark = dto.Remark
        };
    }
}
