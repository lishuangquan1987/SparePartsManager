using SparePartsManager.Models;

namespace SparePartsManager.Services;

/// <summary>
/// 全局登录用户上下文
/// </summary>
public static class CurrentUser
{
    public static User? LoginUser { get; set; }

    public static bool IsAdmin =>
        LoginUser != null && LoginUser.Role == "Admin";

    public static bool IsLoggedIn => LoginUser != null;
}
