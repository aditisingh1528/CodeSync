using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace FileService.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache          _cache;
        private readonly IConnectionMultiplexer     _redis;
        private readonly DistributedCacheEntryOptions _options;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(
            IDistributedCache          cache,
            IConnectionMultiplexer     redis,
            IConfiguration             config,
            ILogger<RedisCacheService> logger)
        {
            _cache  = cache;
            _redis  = redis;
            _logger = logger;

            var sliding  = config.GetValue<int>("CacheSettings:SlidingExpirationSeconds",  120);
            var absolute = config.GetValue<int>("CacheSettings:AbsoluteExpirationSeconds", 600);

            _options = new DistributedCacheEntryOptions
            {
                SlidingExpiration               = TimeSpan.FromSeconds(sliding),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(absolute)
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var json = await _cache.GetStringAsync(key);
                if (json is null) { _logger.LogDebug("Cache MISS → {Key}", key); return default; }
                _logger.LogDebug("Cache HIT  → {Key}", key);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GET failed for {Key}: {Error}", key, ex.Message);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, json, _options);
                _logger.LogDebug("Cache SET  → {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis SET failed for {Key}: {Error}", key, ex.Message);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Cache DEL  → {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis REMOVE failed for {Key}: {Error}", key, ex.Message);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys   = server.Keys(pattern: $"{prefix}*").ToArray();
                if (keys.Length > 0)
                {
                    await _redis.GetDatabase().KeyDeleteAsync(keys);
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
