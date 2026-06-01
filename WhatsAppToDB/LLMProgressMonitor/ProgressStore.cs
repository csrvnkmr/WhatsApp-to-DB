using System.Collections.Concurrent;

public static class ProgressStore
{
    private static readonly ConcurrentDictionary<string, ProgressChannel> _store = new();
    public static void Register(string id, ProgressChannel ch) => _store[id] = ch;
    public static bool TryGet(string id, out ProgressChannel ch) => _store.TryGetValue(id, out ch);
    public static void Remove(string id) => _store.TryRemove(id, out _);
}