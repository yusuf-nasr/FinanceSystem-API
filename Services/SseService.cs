using System.Collections.Concurrent;
using System.Threading.Channels;
using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Services
{
    /// <summary>
    /// Manages Server-Sent Events (SSE) connections per user, matching Node's SseService.
    /// Tracks active connections and updates user presence accordingly.
    /// </summary>
    public interface ISseService
    {
        void EmitToUser(int userId, string eventType, object data);
        IAsyncEnumerable<SseEvent> SubscribeToUser(int userId, CancellationToken cancellationToken);
    }

    public class SseEvent
    {
        public string Type { get; set; }
        public object Data { get; set; }
    }

    public class SseService : ISseService
    {
        private readonly ConcurrentDictionary<int, List<Channel<SseEvent>>> _userChannels = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public SseService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void EmitToUser(int userId, string eventType, object data)
        {
            if (_userChannels.TryGetValue(userId, out var channels))
            {
                var sseEvent = new SseEvent { Type = eventType, Data = data };
                // Write to all channels for this user (multiple tabs/connections)
                lock (channels)
                {
                    foreach (var channel in channels)
                    {
                        channel.Writer.TryWrite(sseEvent);
                    }
                }
            }
        }

        public async IAsyncEnumerable<SseEvent> SubscribeToUser(int userId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<SseEvent>();

            // Add channel
            var channels = _userChannels.GetOrAdd(userId, _ => new List<Channel<SseEvent>>());
            bool isFirst;
            lock (channels)
            {
                isFirst = channels.Count == 0;
                channels.Add(channel);
            }

            // Set user online if first connection
            if (isFirst)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                        await userService.UpdatePresenceAsync(userId, UserPresence.ONLINE);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to set user {userId} online: {ex.Message}");
                    }
                });
            }

            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return item;
                }
            }
            finally
            {
                // Remove channel
                bool isLast;
                lock (channels)
                {
                    channels.Remove(channel);
                    isLast = channels.Count == 0;
                }

                if (isLast)
                {
                    _userChannels.TryRemove(userId, out _);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                            await userService.UpdatePresenceAsync(userId, UserPresence.OFFLINE);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Failed to set user {userId} offline: {ex.Message}");
                        }
                    });
                }
            }
        }
    }
}
