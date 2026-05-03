namespace ProjectService.Services
{
    /// <summary>
    /// ICACHESERVICE
    /// ==============
    /// Abstraction over Redis (IDistributedCache).
    ///
    /// Why abstract it?
    ///   1. ProjectServiceImpl doesn't know or care if we use Redis,
    ///      MemoryCache, or a future cache — it just calls Get/Set/Remove.
    ///   2. Unit tests can mock this interface — no real Redis needed.
    ///
    /// Key design:
    ///   - Get<T>  → returns null if key doesn't exist or is expired
    ///   - Set<T>  → serializes object to JSON, stores with expiry
    ///   - Remove  → deletes a single key
    ///   - RemoveByPrefix → deletes all keys that start with a prefix
    ///                      (used to wipe all cached "get all" lists for a user)
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Retrieve a cached value. Returns null (default) if not found or expired.
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Store a value in cache with sliding + absolute expiration from config.
        /// </summary>
        Task SetAsync<T>(string key, T value);

        /// <summary>
        /// Remove a single cache key immediately.
        /// Called on Update and Delete to invalidate stale data.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove all keys that start with the given prefix.
        /// Used to invalidate the "all projects for user X" list
        /// when a project is created, updated, or deleted.
        /// </summary>
        Task RemoveByPrefixAsync(string prefix);
    }
}
