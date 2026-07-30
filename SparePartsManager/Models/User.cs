using SqlSugar;

namespace SparePartsManager.Models;

/// <summary>
/// 用户实体
/// </summary>
[SugarTable("Users")]
public class User
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>用户名（登录用）</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    /// <summary>密码哈希（PBKDF2-SHA256，100K 迭代）</summary>
    [SugarColumn(Length = 64, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>密码随机盐（hex 编码，32 字符）</summary>
    [SugarColumn(Length = 32, IsNullable = false)]
    public string Salt { get; set; } = string.Empty;

    /// <summary>真实姓名</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string RealName { get; set; } = string.Empty;

    /// <summary>角色：Admin / Operator</summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    public string Role { get; set; } = "Operator";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
