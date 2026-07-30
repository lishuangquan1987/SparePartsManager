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
            typeof(Models.Project)
        );

        // 兼容升级：为已存在的表补齐缺失的列
        EnsureColumns(db);

        // 迁移旧版管理员账户（添加随机盐）
        MigrateAdminSalt(db);

        // 初始化默认管理员账户
        InitDefaultAdmin(db);
    }

    /// <summary>
    /// 补齐已存在表中缺失的列（兼容旧版本数据库升级）
    /// </summary>
    private static void EnsureColumns(ISqlSugarClient db)
    {
        try
        {
            // 修复旧表 SpareParts 的 StockOutDate NOT NULL 问题
            FixStockOutDateNotNull(db);

            if (db.DbMaintenance.IsAnyTable("SpareParts", false))
            {
                var columns = db.DbMaintenance.GetColumnInfosByTableName("SpareParts", false);
                var colNames = columns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // 新增货位三字段
                AddColumnIfMissing(db, "SpareParts", "ShelfNo", "INTEGER", 0);
                AddColumnIfMissing(db, "SpareParts", "LayerNo", "INTEGER", 0);
                AddColumnIfMissing(db, "SpareParts", "PositionNo", "INTEGER", 0);
                // 新增项目字段
                AddColumnIfMissing(db, "SpareParts", "ProjectName", "NVARCHAR(100)", "");
            }

            // 初始化规格字典表默认值
            InitDefaultSpecifications(db);
            // 初始化项目字典表默认值
            InitDefaultProjects(db);

            if (db.DbMaintenance.IsAnyTable("Users", false))
            {
                var cols = db.DbMaintenance.GetColumnInfosByTableName("Users", false);
                var names = cols.Select(c => c.DbColumnName.ToLower()).ToHashSet();
                if (!names.Contains("salt"))
                {
                    db.DbMaintenance.AddColumn("Users",
                        new SqlSugar.DbColumnInfo
                        {
                            DbColumnName = "Salt",
                            DataType = "nvarchar(32)",
                            Length = 32,
                            IsNullable = true,
                            DefaultValue = ""
                        });
                }
            }
        }
        catch
        {
            // 非关键操作，静默失败
        }
    }

    /// <summary>
    /// 修复旧版数据库中 StockOutDate 的 NOT NULL 约束（SQLite 重建表）
    /// </summary>
    private static void FixStockOutDateNotNull(ISqlSugarClient db)
    {
        try
        {
            if (!db.DbMaintenance.IsAnyTable("SpareParts", false))
                return;

            var columns = db.DbMaintenance.GetColumnInfosByTableName("SpareParts", false);
            var stockOutCol = columns.FirstOrDefault(c =>
                c.DbColumnName.Equals("StockOutDate", StringComparison.OrdinalIgnoreCase));

            // 如果列不存在或已经是 NULLABLE，跳过
            if (stockOutCol == null || stockOutCol.IsNullable)
                return;

            // SQLite 不支持 ALTER COLUMN 修改约束，需要重建表
            db.Ado.BeginTran();
            try
            {
                // 1. 创建新表（StockOutDate 可为 NULL）
                db.Ado.ExecuteCommand(@"
                    CREATE TABLE SpareParts_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name NVARCHAR(100) NOT NULL,
                        Specification NVARCHAR(50) NOT NULL,
                        Model NVARCHAR(100) NOT NULL,
                        Manufacturer NVARCHAR(200) NULL,
                        ShelfNo INTEGER DEFAULT 0,
                        LayerNo INTEGER DEFAULT 0,
                        PositionNo INTEGER DEFAULT 0,
                        StockInDate DATETIME NOT NULL,
                        StockOutDate DATETIME NULL,
                        Remark NVARCHAR(500) NULL,
                        StockInPerson NVARCHAR(50) NULL,
                        StockOutPerson NVARCHAR(50) NULL,
                        Status NVARCHAR(20) NOT NULL
                    )");

                // 2. 复制数据（旧 Location 列忽略，三货位列默认0）
                db.Ado.ExecuteCommand(
                    @"INSERT INTO SpareParts_new (Id, Name, Specification, Model, Manufacturer,
                      ShelfNo, LayerNo, PositionNo, StockInDate, StockOutDate,
                      Remark, StockInPerson, StockOutPerson, Status)
                    SELECT Id, Name, Specification, Model, Manufacturer,
                      0, 0, 0, StockInDate, StockOutDate,
                      Remark, StockInPerson, StockOutPerson, Status
                    FROM SpareParts");

                // 3. 删除旧表
                db.Ado.ExecuteCommand("DROP TABLE SpareParts");

                // 4. 重命名
                db.Ado.ExecuteCommand("ALTER TABLE SpareParts_new RENAME TO SpareParts");

                db.Ado.CommitTran();
            }
            catch
            {
                db.Ado.RollbackTran();
                throw;
            }
        }
        catch
        {
            // 修复失败不阻塞启动，下次 CodeFirst.InitTables 会尝试建新表
        }
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
    /// 补齐缺失列
    /// </summary>
    private static void AddColumnIfMissing(ISqlSugarClient db, string table, string col, string dataType, object defaultValue)
    {
        var cols = db.DbMaintenance.GetColumnInfosByTableName(table, false);
        if (!cols.Any(c => c.DbColumnName.Equals(col, StringComparison.OrdinalIgnoreCase)))
        {
            db.DbMaintenance.AddColumn(table,
                new SqlSugar.DbColumnInfo
                {
                    DbColumnName = col,
                    DataType = dataType,
                    IsNullable = true,
                    DefaultValue = defaultValue?.ToString() ?? ""
                });
        }
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
