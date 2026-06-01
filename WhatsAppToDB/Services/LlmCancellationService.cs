using System.Collections.Concurrent;
using System.Threading;

namespace WhatsAppToDB.Services
{
    public class LlmCancellationService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _sources =
            new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        public void Register(string key, CancellationTokenSource tokenSource)
        {
            if (string.IsNullOrWhiteSpace(key) || tokenSource == null)
                return;

            if (_sources.TryGetValue(key, out var existing) && existing != tokenSource)
            {
                try
                {
                    existing.Cancel();
                }
                catch { }
                finally
                {
                    existing.Dispose();
                }
            }

            _sources[key] = tokenSource;
        }

        public bool TryCancel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (_sources.TryRemove(key, out var tokenSource))
            {
                try
                {
                    tokenSource.Cancel();
                    return true;
                }
                finally
                {
                    tokenSource.Dispose();
                }
            }

            return false;
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_sources.TryRemove(key, out var tokenSource))
            {
                try
                {
                    if (!tokenSource.IsCancellationRequested)
                    {
                        tokenSource.Cancel();
                    }
                }
                catch { }
                finally
                {
                    tokenSource.Dispose();
                }
            }
        }

        public bool TryGetToken(string key, out CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(key)
                && _sources.TryGetValue(key, out var tokenSource))
            {
                token = tokenSource.Token;
                return true;
            }

            token = CancellationToken.None;
            return false;
        }
    }
}
