using MonkeyCache.FileStore;

namespace VibeCooking.Services;

/// <summary>
/// Production implementation of IBarrel — delegates to MonkeyCache's Barrel.Current.
/// </summary>
public class MonkeyBarrel : IBarrel
{
    public bool IsExpired(string key) => Barrel.Current.IsExpired(key);
    public T? Get<T>(string key) => Barrel.Current.Get<T>(key);
    public void Add<T>(string key, T data, TimeSpan expireIn) =>
        Barrel.Current.Add(key, data, expireIn);
}
