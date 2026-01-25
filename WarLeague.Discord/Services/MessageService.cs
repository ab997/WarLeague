using Discord;
using Discord.WebSocket;

namespace WarLeague.Discord.Services;

public class MessageService
{
    private readonly Dictionary<ulong, ulong> _standingsMessageIds = new(); // ChannelId -> MessageId

    public void TrackStandingsMessage(ulong channelId, ulong messageId)
    {
        _standingsMessageIds[channelId] = messageId;
    }

    public ulong? GetStandingsMessageId(ulong channelId)
    {
        return _standingsMessageIds.TryGetValue(channelId, out var messageId) ? messageId : null;
    }

    public async Task UpdateOrDeleteStandingsMessageAsync(SocketTextChannel channel, string newContent)
    {
        var messageId = GetStandingsMessageId(channel.Id);
        if (messageId.HasValue)
        {
            try
            {
                var message = await channel.GetMessageAsync(messageId.Value);
                if (message != null)
                {
                    await message.DeleteAsync();
                }
            }
            catch
            {
                // Message might not exist, ignore
            }
        }

        var newMessage = await channel.SendMessageAsync(newContent);
        TrackStandingsMessage(channel.Id, newMessage.Id);
    }
}
