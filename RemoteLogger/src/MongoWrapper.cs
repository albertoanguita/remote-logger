using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace HomesecEngine.Data
{
    // Modelo de entrada de log almacenado en MongoDB
    public class LogEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("level")]
        public string Level { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }

        // ...existing code...
    }

    // Wrapper simple para operaciones de logs en MongoDB
    public class MongoWrapper
    {
        private readonly IMongoCollection<LogEntry> _collection;

        public MongoWrapper(string connectionString, string databaseName = "homesec", string collectionName = "logs")
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

        public async Task InsertLogAsync(LogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (entry.Timestamp == default) entry.Timestamp = DateTime.UtcNow;
            await _collection.InsertOneAsync(entry).ConfigureAwait(false);
        }

        public async Task<List<LogEntry>> GetLogsAsync(DateTime? from = null, DateTime? to = null, string level = null, int limit = 100)
        {
            var filter = Builders<LogEntry>.Filter.Empty;

            if (from.HasValue) filter &= Builders<LogEntry>.Filter.Gte(e => e.Timestamp, from.Value);
            if (to.HasValue) filter &= Builders<LogEntry>.Filter.Lte(e => e.Timestamp, to.Value);
            if (!string.IsNullOrWhiteSpace(level)) filter &= Builders<LogEntry>.Filter.Eq(e => e.Level, level);

            return await _collection.Find(filter)
                                    .SortByDescending(e => e.Timestamp)
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

        // ...existing code...
    }
}
