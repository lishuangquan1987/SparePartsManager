using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 规格字典表
/// </summary>
[SugarTable("Specifications")]
public class Specification
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string Name { get; set; } = string.Empty;
}
