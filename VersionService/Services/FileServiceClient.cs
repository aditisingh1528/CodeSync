namespace VersionService.Services
{
    public interface IFileServiceClient
    {
        Task<bool> UpdateFileContentAsync(int fileId, string content, string bearerToken);
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

        public async Task<bool> UpdateFileContentAsync(int fileId, string content, string bearerToken)
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
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("FileService update failed for fileId {FileId}: {Error}", fileId, ex.Message);
                return false;
            }
        }
    }
}
