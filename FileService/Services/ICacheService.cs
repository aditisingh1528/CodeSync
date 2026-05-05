namespace FileService.Services
{
    /// <summary>
    /// Cache abstraction — same pattern as ProjectService.
    /// Keeps FileServiceImpl clean and makes caching mockable in tests.
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }
}
