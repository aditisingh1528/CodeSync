using System.Security.Claims;
using CollaborationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CollaborationService.Hubs
{
    [Authorize]
    public class CollaborationHub : Hub
    {
        private readonly ISessionStore       _sessions;
        private readonly IFileServiceClient  _fileClient;
        private readonly ILogger<CollaborationHub> _logger;

        public CollaborationHub(
            ISessionStore      sessions,
            IFileServiceClient fileClient,
            ILogger<CollaborationHub> logger)
        {
            _sessions   = sessions;
            _fileClient = fileClient;
            _logger     = logger;
        }

        private int GetUserId()
            => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task JoinSession(string sessionId)
        {
            var session = await _sessions.GetAsync(sessionId);
            if (session is null)
            {
                await Clients.Caller.SendAsync("Error", "Session not found.");
                return;
            }

            var userId = GetUserId();
            await _sessions.AddUserAsync(sessionId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            await Clients.Group(sessionId).SendAsync("UserJoined", new
            {
                userId,
                sessionId,
                participantCount = session.JoinedUserIds.Count
            });

            _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
        }

        public async Task LeaveSession(string sessionId)
        {
            var userId = GetUserId();
            await _sessions.RemoveUserAsync(sessionId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

            await Clients.Group(sessionId).SendAsync("UserLeft", new { userId, sessionId });
        }

        public async Task SendCodeChange(string sessionId, string content, string bearerToken)
        {
            var session = await _sessions.GetAsync(sessionId);
            if (session is null)
            {
                await Clients.Caller.SendAsync("Error", "Session not found.");
                return;
            }

            var userId = GetUserId();

            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveCodeChange", new
            {
                sessionId,
                content,
                changedByUserId = userId,
                timestamp       = DateTime.UtcNow
            });

            await _fileClient.SyncContentAsync(session.FileId, content, bearerToken);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var all    = await _sessions.GetAllAsync();

            foreach (var session in all.Where(s => s.JoinedUserIds.Contains(userId)))
            {
                await _sessions.RemoveUserAsync(session.SessionId, userId);
                await Clients.Group(session.SessionId).SendAsync("UserLeft", new
                {
                    userId,
                    sessionId = session.SessionId
                });
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
