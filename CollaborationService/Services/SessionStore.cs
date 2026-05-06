using CollaborationService.Models;

namespace CollaborationService.Services
{
    public interface ISessionStore
    {
        Task<CollaborationSession>  CreateAsync(int fileId, int projectId, int ownerId);
        Task<CollaborationSession?> GetAsync(string sessionId);
        Task                        AddUserAsync(string sessionId, int userId);
        Task                        RemoveUserAsync(string sessionId, int userId);
        Task<List<CollaborationSession>> GetAllAsync();
    }

    public class InMemorySessionStore : ISessionStore
    {
        private readonly Dictionary<string, CollaborationSession> _sessions = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<CollaborationSession> CreateAsync(int fileId, int projectId, int ownerId)
        {
            var session = new CollaborationSession
            {
                SessionId  = Guid.NewGuid().ToString("N"),
                FileId     = fileId,
                ProjectId  = projectId,
                OwnerId    = ownerId,
                CreatedAt  = DateTime.UtcNow,
                JoinedUserIds = new List<int> { ownerId }
            };

            await _lock.WaitAsync();
            try   { _sessions[session.SessionId] = session; }
            finally { _lock.Release(); }

            return session;
        }

        public async Task<CollaborationSession?> GetAsync(string sessionId)
        {
            await _lock.WaitAsync();
            try   { return _sessions.TryGetValue(sessionId, out var s) ? s : null; }
            finally { _lock.Release(); }
        }

        public async Task AddUserAsync(string sessionId, int userId)
        {
            await _lock.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(sessionId, out var s) && !s.JoinedUserIds.Contains(userId))
                    s.JoinedUserIds.Add(userId);
            }
            finally { _lock.Release(); }
        }

        public async Task RemoveUserAsync(string sessionId, int userId)
        {
            await _lock.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(sessionId, out var s))
                    s.JoinedUserIds.Remove(userId);
            }
            finally { _lock.Release(); }
        }

        public async Task<List<CollaborationSession>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try   { return _sessions.Values.ToList(); }
            finally { _lock.Release(); }
        }
    }
}
