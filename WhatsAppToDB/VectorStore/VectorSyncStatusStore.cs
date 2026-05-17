using System.Collections.Concurrent;

namespace WhatsAppToDB.Services
{
    public class VectorSyncStatus
    {
        public string Database { get; set; } = "";
        public bool IsRunning { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool? Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class VectorSyncStatusStore
    {
        private readonly ConcurrentDictionary<string, VectorSyncStatus> _statuses = new();

        public VectorSyncStatus Get(string database)
        {
            return _statuses.GetOrAdd(database, db => new VectorSyncStatus
            {
                Database = db,
                IsRunning = false,
                Message = "Not started"
            });
        }

        public void SetRunning(string database)
        {
            _statuses[database] = new VectorSyncStatus
            {
                Database = database,
                IsRunning = true,
                StartedAt = DateTime.Now,
                Message = "Running"
            };
        }

        public void SetCompleted(string database, bool success, string message)
        {
            var status = Get(database);

            status.IsRunning = false;
            status.CompletedAt = DateTime.Now;
            status.Success = success;
            status.Message = message;

            _statuses[database] = status;
        }
    }
}