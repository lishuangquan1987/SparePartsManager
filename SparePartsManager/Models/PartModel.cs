using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 型号字典表
/// </summary>
[SugarTable("PartModels")]
public class PartModel
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;
}
