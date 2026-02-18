using Dapper;
using MySqlConnector;
using RemoteLogger.dto;

namespace RemoteLogger;



/*
 * CREATE INDEX RemoteLog_Timestamp_IDX USING BTREE ON test.RemoteLog (`Timestamp`);
 */
public class DbWrapper
{
    public class LogEntity
    {
        public ulong Id { get; set; }
        public long Timestamp { get; set; }
        public required string System { get; set; }
        public required string Module { get; set; }
        public required string LogLevel { get; set; }
        public required string Message { get; set; }
    }
    
    private readonly string _connectionString;
    private readonly string _table;
    
    private const string TimestampCol = nameof(LogEntity.Timestamp);
    private const string SystemCol = nameof(LogEntity.System);
    private const string ModuleCol = nameof(LogEntity.Module);
    private const string LogLevelCol = nameof(LogEntity.LogLevel);
    private const string MessageCol = nameof(LogEntity.Message);

    public DbWrapper(string connectionString, string table)
    {
        _connectionString = connectionString;
        _table = table;
    }

    public bool StoreLog(LogDto log)
    {
        using var con = new MySqlConnection(_connectionString);
        con.Open();

        var res = con.Execute($"INSERT INTO {_table}({TimestampCol}, {SystemCol}, {ModuleCol}, {LogLevelCol}, {MessageCol}) values (@timestamp, @system, @module, @logLevel, @message)", 
            new { timestamp = log.Timestamp, system = log.System, module = log.Module, logLevel = log.LogLevel, message = log.Message });
        
        return res > 0;
    }

    public LogResponseDto[] GetLogs(LogQueryDto query)
    {
        using var con = new MySqlConnection(_connectionString);
        con.Open();
        var sql = $"SELECT * FROM {_table}";
        var where = new List<string>();
        if (query.System != null)
            where.Add($"{SystemCol} = @system");
        
        if (query.Module != null)
            where.Add($"{ModuleCol} = @module");
        
        if (query.LogLevel != null)
            where.Add($"{LogLevelCol} >= @loglevel");
        
        if (query.Message != null)
            where.Add($"{MessageCol} LIKE @message");
        
        if (query.From != null)
            where.Add($"{TimestampCol} >= @from");
        
        if (query.To != null)
            where.Add($"{TimestampCol} <= @to");

        if (where.Count != 0)
        {
            var whereClause = string.Join(" AND ", where.ToArray());
            sql = $"{sql} WHERE {whereClause}";
        }

        sql = $"{sql} ORDER BY {TimestampCol} {(query.Asc!.Value ? "ASC" : "DESC")}";

        sql = $"{sql} LIMIT @limit";

        sql = $"{sql} OFFSET @offset";

        var logEntities = con.Query<LogEntity>(sql,
            new
            {
                system = query.System, 
                module = query.Module, 
                loglevel = query.LogLevel,
                message = $"%{query.Message}%", 
                from = query.From, 
                to = query.To,
                limit = query.Limit, 
                offset = query.Offset
            });
        
        return logEntities.Select(log => new LogResponseDto
        {
            System = log.System,
            Module = log.Module,
            Message = log.Message,
            Timestamp = log.Timestamp
        }).ToArray();
    }
}