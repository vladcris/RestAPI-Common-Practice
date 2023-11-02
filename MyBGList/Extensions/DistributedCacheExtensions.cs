using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace MyBGList.Extensions;

public static class DistributedCacheExtensions
{
    public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T? value) where T : class
    {
        value = default;
        byte[] result = cache.Get(key);
        if(result == null) {
            return false;
        }
        value = JsonSerializer.Deserialize<T>(result);
        return true;
    }

    public static void Set<T>(this IDistributedCache cache, string key, T value, TimeSpan absoluteExpirationRelativeToNow) where T : class {
        var options = new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        };

        //byte[] cacheValue = JsonSerializer.SerializeToUtf8Bytes(value);
        byte[] cacheValue = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        cache.Set(key, cacheValue, options);
    }   
}
