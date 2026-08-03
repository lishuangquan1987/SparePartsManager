using SqlSugar;
using System.Windows;

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

        // 旧结构检测：V1 库 SpareParts 含 Specification 字符串列，与新模型不兼容。
        // 按需求「不考虑数据库兼容性、不迁移数据」，检测到旧结构时直接删除业务表，
        // 由下方 InitTables 重建（Users 表与管理员账户保留）。
        EnsureFreshSchema(db);

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

        // 字典名称唯一索引（保证规格/型号/厂家/项目名称不重复；应用层另有校验兜底）
        CreateUniqueIndexes(db);

        // 迁移旧版管理员账户（添加随机盐）
        MigrateAdminSalt(db);

        // 初始化默认管理员账户
        InitDefaultAdmin(db);
    }

    /// <summary>
    /// 旧结构检测与重建：V1 库的 SpareParts 表含 Specification 字符串列（旧结构），
    /// 或缺少 SpecificationId 列（残缺/混合结构），与新模型（FK ID 列）不兼容。
    /// 按需求不迁移数据，检测命中时弹窗确认后重建业务表（Users 保留）。
    /// </summary>
    private static void EnsureFreshSchema(ISqlSugarClient db)
    {
        try
        {
            if (!db.DbMaintenance.IsAnyTable("SpareParts", false))
                return;

            var cols = db.DbMaintenance.GetColumnInfosByTableName("SpareParts", false)
                .Select(c => c.DbColumnName.ToLower()).ToHashSet();

            // 触发条件：仍含旧 Specification 列（旧/混合结构），或缺少新 SpecificationId 列（残缺库）
            bool legacy = cols.Contains("specification") || !cols.Contains("specificationid");
            if (!legacy)
                return; // 已是新结构，无需重建

            // 破坏性操作：重建会丢弃旧数据，弹窗确认并提示备份
            var confirm = MessageBox.Show(
                "检测到旧版数据库结构（本版本不提供数据迁移兼容）。\n\n" +
                "为正常使用，需要重建业务数据表，旧版备件数据将丢失。\n" +
                "如需保留旧数据，请选择“否”并先备份数据库文件（SpareParts.db）。\n\n是否继续重建？",
                "数据库结构升级", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                Environment.Exit(0);

            // 旧结构：不迁移数据，直接删除业务表，InitTables 会按新模型重建
            DropTableIfExists(db, "SpareParts");
            DropTableIfExists(db, "StockAlerts");
            DropTableIfExists(db, "Specifications");
            DropTableIfExists(db, "PartModels");
            DropTableIfExists(db, "Manufacturers");
            DropTableIfExists(db, "Projects");

            System.Diagnostics.Debug.WriteLine(
                "[SqlSugarHelper] 检测到旧数据库结构，已重建业务表（旧数据未迁移）。");
        }
        catch (Exception ex)
        {
            // 重建失败属阻塞性故障：fail-closed，避免进入半重建状态
            System.Diagnostics.Debug.WriteLine($"[SqlSugarHelper] 旧结构检测/重建失败：{ex.Message}");
            MessageBox.Show($"数据库结构重建失败：{ex.Message}\n请检查数据库文件后重试。",
                "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private static void DropTableIfExists(ISqlSugarClient db, string table)
    {
        if (db.DbMaintenance.IsAnyTable(table, false))
            db.DbMaintenance.DropTable(table);
    }

    /// <summary>
    /// 为四个字典表的 Name 列创建唯一索引（规格/型号/厂家/项目名称不允许重复）。
    /// 若历史数据已存在重名导致建索引失败，静默跳过（应用层校验兜底）。
    /// </summary>
    private static void CreateUniqueIndexes(ISqlSugarClient db)
    {
        TryCreateUniqueIndex(db, "Specifications", "IX_Specifications_Name");
        TryCreateUniqueIndex(db, "PartModels", "IX_PartModels_Name");
        TryCreateUniqueIndex(db, "Manufacturers", "IX_Manufacturers_Name");
        TryCreateUniqueIndex(db, "Projects", "IX_Projects_Name");
    }

    private static void TryCreateUniqueIndex(ISqlSugarClient db, string table, string indexName)
    {
        try
        {
            if (db.DbMaintenance.IsAnyTable(table, false))
                db.Ado.ExecuteCommand($"CREATE UNIQUE INDEX IF NOT EXISTS {indexName} ON {table}(Name)");
        }
        catch { /* 重复数据等场景建索引失败不阻塞启动 */ }
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
