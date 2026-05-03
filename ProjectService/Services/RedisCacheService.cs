using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace ProjectService.Services
{
    /// <summary>
    /// REDISCACHESERVICE
    /// ==================
    /// Concrete implementation of ICacheService using Redis
    /// via IDistributedCache (registered in Program.cs with AddStackExchangeRedisCache).
    ///
    /// SERIALIZATION:
    ///   Objects are serialized to JSON using System.Text.Json before storing in Redis.
    ///   Redis stores only strings/bytes — we can't store C# objects directly.
    ///
    /// EXPIRY STRATEGY (both set together):
    ///   SlidingExpiration  = 2 min → if key is accessed, timer resets
    ///   AbsoluteExpiration = 10 min → hard cap — key always expires after 10 min
    ///   Together they prevent both: stale long-lived cache AND excessive eviction.
    ///
    /// CACHE KEY NAMING CONVENTION (defined in ProjectServiceImpl):
    ///   project:user:{userId}:all       → list of all projects for a user
    ///   project:user:{userId}:{id}      → single project
    ///
    /// PREFIX DELETION:
    ///   IDistributedCache has no built-in "delete by prefix" feature.
    ///   We use the raw IConnectionMultiplexer (direct Redis connection)
    ///   to call KEYS command and delete matching keys.
    ///   This is safe for dev/staging. For very large Redis keyspaces in
    ///   production, SCAN would be preferred over KEYS.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache      _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly DistributedCacheEntryOptions _cacheOptions;
        private readonly ILogger<RedisCacheService>   _logger;

        public RedisCacheService(
            IDistributedCache      cache,
            IConnectionMultiplexer redis,
            IConfiguration         config,
            ILogger<RedisCacheService> logger)
        {
            _cache  = cache;
            _redis  = redis;
            _logger = logger;

            // Read expiry values from config — defaults if missing
            var sliding  = config.GetValue<int>("CacheSettings:SlidingExpirationSeconds",  120);
            var absolute = config.GetValue<int>("CacheSettings:AbsoluteExpirationSeconds", 600);

            _cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration  = TimeSpan.FromSeconds(sliding),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(absolute)
            };
        }

        // ─────────────────────────────────────────────────────────────────
        // GET
        // ─────────────────────────────────────────────────────────────────
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var json = await _cache.GetStringAsync(key);

                if (json is null)
                {
                    _logger.LogDebug("Cache MISS → key: {Key}", key);
                    return default;           // null — caller must fetch from DB
                }

                _logger.LogDebug("Cache HIT  → key: {Key}", key);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                // Redis being down should NOT crash the app — just fall through to DB
                _logger.LogWarning("Redis GET failed for key {Key}: {Error}", key, ex.Message);
                return default;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SET
        // ─────────────────────────────────────────────────────────────────
        public async Task SetAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, json, _cacheOptions);
                _logger.LogDebug("Cache SET  → key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis SET failed for key {Key}: {Error}", key, ex.Message);
                // Don't throw — caching failure is non-fatal
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // REMOVE (single key)
        // ─────────────────────────────────────────────────────────────────
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Cache DEL  → key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis REMOVE failed for key {Key}: {Error}", key, ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // REMOVE BY PREFIX
        // Uses raw Redis KEYS command to find all matching keys, then deletes them.
        // Example: prefix "project:user:5:" deletes all cached data for user 5.
        // ─────────────────────────────────────────────────────────────────
        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var db     = _redis.GetDatabase();
                var server = _redis.GetServer(_redis.GetEndPoints().First());

                // KEYS pattern — get all keys starting with prefix
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();

                if (keys.Length > 0)
                {
                    await db.KeyDeleteAsync(keys);
                    _logger.LogDebug("Cache DEL prefix → {Prefix}* ({Count} keys)", prefix, keys.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis REMOVE BY PREFIX failed for {Prefix}: {Error}", prefix, ex.Message);
            }
        }
    }
}
