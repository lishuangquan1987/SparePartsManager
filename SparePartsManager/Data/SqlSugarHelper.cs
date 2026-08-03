using SqlSugar;

namespace SparePartsManager.Data;

/// <summary>
/// SqlSugar 数据库连接单例
/// </summary>
public static class SqlSugarHelper
{
    private static readonly string _dbPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpareParts.db");

    private static readonly Lazy<ISqlSugarClient> _db = new(() =>
    {
        var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"Data Source={_dbPath}",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        // SQL 日志输出到 Debug（开发调试用）
        client.Aop.OnLogExecuting = (sql, pars) =>
        {
            System.Diagnostics.Debug.WriteLine($"[SqlSugar] {sql}");
        };

        return client;
    });

    public static ISqlSugarClient Db => _db.Value;

    /// <summary>
    /// 初始化数据库表结构（CodeFirst）
    /// </summary>
    public static void InitDatabase()
    {
        // 确保数据库文件目录存在
        var dbDir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dbDir))
            Directory.CreateDirectory(dbDir);

        var db = Db;

        // CodeFirst 建表
        db.CodeFirst.InitTables(
            typeof(Models.User),
            typeof(Models.SparePart),
            typeof(Models.StockAlert),
            typeof(Models.Specification),
            typeof(Models.Project),
            typeof(Models.PartModel),
            typeof(Models.Manufacturer)
        );

        // 初始化字典表默认值（空库时填充常用规格/项目，便于首次使用）
        InitDefaultSpecifications(db);
        InitDefaultProjects(db);

        // 迁移旧版管理员账户（添加随机盐）
        MigrateAdminSalt(db);

        // 初始化默认管理员账户
        InitDefaultAdmin(db);
    }

    private static void InitDefaultAdmin(ISqlSugarClient db)
    {
        var admin = db.Queryable<Models.User>()
            .First(u => u.Username == "admin");
        if (admin == null)
        {
            var salt = GenerateSalt();
            db.Insertable(new Models.User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123", salt),
                Salt = salt,
                RealName = "系统管理员",
                Role = "Admin",
                CreatedAt = DateTime.Now
            }).ExecuteCommand();
        }
    }

    /// <summary>
    /// 迁移旧版无盐管理员账户（安全升级：重置为默认密码）
    /// </summary>
    private static void MigrateAdminSalt(ISqlSugarClient db)
    {
        try
        {
            var admin = db.Queryable<Models.User>()
                .First(u => u.Username == "admin" && string.IsNullOrEmpty(u.Salt));
            if (admin != null)
            {
                var salt = GenerateSalt();
                db.Updateable<Models.User>()
                    .SetColumns(it => new Models.User
                    {
                        Salt = salt,
                        PasswordHash = HashPassword("admin123", salt)
                    })
                    .Where(it => it.Id == admin.Id)
                    .ExecuteCommand();

                System.Diagnostics.Debug.WriteLine(
                    "[安全升级] 管理员密码已重置为 admin123，请尽快修改。");
            }
        }
        catch { /* 迁移失败不阻塞启动 */ }
    }

    /// <summary>
    /// 生成 16 字节随机盐（hex 编码，32 字符）
    /// </summary>
    public static string GenerateSalt()
    {
        var bytes = Compat.GetRandomBytes(16);
        return Compat.ToHex(bytes);
    }

    /// <summary>
    /// 初始化规格字典默认值
    /// </summary>
    private static void InitDefaultSpecifications(ISqlSugarClient db)
    {
        try
        {
            var exists = db.Queryable<Models.Specification>().Any();
            if (!exists)
            {
                var defaults = new[] { "电源", "PLC", "空开", "接触器", "继电器", "传感器", "变频器", "按钮", "指示灯", "端子" };
                db.Insertable(defaults.Select(s => new Models.Specification { Name = s }).ToList()).ExecuteCommand();
            }
        }
        catch { }
    }

    /// <summary>
    /// 初始化项目字典默认值
    /// </summary>
    private static void InitDefaultProjects(ISqlSugarClient db)
    {
        try
        {
            var exists = db.Queryable<Models.Project>().Any();
            if (!exists)
            {
                var defaults = new[] { "A项目", "B项目", "C项目" };
                db.Insertable(defaults.Select(p => new Models.Project { Name = p }).ToList()).ExecuteCommand();
            }
        }
        catch { }
    }

    /// <param name="password">明文密码</param>
    /// <param name="saltHex">hex 编码的随机盐</param>
    public static string HashPassword(string password, string saltHex)
    {
        var salt = Compat.FromHex(saltHex);
        var bytes = Compat.Pbkdf2Hash(password, salt, 100_000, 32);
        return Compat.ToHex(bytes);
    }
}
