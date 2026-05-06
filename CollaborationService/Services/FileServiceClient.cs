namespace CollaborationService.Services
{
    public interface IFileServiceClient
    {
        Task SyncContentAsync(int fileId, string content, string bearerToken);
    }

    public class FileServiceClient : IFileServiceClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<FileServiceClient> _logger;

        public FileServiceClient(HttpClient http, ILogger<FileServiceClient> logger)
        {
            _http   = http;
            _logger = logger;
        }

        public async Task SyncContentAsync(int fileId, string content, string bearerToken)
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                var body = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { content }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _http.PutAsync($"/api/files/{fileId}/content", body);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("FileService sync failed for fileId {FileId}: {Status}", fileId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("FileService sync error for fileId {FileId}: {Error}", fileId, ex.Message);
            }
        }
    }
}
