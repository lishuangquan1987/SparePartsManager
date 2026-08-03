using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 厂家字典表
/// </summary>
[SugarTable("Manufacturers")]
public class Manufacturer
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;
}
