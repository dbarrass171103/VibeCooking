namespace VibeCooking.Services;

/// <summary>
/// Thin abstraction over MonkeyCache's Barrel.Current static singleton.
/// Allows LocalStorageService to be unit-tested without touching the file system.
/// </summary>
public interface IBarrel
{
    bool IsExpired(string key);
    T? Get<T>(string key);
    void Add<T>(string key, T data, TimeSpan expireIn);
}
