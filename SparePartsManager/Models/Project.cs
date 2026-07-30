using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 项目字典表
/// </summary>
[SugarTable("Projects")]
public class Project
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;
}
