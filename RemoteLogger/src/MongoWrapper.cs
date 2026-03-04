using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace RemoteLogger
{
    // Modelo de entrada de log almacenado en MongoDB
    public class LogEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("system")]
        public string? System { get; set; }

        [BsonElement("module")]
        public string? Module { get; set; }

        [BsonElement("level")]
        public sbyte? Level { get; set; }

        [BsonElement("message")]
        public required string Message { get; set; }
    }

    // Wrapper simple para operaciones de logs en MongoDB
    public class MongoWrapper
    {
        private readonly IMongoCollection<LogEntry> _collection;

        public MongoWrapper(string connectionString, string databaseName, string collectionName = "logs")
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("connectionString required", nameof(connectionString));
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<LogEntry>(collectionName);

            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var keys = Builders<LogEntry>.IndexKeys.Descending(e => e.Timestamp);
            var indexModel = new CreateIndexModel<LogEntry>(keys);
            _collection.Indexes.CreateOne(indexModel);

            var levelIndex = new CreateIndexModel<LogEntry>(Builders<LogEntry>.IndexKeys.Ascending(e => e.Level));
            _collection.Indexes.CreateOne(levelIndex);
        }

        public async Task<bool> InsertLogAsync(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (entry.Timestamp == default) entry.Timestamp = DateTime.UtcNow;
            try
            {
                await _collection.InsertOneAsync(entry).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<LogEntry>> GetLogsAsync(
            string? system, 
            string? module, 
            sbyte? level, 
            string? message, 
            DateTime? from, 
            DateTime? to, 
            int? limit, 
            int? offset, 
            bool? asc)
        {
            limit ??= 100;
            offset ??= 0;
            asc ??= false;
            
            var filter = Builders<LogEntry>.Filter.Empty;
            
            if (!string.IsNullOrWhiteSpace(system))
                filter &= Builders<LogEntry>.Filter.Eq(e => e.System, system);

            if (!string.IsNullOrWhiteSpace(module))
                filter &= Builders<LogEntry>.Filter.Eq(e => e.Module, module);
            
            if (level.HasValue) 
                filter &= Builders<LogEntry>.Filter.Eq(e => e.Level, level);

            if (!string.IsNullOrWhiteSpace(message))
                filter &= Builders<LogEntry>.Filter.Regex(e => e.Message, new BsonRegularExpression(message, "i"));

            if (from.HasValue) 
                filter &= Builders<LogEntry>.Filter.Gte(e => e.Timestamp, from.Value);
            
            if (to.HasValue) 
                filter &= Builders<LogEntry>.Filter.Lte(e => e.Timestamp, to.Value);

            var query = _collection.Find(filter);
            
            if (asc.Value)
                query = query.SortBy(e => e.Timestamp);
            else
                query = query.SortByDescending(e => e.Timestamp);

            return await query.Skip(offset)
                             .Limit(limit)
                             .ToListAsync()
                             .ConfigureAwait(false);
        }

        public async Task<LogEntry> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (!ObjectId.TryParse(id, out var oid)) return null;
            return await _collection.Find(e => e.Id == oid).FirstOrDefaultAsync().ConfigureAwait(false);
        }
    }
}
